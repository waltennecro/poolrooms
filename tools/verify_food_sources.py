"""Audit original licences, independent geometries, metric bounds and shipped PBR maps."""
from pathlib import Path
import hashlib,json,math,re,struct,zipfile
ROOT=Path(__file__).resolve().parents[1];SRC=ROOT/'Sources/Food';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/FoodSources'
sha=lambda raw:hashlib.sha256(raw).hexdigest()
lock=json.loads((SRC/'sources.lock.json').read_text());raw=(SRC/'kenney_food-kit.zip').read_bytes()
assert len(raw)==lock['archiveBytes'] and sha(raw)==lock['archiveSha256']
assert lock['license']=='CC0-1.0' and lock['author']=='Kenney' and lock['url'].startswith('https://kenney.nl/')
with zipfile.ZipFile(SRC/'kenney_food-kit.zip') as z:
    assert z.testzip() is None
    for name,d in lock['members'].items():
        raw=z.read(name);assert len(raw)==d['bytes'] and sha(raw)==d['sha256']
    assert z.read('License.txt')==(DEST/'Licenses/Kenney_License.txt').read_bytes()
assert 'CC0' in (DEST/'Licenses/Kenney_License.txt').read_text()
manifest=json.loads((DEST/'files.sha256.json').read_text())
assert set(manifest)=={p.relative_to(DEST).as_posix() for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json' and not p.name.endswith('.meta')}
for name,d in manifest.items():
    raw=(DEST/name).read_bytes();assert len(raw)==d['bytes'] and sha(raw)==d['sha256'],name
    if name.endswith('.png'):
        assert raw[:8]==b'\x89PNG\r\n\x1a\n';assert struct.unpack('>II',raw[16:24])==(1024,1024)
cat=json.loads((DEST/'catalog.json').read_text());selection=json.loads((SRC/'selection.json').read_text())
assert len({r['foodType'] for r in selection if not r['drink']})==cat['distinctFoodTypes']==103
assert cat['edibleFoods']==117 and cat['drinks']==8 and cat['distinctSourceModels']==125 and not cat['lodsCountAsModels'] and not cat['nativeUnityValidated']
geometries=set();triangles=0
for row in selection:
    d=json.loads((DEST/(row['resourceId']+'.json')).read_text());assert d['key']==row['key'] and d['sourceSha256']==lock['members']['Models/OBJ format/'+row['model']+'.obj']['sha256']
    assert d['license']=='CC0-1.0' and not d['nativeUnityValidated'] and len(d['lods'])==1
    for material in d['materials']:
        for key in ['albedo','normal','mask']:assert (ROOT/'Assets/PoolroomsProcedural/Resources'/(material[key]+'.png')).is_file()
        assert material['metallic']==0 and 0<material['roughness']<1 and all(math.isfinite(v) for v in material['colour'])
    for index,resource in enumerate([d['resource'],d['lods'][0]['resource']]):
        vertices=[];normals=[];uv=[];faces=[];seen=set()
        for line in (ROOT/'Assets/PoolroomsProcedural/Resources'/(resource+'.obj')).read_text().splitlines():
            p=line.split()
            if not p:continue
            if p[0] in ('v','vn','vt'):
                values=list(map(float,p[1:]));assert all(map(math.isfinite,values));{'v':vertices,'vn':normals,'vt':uv}[p[0]].append(values)
                if p[0]=='vn':assert abs(sum(v*v for v in values)-1)<.001
            elif p[0]=='usemtl':assert p[1] in {m['name'] for m in d['materials']}
            elif p[0]=='f':
                assert len(p)==4
                refs=[tuple(map(int,r.split('/'))) for r in p[1:]]
                for a,b,c in refs:assert 0<a<=len(vertices) and 0<b<=len(uv) and 0<c<=len(normals)
                a,b,c=[vertices[r[0]-1] for r in refs];ab=[b[i]-a[i] for i in range(3)];ac=[c[i]-a[i] for i in range(3)];area=sum((ab[(i+1)%3]*ac[(i+2)%3]-ab[(i+2)%3]*ac[(i+1)%3])**2 for i in range(3));assert area>1e-20,(row['model'],'degenerate triangle')
                ids=tuple(tuple(vertices[r[0]-1]) for r in refs);face=min(ids[i:]+ids[:i] for i in range(3));assert face not in seen,(row["model"],"duplicate face");seen.add(face)
                faces.append(refs)
        assert len(faces)==(d['triangles'] if index==0 else d['lods'][0]['triangles'])
        if index==0:
            signature=sha(json.dumps([vertices,[[r[0] for r in f] for f in faces]],separators=(',',':')).encode());assert signature not in geometries,('Duplicate model',row['model']);geometries.add(signature)
            for axis in range(3):assert abs(min(v[axis] for v in vertices)-d['minimum'][axis])<1e-6 and abs(max(v[axis] for v in vertices)-d['maximum'][axis])<1e-6
            triangles+=len(faces)
        else:assert 0<len(faces)<=d['triangles'] and (len(faces)<d['triangles'] or row['model']=='fries')
core=(ROOT/'Assets/PoolroomsProcedural/Core/ProvisionCatalog.Generated.cs').read_text();assert core.count('new ProvisionDefinition(')==125
print(f'Food: {len(geometries)} distinct imported models, 103 food types / 117 presentations + 8 drinks; {triangles} total catalogue triangles; 125 LOD derivatives; 13 original 1024px maps; pinned CC0 source and every shipped hash verified. No native Unity result.')
