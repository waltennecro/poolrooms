"""Create original seamless field wind and soft crop rustles; no borrowed samples."""
import hashlib,json,wave
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[1];DEST=ROOT/'Assets/PoolroomsProcedural/Resources/Level10/Audio';RATE=44100

def band(n,seed,low,high):
 rng=np.random.default_rng(seed);f=np.fft.rfftfreq(n,1/RATE);s=np.fft.rfft(rng.normal(size=n));e=np.zeros_like(f)
 keep=(f>=low)&(f<=high);e[keep]=np.sin(np.pi*(f[keep]-low)/(high-low))**2/np.sqrt(f[keep]);v=np.fft.irfft(s*e,n)
 return v/max(abs(v).max(),1e-9)
def write(name,v,loop):
 v=v-v.mean();v=v/max(abs(v).max(),1)*.70
 if loop:v-=np.linspace(-(v[-1]-v[0])*.5,(v[-1]-v[0])*.5,len(v))
 else:
  fade=min(2205,len(v)//4);v[:fade]*=np.linspace(0,1,fade);v[-fade:]*=np.linspace(1,0,fade)
 pcm=np.clip(v*32767,-32766,32766).astype('<i2');path=DEST/(name+'.wav')
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(pcm.tobytes())
 return dict(path=path.name,bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),seconds=len(pcm)/RATE,loop=loop,seamStep=abs(int(pcm[0])-int(pcm[-1])) if loop else 0,peak=float(abs(v).max()),rms=float(np.sqrt(np.mean(v*v))))
def main():
 DEST.mkdir(parents=True,exist_ok=True);duration=32;n=duration*RATE;t=np.arange(n)/RATE
 wind=band(n,101001,90,1900)*(.52+.16*np.sin(t*2*np.pi/duration)+.08*np.cos(t*6*np.pi/duration))
 wind+=band(n,101002,1200,5400)*(.06+.02*np.cos(t*4*np.pi/duration));files=[write('FieldWind',wind,True)]
 for i in range(3):
  n=int((1.8+i*.25)*RATE);t=np.arange(n)/RATE;envelope=np.sin(np.pi*np.arange(n)/(n-1))**2
  rustle=band(n,101010+i,850,7200)*envelope*(.27+.06*np.sin(t*2*np.pi*(3.2+i*.6)))
  files.append(write('GrainRustle'+str(i),rustle,False))
 record=dict(author='Poolrooms project original procedural foley',license='CC0-1.0',sampleRate=RATE,channels=1,bits=16,sourceSamples=0,voicesOrWhispers=False,startleStingers=False,nativeListeningVerified=False,tool='tools/create_level10_audio.py',files=files)
 (DEST/'manifest.json').write_text(json.dumps(record,indent=2)+'\n');print(json.dumps(dict(clips=len(files),bytes=sum(e['bytes'] for e in files))))
if __name__=='__main__':main()
