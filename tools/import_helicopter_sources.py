"""Adapt a pinned CC-BY-4.0 helicopter geometry dataset, never execute its JS.

Warbird's data-only derivative preserves separated moving rotors. Its unlit game
code/materials are not used. Original derivative, credit and license are retained.
Our normals, UVs, PBR finishes and LODs are generated here with NumPy/Pillow.
"""
from pathlib import Path
import json,hashlib,urllib.request
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/Helicopter';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Helicopters'
REV='251d6d85cbe5eedbebf7625a8df7fd2430f8d1a1'
BASE=f'https://raw.githubusercontent.com/jdubba1/warbird/{REV}/'
LOCK=SOURCE/'sources.lock.json'
expected=json.loads(LOCK.read_text()) if LOCK.exists() else {}

def acquire(name,url):
    p=SOURCE/name
    if not p.exists():p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(urllib.request.urlopen(url,timeout=40).read(8*1024*1024+1))
    raw=p.read_bytes();assert len(raw)<=8*1024*1024
    record={'url':url,'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    if name in expected:assert expected[name]==record,'Pinned helicopter source changed'
    expected[name]=record;return raw

def parse_data(raw):
    s=raw.decode();prefix='export default ';start=s.index(prefix)+len(prefix)
    value,end=json.JSONDecoder().raw_decode(s[start:]);assert s[start+end:].strip()==';','Unexpected executable payload'
    assert set(value)=={'pivots','meshes'} and set(value['pivots'])=={'rotor','tail'}
    for m in value['meshes']:
        assert set(m)=={'parent','color','positions','indices'} and m['parent'] in ('body','rotor','tail')
        assert m['color'] in ('body','ink','accent','glass')
    return value

def smooth(positions,faces):
    edges=np.cross(positions[faces[:,1]]-positions[faces[:,0]],positions[faces[:,2]]-positions[faces[:,0]])
    area=np.linalg.norm(edges,axis=1);keep=area>1e-12;faces=faces[keep];edges=edges[keep];area=area[keep]
    normal=edges/area[:,None];adj=[[] for _ in positions]
    for face,vertices in enumerate(faces):
        for v in vertices:adj[v].append(face)
    p=[];n=[];uv=[];out=[];cache={}
    for i,face in enumerate(faces):
        # Projection in metres, with identical periodic physical finishes on each side.
        axis=int(np.argmax(np.abs(normal[i])));pair=((2,1),(0,2),(0,1))[axis]
        triangle=[]
        for v in face:
            neighbours=[j for j in adj[v] if normal[i]@normal[j]>=.70710678]
            weighted=edges[neighbours].sum(axis=0);weighted/=np.linalg.norm(weighted)
            key=(int(v),axis,*np.round(weighted,5))
            if key not in cache:
                cache[key]=len(p);p.append(positions[v]);n.append(weighted);uv.append(positions[v,list(pair)]*2)
            triangle.append(cache[key])
        out.append(triangle)
    return np.array(p),np.array(n),np.array(uv),np.array(out)

def lower(group,grid):
    name,material,p,n,uv,faces=group
    epsilon=max(np.ptp(p,axis=0).max()/grid,.003)
    projected=np.stack((p[:,[2,1]],p[:,[0,2]],p[:,[0,1]]),axis=1)*2
    axis=np.argmin(np.sum((projected-uv[:,None,:])**2,axis=2),axis=1)
    key=np.column_stack((np.rint(p/epsilon),np.rint(n*2),axis)).astype(np.int64)
    _,inverse=np.unique(key,axis=0,return_inverse=True);count=int(inverse.max())+1;weight=np.bincount(inverse)
    def mean(a):return np.stack([np.bincount(inverse,weights=a[:,i],minlength=count)/weight for i in range(a.shape[1])],axis=1)
    pp=mean(p);nn=mean(n);nn/=np.maximum(np.linalg.norm(nn,axis=1,keepdims=True),1e-12);tt=mean(uv)
    ff=inverse[faces];ff=ff[(ff[:,0]!=ff[:,1])&(ff[:,1]!=ff[:,2])&(ff[:,0]!=ff[:,2])]
    ff=ff[np.linalg.norm(np.cross(pp[ff[:,1]]-pp[ff[:,0]],pp[ff[:,2]]-pp[ff[:,0]]),axis=1)>1e-12]
    return name,material,pp,nn,tt,ff

def write_model(name,groups):
    p=DEST/'Models'/f'{name}.obj';p.parent.mkdir(parents=True,exist_ok=True)
    with p.open('w') as f:
        f.write('# MH-6 by AnirudhRao, CC-BY-4.0; adapted via Warbird. See Helicopters/Licenses.\nmtllib H_MH6.mtl\n');start=1
        for part,material,positions,normals,uv,faces in groups:
            f.write(f'o H_MH6_{part}\nusemtl H_MH6_{material}\n')
            for v in positions:f.write('v '+' '.join(f'{x:.8g}' for x in v)+'\n')
            for v in uv:f.write('vt '+' '.join(f'{x:.8g}' for x in v)+'\n')
            for v in normals:f.write('vn '+' '.join(f'{x:.8g}' for x in v)+'\n')
            for face in faces:f.write('f '+' '.join(f'{x+start}/{x+start}/{x+start}' for x in face)+'\n')
            start+=len(positions)
    return sum(len(g[-1]) for g in groups)

def finishes():
    size=1024;rng=np.random.default_rng(180091);y,x=np.mgrid[:size,:size];grain=rng.normal(0,1,(size,size))
    grain=(grain+np.roll(grain,1,0)+np.roll(grain,-1,1))/3;result={}
    for kind,base,metal,smoothness,relief in [('paint',(.39,.43,.46),.05,.57,.012),('metal',(.17,.18,.19),.85,.46,.018),('rubber',(.024,.027,.029),0,.16,.018)]:
        variation=grain*.008;albedo=np.clip(np.array(base)[None,None,:]+variation[:,:,None],0,1)
        nx=(np.roll(grain,-1,1)-np.roll(grain,1,1))*relief;ny=(np.roll(grain,-1,0)-np.roll(grain,1,0))*relief
        normals=np.stack((-nx,ny,np.ones_like(nx)),axis=-1);normals/=np.linalg.norm(normals,axis=2,keepdims=True)
        mask=np.ones((size,size,4));mask[:,:,0]=metal;mask[:,:,1]=.98;mask[:,:,3]=np.clip(smoothness+variation,0,1)
        result[kind]={}
        for slot,array in [('albedo',albedo),('normal',normals*.5+.5),('mask',mask)]:
            p=DEST/'Textures'/f'H_MH6_{kind}_{slot}.png';p.parent.mkdir(parents=True,exist_ok=True);Image.fromarray(np.uint8(np.clip(array,0,1)*255+.5)).save(p)
            result[kind][slot]='Helicopters/Textures/'+p.stem
    return result

def main():
    raw=acquire('little-bird.js',BASE+'dist/assets/little-bird.js')
    credits=acquire('aircraft-license.txt',BASE+'dist/assets/aircraft-license.txt')
    assert b'AnirudhRao' in credits and b'Creative Commons Attribution 4.0' in credits
    license_text=acquire('CC-BY-4.0.txt','https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/edc7c9e67c639d230715049ee31f9a96a6babbbe/LICENSES/CC-BY-4.0.txt')
    acquire('CREDITS.md',BASE+'CREDITS.md')
    value=parse_data(raw);groups=[];source_triangles=sum(len(m['indices'])//3 for m in value['meshes'])
    for i,m in enumerate(value['meshes']):
        positions=np.array(m['positions'],float).reshape(-1,3);faces=np.array(m['indices'],int).reshape(-1,3)
        assert np.all(np.isfinite(positions)) and faces.min()>=0 and faces.max()<len(positions)
        if m['parent'] in value['pivots']:positions+=value['pivots'][m['parent']]
        p,n,uv,f=smooth(positions,faces);part={'rotor':'main_rotor','tail':'tail_rotor'}.get(m['parent'],'body_'+str(i));groups.append((part,m['color'],p,n,uv,f))
    textures=finishes();materials=[]
    for colour in ['ink','body','accent','glass']:
        finish='rubber' if colour=='ink' else 'metal' if colour=='accent' else 'paint';d={'name':'H_MH6_'+colour,'colour':[1,1,1,1],**textures[finish],'normalScale':1,'transparent':colour=='glass','doubleSided':colour=='glass','extensions':[]}
        if colour=='glass':d.update(colour=[.23,.3,.34,.24],albedo=None,normal=None,mask=None,metallic=0,roughness=.06)
        materials.append(d)
    geometry_total=sum(len(g[-1]) for g in groups);markers=[]
    for name,point in [('main_pivot',value['pivots']['rotor']),('tail_pivot',value['pivots']['tail']),('nose_axis',np.array(value['pivots']['rotor'])+[0,0,-2])]:
        point=np.array(point);points=point+np.array([[-.0001,0,-.0001],[.0001,0,-.0001],[0,0,.0001]])
        markers.append((name,'ink',points,np.array([[0,1,0]]*3),np.zeros((3,2)),np.array([[0,2,1]])))
    total=write_model('H_MH6',groups+markers);lods=[]
    for level,grid in [(1,32),(2,12)]:
        count=write_model(f'H_MH6_LOD{level}',[lower(g,grid) for g in groups]+markers);lods.append({'resource':f'Helicopters/Models/H_MH6_LOD{level}','triangles':count})
    (DEST/'Models/H_MH6.mtl').write_text(''.join('newmtl '+m['name']+'\nKd 1 1 1\n' for m in materials))
    points=np.concatenate([g[2] for g in groups]);definition={'id':'H_MH6','resource':'Helicopters/Models/H_MH6','minimum':points.min(axis=0).tolist(),'maximum':points.max(axis=0).tolist(),'materials':materials,'lods':lods,'triangles':total,'sourceSha256':hashlib.sha256(raw).hexdigest(),'sourceUrl':BASE+'dist/assets/little-bird.js','upstreamRevision':REV,'modelLicense':'CC-BY-4.0','authors':['AnirudhRao'],'sourceDistributor':'jdubba1/warbird','sourceDerivative':True,'originalSketchfabDownloadVerified':False,'sourceContainsOriginalUvs':False,'sourceTriangles':source_triangles,'degenerateSourceTrianglesRemoved':source_triangles-geometry_total,'pivotMarkerTriangles':len(markers),'sourceRotorPivots':value['pivots'],'changes':'New crease-aware normals, metre-space UVs, original periodic PBR finishes, two derived LODs, native rotor pivots; no upstream game code/unlit shader, weapons or combat reused','nativeUnityValidated':False}
    (DEST/'H_MH6.json').write_text(json.dumps(definition,indent=2)+'\n')
    licenses=DEST/'Licenses';licenses.mkdir(exist_ok=True);(licenses/'SOURCE_CREDIT.txt').write_bytes(credits);(licenses/'CC-BY-4.0.txt').write_bytes(license_text)
    (licenses/'NOTICE.txt').write_text('MH-6 Little Bird by AnirudhRao, Creative Commons Attribution 4.0 International.\nhttps://creativecommons.org/licenses/by/4.0/\nOriginal model: https://sketchfab.com/3d-models/mh-6-little-bird-d6ec6eeb84b240789f54923bdedafc86\nGeometry derivative from jdubba1/warbird at '+REV+'.\nChanges: '+definition['changes']+'.\nOriginal derivative and its credit retained in Sources/Helicopter. No endorsement implied.\n')
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':1,'models':[definition]},indent=2)+'\n')
    files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json');(DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n');LOCK.write_text(json.dumps(expected,indent=2)+'\n')
    maps={p.relative_to(ROOT).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted((DEST/'Textures').glob('*.png'))};(ROOT/'Verification/helicopter_finish_textures.json').write_text(json.dumps({'size':1024,'provenance':'Original periodic paint, metal and rubber finishes for attributed helicopter geometry; no downloaded texture maps','files':maps},indent=2)+'\n')
    print(json.dumps({'triangles':total,'lods':lods,'minimum':definition['minimum'],'maximum':definition['maximum'],'distinctSources':1,'license':'CC-BY-4.0'}))

if __name__=='__main__':main()
