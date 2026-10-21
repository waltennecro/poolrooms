# Repository Guidelines

## Project Structure & Module Organization

This repository ships a Unity 6.3/HDRP 17.3 **Assets package**, not a complete Unity project. Game code lives in `Assets/PoolroomsProcedural`: engine-independent generation in `Core`, streaming and player components in `Runtime`, setup menus in `Editor`, and optional rendering/input bridges in `Integrations`. `Resources` and `Textures` contain shipped media. Native tests are under `Tests/PlayMode`.

New biomes must join `WalkableExploration` and its route checks; Tools menus are optional shortcuts.

Keep standalone verification in `Verification` and packaging scripts in `tools`, outside Assets. `RECUPERACION_V39.json` records the recovered original files; `RECONSTRUCCION.md` tracks reconstructed features and unfinished work. `Preparacion_V48` preserves historical proposals; current reconstructed level integration is documented under Assets/PoolroomsProcedural/Documentation.

## Build, Test, and Development Commands

- `dotnet run --project Verification/Poolrooms.Checks.csproj`: compile the actual core, check geometry fingerprints and C# 9 syntax. Requires SDK 8.0.408, pinned by `global.json`.
- `python tools/verify_city_sources.py`, `python tools/verify_aircraft_sources.py`, `python tools/verify_helicopter_sources.py`, `python tools/verify_interior_sources.py`, `python tools/verify_urban_sources.py`, `python tools/verify_bistro_sources.py`, `python tools/verify_building_sources.py`, `python tools/verify_garden_sources.py` and `python tools/verify_bath_sources.py`: audit model provenance, licenses, geometry and resource hashes.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --aquatic-plans`: scan all 8,400 waterpark plans at six supported scales.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --generation-audit`: audit room plans, prop/source lookups, full mesh samples and lazy streaming selection.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --variety-expansion`: check expanded recipes, new house footprints, architectural programs and mall reservations.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --biome-extents`: compare regional room areas, populated terrain expansion and accessible mall growth.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --restful-levels --restful-exhaustive`: verify ten new level banks, furnishing support and structural axes.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --restful-mesh-catalog`: fingerprint 200,010 built room meshes without counting colour or props.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --restful-world`: count distinct interactive and licensed-source placements in an actual unified coordinate snapshot.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --restful-refinement`: check refined support, bridge clearance, physical bevels and recessed washer glazing.
- `python tools/refine_restful_batches.py --output /tmp/R34-review --expect-report Verification/refinement_R34_batches.json`: compare 200,010 unchanged recipe IDs in 21 batches against immutable R33; build current Release first. Requires the baseline Git object documented by the script.
- `python tools/verify_restful_sources.py`: audit the three new licensed source models and six LOD derivatives.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --encounter` and `python tools/verify_encounter_resources.py`: audit routes, inventory, furniture support and attributed media; native tests remain separate.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --immersion-stations` and `python tools/verify_immersion_audio.py`: check real-world activity placements, solvable puzzles and original foley hashes/licensing.
- `python tools/verify_play_textures.py`: check shipped finish textures and hashes using Python 3.
- `python tools/package_assets.py --stage reconstruccion --output /tmp/Poolrooms_Assets.zip`: create a complete package with per-file hashes and source revision.
- `python tools/verify_recovery.py`: compare against untouched v39; expected to report intentional reconstruction changes on current main.

Use an external Unity HDRP project to import the package, create the demo through Tools → Poolrooms, and run PlayMode tests. Cloud core checks do not validate Unity APIs, shaders, PhysX, appearance, or FPS.

## Coding Style & Naming Conventions

Use four-space indentation, PascalCase types/methods, and camelCase fields. Follow nearby compact C# formatting. Keep Core free of Unity references and generation deterministic. Append serialized enum values; preserve existing IDs. Keep the flashlight unchanged unless specifically requested. Prefer suitable high-quality external assets with verified licences, provenance, attribution and bounded placement/LOD costs.

## Testing Guidelines

Use the standalone checks for generation and mathematical regressions; Unity Test Framework/NUnit for native interaction and collision. Name tests after observable behavior. Preserve baseline fingerprints instead of regenerating them to hide changes. No percentage coverage target is established.

## Commit & Pull Request Guidelines

History uses concise imperative subjects such as “Reconstruct …” and “Recover …”. Commit bounded stages and back them up without force-pushing. Describe behavior, validation actually executed, and remaining Unity checks. Include real Unity screenshots for visual claims. Update the reconstruction inventory; distinguish reconstructed features from exact recovery and proposals from working integration.
