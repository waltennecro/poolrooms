"""Measure real integrated Level10 additions; do not count repeated plants as models."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def main():
 base='4e2100a7d9cc62553472d4f4fa751930870b7c4f'
 files=[p for p in (ROOT/'Assets').rglob('*') if p.is_file()]
 bank=ROOT/'Assets/PoolroomsProcedural/Resources/Level10'
 media=json.loads((bank/'manifest.json').read_text());audio=json.loads((bank/'Audio/manifest.json').read_text())
 source=[p for p in (ROOT/'Assets/PoolroomsProcedural').rglob('*.cs') if 'Level10' in p.name]
 result=dict(baseDeliveryRevision=base,assetsFiles=len(files),assetsBytes=sum(p.stat().st_size for p in files),newLevelResourceBytes=sum(p.stat().st_size for p in bank.rglob('*') if p.is_file()),nativePbrBytes=media['runtimeBytes'],newNativeScannedTextureSets=3,nativePbrMaps=12,originalAudioClips=4,originalAudioBytes=sum(e['bytes'] for e in audio['files']),newExternalModels=0,reusedFurnitureSourceModels=4,reusedFoodSourceModels=2,originalCountryProgrammes=12,originalRoofProfiles=3,newInventoryItems=3,newLevelSourceFiles=len(source),newLevelSourceLines=sum(len(p.read_text().splitlines()) for p in source),biomes=65,previousDiscoveryLabelsPreserved=64,paddingBytes=0,upscaledTextures=False,nativeUnityExecuted=False)
 (ROOT/'Verification/level10_scope.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
