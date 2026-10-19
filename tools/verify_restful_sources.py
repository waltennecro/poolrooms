"""Audit three new model families, license provenance, LOD geometry and all hashes."""
from pathlib import Path
import hashlib,json,math
ROOT=Path(__file__).resolve().parents[1];DEST=ROOT/'Assets/PoolroomsProcedural/Resources/RestfulSources'
manifest=json.loads((DEST/'files.sha256.json').read_text())
files={p.relative_to(DEST).as_posix() for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
assert files==set(manifest),'Missing or unexpected source resource'
for name,record in manifest.items():
 data=(DEST/name).read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
lock=json.loads((ROOT/'Sources/Restful/sources.lock.json').read_text())
for name,record in lock.items():
 p=ROOT/'Sources/Restful'/name
 assert p.exists() and hashlib.sha256(p.read_bytes()).hexdigest()==record['sha256'] and p.stat().st_size==record['bytes'],name
 assert '/edc7c9e67c639d230715049ee31f9a96a6babbbe/' in record['url'],'Unpinned source URL'
catalog=json.loads((DEST/'catalog.json').read_text());assert catalog['distinctSourceModels']==3 and not catalog['lodsCountAsModels']
expected={'R_Teacup':('DiffuseTransmissionTeacup','CC0-1.0'),'R_FlowerVase':('GlassVaseFlowers','CC0-1.0'),'R_BarnLamp':('AnisotropyBarnLamp','CC-BY-4.0')}
triangles=0
for model in catalog['models']:
 original,license=expected[model['id']];meta=json.loads((DEST/'Licenses'/(original+'_metadata.json')).read_text())
 assert model['modelLicense']==license and {e['spdx'] for e in meta['legal']}=={license}
 assert model['sourceSha256']==lock[original+'.glb']['sha256'] and model['authors'] and model['sourceUrl']==lock[original+'.glb']['url']
 assert len(model['lods'])==2 and model['lods'][1]['triangles']<model['lods'][0]['triangles']<model['triangles']
 assert not model['nativeUnityValidated']
 for variant in [model]+model['lods']:
  path=ROOT/'Assets/PoolroomsProcedural/Resources'/(variant['resource']+'.obj');verts=uvs=normals=faces=0
  for line in path.read_text().splitlines():
   bits=line.split()
   if not bits:continue
   if bits[0] in ['v','vt','vn']:
    assert all(math.isfinite(float(x)) for x in bits[1:])
    if bits[0]=='v':verts+=1
    elif bits[0]=='vt':uvs+=1
    else:
     normals+=1;length=sum(float(x)**2 for x in bits[1:]);assert .98<length<1.02
   elif bits[0]=='f':
    assert len(bits)==4
    for point in bits[1:]:
     v,t,n=map(int,point.split('/'));assert 1<=v<=verts and 1<=t<=uvs and 1<=n<=normals
    faces+=1
  assert faces==variant['triangles'];triangles+=faces
print(f'PASS: 3 distinct newly downloaded source models, 6 LOD derivatives, {len(manifest)} hashed resources, {triangles} total LOD/source triangles; CC0 and CC BY 4.0 provenance, original downloads, OBJ bounds references and normals verified. Native Unity unrun.')
