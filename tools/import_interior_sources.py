"""Adapt two attributed static furniture sources with source UV/normal/AO baking."""
from pathlib import Path
import hashlib,json,struct,urllib.request
from import_city_sources import convert
ROOT=Path(__file__).resolve().parents[1];SOURCE=ROOT/'Sources/Interiors';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Interiors'
REV='edc7c9e67c639d230715049ee31f9a96a6babbbe';BASE=f'https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/{REV}/'
LOCK=SOURCE/'sources.lock.json';expected=json.loads(LOCK.read_text()) if LOCK.exists() else {}
MODELS=('GlamVelvetSofa','ChairDamaskPurplegold')
def acquire(name,url):
 p=SOURCE/name
 if not p.exists():p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(urllib.request.urlopen(url,timeout=45).read(64*1024*1024+1))
 raw=p.read_bytes();assert len(raw)<=64*1024*1024
 record={'url':url,'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
 if name in expected:assert expected[name]==record,'Pinned interior source changed: '+name
 expected[name]=record;return raw

def atlas_seed(raw):
 magic,version,length=struct.unpack_from('<III',raw);assert magic==0x46546c67 and version==2 and length==len(raw)
 n,tag=struct.unpack_from('<II',raw,12);assert tag==0x4e4f534a;doc=json.loads(raw[20:20+n]);aliased=False
 for mesh in doc['meshes']:
  for primitive in mesh['primitives']:
   if 'TEXCOORD_1' not in primitive['attributes']:
    primitive['attributes']['TEXCOORD_1']=primitive['attributes']['TEXCOORD_0'];aliased=True
 if not aliased:return raw,False
 # The existing UV0 becomes a seed for a newly packed disjoint atlas; this does
 # not invent source AO UV1. Bindings retain their actual original UV selection.
 encoded=json.dumps(doc,separators=(',',':')).encode();encoded+=b' '*((-len(encoded))%4)
 return struct.pack('<III',magic,version,20+len(encoded)+len(raw[20+n:]))+struct.pack('<II',len(encoded),tag)+encoded+raw[20+n:],True

def main():
 licenses=DEST/'Licenses';licenses.mkdir(parents=True,exist_ok=True);reports=[]
 license_data=acquire('CC-BY-4.0.txt',BASE+'LICENSES/CC-BY-4.0.txt');(licenses/'CC-BY-4.0.txt').write_bytes(license_data)
 for name in MODELS:
  meta=acquire(name+'_metadata.json',BASE+'Models/'+name+'/metadata.json');j=json.loads(meta)
  assert j['legal'] and all(x['spdx']=='CC-BY-4.0' for x in j['legal'])
  license_text=acquire(name+'_LICENSE.md',BASE+'Models/'+name+'/LICENSE.md')
  raw=acquire(name+'.glb',BASE+'Models/'+name+'/glTF-Binary/'+name+'.glb');prepared,aliased=atlas_seed(raw)
  report=convert('I_'+name,prepared,'Interiors',DEST,False,'CC-BY-4.0',True)
  report.update(sourceSha256=hashlib.sha256(raw).hexdigest(),sourceUrl=expected[name+'.glb']['url'],upstreamRevision=REV,modelLicense='CC-BY-4.0',authors=[x['artist'] for x in j['legal']],owners=[x['owner'] for x in j['legal']],metadataLicense='CC-BY-4.0',atlasSeedFromUV0=aliased,defaultMaterialVariantOnly=True,nativeUnityValidated=False,geometryFamily='WayfairOvalChair' if name=='ChairDamaskPurplegold' else 'WayfairGlamSofa',variantOf='SheenChair' if name=='ChairDamaskPurplegold' else None)
  report['adaptation']='Native OBJ and HDRP metal/roughness PBR; newly packed UV atlas with original AO, normal tangent frames, texture rotations and seam padding; default material variant only. Optional sheen/specular extensions recorded, not exact native matches.'
  (DEST/('I_'+name+'.json')).write_text(json.dumps(report,indent=2)+'\n');reports.append(report)
  (licenses/(name+'_metadata.json')).write_bytes(meta);(licenses/(name+'_LICENSE.md')).write_bytes(license_text)
  print(json.dumps({'source':name,'triangles':report['triangles'],'atlasSeedFromUV0':aliased,'size':[b-a for a,b in zip(report['minimum'],report['maximum'])],'bakedMaps':len(report['uvBake'])}),flush=True)
 (licenses/'NOTICE.txt').write_text('GlamVelvetSofa and ChairDamaskPurplegold by Eric Chadwick, owned by Wayfair, 2021.\nCreative Commons Attribution 4.0 International: https://creativecommons.org/licenses/by/4.0/\nOriginal pinned sources/metadata and individual notices retained in this package/repository.\nChanges: native OBJ conversion and source PBR texture/normal/AO baking to new UV atlas; default material variant only; runtime scale/placement and pickup volumes.\nNo endorsement implied.\n')
 (DEST/'catalog.json').write_text(json.dumps({'sourceFiles':len(reports),'newGeometryFamilies':1,'existingChairVariantSources':1,'nativeUnityValidated':False,'models':reports},indent=2)+'\n')
 files=sorted(p for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json');(DEST/'files.sha256.json').write_text(json.dumps({p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},indent=2)+'\n');LOCK.write_text(json.dumps(expected,indent=2)+'\n')
if __name__=='__main__':main()
