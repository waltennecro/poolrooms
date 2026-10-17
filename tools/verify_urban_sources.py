"""Audit licensed additional sources, original provenance and native derivatives."""
from pathlib import Path
import hashlib,json,math,re,struct,zlib
ROOT=Path(__file__).resolve().parents[1];DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Urban';SOURCE=ROOT/'Sources/Urban'
lock=json.loads((SOURCE/'sources.lock.json').read_text())
for name,item in lock.items():
 raw=(SOURCE/name).read_bytes();assert len(raw)==item['bytes'] and hashlib.sha256(raw).hexdigest()==item['sha256'],name
 assert item['url'].startswith('https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/edc7c9e67c639d230715049ee31f9a96a6babbbe/')
files=json.loads((DEST/'files.sha256.json').read_text());assert set(files)=={p.relative_to(DEST).as_posix() for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
images=0
for name,item in files.items():
 raw=(DEST/name).read_bytes();assert len(raw)==item['bytes'] and hashlib.sha256(raw).hexdigest()==item['sha256'],name
 if not name.endswith('.png'):continue
 images+=1;assert raw[:8]==b'\x89PNG\r\n\x1a\n';w,h=struct.unpack('>II',raw[16:24]);assert w==h and w in (4,1024,2048)
 cursor=8;compressed=bytearray();ended=False
 while cursor<len(raw):
  n=struct.unpack('>I',raw[cursor:cursor+4])[0];kind=raw[cursor+4:cursor+8];data=raw[cursor+8:cursor+8+n];assert zlib.crc32(kind+data)&0xffffffff==struct.unpack('>I',raw[cursor+8+n:cursor+12+n])[0]
  if kind==b'IDAT':compressed.extend(data)
  if kind==b'IEND':ended=True
  cursor+=n+12
 assert ended and zlib.decompress(compressed)
catalog=json.loads((DEST/'catalog.json').read_text());assert catalog['distinctSourceModels']==3 and not catalog['variantsCountAsModels'];assert not catalog['nativeUnityValidated']
counts={}
for model in catalog['models']:
 name=model['id'];assert model['modelLicense'] in ('CC0-1.0','CC-BY-4.0') and model['authors'] and model['legal'];assert len(model['lods'])==2
 assert all(item.get('spdx',item['license']) in ('CC0-1.0','CC-BY-4.0') or item.get('spdx',item['license']).startswith('LicenseRef-LegalMark-') for item in model['legal'])
 assert model['sourceSha256']==lock[name[2:]+'.glb']['sha256']
 if name!='U_AntiqueCamera':assert model['uvBake'] and any(m['sourceUV']==1 for m in model['uvBake'])
 else:assert not model['uvBake'],'Already compatible UV0 must stay original'
 assert model['lods'][1]['triangles']<model['lods'][0]['triangles']<model['triangles']
 for level in [model]+model['lods']:
  p=ROOT/'Assets/PoolroomsProcedural/Resources'/(level['resource']+'.obj');positions=[];normal=[];uv=0;uv_values=[];faces=0;groups={};group=None;referenced=set()
  attribute_hash=hashlib.sha256() if level is model and 'losslessNativeCompaction' in model else None
  with p.open() as stream:
   for line in stream:
    s=line.split()
    if not s:continue
    if s[0]=='o':
     group=s[1];groups.setdefault(group,0)
     if attribute_hash is not None:attribute_hash.update(str(int(re.search(r'_m(\d+)$',group).group(1))).encode())
    elif s[0] in ('v','vn','vt'):
     value=list(map(float,s[1:]));assert all(math.isfinite(x) for x in value),p
     if s[0]=='v':positions.append(value)
     elif s[0]=='vn':normal.append(value)
     else:uv+=1;uv_values.append(value)
    elif s[0]=='f':
     assert len(s)==4;faces+=1;groups[group]+=1
     triangle=[]
     for token in s[1:]:
      a,b,c=map(int,token.split('/'));assert 0<a<=len(positions) and 0<b<=uv and 0<c<=len(normal);referenced.add(c-1)
      if attribute_hash is not None:triangle.extend(positions[a-1]+normal[c-1]+uv_values[b-1])
     if attribute_hash is not None:attribute_hash.update(struct.pack('<24d',*triangle))
  assert faces==level['triangles'];assert all(.99<math.sqrt(sum(x*x for x in normal[i]))<1.01 for i in referenced),'Referenced normal is not unit length: '+str(p)
  if attribute_hash is not None:
   assert attribute_hash.hexdigest()==model['losslessNativeCompaction']['perTriangleAttributeSha256'],'Lossless source triangle attributes changed'
   assert len(positions)==model['losslessNativeCompaction']['afterVertices']
  if name=='U_CarConcept':
   for wheel in model['wheels']:
    assert sum(n for g,n in groups.items() if wheel['part'] in g and '_Caliper' not in g)>40
    assert any(wheel['part']+'_Caliper' in g and n>0 for g,n in groups.items())
 if name=='U_CarConcept':
  assert len(model['wheels'])==4 and sum(x['front'] for x in model['wheels'])==2
  assert set(model['removedTrademarkMeshes'])=={'License Plate','InteriorSteeringEmblem'}
 else:
  longest=2 if 'Sofa' in name else 1.5;dims=[(b-a)*longest/max(y-x for x,y in zip(model['minimum'],model['maximum'])) for a,b in zip(model['minimum'],model['maximum'])]
  source=5 if 'Sofa' in name else 6;text=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text();match=re.search(r'case '+str(source)+r':return new V3\(([^)]+)\)',text)
  assert match;planned=[float(x.strip().rstrip('f')) for x in match.group(1).split(',')];assert max(abs(a-b) for a,b in zip(dims,planned))<.00001,(name,dims,planned)
 counts[name]=[model['triangles']]+[x['triangles'] for x in model['lods']]
print(json.dumps({'distinctNewSourceModels':3,'modelsAndDerivedLodTriangles':counts,'mapsAudited':images,'nativeUnityValidated':False}))
