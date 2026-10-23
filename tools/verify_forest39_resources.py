"""Audit the playable Level 39 media, original foley and native source bindings."""
import hashlib
import json
import math
import struct
import wave
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/PoolroomsProcedural'
RESOURCE = ASSETS / 'Resources/Forest39'


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def textures():
    manifest = json.loads((RESOURCE / 'manifest.json').read_text())
    assert manifest['license'] == 'CC0-1.0'
    assert manifest['native4KScanSets'] == 3
    assert manifest['newExternalTextureSets'] == 2
    assert manifest['existingHigherNativeResolution'] == ['Ground037']
    assert manifest['newExternalModels'] == 0
    assert manifest['runtimeBytes'] == sum(e['bytes'] for e in manifest['files'].values())
    assert manifest['runtimeBytes'] >= 200_000_000, 'Requested usable native media budget not reached'
    actual = {p.relative_to(RESOURCE).as_posix() for p in (RESOURCE / 'Textures').glob('*.png')}
    canopy = json.loads((RESOURCE / 'canopy.json').read_text())
    assert actual == set(manifest['files']) | set(canopy['files']), 'Missing or untracked texture file'
    roles = set()
    for scan in manifest['sets']:
        roles.add(scan['role'])
        assert scan['original'] == 'https://ambientcg.com/a/' + scan['id']
        assert scan['sources'].keys() >= {'Color', 'NormalGL', 'Roughness', 'Displacement'}
        for source in scan['sources'].values():
            assert len(source['sha256']) == 64 and source['bytes'] > 1_000_000
            assert 'raw.githubusercontent.com/' in source['url']
            assert any(revision in source['url'] for revision in [manifest['revision'], '27214d292ece53d9dece0c00a4ba77fde269d189'])
            assert max(source['dimensions']) == 4096
        original = scan['sources']['Color']['dimensions']
        for suffix in ['Albedo', 'Normal', 'Mask', 'Height']:
            assert manifest['files'][f'Textures/{scan["role"]}_{suffix}.png']['dimensions'] == original
    assert roles == {'Ground', 'Bark', 'Stone'}
    for name, expected in manifest['files'].items():
        path = RESOURCE / name
        assert path.stat().st_size == expected['bytes'] and sha(path) == expected['sha256'], name
        with Image.open(path) as image:
            image.load()
            assert list(image.size) == expected['dimensions'], name
            if '_Mask' in name:
                assert image.mode == 'RGBA'
                assert image.getchannel('R').getextrema() == (0, 0)
                assert image.getchannel('B').getextrema() == (255, 255)
                low, high = image.getchannel('A').getextrema()
                assert low < high, 'Flat smoothness map'
            if '_Normal' in name:
                assert image.mode == 'RGB' and image.getchannel('B').getextrema()[1] > 230
    return manifest


def canopy():
    record = json.loads((RESOURCE / 'canopy.json').read_text())
    assert record['id'] == 'LeafSet005' and record['license'] == 'CC0-1.0'
    assert not record['upscaled'] and record['nativeResolution'] == [512, 512]
    assert record['newExternalModels'] == 0 and record['alphaCutoff'] == .45
    for name, entry in record['files'].items():
        path = RESOURCE / name
        assert path.stat().st_size == entry['bytes'] and sha(path) == entry['sha256']
        with Image.open(path) as image:
            assert image.size == (512, 512)
            if '_Albedo' in name:
                assert image.mode == 'RGBA' and image.getchannel('A').getextrema() == (0, 255)
    for entry in record['sources'].values():
        assert record['mirrorRevision'] in entry['url'] and len(entry['sha256']) == 64
    return record


def audio():
    manifest = json.loads((RESOURCE / 'Audio/manifest.json').read_text())
    assert manifest['license'] == 'CC0-1.0' and manifest['sourceSamples'] == 0
    assert not manifest['voicesOrWhispers'] and not manifest['startleStingers']
    assert len(manifest['files']) == 4
    actual = {p.name for p in (RESOURCE / 'Audio').glob('*.wav')}
    assert actual == {e['path'] for e in manifest['files']}
    for expected in manifest['files']:
        path = RESOURCE / 'Audio' / expected['path']
        assert path.stat().st_size == expected['bytes'] and sha(path) == expected['sha256']
        with wave.open(str(path), 'rb') as stream:
            assert stream.getnchannels() == 1 and stream.getsampwidth() == 2
            assert stream.getframerate() == 44100
            assert abs(stream.getnframes() / 44100 - expected['seconds']) < .0001
            data = stream.readframes(stream.getnframes())
        samples = struct.unpack('<' + 'h' * (len(data) // 2), data)
        peak = max(abs(value) for value in samples) / 32767
        assert .01 < peak < .95, 'Silent or clipped ambience'
        if expected['loop']:
            assert abs(samples[0] - samples[-1]) <= 1, 'Audible loop discontinuity'
            assert expected['seconds'] == 24
        else:
            assert samples[0] == 0 and samples[-1] == 0, 'Unfaded foley'
    return manifest


def contracts():
    research = json.loads((ASSETS / 'Documentation/LevelResearch/Level39.json').read_text())
    assert research['author'] == 'Ferraru' and research['licenseUrl'].endswith('/by-sa/3.0/')
    assert len(research['accessibleResearch']['sha256']) == 64
    assert research['nativeUnityExecuted'] is False
    notice = (RESOURCE / 'Licenses/NOTICE.txt').read_text()
    assert 'Lennart Demes' in notice and 'CC0-1.0' in notice and 'Ferraru' in notice and 'CC BY-SA 3.0' in notice
    assert (RESOURCE / 'Licenses/CC0-1.0.txt').stat().st_size > 3000
    importer = (ASSETS / 'Editor/PoolroomsForest39Importer.cs').read_text()
    for contract in ['TextureImporterNPOTScale.None', 'importer.maxTextureSize = 4096', 'importer.sRGBTexture = colour',
                     'TextureImporterType.NormalMap', 'streamingMipmaps = true',
                     'mipMapsPreserveCoverage = foliage', 'alphaTestReferenceValue = .45f', 'TextureImporterAlphaSource.FromInput',
                     'AudioClipLoadType.Streaming', 'AudioSampleRateSetting.PreserveSampleRate']:
        assert contract in importer, contract
    materials = (ASSETS / 'Runtime/PoolroomsForest39Materials.cs').read_text()
    for suffix in ['_Albedo', '_Normal', '_Mask', '_Height']:
        assert suffix in materials
    assert 'Resources.Load<Texture2D>' in materials and 'City11Sources.MaterialFor' in materials
    runtime = (ASSETS / 'Runtime/PoolroomsForest39.cs').read_text()
    assert 'new LOD[3]' in runtime and 'CreateItem' in runtime
    assert 'new[] { .19f, .075f, .008f }' in runtime
    interaction = (ASSETS / 'Runtime/PoolroomsForest39Activity.cs').read_text()
    assert 'inventory.Items.Add' in interaction and 'inventory.WasCollected' in interaction
    assert 'SetState' in interaction and 'Forest39RouteMap.Describe' in interaction
    assert 'Teleport' not in interaction, 'An optional study must not secretly teleport the player'
    ambience = (ASSETS / 'Runtime/PoolroomsForest39Ambience.cs').read_text()
    assert 'EffectiveMasterVolume' in ambience and 'IsPaused' in ambience and 'environmentVolume' in ambience
    assert 'Source("Oak canopy wind")' in ambience and 'Source("Nearby leaf rustle")' in ambience
    world = (ASSETS / 'Runtime/PoolroomsWorld.cs').read_text()
    assert 'PoolroomsForest39.AttachCanopy' in world and 'PoolroomsForest39.Attach(this,r,parent)' in world
    assert 'Forest39Planner.Same' in world
    from generate_asset_credits import build
    assert (ASSETS / 'Resources/PoolroomsAssetCredits.txt').read_text() == build()


def main():
    images = textures()
    foliage = canopy()
    clips = audio()
    contracts()
    print(json.dumps({'nativeScanSets': images['native4KScanSets'], 'newDistinctScanIds': images['newExternalTextureSets'] + 1, 'canopyNativeSize': foliage['nativeResolution'],
                      'textureBytes': images['runtimeBytes'], 'originalSpatialClips': len(clips['files']),
                      'audioBytes': sum(f['bytes'] for f in clips['files']), 'newExternalModels': 0,
                      'nativeUnityExecuted': False}))


if __name__ == '__main__':
    main()
