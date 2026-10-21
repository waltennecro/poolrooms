"""Import 43 attributed surfaces actually bound to the twenty Journey programmes.

Native source resolution capped at 4K, never upscaled. Cache originals outside the
package; only runtime maps, provenance and licences enter Assets. Existing maps
are referenced where identical, rather than copied to increase package size.
"""
from pathlib import Path
from io import BytesIO
import concurrent.futures, hashlib, json, urllib.request
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/JourneyFinishes'
RES=DEST.parent
CACHE=Path('/tmp/journey-texture-cache')
REV='2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e'
BASE=f'https://raw.githubusercontent.com/zeux/niagara_bistro/{REV}/'
INDICES=[5,6,16,23,24,32,34,39,59,61,62,65,66,67,80,122,194,195,196,203,
         9,12,30,36,38,56,57,58,74,81,84,85,86,87,193,197,198,200,204,205,46,73,246]

def main():
    (DEST/'Textures').mkdir(parents=True,exist_ok=True)
    (DEST/'Licenses').mkdir(exist_ok=True)
    CACHE.mkdir(parents=True,exist_ok=True)
    document=ROOT/'Sources/Bistro/bistro_source.gltf'
    doc=json.loads(document.read_text())
    def uri(info):
        t=doc['textures'][info['index']]
        return doc['images'][t.get('extensions',{}).get('MSFT_texture_dds',{}).get('source',t['source'])]['uri']
    uris=set()
    for index in INDICES:
        m=doc['materials'][index];sg=m['extensions']['KHR_materials_pbrSpecularGlossiness']
        for info in [m.get('normalTexture'),sg.get('diffuseTexture'),sg.get('specularGlossinessTexture')]:
            if info:uris.add(uri(info))
    sources={}
    def acquire(u):
        # Local exact upstream originals already used by another importer are safe
        # cache inputs, but are never copied into this playable bank.
        prior=ROOT/'Sources/MallFinishes'/u
        p=prior if prior.exists() else CACHE/u
        p.parent.mkdir(parents=True,exist_ok=True)
        if not p.exists():
            with urllib.request.urlopen(BASE+u,timeout=120) as response:
                data=response.read(100*1024*1024)
            assert data and len(data)<100*1024*1024,u
            p.write_bytes(data)
        data=p.read_bytes()
        return u,p,{'url':BASE+u,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    locations={}
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as executor:
        for u,p,record in executor.map(acquire,sorted(uris)):
            locations[u]=p;sources[u]=record
    lock=DEST/'sources.lock.json'
    if lock.exists():assert json.loads(lock.read_text())==sources,'Pinned input bytes changed'
    lock.write_text(json.dumps(sources,indent=2)+'\n')
    saved={}
    def original(info):
        im=Image.open(locations[uri(info)]).convert('RGBA')
        if max(im.size)>4096:im.thumbnail((4096,4096),Image.Resampling.LANCZOS)
        return im
    def image(info,kind):
        u=uri(info);key=(u,kind)
        if key in saved:return saved[key]
        stem=hashlib.sha256((u+kind).encode()).hexdigest()[:16]+'_'+kind
        existing=RES/'MallFinishes/Textures'/(stem+'.png')
        if existing.exists():result='MallFinishes/Textures/'+stem
        else:
            im=original(info);a=np.array(im)
            if kind=='normal':a[:,:,1]=255-a[:,:,1]
            Image.fromarray(a).save(DEST/'Textures'/(stem+'.png'))
            result='JourneyFinishes/Textures/'+stem
        saved[key]=result;return result
    definitions=[]
    for slot,index in enumerate(INDICES):
        m=doc['materials'][index];sg=m['extensions']['KHR_materials_pbrSpecularGlossiness']
        d={'name':f'JourneyFinish_{slot:02d}','workflow':'specular','colour':sg.get('diffuseFactor',[1,1,1,1]),
           'sourceMaterial':index,'sourceName':m['name'],'roughness':1-sg.get('glossinessFactor',1),
           'normalScale':m.get('normalTexture',{}).get('scale',1),'emission':[0,0,0]}
        if sg.get('diffuseTexture'):d['albedo']=image(sg['diffuseTexture'],'albedo')
        if m.get('normalTexture'):d['normal']=image(m['normalTexture'],'normal')
        if sg.get('specularGlossinessTexture'):a=np.asarray(original(sg['specularGlossinessTexture']),dtype=np.float32)/255
        else:
            im=original(sg['diffuseTexture']);a=np.ones((im.height,im.width,4),dtype=np.float32)
        factor=sg.get('specularFactor',[1,1,1]);gloss=sg.get('glossinessFactor',1)
        linear=np.where(a[:,:,:3]<=.04045,a[:,:,:3]/12.92,((a[:,:,:3]+.055)/1.055)**2.4)*factor
        a[:,:,:3]=np.where(linear<=.0031308,linear*12.92,1.055*np.maximum(linear,0)**(1/2.4)-.055)
        a[:,:,3]*=gloss
        stem=f'Finish_{slot:02d}'
        Image.fromarray(np.uint8(np.clip(a,0,1)*255+.5)).save(DEST/'Textures'/(stem+'_specular.png'))
        d['specular']='JourneyFinishes/Textures/'+stem+'_specular'
        mask=np.ones(a.shape,dtype=np.uint8)*255;mask[:,:,0]=0;mask[:,:,2]=0;mask[:,:,3]=np.uint8(np.clip(a[:,:,3],0,1)*255+.5)
        Image.fromarray(mask).save(DEST/'Textures'/(stem+'_mask.png'))
        d['mask']='JourneyFinishes/Textures/'+stem+'_mask'
        definitions.append(d)
        print(slot,m['name'],a.shape[:2],flush=True)
    (DEST/'palette.json').write_text(json.dumps({'materials':definitions},indent=2)+'\n')
    (DEST/'catalog.json').write_text(json.dumps({'finishes':len(definitions),'source':'Amazon Lumberyard Bistro','upstreamRevision':REV,
        'sourceDocumentSha256':hashlib.sha256(document.read_bytes()).hexdigest(),'licence':'CC-BY-4.0',
        'sourceModelsAdded':0,'maxTextureResolution':4096,'upscaled':False,'nativeUnityValidated':False,
        'materials':definitions},indent=2)+'\n')
    for name in ['Bistro_CC_BY_4.txt','upstream_MIT.txt']:(DEST/'Licenses'/name).write_bytes((ROOT/'Sources/Bistro'/name).read_bytes())
    (DEST/'Licenses/NOTICE.txt').write_text('Journey surface bank: 43 authored materials derived from Amazon Lumberyard Bistro, Amazon.com, Inc. or its affiliates, CC BY 4.0.\nSource: https://developer.nvidia.com/orca/amazon-lumberyard-bistro\nPinned derivative: https://github.com/zeux/niagara_bistro/tree/'+REV+'\nAdaptations: decode DDS to PNG; invert tangent normal green for Unity; retain specular workflow; bake source factors; pack HDRP mask. Native resolution capped at 4K, never upscaled. No original presentation lights or emissive signs are imported. Sources and hashes in sources.lock.json; map bindings in palette.json.\n')
    hashes={str(p.relative_to(DEST)):{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
            for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'}
    (DEST/'files.sha256.json').write_text(json.dumps(hashes,indent=2)+'\n')
    print('PLAYABLE_BANK_BYTES',sum(i['bytes'] for i in hashes.values()),flush=True)

if __name__=='__main__':main()
