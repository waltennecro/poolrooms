"""Audit original frontend illustrations and import/display bindings, not gameplay graphics."""
from pathlib import Path
import hashlib
import json
from PIL import Image

root = Path(__file__).resolve().parents[1]
bank = root / 'Assets/PoolroomsProcedural/Runtime/Resources/FrontendArt'
record = json.loads((bank / 'Provenance.json').read_text())
assert len(record['files']) == 6
assert 'not a Unity screenshot' in record['files'][0]['kind']
assert {p.name for p in bank.glob('*.png')} == {r['file'] for r in record['files']}
digests = set()
for item in record['files']:
    path = bank / item['file']
    data = path.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    assert digest == item['sha256'] and len(data) == item['bytes']
    digests.add(digest)
    with Image.open(path) as image:
        assert image.size == (item['width'], item['height'])
        assert image.width >= 1600 and image.height >= 900
        assert abs(image.width / image.height - 16 / 9) < .01
        image.verify()
assert len(digests) == 6
ui = (root / 'Assets/PoolroomsProcedural/Runtime/PoolroomsFrontend.UI.cs').read_text()
assert 'FrontendArt/' in ui and 'Resources.UnloadAsset' in ui
importer = (root / 'Assets/PoolroomsProcedural/Editor/PoolroomsFrontendSetup.cs').read_text()
assert 'importer.mipmapEnabled=false' in importer and 'TextureImporterNPOTScale.None' in importer
assert 'TextureImporterCompression.Uncompressed' in importer
print(json.dumps({'result': 'PASS', 'distinctOriginalBackgrounds': 6,
                  'bytes': sum(x['bytes'] for x in record['files']),
                  'gameplayScreenshots': 0, 'nativeImportRendering': 'unrun'}))
