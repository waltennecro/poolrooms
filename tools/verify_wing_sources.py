"""Audit licensed wing menu resources, provenance and actual selector integration."""
from pathlib import Path
import hashlib
import json
import struct
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Sources/Wings';DEST=ROOT/'Assets/PoolroomsProcedural/Resources/WingSources'
lock=json.loads((SOURCE/'sources.lock.json').read_text())
catalog=json.loads((DEST/'catalog.json').read_text())
manifest=json.loads((DEST/'files.sha256.json').read_text())
actual={p.relative_to(DEST).as_posix() for p in DEST.rglob('*') if p.is_file() and p.name!='files.sha256.json'}
assert actual==set(manifest),'Untracked or missing wing source resource'
for base,table in [(SOURCE,lock),(DEST,manifest)]:
    for name,item in table.items():
        data=(base/name).read_bytes()
        assert len(data)==item['bytes'] and hashlib.sha256(data).hexdigest()==item['sha256'],name
for icon in catalog['icons']:
    entry=lock[icon['id']+'.svg']
    assert '/'+catalog['sourceRevision']+'/lorc/' in entry['url']
    assert entry['url']==icon['sourceUrl'] and entry['sha256']==icon['sourceSha256']
    assert ET.fromstring((SOURCE/(icon['id']+'.svg')).read_bytes()).tag.endswith('svg')
    png=(DEST/'Icons'/(icon['id']+'.png')).read_bytes()
    assert png[:8]==b'\x89PNG\r\n\x1a\n' and struct.unpack('>II',png[16:24])==(192,192)
    assert icon['id'] in (ROOT/'Assets/PoolroomsProcedural/Core/WingCatalog.cs').read_text()
assert catalog['distinctExternalIcons']==12 and catalog['distinctExternal3DModels']==0
assert catalog['originalGeometryVariants']==144 and not catalog['paletteSwapsCountAsDesigns']
assert 'CC-BY-3.0' in (DEST/'Licenses/NOTICE.txt').read_text()
assert 'Attribution 3.0' in (DEST/'Licenses/CC-BY-3.0.txt').read_text()
menu=(ROOT/'Assets/PoolroomsProcedural/Runtime/PoolroomsPauseMenu.cs').read_text()
assert 'WingSources/Icons/' in menu and 'DrawWings();' in menu
from generate_asset_credits import build
assert (ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsAssetCredits.txt').read_text()==build()
print(f'PASS: 12 external CC BY 3.0 Lorc icons, {len(lock)} source locks and {len(manifest)} shipped file hashes; selector usage and credits. 0 downloaded wing models; 144 original geometry variants tested separately.')
