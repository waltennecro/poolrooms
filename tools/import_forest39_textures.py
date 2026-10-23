"""Import native ambientCG scans from a pinned public mirror, never upscale.

Run with /usr/bin/python3 tools/import_forest39_textures.py --cache PATH.
The cache contains original downloaded maps; only runtime PBR derivatives ship.
"""
import argparse
import hashlib
import json
import urllib.request
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/PoolroomsProcedural/Resources/Forest39'
REV = 'd4fe80d4885ca3bb8cee69e62a34568676c71b29'
BASE = f'https://raw.githubusercontent.com/quickblack1/Duniaku-Free-and-Open-Source-Game/{REV}/'
ROCK_BASE = 'https://raw.githubusercontent.com/septianrazi/CornellBoxInThreeJS/27214d292ece53d9dece0c00a4ba77fde269d189/'
SETS = {
    'Ground': ('Ground037', 'tres/Ground037_4K-PNG/', 'Ground037_4K-PNG_', '.png'),
    'Bark': ('Bark001', 'Bark001_4K-JPG/', 'Bark001_4K_', '.jpg'),
    'Stone': ('Rock030', 'Rock030_4K-JPG/', 'Rock030_4K-JPG_', '.jpg'),
}


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def acquire(cache, path, base=BASE):
    local = cache / Path(path).name
    if not local.exists():
        temporary = local.with_suffix(local.suffix + '.part')
        with urllib.request.urlopen(base + path, timeout=120) as source, temporary.open('wb') as out:
            while data := source.read(1024 * 1024):
                out.write(data)
        temporary.replace(local)
    return local


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cache', required=True, type=Path)
    args = parser.parse_args()
    args.cache.mkdir(parents=True, exist_ok=True)
    (DEST / 'Textures').mkdir(parents=True, exist_ok=True)
    (DEST / 'Licenses').mkdir(exist_ok=True)
    manifest = {'revision': REV, 'author': 'Lennart Demes / ambientCG',
                'license': 'CC0-1.0', 'licenseUrl': 'https://creativecommons.org/publicdomain/zero/1.0/',
                'sets': [], 'files': {}, 'newExternalModels': 0,
                'normalConvention': 'OpenGL +Y; Unity NormalMap importer',
                'adaptation': 'Native dimensions retained. Lossless PNG derivatives; packed HDRP mask. No upscaling.'}
    for key, (identifier, folder, prefix, extension) in SETS.items():
        maps = {}
        records = {}
        for suffix in ['Color', 'NormalGL', 'Roughness', 'Displacement'] + (['AmbientOcclusion'] if key in ['Ground', 'Stone'] else []):
            path = folder + prefix + suffix + extension
            source_base = ROCK_BASE if key == 'Stone' else BASE
            local = acquire(args.cache, path, source_base)
            with Image.open(local) as image:
                maps[suffix] = image.convert('RGB' if suffix in ['Color', 'NormalGL'] else 'L')
                dimensions = list(image.size)
            records[suffix] = {'url': source_base + path, 'bytes': local.stat().st_size,
                               'sha256': digest(local), 'dimensions': dimensions}
        dimensions = maps['Color'].size
        assert all(m.size == dimensions for m in maps.values()), identifier
        assert max(dimensions) == 4096 and min(dimensions) >= 2048, dimensions
        zero = Image.new('L', dimensions, 0)
        one = Image.new('L', dimensions, 255)
        ao = maps.get('AmbientOcclusion', one)
        runtime = {
            'Albedo': maps['Color'],
            'Normal': maps['NormalGL'],
            'Mask': Image.merge('RGBA', (zero, ao, one, ImageChops.invert(maps['Roughness']))),
            'Height': maps['Displacement'],
        }
        for suffix, image in runtime.items():
            local = DEST / 'Textures' / f'{key}_{suffix}.png'
            image.save(local, compress_level=9)
            relative = local.relative_to(DEST).as_posix()
            manifest['files'][relative] = {'sha256': digest(local), 'bytes': local.stat().st_size,
                                          'dimensions': list(dimensions), 'role': suffix}
        manifest['sets'].append({'id': identifier, 'role': key,
                                 'original': f'https://ambientcg.com/a/{identifier}', 'sources': records})
    # A small original leaf study supplies vein detail to the actual lobed geometry.
    # It is not labelled a photographed scan or counted as an external asset.
    import numpy as np
    size = 1024
    yy, xx = np.mgrid[0:size, 0:size] / (size - 1)
    vein = np.exp(-((xx - .5) * 145) ** 2)
    secondary = np.exp(-(np.sin((yy - abs(xx - .5) * .7) * 31) * 13) ** 2) * .24
    grain = np.random.default_rng(390039).normal(0, 1.3, (size, size))
    channels = [65 + vein * 20 + secondary * 18 + grain,
                86 + vein * 25 + secondary * 22 + grain,
                32 + vein * 10 + secondary * 8 + grain]
    leaf = Image.fromarray(np.clip(np.stack(channels, axis=2), 0, 255).astype('uint8'))
    local = DEST / 'Textures/Leaf_Albedo.png'
    leaf.save(local)
    manifest['files']['Textures/Leaf_Albedo.png'] = {'sha256': digest(local), 'bytes': local.stat().st_size,
                                                   'dimensions': [size, size], 'role': 'original leaf vein study'}
    manifest['native4KScanSets'] = len(SETS)
    manifest['newExternalTextureSets'] = 2
    manifest['existingHigherNativeResolution'] = ['Ground037']
    manifest['runtimeBytes'] = sum(v['bytes'] for v in manifest['files'].values())
    (DEST / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    (DEST / 'Licenses/NOTICE.txt').write_text(
        'Level 39 texture scans: Ground037, Bark001 and Rock030 by Lennart Demes / ambientCG.\n'
        'https://ambientcg.com/a/Ground037\nhttps://ambientcg.com/a/Bark001\n'
        'CC0-1.0: https://creativecommons.org/publicdomain/zero/1.0/\n'
        'Pinned public mirror and original byte hashes: ../manifest.json.\n'
        'Native 4096x4096 ground and 2230x4096 bark retained; no upscaling.\n'
        'Leaf vein texture and procedural oak geometry are original project work, CC0-1.0.\n'
        'Level 39 / Enchanted Forest setting: Ferraru, The Backrooms Wiki, CC BY-SA 3.0.\n'
        'https://backrooms-wiki.wikidot.com/level-39\n'
        'https://creativecommons.org/licenses/by-sa/3.0/\n'
        'Adaptation: peaceful accessible forest, optional activities and walking transitions; '
        'no hypnosis, lethal hunger, boundary storms or copied Wiki photographs.\n'
        'Canopy scan: LeafSet005, Lennart Demes / ambientCG, CC0-1.0. https://ambientcg.com/a/LeafSet005\n'
        'Pinned mirror and native 512x512 dimensions: ../canopy.json. Broadleaf texture adaptation; not a botanical Quercus replica. No upscaling.\n')
    print(json.dumps({'runtimeBytes': manifest['runtimeBytes'], 'textureSets': len(SETS)}))


if __name__ == '__main__':
    main()
