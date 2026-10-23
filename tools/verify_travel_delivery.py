"""Download and verify the public travel delivery without storing the full ZIP.

This catches empty/broken downloads and validates the cumulative patch against
the delivered immutable source. It never relies on a UI download confirmation.
"""
import argparse
import concurrent.futures
import hashlib
import json
import subprocess
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = 'waltennecro/poolrooms'
TAG = 'travesia-libre'
BASE = f'https://github.com/{REPOSITORY}/releases/download/{TAG}/'
FULL = 'Poolrooms_Travesia_Libre_Assets.zip'
PATCH = 'Poolrooms_Parche_Travesia_Libre.zip'
METADATA = 'Poolrooms_Travesia_Libre_Entrega.json'


def fetch_record():
    request = urllib.request.Request(BASE + METADATA, headers={'User-Agent': 'Poolrooms-delivery-verifier'})
    with urllib.request.urlopen(request, timeout=60) as response:
        assert response.status == 200
        raw = response.read(1_000_000)
        assert len(raw) < 1_000_000, 'Unexpected delivery metadata length'
    record = json.loads(raw)
    assert len(record['sourceRevision']) == 40
    assert record['nativeUnityExecuted'] is False
    assert record['newExternalModels'] == 0
    assert record['grappleRange'] == 28 and record['travelModes'] == ['normal', 'agile', 'express']
    assert record['preservesForest39'] is True
    for key in ['package', 'patch']:
        assert record[key]['sourceRevision'] == record['sourceRevision']
        assert not record[key]['workingTreeChanged']
        assert record[key]['bytes'] > 200_000_000
        assert len(record[key]['sha256']) == 64
    return record


def download(name, entry, target=None):
    request = urllib.request.Request(BASE + name, headers={'User-Agent': 'Poolrooms-delivery-verifier'})
    digest = hashlib.sha256()
    length = 0
    prefix = b''
    stream = None
    partial = target.with_suffix('.partial') if target else None
    try:
        if partial:
            stream = partial.open('wb')
        with urllib.request.urlopen(request, timeout=120) as response:
            assert response.status == 200
            while True:
                block = response.read(1024 * 1024)
                if not block:
                    break
                if not prefix:
                    prefix = block[:4]
                digest.update(block)
                length += len(block)
                assert length <= entry['bytes'], 'Unexpected extra download bytes'
                if stream:
                    stream.write(block)
        assert prefix == b'PK\x03\x04', 'Response is not a real ZIP download'
        assert length == entry['bytes'] and length > 0, 'Truncated or empty download'
        assert digest.hexdigest() == entry['sha256'], 'Public ZIP hash mismatch'
    finally:
        if stream:
            stream.close()
    if partial:
        partial.replace(target)
    return {'file': name, 'httpStatus': 200, 'bytes': length,
            'sha256': digest.hexdigest(), 'downloadedCompletely': True,
            'storedLocally': target is not None}


def check_source(record):
    source = record['sourceRevision']
    run = str(record['successfulFullRun'])
    data = json.loads(subprocess.check_output(
        ['gh', 'api', f'repos/{REPOSITORY}/actions/runs/{run}'], text=True))
    assert data['head_sha'] == source and data['conclusion'] == 'success'
    assert data['status'] == 'completed'
    assert data['path'] == '.github/workflows/verify-and-package.yml'
    # Later delivery metadata commits are allowed only when Assets is identical.
    subprocess.run(['git', 'diff', '--quiet', source, '--', 'Assets'], cwd=ROOT, check=True)
    return {'run': run, 'head': source, 'conclusion': 'success', 'exactSourceMatched': True}


def check_patch(path, record):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None, 'ZIP CRC failure'
        names = archive.namelist()
        assert len(names) == len(set(names)), 'Duplicate ZIP members'
        assert all(not n.startswith('/') and '..' not in Path(n).parts for n in names), 'Unsafe ZIP path'
        manifest = json.loads(archive.read('PARCHE_MANIFIESTO.json'))
        assert manifest['sourceRevision'] == record['sourceRevision']
        assert 'nivel39-bosque-encantado' in manifest['compatibleReleases']
        for name, expected in manifest['files'].items():
            local = ROOT / 'Assets' / name
            assert local.is_file(), 'Patch file does not exist in reviewed source: ' + name
            with archive.open(name) as entry:
                digest = hashlib.file_digest(entry, 'sha256').hexdigest()
            with local.open('rb') as stream:
                source_digest = hashlib.file_digest(stream, 'sha256').hexdigest()
            assert digest == expected['sha256'] == source_digest, name
            assert archive.getinfo(name).file_size == expected['bytes'] == local.stat().st_size
        assert any(name.endswith('/Core/ExplorerTravel.cs') for name in manifest['files'])
        assert any(name.endswith('/Runtime/PoolroomsTravel.cs') for name in manifest['files'])
        return {'crcVerified': True, 'filesComparedToSource': len(manifest['files']),
                'fileNamesSafe': True, 'duplicateMembers': 0}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    record = fetch_record()
    source = check_source(record)
    (args.output / 'delivery.json').write_text(json.dumps(record, indent=2) + '\n')
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as workers:
        whole = workers.submit(download, FULL, record['package'])
        patch = workers.submit(download, PATCH, record['patch'], args.output / PATCH)
        transfers = [whole.result(), patch.result()]
    patch_audit = check_patch(args.output / PATCH, record)
    result = {'tag': TAG, 'source': source, 'downloads': transfers, 'patchAudit': patch_audit,
              'nativeUnityExecuted': False,
              'scope': 'Actual complete public downloads, SHA-256, patch CRC and per-file source comparison. Full ZIP CRC checked by publisher.'}
    (args.output / 'verified.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
