"""Pinned licensed potted plant. Only source data is read; upstream scripts never run.
The presentation's insects, lights, paths and animation are excluded from adaptation.
"""
from pathlib import Path
import hashlib,json,struct,urllib.request
import numpy as np
from source_lods import cluster
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Garden';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/GardenSources'
REV='edc7c9e67c639d230715049ee31f9a96a6babbbe';BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{REV}/';MODEL='DiffuseTransmissionPlant'
LOCK=SOURCE/'sources.lock.json';locked=json.loads(LOCK.read_text()) if LOCK.exists() else {}

def acquire(name,url):
    path=SOURCE/name
    if not path.exists():
        path.parent.mkdir(parents=True,exist_ok=True);data=urllib.request.urlopen(url,timeout=60).read(64*1024*1024+1);assert len(data)<=64*1024*1024;path.write_bytes(data)
    data=path.read_bytes();record={'url':url,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    if name in locked:assert locked[name]==record,'Pinned source changed: '+name
    locked[name]=record;return data

def static_plant(raw):
    size=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+size])
    names=('pot','leaves','dirt');nodes=[dict(next(n for n in doc['nodes'] if n.get('name')==name)) for name in names]
    assert all('mesh' in n and not n.get('children') for n in nodes)
    meshes=[doc['meshes'][n['mesh']] for n in nodes]
    for i,n in enumerate(nodes):n['mesh']=i
    assert {p['material'] for m in meshes for p in m['primitives']}=={0,1}
    doc.update(nodes=nodes,meshes=meshes,scenes=[{'nodes':[0,1,2]}],scene=0,materials=doc['materials'][:2])
    for key in ('animations','extensions','extensionsRequired','extensionsUsed'):doc.pop(key,None)
    # KHR diffuse transmission has no exact automatic HDRP/Lit translation here.
    # Preserve core PBR, alpha and normals; record approximation instead of pretending SSS.
    doc['materials'][1].pop('extensions',None)
    doc['materials'][0]['emissiveFactor']=[0,0,0];doc['materials'][0].pop('emissiveTexture',None)
    # Soil is a small scanned surface inside the pot. Reduce only that dense
    # presentation mesh; retain authored pot/leaf geometry and all source maps.
    binary=bytearray(raw[28+size:])
    def read(index):
        a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']]
        dtype={5126:'<f4',5125:'<u4',5123:'<u2'}[a['componentType']];width={'SCALAR':1,'VEC2':2,'VEC3':3}[a['type']]
        return np.ndarray((a['count'],width),dtype=dtype,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',width*np.dtype(dtype).itemsize),np.dtype(dtype).itemsize)).copy()
    def append(array,kind,component):
        while len(binary)%4:binary.append(0)
        data=array.tobytes();view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':len(data)});binary.extend(data)
        i=len(doc['accessors']);doc['accessors'].append({'bufferView':view,'componentType':component,'count':len(array),'type':kind});return i
    primitive=doc['meshes'][2]['primitives'][0];attr=primitive['attributes'];pos=read(attr['POSITION']).astype(float);norm=read(attr['NORMAL']).astype(float);uv=read(attr['TEXCOORD_0']).astype(float);faces=read(primitive['indices']).reshape(-1,3)
    _,pos,norm,uv,faces=cluster([(0,pos,norm,uv,faces)],24,12,pos.min(0),pos.max(0),preserve_normals=True)[0]
    primitive['attributes']={'POSITION':append(pos.astype('<f4'),'VEC3',5126),'NORMAL':append(norm.astype('<f4'),'VEC3',5126),'TEXCOORD_0':append(uv.astype('<f4'),'VEC2',5126)}
    primitive['indices']=append(faces.astype('<u4').reshape(-1,1),'SCALAR',5125)
    doc['buffers'][0]['byteLength']=len(binary)
    data=json.dumps(doc,separators=(',',':')).encode();data+=b' '*((-len(data))%4);rest=struct.pack('<II',len(binary),0x004e4942)+binary
    return struct.pack('<III',0x46546c67,2,20+len(data)+len(rest))+struct.pack('<II',len(data),0x4e4f534a)+data+rest

def main():
    meta=acquire(MODEL+'_metadata.json',BASE+'Models/'+MODEL+'/metadata.json');metadata=json.loads(meta)
    assert {x['spdx'] for x in metadata['legal']}=={'CC0-1.0','CC-BY-4.0'}
    notice=acquire(MODEL+'_LICENSE.md',BASE+'Models/'+MODEL+'/LICENSE.md')
    licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True)
    for name in ('CC0-1.0.txt','CC-BY-4.0.txt'):(licenses/name).write_bytes(acquire(name,BASE+'LICENSES/'+name))
    raw=acquire(MODEL+'.glb',BASE+'Models/'+MODEL+'/glTF-Binary/'+MODEL+'.glb');prepared=static_plant(raw)
    (SOURCE/'PottedPlant_static.glb').write_bytes(prepared)
    report=convert('G_PottedPlant',prepared,'GardenSources',DEST,True,'CC0-1.0 + CC-BY-4.0',False,[(1,64,32),(2,28,16)])
    report['soilReducedOnly']=True
    report['originalPlantTriangles']=68273
    report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),sourceUrl=locked[MODEL+'.glb']['url'],preparedSourceSha256=hashlib.sha256(prepared).hexdigest(),upstreamRevision=REV,modelLicense='CC0-1.0 + CC-BY-4.0',authors=[x['artist'] for x in metadata['legal']],nativeUnityValidated=False,sourceNodeNames=['pot','leaves','dirt'],removedPresentation=['insects','lights','animation','paths'],diffuseTransmissionReproduced=False,adaptation='Only static pot, leaves and dirt. Native OBJ/LOD derivatives with original UV/normal/core PBR and alpha-tested leaves. Diffuse-transmission extension not reproduced; firefly emission disabled. Original presentation remains solely in Sources for provenance, not in runtime resources.')
    (DEST/'G_PottedPlant.json').write_text(json.dumps(report,indent=2)+'\n')
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':1,'lodsCountAsModels':False,'models':[report],'nativeUnityValidated':False},indent=2)+'\n')
    (licenses/(MODEL+'_metadata.json')).write_bytes(meta);(licenses/(MODEL+'_LICENSE.md')).write_bytes(notice)
    (licenses/'NOTICE.txt').write_text('Potted plant: original asset by Rico Cilliers (2021), CC0 1.0; materials by Eric Chadwick / Darmstadt Graphics Group GmbH (2024), CC BY 4.0.\nSource: KhronosGroup/glTF-Sample-Assets, revision '+REV+'. Full licenses and upstream notice included.\nChanges: static pot/leaves/dirt only, native OBJ and two LOD derivatives, original UV/normal/PBR conversion, alpha-cutout support, disabled presentation emission. No insects, animation or presentation lights imported. Optional glTF diffuse transmission not reproduced in HDRP. No endorsement implied.\n')
    files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json')
    (DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n')
    LOCK.write_text(json.dumps(locked,indent=2)+'\n');print(json.dumps({k:report[k] for k in ('triangles','lods','minimum','maximum','materials')}))
if __name__=='__main__':main()
