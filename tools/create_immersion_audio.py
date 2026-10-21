"""Original non-vocal acoustic foley synthesis, deterministic PCM; no external recordings."""
from pathlib import Path
import hashlib,json,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/PoolroomsProcedural/Runtime/Resources/ImmersionAudio'
OUT.mkdir(parents=True,exist_ok=True)
NAMES=['Cup','Pour','Page','Paper','Pencil','Chime','Music','Latch','Parcel','Tile','Switch','Fabric','WaterDrop','Leaves','Duct','Ceramic']
manifest={}
for index,name in enumerate(NAMES):
 for variant in range(2):
  sr=24000;length=1.6 if name in ('Chime','Music','Duct') else 1.1 if name=='Pour' else .65
  t=np.arange(round(length*sr))/sr;rng=np.random.default_rng(3189+index*31+variant)
  noise=rng.standard_normal(len(t));smooth=np.convolve(noise,np.ones(9)/9,mode='same')
  if name in ('Cup','Tile','Ceramic','Chime','Music'):
   f=[720,940,520,660,880][['Cup','Tile','Ceramic','Chime','Music'].index(name)]*(1+variant*.12)
   x=sum(np.sin(2*np.pi*f*ratio*t)*np.exp(-t*decay)*gain for ratio,decay,gain in [(1,5,.5),(2.71,9,.2),(4.1,14,.1)])
   if name in ('Chime','Music'):x+=np.sin(2*np.pi*f*1.5*t)*np.exp(-t*3)*.15
  elif name=='Pour':
   x=smooth*.25*(.55+.3*np.sin(2*np.pi*9*t))+np.sin(2*np.pi*(620*t+130*t*t))*.07*np.exp(-t*2)
   for at in (.12,.31,.53,.72):x+=np.sin(2*np.pi*1100*t)*np.exp(-np.maximum(t-at,0)*70)*(t>=at)*.025
  elif name=='WaterDrop':x=np.sin(2*np.pi*(1300*t-550*t*t))*np.exp(-t*22)*.7+smooth*np.exp(-t*35)*.2
  elif name in ('Latch','Switch'):x=smooth*np.exp(-t*50)*.7+np.sin(2*np.pi*(220+variant*70)*t)*np.exp(-t*35)*.25
  elif name=='Duct':x=smooth*.15+np.sin(2*np.pi*92*t)*.025+np.sin(2*np.pi*184*t)*.008
  else:
   rate={'Page':9,'Paper':16,'Pencil':25,'Parcel':7,'Fabric':5,'Leaves':11}[name]
   pulse=(.5+.5*np.sin(2*np.pi*rate*t+variant))**2
   x=(smooth*.35+(noise-smooth)*.08)*pulse
  # All contacts start/end in silence. No clipped or unbounded normalization.
  envelope=np.minimum(1,t/.015)*np.minimum(1,(length-t)/.08)*np.exp(-t*(.2 if name=='Duct' else .8))
  x=x*envelope;x-=np.mean(x);x*=envelope
  peak=np.max(np.abs(x));x=x/max(peak,.001)*(.25 if name in ('Duct','Leaves') else .46)
  x[:32]*=np.linspace(0,1,32);x[-64:]*=np.linspace(1,0,64)
  pcm=np.rint(x*32767).astype('<i2');path=OUT/(name+str(variant)+'.wav')
  with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(pcm.tobytes())
  data=path.read_bytes();manifest[path.relative_to(ROOT).as_posix()]={'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest(),'seconds':len(pcm)/sr,'cue':name,'variant':variant}
source=ROOT/'Sources/Immersion';source.mkdir(exist_ok=True)
(source/'audio.generated.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Created 32 original, non-vocal PCM foley clips. CC0. No downloaded recordings.')
