# Repository Guidelines

## Project Structure & Module Organization

This repository ships a Unity 6.3/HDRP 17.3 **Assets package**, not a complete Unity project. Game code lives in `Assets/PoolroomsProcedural`: engine-independent generation in `Core`, streaming and player components in `Runtime`, setup menus in `Editor`, and optional rendering/input bridges in `Integrations`. `Resources` and `Textures` contain shipped media. Native tests are under `Tests/PlayMode`.

Keep standalone verification in `Verification` and packaging scripts in `tools`, outside Assets. `RECUPERACION_V39.json` records the recovered original files; `RECONSTRUCCION.md` tracks reconstructed features and unfinished work. `Preparacion_V48` contains source models and proposals, not implemented levels.

## Build, Test, and Development Commands

- `dotnet run --project Verification/Poolrooms.Checks.csproj`: compile the actual core, check geometry fingerprints and C# 9 syntax. Requires .NET SDK 8.
- `python tools/verify_play_textures.py`: check shipped finish textures and hashes using Python 3.
- `python tools/package_assets.py --stage reconstruccion --output /tmp/Poolrooms_Assets.zip`: create a complete package with per-file hashes and source revision.
- `python tools/verify_recovery.py`: compare against untouched v39; expected to report intentional reconstruction changes on current main.

Use an external Unity HDRP project to import the package, create the demo through Tools → Poolrooms, and run PlayMode tests. Cloud core checks do not validate Unity APIs, shaders, PhysX, appearance, or FPS.

## Coding Style & Naming Conventions

Use four-space indentation, PascalCase types/methods, and camelCase fields. Follow nearby compact C# formatting. Keep Core free of Unity references and generation deterministic. Append serialized enum values; preserve existing IDs. Keep the flashlight unchanged unless specifically requested.

## Testing Guidelines

Use the standalone checks for generation and mathematical regressions; Unity Test Framework/NUnit for native interaction and collision. Name tests after observable behavior. Preserve baseline fingerprints instead of regenerating them to hide changes. No percentage coverage target is established.

## Commit & Pull Request Guidelines

History uses concise imperative subjects such as “Reconstruct …” and “Recover …”. Commit bounded stages and back them up without force-pushing. Describe behavior, validation actually executed, and remaining Unity checks. Include real Unity screenshots for visual claims. Update the reconstruction inventory; distinguish reconstructed features from exact recovery and proposals from working integration.
