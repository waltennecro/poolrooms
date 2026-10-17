"""Offline audit of attributed helicopter data, native geometry, PBR maps and LODs."""
from pathlib import Path
import hashlib,json,math,struct,zlib
ROOT=Path(__file__).resolve().parents[1];source=ROOT/'Sources/Helicopter';dest=ROOT/'Assets/PoolroomsProcedural/Resources/Helicopters'
lock=json.loads((source/'sources.lock.json').read_text());rev='251d6d85cbe5eedbebf7625a8df7fd2430f8d1a1'
for name,record in lock.items():
 raw=(source/name).read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],name
 assert record['url'].startswith('https://raw.githubusercontent.com/jdubba1/warbird/'+rev+'/') or record['url']=='https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/edc7c9e67c639d230715049ee31f9a96a6babbbe/LICENSES/CC-BY-4.0.txt'
raw=(source/'little-bird.js').read_text();start=raw.index('export default ')+15;data,end=json.JSONDecoder().raw_decode(raw[start:]);assert raw[start+end:].strip()==';'
assert set(data)=={'pivots','meshes'} and len(data['meshes'])==6
assert (dest/'Licenses/SOURCE_CREDIT.txt').read_bytes()==(source/'aircraft-license.txt').read_bytes()
assert (dest/'Licenses/CC-BY-4.0.txt').read_bytes()==(source/'CC-BY-4.0.txt').read_bytes()
assert 'AnirudhRao' in (dest/'Licenses/NOTICE.txt').read_text() and 'Changes:' in (dest/'Licenses/NOTICE.txt').read_text()
manifest=json.loads((dest/'files.sha256.json').read_text());assert set(manifest)=={p.relative_to(dest).as_posix() for p in dest.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
for name,record in manifest.items():
 raw=(dest/name).read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],name
 if name.endswith('.png'):
  assert raw[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',raw[16:24])==(1024,1024)
  pos=8;pixels=bytearray();ended=False
  while pos<len(raw):
   size=struct.unpack('>I',raw[pos:pos+4])[0];tag=raw[pos+4:pos+8];payload=raw[pos+8:pos+8+size];crc=struct.unpack('>I',raw[pos+8+size:pos+12+size])[0];assert zlib.crc32(tag+payload)&0xffffffff==crc
   if tag==b'IDAT':pixels.extend(payload)
   if tag==b'IEND':ended=True
   pos+=size+12
  assert ended and zlib.decompress(pixels)
model=json.loads((dest/'H_MH6.json').read_text());assert model['modelLicense']=='CC-BY-4.0' and model['sourceSha256']==lock['little-bird.js']['sha256']
assert model['sourceTriangles']==sum(len(m['indices'])//3 for m in data['meshes'])==40642
assert model['degenerateSourceTrianglesRemoved']==41 and model['pivotMarkerTriangles']==3
assert model['triangles']==model['sourceTriangles']-41+3==40604
for level,count in [(0,40604),(1,10771),(2,5998)]:
 name='H_MH6'+('' if level==0 else '_LOD'+str(level));positions=[];uv=[];normals=[];faces=[];groups={};group=None
 for line in (dest/'Models'/f'{name}.obj').read_text().splitlines():
  p=line.split()
  if not p:continue
  if p[0] in ('v','vt','vn'):
   value=list(map(float,p[1:]));assert all(map(math.isfinite,value));{'v':positions,'vt':uv,'vn':normals}[p[0]].append(value)
   if p[0]=='vn':assert abs(sum(v*v for v in value)-1)<.0001
  elif p[0]=='o':group=p[1];assert group not in groups;groups[group]=[]
  elif p[0]=='usemtl':assert p[1] in {m['name'] for m in model['materials']}
  elif p[0]=='f':
   assert len(p)==4;face=[]
   for ref in p[1:]:
    a,b,c=map(int,ref.split('/'));assert 0<a<=len(positions) and 0<b<=len(uv) and 0<c<=len(normals);face.append(a-1)
   faces.append(face);groups[group].extend(face)
 assert len(faces)==count
 for part in ['main_rotor','tail_rotor','main_pivot','tail_pivot','nose_axis']:assert 'H_MH6_'+part in groups
 for part,source_name in [('main_pivot','rotor'),('tail_pivot','tail')]:
  points=[positions[i] for i in groups['H_MH6_'+part]];center=[(min(p[i] for p in points)+max(p[i] for p in points))*.5 for i in range(3)]
  assert all(abs(a-b)<1e-6 for a,b in zip(center,data['pivots'][source_name])),part
 if level>0:assert model['lods'][level-1]['triangles']==count
assert model['originalSketchfabDownloadVerified'] is False and model['sourceContainsOriginalUvs'] is False
print('Verified 1 CC-BY-4.0 helicopter derivative; 40604/10771/5998 native triangles including 3 hidden pivot markers, nine original PBR maps, license and source hashes. Native Unity validation pending.')
