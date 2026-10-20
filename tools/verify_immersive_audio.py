"""Audit actual shipped PCM, provenance and resource references; not a listening/Unity test."""
from pathlib import Path
import hashlib,json,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Sources/ImmersiveAudio'
for name,record in json.loads((source/'sources.lock.json').read_text()).items():
    data=(source/name).read_bytes()
    assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
assert 'Sound effects _(CC0 licensed)_' in (source/'README.md').read_text()
manifest=json.loads((source/'generated.lock.json').read_text());assert len(manifest)==48
actual={p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/PoolroomsProcedural/Runtime/Resources/PoolroomsAudio').rglob('*.wav')}
assert actual==set(manifest),'Untracked or missing audio clip'
frames=0;max_peak=0;loops=0
for name,record in manifest.items():
    path=ROOT/name;data=path.read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
    with wave.open(str(path)) as w:
        assert w.getsampwidth()==2 and w.getframerate()==24000 and w.getcomptype()=='NONE',name
        channels=w.getnchannels();assert channels==(2 if record['loop'] else 1),name
        x=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').reshape(-1,channels).astype(float)/32768
        assert abs(w.getnframes()/24000-record['seconds'])<1e-8
        frames+=x.size
    peak=float(np.max(np.abs(x)));rms=float(np.sqrt(np.mean(x*x)));max_peak=max(max_peak,peak)
    assert .01<rms<=.151 and peak<=.651 and np.max(np.abs(np.mean(x,axis=0)))<.001,name
    assert np.all(np.isfinite(x)),name
    if record['loop']:
        loops+=1;assert len(x)==16*24000
        # A periodic sampled waveform has a normal inter-sample delta at its seam.
        seam=np.abs(x[0]-x[-1]);steps=np.abs(np.diff(x,axis=0))
        assert np.all(seam<np.quantile(steps,.995,axis=0)*2+.002),'Discontinuous seam: '+name
        assert abs(float(np.sqrt(np.mean(x[:24000]**2)))-float(np.sqrt(np.mean(x[-24000:]**2))))<.04,name
    else:assert np.max(np.abs(x[0]))==0 and np.max(np.abs(x[-1]))==0,'Contact endpoints need silence: '+name
assert loops==10
notice=ROOT/'Assets/PoolroomsProcedural/Resources/ImmersiveAudioSources/Licenses/NOTICE.txt'
assert 'CC0' in notice.read_text() and 'Two downloaded recordings' in notice.read_text()
assert notice.read_text().strip() in (ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsAssetCredits.txt').read_text()
# Verify every named runtime clip is real, without treating a source-text check as playback.
runtime=(ROOT/'Assets/PoolroomsProcedural/Runtime/PoolroomsSoundscape.cs').read_text()
for clip in ['QuietIndoorLoop','GardenWindLoop','UneasyAirLoop','FlightWindLoop','DistantCreak','PipeResonance','AirMovement','DashAir','ClothMovement','LandContact','ObjectHandling','SoftImpact','HardImpact']:
    assert '"'+clip+'"' in runtime and any(p.endswith('/Horizonte/'+clip+'.wav') for p in manifest),clip
print(json.dumps({'result':'PASS','downloaded_recordings':2,'shipped_clips':len(manifest),'loops':loops,'pcm_peak_max':round(max_peak,6),'decoded_float_memory_bytes':frames*4,'unity_playback':'unrun','listening_review':'unrun'}))
