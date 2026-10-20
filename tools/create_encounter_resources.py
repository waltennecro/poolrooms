"""Original room-life PBR surfaces and restrained eight-note plucked keyboard; CC0.
Offline generation only, all maps 512px. Mask channels match HDRP metallic/AO/detail/smoothness.
"""
from pathlib import Path
import hashlib,json,wave
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
textures=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures'
audio=ROOT/'Assets/PoolroomsProcedural/Runtime/Resources/EncounterAudio';audio.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(941188);size=512;y,x=np.mgrid[0:size,0:size]/size
files={}
def save(path,data):
 Image.fromarray(np.uint8(np.clip(data,0,1)*255)).save(path,optimize=True)
 files[path.relative_to(ROOT).as_posix()]={'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
for name in ['Oak','Brass','Ceramic','Paper','Linen']:
 fine=rng.normal(0,.012,(size,size));warp=.03*np.sin(y*2*np.pi)+.012*np.sin(y*8*np.pi)
 if name=='Oak':
  grain=np.sin((x+warp)*np.pi*92)+.35*np.sin((x+warp)*np.pi*278);height=.18*grain+.03*np.sin(y*np.pi*8)+fine;base=np.array([.46,.29,.145]);variation=.045*grain+fine;metal=0;smooth=.29
 elif name=='Brass':
  grain=.5*np.sin(y*np.pi*186)+rng.normal(0,.14,(size,size));height=grain*.018;base=np.array([.64,.49,.24]);variation=grain*.035;metal=.9;smooth=.51
 elif name=='Ceramic':
  height=.008*np.sin(x*np.pi*20)*np.sin(y*np.pi*16)+fine*.25;base=np.array([.83,.84,.78]);variation=fine*.4;metal=0;smooth=.66
 elif name=='Paper':
  height=rng.normal(0,.008,(size,size));base=np.array([.88,.85,.74]);variation=height*.7;metal=0;smooth=.08
 else:
  weave=(np.sin(x*np.pi*256)*np.sin(y*np.pi*256))*.06;height=weave+fine;base=np.array([.49,.56,.45]);variation=weave*.28+fine*.6;metal=0;smooth=.12
 dx=(np.roll(height,-1,1)-np.roll(height,1,1))*.8;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*.8
 n=np.stack([-dx,-dy,np.ones_like(dx)],axis=-1);n/=np.linalg.norm(n,axis=-1)[...,None]
 mask=np.stack([np.full_like(x,metal),np.clip(.96-abs(height)*.12,0,1),np.full_like(x,.5),np.clip(smooth+fine,0,1)],axis=-1)
 for suffix,data in [('Albedo',base+variation[...,None]),('Normal',n*.5+.5),('Mask',mask)]:save(textures/('Encounter'+name+'_'+suffix+'.png'),data)
for i,hz in enumerate([261.6256,293.6648,329.6276,349.2282,391.9954,440,493.8833,523.2511]):
 sr=24000;t=np.arange(int(sr*1.4))/sr
 signal=np.zeros_like(t)
 for harmonic in range(1,8):signal+=np.sin(2*np.pi*hz*harmonic*t+.08*harmonic)*np.exp(-t*(4.2+harmonic*1.1))/(harmonic**1.8)
 signal*=np.minimum(1,t/.008)**2;signal[-int(sr*.15):]*=np.linspace(1,0,int(sr*.15))**2
 signal-=signal.mean();signal*=.45/max(abs(signal));signal[0]=signal[-1]=0
 path=audio/('Note'+str(i)+'.wav')
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.rint(signal*32767).astype('<i2').tobytes())
 files[path.relative_to(ROOT).as_posix()]={'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
source=ROOT/'Sources/Encounter';source.mkdir(parents=True,exist_ok=True)
(source/'original.generated.json').write_text(json.dumps(files,indent=2)+'\n')
print('Generated five original 512px PBR map sets and eight bounded musical notes; CC0.')
