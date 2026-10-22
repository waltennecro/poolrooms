"""Build the cumulative ten-file installation/compiler fix, without media."""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile
from package_runtime_hotfix import FILES as RUNTIME_FILES

FILES = RUNTIME_FILES + (
    "Editor/PoolroomsPackagePaths.cs",
    "Editor/PoolroomsSetup.cs",
    "Editor/PoolroomsBiomeMaterialSetup.cs",
    "Editor/PoolroomsTerrainMenu.cs",
    "Editor/PoolroomsTerrainShaderSetup.cs",
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[1]
    output = args.output.resolve()
    if output.exists() or output.is_relative_to(root / "Assets"):
        raise SystemExit("Choose a new ZIP path outside Assets")
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    changed = bool(subprocess.check_output(["git", "status", "--porcelain"], cwd=root, text=True).strip())
    entries = {"PoolroomsProcedural/" + p: (root / "Assets/PoolroomsProcedural" / p).read_bytes() for p in FILES}
    manifest = {"sourceRevision": revision, "workingTreeChanged": changed,
                "cumulativeCompilerAndInstallationFix": True, "unityValidated": False,
                "compatibleReleases": ["rincones-procedurales", "cinco-horizontes", "correccion-compilacion-runtime"],
                "files": {p: {"bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()} for p, data in entries.items()}}
    entries["PARCHE_MANIFIESTO.json"] = (json.dumps(manifest, indent=2) + "\n").encode()
    entries["LEEME.txt"] = (
        "Parche acumulativo: errores de compilacion y rutas flexibles de instalacion.\n\n"
        "Cierra Unity. Abre la carpeta PoolroomsProcedural que YA tienes dentro de Assets, "
        "aunque este dentro de Poolrooms_Compilacion_Corregida_Assets u otra carpeta. "
        "Copia Core, Runtime y Editor de este parche DENTRO de esa carpeta existente. "
        "Sobrescribe los archivos correspondientes y agrega PoolroomsPackagePaths.cs. "
        "Conserva los demas archivos y sus .meta. No agregues otra copia del codigo.\n\n"
        "Reabre Unity y espera a que termine de importar. No necesitas borrar escenas "
        "ni volver a descargar las texturas. Funciona con Rincones, Cinco horizontes "
        "y Compilacion corregida. El parche no agrega niveles nuevos a versiones antiguas.\n\n"
        "Se comprobo el localizador real con APIs reducidas y carpetas fisicas; "
        "Unity/HDRP e importacion nativa pendientes en el entorno remoto.\n"
    ).encode()
    output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(output, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        for name, data in sorted(entries.items()):
            info = zipfile.ZipInfo(name, (2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, data)
    with zipfile.ZipFile(output) as archive:
        if archive.testzip() is not None or any(archive.read(p) != data for p, data in entries.items()):
            raise SystemExit("Patch CRC/content failed")
    print(json.dumps({"zip": str(output), "files": len(FILES), "bytes": output.stat().st_size,
                      "sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
                      "sourceRevision": revision, "workingTreeChanged": changed}))


if __name__ == "__main__":
    main()
