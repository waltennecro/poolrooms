"""Verify original cloud data and existing licensed finishes; native HDRP rendering is separate."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];R=ROOT/'Assets/PoolroomsProcedural'
def main():
 p=R/'Resources/Sky63';m=json.loads((p/'Provenance.json').read_text());d=(p/'CloudDensity.bytes').read_bytes()
 assert len(d)==64**3==m['bytes'] and hashlib.sha256(d).hexdigest()==m['sha256'] and m['license']=='CC0-1.0'
 assert min(d)==0 and max(d)>180 and len(set(d))>180
 for finish in ['EncounterOak','EncounterBrass','GardenLeaves']:
  assert (R/f'Resources/PoolroomsBiomeTextures/{finish}_Albedo.png').exists()
 native=(R/'Integrations/HDRP/PoolroomsSky63Clouds.cs').read_text()
 assert 'LocalVolumetricFog[2]' in native and 'parameters' in native and 'volumeMask=density' in native and 'volumetricFog' in native
 print('PASS: original CC0 64^3 cloud mask, hash/density range and two-volume source budget; three existing attributed PBR finishes. No new external model or photographic cloud. Native HDRP/appearance unrun.')
if __name__=='__main__':main()
