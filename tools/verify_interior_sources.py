"""Offline audit of furniture licenses, baked maps, geometry and Core scale contracts."""
from pathlib import Path
import hashlib,json,math,re,struct,zlib
ROOT=Path(__file__).resolve().parents[1];source=ROOT/'Sources/Interiors';dest=ROOT/'Assets/PoolroomsProcedural/Resources/Interiors'
rev='edc7c9e67c639d230715049ee31f9a96a6babbbe';lock=json.loads((source/'sources.lock.json').read_text())
for name,record in lock.items():
 raw=(source/name).read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],name
 assert record['url'].startswith('https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/'+rev+'/')
 if name.endswith('_metadata.json'):assert all(x['spdx']=='CC-BY-4.0' for x in json.loads(raw)['legal'])
for name in ['CC-BY-4.0.txt','GlamVelvetSofa_LICENSE.md','ChairDamaskPurplegold_LICENSE.md','GlamVelvetSofa_metadata.json','ChairDamaskPurplegold_metadata.json']:
 assert (dest/'Licenses'/name).read_bytes()==(source/name).read_bytes()
notice=(dest/'Licenses/NOTICE.txt').read_text();assert 'Eric Chadwick' in notice and 'Wayfair' in notice and 'Changes:' in notice
manifest=json.loads((dest/'files.sha256.json').read_text());assert set(manifest)=={p.relative_to(dest).as_posix() for p in dest.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
maps=0
for name,record in manifest.items():
 raw=(dest/name).read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],name
 if name.endswith('.png'):
  maps+=1;assert raw[:8]==b'\x89PNG\r\n\x1a\n';w,h=struct.unpack('>II',raw[16:24]);assert w<=2048 and h<=2048
  pos=8;pixels=bytearray();ended=False
  while pos<len(raw):
   size=struct.unpack('>I',raw[pos:pos+4])[0];tag=raw[pos+4:pos+8];payload=raw[pos+8:pos+8+size];crc=struct.unpack('>I',raw[pos+8+size:pos+12+size])[0];assert zlib.crc32(tag+payload)&0xffffffff==crc
   if tag==b'IDAT':pixels.extend(payload)
   if tag==b'IEND':ended=True
   pos+=size+12
  assert ended and zlib.decompress(pixels)
catalog=json.loads((dest/'catalog.json').read_text());assert catalog['sourceFiles']==2 and catalog['newGeometryFamilies']==1 and catalog['existingChairVariantSources']==1
core=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text()
for model,count,source_id,longest in [('GlamVelvetSofa',4196,3,2),('ChairDamaskPurplegold',9984,4,1)]:
 d=json.loads((dest/('I_'+model+'.json')).read_text());assert d['triangles']==count and d['modelLicense']=='CC-BY-4.0' and d['sourceSha256']==lock[model+'.glb']['sha256']
 assert d['defaultMaterialVariantOnly'] and d['uvBake'] and all(x['bakedPixels']>0 and x['resolution']==2048 for x in d['uvBake'])
 if source_id==3:assert d['variantOf'] is None and d['atlasSeedFromUV0']
 else:assert d['variantOf']=='SheenChair' and not d['atlasSeedFromUV0']
 size=[b-a for a,b in zip(d['minimum'],d['maximum'])];expected=[v/max(size)*longest for v in size]
 encoded=re.search(r'case '+str(source_id)+r':return new V3\(([^)]+)\)',core).group(1);actual=[float(x.rstrip('f')) for x in encoded.split(',')];assert all(abs(a-b)<1e-5 for a,b in zip(actual,expected)),(model,actual,expected)
 positions=[];normals=[];uv=[];faces=0
 for line in (dest/'Models'/('I_'+model+'.obj')).read_text().splitlines():
  p=line.split()
  if not p:continue
  if p[0] in ('v','vn','vt'):
   value=list(map(float,p[1:]));assert all(map(math.isfinite,value));{'v':positions,'vn':normals,'vt':uv}[p[0]].append(value)
   if p[0]=='vn':assert abs(sum(v*v for v in value)-1)<.0001
  elif p[0]=='usemtl':assert p[1] in {m['name'] for m in d['materials']}
  elif p[0]=='f':
   faces+=1;assert len(p)==4
   for ref in p[1:]:
    a,b,c=map(int,ref.split('/'));assert 0<a<=len(positions) and 0<b<=len(uv) and 0<c<=len(normals)
 assert faces==count and all(-1e-5<=x<=1.00001 for u in uv for x in u)
 # The shipped bounds and scale contract must describe actual exported vertices.
 for axis in range(3):assert abs(min(p[axis] for p in positions)-d['minimum'][axis])<1e-5 and abs(max(p[axis] for p in positions)-d['maximum'][axis])<1e-5
print(f'Verified two CC-BY furniture source files: one new sofa geometry family and one variant of the existing oval chair; 4196/9984 triangles, {maps} baked PNG maps, licences and Core dimensions. Native Unity validation pending.')
