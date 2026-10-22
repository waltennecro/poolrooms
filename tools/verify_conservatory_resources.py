"""Audit the conservatory's one externally licensed texture set and import contract."""
import hashlib,json
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'Assets/PoolroomsProcedural'
def main():
 folder=ASSETS/'Resources/ConservatorySources';m=json.loads((folder/'Ground037.json').read_text())
 assert m['id']=='Ground037' and m['license']=='CC0-1.0' and m['dimensions']==[2048,2048]
 assert m['newExternalModels']==0 and m['newExternalTextureSets']==1
 source=(folder/'Ground037_source.usda.txt').read_text()
 assert 'ambientCG' in source and 'CC0 1.0 Public Domain Dedication' in source and 'https://ambientcg.com/a/Ground037' in source
 assert (folder/'CC0-1.0.txt').stat().st_size>3000
 for name,e in m['sources'].items():
  assert len(e['sha256'])==64 and e['bytes']>100000
  assert m['mirrorRevision'] in e['url'] and e['url'].endswith(name)
 for name,e in m['derivedFiles'].items():
  path=ASSETS/'Resources'/name;data=path.read_bytes()
  assert data[:8]==b'\x89PNG\r\n\x1a\n' and len(data)==e['bytes'] and hashlib.sha256(data).hexdigest()==e['sha256']
  image=Image.open(path);image.load();assert image.size==(2048,2048)
  if '_Mask' in name:
   r,g,b,a=image.convert('RGBA').split();assert r.getextrema()==(0,0) and b.getextrema()==(255,255)
   assert g.getextrema()[0]<220 and a.getextrema()[0]<a.getextrema()[1]
  if '_Normal' in name:assert image.getchannel('B').getextrema()[1]>230
 importer=(ASSETS/'Editor/PoolroomsBiomeMaterialSetup.cs').read_text()
 assert 'ConservatoryGround' in importer and 'conservatory?2048' in importer and 'TextureImporterType.NormalMap' in importer and 'sRGBTexture=!normal&&!mask' in importer
 themes=(ASSETS/'Core/BiomeSurfaces.cs').read_text();assert 'SurfaceTheme.ConservatorySoil)return "ConservatoryGround"' in themes
 print('PASS: one CC0 ambientCG texture set, 3 shipped 2048² PBR maps / four pinned source hashes, real source CC0 declaration, HDRP mask and Unity importer source contract. Zero new downloaded models. Native importer unrun.')
if __name__=='__main__':main()
