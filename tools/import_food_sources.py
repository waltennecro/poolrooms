"""Reproducible CC0 Food Kit adaptation. Requires Blender 4.3 and system Python PIL/numpy.
The source is a stylised kit, not photogrammetry: measured bevels, smooth normals,
metric UVs and original PBR microfinish maps improve it without claiming scans.
"""
from pathlib import Path
import hashlib, json, zipfile, sys, subprocess, os, tempfile, urllib.request
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/Food'; DEST=ROOT/'Assets/PoolroomsProcedural/Resources/FoodSources'
URL='https://kenney.nl/media/pages/assets/food-kit/83086fa91c-1719418518/kenney_food-kit.zip'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def prepare():
    import numpy as np
    from PIL import Image,ImageFilter,ImageDraw,ImageFont
    source=SOURCE/'kenney_food-kit.zip'
    if not source.exists():
        with urllib.request.urlopen(URL,timeout=60) as r:source.write_bytes(r.read())
    selection=json.loads((SOURCE/'selection.json').read_text())
    z=zipfile.ZipFile(source); licences=DEST/'Licenses';licences.mkdir(parents=True,exist_ok=True)
    (licences/'Kenney_License.txt').write_bytes(z.read('License.txt'))
    (licences/'NOTICE.txt').write_text('Food Kit 2.0 by Kenney (https://kenney.nl/assets/food-kit), CC0 1.0.\nAdaptation by this project: bevels, smooth normals, metric detail UVs, one simplified LOD, original tiled PBR microfinishes and almond-water label. These additions are also CC0.\nThis is stylised source geometry, not photogrammetric food. LODs and textures are not additional source models.\n')
    files={p:{'bytes':len(z.read(p)),'sha256':hashlib.sha256(z.read(p)).hexdigest()} for p in z.namelist() if p=='License.txt' or p=='Models/OBJ format/Textures/colormap.png' or any(p=='Models/OBJ format/'+d['model']+'.obj' for d in selection)}
    (SOURCE/'sources.lock.json').write_text(json.dumps({'url':URL,'page':'https://kenney.nl/assets/food-kit','license':'CC0-1.0','author':'Kenney','version':'2.0','archiveBytes':source.stat().st_size,'archiveSha256':digest(source),'members':files},indent=2)+'\n')
    # A small set of genuinely different shared surface functions, not 125 duplicated 4K images.
    textures=DEST/'Textures';textures.mkdir(parents=True,exist_ok=True)
    size=1024; yy,xx=np.mgrid[:size,:size];rng=np.random.default_rng(110125)
    for family in ['produce','pastry','prepared','drink']:
        grain=rng.random((size,size)).astype(np.float32)
        blurred=np.array(Image.fromarray(np.uint8(grain*255)).filter(ImageFilter.GaussianBlur(2)),dtype=np.float32)/255
        if family=='produce':
            pores=np.where(grain>.965,(grain-.965)*9,0);height=blurred*.3-pores*.3;shade=.94-pores*.13+(blurred-.5)*.1;smooth=.54-pores*.2
        elif family=='pastry':
            height=blurred*.65+grain*.08;shade=.89+(blurred-.5)*.26-(grain>.98)*.09;smooth=np.full_like(grain,.17)+(blurred-.5)*.12
        elif family=='prepared':
            height=blurred*.32+grain*.035;shade=.93+(blurred-.5)*.16;smooth=.28+blurred*.12
        else:
            height=blurred*.07;shade=.98+(blurred-.5)*.035;smooth=.46+blurred*.08
        Image.fromarray(np.uint8(np.clip(np.repeat(shade[:,:,None],3,axis=2)*255,0,255))).save(textures/(family+'_albedo.png'))
        dx=(np.roll(height,-1,1)-np.roll(height,1,1))*.7;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*.7
        normal=np.stack([-dx,dy,np.ones_like(dx)],axis=2);normal/=np.linalg.norm(normal,axis=2)[:,:,None]
        Image.fromarray(np.uint8(np.clip((normal*.5+.5)*255,0,255))).save(textures/(family+'_normal.png'))
        mask=np.stack([np.zeros_like(grain),np.ones_like(grain),np.full_like(grain,.5),smooth],axis=2)
        Image.fromarray(np.uint8(np.clip(mask*255,0,255))).save(textures/(family+'_mask.png'))
    # An original label, not an unlicensed brand. Colour/roughness follow carton stock.
    label=Image.new('RGB',(1024,1024),(233,226,207));draw=ImageDraw.Draw(label)
    font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
    for text,y,pt in [('AGUA',160,110),('DE ALMENDRA',310,82),('Para la próxima visita',690,42),('500 ml',810,48)]:
        draw.text((512,y),text,font=ImageFont.truetype(font,pt),fill=(57,77,65),anchor='mt')
    draw.ellipse((420,450,604,625),fill=(161,114,66),outline=(91,66,38),width=9)
    draw.arc((445,466,590,611),80,270,fill=(226,196,151),width=8)
    label.save(textures/'almond_label_albedo.png')
    return selection

def main():
    selection=prepare();blender=os.environ.get('BLENDER','/usr/bin/blender')
    subprocess.run([blender,'--background','--factory-startup','--threads','2','--python',str(ROOT/'tools/food_blender_adapter.py'),'--',str(ROOT)],check=True,stdout=(SOURCE/'adaptation.log').open('w'),stderr=subprocess.STDOUT)
    rows=[json.loads((DEST/(r['resourceId']+'.json')).read_text()) for r in selection]
    catalog={'distinctSourceModels':len(selection),'distinctFoodTypes':len({r['foodType'] for r in selection if not r['drink']}),'edibleFoods':sum(not r['drink'] for r in selection),'drinks':sum(r['drink'] for r in selection),'lodsCountAsModels':False,'source':'Kenney Food Kit 2.0','license':'CC0-1.0','nativeUnityValidated':False,'originalSharedMicrofinishSets':4,'textureResolution':1024,'models':rows}
    (DEST/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n')
    manifest={p.relative_to(DEST).as_posix():{'bytes':p.stat().st_size,'sha256':digest(p)} for p in sorted(DEST.rglob('*')) if p.is_file() and p.name!='files.sha256.json' and not p.name.endswith('.meta')}
    (DEST/'files.sha256.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (SOURCE/'adaptation.log').unlink() # Logs include nonreproducible timings; retain auditable outputs instead.
    print('Adapted',len(rows),'distinct models;',sum(r['triangles'] for r in rows),'triangles across the catalogue, not a simultaneous scene.')
if __name__=='__main__':main()
