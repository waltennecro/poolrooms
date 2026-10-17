"""Verify the recovered v39 bytes and optionally reproduce its Assets package."""

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--zip", type=Path, help="Write a verified Assets package")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    manifest = json.loads((root / "RECUPERACION_V39.json").read_text(encoding="utf-8"))
    expected = manifest["files"]
    actual = {
        path.relative_to(root).as_posix()
        for path in (root / "Assets" / "PoolroomsProcedural").rglob("*")
        if path.is_file()
    }
    errors = []
    for name, record in expected.items():
        relative = PurePosixPath(name)
        if relative.is_absolute() or ".." in relative.parts or "\\" in name:
            errors.append(f"Invalid manifest path: {name}")
            continue
        path = root / relative
        if not path.is_file():
            errors.append(f"Missing: {name}")
            continue
        data = path.read_bytes()
        if len(data) != record["bytes"] or hashlib.sha256(data).hexdigest() != record["sha256"]:
            errors.append(f"Changed: {name}")
    for name in sorted(actual - expected.keys()):
        errors.append(f"Unlisted: {name}")
    if errors:
        raise SystemExit("\n".join(errors))
    print(f"Verified {len(expected)} original v39 files (SHA-256).")
    if args.zip:
        output = args.zip.resolve()
        if output.is_relative_to(root / "Assets"):
            raise SystemExit("Write the archive outside Assets.")
        if output.exists():
            raise SystemExit(f"Refusing to overwrite: {output}")
        output.parent.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for name in sorted(expected):
                archive.write(root / name, PurePosixPath(name).relative_to("Assets").as_posix())
        with zipfile.ZipFile(output) as archive:
            if archive.testzip() is not None:
                raise SystemExit("Archive CRC verification failed.")
        print(f"Created {output}")


if __name__ == "__main__":
    main()
