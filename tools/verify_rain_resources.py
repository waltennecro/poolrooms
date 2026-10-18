"""Audit original rain media, not native Unity rendering or audio playback."""
from pathlib import Path
import array,hashlib,json,struct,sys,wave,zlib
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/'Verification/rain_media.json').read_text())
for name,expected in manifest['files'].items():
    data=(root/name).read_bytes()
    assert len(data)==expected['bytes'] and hashlib.sha256(data).hexdigest()==expected['sha256'],name
    if name.endswith('.png'):
        assert data[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',data[16:24])==(64,256)
        pos=8;packed=bytearray();ended=False
        while pos<len(data):
            length=struct.unpack('>I',data[pos:pos+4])[0];tag=data[pos+4:pos+8];payload=data[pos+8:pos+8+length]
            assert zlib.crc32(tag+payload)&0xffffffff==struct.unpack('>I',data[pos+8+length:pos+12+length])[0]
            if tag==b'IDAT':packed.extend(payload)
            pos+=length+12
            if tag==b'IEND':ended=True;break
        assert ended and pos==len(data) and len(zlib.decompress(packed))==256*(64*4+1)
    elif name.endswith('.wav'):
        with wave.open(str(root/name)) as audio:
            assert audio.getnchannels()==1 and audio.getsampwidth()==2 and audio.getframerate()==22050 and audio.getnframes()==352800
            samples=array.array('h',audio.readframes(audio.getnframes()))
            if sys.byteorder!='little':samples.byteswap()
            assert max(map(abs,samples))<20000 and sum(s*s for s in samples)/len(samples)>10000,'Empty or clipped rain audio'
    else:raise AssertionError('Unrecognized rain media')
print('Rain media: two original resource hashes, full alpha PNG/CRC and nonempty bounded 16-second mono PCM verified. Native playback/rendering unrun.')
