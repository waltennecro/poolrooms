"""Reproducible 24 kHz PCM sound design. Requires ffmpeg and numpy; no runtime synthesis.
Two CC0 Kenney contact recordings; remaining layers original analytic/non-vocal audio.
"""
from pathlib import Path
import hashlib,json,subprocess,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/PoolroomsProcedural/Runtime/Resources/PoolroomsAudio'
SR=24000
rng=np.random.default_rng(20261020)
manifest={}
def decode(name):
    data=subprocess.check_output(['ffmpeg','-v','error','-i',str(ROOT/'Sources/ImmersiveAudio'/name),'-ac','1','-ar',str(SR),'-f','f32le','-'])
    return np.frombuffer(data,dtype='<f4').astype(float)
def band(x,lo,hi):
    f=np.fft.rfftfreq(len(x),1/SR);w=(1-np.exp(-(f/max(1,lo))**4))*np.exp(-(f/hi)**4)
    return np.fft.irfft(np.fft.rfft(x)*w,n=len(x))
def noise(n,lo=90,hi=6000):return band(rng.normal(size=n),lo,hi)
def norm(x,rms=.14,peak=.7):
    x=x-np.mean(x,axis=0);v=np.sqrt(np.mean(x*x));x=x*(rms/max(1e-12,v));x*=min(1,peak/max(1e-12,np.max(np.abs(x))));return x

def envelope(x,attack=.009,release=.06):
    w=np.ones(len(x));a=min(int(SR*attack),len(x)//2);r=min(int(SR*release),len(x)//2)
    w[:a]=np.sin(np.linspace(0,np.pi/2,a))**2;w[-r:]=np.cos(np.linspace(0,np.pi/2,r))**2
    return x*w[:,None] if x.ndim==2 else x*w

def save(name,x,derived=False,loop=False):
    x=norm(x,.1 if loop else .15,.65);x=x if loop else envelope(x)
    p=OUT/(name+'.wav');p.parent.mkdir(parents=True,exist_ok=True)
    pcm=np.rint(np.clip(x,-1,1)*32767).astype('<i2')
    with wave.open(str(p),'wb') as w:w.setnchannels(1 if x.ndim==1 else x.shape[1]);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
    manifest[p.relative_to(ROOT).as_posix()]={'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'bytes':p.stat().st_size,'seconds':len(x)/SR,'loop':loop,'provenance':'Kenney CC0 recording + original layers' if derived else 'Original non-vocal synthesis'}

def loop(name,lo,hi,seconds=16,tone=0):
    n=SR*seconds;t=np.arange(n)/SR
    # Every component is periodic at the exact loop length; no periodic gate/click.
    def channel():
        x=noise(n,lo,hi);x*=1+.12*np.sin(2*np.pi*t/seconds)+.07*np.cos(4*np.pi*t/seconds)
        if tone:x+=.06*np.sin(2*np.pi*(round(tone*seconds)/seconds)*t)
        return x
    save(name,np.column_stack([channel(),channel()]),loop=True)
walk=decode('walking.ogg');landing=decode('land.ogg')
for family in ['Tile','Metal','WetTile','Wade','Horizonte/SoftStep','Horizonte/GrassStep','Horizonte/WoodStep','Horizonte/RemoteStep']:
    for i,onset in enumerate([.0,.20,.40]):
        n=int(SR*.32);x=np.zeros(n);raw=walk[int(onset*SR):int((onset+.17)*SR)];x[:len(raw)]=raw
        x=norm(x,.07);t=np.arange(n)/SR
        if 'Metal' in family:
            for hz in [340,697,1160]:x+=.035*np.sin(2*np.pi*(hz+i*17)*t)*np.exp(-t*28)
        if family in ['Wade','WetTile']:x+=norm(noise(n,300,6500),.045)*np.exp(-t*(9 if family=='Wade' else 22))
        if 'Soft' in family:x=band(x,70,1800)
        if 'Grass' in family:x=band(x,130,2500)+noise(n,800,4200)*np.exp(-t*25)*.04
        if 'Wood' in family:x=band(x,90,4000)+.03*np.sin(2*np.pi*(180+i*13)*t)*np.exp(-t*20)
        if 'Remote' in family:x=band(x,90,1600)
        save(family+str(i),x,True)
for i in range(3):
    t=np.arange(int(SR*.65))/SR
    # A small water plink plus a broad, low splash; avoid piercing pure high sine drops.
    x=noise(len(t),300,6500)*np.exp(-t*16)*.09+np.sin(2*np.pi*(530*t+85*(1-np.exp(-t*12))))*np.exp(-t*17)*.12
    save('Drip'+str(i),x)
for i in range(2):
    t=np.arange(int(SR*(.9+.15*i)))/SR
    x=noise(len(t),140,6000)*np.exp(-t*5)*(1+.12*np.sin(t*42))
    save('Splash'+str(i),x)
for name,lo,hi,tone in [('VentilationLoop',70,1600,0),('WaterLoop',180,6400,0),('SlideTileLoop',190,4000,0),('SlideMetalLoop',230,5500,0),('SlideSoftLoop',100,2200,0),('SlideWetLoop',200,6200,0),('Horizonte/QuietIndoorLoop',90,1350,0),('Horizonte/GardenWindLoop',150,4200,0),('Horizonte/UneasyAirLoop',80,700,91),('Horizonte/FlightWindLoop',130,5500,0)]:loop(name,lo,hi,tone=tone)
for name,seconds,lo,hi in [('DashAir',.45,180,6500),('ClothMovement',.3,400,3500),('ObjectHandling',.3,200,2200),('AirMovement',3.8,100,1800)]:
    n=int(seconds*SR);x=noise(n,lo,hi)*np.sin(np.linspace(0,np.pi,n))**2
    save('Horizonte/'+name,x)
for name,soft in [('LandContact',False),('SoftImpact',True),('HardImpact',False)]:
    n=int(SR*.4);x=np.zeros(n);x[:len(landing)]=landing;t=np.arange(n)/SR
    x=band(norm(x,.08),65,1500 if soft else 3800)+.05*np.sin(2*np.pi*(120 if soft else 190)*t)*np.exp(-t*24)
    save('Horizonte/'+name,x,True)
for name,tones in [('DistantCreak',[170,267,351]),('PipeResonance',[122,246,488])]:
    n=SR*4;t=np.arange(n)/SR;x=noise(n,100,2000)*.015
    for k,hz in enumerate(tones):x+=np.sin(2*np.pi*(hz*t+.4*np.sin(t*(1+k*.13))))*(.08/(k+1))
    x*=np.sin(np.linspace(0,np.pi,n))**2
    save('Horizonte/'+name,x)
(ROOT/'Sources/ImmersiveAudio/generated.lock.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(f'Generated {len(manifest)} bounded PCM clips; two original Kenney source recordings.')
