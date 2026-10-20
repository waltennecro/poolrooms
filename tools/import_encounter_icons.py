from pathlib import Path
import urllib.request,hashlib,json,subprocess,shutil,tempfile,xml.etree.ElementTree as ET
rev='82d948812bfe3f269ef8f731dcdb07b08160edc4'
models={'bag':('lorc','knapsack'),'compass':('lorc','compass'),'plant':('delapouite','watering-can'),'cup':('delapouite','coffee-cup'),'journal':('delapouite','notebook'),'camera':('delapouite','photo-camera'),'music':('delapouite','musical-notes'),'book':('lorc','book-cover'),'game':('delapouite','dice-six-faces-five'),'food':('delapouite','cookie'),'furniture':('delapouite','wooden-chair')}
src=Path('Sources/Encounter');dest=Path('Assets/PoolroomsProcedural/Runtime/Resources/EncounterIcons');legal=Path('Assets/PoolroomsProcedural/Resources/EncounterSources/Licenses')
for p in [src,dest,legal]:p.mkdir(parents=True,exist_ok=True)
lock={};resources={}
for name,(author,filename) in models.items():
 url=f'https://raw.githubusercontent.com/game-icons/icons/{rev}/{author}/{filename}.svg';data=urllib.request.urlopen(url,timeout=30).read();path=src/(name+'.svg');path.write_bytes(data)
 out=dest/(name+'.png')
 document=ET.fromstring(data)
 for child in list(document):
  if child.tag.endswith('path') and child.get('d','').replace(' ','')=='M00h512v512H0z':document.remove(child)
 with tempfile.TemporaryDirectory(prefix='encounter-icon-') as temp:
  adapted=Path(temp)/'transparent.svg';adapted.write_bytes(ET.tostring(document))
  subprocess.run(['inkscape',str(adapted),'--export-type=png','--export-background-opacity=0','--export-width=96','--export-height=96','--export-filename='+str(out)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 lock[name+'.svg']={'url':url,'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'author':'Lorc' if author=='lorc' else 'Delapouite','license':'CC-BY-3.0'}
 resources[out.as_posix()]={'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest()}
(src/'icons.lock.json').write_text(json.dumps(lock,indent=2)+'\n');(src/'icons.generated.json').write_text(json.dumps(resources,indent=2)+'\n')
shutil.copyfile('Assets/PoolroomsProcedural/Resources/WingSources/Licenses/CC-BY-3.0.txt',legal/'CC-BY-3.0.txt')
(legal/'NOTICE.txt').write_text('Encuentro · inventory icons and original room-life content\n11 icons by Lorc and Delapouite / game-icons.net, CC BY 3.0.\nhttps://game-icons.net/ · https://creativecommons.org/licenses/by/3.0/\nSource revision: '+rev+'\nAdaptation: black background removed; transparent 96px PNG rasterization; see Sources/Encounter/icons.lock.json for each author and original URL.\nNo downloaded 3D models or motion-capture clips are counted as new for this stage. The new room-life placements reuse existing licensed furniture. Its original notices remain in City11, Bistro, GardenSources, BathSources, Interiors and RestfulSources.\nOriginal procedural joinery, stories, interaction poses, piano-note synthesis and Encounter PBR textures: this project, CC0-1.0. No brands or characters.\n')
shutil.copyfile('Sources/Restful/CC0-1.0.txt',legal/'CC0-1.0.txt')
print('11 licensed icons integrated; source locks and author notices saved.')
