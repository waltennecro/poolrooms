"""Audit shipped Encuentro media, licences, actual file hashes and import contracts."""
import hashlib
import json
from pathlib import Path
import wave
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/Encounter'

def check_manifest(name, expected):
    data=json.loads((SOURCE/name).read_text())
    assert len(data)==expected,(name,len(data))
    for name,info in data.items():
        path=ROOT/name
        assert path.is_file() and path.stat().st_size==info['bytes'],name
        assert hashlib.sha256(path.read_bytes()).hexdigest()==info['sha256'],name
    return data

original=check_manifest('original.generated.json',23)
icons=check_manifest('icons.generated.json',11)
sources=json.loads((SOURCE/'icons.lock.json').read_text())
assert len(sources)==11
for name,info in sources.items():
    data=(SOURCE/name).read_bytes()
    assert len(data)==info['bytes'] and hashlib.sha256(data).hexdigest()==info['sha256']
    assert info['license']=='CC-BY-3.0' and info['author'] in ('Lorc','Delapouite')
    assert '/82d948812bfe3f269ef8f731dcdb07b08160edc4/' in info['url']
for name in icons:
    im=Image.open(ROOT/name)
    assert im.mode=='RGBA' and im.size==(96,96),name
    alpha=np.asarray(im)[:,:,3]
    assert alpha[0,0]==0 and alpha.max()==255 and np.count_nonzero(alpha)>100,name
for name in original:
    path=ROOT/name
    if path.suffix=='.png':
        im=Image.open(path);a=np.asarray(im)
        assert im.size==(512,512) and a.shape[2]>=3,name
        if '_Normal' in name:
            n=a[:,:,:3].astype(float)/127.5-1
            lengths=np.linalg.norm(n,axis=2)
            assert lengths.min()>.98 and lengths.max()<1.02,name
        if '_Mask' in name:
            assert a.shape[2]==4 and a[:,:,1].min()>150 and a[:,:,2].min()>100,name
            if 'Brass' in name:assert a[:,:,0].mean()>200,name
            else:assert a[:,:,0].max()==0,name
    else:
        with wave.open(str(path)) as w:
            assert w.getnchannels()==1 and w.getsampwidth()==2 and w.getframerate()==24000,name
            assert 1<w.getnframes()/w.getframerate()<2,name
            a=np.frombuffer(w.readframes(w.getnframes()),dtype='<i2').astype(float)/32768
            assert .05<np.max(np.abs(a))<.6 and abs(a[0])<.001 and abs(a[-1])<.001,name
            assert np.sqrt(np.mean(a*a))>.005,name
legal=ROOT/'Assets/PoolroomsProcedural/Resources/EncounterSources/Licenses'
notice=(legal/'NOTICE.txt').read_text()
for expected in ['Lorc','Delapouite','CC BY 3.0','CC0','No downloaded 3D models']:assert expected in notice
for name in ['CC-BY-3.0.txt','CC0-1.0.txt']:assert (legal/name).stat().st_size>500
for p in (ROOT/'Assets/PoolroomsProcedural/Runtime').glob('PoolroomsInventory*.cs'):
    assert 'PlayerPrefs' not in p.read_text(),p
assert 'EncounterSources' in (ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsAssetCredits.txt').read_text()
print('PASS: 11 attributed transparent icons, 5 original PBR sets (15 maps), 8 original PCM notes; source and generated hashes, licences and no inventory disk-save calls. Native rendering/audio unrun.')
