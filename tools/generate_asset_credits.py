"""Build in-game attribution text from the shipped upstream notices and metadata."""
from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Assets/PoolroomsProcedural/Resources'
def build():
    text=['Créditos de recursos externos\n\nLicencias y notas de adaptación incluidas con los recursos. No se implica respaldo de sus autores.\nCC0: https://creativecommons.org/publicdomain/zero/1.0/\nCC BY 4.0: https://creativecommons.org/licenses/by/4.0/\nApache 2.0: https://www.apache.org/licenses/LICENSE-2.0\n']
    for p in sorted((BASE/'City11/Licenses').glob('*_metadata.json')):
        legal=json.loads(p.read_text())['legal']
        text.append('\n'+p.name.replace('_metadata.json','')+'\n'+'\n'.join(x['artist']+' · '+x.get('owner','')+' · '+x.get('spdx',x.get('license',''))+'\n'+x.get('licenseUrl','') for x in legal))
    for name in ['Aircraft','Helicopters','Interiors','Urban','Bistro','Buildings','GardenSources','BathSources','RestfulSources','WingSources','ImmersiveAudioSources','EncounterSources','ImmersionSources','MallSources','MallFinishes','JourneyFinishes','JourneySources','FoodSources']:
        text.append('\n\n'+name+'\n'+(BASE/name/'Licenses/NOTICE.txt').read_text())
    text.append('\nCalm water micro-normal: original analytic texture for this project, CC0-1.0. tools/create_calm_water_normal.py.\n')
    food=json.loads((BASE/'FoodSources/catalog.json').read_text())
    text.append('\nFood catalogue: '+str(food['distinctFoodTypes'])+' food types / '+str(food['edibleFoods'])+' presentations, '+str(food['drinks'])+' drinks and '+str(food['distinctSourceModels'])+' distinct imported source models. LODs are derivatives, not new models. Stylised geometry, not photogrammetry.\n')
    text.append('Dreamcore Level11 civic pavilions are original architecture. Textual research: Level 11, The Backrooms Wiki, Stretchster / Boring Talking, https://backrooms-wiki.wikidot.com/level-11 (CC BY-SA 3.0). No wiki image or quoted passage included. Existing furniture retains its original attribution.\n')
    return '\n'.join(text)
if __name__=='__main__':(BASE/'PoolroomsAssetCredits.txt').write_text(build())
