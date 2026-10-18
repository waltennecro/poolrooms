"""Deterministic original pool mosaics, glazed ceramic and gel-coated fibreglass.
Not downloaded photography. World-space UV metres preserve player-scale tile size.
"""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]; OUT=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures'; SIZE=1024

def build():
    y,x=np.mgrid[:SIZE,:SIZE].astype(float)/SIZE
    fine=sum(np.sin(2*np.pi*(a*x+b*y)+a*.13)/(k+1)**1.5 for k,(a,b) in enumerate([(13,17),(31,43),(97,83),(181,223)]))*.15
    files={}
    for name,colour,tile in [('AquaticMosaic',[.46,.66,.62],True),('AquaticCeramic',[.79,.79,.7],True),('AquaticFiberglass',[.43,.7,.61],False),('AquaticFiberglassCoral',[.78,.39,.44],False)]:
        n=20 if name=='AquaticMosaic' else 4
        edge=np.minimum.reduce([(x*n)%1,1-(x*n)%1,(y*n)%1,1-(y*n)%1])
        grout=np.clip((.028-edge)/.015,0,1) if tile else np.zeros_like(x)
        bevel=np.clip((.052-edge)/.026,0,1) if tile else np.zeros_like(x)
        grain=.00018*fine
        height=grain-.0015*bevel-.0018*grout if tile else grain*.32+.0002*np.sin(2*np.pi*(3*x+2*y))
        dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*SIZE*.5;dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*SIZE*.5
        normal=np.stack((-dx,-dy,np.ones_like(x)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
        variation=(np.sin(np.floor(x*n)*12.2+np.floor(y*n)*7.7)*.018 if tile else fine*.008)
        rgb=np.clip(np.array(colour)+variation[:,:,None],0,1)
        rgb=rgb*(1-grout[:,:,None]*.34)
        smooth=(.65 if name=='AquaticMosaic' else .4 if name=='AquaticCeramic' else .76)-grout*.44+fine*.02
        mask=np.stack((np.zeros_like(x),1-grout*.15,np.full_like(x,.5),smooth),axis=-1)
        for suffix,data in [('Albedo',rgb),('Normal',normal*.5+.5),('Mask',mask)]:
            p=OUT/(name+'_'+suffix+'.png');Image.fromarray(np.rint(np.clip(data,0,1)*255).astype('uint8')).save(p,compress_level=9);raw=p.read_bytes();files[p.relative_to(ROOT).as_posix()]={'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
    (ROOT/'Verification/aquatic_textures.json').write_text(json.dumps({'size':SIZE,'origin':'Original procedural artwork, metre UVs: 5cm pool mosaic and 25cm ceramic. Previously licensed furnishings reused; no new downloaded model counted.','files':files},indent=2)+'\n');print('Generated twelve original 1024px aquatic maps.')
if __name__=='__main__':build()
