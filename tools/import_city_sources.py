"""Acquire verified CC0 sources and produce native Unity OBJ/PBR assets.

Only the selected glTF core metal/roughness materials are adapted. Optional
transmission/clearcoat/sheen are recorded, not advertised as exact HDRP matches.
No Draco, skins, morphs, external URIs or sparse accessors are silently accepted.
Requires NumPy and Pillow. Metadata/license text attribution is retained.
"""
from pathlib import Path
import argparse,hashlib,json,struct,urllib.request
import numpy as np
from PIL import Image
from io import BytesIO

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/City11'
SOURCES=ROOT/'Sources/City11'
UPSTREAM_REVISION='edc7c9e67c639d230715049ee31f9a96a6babbbe'
BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{UPSTREAM_REVISION}/Models/'
MODELS=('WaterBottle','Lantern','BoomBox','ToyCar','SheenChair')
LOCK_PATH=SOURCES/'sources.lock.json'
LOCK=json.loads(LOCK_PATH.read_text()) if LOCK_PATH.exists() else {}

def acquire(url,path):
    if not path.exists():
        data=urllib.request.urlopen(url,timeout=45).read(96*1024*1024+1)
        if len(data)>96*1024*1024:raise ValueError('Source exceeds budget')
        path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
    data=path.read_bytes();key=path.relative_to(ROOT).as_posix()
    if key in LOCK:assert hashlib.sha256(data).hexdigest()==LOCK[key]['sha256'],'Pinned source changed: '+key
    LOCK[key]={'url':url,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    return data

def convert(name,raw,resource_root="City11",destination=None,preserve_nodes=False,source_license="CC0-1.0",bake_uvs=False,lod_levels=None,merge_nodes=False,bake_size=2048,skip_uniform=False,weld_charts=False):
    dest=DEST if destination is None else destination
    original_raw=raw;bake_report=[]
    if name=='SheenChair' or bake_uvs:
        from bake_gltf_uvs import bake
        raw,bake_report=bake(raw,size=bake_size,skip_uniform=skip_uniform,weld_charts=weld_charts)
    magic,version,total=struct.unpack_from('<III',raw)
    assert magic==0x46546c67 and version==2 and total==len(raw)
    n,tag=struct.unpack_from('<II',raw,12);assert tag==0x4e4f534a
    doc=json.loads(raw[20:20+n]);offset=20+n
    length,tag=struct.unpack_from('<II',raw,offset);assert tag==0x004e4942
    binary=raw[offset+8:offset+8+length]
    assert not any(b.get('uri') for b in doc['buffers'])
    assert not doc.get('skins') and not doc.get('animations'), 'Animated sources need a separate import path'
    for ext in doc.get('extensionsRequired',[]):
        assert ext in ('KHR_materials_transmission','KHR_materials_clearcoat','KHR_materials_sheen','KHR_materials_volume','KHR_materials_specular','KHR_materials_ior'),ext
    def accessor(index):
        a=doc['accessors'][index];assert not a.get('sparse') and not a.get('normalized')
        v=doc['bufferViews'][a['bufferView']]
        dtype={5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}[a['componentType']]
        size={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        item=np.dtype(dtype).itemsize;start=v.get('byteOffset',0)+a.get('byteOffset',0)
        stride=v.get('byteStride',size*item)
        result=np.ndarray((a['count'],size),dtype=dtype,buffer=binary,offset=start,strides=(stride,item)).copy()
        assert np.all(np.isfinite(result));return result
    def image_for(texture):
        info=doc['images'][doc['textures'][texture['index']]['source']]
        view=doc['bufferViews'][info['bufferView']];start=view.get('byteOffset',0)
        image=Image.open(BytesIO(binary[start:start+view['byteLength']])).convert('RGBA')
        transform=texture.get('extensions',{}).get('KHR_texture_transform')
        if transform:
            assert transform.get('rotation',0)==0 and transform.get('texCoord',texture.get('texCoord',0))==0,'UV rotation/alternate UV baking required'
            scale=transform.get('scale',[1,1]);offset=transform.get('offset',[0,0])
            assert all(v>=1 and int(v)==v for v in scale) and all(int(v)==v for v in offset),'Only periodic integer texture transforms are supported'
            data=np.asarray(image);h,w=data.shape[:2]
            xx=(np.arange(w)*int(scale[0]))%w;yy=(np.arange(h)*int(scale[1]))%h
            image=Image.fromarray(data[yy[:,None],xx[None,:]])
        return image
    materials=[];native=dest/'Textures';native.mkdir(parents=True,exist_ok=True)
    for i,mat in enumerate(doc.get('materials',[{}])):
        pbr=mat.get('pbrMetallicRoughness',{});prefix=f'{name}_m{i}'
        definition={'name':prefix,'colour':pbr.get('baseColorFactor',[1,1,1,1]),'normalScale':mat.get('normalTexture',{}).get('scale',1),
                    'metallic':pbr.get('metallicFactor',1),'roughness':pbr.get('roughnessFactor',1),'emission':mat.get('emissiveFactor',[0,0,0]),
                    'extensions':list(mat.get('extensions',{})),'transparent':mat.get('alphaMode')=='BLEND','doubleSided':mat.get('doubleSided',False)}
        strength=mat.get('extensions',{}).get('KHR_materials_emissive_strength',{}).get('emissiveStrength',1)
        if strength!=1:definition['emission']=[x*strength for x in definition['emission']]
        for prop,key in [('baseColorTexture','albedo'),('normalTexture','normal'),('emissiveTexture','emissive')]:
            tex=pbr.get(prop) if prop=='baseColorTexture' else mat.get(prop)
            if tex:
                assert tex.get('texCoord',0)==0, 'Alternate UV baking required'
                image=image_for(tex)
                if key=='normal':
                    data=np.array(image);data[:,:,1]=255-data[:,:,1];image=Image.fromarray(data)
                image.save(native/(prefix+'_'+key+'.png'));definition[key]=resource_root+'/Textures/'+prefix+'_'+key
        mr=pbr.get('metallicRoughnessTexture');ao=mat.get('occlusionTexture')
        if mr:assert mr.get('texCoord',0)==0
        if ao:assert ao.get('texCoord',0)==0
        shape=image_for(mr).size if mr else image_for(ao).size if ao else (4,4)
        mask=np.ones((shape[1],shape[0],4),dtype=np.float32)
        mask[:,:,0]=definition['metallic'];mask[:,:,3]=1-definition['roughness']
        if mr:
            data=np.asarray(image_for(mr).resize(shape),dtype=np.float32)/255
            mask[:,:,0]=data[:,:,2]*definition['metallic'];mask[:,:,3]=1-data[:,:,1]*definition['roughness']
        if ao:
            strength=ao.get('strength',1);mask[:,:,1]=1-strength+strength*np.asarray(image_for(ao).resize(shape),dtype=np.float32)[:,:,0]/255
        Image.fromarray(np.uint8(np.clip(mask,0,1)*255+.5)).save(native/(prefix+'_mask.png'))
        definition['mask']=resource_root+'/Textures/'+prefix+'_mask';materials.append(definition)
    entries=[];entry_names=[]
    def walk(index,parent,ancestors):
        assert index not in ancestors,'Cyclic glTF node graph'
        node=doc['nodes'][index]
        if 'matrix' in node:matrix=np.array(node['matrix']).reshape((4,4),order='F')
        else:
            x,y,z,w=node.get('rotation',[0,0,0,1]);r=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
            matrix=np.eye(4);matrix[:3,:3]=r@np.diag(node.get('scale',[1,1,1]));matrix[:3,3]=node.get('translation',[0,0,0])
        matrix=parent@matrix
        if 'mesh' in node:
            for primitive in doc['meshes'][node['mesh']]['primitives']:
                # The source presentation includes a large cloth pedestal. It is not
                # part of a driveable car; keep it only in the untouched original GLB.
                if name=='ToyCar' and primitive.get('material')==1:continue
                assert primitive.get('mode',4)==4 and not primitive.get('targets')
                attr=primitive['attributes'];positions=accessor(attr['POSITION']).astype(float);normals=accessor(attr['NORMAL']).astype(float);uv=accessor(attr['TEXCOORD_0']).astype(float)
                positions=positions@matrix[:3,:3].T+matrix[:3,3];normals=normals@np.linalg.inv(matrix[:3,:3]);normals/=np.linalg.norm(normals,axis=1,keepdims=True)
                # OBJ is right-handed like glTF; Unity's native model importer owns its
                # coordinate conversion. Avoid converting handedness twice.
                uv[:,1]=1-uv[:,1]
                indices=accessor(primitive['indices']).reshape((-1,3)).astype(int)
                if np.linalg.det(matrix[:3,:3])<0:indices=indices[:,[0,2,1]]
                assert indices.min()>=0 and indices.max()<len(positions)
                entries.append((primitive.get('material',0),positions,normals,uv,indices));entry_names.append(node.get('name','part'))
        for child in node.get('children',[]):walk(child,matrix,ancestors|{index})
    for node in doc['scenes'][doc.get('scene',0)]['nodes']:walk(node,np.eye(4),set())
    if merge_nodes:
        merged={}
        for entry,label in zip(entries,entry_names):merged.setdefault((label,entry[0]),[]).append(entry)
        entries=[];entry_names=[]
        for (label,material),group in merged.items():
            start=0;faces=[]
            for e in group:faces.append(e[4]+start);start+=len(e[1])
            entries.append((material,*[np.concatenate([e[k] for e in group]) for k in (1,2,3)],np.concatenate(faces)));entry_names.append(label)
    folder=dest/'Models';folder.mkdir(parents=True,exist_ok=True)
    if name=='ToyCar':
        points=np.concatenate([e[1] for e in entries]);_,axes=np.linalg.eigh(np.cov(points[:,[0,2]],rowvar=False));direction=axes[:,-1]
        angle=-np.arctan2(direction[0],direction[1]);co,si=np.cos(angle),np.sin(angle)
        rotation=np.array([[co,0,si],[0,1,0],[-si,0,co]])
        entries=[(m,p@rotation.T,n@rotation.T,t,f) for m,p,n,t,f in entries]
    wheels=[]
    if name=='ToyCar':
        from rig_city_car import split
        entries,entry_names,wheels=split(entries);preserve_nodes=True
    all_positions=np.concatenate([e[1] for e in entries]);lo=all_positions.min(axis=0);hi=all_positions.max(axis=0)
    with (folder/(name+'.obj')).open('w') as f:
        header='# Adapted from verified CC0 glTF; see City11/Licenses' if source_license=='CC0-1.0' and resource_root=='City11' else f'# Adapted from licensed glTF ({source_license}); see {resource_root}/Licenses'
        f.write(header+f'\nmtllib {name}.mtl\n');start=1
        for entry_index,(material,positions,normals,uv,indices) in enumerate(entries):
            part=(entry_names[entry_index].replace(' ','_')+'_') if preserve_nodes else ''
            f.write(f'o {name}_{part}m{material}\nusemtl {name}_m{material}\n')
            for v in positions:f.write('v '+' '.join(f'{p:.8g}' for p in v)+'\n')
            for v in uv:f.write('vt '+' '.join(f'{p:.8g}' for p in v)+'\n')
            for v in normals:f.write('vn '+' '.join(f'{p:.8g}' for p in v)+'\n')
            for face in indices:f.write('f '+' '.join(f'{i+start}/{i+start}/{i+start}' for i in face)+'\n')
            start+=len(positions)
    (folder/(name+'.mtl')).write_text(''.join('newmtl '+m['name']+'\nKd '+' '.join(map(str,m['colour'][:3]))+'\n' for m in materials))
    lods=[]
    if name in ('ToyCar','SheenChair') or lod_levels:
        levels=lod_levels or ([(1,64,64),(2,24,16)] if name=='ToyCar' else [(1,32,16),(2,12,8)])
        for level,grid,uv_grid in levels:
            from source_lods import cluster
            groups=cluster(entries,grid,uv_grid,lo,hi,preserve_normals=resource_root=='Urban')
            lod_name=name+'_LOD'+str(level);start=1
            with (folder/(lod_name+'.obj')).open('w') as f:
                license_label='CC0' if source_license=='CC0-1.0' else source_license
                f.write(f'# LOD adapted from the same {license_label} model, not a new source model\nmtllib {name}.mtl\n')
                for entry_index,(material,positions,normals,uv,indices) in enumerate(groups):
                    part=(entry_names[entry_index].replace(' ','_')+'_') if preserve_nodes else ''
                    f.write(f'o {name}_{part}m{material}\nusemtl {name}_m{material}\n')
                    for v in positions:f.write('v '+' '.join(f'{p:.8g}' for p in v)+'\n')
                    for v in uv:f.write('vt '+' '.join(f'{p:.8g}' for p in v)+'\n')
                    for v in normals:f.write('vn '+' '.join(f'{p:.8g}' for p in v)+'\n')
                    for face in indices:f.write('f '+' '.join(f'{i+start}/{i+start}/{i+start}' for i in face)+'\n')
                    start+=len(positions)
            lods.append({'resource':resource_root+'/Models/'+lod_name,'triangles':sum(len(e[4]) for e in groups)})
    record={'id':name,'resource':resource_root+'/Models/'+name,'minimum':lo.tolist(),'maximum':hi.tolist(),'triangles':sum(len(e[4]) for e in entries),'materials':materials,'lods':lods,
            'uvBake':bake_report,'sourceSha256':hashlib.sha256(original_raw).hexdigest(),'upstreamRevision':UPSTREAM_REVISION,'presentationClothRemoved':name=='ToyCar','adaptation':'Native OBJ, glTF core metal/roughness to HDRP mask; optional extensions require native review'}
    if preserve_nodes:record['sourceNodeNames']=entry_names
    if wheels:record['wheels']=wheels;record['forwardAxis']=[0,0,1]
    (dest/(name+'.json')).write_text(json.dumps(record,indent=2)+'\n');return record

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--only',nargs='+',choices=MODELS,help='Rebuild selected sources, retaining already adapted files for the others');args=parser.parse_args()
    licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True);reports=[]
    for name in MODELS:
        meta=acquire(BASE+name+'/metadata.json',licenses/(name+'_metadata.json'));j=json.loads(meta)
        assert j['legal'] and all(x.get('spdx')=='CC0-1.0' for x in j['legal']), 'Model license is not fully CC0: '+name
        acquire(BASE+name+'/LICENSE.md',licenses/(name+'_LICENSE.md'))
        raw=acquire(BASE+name+'/glTF-Binary/'+name+'.glb',SOURCES/(name+'.glb'))
        report=convert(name,raw) if args.only is None or name in args.only else json.loads((DEST/(name+'.json')).read_text());report['sourceUrl']=BASE+name+'/glTF-Binary/'+name+'.glb';report['modelLicense']='CC0-1.0';report['metadataLicense']='CC-BY-4.0';report['authors']=[x['artist'] for x in j['legal']]
        reports.append(report);print(name,report['triangles'],'triangles')
    catalog={'distinctSourceModels':len(reports),'colourVariantsCountAsModels':False,'nativeUnityValidated':False,'models':reports}
    (DEST/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n')
    files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json')
    (DEST/'files.sha256.json').write_text(json.dumps({str(p.relative_to(DEST)):{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n')
    LOCK_PATH.write_text(json.dumps(LOCK,indent=2)+'\n')
    print('Verified and adapted',len(reports),'distinct CC0 sources; native import still pending.')

if __name__=='__main__':main()
