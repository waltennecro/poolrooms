"""Audit building licences/resources and independently check all LOD access volumes."""
from pathlib import Path
import hashlib,json,math,struct,zlib
import numpy as np
from building_geometry import read_obj,building_boxes
ROOT=Path(__file__).resolve().parents[1];source=ROOT/'Sources/Buildings';dest=ROOT/'Assets/PoolroomsProcedural/Resources/Buildings'
files=json.loads((dest/'files.sha256.json').read_text());pngs=0
for name,record in files.items():
 p=dest/name;raw=p.read_bytes();assert len(raw)==record['bytes'] and hashlib.sha256(raw).hexdigest()==record['sha256'],name
 if p.suffix=='.png':
  assert raw.startswith(b'\x89PNG\r\n\x1a\n');w,h=struct.unpack_from('>II',raw,16);assert 0<w<=1024 and 0<h<=1024
  offset=8;data=bytearray();ended=False
  while offset<len(raw):
   size=struct.unpack_from('>I',raw,offset)[0];tag=raw[offset+4:offset+8];payload=raw[offset+8:offset+8+size];crc=struct.unpack_from('>I',raw,offset+8+size)[0];assert zlib.crc32(tag+payload)&0xffffffff==crc
   if tag==b'IDAT':data.extend(payload)
   if tag==b'IEND':ended=True
   offset+=size+12
  assert ended and zlib.decompress(data);pngs+=1
assert 'Amazon' in (dest/'Licenses/NOTICE.txt').read_text() and 'Creative Commons' in (dest/'Licenses/Bistro_CC_BY_4.txt').read_text()
assert (source/'Bistro_CC_BY_4.txt').read_bytes()==(dest/'Licenses/Bistro_CC_BY_4.txt').read_bytes()
lock=json.loads((source/'sources.lock.json').read_text());original=(ROOT/'Sources/Bistro/bistro_source.gltf').read_bytes();key=next(k for k in lock if k.endswith('/bistro.gltf'));assert hashlib.sha256(original).hexdigest()==lock[key]['sha256']
catalog=json.loads((dest/'catalog.json').read_text());assert catalog['distinctBuildingSourceFamilies']==2 and catalog['exteriorSkinAdaptations']==2 and catalog['nativeUnityValidated'] is False
referenced={m[key]+'.png' for model in catalog['models'] for m in model['materials'] for key in ['albedo','normal','mask','specular','emissive'] if m.get(key)}
actual={'Buildings/Textures/'+p.name for p in (dest/'Textures').glob('*.png')};assert actual==referenced,'Unreferenced or missing generated maps prevent clean-checkout reproducibility'
seen=set();total=0
# Independent separating-axis test: a nondegenerate triangle and a slightly inset
# box must be disjoint. It does not reuse the importer's polygon subtraction.
def overlaps(triangle,minimum,maximum):
 center=(minimum+maximum)*.5;half=(maximum-minimum)*.5;v=triangle-center;edges=np.roll(v,-1,axis=0)-v
 axes=[*np.eye(3),np.cross(edges[0],edges[1])]+[np.cross(e,a) for e in edges for a in np.eye(3)]
 for axis in axes:
  if np.linalg.norm(axis)<1e-12:continue
  projection=v@axis;radius=np.abs(axis)@half
  if projection.min()>radius or projection.max()<-radius:return False
 return True
for model in catalog['models']:
 name=model['id'];raw=(source/(name+'_adapted.glb')).read_bytes();sha=hashlib.sha256(raw).hexdigest();assert sha==model['sourceSha256'] and sha not in seen;seen.add(sha)
 assert model['modelLicense']=='CC-BY-4.0' and model['sourceSubtreeTriangles']>model['triangles']>1000
 assert model['canonicalFloors']==4 and model['canonicalWidth']==model['canonicalDepth']==10 and model['nativeUnityValidated'] is False
 assert abs(model['maximum'][1]-12.92)<1e-4 and abs(model['minimum'][1])<1e-4,(name,'Adapter pivot/height drift')
 assert max(abs(model['minimum'][0]),abs(model['maximum'][0]),abs(model['minimum'][2]),abs(model['maximum'][2]))<=5.3001
 names=[m['name'] for m in model['materials']]
 for material in model['materials']:
  if material.get('workflow')=='specular':assert (ROOT/'Assets/PoolroomsProcedural/Resources'/(material['specular']+'.png')).exists()
 resources=[{'resource':model['resource'],'triangles':model['triangles']}]+model['lods'];counts=[]
 for resource in resources:
  entries=read_obj(ROOT/'Assets/PoolroomsProcedural/Resources'/(resource['resource']+'.obj'),names);count=0
  for _,p,n,uv,faces in entries:
   assert np.all(np.isfinite(p)) and np.all(np.isfinite(uv)) and np.allclose(np.linalg.norm(n,axis=1),1,atol=2e-5)
   t=p[faces];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-10)
   low=t.min(1);high=t.max(1)
   for a,b in building_boxes():
    a=a+2e-5;b=b-2e-5
    possible=np.all(high>a,axis=1)&np.all(low<b,axis=1)
    for triangle in t[possible]:assert not overlaps(triangle,a,b),(name,resource['resource'],'Geometry enters an interior, doorway or window clearance box')
   count+=len(faces)
  assert count==resource['triangles'];counts.append(count);total+=count
 assert counts[0]>counts[1]>counts[2]>0
print(json.dumps({'buildingSourceFamilies':2,'exteriorSkinAdaptations':2,'sourceTriangleCounts':[m['sourceSubtreeTriangles'] for m in catalog['models']],'nativeTriangleCounts':{m['id']:[m['triangles']]+[l['triangles'] for l in m['lods']] for m in catalog['models']},'mapsAudited':pngs,'trianglesAudited':total,'allLodClearanceVolumes':'independent_SAT_passed','nativeUnityExecuted':False}))
