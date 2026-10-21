"""Build honest source/geometry references and actual Core plan diagrams.

These images are not Unity screenshots or promises of final shader quality.
Run the explicit --mall-atlas Core export before this script.
"""
from pathlib import Path
import json,math
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1];FOLDER=ROOT/'Documentation/References/Mall'
RES=ROOT/'Assets/PoolroomsProcedural/Resources/MallSources'
def main():
    plans=json.loads((FOLDER/'layouts.json').read_text());blueprints=[];previews=[]
    for plan in plans:
        count=plan['floors'];fig,axes=plt.subplots(count,1,figsize=(16,count*4),squeeze=False)
        kinds=sorted({s['kind'] for s in plan['shops']});colours={k:plt.get_cmap('tab20')(i%20) for i,k in enumerate(kinds)}
        for floor in range(count):
            ax=axes[floor,0];ax.set_facecolor('#d9e3e8')
            for slab in plan['slabs'][floor]:ax.add_patch(Rectangle((slab['x0'],slab['z0']),slab['x1']-slab['x0'],slab['z1']-slab['z0'],facecolor='#eee6da',edgecolor='#c4b9aa',linewidth=.3))
            for s in plan['shops']:
                if s['floor']!=floor:continue
                x=s['x']-s['width']/2;z=22 if s['side']>0 else -22-s['depth']
                ax.add_patch(Rectangle((x,z),s['width'],s['depth'],facecolor=colours[s['kind']],edgecolor='#454d52',linewidth=.5,alpha=.8))
                ax.text(s['x'],z+s['depth']/2,s['kind'],ha='center',va='center',fontsize=5,rotation=90)
            ax.set_xlim(-plan['width']/2,plan['width']/2);ax.set_ylim(-plan['depth']/2,plan['depth']/2);ax.set_aspect('equal');ax.set_title(f'Floor {floor+1} | {plan["atrium"]} | {plan["roof"]}');ax.set_xlabel('Metres');ax.set_ylabel('Metres')
        fig.suptitle(f'Actual generated mall plan · seed {plan["seed"]} · {len(plan["shops"])} shops\nCore blueprint, NOT an in-game image',fontsize=14);fig.tight_layout(rect=[0,0,1,.97]);name=f'Plan_{plan["seed"]}.png';fig.savefig(FOLDER/name,dpi=120);plt.close(fig);blueprints.append(name)
    names=['IndoorChair','IndoorTable','CashRegister','Stool','Napkins','MenuSign']
    for name in names:
        vertices=[];faces=[];colours=[];definition=json.loads((RES/('M_'+name+'.json')).read_text());materials={m['name']:m for m in definition['materials']};colour=np.array([.62,.49,.34]);light=np.array([.35,.8,.55]);light/=np.linalg.norm(light)
        with (RES/'Models'/('M_'+name+'.obj')).open() as f:
            for line in f:
                p=line.split()
                if not p:continue
                if p[0]=='v':vertices.append(list(map(float,p[1:])))
                elif p[0]=='usemtl':colour=np.array(materials[p[1]].get('colour',[.65,.6,.5])[:3]);colour=np.maximum(colour,.06)**.4545
                elif p[0]=='f':
                    points=np.array([vertices[int(i.split('/')[0])-1] for i in p[1:]])
                    n=np.cross(points[1]-points[0],points[2]-points[0]);n/=max(np.linalg.norm(n),1e-12);shade=.3+.7*max(0,np.dot(n,light));faces.append(points[:,[0,2,1]]);colours.append(np.clip(colour*shade,0,1))
        fig=plt.figure(figsize=(6,6));ax=fig.add_subplot(111,projection='3d');ax.add_collection3d(Poly3DCollection(faces,facecolors=colours,edgecolors='none'))
        points=np.array(vertices);lo=points.min(0);hi=points.max(0);span=max(hi-lo)*.6;mid=(lo+hi)/2
        ax.set_xlim(mid[0]-span,mid[0]+span);ax.set_ylim(mid[2]-span,mid[2]+span);ax.set_zlim(mid[1]-span,mid[1]+span);ax.set_box_aspect((1,1,1));ax.view_init(elev=20,azim=-58);ax.set_axis_off()
        fig.suptitle(name+'\nSource geometry preview · no game shading',fontsize=12);file='Geometry_'+name+'.png';fig.savefig(FOLDER/file,dpi=120);plt.close(fig)
        previews.append({'file':file,'model':'M_'+name,'license':'CC-BY-4.0','author':'Amazon.com, Inc. or its affiliates','description':'Derived preview of licensed source geometry; no Unity shaders or ray tracing.'})
    (FOLDER/'bistro-previews.json').write_text(json.dumps(previews,indent=2)+'\n')
    model_refs=json.loads((FOLDER/'models.json').read_text());finish_refs=json.loads((FOLDER/'finishes.json').read_text())
    all_images=[x['file'] for x in model_refs]+[x['file'] for x in finish_refs]+[x['file'] for x in previews]
    board=Image.new('RGB',(1440,((len(all_images)+5)//6)*190),(35,37,41));draw=ImageDraw.Draw(board)
    for i,file in enumerate(all_images):
        im=Image.open(FOLDER/file).convert('RGB');im.thumbnail((230,150));x=(i%6)*240;y=(i//6)*190;board.paste(im,(x+(230-im.width)//2,y));draw.text((x+4,y+154),Path(file).stem[:31],fill='white')
    board.save(FOLDER/'Contact_sheet.jpg',quality=92)
    text=['# Centro comercial: referencias y planos','', '34 referencias de superficies/modelos y seis planos generados. Ninguna imagen es una captura de Unity. Las ocho imágenes de Khronos son referencias de sus autores; las seis vistas Bistro muestran sólo geometría. Los veinte muestrarios son texturas originales adaptadas, no veinte edificios nuevos.','', '![Muestrario](Contact_sheet.jpg)','', '## Planos de la generación real','', 'Los seis planos salen de `MallPlan`, con forjados, huecos y tiendas de cada planta; muestran distribución y escala, no iluminación.','']
    for file in blueprints:text+=['![Plano '+Path(file).stem+']('+file+')','']
    text+=['## Referencias de objetos y acabados','']
    for entry in model_refs+finish_refs+previews:
        text+=['### '+Path(entry['file']).stem,'', '![Referencia]('+entry['file']+')','',entry['license']+' · '+', '.join(entry.get('authors',[entry.get('author','Amazon.com, Inc. or its affiliates')]))+'. '+entry.get('description','Source reference.')+'','']
        if entry.get('sourceUrl'):text +=['[Fuente fijada]('+entry['sourceUrl']+')','']
    text+=['## Licencias','', 'Modelos y materiales: licencias, atribución y cambios en `Assets/PoolroomsProcedural/Resources/MallSources/Licenses` y `MallFinishes/Licenses`. Metadatos y hashes junto a los recursos. Los planos son salidas del generador de este proyecto.','']
    (FOLDER/'README.md').write_text('\n'.join(text))
    print(json.dumps({'sourceReferences':34,'actualCorePlanImages':len(blueprints),'notUnityScreenshots':True}))
if __name__=='__main__':main()
