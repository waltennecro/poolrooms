"""Integrate two CC BY Bistro building derivatives as doorway-safe exterior skins.

The authored scene is downloaded as data, never executed. Four-storey City11
interiors retain their proven physical floor/stair/window/door geometry. Imported
interior proxies and closed door/window panels are removed offline with UV/normal
interpolation. The resulting skins are explicitly adaptations, not untouched assets.
"""
import argparse,hashlib,json,sys
from pathlib import Path
import numpy as np
from import_bistro_sources import acquire,extract,BASE,REV,LICENSE_REV,locked
from import_city_sources import convert
from building_geometry import cut,building_boxes,read_obj,write_obj
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Buildings';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Buildings'
MODELS={'B_BuildingCourtyard':'Paris_Building_011','B_BuildingCorner':'Paris_Building_003'}


LAST_FIT={}
def adapt(entries,doc):
    global LAST_FIT
    entries=[e for e in entries if not any(s in doc['materials'][e[0]]['name'].lower() for s in ['room_interior','glass_','curtains'])]
    angle_sum=0j
    for _,p,n,uv,f in entries:
        t=p[f];cross=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);area=np.linalg.norm(cross,axis=1);normal=cross/(area[:,None]+1e-20)
        keep=np.abs(normal[:,1])<.2
        theta=np.arctan2(normal[keep,2],normal[keep,0]);angle_sum+=np.sum(area[keep]*np.exp(4j*theta))
    angle=np.angle(angle_sum)/4;co,si=np.cos(angle),np.sin(angle);rotation=np.array([[co,0,si],[0,1,0],[-si,0,co]])
    for e in entries:e[1]=e[1]@rotation.T;e[2]=e[2]@rotation.T
    points=np.concatenate([e[1] for e in entries]);lo=points.min(0);hi=points.max(0);center=(lo+hi)*.5;planes=[]
    for axis in [0,2]:
        bins=[{},{}]
        for _,p,n,uv,f in entries:
            t=p[f];cross=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);area=np.linalg.norm(cross,axis=1);normal=cross/(area[:,None]+1e-20);centers=t.mean(1)
            for sign in [0,1]:
                direction=sign*2-1
                keep=(normal[:,axis]*direction>.98)&((centers[:,axis]-center[axis])*direction>(hi[axis]-lo[axis])*.2)
                for coordinate,weight in zip(np.round(centers[keep,axis]*20)/20,area[keep]):bins[sign][float(coordinate)]=bins[sign].get(float(coordinate),0)+weight
        assert all(bins),"Source has no consistent opposing facade planes"
        a=max(bins[0],key=bins[0].get);b=max(bins[1],key=bins[1].get);assert b-a>2
        planes.append((a,b))
    offset=np.array([(planes[0][0]+planes[0][1])*.5,lo[1],(planes[1][0]+planes[1][1])*.5])
    scale=np.array([10.36/(planes[0][1]-planes[0][0]),12.92/(hi[1]-lo[1]),10.36/(planes[1][1]-planes[1][0])])
    LAST_FIT={'sourceFacadePlanes':planes,'worldUpYawCorrectionRadians':float(angle),'linearScale':scale.tolist(),'offset':offset.tolist(),'targetFacadeHalfSpan':5.18}
    for e in entries:
        e[1]=(e[1]-offset)*scale;e[2]=e[2]/scale;e[2]/=np.linalg.norm(e[2],axis=1,keepdims=True)
    # Bound decorative projections to the reserved footprint, then carve every
    # physical approach/room. Core structure stays authoritative at every LOD.
    bounds=[]
    for axis in [0,2]:
        for sign in [-1,1]:
            low=np.array([-1e5,-1e5,-1e5]);high=-low.copy()
            if sign<0:high[axis]=-5.3
            else:low[axis]=5.3
            bounds.append((low,high))
    return cut(entries,bounds+building_boxes())


def prune_unused_maps(reports,destination):
    used={Path(m[key]).name+'.png' for report in reports for m in report['materials'] for key in ['albedo','normal','mask','specular','emissive'] if m.get(key)}
    removed=[]
    for path in (destination/'Textures').glob('*.png'):
        if path.name.startswith(tuple(MODELS)) and path.name not in used:
            path.unlink();removed.append(path.name)
    return sorted(removed)


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--cache',required=True,type=Path);args=parser.parse_args()
    if (SOURCE/'sources.lock.json').exists():locked.update(json.loads((SOURCE/'sources.lock.json').read_text()))
    SOURCE.mkdir(exist_ok=True);(DEST/'Licenses').mkdir(parents=True,exist_ok=True)
    original=acquire(BASE+'bistro.gltf',args.cache);binary=acquire(BASE+'bistro.bin',args.cache)
    assert hashlib.sha256(binary).hexdigest()=='5fb11a1d61aeb2537197aa97833ff950638c9d42451ee4fafbc9550f03918faa'
    doc=json.loads(original);reports=[]
    for name,node in MODELS.items():
        print('Extracting building',node,flush=True)
        raw,specular,detail=extract(doc,binary,node,args.cache,adapt);(SOURCE/(name+'_adapted.glb')).write_bytes(raw)
        report=convert(name,raw,'Buildings',DEST,True,'CC-BY-4.0',False,[(1,40,20),(2,20,12)],True)
        for i,(material,image) in enumerate(zip(report['materials'],specular)):
            if image is None:continue
            resource=f'Buildings/Textures/{name}_m{i}_specular';image.save(ROOT/'Assets/PoolroomsProcedural/Resources'/(resource+'.png'));material['workflow']='specular';material['specular']=resource
        material_names=[m['name'] for m in report['materials']]
        for lod in report['lods']:
            path=ROOT/'Assets/PoolroomsProcedural/Resources'/(lod['resource']+'.obj')
            entries=cut(read_obj(path,material_names),building_boxes());write_obj(path,entries,material_names)
            lod['triangles']=sum(len(e[4]) for e in entries)
        from source_lods import compact
        removed=[]
        for resource in [{'resource':report['resource'],'triangles':report['triangles']}]+report['lods']:
            path=ROOT/'Assets/PoolroomsProcedural/Resources'/(resource['resource']+'.obj')
            entries=read_obj(path,material_names);before=sum(len(e[4]) for e in entries);clean=[]
            for material,p,n,uv,faces in entries:
                t=p[faces];keep=np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9
                if keep.any():clean.append((material,p,n,uv,faces[keep]))
            clean=compact(clean);write_obj(path,clean,material_names);after=sum(len(e[4]) for e in clean);removed.append(before-after)
            if resource['resource']==report['resource']:report['triangles']=after
            else:resource['triangles']=after
        report['nativeDegenerateTrianglesRemovedByLod']=removed
        # A hidden orientation marker is added AFTER simplification, so every LOD
        # keeps it. Runtime reads the actual imported marker instead of assuming
        # that a particular native OBJ importer flips X or Z.
        for resource in [{'resource':report['resource'],'triangles':report['triangles']}]+report['lods']:
            path=ROOT/'Assets/PoolroomsProcedural/Resources'/(resource['resource']+'.obj')
            count=sum(1 for line in path.read_text().splitlines() if line.startswith('v '))
            with path.open('a') as f:
                f.write('o '+name+'_EntryOrientationMarker\nusemtl '+material_names[0]+'\n')
                for value in [(-.0002,12.7998,-5.15),(.0002,12.7998,-5.15),(0,12.8002,-5.15)]:f.write('v '+' '.join(map(str,value))+'\n')
                f.write('vt 0 0\nvt 1 0\nvt .5 1\nvn 0 0 -1\nvn 0 0 -1\nvn 0 0 -1\n')
                f.write('f '+' '.join(f'{i+count}/{i+count}/{i+count}' for i in [1,3,2])+'\n')
            if resource['resource']==report['resource']:report['triangles']+=1
            else:resource['triangles']+=1
        report['hiddenOrientationMarkerTrianglesPerLod']=1
        report['adapterFit']=LAST_FIT
        source_root=detail['sourceRootNode']
        def count_triangles(index):
            n=doc['nodes'][index];return (sum(doc['accessors'][p['indices']]['count']//3 for p in doc['meshes'][n['mesh']]['primitives']) if 'mesh' in n else 0)+sum(count_triangles(i) for i in n.get('children',[]))
        report['sourceSubtreeTriangles']=count_triangles(source_root)
        report.update(detail,sourceUrl=BASE+'bistro.gltf',upstreamRevision=REV,authors=['Amazon.com, Inc. or its affiliates'],modelLicense='CC-BY-4.0',sourceBinarySha256=hashlib.sha256(binary).hexdigest(),nativeUnityValidated=False,canonicalWidth=10,canonicalDepth=10,canonicalFloors=4,canonicalFloorHeight=3.2,adaptation='Authored complete building subtree adapted as a visual skin, source interior/glass proxies removed, canonical four-storey fit, interior/door/window geometry subtracted with UV interpolation, authored PBR specular/gloss and two LOD derivatives. Native City11 primary architecture and collision remain authoritative.')
        (DEST/(name+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report)
        print(name,'triangles',report['triangles'],'LODs',report['lods'],flush=True)
    for name in ['Bistro_CC_BY_4.txt','upstream_MIT.txt','upstream_README.md']:(DEST/'Licenses'/name).write_bytes((ROOT/'Sources/Bistro'/name).read_bytes());(SOURCE/name).write_bytes((ROOT/'Sources/Bistro'/name).read_bytes())
    notice='Amazon Lumberyard Bistro v5.2, Amazon.com, Inc. or its affiliates, CC BY 4.0. Original https://developer.nvidia.com/orca/amazon-lumberyard-bistro . Data source zeux/niagara_bistro revision '+REV+'. Changes: two building subtree adaptations into four-storey doorway-safe exterior skins, cut interiors/windows/doors, native PNG/OBJ, authored specular/gloss conversion and LOD derivatives. Original interiors are not preserved; generated accessible interiors are by Poolrooms. No endorsement. Full CC BY and upstream MIT notices included.\n'
    for p in [SOURCE,DEST/'Licenses']:(p/'NOTICE.txt').write_text(notice)
    (SOURCE/'sources.lock.json').write_text(json.dumps(locked,indent=2)+'\n')
    prune_unused_maps(reports,DEST)
    (DEST/'catalog.json').write_text(json.dumps({'distinctBuildingSourceFamilies':2,'untouchedCompleteModels':0,'exteriorSkinAdaptations':2,'nativeUnityValidated':False,'models':reports},indent=2)+'\n')
    (DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'},indent=2)+'\n')
if __name__=='__main__':main()
