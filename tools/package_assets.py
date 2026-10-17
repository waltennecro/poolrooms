"""Create a complete current Assets package, with hashes and source revision.

Does not label reconstructed files as recovered originals or include Verification
inside Assets. Existing archives are never overwritten.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output",required=True,type=Path)
    parser.add_argument("--stage",required=True)
    args=parser.parse_args()
    root=Path(__file__).resolve().parents[1]
    output=args.output.resolve()
    assets=root/"Assets/PoolroomsProcedural"
    if output.exists() or output.is_relative_to(root/"Assets"):
        raise SystemExit("Choose a new ZIP path outside Assets")
    files=sorted(p for p in assets.rglob("*") if p.is_file())
    manifest={"stage":args.stage,"base":"v39","reconstructionNotExactRecovery":True,
              "sourceRevision":subprocess.check_output(["git","rev-parse","HEAD"],cwd=root,text=True).strip(),
              "workingTreeChanged":bool(subprocess.check_output(["git","status","--porcelain"],cwd=root,text=True).strip()),
              "unityValidated":False,"files":{p.relative_to(root/"Assets").as_posix():{"bytes":p.stat().st_size,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()} for p in files}}
    output.parent.mkdir(parents=True,exist_ok=True)
    # Stable file order/timestamps: same committed source yields the same ZIP bytes.
    with zipfile.ZipFile(output,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
        for p in files:
            info=zipfile.ZipInfo(p.relative_to(root/"Assets").as_posix(),(2026,1,1,0,0,0))
            info.compress_type=zipfile.ZIP_DEFLATED
            archive.writestr(info,p.read_bytes())
        info=zipfile.ZipInfo("RECONSTRUCCION_PAQUETE.json",(2026,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED
        archive.writestr(info,json.dumps(manifest,indent=2,ensure_ascii=False).encode("utf-8"))
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None:raise SystemExit("ZIP CRC failed")
        for name,item in manifest["files"].items():
            if hashlib.sha256(archive.read(name)).hexdigest()!=item["sha256"]:raise SystemExit("ZIP SHA256 failed: "+name)
    print(json.dumps({"zip":str(output),"files":len(files),"bytes":output.stat().st_size,"sha256":hashlib.sha256(output.read_bytes()).hexdigest(),"sourceRevision":manifest["sourceRevision"],"workingTreeChanged":manifest["workingTreeChanged"]}))


if __name__=="__main__":main()
