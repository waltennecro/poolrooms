"""Audit aircraft source provenance, Apache license, native OBJ and resource hashes."""
from pathlib import Path
import hashlib,json,math,struct,zlib
r=Path(__file__).resolve().parents[1];sources=r/'Sources/Aircraft';assets=r/'Assets/PoolroomsProcedural/Resources/Aircraft'
lock=json.loads((sources/'sources.lock.json').read_text())
for name,record in lock.items():
 data=(sources/name).read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
 assert record['url'].startswith('https://raw.githubusercontent.com/CesiumGS/cesium/05adbc742db7c0aa9d53836ba46a5bbfdbb9fbc1/')
assert (assets/'Licenses/Cesium_LICENSE.md').read_bytes()==(sources/'Cesium_LICENSE.md').read_bytes()
assert b'Apache License' in (assets/'Licenses/Cesium_LICENSE.md').read_bytes()
expected=json.loads((assets/'files.sha256.json').read_text());actual={p.relative_to(assets).as_posix() for p in assets.rglob('*') if p.is_file() and p.name!='files.sha256.json'};assert set(expected)==actual
for name,record in expected.items():
 data=(assets/name).read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
 if name.endswith('.png'):
  assert data[:8]==b'\x89PNG\r\n\x1a\n';width,height=struct.unpack('>II',data[16:24]);assert 0<width<=2048 and 0<height<=2048
  offset=8;pixels=bytearray();ended=False
  while offset<len(data):
   size=struct.unpack('>I',data[offset:offset+4])[0];tag=data[offset+4:offset+8];payload=data[offset+8:offset+8+size];crc=struct.unpack('>I',data[offset+8+size:offset+12+size])[0]
   assert zlib.crc32(tag+payload)&0xffffffff==crc
   if tag==b'IDAT':pixels.extend(payload)
   if tag==b'IEND':ended=True
   offset+=size+12
  assert ended and zlib.decompress(pixels)
catalog=json.loads((assets/'catalog.json').read_text());assert catalog['distinctSourceModels']==1 and len(catalog['models'])==1
model=catalog['models'][0];assert model['modelLicense']=='Apache-2.0' and model['sourceSha256']==lock['Cesium_Air.glb']['sha256']
assert sorted(model['animatedPropellerNodes'])==['Prop','Prop__2_'] and model['triangles']==5984
positions=[];normals=[];uvs=[];triangles=0;groups=[]
for line in (assets/'Models/A_CesiumAir.obj').read_text().splitlines():
 parts=line.split()
 if not parts:continue
 if parts[0] in ('v','vn','vt'):
  p=list(map(float,parts[1:]));assert all(map(math.isfinite,p))
  if parts[0]=='v':positions.append(p)
  elif parts[0]=='vn':normals.append(p)
  else:uvs.append(p)
 elif parts[0]=='o':groups.append(parts[1])
 elif parts[0]=='f':
  assert len(parts)==4
  for part in parts[1:]:
   a,b,c=map(int,part.split('/'));assert 0<a<=len(positions) and 0<b<=len(uvs) and 0<c<=len(normals)
   assert abs(sum(v*v for v in normals[c-1])-1)<.002
  triangles+=1
assert triangles==5984 and len([x for x in groups if '_Prop' in x])==4
assert model['materials'][1]['transparent'] and model['materials'][1]['colour'][3]==1
print('Verified 1 distinct Apache-2.0 aircraft source, 5984 native triangles, 2 propellers / 4 material groups and all resource/PNG/license hashes. Native Unity validation pending.')
