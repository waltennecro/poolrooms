"""Extract three attributed Bistro pieces from pinned original geometry.

Downloads are data only. The complete upstream scene/BC7 textures remain in an
external cache; selected, rebased GLBs with bounded PNGs ship as documented
adaptations, not as untouched original downloads. No upstream script executes.
"""
from pathlib import Path
import argparse,hashlib,json,struct,urllib.request
from io import BytesIO
import numpy as np
from PIL import Image
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Bistro';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Bistro'
REV='2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e';BASE=f'https://raw.githubusercontent.com/zeux/niagara_bistro/{REV}/'
LICENSE_REV='41b6f6ca07da385b5be8f632f440b25a03a427f5'
MODELS={'Awning':'paris_bistroawning_1','CafeTable':'Paris_Table_01','CafeChair':'Paris_Bistro_Chair_01'}
LOCK=SOURCE/'sources.lock.json';locked=json.loads(LOCK.read_text()) if LOCK.exists() else {}

def acquire(url,cache):
 key=url.split('://',1)[1];p=cache/key
 if not p.exists():
  p.parent.mkdir(parents=True,exist_ok=True);temp=p.with_suffix(p.suffix+'.partial')
  with urllib.request.urlopen(url,timeout=60) as r,temp.open('wb') as f:
   while True:
    b=r.read(1024*1024)
    if not b:break
    f.write(b)
    if f.tell()>100*1024*1024:raise ValueError('Source exceeds 100 MiB budget')
  temp.replace(p)
 raw=p.read_bytes();record={'url':url,'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
 if key in locked:assert record==locked[key],'Pinned source changed: '+key
 locked[key]=record;return raw

def matrix(n):
 if 'matrix' in n:return np.array(n['matrix']).reshape(4,4,order='F')
 x,y,z,w=n.get('rotation',[0,0,0,1]);m=np.eye(4)
 m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag(n.get('scale',[1,1,1]));m[:3,3]=n.get('translation',[0,0,0]);return m

def extract(doc,binary,node_name,cache,geometry_transform=None,max_texture_size=1024):
 matches=[i for i,n in enumerate(doc['nodes']) if n.get('name')==node_name];assert len(matches)==1
 root=matches[0];parents={c:i for i,n in enumerate(doc['nodes']) for c in n.get('children',[])}
 def world(i):return (world(parents[i]) if i in parents else np.eye(4))@matrix(doc['nodes'][i])
 # Preserve world up: exporter object axes include pitch/roll and must not
 # turn an upright awning or chair onto its side when removing scene placement.
 inverse=np.eye(4)
 def accessor(i):
  a=doc['accessors'][i];assert not a.get('sparse') and not a.get('normalized');v=doc['bufferViews'][a['bufferView']]
  dt={5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']];count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
  return np.ndarray((a['count'],count),dtype=dt,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',count*np.dtype(dt).itemsize),np.dtype(dt).itemsize)).copy()
 entries=[]
 def walk(i,parent):
  n=doc['nodes'][i];m=parent@matrix(n)
  if 'mesh' in n:
   for p in doc['meshes'][n['mesh']]['primitives']:
    assert p.get('mode',4)==4;attr=p['attributes'];pos=accessor(attr['POSITION'])@m[:3,:3].T+m[:3,3];normal=accessor(attr['NORMAL'])@np.linalg.inv(m[:3,:3]);normal/=np.linalg.norm(normal,axis=1,keepdims=True);faces=accessor(p['indices']).reshape(-1,3)
    if np.linalg.det(m[:3,:3])<0:faces=faces[:,[0,2,1]]
    entries.append([p['material'],pos,normal,accessor(attr['TEXCOORD_0']),faces])
  for child in n.get('children',[]):walk(child,m)
 walk(root,inverse@(world(parents[root]) if root in parents else np.eye(4)))
 points=np.concatenate([e[1] for e in entries]);_,axes=np.linalg.eigh(np.cov(points[:,[0,2]],rowvar=False));direction=axes[:,-1]
 angle=-np.arctan2(direction[0],direction[1])+np.pi*.5;co,si=np.cos(angle),np.sin(angle);rotation=np.array([[co,0,si],[0,1,0],[-si,0,co]])
 for e in entries:e[1]=e[1]@rotation.T;e[2]=e[2]@rotation.T
 points=np.concatenate([e[1] for e in entries]);lo=points.min(0);hi=points.max(0)
 # Put the raised attachment/back of chairs and awnings at local +Z.
 if np.cov(points[:,1],points[:,2])[0,1]<0:
  rotation=np.diag([-1,1,-1])
  for e in entries:e[1]=e[1]@rotation;e[2]=e[2]@rotation
  points=np.concatenate([e[1] for e in entries]);lo=points.min(0);hi=points.max(0)
 offset=np.array([(lo[0]+hi[0])*.5,lo[1],(lo[2]+hi[2])*.5])
 for e in entries:e[1]-=offset
 if geometry_transform is not None:entries=geometry_transform(entries,doc)
 used=sorted({e[0] for e in entries});out={'asset':{'version':'2.0','generator':'Poolrooms licensed Bistro extraction'},'scenes':[{'nodes':[0]}],'scene':0,'nodes':[{'mesh':0}],'meshes':[{'primitives':[]}],'materials':[],'accessors':[],'bufferViews':[],'textures':[],'images':[]};blob=bytearray();specular=[]
 def append(raw):
  while len(blob)%4:blob.append(0)
  index=len(out['bufferViews']);out['bufferViews'].append({'buffer':0,'byteOffset':len(blob),'byteLength':len(raw)});blob.extend(raw);return index
 def attribute(values,kind):
  values=np.asarray(values,dtype='<u4' if kind=='SCALAR' else '<f4');index=len(out['accessors']);out['accessors'].append({'bufferView':append(values.tobytes()),'componentType':5125 if kind=='SCALAR' else 5126,'count':len(values),'type':kind});return index
 def image_for(info):
  t=doc['textures'][info['index']];assert not info.get('extensions') and not info.get('texCoord',0)
  uri=doc['images'][t.get('extensions',{}).get('MSFT_texture_dds',{}).get('source',t['source'])]['uri'];raw=acquire(BASE+uri,cache)
  im=Image.open(BytesIO(raw)).convert('RGBA');im.thumbnail((max_texture_size,max_texture_size),Image.Resampling.LANCZOS);return im
 def texture(image):
  stream=BytesIO();image.save(stream,format='PNG');view=append(stream.getvalue());index=len(out['textures']);out['images'].append({'bufferView':view,'mimeType':'image/png'});out['textures'].append({'source':index});return {'index':index}
 for i in used:
  original=doc['materials'][i];assert original.get('alphaMode','OPAQUE')=='OPAQUE'
  sg=original.get('extensions',{}).get('KHR_materials_pbrSpecularGlossiness')
  if sg is None:
   source_pbr=original.get('pbrMetallicRoughness',{});pbr={key:source_pbr[key] for key in ['baseColorFactor','metallicFactor','roughnessFactor'] if key in source_pbr}
   for key in ['baseColorTexture','metallicRoughnessTexture']:
    if key in source_pbr:pbr[key]=texture(image_for(source_pbr[key]))
   material={'name':original['name'],'pbrMetallicRoughness':pbr,'doubleSided':original.get('doubleSided',False)}
   if 'normalTexture' in original:material['normalTexture']=texture(image_for(original['normalTexture']))
   out['materials'].append(material);specular.append(None);continue
  diffuse=image_for(sg['diffuseTexture']) if 'diffuseTexture' in sg else Image.new('RGBA',(4,4),(255,255,255,255))
  spec=image_for(sg['specularGlossinessTexture']) if 'specularGlossinessTexture' in sg else Image.new('RGBA',(4,4),(255,255,255,255))
  normal=image_for(original['normalTexture']) if 'normalTexture' in original else Image.new('RGBA',(4,4),(128,128,255,255))
  data=np.asarray(spec,dtype=np.float32)/255
  # Keep authored diffuse and dielectric F0 in HDRP Specular Color mode. Gloss
  # lives in source alpha; it is not a constant roughness approximation.
  rough=1-data[:,:,3]*sg.get('glossinessFactor',1);mr=np.ones_like(data);mr[:,:,1]=rough;mr[:,:,2]=0
  pbr={'metallicFactor':0,'roughnessFactor':1,'baseColorFactor':sg.get('diffuseFactor',[1,1,1,1]),'baseColorTexture':texture(diffuse),'metallicRoughnessTexture':texture(Image.fromarray(np.uint8(mr*255+.5)))}
  # Source normal DDS follows its glTF RGB normal binding; no packed-channel
  # reinterpretation is inferred from a file suffix.
  material={'name':original['name'],'pbrMetallicRoughness':pbr,'normalTexture':texture(normal),'doubleSided':original.get('doubleSided',False)}
  linear=np.where(data[:,:,:3]<=.04045,data[:,:,:3]/12.92,((data[:,:,:3]+.055)/1.055)**2.4)*np.array(sg.get('specularFactor',[1,1,1]))
  srgb=np.where(linear<=.0031308,linear*12.92,1.055*np.maximum(linear,0)**(1/2.4)-.055);spec_image=np.ones_like(data);spec_image[:,:,:3]=srgb;spec_image[:,:,3]=1-rough;specular.append(Image.fromarray(np.uint8(np.clip(spec_image,0,1)*255+.5)))
  out['materials'].append(material)
 for material,pos,norm,uv,faces in entries:
  out['meshes'][0]['primitives'].append({'material':used.index(material),'attributes':{'POSITION':attribute(pos,'VEC3'),'NORMAL':attribute(norm,'VEC3'),'TEXCOORD_0':attribute(uv,'VEC2')},'indices':attribute(faces.reshape(-1),'SCALAR')})
 while len(blob)%4:blob.append(0)
 out['buffers']=[{'byteLength':len(blob)}];encoded=json.dumps(out,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
 glb=struct.pack('<III',0x46546c67,2,28+len(encoded)+len(blob))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(blob),0x004e4942)+blob
 return glb,specular,{'sourceRootNode':root,'sourceRootName':node_name,'sourceMaterialIndices':used,'originalTriangles':sum(len(e[4]) for e in entries),'sourceDimensionsMetres':(hi-lo).tolist(),'normalBinding':'original glTF RGB, flipped green only for Unity','nativeWorkflow':'HDRP Specular Color; authored diffuse, gloss alpha and F0 texture/factors'}

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--cache',type=Path,required=True);args=parser.parse_args();SOURCE.mkdir(exist_ok=True);(DEST/'Licenses').mkdir(parents=True,exist_ok=True)
 original=acquire(BASE+'bistro.gltf',args.cache);binary=acquire(BASE+'bistro.bin',args.cache);assert len(binary)==95660264 and hashlib.sha256(binary).hexdigest()=='5fb11a1d61aeb2537197aa97833ff950638c9d42451ee4fafbc9550f03918faa'
 (SOURCE/'bistro_source.gltf').write_bytes(original);doc=json.loads(original);reports=[]
 for name,node in MODELS.items():
  print('Extracting',name,flush=True);glb,specular,details=extract(doc,binary,node,args.cache);(SOURCE/(name+'_adapted.glb')).write_bytes(glb)
  report=convert('B_'+name,glb,'Bistro',DEST,False,'CC-BY-4.0',False,[(1,32,16),(2,16,8)])
  for i,(material,image) in enumerate(zip(report['materials'],specular)):
   resource=f'Bistro/Textures/B_{name}_m{i}_specular';image.save(ROOT/'Assets/PoolroomsProcedural/Resources'/(resource+'.png'));material['workflow']='specular';material['specular']=resource
  report.update(details,authors=['Amazon.com, Inc. or its affiliates'],modelLicense='CC-BY-4.0',sourceUrl=BASE+'bistro.gltf',upstreamRevision=REV,sourceSceneSha256=hashlib.sha256(original).hexdigest(),sourceBinarySha256=hashlib.sha256(binary).hexdigest(),nativeUnityValidated=False,adaptation='Selected original subtree, metric rebasing, DDS to bounded PNG, authored specular/gloss conversion, native OBJ and two LOD derivatives; not an untouched original download')
  (DEST/('B_'+name+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report);print(name,report['triangles'],report['maximum'],report['lods'],flush=True)
 for name,url in {'Bistro_CC_BY_4.txt':f'https://raw.githubusercontent.com/neftaly/bistro-gltf/{LICENSE_REV}/LICENSE','upstream_MIT.txt':BASE+'LICENSE','upstream_README.md':BASE+'README.md'}.items():
  raw=acquire(url,args.cache);(SOURCE/name).write_bytes(raw);(DEST/'Licenses'/name).write_bytes(raw)
 notice='Amazon Lumberyard Bistro v5.2, Amazon.com, Inc. or its affiliates, CC BY 4.0. Original: https://developer.nvidia.com/orca/amazon-lumberyard-bistro . Geometry/data derivative from zeux/niagara_bistro, revision '+REV+'. Further changes: three selected subtrees, scene placement removed, bounded PNG textures, authored HDRP specular/gloss workflow, OBJ and two LOD derivatives. No endorsement. Full CC BY text and derivative MIT notice included. These are three component models, not three downloaded complete buildings.\n'
 (DEST/'Licenses/NOTICE.txt').write_text(notice);(SOURCE/'NOTICE.txt').write_text(notice)
 (DEST/'catalog.json').write_text(json.dumps({'distinctGeometryFamilies':3,'completeBuildingModels':0,'nativeUnityValidated':False,'models':reports},indent=2)+'\n')
 (DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'},indent=2)+'\n');LOCK.write_text(json.dumps(locked,indent=2)+'\n')
if __name__=='__main__':main()
