"""Audit usable Level10 media, native depth, provenance and actual runtime bindings."""
import hashlib,json,struct,wave
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'Assets/PoolroomsProcedural';BANK=ASSETS/'Resources/Level10'
def sha(p):
 with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 m=json.loads((BANK/'manifest.json').read_text());assert m['license']=='CC0-1.0' and not m['upscaled']
 assert m['newNativeTextureSets']==3 and m['newExternalModels']==0
 assert m['runtimeBytes']==sum(e['bytes'] for e in m['files'].values()) and m['runtimeBytes']>=200_000_000
 assert {s['id'] for s in m['sets']}=={'Grass001','Planks037A','WoodSiding009'}
 assert {p.relative_to(BANK).as_posix() for p in (BANK/'Textures').glob('*.png')}==set(m['files'])
 digests=set()
 for name,e in m['files'].items():
  p=BANK/name;assert p.stat().st_size==e['bytes'] and sha(p)==e['sha256'],name
  assert e['sha256'] not in digests,'Duplicate media';digests.add(e['sha256'])
  with Image.open(p) as im:
   im.load();assert list(im.size)==e['dimensions'] and max(im.size)==4096 and min(im.size)>=2048,name
   assert im.mode==e['mode'],name
   if '_Height' in name:
    lo,hi=im.getextrema();assert lo<hi,'Flattened displacement: '+name
    if name.startswith('Textures/Siding'):assert im.mode in ('I;16','I;16B','I') and hi>255,'Native 16-bit depth was lost'
   if '_Mask' in name:
    assert im.mode=='RGBA' and im.getchannel('R').getextrema()==(0,0) and im.getchannel('B').getextrema()==(255,255)
    lo,hi=im.getchannel('A').getextrema();assert lo<hi,'Flattened roughness'
   if '_Normal' in name:assert im.mode=='RGB' and im.getchannel('B').getextrema()[1]>230
 for s in m['sets']:
  assert s['original']=='https://ambientcg.com/a/'+s['id'] and len(s['mirrorRevision'])==40
  assert set(s['sources'])=={'Color','NormalGL','Roughness','Displacement','AmbientOcclusion'}
  for e in s['sources'].values():assert s['mirrorRevision'] in e['url'] and len(e['sha256'])==64 and e['bytes']>10000
  assert m['files']['Textures/'+s['role']+'_Albedo.png']['dimensions']==s['sources']['Color']['dimensions']
 a=json.loads((BANK/'Audio/manifest.json').read_text());assert a['license']=='CC0-1.0' and a['sourceSamples']==0 and not a['voicesOrWhispers'] and not a['startleStingers']
 assert len(a['files'])==4
 for e in a['files']:
  p=BANK/'Audio'/e['path'];assert p.stat().st_size==e['bytes'] and sha(p)==e['sha256']
  with wave.open(str(p),'rb') as w:
   assert w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==44100
   assert abs(w.getnframes()/44100-e['seconds'])<.0001;data=w.readframes(w.getnframes())
  samples=struct.unpack('<'+'h'*(len(data)//2),data);assert .01<max(abs(v) for v in samples)/32767<.95
  if e['loop']:assert abs(samples[0]-samples[-1])<=1 and e['seconds']==32
  else:assert samples[0]==samples[-1]==0
 notice=(BANK/'Licenses/NOTICE.txt').read_text();assert all(t in notice for t in ['Lennart Demes','CC0-1.0','scutoid studios','egglord','CC BY-SA 3.0'])
 assert (BANK/'Licenses/CC0-1.0.txt').stat().st_size>3000
 research=json.loads((ASSETS/'Documentation/LevelResearch/Level10.json').read_text());assert research['level'].startswith('10') and len(research['accessibleResearch']['sha256'])==64 and not research['nativeUnityExecuted']
 importer=(ASSETS/'Editor/PoolroomsLevel10Importer.cs').read_text()
 for literal in ['TextureImporterNPOTScale.None','maxTextureSize=4096','streamingMipmaps=true','TextureImporterType.NormalMap','AudioClipLoadType.Streaming','TextureImporterFormat.R16']:assert literal in importer,literal
 materials=(ASSETS/'Runtime/PoolroomsLevel10Materials.cs').read_text()
 for role in ['Meadow','Planks','Siding']:assert '"'+role+'"' in materials
 for suffix in ['_Albedo','_Normal','_Mask','_Height']:assert suffix in materials
 assert 'new Vector2(.5f,1)' in materials and 'Forest39Ground' in materials,'Aspect/dirt reuse binding missing'
 world=(ASSETS/'Runtime/PoolroomsWorld.cs').read_text();assert 'PoolroomsLevel10Crops.Attach' in world and 'PoolroomsLevel10.Attach' in world
 atmosphere=(ASSETS/'Integrations/HDRP/PoolroomsBiomeAtmosphere.cs').read_text();assert 'wheat10?4200' in atmosphere and '!wheat10' in atmosphere
 ambient=(ASSETS/'Runtime/PoolroomsLevel10Ambience.cs').read_text();assert 'Level10/Audio/FieldWind' in ambient and 'EffectiveMasterVolume' in ambient and 'environmentVolume' in ambient
 from generate_asset_credits import build
 assert (ASSETS/'Resources/PoolroomsAssetCredits.txt').read_text()==build()
 print(json.dumps(dict(nativeTextureBytes=m['runtimeBytes'],nativeTextureSets=3,nativeMaps=12,originalSpatialClips=4,audioBytes=sum(e['bytes'] for e in a['files']),newExternalModels=0,upscaled=False,nativeUnityExecuted=False)))
if __name__=='__main__':main()
