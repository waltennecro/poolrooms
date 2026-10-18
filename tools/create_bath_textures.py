"""Original seamless travertine finishes; not downloaded photographs or model counts."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures';SIZE=1024

def build():
    y,x=np.mgrid[:SIZE,:SIZE].astype(float)/SIZE
    grain=sum(np.sin(2*np.pi*(a*x+b*y)+a*.37)/(i+1)**1.5 for i,(a,b) in enumerate([(3,7),(9,11),(23,19),(53,61),(127,151)]))*.1
    veins=np.sin(2*np.pi*y*13+grain*2)+.32*np.sin(2*np.pi*y*41+grain*4)
    pores=np.maximum(0,np.sin(2*np.pi*(x*97+y*29))*np.cos(2*np.pi*(x*31-y*73))-.78)**2*12
    xx=x*3+(np.floor(y*3)%2)*.5;yy=y*3;edge=np.minimum.reduce([xx%1,1-xx%1,yy%1,1-yy%1]);joint=np.clip((.009-edge)/.005,0,1)
    height=.015*grain+.002*veins-.018*pores-.035*joint
    dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*SIZE*.5;dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*SIZE*.5
    normal=np.stack((-dx,-dy,np.ones_like(x)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    files={}
    for name,wet in [('BathTravertineDry',False),('BathTravertineWet',True)]:
        colour=np.array([.75,.69,.55])*(.91 if wet else 1);variation=.035*grain+.014*veins-.075*pores-.075*joint
        rgb=np.clip(colour+variation[:,:,None],0,1);mask=np.stack((np.zeros_like(x),np.clip(1-.25*pores-.2*joint,0,1),np.full_like(x,.5),np.clip((.68 if wet else .4)+.035*grain-.22*pores-.2*joint,0,1)),axis=-1)
        for suffix,data in [('Albedo',rgb),('Normal',normal*.5+.5),('Mask',mask)]:
            p=OUT/(name+'_'+suffix+'.png');Image.fromarray(np.rint(np.clip(data,0,1)*255).astype('uint8')).save(p,compress_level=9);raw=p.read_bytes();files[p.relative_to(ROOT).as_posix()]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    (ROOT/'Verification/bath_textures.json').write_text(json.dumps({'size':SIZE,'origin':'Original procedural stone artwork; no external images. Online wicker PBR is separately audited under BathSources.','files':files},indent=2)+'\n');print('Generated six original 1024px bath stone maps.')
if __name__=='__main__':build()
