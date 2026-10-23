"""Extract a third distinct CC BY 4.0 building skin; no opaque source interiors.

Original authored texture resolutions are retained in reused 4K/2K banks;
new source-only maps are restored from native DDS inputs, capped at 4K.
Invalid/interpolated opposite face normals are repaired only in this derivative.
"""
from pathlib import Path
import sys,json,hashlib,copy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import import_bistro_sources as upstream
import import_building_sources as building
from import_city_sources import convert
from building_geometry import read_obj,write_obj,cut,building_boxes
from source_lods import compact
import numpy as np
root=Path(__file__).resolve().parents[1];dest=root/'Assets/PoolroomsProcedural/Resources/CityArchitectureSources';source=root/'Sources/CityArchitecture';parser=__import__('argparse').ArgumentParser(description='Import one licensed Bistro facade derivative');parser.add_argument('--cache',type=Path,required=True);cache=parser.parse_args().cache
dest.mkdir(exist_ok=True);source.mkdir(exist_ok=True);(dest/'Licenses').mkdir(exist_ok=True)
# Read/download the pinned upstream document; match the already licensed source.
raw=upstream.acquire(upstream.BASE+'bistro.gltf',cache);binary=upstream.acquire(upstream.BASE+'bistro.bin',cache);doc=json.loads(raw)
assert hashlib.sha256(raw).hexdigest()==hashlib.sha256((root/'Sources/Bistro/bistro_source.gltf').read_bytes()).hexdigest()
def adapted(entries,document):
 entries=[e for e in entries if document['materials'][e[0]].get('alphaMode','OPAQUE')=='OPAQUE']
 return building.adapt(entries,document)
name='C_BuildingArcade';data,specular,detail=upstream.extract(doc,binary,'Paris_Building_010',cache,adapted)
(source/(name+'_adapted.glb')).write_bytes(data)
report=convert(name,data,'CityArchitectureSources',dest,True,'CC-BY-4.0',False,[(1,40,20),(2,20,12)],True)
# Reuse identical authored high-resolution source materials, without copying maps.
lookup={}
for bank in ['MallFinishes','JourneyFinishes']:
 for m in json.loads((dest.parent/bank/'palette.json').read_text())['materials']:
  lookup[m.get('sourceName',m['name'])]=m
for i,(m,spec) in enumerate(zip(report['materials'],specular)):
 exact=lookup.get(doc['materials'][detail['sourceMaterialIndices'][i]]['name'])
 if exact:
  nativeName=m['name'];m.clear();m.update(copy.deepcopy(exact));m['name']=nativeName
 elif spec is not None:
  resource=f'CityArchitectureSources/Textures/{name}_m{i}_specular';spec.save(dest.parent/(resource+'.png'));m['workflow']='specular';m['specular']=resource
def prune_exported_faces(path):
 lines=path.read_text().splitlines();v=[];out=[];count=0
 for line in lines:
  if line.startswith('v '):v.append([float(x) for x in line.split()[1:]])
  if line.startswith('f '):
   t=np.array([v[int(x.split('/')[0])-1] for x in line.split()[1:]])
   if np.linalg.norm(np.cross(t[1]-t[0],t[2]-t[0]))<=1e-10:continue
   count+=1
  out.append(line.replace('see Buildings/Licenses','see CityArchitectureSources/Licenses'))
 path.write_text('\n'.join(out)+'\n');return count

names=[m['name'] for m in report['materials']]
for resource in [{'resource':report['resource']}]+report['lods']:
 p=dest.parent/(resource['resource']+'.obj');entries=read_obj(p,names);repaired=[]
 for mat,v,n,uv,f in entries:
  t=v[f];cross=np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]);length=np.linalg.norm(cross,axis=1);keep=length>1e-9;t=t[keep];nn=n[f][keep].copy();tt=uv[f][keep];face=cross[keep]/length[keep,None]
  bad=np.any(np.sum(nn*face[:,None,:],axis=2)<.05,axis=1);nn[bad]=face[bad,None,:]
  repaired.append((mat,t.reshape(-1,3),nn.reshape(-1,3),tt.reshape(-1,2),np.arange(len(t)*3).reshape(-1,3)))
 entries=cut(repaired,building_boxes());clean=[]
 for mat,v,n,uv,f in entries:
  t=v[f];keep=np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-9
  if keep.any():clean.append((mat,v,n,uv,f[keep]))
 entries=compact(clean);write_obj(p,entries,names);count=sum(len(e[4]) for e in entries)
 # Native importer-independent facing marker, hidden by the existing loader.
 vertexCount=sum(1 for line in p.read_text().splitlines() if line.startswith('v '))
 with p.open('a') as f:
  f.write('o '+name+'_EntryOrientationMarker\nusemtl '+names[0]+'\n')
  for v in [(-.0002,12.7998,-5.15),(.0002,12.7998,-5.15),(0,12.8002,-5.15)]: f.write('v '+' '.join(map(str,v))+'\n')
  f.write('vt 0 0\nvt 1 0\nvt .5 1\nvn 0 0 -1\nvn 0 0 -1\nvn 0 0 -1\n')
  f.write('f '+' '.join(f'{vertexCount+i}/{vertexCount+i}/{vertexCount+i}' for i in [1,3,2])+'\n')
 count=prune_exported_faces(p)
 if resource['resource']==report['resource']:report['triangles']=count
 else:resource['triangles']=count
used={Path(m[k]).name+'.png' for m in report['materials'] for k in ['albedo','normal','mask','specular','emissive'] if m.get(k,'').startswith('CityArchitectureSources/')}
for p in (dest/'Textures').glob('*.png'):
 if p.name not in used:p.unlink()
report.update(detail,sourceUrl=upstream.BASE+'bistro.gltf',upstreamRevision=upstream.REV,modelLicense='CC-BY-4.0',canonicalWidth=10,canonicalDepth=10,canonicalFloors=4,nativeUnityValidated=False,adaptation='Third distinct Bistro building source subtree; canonical doorway/window/interior cut, physical shell generated separately; shared source PBR maps; two clipped LODs.')
(dest/(name+'.json')).write_text(json.dumps(report,indent=2)+'\n')
for n in ['Bistro_CC_BY_4.txt','upstream_MIT.txt']:(dest/'Licenses'/n).write_bytes((root/'Sources/Bistro'/n).read_bytes())
(dest/'Licenses/NOTICE.txt').write_text('Amazon Lumberyard Bistro, Amazon.com, Inc. or its affiliates, CC BY 4.0. https://developer.nvidia.com/orca/amazon-lumberyard-bistro . Pinned source: '+upstream.BASE+'bistro.gltf . Adaptation: Paris_Building_010 as an exterior skin with generated interior clearances, two LODs and reused native source PBR finishes. Texture adaptations: decode original DDS to PNG at native resolution up to 4K, invert tangent-normal green for Unity, retain authored specular colour/gloss and pack the HDRP mask; original input sizes/hashes are recorded. Original interior is not retained. No endorsement.\n')
(dest/'catalog.json').write_text(json.dumps({'distinctNewSourceModels':1,'proceduralBuildingDesigns':256,'untouchedCompleteModels':0,'nativeUnityValidated':False,'models':[report]},indent=2)+'\n')
(source/'sources.lock.json').write_text(json.dumps(upstream.locked,indent=2)+'\n')
(dest/'files.sha256.json').write_text(json.dumps({p.relative_to(dest).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(dest.rglob('*')) if p.is_file() and p.name!='files.sha256.json'},indent=2)+'\n')
from refine_city_architecture_textures import main as native_finishes
native_finishes(cache)
print('SOURCE_COMPLETE',report['triangles'],report['lods'])
