"""Original neutral gelcoat and advected thin-film water artwork, not downloaded media."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures';SIZE=1024

def build():
    y,x=np.mgrid[:SIZE,:SIZE].astype(float)/SIZE
    streak=np.sin(2*np.pi*(x*9+.1*np.sin(y*2*np.pi)))+.4*np.cos(2*np.pi*(x*21-y*3))
    foam=np.clip((streak-1.0)*2.1,0,1)*(.35+.65*np.sin(y*2*np.pi)**8)
    edge=np.clip(np.minimum(x,1-x)*12,0,1)
    alpha=(.14+foam*.5)*edge
    rgba=np.concatenate((np.ones((*x.shape,3))*(.94+.06*foam[:,:,None]),alpha[:,:,None]),axis=-1)
    height=.00045*np.sin(2*np.pi*(x*11-y*3))+.00012*np.sin(2*np.pi*(x*23+y*7))
    dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*SIZE*.5;dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*SIZE*.5
    normal=np.stack((-dx,-dy,np.ones_like(x)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    mask=np.stack((np.zeros_like(x),np.ones_like(x),np.full_like(x,.5),.96-foam*.08),axis=-1)
    neutral=np.repeat((.88+.001*np.sin(2*np.pi*(x*53+y*61)))[:,:,None],3,axis=-1)
    files={}
    for name,data in [('AquaticGelcoat_Albedo',neutral),('FlumeFlow_Albedo',rgba),('FlumeFlow_Normal',normal*.5+.5),('FlumeFlow_Mask',mask)]:
        path=OUT/(name+'.png');Image.fromarray(np.rint(np.clip(data,0,1)*255).astype('uint8')).save(path,compress_level=9);raw=path.read_bytes();files[path.relative_to(ROOT).as_posix()]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    (ROOT/'Verification/flume_textures.json').write_text(json.dumps({'size':SIZE,'origin':'Original neutral gelcoat and thin film artwork; animated UV advection, not a hydraulic simulation. Existing gelcoat normal/mask maps reused.','files':files},indent=2)+'\n');print('Generated four original 1024px flume images.')
if __name__=='__main__':build()
