"""Compare the same 200,010 R33 room recipes in batches of 10,000.

The old core is compiled from an immutable Git revision, never from modified
working files. This verifies geometric change, not perceived/native quality.
Requires the pinned .NET SDK and the baseline Git object (fetch if shallow).
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shutil
import subprocess
import tarfile

BASELINE = "0a43903830cf694377118229a23dbeeed9a6346b"
ROOT = Path(__file__).resolve().parents[1]


def run(*args, **kwargs):
    kwargs.setdefault("cwd", ROOT)
    return subprocess.run(args, check=True, **kwargs)


def prepare_baseline(output, dotnet):
    project = output / "baseline-project"
    project.mkdir()
    raw = subprocess.check_output(["git", "archive", BASELINE,
                                  "Assets/PoolroomsProcedural/Core", "global.json"], cwd=ROOT)
    with tarfile.open(fileobj=io.BytesIO(raw)) as archive:
        for entry in archive:
            dest = (project / entry.name).resolve()
            if not dest.is_relative_to(project.resolve()) or entry.issym() or entry.islnk():
                raise SystemExit("Unsafe baseline archive member")
            if entry.isfile():
                dest.parent.mkdir(parents=True, exist_ok=True)
                dest.write_bytes(archive.extractfile(entry).read())
    verification = project / "Verification"
    verification.mkdir()
    # The current verifier compiles against old Core; its room/mesh comparison
    # code is identical for both builds. No new engine API is substituted.
    for source in (ROOT / "Verification").glob("*.cs"):
        shutil.copy2(source, verification / source.name)
    shutil.copy2(ROOT / "Verification/Poolrooms.Checks.csproj", verification)
    csproj = str(verification / "Poolrooms.Checks.csproj")
    run(dotnet, "build", csproj, "--configuration", "Release", "-warnaserror", cwd=project)
    regression = subprocess.run([dotnet, "run", "--no-build", "--configuration", "Release", "--project", csproj,
                                 "--", "--restful-refinement"], cwd=project, text=True, capture_output=True)
    (output / "before-regression.log").write_text(regression.stdout + regression.stderr)
    if regression.returncode == 0 or "Table edge has no physical sloped bevel" not in regression.stderr:
        raise SystemExit("Old implementation did not reproduce the expected geometric failure")
    return csproj


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--baseline-dir", type=Path, help="Reuse a completed immutable-core baseline export")
    parser.add_argument("--refined-dir", type=Path, help="Reuse a completed comparison export")
    parser.add_argument("--expect-report", type=Path, help="Require the checked-in deterministic batch report")
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    before = args.baseline_dir.resolve() if args.baseline_dir else output / "before"
    if args.baseline_dir is None:
        baseline_project = prepare_baseline(output, args.dotnet)
        run(args.dotnet, "run", "--no-build", "--configuration", "Release", "--project", baseline_project,
            "--", "--restful-batches", str(before))
    after = args.refined_dir.resolve() if args.refined_dir else output / "after"
    if args.refined_dir is None:
        run(args.dotnet, "run", "--no-build", "--configuration", "Release", "--project", "Verification/Poolrooms.Checks.csproj",
            "--", "--restful-batches", str(after), "--compare-batches", str(before))
    old, new = (json.loads((p / "summary.json").read_text()) for p in (before, after))
    if old["comparison"] or not new["comparison"] or old["rooms"] != 200010 or new["rooms"] != 200010:
        raise SystemExit("Incomplete baseline/comparison export")
    if old["completedBatches"] != 21 or new["completedBatches"] != 21:
        raise SystemExit("Missing batch evidence")
    batches = []
    for previous, current in zip(old["batches"], new["batches"]):
        for folder, record in ((before, previous), (after, current)):
            content = folder / f'batch-{record["batch"]:02}.jsonl'
            if hashlib.sha256(content.read_bytes()).hexdigest().upper() != record["sha256"]:
                raise SystemExit("Batch evidence hash mismatch")
        if current["changedMeshes"] != current["rooms"] or current["changedStructures"] != current["rooms"]:
            raise SystemExit("Some architectures were merely recounted, not structurally changed")
        if current["beforeTriangles"] != previous["triangles"]:
            raise SystemExit("Comparison used a different geometric baseline")
        batches.append(dict(current, beforeSha256=previous["sha256"], status="core_verified_native_unrun"))
    report = dict(stage="R34", baselineRevision=BASELINE, rooms=200010, batchSize=10000, batches=batches,
                  beforeTriangles=old["triangles"], afterTriangles=new["triangles"],
                  sameRecipeIDs=True, nativeUnityExecuted=False,
                  scope="Existing ten R33 structural programmes; no new models or handcrafted room count")
    (output / "report.json").write_text(json.dumps(report, indent=2) + "\n")
    if args.expect_report and report != json.loads(args.expect_report.read_text()):
        raise SystemExit("Generated batch report differs from the reviewed report")
    print(json.dumps(dict(report=str(output / "report.json"), rooms=200010, completedBatches=21)))


if __name__ == "__main__":
    main()
