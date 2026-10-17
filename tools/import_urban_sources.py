"""Pinned, attributed urban sources. Never executes upstream scripts."""
from pathlib import Path
import argparse,hashlib,json,struct,urllib.request
from import_city_sources import convert
from import_interior_sources import atlas_seed
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Urban';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Urban'
REV='edc7c9e67c639d230715049ee31f9a96a6babbbe';BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{REV}/'
LOCK=SOURCE/'sources.lock.json';expected=json.loads(LOCK.read_text()) if LOCK.exists() else {}
MODELS=('CarConcept','AntiqueCamera','SheenWoodLeatherSofa')
def acquire(name,url):
 p=SOURCE/name
 if not p.exists():
  p.parent.mkdir(parents=True,exist_ok=True);data=urllib.request.urlopen(url,timeout=50).read(64*1024*1024+1);assert len(data)<=64*1024*1024;p.write_bytes(data)
 raw=p.read_bytes();record={'url':url,'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
 if name in expected:assert expected[name]==record,'Pinned source changed: '+name
 expected[name]=record;return raw

def prepare_car(raw):
 n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n]);removed=[]
 def walk(i,part=None):
  node=doc['nodes'][i];name=node.get('name','')
  part={'WheelFrontL':'Wheel_FL','WheelFrontR':'Wheel_FR','WheelRearL':'Wheel_RL','WheelRearR':'Wheel_RR'}.get(name,part)
  if name in ('License Plate','InteriorSteeringEmblem'):
   removed.append(name);node.pop('mesh',None)
  if 'mesh' in node:node['name']=(part+('_Caliper' if 'BrakePad' in name else '_Tyre' if not name else '')) if part else 'Body'
  for child in node.get('children',[]):walk(child,part)
 for i in doc['scenes'][doc.get('scene',0)]['nodes']:walk(i)
 # Remove unreachable logo meshes from material/UV baking as well as the scene.
 used=sorted({node['mesh'] for node in doc['nodes'] if 'mesh' in node});meshes=doc['meshes'];doc['meshes']=[meshes[i] for i in used]
 for node in doc['nodes']:
  if 'mesh' in node:node['mesh']=used.index(node['mesh'])
 encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4);rest=raw[20+n:]
 return struct.pack('<III',0x46546c67,2,20+len(encoded)+len(rest))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+rest,removed

def uniform_uv(raw):
 n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n]);binary=bytearray(raw[28+n:])
 for mesh in doc['meshes']:
  for p in mesh['primitives']:
   if 'TEXCOORD_0' in p['attributes']:continue
   m=doc['materials'][p.get('material',0)];assert not any(k.endswith('Texture') for k in m) and not any(k.endswith('Texture') for k in m.get('pbrMetallicRoughness',{})),'Missing UVs for a textured part'
   count=doc['accessors'][p['attributes']['POSITION']]['count'];length=count*8
   while len(binary)%4:binary.append(0)
   view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':length});binary.extend(bytes(length))
   index=len(doc['accessors']);doc['accessors'].append({'bufferView':view,'componentType':5126,'count':count,'type':'VEC2'});p['attributes']['TEXCOORD_0']=index
 doc['buffers'][0]['byteLength']=len(binary);encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
 return struct.pack('<III',0x46546c67,2,28+len(encoded)+len(binary))+struct.pack('<II',len(encoded),0x4e4f534a)+encoded+struct.pack('<II',len(binary),0x004e4942)+binary

def refresh_material_constants():
 # Keep material constants from the original, not from baked RGBs. This pass is
 # idempotent and also permits refreshing constants without rebaking atlas PNGs.
 catalog=json.loads((DEST/'catalog.json').read_text())
 for report in catalog['models']:
  name=report['id'][2:];raw=(SOURCE/(name+'.glb')).read_bytes()
  if name=='CarConcept':raw,_=prepare_car(raw)
  n=struct.unpack_from('<I',raw,12)[0];doc=json.loads(raw[20:20+n]);used=sorted({p.get('material',0) for m in doc['meshes'] for p in m['primitives']})
  assert len(used)==len(report['materials'])
  for native,source in zip(report['materials'],used):
   m=doc['materials'][source];ext=m.get('extensions',{});native['sourceName']=m.get('name','material '+str(source));native['clearcoat']=ext.get('KHR_materials_clearcoat',{}).get('clearcoatFactor',0)
   strength=ext.get('KHR_materials_emissive_strength',{}).get('emissiveStrength',1);native['emission']=[x*strength for x in m.get('emissiveFactor',[0,0,0])]
  (DEST/(report['id']+'.json')).write_text(json.dumps(report,indent=2)+'\n')
 (DEST/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n')
 files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json');(DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n')

def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--only',nargs='+',choices=MODELS);args=parser.parse_args()
 licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True);reports=[]
 for name in MODELS:
  meta=acquire(name+'_metadata.json',BASE+'Models/'+name+'/metadata.json');j=json.loads(meta)
  assert j['legal'] and all(x.get('spdx',x.get('license','')) in ('CC0-1.0','CC-BY-4.0') or x.get('spdx',x.get('license','')).startswith('LicenseRef-LegalMark-') for x in j['legal'])
  license_text=acquire(name+'_LICENSE.md',BASE+'Models/'+name+'/LICENSE.md')
  for item in j['legal']:
   spdx=item.get('spdx',item['license']);filename=spdx+'.txt';url=BASE+'LICENSES/'+filename
   (licenses/filename).write_bytes(acquire(filename,url))
  raw=acquire(name+'.glb',BASE+'Models/'+name+'/glTF-Binary/'+name+'.glb');prepared=raw;removed=[]
  if name=='CarConcept':prepared,removed=prepare_car(raw)
  prepared,aliased=atlas_seed(uniform_uv(prepared))
  print('Baking licensed urban source:',name,flush=True)
  # All maps keep the original UV selection, normal tangent frame and material
  # factors. The car's optional paint variants are not presented as new models.
  report=convert('U_'+name,prepared,'Urban',DEST,name=='CarConcept','CC0-1.0' if name=='AntiqueCamera' else 'CC-BY-4.0',name!='AntiqueCamera',
    [(1,32,16),(2,14,8)] if name=='CarConcept' else [(1,32,16),(2,12,8)],name=='CarConcept',1024 if name=='CarConcept' else 2048,True,True) if not args.only or name in args.only else json.loads((DEST/('U_'+name+'.json')).read_text())
  report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),sourceUrl=expected[name+'.glb']['url'],upstreamRevision=REV,
    modelLicense='CC0-1.0' if name=='AntiqueCamera' else 'CC-BY-4.0',legal=j['legal'],authors=sorted({x['artist'] for x in j['legal'] if not x.get('spdx',x['license']).startswith('LicenseRef-')}),nativeUnityValidated=False,removedTrademarkMeshes=removed,atlasSeedFromUV0=aliased,defaultMaterialVariantOnly=True)
  if name=='CarConcept':report['wheels']=[{'part':'Wheel_'+axle+side,'front':axle=='F'} for axle in ('R','F') for side in ('L','R')];report['adaptation']='Original static body merged per material, four native wheel assemblies, brake calipers kept separate; two derivatives/UV atlas/PBR; steering emblem and logo plate meshes omitted; default paint only, optional clearcoat/transmission approximated in HDRP.'
  else:report['adaptation']='Native OBJ, two bounded LOD derivatives and baked original PBR/normal/AO atlas. Legal marks retain their separate notices; not a claim of trademark rights.'
  (DEST/('U_'+name+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report)
  (licenses/(name+'_metadata.json')).write_bytes(meta);(licenses/(name+'_LICENSE.md')).write_bytes(license_text)
  print(json.dumps({'model':name,'triangles':report['triangles'],'lods':report['lods'],'maps':len(report['uvBake'])}),flush=True)
 (licenses/'NOTICE.txt').write_text('CarConcept by Eric Chadwick, Darmstadt Graphics Group GmbH, 2024, CC BY 4.0.\nAntiqueCamera by Maximillan Kamps, UX3D, 2018, CC0; embedded UX3D mark has separate terms.\nSheenWoodLeatherSofa: original by Fran Calvente (CC0, 2021), improvements by Eric Chadwick / DGG (CC BY 4.0, 2024).\nAll metadata, full licenses and pinned source references included. No endorsement or independent trademark rights implied.\nChanges: native meshes/LOD derivatives, original PBR UV/normal/AO atlas baking, default material choices; car mesh grouping, wheel pivots and removal of two logo meshes.\n')
 (DEST/'catalog.json').write_text(json.dumps({'distinctSourceModels':len(reports),'variantsCountAsModels':False,'nativeUnityValidated':False,'models':reports},indent=2)+'\n')
 files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json');(DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n');LOCK.write_text(json.dumps(expected,indent=2)+'\n');refresh_material_constants()
if __name__=='__main__':main()
