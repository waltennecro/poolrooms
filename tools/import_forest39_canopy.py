"""Import one existing CC0 scanned broadleaf cluster at its native resolution.

The photographed leaves are deciduous, not an exact Quercus botanical model.
The original oak branch generator uses them as a deliberate visual adaptation.
"""
import argparse
import hashlib
import json
import urllib.request
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/PoolroomsProcedural/Resources/Forest39'
REV = '64285742023325e6275866f1379fffacf8323a46'
BASE = f'https://raw.githubusercontent.com/Affinator/bevy_procedural_tree/{REV}/assets/textures/deciduous_leaves/'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cache', type=Path, required=True)
    args = parser.parse_args()
    args.cache.mkdir(parents=True, exist_ok=True)
    sources = {}
    for name in ['color.png', 'normal_gl.png', 'roughness.png', 'source.txt']:
        path = args.cache / name
        if not path.exists():
            path.write_bytes(urllib.request.urlopen(BASE + name, timeout=30).read())
        sources[name] = {'url': BASE + name, 'bytes': path.stat().st_size,
                         'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}
    assert 'ambientcg.com/view?id=LeafSet005' in (args.cache / 'source.txt').read_text()
    colour = Image.open(args.cache / 'color.png').convert('RGBA')
    normal = Image.open(args.cache / 'normal_gl.png').convert('RGB')
    roughness = Image.open(args.cache / 'roughness.png').convert('L')
    assert colour.size == normal.size == roughness.size == (512, 512)
    assert colour.getchannel('A').getextrema() == (0, 255)
    zero = Image.new('L', colour.size, 0)
    one = Image.new('L', colour.size, 255)
    maps = {'Albedo': colour, 'Normal': normal,
            'Mask': Image.merge('RGBA', (zero, one, one, ImageChops.invert(roughness)))}
    files = {}
    for suffix, image in maps.items():
        path = DEST / 'Textures' / ('Canopy_' + suffix + '.png')
        image.save(path, compress_level=9)
        files[path.relative_to(DEST).as_posix()] = {'bytes': path.stat().st_size,
                                                  'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                                                  'dimensions': list(image.size)}
    manifest = {'id': 'LeafSet005', 'author': 'Lennart Demes / ambientCG', 'license': 'CC0-1.0',
                'source': 'https://ambientcg.com/a/LeafSet005', 'mirrorRevision': REV,
                'sources': sources, 'files': files, 'nativeResolution': [512, 512],
                'upscaled': False, 'alphaCutoff': .45, 'newExternalModels': 0,
                'adaptation': 'Scanned deciduous foliage on original oak branch architecture. Not a botanical Quercus replica.'}
    (DEST / 'canopy.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps({'scanId': 'LeafSet005', 'nativeResolution': [512, 512], 'bytes': sum(v['bytes'] for v in files.values())}))


if __name__ == '__main__':
    main()
