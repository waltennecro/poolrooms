"""Verify the new facade, imported clearance at every LOD and native 4K bindings."""
from pathlib import Path
import hashlib,json,struct
import numpy as np
from building_geometry import read_obj,building_boxes
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'Assets/PoolroomsProcedural/Resources';DEST=RES/'CityArchitectureSources'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
records=json.loads((DEST/'files.sha256.json').read_text())
for name,d in records.items():
 p=DEST/name;assert p.stat().st_size==d['bytes'] and sha(p)==d['sha256'],name
assert set(records)=={p.relative_to(DEST).as_posix() for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
catalog=json.loads((DEST/'catalog.json').read_text());assert catalog['distinctNewSourceModels']==1 and catalog['proceduralBuildingDesigns']==256 and catalog['untouchedCompleteModels']==0 and not catalog['nativeUnityValidated']
assert 'CC BY 4.0' in (DEST/'Licenses/NOTICE.txt').read_text()
assert (DEST/'Licenses/Bistro_CC_BY_4.txt').read_bytes()==(ROOT/'Sources/Bistro/Bistro_CC_BY_4.txt').read_bytes()
m=catalog['models'][0];assert m['sourceRootName']=='Paris_Building_010' and m['modelLicense']=='CC-BY-4.0' and len(m['lods'])==2
assert sha(ROOT/'Sources/CityArchitecture'/f"{m['id']}_adapted.glb")==m['sourceSha256']
lock=json.loads((ROOT/'Sources/CityArchitecture/sources.lock.json').read_text());doc=next(v for k,v in lock.items() if k.endswith('/bistro.gltf'));assert sha(ROOT/'Sources/Bistro/bistro_source.gltf')==doc['sha256']
# Independent separating-axis triangle/box intersection, not the importer cut.
def intersects(triangle,lo,hi):
 center=(lo+hi)*.5;half=(hi-lo)*.5;v=triangle-center;edges=np.roll(v,-1,axis=0)-v
 for axis in [*np.eye(3),np.cross(edges[0],edges[1])]+[np.cross(e,a) for e in edges for a in np.eye(3)]:
  if np.linalg.norm(axis)<1e-12:continue
  p=v@axis;r=np.abs(axis)@half
  if p.min()>r or p.max()<-r:return False
 return True
counts=[];names=[a['name'] for a in m['materials']]
for definition in [{'resource':m['resource'],'triangles':m['triangles']}]+m['lods']:
 p=RES/(definition['resource']+'.obj');entries=read_obj(p,names);count=0
 assert 'EntryOrientationMarker' in p.read_text()
 for _,v,n,uv,f in entries:
  assert np.isfinite(v).all() and np.isfinite(uv).all() and np.allclose(np.linalg.norm(n,axis=1),1,atol=2e-5)
  t=v[f];assert np.all(np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1)>1e-10)
  low=t.min(1);high=t.max(1)
  for a,b in building_boxes():
   a=a+2e-5;b=b-2e-5;possible=np.all(high>a,axis=1)&np.all(low<b,axis=1)
   for tri in t[possible]:assert not intersects(tri,a,b),(definition['resource'],'Clearance intrusion')
  count+=len(f)
 assert count==definition['triangles'];counts.append(count)
assert counts[0]>counts[1]>counts[2]>0
palette=json.loads((RES/'JourneyFinishes/palette.json').read_text())['materials'];fourK={}
for i in [0,19,33]:
 fourK[i]={}
 for key in ['albedo','normal','mask','specular']:
  p=RES/(palette[i][key]+'.png');w,h=struct.unpack_from('>II',p.read_bytes(),16);fourK[i][key]=[w,h]
  assert max(w,h)<=4096
  if key in ['albedo','normal'] or i!=0:assert (w,h)==(4096,4096)
  # Validate shared maps against the original bank hash, not merely their headers.
  bank=p.relative_to(RES).parts[0];rel=p.relative_to(RES/bank).as_posix();expected=json.loads((RES/bank/'files.sha256.json').read_text())[rel];assert sha(p)==expected['sha256']
for mat in m['materials']:
 for key in ['albedo','normal','mask','specular','emissive']:
  if mat.get(key):assert (RES/(mat[key]+'.png')).is_file()
importer=(ROOT/'Assets/PoolroomsProcedural/Editor/City11SourceImporter.cs').read_text();assert '/Resources/CityArchitectureSources/Models/' in importer
assert 'importer.maxTextureSize=4096' in (ROOT/'Assets/PoolroomsProcedural/Editor/PoolroomsMallImporter.cs').read_text()
print(json.dumps({'ordinaryDesigns':128,'strangeDesigns':128,'newExternalSourceModels':1,'previousBuildingSourceModelsPreserved':2,'newFacadeLodTriangles':counts,'native4KBindings':fourK,'upscaled':False,'sourceAndAllLodClearances':'independent SAT passed','nativeUnityExecuted':False}))
