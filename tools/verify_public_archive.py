"""Verify a fresh public GitHub ZIP against every tracked file without storing it.

Keeps bounded transfer buffers for workspaces that cannot hold a second 500 MB ZIP.
Checks every file's SHA-256, ZIP CRC/length, entry set and end-of-directory record.
Only supports the stored/deflated, non-ZIP64 archives currently emitted by codeload.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import urllib.request
import zlib


class Download:
    def __init__(self, response):
        self.response = response
        self.buffer = b""
        self.tail = b""
        self.hash = hashlib.sha256()
        self.size = 0

    def fill(self):
        chunk = self.response.read(1024 * 1024)
        if chunk:
            self.hash.update(chunk)
            self.size += len(chunk)
            self.tail = (self.tail + chunk)[-65557:]
        return chunk

    def read(self, count):
        while len(self.buffer) < count:
            chunk = self.fill()
            if not chunk:
                raise ValueError("Truncated ZIP")
            self.buffer += chunk
        result, self.buffer = self.buffer[:count], self.buffer[count:]
        return result

    def finish(self):
        self.buffer = b""
        while self.fill():
            pass


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--revision", required=True)
    parser.add_argument("--report", required=True, type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    if args.revision != revision or subprocess.check_output(["git", "status", "--porcelain"], cwd=root).strip():
        raise SystemExit("Use the current committed, clean source revision")
    tracked = set(subprocess.check_output(["git", "ls-files", "-z"], cwd=root).decode().split("\0")) - {""}
    matched = set()
    url = "https://codeload.github.com/waltennecro/poolrooms/zip/" + revision
    entries = 0
    with urllib.request.urlopen(url, timeout=120) as response:
        reader = Download(response)
        while True:
            signature = reader.read(4)
            if signature == b"PK\x01\x02":
                break
            if signature != b"PK\x03\x04":
                raise ValueError("Invalid local ZIP header")
            _, flags, method, _, _, crc, packed, size, name_size, extra_size = struct.unpack("<HHHHHIIIHH", reader.read(26))
            name = reader.read(name_size).decode("utf-8")
            reader.read(extra_size)
            if flags & 9 or method not in (0, 8) or packed == 0xFFFFFFFF or size == 0xFFFFFFFF:
                raise ValueError("Unsupported encrypted, descriptor, ZIP64 or compression layout")
            digest, actual_crc, actual_size = hashlib.sha256(), 0, 0
            inflater = zlib.decompressobj(-15) if method == 8 else None
            remaining = packed
            while remaining:
                chunk = reader.read(min(65536, remaining))
                remaining -= len(chunk)
                decoded = inflater.decompress(chunk) if inflater else chunk
                digest.update(decoded)
                actual_crc = zlib.crc32(decoded, actual_crc)
                actual_size += len(decoded)
            if inflater:
                decoded = inflater.flush()
                digest.update(decoded)
                actual_crc = zlib.crc32(decoded, actual_crc)
                actual_size += len(decoded)
                if not inflater.eof or inflater.unused_data:
                    raise ValueError("Invalid deflate stream")
            if actual_size != size or actual_crc != crc:
                raise ValueError("ZIP CRC/size mismatch: " + name)
            entries += 1
            if name.endswith("/"):
                continue
            relative = name.split("/", 1)[1]
            if relative not in tracked or relative in matched:
                raise ValueError("Unexpected or duplicate archive file: " + relative)
            expected = hashlib.sha256()
            with (root / relative).open("rb") as local:
                for chunk in iter(lambda: local.read(1024 * 1024), b""):
                    expected.update(chunk)
            if digest.digest() != expected.digest():
                raise ValueError("Public source differs: " + relative)
            matched.add(relative)
        reader.finish()
        end = reader.tail.rfind(b"PK\x05\x06")
        if end < 0 or len(reader.tail) - end < 22:
            raise ValueError("Missing ZIP end record")
        _, disk, directory_disk, on_disk, total, _, _, comment_size = struct.unpack("<4sHHHHIIH", reader.tail[end:end + 22])
        if disk or directory_disk or on_disk != entries or total != entries or len(reader.tail) - end != 22 + comment_size:
            raise ValueError("Invalid ZIP end record")
    if matched != tracked:
        raise ValueError("Files missing from public archive")
    report = {"result": "PASS", "revision": revision, "url": url,
              "bytes": reader.size, "sha256": reader.hash.hexdigest(),
              "matchedTrackedFiles": len(matched),
              "matchedAssetFiles": sum(p.startswith("Assets/PoolroomsProcedural/") for p in matched),
              "everyFileSha256AndCrc": True, "archiveStoredOnDisk": False}
    args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))


if __name__ == "__main__":
    main()
