"""Package the five-file Runtime compilation fix without redistributing media.

Compatible with the Rincones and Cinco horizontes releases; records exact
before/after hashes. Does not overwrite archives or alter repository files.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile


FILES = (
    "Core/ImmersionStations.cs",
    "Runtime/PoolroomsRoomLife.cs",
    "Runtime/PoolroomsPickup.cs",
    "Runtime/PoolroomsSoundscape.cs",
    "Runtime/PoolroomsStation.cs",
)
BASES = {
    "rincones-procedurales": "cb2a67d633fe0efd8279f711d5b42a55d00138cc",
    "cinco-horizontes": "b6e631b39f01915fcd367cf3abd4106e27c0707a",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = args.output.resolve()
    if output.exists() or output.is_relative_to(root / "Assets"):
        raise SystemExit("Choose a new ZIP path outside Assets")
    git = lambda *cmd: subprocess.check_output(["git", *cmd], cwd=root)
    entries = {}
    records = {}
    for relative in FILES:
        path = "Assets/PoolroomsProcedural/" + relative
        data = (root / path).read_bytes()
        before = {name: hashlib.sha256(git("show", revision + ":" + path)).hexdigest()
                  for name, revision in BASES.items()}
        if len(set(before.values())) != 1:
            raise SystemExit("Previous release sources differ; review compatibility: " + path)
        name = "PoolroomsProcedural/" + relative
        entries[name] = data
        records[name] = {"bytes": len(data), "sha256": hashlib.sha256(data).hexdigest(),
                         "beforeSha256": before}
    manifest = {
        "sourceRevision": git("rev-parse", "HEAD").decode().strip(),
        "workingTreeChanged": bool(git("status", "--porcelain").strip()),
        "compatibleReleaseRevisions": BASES, "files": records,
        "unityValidated": False, "newBiomesIncludedInPatch": False,
    }
    entries["PARCHE_MANIFIESTO.json"] = (json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode()
    entries["LEEME.txt"] = (
        "Correccion de tres errores de compilacion y una advertencia.\n\n"
        "Cierra Unity. Abre la carpeta PoolroomsProcedural que ya tienes dentro de Assets "
        "(puede estar dentro de Poolrooms_Rincones_Procedurales_Assets). "
        "Copia Core y Runtime de este parche dentro de esa carpeta, sobrescribiendo los cinco archivos. "
        "Conserva las demas carpetas y sus archivos .meta. No importes una segunda copia.\n\n"
        "Este parche sirve para Rincones procedurales y Cinco horizontes. "
        "No agrega los cinco niveles si aun tienes Rincones; usa el paquete completo para eso. "
        "Reabre Unity y deja terminar la compilacion. Si queda algun error, comparte la consola completa.\n\n"
        "Verificacion remota de tipos/nucleo, no compilacion nativa Unity/HDRP.\n"
    ).encode("utf-8")
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(entries.items()):
            info = zipfile.ZipInfo(name, (2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None:
            raise SystemExit("Patch CRC failed")
        for name, data in entries.items():
            if archive.read(name) != data:
                raise SystemExit("Patch content mismatch: " + name)
    print(json.dumps({"zip": str(output), "files": len(FILES), "bytes": output.stat().st_size,
                      "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
                      "sourceRevision": manifest["sourceRevision"],
                      "workingTreeChanged": manifest["workingTreeChanged"]}))


if __name__ == "__main__":
    main()
