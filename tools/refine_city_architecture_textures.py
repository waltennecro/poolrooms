"""Keep native Bistro texture resolution (up to 4K) on the new building skin.

The canonical adapted GLB is an authoring geometry input, not a resolution limit
on shipped runtime materials. Original DDS inputs are pinned and not upscaled.
"""
from pathlib import Path
from io import BytesIO
import argparse,hashlib,json
import numpy as np
from PIL import Image
import import_bistro_sources as bistro
ROOT=Path(__file__).resolve().parents[1];DEST=ROOT/'Assets/PoolroomsProcedural/Resources/CityArchitectureSources';SOURCE=ROOT/'Sources/CityArchitecture'
def main(cache):
 doc=json.loads((ROOT/'Sources/Bistro/bistro_source.gltf').read_text());p=DEST/'C_BuildingArcade.json';report=json.loads(p.read_text());bindings=[]
 bistro.locked.update(json.loads((SOURCE/'sources.lock.json').read_text()))
 def original(info):
  t=doc['textures'][info['index']];u=doc['images'][t.get('extensions',{}).get('MSFT_texture_dds',{}).get('source',t['source'])]['uri'];raw=bistro.acquire(bistro.BASE+u,cache)
  im=Image.open(BytesIO(raw)).convert('RGBA');native=im.size
  if max(im.size)>4096:im.thumbnail((4096,4096),Image.Resampling.LANCZOS)
  return im,{'url':bistro.BASE+u,'sha256':hashlib.sha256(raw).hexdigest(),'originalSize':native,'emittedSize':im.size}
 for index,m in enumerate(report['materials']):
  src=doc['materials'][report['sourceMaterialIndices'][index]];sg=src.get('extensions',{}).get('KHR_materials_pbrSpecularGlossiness',{})
  record={'material':m['name'],'sourceMaterial':report['sourceMaterialIndices'][index],'maps':{}}
  for key,info in [('albedo',sg.get('diffuseTexture')),('normal',src.get('normalTexture'))]:
   resource=m.get(key,'')
   if not resource.startswith('CityArchitectureSources/') or info is None:continue
   im,proof=original(info);a=np.array(im)
   if key=='normal':a[:,:,1]=255-a[:,:,1]
   Image.fromarray(a).save(DEST.parent/(resource+'.png'));record['maps'][key]=dict(proof,resource=resource,normalGreenInverted=key=='normal')
  if m.get('specular','').startswith('CityArchitectureSources/') and sg.get('specularGlossinessTexture'):
   im,proof=original(sg['specularGlossinessTexture']);a=np.asarray(im,dtype=np.float32)/255
   factor=sg.get('specularFactor',[1,1,1]);linear=np.where(a[:,:,:3]<=.04045,a[:,:,:3]/12.92,((a[:,:,:3]+.055)/1.055)**2.4)*factor
   a[:,:,:3]=np.where(linear<=.0031308,linear*12.92,1.055*np.maximum(linear,0)**(1/2.4)-.055);a[:,:,3]*=sg.get('glossinessFactor',1)
   Image.fromarray(np.uint8(np.clip(a,0,1)*255+.5)).save(DEST.parent/(m['specular']+'.png'))
   mask=np.ones(a.shape,dtype=np.uint8)*255;mask[:,:,0]=0;mask[:,:,2]=0;mask[:,:,3]=np.uint8(np.clip(a[:,:,3],0,1)*255+.5)
   Image.fromarray(mask).save(DEST.parent/(m['mask']+'.png'))
   record['maps']['specular']=dict(proof,resource=m['specular']);record['maps']['mask']=dict(proof,resource=m['mask'])
  if record['maps']:bindings.append(record)
 report['nativeTextureResolutionPreserved']=True;p.write_text(json.dumps(report,indent=2)+'\n')
 cat=json.loads((DEST/'catalog.json').read_text());cat['models']=[report];(DEST/'catalog.json').write_text(json.dumps(cat,indent=2)+'\n')
 (DEST/'native_texture_bindings.json').write_text(json.dumps({'upscaled':False,'maximum':4096,'materials':bindings},indent=2)+'\n')
 (SOURCE/'sources.lock.json').write_text(json.dumps(bistro.locked,indent=2)+'\n')
 (DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'},indent=2)+'\n')
 print('Native source maps preserved:',len(bindings),'materials;',sum(len(m['maps']) for m in bindings),'maps; no upscaling')
if __name__=='__main__':
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--cache',type=Path,required=True);main(parser.parse_args().cache)
