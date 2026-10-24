"""Import three native 4K CC0 agricultural finishes; keep source hashes and HDRP channels.
Run with /usr/bin/python3 tools/import_level10_textures.py --cache /tmp/level10-sources.
"""
import argparse,concurrent.futures,hashlib,json,shutil,urllib.request
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Level10'
SETS={
 'Meadow':dict(id='Grass001',repo='KDAB/kdgpu-examples',revision='a1b22f17e9b1db2ad5cf1f0b30d3dffc489b7f79',folder='assets/textures/Grass001/',prefix='Grass001_4K_',ext='.jpg'),
 'Planks':dict(id='Planks037A',repo='CodyMarkix/PetNociuKrtkuse2',revision='2b6f7bb0e084683fcdfe47c9a09c09ac54f6e030',folder='Assets/Materials/Textures/StageWood/',prefix='Planks037A_4K-JPG_',ext='.jpg'),
 'Siding':dict(id='WoodSiding009',repo='CodyMarkix/PetNociuKrtkuse2',revision='2b6f7bb0e084683fcdfe47c9a09c09ac54f6e030',folder='Assets/Materials/Textures/Wood/',prefix='WoodSiding009_4K_',ext='.png')}
def sha(p):
 with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--cache',required=True,type=Path);a=p.parse_args();a.cache.mkdir(parents=True,exist_ok=True)
 (DEST/'Textures').mkdir(parents=True,exist_ok=True);(DEST/'Licenses').mkdir(exist_ok=True)
 def acquire(task):
  role,suffix=task;s=SETS[role];url=f'https://raw.githubusercontent.com/{s["repo"]}/{s["revision"]}/{s["folder"]}{s["prefix"]}{suffix}{s["ext"]}'
  local=a.cache/(role+'_'+suffix+s['ext'])
  if not local.exists():
   temp=local.with_suffix(local.suffix+'.partial')
   with urllib.request.urlopen(url,timeout=120) as source,temp.open('wb') as out:
    while block:=source.read(1024*1024):out.write(block)
   temp.replace(local)
  if local.stat().st_size<500 and local.read_bytes().startswith(b'version https://git-lfs.github.com/spec/v1'):
   pointer=local.read_text();oid=pointer.split('oid sha256:')[1].split()[0];length=int(pointer.split('size ')[1].split()[0])
   url=url.replace('https://raw.githubusercontent.com/','https://media.githubusercontent.com/media/')
   temp=local.with_suffix(local.suffix+'.partial')
   with urllib.request.urlopen(url,timeout=120) as source,temp.open('wb') as out:
    while block:=source.read(1024*1024):out.write(block)
   assert temp.stat().st_size==length and sha(temp)==oid,'Invalid LFS payload'
   temp.replace(local)
  url=url.replace('https://raw.githubusercontent.com/','https://media.githubusercontent.com/media/')
  return role,suffix,local,url
 tasks=[(k,s) for k in SETS for s in ['Color','NormalGL','Roughness','Displacement','AmbientOcclusion']]
 with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:sources=list(pool.map(acquire,tasks))
 manifest=dict(license='CC0-1.0',author='Lennart Demes / ambientCG (formerly CC0 Textures)',newExternalModels=0,newNativeTextureSets=3,upscaled=False,normalConvention='OpenGL +Y; Unity NormalMap importer',sets=[],files={})
 for role,s in SETS.items():
  maps={};records={}
  for r,suffix,local,url in sources:
   if r!=role:continue
   with Image.open(local) as im:
    assert im.size==(4096,4096),(role,suffix,im.size)
    maps[suffix]=im.convert('RGB' if suffix in ['Color','NormalGL'] else 'L')
   records[suffix]=dict(url=url,sha256=sha(local),bytes=local.stat().st_size,dimensions=[4096,4096])
  zero=Image.new('L',(4096,4096),0);one=Image.new('L',(4096,4096),255)
  images={'Albedo':maps['Color'],'Normal':maps['NormalGL'],'Mask':Image.merge('RGBA',(zero,maps['AmbientOcclusion'],one,ImageChops.invert(maps['Roughness']))),'Height':maps['Displacement']}
  for suffix,im in images.items():
   local=DEST/'Textures'/f'{role}_{suffix}.png';im.save(local,compress_level=9)
   manifest['files'][local.relative_to(DEST).as_posix()]=dict(bytes=local.stat().st_size,sha256=sha(local),dimensions=[4096,4096],role=suffix)
  manifest['sets'].append(dict(role=role,id=s['id'],original='https://ambientcg.com/a/'+s['id'],mirrorRevision=s['revision'],sources=records))
 manifest['runtimeBytes']=sum(v['bytes'] for v in manifest['files'].values())
 (DEST/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
 shutil.copyfile(ROOT/'Assets/PoolroomsProcedural/Resources/Forest39/Licenses/CC0-1.0.txt',DEST/'Licenses/CC0-1.0.txt')
 (DEST/'Licenses/NOTICE.txt').write_text('Level 10 native PBR finishes: Grass001, Planks037A, WoodSiding009 by Lennart Demes / ambientCG (formerly CC0 Textures). CC0-1.0.\n'+''.join('https://ambientcg.com/a/'+s['id']+'\n' for s in SETS.values())+'https://creativecommons.org/publicdomain/zero/1.0/\nGrass001 mirror declares CC-0 in KDAB/kdgpu-examples .reuse/dep5.\nPinned originals and byte hashes: ../manifest.json. Native 4096x4096; no upscaling or duplicate source maps. HDRP mask: R metallic=0, G AO, B detail=1, A inverted roughness.\nOriginal wheat geometry and original agricultural foley: project work, CC0-1.0.\nLevel 10 / Bounty setting: scutoid studios and egglord, based on u/Hertz3241 and prior rewrite by Stretchsterz, The Backrooms Wiki, CC BY-SA 3.0.\nhttps://backrooms-wiki.wikidot.com/level-10\nhttps://creativecommons.org/licenses/by-sa/3.0/\nAdaptation: accessible peaceful countryside, optional farm activities, no worms, starvation or forced water teleportation. No wiki photographs, copied passages or character models shipped.\nExisting licensed furniture and food retain their separate attribution.\n')
 print(json.dumps(dict(runtimeBytes=manifest['runtimeBytes'],native4KSets=3)))
if __name__=='__main__':main()
