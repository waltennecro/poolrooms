"""Rebuild all original/derived forest media in an isolated temporary directory.

No downloaded authoring cache enters Assets. The generated per-file hashes are
compared with the actual shipped bank. This checks reproducibility in the local
recorded Python/Pillow/numpy environment, not universal floating-point identity.
"""
import argparse
import contextlib
import hashlib
import importlib.util
import io
import json
import platform
import sys
import tempfile
from pathlib import Path
import numpy
import PIL

ROOT = Path(__file__).resolve().parents[1]
SHIPPED = ROOT / 'Assets/PoolroomsProcedural/Resources/Forest39'


def module(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'tools' / (name + '.py'))
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def invoke(name, target, arguments):
    loaded = module(name)
    loaded.DEST = target
    previous = sys.argv
    sys.argv = [name, *arguments]
    try:
        with contextlib.redirect_stdout(io.StringIO()):
            loaded.main()
    finally:
        sys.argv = previous


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cache', required=True, type=Path)
    parser.add_argument('--report', required=True, type=Path)
    args = parser.parse_args()
    results = []
    with tempfile.TemporaryDirectory(prefix='forest39-reproduction-') as temporary:
        target = Path(temporary) / 'Forest39'
        target.mkdir()
        invoke('import_forest39_textures', target, ['--cache', str(args.cache.resolve())])
        invoke('import_forest39_canopy', target, ['--cache', str(args.cache.resolve())])
        invoke('create_forest39_audio', target / 'Audio', [])
        for generated in sorted(target.rglob('*')):
            if not generated.is_file():
                continue
            name = generated.relative_to(target).as_posix()
            shipped = SHIPPED / name
            assert shipped.exists(), 'Generated file is not shipped: ' + name
            assert generated.stat().st_size == shipped.stat().st_size, 'Byte length differs: ' + name
            expected = sha(shipped)
            assert sha(generated) == expected, 'Reproduction differs: ' + name
            results.append({'path': name, 'bytes': generated.stat().st_size, 'sha256': expected})
    report = {'reproducedFiles': len(results), 'files': results, 'python': platform.python_version(),
              'pillow': PIL.__version__, 'numpy': numpy.__version__, 'byteIdentical': True,
              'temporaryDerivativesRemoved': True, 'nativeUnityExecuted': False}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key: value for key, value in report.items() if key != 'files'}))


if __name__ == '__main__':
    main()
