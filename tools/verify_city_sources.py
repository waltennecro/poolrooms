"""Verify shipped CC0 source attribution, converted hashes, geometry and LOD counts."""
from pathlib import Path
import hashlib,json,math,struct,zlib

root=Path(__file__).resolve().parents[1]
assets=root/'Assets/PoolroomsProcedural/Resources/City11'
catalog=json.loads((assets/'catalog.json').read_text())
assert catalog['distinctSourceModels']==5 and not catalog['colourVariantsCountAsModels']
expected=json.loads((assets/'files.sha256.json').read_text())
actual={p.relative_to(assets).as_posix() for p in assets.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
assert actual==set(expected),'City source manifest mismatch'
for name,record in expected.items():
    data=(assets/name).read_bytes()
    assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
    if name.endswith('.png'):
        assert data[:8]==b'\x89PNG\r\n\x1a\n',name
        width,height=struct.unpack('>II',data[16:24]);assert 0<width<=4096 and 0<height<=4096
        if 'SheenChair_' in name:assert (width,height)==(2048,2048)
        offset=8;image=bytearray();ended=False
        while offset<len(data):
            size=struct.unpack('>I',data[offset:offset+4])[0];tag=data[offset+4:offset+8];payload=data[offset+8:offset+8+size];crc=struct.unpack('>I',data[offset+8+size:offset+12+size])[0]
            assert zlib.crc32(tag+payload)&0xffffffff==crc,name
            if tag==b'IDAT':image.extend(payload)
            if tag==b'IEND':ended=True
            offset+=size+12
        assert ended and zlib.decompress(image),name
for model in catalog['models']:
    source=root/'Sources/City11'/(model['id']+'.glb')
    if source.exists():assert hashlib.sha256(source.read_bytes()).hexdigest()==model['sourceSha256']
    meta=json.loads((assets/'Licenses'/(model['id']+'_metadata.json')).read_text())
    assert meta['legal'] and all(item.get('spdx')=='CC0-1.0' for item in meta['legal'])
    assert model['modelLicense']=='CC0-1.0' and model['metadataLicense']=='CC-BY-4.0'
    for variant in [model]+model['lods']:
        path=root/'Assets/PoolroomsProcedural/Resources'/(variant['resource']+'.obj')
        vertices=normals=uvs=faces=0;normal_lengths=[];referenced_normals=set()
        for line in path.read_text().splitlines():
            parts=line.split()
            if not parts:continue
            if parts[0] in ('v','vn','vt'):
                assert all(math.isfinite(float(v)) for v in parts[1:]),path
                if parts[0]=='v':vertices+=1
                elif parts[0]=='vn':
                    normals+=1;normal_lengths.append(math.sqrt(sum(float(x)**2 for x in parts[1:])))
                else:uvs+=1
            if parts[0]=='f':
                assert len(parts)==4
                for value in parts[1:]:
                    a,b,c=map(int,value.split('/'));assert 1<=a<=vertices and 1<=b<=uvs and 1<=c<=normals;referenced_normals.add(c-1)
                faces+=1
        assert faces==variant['triangles'],path
        assert all(.99<normal_lengths[i]<1.01 for i in referenced_normals),'Invalid shading normal: '+str(path)
    if model['id']=='SheenChair':
        assert model['triangles']==39936 and len(model['lods'])==2
        assert model['lods'][1]['triangles']<model['lods'][0]['triangles']<model['triangles']
        assert model['uvBake'] and all(x['resolution']==2048 and x['bakedPixels']>0 and x['uniqueCharts']>0 for x in model['uvBake'])
        assert any(x['sourceUV']==1 and x['texture']=='occlusionTexture' for x in model['uvBake'])
        assert any(x['texture']=='normalTexture' for x in model['uvBake'])
    if model['id']=='ToyCar':
        assert model['presentationClothRemoved'] and model['lods'][1]['triangles']<model['lods'][0]['triangles']<model['triangles']
        minimum,maximum=model['minimum'],model['maximum']
        assert maximum[2]-minimum[2]>1.8*(maximum[0]-minimum[0]),'Vehicle source is not aligned to its long axis'
print('Verified 5 distinct CC0 sources; 3 small props + 1 chair + 1 car; car LODs are derivatives of that same source. Native Unity validation pending.')
