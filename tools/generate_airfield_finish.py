"""Original periodic asphalt PBR maps; no reference photos or third-party textures."""
from pathlib import Path
import hashlib,json
import numpy as np
from PIL import Image
r=Path(__file__).resolve().parents[1];size=1024;rng=np.random.default_rng(180081)
y,x=np.mgrid[0:size,0:size];grain=rng.normal(0,1,(size,size));aggregate=grain*.45
for dx,dy,weight in [(1,0,.12),(-1,0,.12),(0,1,.12),(0,-1,.12)]:aggregate+=np.roll(grain,(dy,dx),(0,1))*weight
cloud=np.sin(x*2*np.pi/size*5)*np.sin(y*2*np.pi/size*3)*.6
base=np.clip(54+aggregate*5+cloud,38,71);albedo=np.stack((base*.94,base*.98,base*1.04),axis=-1)
dx=(np.roll(aggregate,-1,1)-np.roll(aggregate,1,1))*.045;dy=(np.roll(aggregate,-1,0)-np.roll(aggregate,1,0))*.045
normal=np.stack((-dx,dy,np.ones_like(dx)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
mask=np.zeros((size,size,4));mask[:,:,1]=np.clip(.96+aggregate*.015,.85,1);mask[:,:,2]=1;mask[:,:,3]=np.clip(.12+aggregate*.025,.05,.2)
files={}
for name,array in [('Albedo',albedo),('Normal',(normal*.5+.5)*255),('Mask',mask*255)]:
 p=r/f'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures/AirfieldAsphalt_{name}.png';Image.fromarray(np.uint8(np.clip(array,0,255)+.5)).save(p)
 files[p.relative_to(r).as_posix()]={'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
(r/'Verification/airfield_finish_textures.json').write_text(json.dumps({'size':size,'provenance':'Original periodic numerical asphalt by this project','files':files},indent=2)+'\n')
