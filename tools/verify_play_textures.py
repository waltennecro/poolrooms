"""Check original PBR texture hashes, dimensions and PNG integrity without image libraries."""
from pathlib import Path
import hashlib
import json
import struct
import zlib

root=Path(__file__).resolve().parents[1]
reports=[json.loads((root/name).read_text()) for name in ['Verification/play_finish_textures.json','Verification/quiet_sand_textures.json','Verification/airfield_finish_textures.json','Verification/helicopter_finish_textures.json','Verification/orangerie_textures.json','Verification/bath_textures.json','Verification/aquatic_textures.json','Verification/flume_textures.json','Verification/rain_textures.json']]
expected_maps={name:(record,report['size']) for report in reports for name,record in report['files'].items()}
for name,(expected,expected_size) in expected_maps.items():
    path=root/name
    data=path.read_bytes()
    if len(data)!=expected["bytes"] or hashlib.sha256(data).hexdigest()!=expected["sha256"]:
        raise SystemExit("Changed PBR texture: "+name)
    if data[:8]!=b"\x89PNG\r\n\x1a\n":raise SystemExit("Not PNG: "+name)
    width,height=struct.unpack(">II",data[16:24])
    if width!=expected_size or height!=expected_size:raise SystemExit("Wrong dimensions: "+name)
    offset=8;image=bytearray();ended=False
    while offset<len(data):
        size=struct.unpack(">I",data[offset:offset+4])[0]
        tag=data[offset+4:offset+8];payload=data[offset+8:offset+8+size]
        crc=struct.unpack(">I",data[offset+8+size:offset+12+size])[0]
        if zlib.crc32(tag+payload)&0xffffffff!=crc:raise SystemExit("PNG CRC mismatch: "+name)
        if tag==b"IDAT":image.extend(payload)
        if tag==b"IEND":ended=True
        offset+=size+12
    if not ended or not zlib.decompress(image):raise SystemExit("Incomplete PNG: "+name)
print(f'Verified {len(expected_maps)} PBR maps: SHA-256, 1024px, PNG CRC and decompression.')
