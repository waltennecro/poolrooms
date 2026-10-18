"""Acquire an authored DGG lamp and CC0 wicker PBR from pinned online glTF sources."""
from pathlib import Path
from io import BytesIO
import hashlib,json,struct,urllib.request
import numpy as np
from PIL import Image
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Baths';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/BathSources';TEXTURES=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures'
REV='edc7c9e67c639d230715049ee31f9a96a6babbbe';BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{REV}/'
LOCK=SOURCE/'sources.lock.json';locked=json.loads(LOCK.read_text()) if LOCK.exists() else {}
def acquire(name,url):
    p=SOURCE/name
    if not p.exists():p.parent.mkdir(parents=True,exist_ok=True);data=urllib.request.urlopen(url,timeout=60).read(64*1024*1024+1);assert len(data)<=64*1024*1024;p.write_bytes(data)
    data=p.read_bytes();r={'url':url,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    if name in locked:assert r==locked[name]
    locked[name]=r;return data

def main():
    licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True)
    originals={}
    for name,license in [('LightsPunctualLamp','CC-BY-4.0'),('ClearcoatWicker','CC0-1.0')]:
        meta=acquire(name+'_metadata.json',BASE+'Models/'+name+'/metadata.json');assert {x['spdx'] for x in json.loads(meta)['legal']}=={license}
        (licenses/(name+'_metadata.json')).write_bytes(meta);(licenses/(name+'_LICENSE.md')).write_bytes(acquire(name+'_LICENSE.md',BASE+'Models/'+name+'/LICENSE.md'))
        (licenses/(license+'.txt')).write_bytes(acquire(license+'.txt',BASE+'LICENSES/'+license+'.txt'))
        originals[name]=acquire(name+'.glb',BASE+'Models/'+name+'/glTF-Binary/'+name+'.glb')
    raw=originals['LightsPunctualLamp'];size=struct.unpack_from('<I',raw,12)[0];d=json.loads(raw[20:20+size])
    assert not d.get('animations') and not d.get('skins')
    d['nodes']=[n for n in d['nodes'] if 'mesh' in n];assert len(d['nodes'])==3 and all(not n.get('children') for n in d['nodes'])
    d['scenes']=[{'nodes':list(range(3))}];d['scene']=0
    for key in ('extensions','extensionsUsed','extensionsRequired'):d.pop(key,None)
    used=sorted({k for m in d['materials'] for k in m.get('extensions',{})})
    if used:d['extensionsUsed']=used
    for m in d['materials']:m['emissiveFactor']=[0,0,0];m.pop('emissiveTexture',None)
    encoded=json.dumps(d,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);rest=raw[20+size:]
    prepared=struct.pack('<III',0x46546c67,2,20+len(encoded)+len(rest))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+rest
    (SOURCE/'Lamp_static.glb').write_bytes(prepared)
    report=convert('T_ArtNouveauLamp',prepared,'BathSources',DEST,False,'CC-BY-4.0',False,[(1,40,24),(2,20,12)])
    report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),preparedSourceSha256=hashlib.sha256(prepared).hexdigest(),sourceUrl=locked['LightsPunctualLamp.glb']['url'],upstreamRevision=REV,modelLicense='CC-BY-4.0',authors=['DGG'],nativeUnityValidated=False,adaptation='Static decorative unlit lamp, only the three original meshes; demonstration punctual lights and emission removed. Native OBJ and LOD derivatives. Core PBR retained; transmission is an approximate transparent HDRP material, not an exact glTF glass reproduction.')
    (DEST/'T_ArtNouveauLamp.json').write_text(json.dumps(report,indent=2)+'\n')
    raw=originals['ClearcoatWicker'];n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n]);binary=raw[28+n:];mat=doc['materials'][0];pbr=mat['pbrMetallicRoughness']
    def image(texture):
        t=doc['images'][doc['textures'][texture['index']]['source']];v=doc['bufferViews'][t['bufferView']];start=v.get('byteOffset',0)
        return np.array(Image.open(BytesIO(binary[start:start+v['byteLength']])).convert('RGBA'))
    albedo=image(pbr['baseColorTexture']);normal=image(mat['normalTexture']);normal[:,:,1]=255-normal[:,:,1];mr=image(pbr['metallicRoughnessTexture']);mask=np.zeros_like(mr);mask[:,:,0]=mr[:,:,2];mask[:,:,1]=mr[:,:,0];mask[:,:,2]=128;mask[:,:,3]=255-mr[:,:,1]
    maps={}
    for suffix,data in [('Albedo',albedo),('Normal',normal),('Mask',mask)]:
        p=TEXTURES/('BathWicker_'+suffix+'.png');Image.fromarray(data).save(p,compress_level=9);b=p.read_bytes();maps[p.relative_to(ROOT).as_posix()]={'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest()}
    material={'sourceUrl':locked['ClearcoatWicker.glb']['url'],'sourceSha256':hashlib.sha256(originals['ClearcoatWicker']).hexdigest(),'authors':['Eric Chadwick'],'license':'CC0-1.0','role':'PBR material adapted onto original procedural loungers; source demonstration sphere not instantiated','maps':maps,'clearcoatNormalReproduced':False,'nativeUnityValidated':False}
    (DEST/'catalog.json').write_text(json.dumps({'distinctNewModels':1,'licensedMaterialSets':1,'lodsCountAsModels':False,'models':[report],'wicker':material,'nativeUnityValidated':False},indent=2)+'\n')
    (licenses/'NOTICE.txt').write_text('Art-nouveau lamp: DGG, 2021, CC BY 4.0. https://creativecommons.org/licenses/by/4.0/\nWicker material: Eric Chadwick, 2023, CC0 1.0. https://creativecommons.org/publicdomain/zero/1.0/\nSources: Khronos glTF Sample Assets, revision '+REV+'. Full upstream notices, metadata and licences included.\nChanges: lamp OBJ/two LOD derivatives and original core PBR; original presentation lights/emission removed, glass transmission approximated. Wicker base colour, normals and AO/metal/roughness adapted to native HDRP maps on original lounger geometry; clearcoat normal omitted. Wicker demonstration sphere not placed in game. No endorsement implied.\n')
    files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json');(DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n')
    LOCK.write_text(json.dumps(locked,indent=2)+'\n');print(json.dumps({'lampTriangles':[report['triangles']]+[x['triangles'] for x in report['lods']],'bounds':[report['minimum'],report['maximum']],'wickerMaps':len(maps)}))
if __name__=='__main__':main()
