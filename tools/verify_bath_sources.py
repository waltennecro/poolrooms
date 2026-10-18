"""Validate pinned model/material sources, provenance and native thermal resource data."""
from pathlib import Path
import hashlib,json,math,re,struct
from PIL import Image
from generate_asset_credits import build
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Baths';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/BathSources'
lock=json.loads((SOURCE/'sources.lock.json').read_text());catalog=json.loads((DEST/'catalog.json').read_text())
assert catalog['distinctNewModels']==1 and catalog['licensedMaterialSets']==1 and not catalog['lodsCountAsModels'] and not catalog['nativeUnityValidated']
for name,r in lock.items():
    data=(SOURCE/name).read_bytes();assert len(data)==r['bytes'] and hashlib.sha256(data).hexdigest()==r['sha256']
for name,r in json.loads((DEST/'files.sha256.json').read_text()).items():
    data=(DEST/name).read_bytes();assert len(data)==r['bytes'] and hashlib.sha256(data).hexdigest()==r['sha256']
model=catalog['models'][0];assert model==json.loads((DEST/'T_ArtNouveauLamp.json').read_text())
assert model['sourceSha256']==lock['LightsPunctualLamp.glb']['sha256'] and model['authors']==['DGG'] and model['modelLicense']=='CC-BY-4.0'
raw=(SOURCE/'Lamp_static.glb').read_bytes();assert hashlib.sha256(raw).hexdigest()==model['preparedSourceSha256'];doc=json.loads(raw[20:20+struct.unpack_from('<I',raw,12)[0]])
assert len(doc['nodes'])==3 and len(doc['meshes'])==3 and not doc.get('animations') and not doc.get('extensions') and all('mesh' in x for x in doc['nodes'])
assert all(m.get('emissiveFactor')==[0,0,0] for m in doc['materials'])
for name,expected in [('LightsPunctualLamp','CC-BY-4.0'),('ClearcoatWicker','CC0-1.0')]:
    meta=json.loads((DEST/'Licenses'/(name+'_metadata.json')).read_text());assert {x['spdx'] for x in meta['legal']}=={expected}
    assert len((DEST/'Licenses'/(expected+'.txt')).read_bytes())>5000
assert model['lods'][1]['triangles']<model['lods'][0]['triangles']<model['triangles']==6000
for level in [model]+model['lods']:
    points=[];normals=[];uv=0;triangles=0
    for line in (ROOT/'Assets/PoolroomsProcedural/Resources'/(level['resource']+'.obj')).read_text().splitlines():
        f=line.split()
        if not f:continue
        if f[0]=='v':points.append(list(map(float,f[1:])));assert all(math.isfinite(x) for x in points[-1])
        elif f[0]=='vn':normals.append(list(map(float,f[1:])));assert .99<math.sqrt(sum(v*v for v in normals[-1]))<1.01
        elif f[0]=='vt':uv+=1;assert all(math.isfinite(float(v)) for v in f[1:])
        elif f[0]=='f':
            triangles+=1;assert len(f)==4
            for token in f[1:]:
                a,b,c=map(int,token.split('/'));assert 0<a<=len(points) and 0<b<=uv and 0<c<=len(normals)
    assert triangles==level['triangles']
wicker=catalog['wicker'];assert wicker['license']=='CC0-1.0' and wicker['authors']==['Eric Chadwick'] and wicker['sourceSha256']==lock['ClearcoatWicker.glb']['sha256']
assert len(wicker['maps'])==3 and not wicker['clearcoatNormalReproduced']
for name,r in wicker['maps'].items():
    data=(ROOT/name).read_bytes();assert len(data)==r['bytes'] and hashlib.sha256(data).hexdigest()==r['sha256'];im=Image.open(ROOT/name);assert max(im.size)<=1024;im.verify()
for p in (DEST/'Textures').glob('*.png'):
    im=Image.open(p);assert max(im.size)<=2048;im.verify()
size=[(b-a)*.55/max(y-x for x,y in zip(model['minimum'],model['maximum'])) for a,b in zip(model['minimum'],model['maximum'])]
code=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text();match=re.search(r'case 10:return new V3\(([^)]+)\)',code);assert match and max(abs(a-float(b.strip().rstrip('f'))) for a,b in zip(size,match.group(1).split(',')))<.00001
assert (ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsAssetCredits.txt').read_text()==build()
print(json.dumps({'newAuthoredModels':1,'licensedMaterialSets':1,'lampTriangles':[6000]+[x['triangles'] for x in model['lods']],'sourcePbrImages':len(list((DEST/'Textures').glob('*.png'))),'wickerMaps':3,'hashesLicencesAndDimensions':'passed','nativeUnityExecuted':False}))
