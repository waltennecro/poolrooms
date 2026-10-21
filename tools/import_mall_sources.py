"""Pinned licensed retail models, native OBJ/PBR adaptation and two LODs each.

Source animations/presentation lights are deliberately not imported. Glass,
iridescence and sheen use the documented core-PBR approximation, not fake claims
of exact physical transmission. Source UV transforms/AO are baked by the audited
adapter. Data downloads only; no upstream code is executed.
"""
from pathlib import Path
import concurrent.futures,hashlib,json,mimetypes,struct,urllib.parse,urllib.request
import numpy as np
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Mall'
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/MallSources'
REV='15cd02d304e54cb42a4db4eefe719f76190f4b68'
BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{REV}/'
MODELS=[('CommercialRefrigerator','Refrigerator'),('SpecularSilkPouf','Pouf'),
 ('MaterialsVariantsShoe','Shoe'),('StainedGlassLamp','StainedLamp'),
 ('IridescenceLamp','DeskLamp'),('MandarinOrange','Mandarin'),('Avocado','Avocado'),
 ('IridescentDishWithOlives','OliveDish')]
locked={}
def acquire(name,url):
    path=SOURCE/name;path.parent.mkdir(parents=True,exist_ok=True)
    if not path.exists():
        temp=path.with_suffix(path.suffix+'.partial')
        with urllib.request.urlopen(url,timeout=90) as r,temp.open('wb') as f:
            while True:
                b=r.read(1024*1024)
                if not b:break
                f.write(b)
                if f.tell()>98*1024*1024:raise ValueError('GitHub per-file source budget exceeded')
        temp.replace(path)
    data=path.read_bytes();record={'url':url,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    if name in locked:assert locked[name]==record,'Pinned source changed: '+name
    locked[name]=record;return data

def encode(doc,binary):
    while len(binary)%4:binary+=b'\0'
    doc['buffers']=[{'byteLength':len(binary)}]
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
    return struct.pack('<III',0x46546c67,2,28+len(encoded)+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary

def load(name):
    prefix=BASE+'Models/'+name+'/'
    if name not in ['StainedGlassLamp','MandarinOrange']:
        raw=acquire(name+'.glb',prefix+'glTF-Binary/'+name+'.glb');n=struct.unpack_from('<I',raw,12)[0]
        return raw,json.loads(raw[20:20+n]),bytearray(raw[28+n:])
    folder='glTF-JPG-PNG/' if name=='StainedGlassLamp' else 'glTF/'
    raw=acquire(name+'/'+name+'.gltf',prefix+folder+name+'.gltf');doc=json.loads(raw)
    assert len(doc['buffers'])==1
    binary=bytearray(acquire(name+'/'+doc['buffers'][0]['uri'],prefix+folder+doc['buffers'][0]['uri']))
    for image in doc.get('images',[]):
        uri=image.pop('uri');data=acquire(name+'/'+uri,prefix+folder+urllib.parse.quote(uri,safe='/'))
        while len(binary)%4:binary.append(0)
        view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':len(data)})
        binary.extend(data);image.update(bufferView=view,mimeType=mimetypes.guess_type(uri)[0])
    return encode(doc,binary),doc,binary

def main():
    global locked
    SOURCE.mkdir(parents=True,exist_ok=True);licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True)
    lockfile=SOURCE/'sources.lock.json'
    if lockfile.exists():locked=json.loads(lockfile.read_text()).get('files',{})
    reports=[];references=[]
    for name,short in MODELS:
        prefix=BASE+'Models/'+name+'/'
        meta=acquire(name+'_metadata.json',prefix+'metadata.json');metadata=json.loads(meta)
        license={e['spdx'] for e in metadata['legal']};assert len(license)==1 and license<={'CC0-1.0','CC-BY-4.0'};license=license.pop()
        for local,url in [(name+'_metadata.json',prefix+'metadata.json'),(name+'_LICENSE.md',prefix+'LICENSE.md'),(license+'.txt',BASE+'LICENSES/'+license+'.txt')]:
            (licenses/local).write_bytes(acquire(local,url))
        raw,doc,binary=load(name)
        removed=sorted(set(doc.get('extensionsUsed',[])) & {'KHR_materials_iridescence','KHR_materials_anisotropy','KHR_lights_punctual','KHR_materials_variants','KHR_animation_pointer'})
        doc.pop('animations',None)
        assert not doc.get('skins')
        for node in doc['nodes']:
            if 'extensions' in node:node['extensions'].pop('KHR_lights_punctual',None)
        for mesh in doc['meshes']:
            for primitive in mesh['primitives']:
                if 'extensions' in primitive:primitive['extensions'].pop('KHR_materials_variants',None)
                attributes=primitive['attributes']
                if 'TEXCOORD_0' not in attributes:
                    mat=doc['materials'][primitive['material']]
                    assert not any('Texture' in k for k in mat.get('pbrMetallicRoughness',{})) and not any('Texture' in k for k in mat)
                    count=doc['accessors'][attributes['POSITION']]['count']
                    while len(binary)%4:binary.append(0)
                    pos=doc['accessors'][attributes['POSITION']];pv=doc['bufferViews'][pos['bufferView']]
                    points=np.ndarray((count,3),dtype='<f4',buffer=binary,offset=pv.get('byteOffset',0)+pos.get('byteOffset',0),strides=(pv.get('byteStride',12),4)).copy()
                    axes=np.argsort(np.ptp(points,axis=0))[-2:];uv=points[:,axes];extent=np.ptp(uv,axis=0);assert np.all(extent>1e-8)
                    uv=np.asarray((uv-uv.min(axis=0))/extent,dtype='<f4')
                    view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':count*8});binary.extend(uv.tobytes())
                    attributes['TEXCOORD_0']=len(doc['accessors']);doc['accessors'].append({'bufferView':view,'componentType':5126,'count':count,'type':'VEC2'})
                if 'TEXCOORD_1' not in attributes:attributes['TEXCOORD_1']=attributes['TEXCOORD_0']
        for mat in doc['materials']:
            mat['emissiveFactor']=[0,0,0];mat.pop('emissiveTexture',None)
            for ext in removed:mat.get('extensions',{}).pop(ext,None)
        for key in ['extensionsUsed','extensionsRequired']:
            if key in doc:doc[key]=[e for e in doc[key] if e not in removed]
        for ext in removed:doc.get('extensions',{}).pop(ext,None)
        previous=DEST/('M_'+short+'.json')
        report=json.loads(previous.read_text()) if previous.exists() else None
        if report is None or report.get('sourceSha256')!=hashlib.sha256(raw).hexdigest():
            report=convert('M_'+short,encode(doc,binary),'MallSources',DEST,True,license,name in ['CommercialRefrigerator','SpecularSilkPouf','MaterialsVariantsShoe'],[(1,64,36),(2,28,18)],bake_size=2048,skip_uniform=True)
        for mat in report['materials']:
            if 'KHR_materials_transmission' in mat.get('extensions',[]):mat['transparent']=True;mat['colour'][3]=.13
        report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),upstreamRevision=REV,modelLicense=license,
          sourceName=name,authors=[e['artist'] for e in metadata['legal']],owners=[e['owner'] for e in metadata['legal']],
          sourceUrl=prefix,nativeUnityValidated=False,removedExtensions=removed,
          adaptation='Static native OBJ, two LODs, source UV/PBR baking. Default material variant. Presentation lights, emission and animation not imported. Transmission/iridescence/sheen approximate; original source notices retained.')
        (DEST/('M_'+short+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report)
        reference=acquire(name+'_reference'+Path(metadata['screenshot']).suffix,prefix+metadata['screenshot'])
        folder=ROOT/'Documentation/References/Mall';folder.mkdir(parents=True,exist_ok=True)
        file='Model_'+short+Path(metadata['screenshot']).suffix;(folder/file).write_bytes(reference)
        references.append({'file':file,'sourceUrl':prefix+metadata['screenshot'],'license':license,'authors':report['authors'],'description':'Upstream reference image; not a screenshot from this game.'})
        lockfile.write_text(json.dumps({'upstreamRevision':REV,'files':locked},indent=2)+'\n')
        print(short,report['triangles'],report['minimum'],report['maximum'],flush=True)
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':len(reports),'lodsCountAsModels':False,'models':reports,'nativeUnityValidated':False},indent=2)+'\n')
    (licenses/'NOTICE.txt').write_text('Licensed mall source adaptation. Each model has upstream metadata, named authors, ownership and license text alongside this notice.\nChanges: static OBJ export, UV/PBR baking, two LOD derivatives; no original presentation lights or animations, unsupported optical effects approximate. No endorsement implied.\n'+ '\n'.join(m['sourceName']+': '+', '.join(m['authors'])+'; '+m['modelLicense'] for m in reports)+'\n')
    (ROOT/'Documentation/References/Mall/models.json').write_text(json.dumps(references,indent=2)+'\n')
    manifest={p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json'}
    (DEST/'files.sha256.json').write_text(json.dumps(manifest,indent=2)+'\n')

if __name__=='__main__':main()
