"""Offline provenance, licence, native model/LOD bounds and PBR channel audit."""
from pathlib import Path
import hashlib,json,math,re
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'Assets/PoolroomsProcedural/Resources'
core=(ROOT/'Assets/PoolroomsProcedural/Core/SourceFurniture.cs').read_text()
maps=0;source_bytes=0;triangles=0
for folder in ['Mall','MallFinishes']:
    source=ROOT/'Sources'/folder;lock=json.loads((source/'sources.lock.json').read_text())
    if folder=='Mall':lock=lock['files']
    for name,record in lock.items():
        path=source/name;raw=path.read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],path
        assert record['url'].startswith('https://raw.githubusercontent.com/'),path;source_bytes+=len(raw)
bistro_lock=json.loads((ROOT/'Sources/MallBistro/sources.lock.json').read_text())
for name,record in bistro_lock.items():
    if not name.lower().endswith('.dds'):continue
    local=ROOT/'Sources/MallBistro/OriginalMaps'/name.split('/2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e/',1)[1]
    raw=local.read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256']
for folder in ['MallSources','MallFinishes']:
    dest=RES/folder;manifest=json.loads((dest/'files.sha256.json').read_text())
    assert set(manifest)=={p.relative_to(dest).as_posix() for p in dest.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
    for name,record in manifest.items():
        path=dest/name;raw=path.read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],path
        assert len(raw)<100*1024*1024,'GitHub per-file bound'
        if name.endswith('.png'):
            with Image.open(path) as im:assert max(im.size)<=4096;im.verify()
            maps+=1
    assert (dest/'Licenses/NOTICE.txt').is_file()
models=json.loads((RES/'MallSources/catalog.json').read_text())['models'];assert len(models)==14
lengths=[2.1,.75,.30,.65,.65,.09,.16,.32,1,1.3,.45,.95,.28,1.25]
for i,model in enumerate(models):
    if i<8:
        meta=json.loads((RES/'MallSources/Licenses'/(model['sourceName']+'_metadata.json')).read_text())
        assert all(x['spdx']==model['modelLicense'] and x['artist'] in model['authors'] for x in meta['legal'])
    else:
        assert model['upstreamRevision']=='2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e' and model['authors']==['Amazon.com, Inc. or its affiliates']
        original=ROOT/'Sources/MallBistro'/(model['sourceName']+'_adapted.glb')
        assert hashlib.sha256(original.read_bytes()).hexdigest()==model['sourceSha256']
        assert (RES/'MallSources/Licenses/Bistro_Bistro_CC_BY_4.txt').is_file()
    assert model['modelLicense'] in ['CC0-1.0','CC-BY-4.0'] and not model['nativeUnityValidated']
    assert len(model['lods'])==2 and all(0<lod['triangles']<model['triangles'] for lod in model['lods'])
    size=[b-a for a,b in zip(model['minimum'],model['maximum'])];expected=[x/max(size)*lengths[i] for x in size]
    code=re.search(r'case '+str(14+i)+r':return new V3\(([^)]+)\)',core).group(1);actual=[float(x.rstrip('f')) for x in code.split(',')]
    assert max(abs(a-b) for a,b in zip(expected,actual))<1e-7,(model['id'],expected,actual)
    for resource,count in [(model['resource'],model['triangles'])]+[(l['resource'],l['triangles']) for l in model['lods']]:
        faces=0;points=[];normals=0;uv=0
        with (RES/(resource+'.obj')).open() as f:
            for line in f:
                s=line.split()
                if not s:continue
                if s[0]=='v':
                    v=list(map(float,s[1:]));assert all(map(math.isfinite,v));points.append(v)
                elif s[0]=='vn':
                    v=list(map(float,s[1:]));assert abs(sum(x*x for x in v)-1)<.0005;normals+=1
                elif s[0]=='vt':
                    v=list(map(float,s[1:]));assert all(map(math.isfinite,v));uv+=1
                elif s[0]=='f':
                    assert len(s)==4;faces+=1
                    for corner in s[1:]:
                        a,b,c=map(int,corner.split('/'));assert 0<a<=len(points) and 0<b<=uv and 0<c<=normals
        assert faces==count and points
        if resource==model['resource']:
            for axis in range(3):assert abs(min(v[axis] for v in points)-model['minimum'][axis])<1e-5 and abs(max(v[axis] for v in points)-model['maximum'][axis])<1e-5
        triangles+=faces
    for material in model['materials']:
        for key in ['albedo','normal','mask','specular']:
            if material.get(key):assert (RES/(material[key]+'.png')).is_file()
palette=json.loads((RES/'MallFinishes/palette.json').read_text())['materials'];assert len(palette)==20
for finish in palette:
    assert finish['workflow']=='specular' and finish['emission']==[0,0,0]
    for key in ['albedo','mask','specular']:assert (RES/(finish[key]+'.png')).is_file()
    if finish.get('normal'):assert (RES/(finish['normal']+'.png')).is_file()
    with Image.open(RES/(finish['specular']+'.png')) as spec,Image.open(RES/(finish['mask']+'.png')) as mask:
        spec=np.asarray(spec);mask=np.asarray(mask);assert spec.shape==mask.shape
        assert np.array_equal(spec[:,:,3],mask[:,:,3]) and np.all(mask[:,:,0]==0),'Source gloss was lost or metalness invented'
references=[]
for name in ['models','finishes','bistro-previews']:
    references+=json.loads((ROOT/'Documentation/References/Mall'/(name+'.json')).read_text())
assert len(references)==34
for entry in references:
    assert entry['license'] in ['CC0-1.0','CC-BY-4.0'];Image.open(ROOT/'Documentation/References/Mall'/entry['file']).verify()
plans=json.loads((ROOT/'Documentation/References/Mall/layouts.json').read_text());assert len(plans)==6
for plan in plans:Image.open(ROOT/'Documentation/References/Mall'/('Plan_'+str(plan['seed'])+'.png')).verify()
credits=(RES/'PoolroomsAssetCredits.txt').read_text();assert 'MallSources' in credits and 'MallFinishes' in credits
print(json.dumps({'verified':True,'distinctNewModels':14,'lodDerivatives':28,'finishes':20,'maps':maps,'sourceBytes':source_bytes,'nativeAndLodTriangles':triangles,'sourceReferenceImages':34,'corePlanImages':6,'unityValidated':False}))
