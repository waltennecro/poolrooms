"""Create the cumulative Level63/conservatory/Cygnus/hotel/skin patch over Cinco horizontes or a newer release."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile

BASE = "b6e631b39f01915fcd367cf3abd4106e27c0707a"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = args.output.resolve()
    if output.exists() or output.is_relative_to(root / "Assets"):
        raise SystemExit("Choose a new ZIP path outside Assets")
    def git(*args):
        return subprocess.check_output(["git", *args], cwd=root, text=True).strip()
    revision = git("rev-parse", "HEAD")
    if git("status", "--porcelain"):
        raise SystemExit("Commit the reviewed source before packaging")
    changed = git("diff", "--name-status", BASE, revision, "--", "Assets/PoolroomsProcedural").splitlines()
    entries = {}
    for line in changed:
        status, path = line.split("\t", 1)
        if status not in ("A", "M"):
            raise SystemExit("This patch needs explicit removal/rename instructions: " + line)
        entries[Path(path).relative_to("Assets").as_posix()] = (root / path).read_bytes()
    if not entries:
        raise SystemExit("Refuse an empty patch")
    manifest = {"sourceRevision": revision, "baseRevision": BASE, "workingTreeChanged": False,
                "nativeUnityExecuted": False, "fullAssetsRequired": True,
                "compatibleReleases": ["cinco-horizontes", "correccion-compilacion-runtime", "rutas-flexibles", "hotel-ventanas-skins", "cygnus-reconstruido", "invernadero-cristal"],
                "files": {name: {"bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()} for name, data in sorted(entries.items())}}
    entries["PARCHE_MANIFIESTO.json"] = (json.dumps(manifest, indent=2) + "\n").encode()
    entries["LEEME.txt"] = (
        "Actualizacion acumulativa: Nivel63 Tranquilidad, Invernadero de Cristal, biblioteca Cygnus, hotel de ventanas, skins y correcciones anteriores.\n\n"
        "Requiere los Assets completos de Cinco horizontes o una entrega posterior. "
        "Con Unity detenido, copia el contenido de PoolroomsProcedural dentro de la carpeta "
        "PoolroomsProcedural que YA tienes en Assets, aunque este en otra subcarpeta. "
        "Sobrescribe archivos y agrega los nuevos; conserva los demas recursos y .meta. "
        "No instales otra copia paralela del codigo.\n\n"
        "Exploracion unificada activa Nivel63 (rebuildSky63), Invernadero de Cristal (rebuildConservatory), Cygnus y el hotel 188. "
        "Para una escena personalizada revisa rebuildCourtyard188 en el perfil. "
        "Las skins se importan en Ajustes > Skins; el boton de tercera persona permite ver el resultado.\n\n"
        "Incluye las correcciones previas de compilacion y rutas flexibles de texturas. "
        "El resto de los niveles genericos aun espera su reconstruccion individual. "
        "Unity/HDRP, shaders, apariencia y fisica nativos pendientes de ejecutar.\n"
    ).encode()
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(entries.items()):
            info = zipfile.ZipInfo(name, (2026, 1, 1, 0, 0, 0)); info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None or any(archive.read(p) != data for p, data in entries.items()):
            raise SystemExit("Patch CRC/content validation failed")
    print(json.dumps({"zip": str(output), "files": len(manifest["files"]), "bytes": output.stat().st_size,
                      "sha256": hashlib.sha256(output.read_bytes()).hexdigest(), "sourceRevision": revision, "workingTreeChanged": False}))


if __name__ == "__main__":
    main()
