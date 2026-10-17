"""Original periodic granular sand PBR maps; no reference photograph is copied."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]
out=root/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures'
n=1024;random=np.random.default_rng(48048)
noise=random.normal(0,1,(n,n));soft=(noise+np.roll(noise,1,0)+np.roll(noise,-1,0)+np.roll(noise,1,1)+np.roll(noise,-1,1))*.2
height=soft*.00015+noise*.000035
dx=(np.roll(height,-1,1)-np.roll(height,1,1))/(2*1.2/n);dy=(np.roll(height,-1,0)-np.roll(height,1,0))/(2*1.2/n)
normal=np.stack((-dx,dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
colour=np.clip(np.array([.72,.66,.52])[None,None,:]+soft[...,None]*.023+noise[...,None]*.009,0,1)
mask=np.stack((np.zeros_like(dx),np.clip(.98+soft*.008,.92,1),np.ones_like(dx),np.clip(.14+soft*.018,.08,.22)),axis=-1)
for kind,data in [('Albedo',colour),('Normal',normal*.5+.5),('Mask',mask)]:Image.fromarray(np.uint8(np.clip(data,0,1)*255+.5)).save(out/('QuietSand_'+kind+'.png'))
files=sorted(out.glob('QuietSand_*.png'))
report={'source':'Original generated granular material','size':n,'files':{str(p.relative_to(root)):{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files}}
(root/'Verification/quiet_sand_textures.json').write_text(json.dumps(report,indent=2)+'\n')
print('Generated 3 original 1024px sand PBR maps')
