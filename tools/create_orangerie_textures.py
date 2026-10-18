"""Original deterministic Orangerie finishes, not edits of downloaded photographs.
Albedo is sRGB; tangent normals and HDRP masks (metal/AO/detail/smoothness) are linear.
Run with the pinned Verification/requirements-art-tools.txt versions.
"""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures'
SIZE=1024

def build():
    out={}
    v,u=np.mgrid[:SIZE,:SIZE].astype(np.float64)/SIZE
    rng=np.random.default_rng(1902500)
    grain=np.zeros((SIZE,SIZE));broad=grain.copy()
    for octave,freq in enumerate((2,5,11,29,71,179)):
        for wave in range(4):
            x=int(rng.integers(1,freq+1));y=int(rng.integers(1,freq+1));phase=rng.uniform(0,2*np.pi)
            term=np.sin(2*np.pi*(x*u+y*v)+phase)/(4*(1.6**octave))
            grain+=term
            if octave<2:broad+=term
    for name in ('GardenLimestone','GardenLimewash','GardenTerracotta','GardenLeaves'):
        if name=='GardenLimestone':
            # 4 x 4 staggered honed limestone blocks, shallow recessed mortar.
            x=u*4+(np.floor(v*4)%2)*.5;y=v*4
            edge=np.minimum.reduce((x%1,1-x%1,y%1,1-y%1))
            joint=np.clip((.012-edge)/.007,0,1)
            tile=(np.sin(np.floor(x)*13+np.floor(y)*37)+1)*.012
            pores=np.clip(-grain-.37,0,.5)
            height=.035*grain-.12*joint-.12*pores
            base=np.array([.73,.69,.58]);variation=.035*grain+.015*broad+tile-.065*joint
            smooth=.44+.04*grain-.22*joint;ao=1-.18*joint-.12*pores;strength=2.3
        elif name=='GardenLimewash':
            height=.012*grain+.009*broad;base=np.array([.84,.82,.74]);variation=.023*broad+.009*grain
            smooth=.12+.025*grain;ao=np.full_like(u,1);strength=1.0
        elif name=='GardenTerracotta':
            pores=np.clip(-grain-.3,0,.6)
            height=.028*grain-.05*pores;base=np.array([.51,.265,.15]);variation=.025*broad+.035*grain
            smooth=.18+.04*grain;ao=1-.1*pores;strength=1.8
        else:
            midrib=np.exp(-((u-.5)/.013)**2)
            veins=np.exp(-(np.sin((v*12+np.abs(u-.5)*4)*np.pi)/.12)**2)*np.clip(1-np.abs(u-.5)*2,0,1)
            height=.015*midrib+.006*veins+.001*grain
            base=np.array([.14,.29,.075]);variation=.035*midrib+.012*veins+.012*broad
            smooth=.32+.09*midrib+.03*grain;ao=1-.06*veins;strength=2.4
        albedo=np.clip(base[None,None,:]+variation[:,:,None],0,1)
        dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*SIZE*.5
        dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*SIZE*.5
        normal=np.stack((-dx*strength,-dy*strength,np.ones_like(u)),axis=-1)
        normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
        mask=np.stack((np.zeros_like(u),np.clip(ao,0,1),np.full_like(u,.5),np.clip(smooth,0,1)),axis=-1)
        for suffix,array in [('Albedo',albedo),('Normal',normal*.5+.5),('Mask',mask)]:
            path=OUT/(name+'_'+suffix+'.png')
            Image.fromarray(np.rint(np.clip(array,0,1)*255).astype('uint8')).save(path,compress_level=9)
            data=path.read_bytes();out[path.relative_to(ROOT).as_posix()]={'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
    manifest={'size':SIZE,'origin':'Original procedural artwork created for this repository; no third-party image inputs.','generator':'tools/create_orangerie_textures.py','maskChannels':'R metallic (0), G occlusion, B detail (0.5), A smoothness','heightDisplacementEnabled':False,'files':out}
    (ROOT/'Verification/orangerie_textures.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('Created 12 original 1024px Orangerie PBR maps.')
if __name__=='__main__':build()
