"""Audit actual original foley PCM and licensing; no native playback claim."""
from pathlib import Path
import hashlib,json,wave
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'Sources/Immersion/audio.generated.json').read_text());assert len(manifest)==32
actual={p.relative_to(ROOT).as_posix() for p in (ROOT/'Assets/PoolroomsProcedural/Runtime/Resources/ImmersionAudio').glob('*.wav')};assert actual==set(manifest)
peak=0
for name,record in manifest.items():
 p=ROOT/name;data=p.read_bytes();assert len(data)==record['bytes'] and hashlib.sha256(data).hexdigest()==record['sha256'],name
 with wave.open(str(p)) as w:
  assert w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==24000
  assert w.getnframes()/24000==record['seconds']
  x=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
  assert x[0]==0 and x[-1]==0 and .01<np.max(np.abs(x))<=.461
  assert abs(np.mean(x))<.005 and np.all(np.isfinite(x)) and np.sqrt(np.mean(x*x))>.005
  peak=max(peak,float(np.max(np.abs(x))))
notice=ROOT/'Assets/PoolroomsProcedural/Resources/ImmersionSources/Licenses/NOTICE.txt'
assert 'CC0' in notice.read_text() and 'No external recordings' in notice.read_text()
assert notice.read_text().strip() in (ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsAssetCredits.txt').read_text()
assert (notice.parent/'CC0-1.0.txt').stat().st_size>500
runtime=(ROOT/'Assets/PoolroomsProcedural/Runtime/PoolroomsSoundscape.cs').read_text()
assert 'new AudioSource[4]' in runtime and 'ImmersionAudio/' in runtime
print(json.dumps({'result':'PASS','originalPcmClips':32,'distinctCues':16,'variantsPerCue':2,'downloadedRecordings':0,'peak':peak,'extraAudioSources':0,'nativePlayback':'unrun'}))
