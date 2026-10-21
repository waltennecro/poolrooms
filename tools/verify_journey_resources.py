"""Offline runtime resource, provenance, metric/LOD and PBR gloss audit.

Input originals can be re-downloaded by the import tools from pinned URLs. This
check deliberately does not claim to authenticate unavailable original bytes;
it validates every shipped file and binding without bundling unused originals.
"""
from pathlib import Path
import hashlib,json,math,re
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
RES=ROOT/'Assets/PoolroomsProcedural/Resources'
core=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text()
maps=0;total=0;used=set()
for name in ['JourneySources','JourneyFinishes']:
    root=RES/name;manifest=json.loads((root/'files.sha256.json').read_text())
    assert set(manifest)=={p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
    for path,record in manifest.items():
        file=root/path;data=file.read_bytes()
        assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],path
        assert len(data)<100*1024*1024,'Individual GitHub file limit'
        total+=len(data)
        if path.endswith('.png'):
            with Image.open(file) as im:assert max(im.size)<=4096;im.verify()
            maps+=1
    assert 'Amazon.com' in (root/'Licenses/NOTICE.txt').read_text()
    assert 'Attribution 4.0' in (root/'Licenses/Bistro_CC_BY_4.txt').read_text()
    lock=json.loads((root/'sources.lock.json').read_text());assert lock
    for record in lock.values():
        assert record['url'].startswith('https://raw.githubusercontent.com/zeux/niagara_bistro/2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e/')
        assert record['bytes']>0 and len(record['sha256'])==64
def audit_material(m):
    assert m['workflow']=='specular'
    for key in ['albedo','normal','mask','specular']:
        if m.get(key):
            p=RES/(m[key]+'.png');assert p.is_file();used.add(p.resolve())
    if m.get('specular'):
        with Image.open(RES/(m['specular']+'.png')) as spec,Image.open(RES/(m['mask']+'.png')) as mask:
            spec=np.asarray(spec);mask=np.asarray(mask)
            assert spec.shape==mask.shape and np.array_equal(spec[:,:,3],mask[:,:,3]),'Authored gloss lost'
models=json.loads((RES/'JourneySources/catalog.json').read_text())['models'];assert len(models)==8
fingerprints=set();lods=0
for i,model in enumerate(models):
    assert model['modelLicense']=='CC-BY-4.0' and not model['nativeUnityValidated']
    assert model['upstreamRevision']=='2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e'
    assert len(model['lods'])==2 and all(0<v['triangles']<model['triangles'] for v in model['lods']);lods+=2
    size=[b-a for a,b in zip(model['minimum'],model['maximum'])];expected=[v/max(size)*model['gameplayLongest'] for v in size]
    code=re.search(r'case '+str(28+i)+r':return new V3\(([^)]+)\)',core).group(1);actual=[float(v.rstrip('f')) for v in code.split(',')]
    assert max(abs(a-b) for a,b in zip(actual,expected))<1e-7
    for resource,count in [(model['resource'],model['triangles'])]+[(v['resource'],v['triangles']) for v in model['lods']]:
        p=RES/(resource+'.obj');faces=0;positions=[];uv=0;normals=0;digest=hashlib.sha256()
        for line in p.read_text().splitlines():
            s=line.split()
            if not s:continue
            if s[0]=='v':
                v=list(map(float,s[1:]));assert all(map(math.isfinite,v));positions.append(v);digest.update(line.encode())
            elif s[0]=='vt':uv+=1;assert all(map(math.isfinite,map(float,s[1:])))
            elif s[0]=='vn':normals+=1;assert abs(sum(float(v)**2 for v in s[1:])-1)<.0005
            elif s[0]=='f':
                assert len(s)==4;faces+=1;digest.update(line.encode())
                for corner in s[1:]:
                    a,b,c=map(int,corner.split('/'));assert 0<a<=len(positions) and 0<b<=uv and 0<c<=normals
        assert faces==count
        if resource==model['resource']:
            assert digest.hexdigest() not in fingerprints,'Repeated source geometry advertised as a new model'
            fingerprints.add(digest.hexdigest())
            for axis in range(3):assert abs(min(v[axis] for v in positions)-model['minimum'][axis])<1e-5 and abs(max(v[axis] for v in positions)-model['maximum'][axis])<1e-5
    for material in model['materials']:audit_material(material)
materials=json.loads((RES/'JourneyFinishes/palette.json').read_text())['materials'];assert len(materials)==43
assert len({m['sourceMaterial'] for m in materials})==43
for m in materials:audit_material(m)
for name in ['JourneySources','JourneyFinishes']:
    shipped={p.resolve() for p in (RES/name/'Textures').glob('*.png')}
    assert shipped<=used,'Unused maps would inflate Assets'
credits=(RES/'PoolroomsAssetCredits.txt').read_text();assert 'JourneySources' in credits and 'JourneyFinishes' in credits
assets_bytes=sum(p.stat().st_size for p in (ROOT/'Assets').rglob('*') if p.is_file())
assert assets_bytes<=6000000000,'Assets budget exceeded'
print(json.dumps({'verified':True,'newSourceModels':len(models),'lodDerivatives':lods,'surfaceDefinitions':len(materials),'shippedMaps':maps,
    'newRuntimeResourceBytes':total,'allAssetsBytes':assets_bytes,'budgetBytes':6000000000,'paddingBytes':0,'nativeUnityValidated':False}))
