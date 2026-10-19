"""Import three pinned licensed source models; never execute upstream code.
Original GLBs and notices are retained. Unsupported optical extensions are recorded,
not represented as exact reproduction. OBJ exports use the existing audited importer.
"""
from pathlib import Path
import hashlib,json,struct,urllib.request
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/Restful';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/RestfulSources'
REV='edc7c9e67c639d230715049ee31f9a96a6babbbe'
BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{REV}/'
MODELS=[('DiffuseTransmissionTeacup','R_Teacup','CC0-1.0'),('GlassVaseFlowers','R_FlowerVase','CC0-1.0'),('AnisotropyBarnLamp','R_BarnLamp','CC-BY-4.0')]

def main():
    SOURCE.mkdir(parents=True,exist_ok=True);licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True)
    lockfile=SOURCE/'sources.lock.json';locked=json.loads(lockfile.read_text()) if lockfile.exists() else {};models=[]
    def acquire(name,url):
        path=SOURCE/name
        if not path.exists():
            data=urllib.request.urlopen(url,timeout=60).read(64*1024*1024+1)
            assert 0<len(data)<=64*1024*1024
            path.write_bytes(data)
        data=path.read_bytes();record={'url':url,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
        if name in locked:assert locked[name]==record,'Pinned source changed: '+name
        locked[name]=record;return data
    for original,identifier,license in MODELS:
        prefix=BASE+'Models/'+original+'/'
        meta=acquire(original+'_metadata.json',prefix+'metadata.json');metadata=json.loads(meta)
        assert {entry['spdx'] for entry in metadata['legal']}=={license}
        (licenses/(original+'_metadata.json')).write_bytes(meta)
        (licenses/(original+'_LICENSE.md')).write_bytes(acquire(original+'_LICENSE.md',prefix+'LICENSE.md'))
        (licenses/(license+'.txt')).write_bytes(acquire(license+'.txt',BASE+'LICENSES/'+license+'.txt'))
        raw=acquire(original+'.glb',prefix+'glTF-Binary/'+original+'.glb')
        n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n]);assert not doc.get('skins') and not doc.get('animations')
        removed=[]
        for material in doc.get('materials',[]):
            extensions=material.get('extensions',{})
            for ext in ['KHR_materials_diffuse_transmission','KHR_materials_anisotropy']:
                if ext in extensions:removed.append(ext);extensions.pop(ext)
            material['emissiveFactor']=[0,0,0];material.pop('emissiveTexture',None)
        for key in ['extensionsUsed','extensionsRequired']:
            if key in doc:doc[key]=[e for e in doc[key] if e not in removed]
        # The lamp has untextured metal primitives without UVs. Supply a neutral
        # coordinate only for those primitives; textured missing UVs remain an error.
        binary=bytearray(raw[28+n:])
        for mesh in doc['meshes']:
            for primitive in mesh['primitives']:
                if 'TEXCOORD_0' in primitive['attributes']:continue
                material=doc['materials'][primitive['material']]
                assert not any('Texture' in key for key in material.get('pbrMetallicRoughness',{}))
                assert not any('Texture' in key for key in material)
                count=doc['accessors'][primitive['attributes']['POSITION']]['count']
                while len(binary)%4:binary.append(0)
                view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':count*8});binary.extend(bytes(count*8))
                index=len(doc['accessors']);doc['accessors'].append({'bufferView':view,'componentType':5126,'count':count,'type':'VEC2'})
                primitive['attributes']['TEXCOORD_0']=index
        doc['buffers'][0]['byteLength']=len(binary)
        encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);rest=struct.pack('<II',len(binary),0x004e4942)+binary
        prepared=struct.pack('<III',0x46546c67,2,20+len(encoded)+len(rest))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+rest
        report=convert(identifier,prepared,'RestfulSources',DEST,True,license,False,[(1,56,28),(2,24,16)])
        # Existing material bridge understands transparent core PBR. Preserve vase
        # transparency approximately; volume/transmission extensions are not exact.
        for mat in report['materials']:
            if 'KHR_materials_transmission' in mat.get('extensions',[]):
                mat['transparent']=True;mat['colour'][3]=.16
        report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),preparedSourceSha256=hashlib.sha256(prepared).hexdigest(),sourceUrl=locked[original+'.glb']['url'],upstreamRevision=REV,modelLicense=license,authors=[e['artist'] for e in metadata['legal']],nativeUnityValidated=False,unsupportedExtensionsApproximated=sorted(set(removed)),adaptation='Static OBJ and two LOD derivatives; core PBR maps. Anisotropy/diffuse transmission omitted; glass uses approximate alpha transparency. Presentation lights and emissive factor disabled. No exact subsurface/volume reproduction claimed.')
        (DEST/(identifier+'.json')).write_text(json.dumps(report,indent=2)+'\n');models.append(report)
        lockfile.write_text(json.dumps(locked,indent=2)+'\n')
        print(identifier,report['triangles'],[x['triangles'] for x in report['lods']],report['minimum'],report['maximum'],flush=True)
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':len(models),'lodsCountAsModels':False,'models':models,'nativeUnityValidated':False},indent=2)+'\n')
    (licenses/'NOTICE.txt').write_text('Teacup: Polyhaven.com and Eric Chadwick, 2023, CC0-1.0.\nGlass vase: Eric Chadwick; flowers: Rico Cilliers, 2023, CC0-1.0.\nBarn lamp: Eric Chadwick, copyright 2023 Wayfair, LLC, CC BY 4.0.\nKhronos glTF Sample Assets revision '+REV+'.\nFull upstream notices, metadata and license texts included. Changes: OBJ exports and two LOD derivatives, core PBR maps; diffuse transmission and anisotropy omitted, glass transparency approximate, original lights/emission not instantiated. No endorsement implied.\n')
    files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json')
    (DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n')
if __name__=='__main__':main()
