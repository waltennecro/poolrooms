"""Verify shipped CC0 source attribution, converted hashes, geometry and LOD counts."""
from pathlib import Path
import hashlib,json,math

root=Path(__file__).resolve().parents[1]
assets=root/'Assets/PoolroomsProcedural/Resources/City11'
catalog=json.loads((assets/'catalog.json').read_text())
assert catalog['distinctSourceModels']==4 and not catalog['colourVariantsCountAsModels']
expected=json.loads((assets/'files.sha256.json').read_text())
actual={p.relative_to(assets).as_posix() for p in assets.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
assert actual==set(expected),'City source manifest mismatch'
for name,record in expected.items():
    data=(assets/name).read_bytes()
    assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
for model in catalog['models']:
    source=root/'Sources/City11'/(model['id']+'.glb')
    if source.exists():assert hashlib.sha256(source.read_bytes()).hexdigest()==model['sourceSha256']
    meta=json.loads((assets/'Licenses'/(model['id']+'_metadata.json')).read_text())
    assert meta['legal'] and all(item.get('spdx')=='CC0-1.0' for item in meta['legal'])
    assert model['modelLicense']=='CC0-1.0' and model['metadataLicense']=='CC-BY-4.0'
    for variant in [model]+model['lods']:
        path=root/'Assets/PoolroomsProcedural/Resources'/(variant['resource']+'.obj')
        vertices=normals=uvs=faces=0
        for line in path.read_text().splitlines():
            parts=line.split()
            if not parts:continue
            if parts[0] in ('v','vn','vt'):
                assert all(math.isfinite(float(v)) for v in parts[1:]),path
                if parts[0]=='v':vertices+=1
                elif parts[0]=='vn':normals+=1
                else:uvs+=1
            if parts[0]=='f':
                assert len(parts)==4
                for value in parts[1:]:
                    a,b,c=map(int,value.split('/'));assert 1<=a<=vertices and 1<=b<=uvs and 1<=c<=normals
                faces+=1
        assert faces==variant['triangles'],path
    if model['id']=='ToyCar':
        assert model['presentationClothRemoved'] and model['lods'][1]['triangles']<model['lods'][0]['triangles']<model['triangles']
        minimum,maximum=model['minimum'],model['maximum']
        assert maximum[2]-minimum[2]>1.8*(maximum[0]-minimum[0]),'Vehicle source is not aligned to its long axis'
print('Verified 4 distinct CC0 sources; 3 props + 1 car; car LODs are derivatives of that same source. Native Unity validation pending.')
