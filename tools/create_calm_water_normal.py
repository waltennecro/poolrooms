"""Deterministic, seamless original water micro-normal. No third-party image input."""
from pathlib import Path
import hashlib,json,numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
size=1024
# Endpoint-inclusive periodic sampling guarantees matching opposite border texels.
u=np.linspace(0,2*np.pi,size,dtype=np.float64);x,z=np.meshgrid(u,u)
dx=np.zeros_like(x);dz=np.zeros_like(x)
for a,b,amplitude,phase in [(3,2,.038,.3),(5,-3,.027,1.1),(11,7,.012,.8),(17,-9,.006,2.1)]:
 wave=np.cos(a*x+b*z+phase)*amplitude
 dx+=a*wave;dz+=b*wave
n=np.stack([-dx,np.ones_like(dx)*5,-dz],axis=-1);n/=np.linalg.norm(n,axis=-1,keepdims=True)
# Tangent +Z is the up direction in a normal map; glTF/Unity importer performs packing.
rgb=np.stack([n[:,:,0],n[:,:,2],n[:,:,1]],axis=-1)
rgb=np.uint8(np.clip(rgb*.5+.5,0,1)*255+.5)
assert np.array_equal(rgb[0],rgb[-1]) and np.array_equal(rgb[:,0],rgb[:,-1])
path=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures/CalmWater_Normal.png'
Image.fromarray(rgb,'RGB').save(path,compress_level=9)
data=path.read_bytes()
report={'size':size,'license':'CC0-1.0','authorship':'Original analytic texture generated for this project; no external image input.','generator':'tools/create_calm_water_normal.py','seamlessBordersVerified':True,'files':{path.relative_to(ROOT).as_posix():{'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}}}
(ROOT/'Verification/calm_water_texture.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
