"""Build in-game attribution text from the shipped upstream notices and metadata."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Assets/PoolroomsProcedural/Resources'
def build():
    text=['Créditos de recursos externos\n\nLicencias y notas de adaptación incluidas con los recursos. No se implica respaldo de sus autores.\nCC0: https://creativecommons.org/publicdomain/zero/1.0/\nCC BY 4.0: https://creativecommons.org/licenses/by/4.0/\nApache 2.0: https://www.apache.org/licenses/LICENSE-2.0\n']
    for p in sorted((BASE/'City11/Licenses').glob('*_metadata.json')):
        legal=json.loads(p.read_text())['legal']
        text.append('\n'+p.name.replace('_metadata.json','')+'\n'+'\n'.join(x['artist']+' · '+x.get('owner','')+' · '+x.get('spdx',x.get('license',''))+'\n'+x.get('licenseUrl','') for x in legal))
    for name in ['Aircraft','Helicopters','Interiors','Urban','Bistro','Buildings','GardenSources','BathSources']:
        text.append('\n\n'+name+'\n'+(BASE/name/'Licenses/NOTICE.txt').read_text())
    return '\n'.join(text)
if __name__=='__main__':(BASE/'PoolroomsAssetCredits.txt').write_text(build())
