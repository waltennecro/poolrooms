"""Reproduce original cedar, oak, slate, wet paving and peaceful rain media.
No photography or third-party model is downloaded by this tool.
"""
from pathlib import Path
import hashlib,json,wave
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures';SIZE=1024

def record(path):
 raw=path.read_bytes();return {'bytes':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}
def build():
 y,x=np.mgrid[:SIZE,:SIZE].astype(float)/SIZE
 micro=sum(np.sin(2*np.pi*(a*x+b*y)+a*.17)/(k+1)**1.3 for k,(a,b) in enumerate([(17,19),(47,67),(109,127),(251,269)]))
 files={}
 for name in ['RainCedar','RainOak','RainSlate','RainPaving']:
  wood=name in ['RainCedar','RainOak'];n=4 if wood else 5
  sx=x*n;sy=y*(1 if wood else 5)
  if not wood:sx+=np.floor(sy)*.5
  edge=np.minimum(sx%1,1-sx%1) if wood else np.minimum.reduce([sx%1,1-sx%1,sy%1,1-sy%1])
  seam=np.clip((.027-edge)/.021,0,1)
  variation=np.sin(np.floor(sx)*7.13+np.floor(sy)*13.71)*.045
  if wood:
   knot=np.sin(2*np.pi*y)*np.sin(2*np.pi*x*2)
   grain=np.sin(2*np.pi*(x*91+.26*np.sin(2*np.pi*y*3)+.21*knot))*.018+.008*micro
   base=np.array([.39,.26,.15] if name=='RainCedar' else [.49,.37,.23]);rgb=base+(grain+variation)[:,:,None]
   height=-seam*.0018+grain*.001
   smooth=np.full_like(x,.24 if name=='RainCedar' else .34)+grain*.5-seam*.12
  else:
   grain=micro*.012+np.sin(2*np.pi*(x*31+y*13))*.006
   base=np.array([.19,.23,.25] if name=='RainSlate' else [.31,.34,.32]);rgb=base+(grain+variation*.4)[:,:,None]
   height=-seam*.0025+grain*.008
   # Broken reflective patches, rather than a uniform chrome mirror.
   wet=(np.sin(2*np.pi*(x*3+y*2))+np.sin(2*np.pi*(y*5-x)))*.07
   smooth=np.clip((.42 if name=='RainSlate' else .64)+wet-seam*.25,.18,.82)
  rgb*=1-seam[:,:,None]*.23
  dx=(np.roll(height,-1,1)-np.roll(height,1,1))*SIZE*.5;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*SIZE*.5
  normal=np.stack((-dx,-dy,np.ones_like(x)),axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
  mask=np.stack((np.zeros_like(x),1-seam*.17,np.full_like(x,.5),smooth),axis=-1)
  for suffix,data in [('Albedo',rgb),('Normal',normal*.5+.5),('Mask',mask)]:
   p=OUT/(name+'_'+suffix+'.png');Image.fromarray(np.rint(np.clip(data,0,1)*255).astype('uint8')).save(p,compress_level=9);files[p.relative_to(ROOT).as_posix()]=record(p)
 (ROOT/'Verification/rain_textures.json').write_text(json.dumps({'size':SIZE,'origin':'Original periodic PBR artwork, no downloaded photographs. Cedar/oak grain and joints, slate roofing and wet paving with bounded microsurface roughness.','files':files},indent=2)+'\n')
 media=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsRain';media.mkdir(exist_ok=True)
 yy,xx=np.mgrid[:256,:64].astype(float);alpha=np.exp(-((xx-31.5)/8)**2)*np.sin(np.pi*yy/255)**2
 rgba=np.ones((256,64,4));rgba[:,:,:3]=.88;rgba[:,:,3]=alpha*.6
 p=media/'RainStreak.png';Image.fromarray(np.rint(rgba*255).astype('uint8')).save(p,compress_level=9)
 rate=22050;count=rate*16;rng=np.random.default_rng(240401);freq=np.fft.rfftfreq(count,1/rate)
 spectrum=(rng.normal(size=len(freq))+1j*rng.normal(size=len(freq)))*np.minimum(freq/180,1)/(1+(freq/3800)**2)**.7;spectrum[0]=0
 sound=np.fft.irfft(spectrum,n=count);sound=sound/max(abs(sound))*.42
 for start in range(0,count,rate//2):
  length=min(1500,count-start);tt=np.arange(length)/rate
  sound[start:start+length]+=.008*np.exp(-tt*65)*np.sin(2*np.pi*(600+(start%173))*tt)
 sound=np.rint(np.clip(sound,-1,1)*32767).astype('<i2')
 p=media/'QuietRainLoop.wav'
 with wave.open(str(p),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(sound.tobytes())
 (ROOT/'Verification/rain_media.json').write_text(json.dumps({'origin':'Original generated rain streak and quiet noise/tap synthesis. No third-party audio.','audioSeconds':16,'audioSampleRate':rate,'files':{p.relative_to(ROOT).as_posix():record(p) for p in sorted(media.iterdir())}},indent=2)+'\n')
 print('Generated 12 original 1024px rain PBR maps, one alpha streak and 16-second PCM rain loop.')
if __name__=='__main__':build()
