"""Audit the actual licensed source, static adaptation, alpha, native meshes and LODs."""
from pathlib import Path
import hashlib,json,math,re,struct
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Garden';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/GardenSources'
lock=json.loads((SOURCE/'sources.lock.json').read_text());catalog=json.loads((DEST/'catalog.json').read_text())
assert catalog['distinctSourceModels']==1 and not catalog['lodsCountAsModels']
for name,record in lock.items():
    data=(SOURCE/name).read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256']
for name,record in json.loads((DEST/'files.sha256.json').read_text()).items():
    data=(DEST/name).read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256']
def glb_doc(data):return json.loads(data[20:20+struct.unpack_from('<I',data,12)[0]])
r=catalog['models'][0];assert r==json.loads((DEST/'G_PottedPlant.json').read_text());assert r['authors']==['Eric Chadwick','Rico Cilliers']
assert r['sourceSha256']==lock['DiffuseTransmissionPlant.glb']['sha256']
assert hashlib.sha256((SOURCE/'PottedPlant_static.glb').read_bytes()).hexdigest()==r['preparedSourceSha256']
original=glb_doc((SOURCE/'DiffuseTransmissionPlant.glb').read_bytes());adapted=glb_doc((SOURCE/'PottedPlant_static.glb').read_bytes())
assert [n['name'] for n in adapted['nodes']]==['pot','leaves','dirt'] and len(adapted['meshes'])==3 and not adapted.get('animations')
assert not adapted.get('extensions') and not adapted['materials'][0].get('emissiveTexture')
assert sum(original['accessors'][p['indices']]['count']//3 for m in original['meshes'][:3] for p in m['primitives'])==r['originalPlantTriangles']==68273
assert r['triangles']<r['originalPlantTriangles'] and r['soilReducedOnly'] and not r['diffuseTransmissionReproduced']
assert {x['spdx'] for x in json.loads((DEST/'Licenses/DiffuseTransmissionPlant_metadata.json').read_text())['legal']}=={'CC0-1.0','CC-BY-4.0'}
for license in ['CC0-1.0.txt','CC-BY-4.0.txt']:assert len((DEST/'Licenses'/license).read_bytes())>5000
assert 'Rico Cilliers' in (DEST/'Licenses/NOTICE.txt').read_text() and 'Eric Chadwick' in (DEST/'Licenses/NOTICE.txt').read_text()
assert r['materials'][1]['alphaClip'] and r['materials'][1]['alphaCutoff']==.5 and r['materials'][1]['doubleSided'] and not r['materials'][1]['transparent']
img=Image.open(DEST/'Textures/G_PottedPlant_m1_albedo.png').convert('RGBA');assert img.getchannel('A').getextrema()==(0,255)
images=list((DEST/'Textures').glob('*.png'))
for path in images:
    im=Image.open(path);im.verify();assert max(im.size)<=2048
assert r['lods'][1]['triangles']<r['lods'][0]['triangles']<r['triangles']
for level in [r]+r['lods']:
    points=[];normals=[];uv=0;count=0
    for line in (ROOT/'Assets/PoolroomsProcedural/Resources'/(level['resource']+'.obj')).read_text().splitlines():
        f=line.split()
        if not f:continue
        if f[0]=='o':assert any('_'+x+'_' in f[1] for x in ['pot','leaves','dirt'])
        elif f[0]=='v':points.append(tuple(map(float,f[1:])));assert all(math.isfinite(x) for x in points[-1])
        elif f[0]=='vn':normals.append(tuple(map(float,f[1:])));assert .99<math.sqrt(sum(x*x for x in normals[-1]))<1.01
        elif f[0]=='vt':uv+=1;assert all(math.isfinite(float(x)) for x in f[1:])
        elif f[0]=='f':
            assert len(f)==4;count+=1
            for value in f[1:]:
                a,b,c=map(int,value.split('/'));assert 0<a<=len(points) and 0<b<=uv and 0<c<=len(normals)
    assert count==level['triangles']
size=[(b-a)*1.15/max(y-x for x,y in zip(r['minimum'],r['maximum'])) for a,b in zip(r['minimum'],r['maximum'])]
code=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text();match=re.search(r'case 9:return new V3\(([^)]+)\)',code)
assert match and max(abs(a-float(b.strip().rstrip('f'))) for a,b in zip(size,match.group(1).split(',')))<.00001
from generate_asset_credits import build
assert (ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsAssetCredits.txt').read_text()==build()
print(json.dumps({'distinctNewModels':1,'triangles':[r['triangles']]+[l['triangles'] for l in r['lods']],'licensedMaps':len(images),'sourceAndDerivativeHashes':'passed','insectsLightsAndAnimationImported':False,'diffuseTransmissionReproduced':False,'nativeUnityExecuted':False}))
