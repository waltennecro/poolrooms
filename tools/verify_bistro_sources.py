"""Offline audit of attributed Bistro adaptations, authored material bindings and scale."""
from pathlib import Path
import hashlib,json,math,re,struct,zlib
ROOT=Path(__file__).resolve().parents[1];source=ROOT/'Sources/Bistro';dest=ROOT/'Assets/PoolroomsProcedural/Resources/Bistro'
lock=json.loads((source/'sources.lock.json').read_text());rev='2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e'
original=(source/'bistro_source.gltf').read_bytes();assert hashlib.sha256(original).hexdigest()==lock['raw.githubusercontent.com/zeux/niagara_bistro/'+rev+'/bistro.gltf']['sha256']
assert lock['raw.githubusercontent.com/zeux/niagara_bistro/'+rev+'/bistro.bin']['sha256']=='5fb11a1d61aeb2537197aa97833ff950638c9d42451ee4fafbc9550f03918faa'
for key,record in lock.items():
 assert record['url']=='https://'+key and record['bytes']>0 and re.fullmatch('[0-9a-f]{64}',record['sha256'])
 assert key.startswith('raw.githubusercontent.com/zeux/niagara_bistro/'+rev+'/') or key=='raw.githubusercontent.com/neftaly/bistro-gltf/41b6f6ca07da385b5be8f632f440b25a03a427f5/LICENSE'
for name in ['Bistro_CC_BY_4.txt','upstream_MIT.txt','upstream_README.md','NOTICE.txt']:assert (source/name).read_bytes()==(dest/'Licenses'/name).read_bytes()
assert 'Attribution 4.0 International' in (source/'Bistro_CC_BY_4.txt').read_text() and 'Amazon.com' in (source/'NOTICE.txt').read_text()
manifest=json.loads((dest/'files.sha256.json').read_text());assert set(manifest)=={p.relative_to(dest).as_posix() for p in dest.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
maps=0
for name,record in manifest.items():
 raw=(dest/name).read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],name
 if name.endswith('.png'):
  maps+=1;assert raw[:8]==b'\x89PNG\r\n\x1a\n';w,h=struct.unpack('>II',raw[16:24]);assert 0<w<=1024 and 0<h<=1024
  pos=8;pixels=bytearray();ended=False
  while pos<len(raw):
   size=struct.unpack_from('>I',raw,pos)[0];tag=raw[pos+4:pos+8];payload=raw[pos+8:pos+8+size];crc=struct.unpack_from('>I',raw,pos+8+size)[0];assert zlib.crc32(tag+payload)&0xffffffff==crc
   if tag==b'IDAT':pixels.extend(payload)
   if tag==b'IEND':ended=True
   pos+=size+12
  assert ended and zlib.decompress(pixels)
core=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text();catalog=json.loads((dest/'catalog.json').read_text());assert catalog['distinctGeometryFamilies']==3 and catalog['completeBuildingModels']==0 and catalog['nativeUnityValidated'] is False
for name,tri,source_id,longest,height in [('Awning',7032,None,2.1,.89355332),('CafeTable',1692,7,.9,.84341568),('CafeChair',3328,8,1,1.02585685)]:
 d=json.loads((dest/('B_'+name+'.json')).read_text());raw=(source/(name+'_adapted.glb')).read_bytes();assert d['sourceSha256']==hashlib.sha256(raw).hexdigest();assert d['modelLicense']=='CC-BY-4.0' and d['upstreamRevision']==rev and d['originalTriangles']==d['triangles']==tri
 n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n]);assert len(doc['meshes'])==1 and not doc.get('animations')
 assert all(m['workflow']=='specular' and m['specular'].startswith('Bistro/Textures/') for m in d['materials'])
 for material in d['materials']:
  for key in ['albedo','normal','mask','specular']:assert (ROOT/'Assets/PoolroomsProcedural/Resources'/(material[key]+'.png')).is_file()
 dims=[b-a for a,b in zip(d['minimum'],d['maximum'])];assert abs(dims[1]-height)<1e-5,(name,'Incorrect original vertical axis')
 if source_id is not None:
  encoded=re.search(r'case '+str(source_id)+r':return new V3\(([^)]+)\)',core).group(1);actual=[float(x.rstrip('f')) for x in encoded.split(',')];assert all(abs(a-b/max(dims)*longest)<1e-5 for a,b in zip(actual,dims)),(name,actual,dims)
 counts=[tri]+[lod['triangles'] for lod in d['lods']];assert counts[0]>counts[1]>counts[2]>0
 for index,resource in enumerate([d['resource']]+[lod['resource'] for lod in d['lods']]):
  positions=[];normals=[];uv=[];faces=0
  for line in (ROOT/'Assets/PoolroomsProcedural/Resources'/(resource+'.obj')).read_text().splitlines():
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
  assert faces==counts[index],(name,index,faces,counts[index])
  if index==0:
   for axis in range(3):assert abs(min(p[axis] for p in positions)-d['minimum'][axis])<1e-5 and abs(max(p[axis] for p in positions)-d['maximum'][axis])<1e-5
print(f'Verified three CC-BY Bistro component adaptations, {maps} bounded PNGs, original metric up-axis, material/source hashes and two LODs per model. No downloaded complete buildings or native Unity certification.')
