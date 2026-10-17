"""Adapt the licensed Cesium aircraft while preserving the original and provenance."""
from pathlib import Path
import hashlib,json,struct,urllib.request
from import_city_sources import convert

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/Aircraft';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Aircraft'
REV='05adbc742db7c0aa9d53836ba46a5bbfdbb9fbc1'
BASE=f'https://raw.githubusercontent.com/CesiumGS/cesium/{REV}/'
LOCK=SOURCE/'sources.lock.json'
expected=json.loads(LOCK.read_text()) if LOCK.exists() else {}

def acquire(name,url):
    path=SOURCE/name
    if not path.exists():
        data=urllib.request.urlopen(url,timeout=40).read(16*1024*1024+1);assert len(data)<=16*1024*1024
        path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
    data=path.read_bytes();record={'url':url,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    if name in expected:assert expected[name]==record,'Pinned aircraft source changed'
    expected[name]=record;return data


def main():
    raw=acquire('Cesium_Air.glb',BASE+'Apps/SampleData/models/CesiumAir/Cesium_Air.glb')
    license_data=acquire('Cesium_LICENSE.md',BASE+'LICENSE.md')
    assert b'Apache License' in license_data and b'Version 2.0' in license_data
    magic,version,length=struct.unpack_from('<III',raw);assert magic==0x46546c67 and version==2 and length==len(raw)
    size,tag=struct.unpack_from('<II',raw,12);assert tag==0x4e4f534a
    doc=json.loads(raw[20:20+size]);assert not doc.get('skins')
    animated=[]
    for animation in doc.get('animations',[]):
        for channel in animation['channels']:
            node=doc['nodes'][channel['target']['node']].get('name','')
            assert node.startswith('Prop') and channel['target']['path']=='rotation','Unexpected aircraft animation; do not silently freeze it'
            animated.append(node)
    assert len(set(animated))==2
    doc.pop('animations',None)
    encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
    # Retain the original BIN chunk verbatim. Only the two propeller rotation clips
    # are replaced by engine-driven native pivots; no skin/deformation is discarded.
    rest=raw[20+size:]
    frozen=struct.pack('<III',magic,version,20+len(encoded)+len(rest))+struct.pack('<II',len(encoded),tag)+encoded+rest
    report=convert('A_CesiumAir',frozen,'Aircraft',DEST,True,'Apache-2.0')
    report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),upstreamRevision=REV,sourceUrl=expected['Cesium_Air.glb']['url'],modelLicense='Apache-2.0',authors=['CesiumJS Contributors'],animatedPropellerNodes=sorted(set(animated)),adaptation='Native OBJ/HDRP PBR; two propeller rotation tracks replaced with native RPM pivots; original GLB retained')
    (DEST/'A_CesiumAir.json').write_text(json.dumps(report,indent=2)+'\n')
    licenses=DEST/'Licenses';licenses.mkdir(exist_ok=True)
    (licenses/'Cesium_LICENSE.md').write_bytes(license_data)
    (licenses/'NOTICE.txt').write_text(f'Aircraft derived from CesiumJS, Copyright 2011–2026 CesiumJS Contributors.\nApache License 2.0; full source license included.\nSource: {report["sourceUrl"]}\nChanges: native OBJ conversion, HDRP metal/roughness mask, normalized placement and two propeller pivots.\n')
    (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':1,'nativeUnityValidated':False,'models':[report]},indent=2)+'\n')
    files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json')
    (DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n')
    LOCK.write_text(json.dumps(expected,indent=2)+'\n');print(json.dumps({'triangles':report['triangles'],'minimum':report['minimum'],'maximum':report['maximum'],'sourceLicense':report['modelLicense'],'propellers':animated}))

if __name__=='__main__':main()
