"""Adapt pinned CC BY 4 Bistro finishes at their native resolution, without upscaling.

Only data is downloaded. Identical source maps are shared instead of inflating the
package with recolours. Original specular/gloss workflow and RGB normals retained.
"""
from pathlib import Path
from io import BytesIO
import concurrent.futures, hashlib, json, urllib.request
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/MallFinishes'
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/MallFinishes'
REV='2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e'
BASE=f'https://raw.githubusercontent.com/zeux/niagara_bistro/{REV}/'
# Distinct authored finishes, not extra models or copied recolours.
FINISHES=[('Plaster',4),('Stone',11),('Limestone',25),('FlutedStone',21),
 ('WhiteBrick',22),('WarmPlaster',29),('Oak',46),('PolishedWood',237),
 ('WoodPlanks',238),('PaintedWood',240),('BrushedMetal',242),('Bronze',79),
 ('Chrome',73),('HexTile',244),('Linen',18),('Curtain',247),('Plastic',27),
 ('Books',83),('BarTrim',248),('Metal',26)]

def main():
    SOURCE.mkdir(parents=True,exist_ok=True);(DEST/'Licenses').mkdir(parents=True,exist_ok=True)
    textures=DEST/'Textures';textures.mkdir(exist_ok=True)
    doc=json.loads((ROOT/'Sources/Bistro/bistro_source.gltf').read_text())
    lockfile=SOURCE/'sources.lock.json';locked=json.loads(lockfile.read_text()) if lockfile.exists() else {}
    uris=set()
    def uri(info):
        t=doc['textures'][info['index']]
        return doc['images'][t.get('extensions',{}).get('MSFT_texture_dds',{}).get('source',t['source'])]['uri']
    for _,index in FINISHES:
        m=doc['materials'][index];sg=m.get('extensions',{}).get('KHR_materials_pbrSpecularGlossiness',{})
        for info in [m.get('normalTexture'),sg.get('diffuseTexture'),sg.get('specularGlossinessTexture')]:
            if info:uris.add(uri(info))
    def acquire(u):
        path=SOURCE/u;path.parent.mkdir(parents=True,exist_ok=True)
        if not path.exists():
            with urllib.request.urlopen(BASE+u,timeout=90) as r,path.with_suffix('.partial').open('wb') as f:
                while True:
                    b=r.read(1024*1024)
                    if not b:break
                    f.write(b)
                    if f.tell()>96*1024*1024:raise ValueError('Source map exceeds budget')
            path.with_suffix('.partial').replace(path)
        data=path.read_bytes();record={'url':BASE+u,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
        if u in locked:assert locked[u]==record,'Pinned source changed: '+u
        locked[u]=record;return u
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as ex:list(ex.map(acquire,sorted(uris)))
    saved={};definitions=[];references=[]
    def image(info,kind):
        u=uri(info);key=(u,kind)
        if key in saved:return saved[key]
        im=Image.open(SOURCE/u).convert('RGBA');assert max(im.size)<=8192
        # Native size retained; importer uses mip streaming, not eager 8K residency.
        a=np.array(im)
        if kind=='normal':a[:,:,1]=255-a[:,:,1]
        stem=hashlib.sha256((u+kind).encode()).hexdigest()[:16]+'_'+kind
        Image.fromarray(a).save(textures/(stem+'.png'))
        saved[key]='MallFinishes/Textures/'+stem;return saved[key]
    for name,index in FINISHES:
        m=doc['materials'][index];sg=m['extensions']['KHR_materials_pbrSpecularGlossiness']
        definition={'name':'MallFinish_'+name,'workflow':'specular','colour':sg.get('diffuseFactor',[1,1,1,1]),
          'metallic':0,'roughness':1-sg.get('glossinessFactor',1),'normalScale':m.get('normalTexture',{}).get('scale',1),
          'sourceMaterial':index,'sourceName':m['name'],'emission':[0,0,0]}
        if sg.get('diffuseTexture'):definition['albedo']=image(sg['diffuseTexture'],'albedo')
        if m.get('normalTexture'):definition['normal']=image(m['normalTexture'],'normal')
        # Specular RGB is authored in sRGB; alpha stores source gloss. Factors are
        # baked once. Runtime must use this alpha instead of a constant gloss.
        factor=sg.get('specularFactor',[1,1,1]);gloss=sg.get('glossinessFactor',1)
        if sg.get('specularGlossinessTexture'):
            im=Image.open(SOURCE/uri(sg['specularGlossinessTexture'])).convert('RGBA');a=np.asarray(im,dtype=np.float32)/255
        else:
            im=Image.open(ROOT/'Assets/PoolroomsProcedural/Resources'/(definition['albedo']+'.png'));a=np.ones((im.height,im.width,4),dtype=np.float32)
        linear=np.where(a[:,:,:3]<=.04045,a[:,:,:3]/12.92,((a[:,:,:3]+.055)/1.055)**2.4)*factor
        rgb=np.where(linear<=.0031308,linear*12.92,1.055*np.maximum(linear,0)**(1/2.4)-.055)
        a[:,:,:3]=rgb;a[:,:,3]*=gloss
        Image.fromarray(np.uint8(np.clip(a,0,1)*255+.5)).save(textures/(name+'_specular.png'))
        definition['specular']='MallFinishes/Textures/'+name+'_specular'
        mask=np.ones(a.shape,dtype=np.uint8)*255;mask[:,:,0]=0;mask[:,:,2]=0;mask[:,:,3]=np.uint8(np.clip(a[:,:,3],0,1)*255+.5)
        Image.fromarray(mask).save(textures/(name+'_mask.png'));definition['mask']='MallFinishes/Textures/'+name+'_mask'
        definitions.append(definition)
        ref=Image.open(ROOT/'Assets/PoolroomsProcedural/Resources'/(definition['albedo']+'.png')).convert('RGB');ref.thumbnail((768,768))
        folder=ROOT/'Documentation/References/Mall';folder.mkdir(parents=True,exist_ok=True);ref.save(folder/(name+'.jpg'),quality=91)
        references.append({'file':name+'.jpg','finish':name,'sourceMaterial':index,'license':'CC-BY-4.0','author':'Amazon.com, Inc. or its affiliates','derivedFrom':definition['albedo'],'description':'Source surface reference; not an in-game screenshot.'})
        print(name,im.size,flush=True)
    (DEST/'palette.json').write_text(json.dumps({'materials':definitions},indent=2)+'\n')
    (DEST/'catalog.json').write_text(json.dumps({'finishes':len(definitions),'sharedSourceMaps':len(saved),'upstreamRevision':REV,'sourceLicense':'CC-BY-4.0','nativeUnityValidated':False,'materials':definitions},indent=2)+'\n')
    for name in ['Bistro_CC_BY_4.txt','upstream_MIT.txt']:(DEST/'Licenses'/name).write_bytes((ROOT/'Sources/Bistro'/name).read_bytes())
    (DEST/'Licenses/NOTICE.txt').write_text('Mall finishes derived from Amazon Lumberyard Bistro v5.2, Amazon.com, Inc. or its affiliates, CC BY 4.0.\nOriginal: https://developer.nvidia.com/orca/amazon-lumberyard-bistro . Pinned derivative scene: https://github.com/zeux/niagara_bistro/tree/2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e .\nChanges: twenty selected authored finishes, DDS decoded to native-resolution PNG; green normal channel converted for Unity; source specular RGB and gloss alpha preserved with original factors, shared source maps deduplicated. No upscaling; no new model count implied.\nFull CC BY text and upstream MIT notice included. No endorsement implied. Reference thumbnails are surface references, not in-game captures.\n')
    lockfile.write_text(json.dumps(locked,indent=2)+'\n')
    (ROOT/'Documentation/References/Mall/finishes.json').write_text(json.dumps(references,indent=2)+'\n')
    manifest={p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'}
    (DEST/'files.sha256.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('FINISH_BYTES',sum(x['bytes'] for x in manifest.values()),flush=True)

if __name__=='__main__':main()
