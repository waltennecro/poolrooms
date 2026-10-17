"""Download and audit the selected source GLBs; does not import or modify Unity projects."""
from pathlib import Path
import base64
import hashlib
import io
import json
import struct
import urllib.request
from PIL import Image

ROOT=Path(__file__).resolve().parent
BASE='https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/main/Models/'
SELECTED=('SheenChair','Lantern','WaterBottle')
LIMIT=32*1024*1024

def fetch(url):
    with urllib.request.urlopen(url,timeout=45) as response:
        raw=response.read(LIMIT+1)
    if len(raw)>LIMIT:
        raise ValueError('Asset exceeds the per-file download budget')
    return raw

def audit(raw):
    magic,version,length=struct.unpack_from('<4sII',raw)
    assert magic==b'glTF' and version==2 and length==len(raw)
    chunks={};offset=12
    while offset<len(raw):
        size,kind=struct.unpack_from('<II',raw,offset);offset+=8
        assert offset+size<=len(raw) and kind not in chunks
        chunks[kind]=raw[offset:offset+size];offset+=size
    assert offset==len(raw)
    doc=json.loads(chunks[0x4E4F534A]);binary=chunks[0x004E4942]
    assert doc['asset']['version']=='2.0'
    for buffer in doc['buffers']:
        assert 'uri' not in buffer and buffer['byteLength']<=len(binary)
    views=doc.get('bufferViews',[])
    for view in views:
        assert view['buffer']==0 and view.get('byteOffset',0)+view['byteLength']<=len(binary)
    image_info=[]
    for image in doc.get('images',[]):
        if 'bufferView' in image:
            v=views[image['bufferView']];start=v.get('byteOffset',0);payload=binary[start:start+v['byteLength']]
        else:
            assert image['uri'].startswith('data:')
            payload=base64.b64decode(image['uri'].split(',',1)[1],validate=True)
        with Image.open(io.BytesIO(payload)) as im:
            image_info.append({'width':im.width,'height':im.height,'format':im.format})
            im.verify()
    triangles=0;vertices=0;primitives=0
    for mesh in doc.get('meshes',[]):
        for p in mesh['primitives']:
            assert p.get('mode',4)==4
            position=doc['accessors'][p['attributes']['POSITION']]
            assert position['type']=='VEC3' and position['componentType']==5126
            vertices+=position['count'];primitives+=1
            index=doc['accessors'][p['indices']] if 'indices' in p else position
            assert index['count']%3==0
            triangles+=index['count']//3
            for a in position.get('min',[])+position.get('max',[]):
                assert abs(a)<1e6
    return {'sourceVertices':vertices,'sourceTriangles':triangles,'primitives':primitives,
            'materials':len(doc.get('materials',[])),'images':image_info,
            'extensionsRequired':doc.get('extensionsRequired',[]),
            'extensionsUsed':doc.get('extensionsUsed',[]),'nativeUnityImportValidated':False}

def main():
    assets=ROOT/'Modelos_origen';licenses=ROOT/'Licencias'
    assets.mkdir(exist_ok=True);licenses.mkdir(exist_ok=True)
    previous_path=ROOT/'recursos_verificados.json'
    previous=json.loads(previous_path.read_text()) if previous_path.exists() else []
    expected={item['id']:item['sha256'] for item in previous}
    report=[]
    for name in SELECTED:
        metadata=fetch(BASE+name+'/metadata.json');license_text=fetch(BASE+name+'/LICENSE.md')
        legal=json.loads(metadata)['legal']
        assert all(item['license']=='CC0-1.0' for item in legal),name
        (licenses/(name+'_metadata.json')).write_bytes(metadata)
        (licenses/(name+'_LICENSE.md')).write_bytes(license_text)
        url=BASE+name+'/glTF-Binary/'+name+'.glb';path=assets/(name+'.glb')
        raw=path.read_bytes() if path.exists() else fetch(url)
        sha=hashlib.sha256(raw).hexdigest()
        if name in expected:
            assert expected[name]==sha,'Existing asset integrity mismatch: '+name
        details=audit(raw)
        path.write_bytes(raw)
        record={'id':name,'url':url,'sha256':sha,'bytes':len(raw),'modelLicense':'CC0-1.0',
                'metadataLicense':'CC-BY-4.0','authors':legal,'audit':details}
        report.append(record)
        print(json.dumps({'id':name,'bytes':len(raw),'triangles':details['sourceTriangles'],
                          'images':len(details['images']),'sha256':sha}))
    previous_path.write_text(json.dumps(report,indent=2,ensure_ascii=False)+'\n')

if __name__=='__main__':
    main()
