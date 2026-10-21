"""Reproducible full authoring ZIP: tracked Assets, original sources, docs and checks.

Unlike the compressed runtime Assets package, this archival edition stores the
actual files without recompression. It is useful for offline source inspection
and rebuilding licensed adaptations. It contains no padding, git objects, build
outputs, old downloads or fabricated assets. Existing archives are never replaced.
"""
from pathlib import Path
import argparse,hashlib,json,subprocess,zipfile

ROOT=Path(__file__).resolve().parents[1]
def digest(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    output=args.output.resolve()
    if output.exists() or output.is_relative_to(ROOT):raise SystemExit('Choose a new path outside the repository')
    if subprocess.check_output(['git','status','--porcelain'],cwd=ROOT):raise SystemExit('Commit source before creating a delivery archive')
    revision=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    names=sorted(subprocess.check_output(['git','ls-files','-z'],cwd=ROOT).decode().rstrip('\0').split('\0'))
    assert names and all((ROOT/name).is_file() and not (ROOT/name).is_symlink() for name in names)
    manifest={'sourceRevision':revision,'edition':'Complete offline authoring sources','compression':'ZIP_STORED','nativeUnityValidated':False,
      'files':{name:{'bytes':(ROOT/name).stat().st_size,'sha256':digest(ROOT/name)} for name in names}}
    output.parent.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(output,'x',compression=zipfile.ZIP_STORED,allowZip64=True) as archive:
        for name in names:
            info=zipfile.ZipInfo('Poolrooms/'+name,(2026,1,1,0,0,0));info.compress_type=zipfile.ZIP_STORED;info.external_attr=0o100644<<16
            with archive.open(info,'w') as dest,(ROOT/name).open('rb') as source:
                while True:
                    data=source.read(1024*1024)
                    if not data:break
                    dest.write(data)
        archive.writestr(zipfile.ZipInfo('Poolrooms/AUTHORING_MANIFEST.json',(2026,1,1,0,0,0)),json.dumps(manifest,indent=2,ensure_ascii=False).encode())
    with zipfile.ZipFile(output) as archive:
        assert archive.testzip() is None
        for name,record in manifest['files'].items():
            with archive.open('Poolrooms/'+name) as f:assert hashlib.file_digest(f,'sha256').hexdigest()==record['sha256'],name
    print(json.dumps({'zip':str(output),'sourceRevision':revision,'files':len(names),'bytes':output.stat().st_size,'sha256':digest(output),'everyFileSha256AndCRCVerified':True,'paddingBytes':0,'nativeUnityValidated':False}))
if __name__=='__main__':main()
