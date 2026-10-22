"""Build the original seamless 64-cubed cloud-density volume (not a photo or model)."""
from pathlib import Path
import hashlib,json
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
def density():
 size=64;z,y,x=np.indices((size,size,size),dtype=np.float64);field=np.zeros_like(x)
 # Periodic horizontal smooth value noise; vertically shaped cloud banks.
 for frequency,weight in [(3,1),(6,.5),(12,.25),(24,.125)]:
  xx=x*frequency/size;yy=y*frequency/size;zz=z*frequency/size
  ix=np.floor(xx).astype(np.int64);iy=np.floor(yy).astype(np.int64);iz=np.floor(zz).astype(np.int64)
  fx=xx-ix;fy=yy-iy;fz=zz-iz;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);fz=fz*fz*(3-2*fz)
  noise=np.zeros_like(x)
  for dx in (0,1):
   for dy in (0,1):
    for dz in (0,1):
     h=((ix+dx)%frequency)*374761393+((iy+dy)%frequency)*668265263+((iz+dz)%frequency)*2147483647+630063
     h=(h^(h>>13))*1274126177;h=(h^(h>>16))&0xffffffff
     noise+=(h/4294967295.0)*(fx if dx else 1-fx)*(fy if dy else 1-fy)*(fz if dz else 1-fz)
  field+=noise*weight
 envelope=np.sin(np.pi*(y+.5)/size)**1.4
 return np.clip((field/1.875-.33)*3.7*envelope,0,1).reshape(-1).astype(np.float64).__mul__(255).astype(np.uint8).tobytes()
def main():
 folder=ROOT/'Assets/PoolroomsProcedural/Resources/Sky63';folder.mkdir(exist_ok=True)
 blob=density();(folder/'CloudDensity.bytes').write_bytes(blob)
 meta={'author':'Poolrooms contributors','license':'CC0-1.0','licenseUrl':'https://creativecommons.org/publicdomain/zero/1.0/','kind':'Original periodic 3D density, no imported photograph/model','size':[64,64,64],'layout':'x fastest, then y, then z; uint8 density copied to RGBA channels','bytes':len(blob),'sha256':hashlib.sha256(blob).hexdigest(),'generator':'tools/build_sky63_clouds.py'}
 (folder/'Provenance.json').write_text(json.dumps(meta,indent=2)+'\n');print(json.dumps(meta))
if __name__=='__main__':main()
