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
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --journey-levels` and `python tools/verify_journey_resources.py`: check twenty appended programmes, 58-biome walkable discovery, old-gallery enrichment, eight new attributed models/two LODs each and 43 runtime surface definitions. `MEGA_EXPANSION_ESTADO.json` records the actual Assets size against the 6 GB ceiling; never pad files to reach that ceiling.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --mall-retail` and `python tools/verify_mall_resources.py`: audit compact/large commercial fit-outs, floor-local lights, route-safe packing, fourteen new attributed models, their LODs and source PBR maps. `--mall-atlas` exports actual floor plans; reference images are not Unity screenshots.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --large-regions`: check physical extent for all 38 biomes, broad hall seams, spacious mall/terrain reservations and bounded A* routes. Native profile defaults enable this independently from view distance; core legacy fixtures retain their compact default.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --special-regions`: validate ten special programmes, preserved discovery assignments, furniture support and physical walking routes across all 38 catalogue biomes.
- `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --immersion-stations` and `python tools/verify_immersion_audio.py`: check real-world activity placements, solvable puzzles and original foley hashes/licensing.
- `python tools/verify_play_textures.py`: check shipped finish textures and hashes using Python 3.
- `python tools/package_assets.py --stage reconstruccion --output /tmp/Poolrooms_Assets.zip`: create a complete package with per-file hashes and source revision.
- `python tools/verify_recovery.py`: compare against untouched v39; expected to report intentional reconstruction changes on current main.

Use an external Unity HDRP project to import the package, create the demo through Tools → Poolrooms, and run PlayMode tests. Cloud core checks do not validate Unity APIs, shaders, PhysX, appearance, or FPS.

Use `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --workshops` for constructive puzzle solutions, bounded caches, state geometry and biome distribution. Workshop IDs append after the original twelve; keep legacy station IDs stable.

Use `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --frontend` and `python tools/verify_frontend_art.py` for independent-menu flow and original background audits. Keep gameplay uninstantiated in the dedicated entry scene until Play is selected; returning to title must preserve the open session.

Use `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --scenery` for the appended furnishing layer. Preserve recipe/anchor/piece pickup keys, both protected crossing axes and source-model dimensions; optional budget exhaustion must not stop structural streaming.

Use `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --horizons` for the five appended programmes, human-scale cabin/lookout access, previous ID baseline and 63-biome walking discovery. Keep the prior Journey bank at twenty and custom profiles opt-in through EnableHorizonLevels.

Use `--courtyard-hotel` for the first researched level rebuild: one shared four-sector court, five hotel storeys, human-scale stairs, supported furniture, finite-world edges and mixed-world annexes. `RebuildCourtyard188` remains false in historical Core fixtures and is enabled in unified exploration/native profiles. `RECONSTRUCCION_NIVELES_ESTADO.json` lists the other generic programmes as pending individual reconstruction; do not label them finished because the catalogue exists. `--unity-lifetimes` audits native Unity allocations in MonoBehaviour instance fields; native constructor/OBJ/door/collider tests remain in PlayMode and must not be claimed executed by source checks.

## Coding Style & Naming Conventions

Use four-space indentation, PascalCase types/methods, and camelCase fields. Follow nearby compact C# formatting. Keep Core free of Unity references and generation deterministic. Append serialized enum values; preserve existing IDs. Keep the flashlight unchanged unless specifically requested. Prefer suitable high-quality external assets with verified licences, provenance, attribution and bounded placement/LOD costs.

## Testing Guidelines

Use the standalone checks for generation and mathematical regressions; Unity Test Framework/NUnit for native interaction and collision. Name tests after observable behavior. Preserve baseline fingerprints instead of regenerating them to hide changes. No percentage coverage target is established.

## Commit & Pull Request Guidelines

History uses concise imperative subjects such as “Reconstruct …” and “Recover …”. Commit bounded stages and back them up without force-pushing. Describe behavior, validation actually executed, and remaining Unity checks. Include real Unity screenshots for visual claims. Update the reconstruction inventory; distinguish reconstructed features from exact recovery and proposals from working integration.

Use `--archive-library` for the researched Cygnus rebuild: continuous vaulted naves, three walkable storeys, galleries/bridges, stairs, distinct wing programmes, real roof glazing holes, furnishing support and mixed-world routes. `RebuildArchive906` is opt-in in Core historical fixtures and enabled in unified/native profiles. Preserve the individual queue; Courtyard188, Archive906, Conservatory and CloudTerminal are rebuilt so far. `--archive-atlas <new directory>` exports Core geometry for optional technical previews, never Unity screenshots.

Use `--crystal-conservatory` for the original Conservatory rebuild: continuous glass district roof, two connected walking levels, eight botanical programmes, staircase/pond separation, real water bottoms and steps, roof collision, source support and mixed-world entrances. Core `RebuildConservatory` defaults false for historical fixtures; unified/native profiles enable it. `tools/verify_conservatory_resources.py` audits the new CC0 Ground037 texture set; keep the +Y normal convention, 2048 import and HDRP mask channels. This is an original liminal design, not a claimed canonical wiki replica. `--conservatory-atlas <new directory>` exports geometry for technical previews, not Unity screenshots.

Use `--sky63` for the researched Tranquility rebuild of CloudTerminal: real suspended deck voids, two walking heights, contour guard rails, stairs, supported activities and mixed-world walking entries. Historical Core `RebuildSky63` defaults false; unified/native profiles enable it. `tools/verify_sky63_resources.py` checks the original CC0 64³ cloud mask; HDRP uses two viewer-wide local fog volumes and respects the volumetric quality toggle. Preserve source attribution and documented adaptations; native cloud rendering/physics remain separate checks.

Shared verification files (including `RestfulExpansionChecks.cs`) also compile against immutable R33. Guard references to newer Core APIs with `!POOLROOMS_HISTORICAL_CORE`; preserve the legacy paths and thresholds. Validate `tools.refine_restful_batches.prepare_baseline` with the current verifier after adding such references, before waiting for remote batches.

Use `--render-surfaces` for opaque coplanar ownership, UV/blend/area preservation, generated-room/mall samples and shared-wall unload restoration. Keep this upload adapter separate from historical generation/physics. `RenderBoundary` caches only primitives touching the perimeter; refresh when loaded neighbours change; prefer inward-facing room paint and use coordinates only for ties, never by a per-frame world scan. Preserve transparent exclusions and classify mutable seam renderers as HDRP DynamicGeometry. Native PlayMode/appearance/FPS remain separate.

Use `--city-provisions` and `python tools/verify_food_sources.py` for the Level11 dreamcore pavilions, 103 food types/117 presentations + 8 drinks, licensed geometry and unbounded inventory stacks. Historical Core `DreamCity11`/`EnableProvisions` default false; unified/native profiles enable them. Food distribution must cover room, connected/legacy mall and Level94 house hooks, use bounded physical support/clearance checks, and preserve walking/driving routes. No hunger, thirst or additional HUD; persistence remains postponed. Whole/cut presentations and LODs are not new food types/source models. Native PlayMode tests remain separate.

When adding asset attribution, update `tools/generate_asset_credits.py`, regenerate the shipped credits and run `python tools/verify_wing_sources.py` early. That existing audit enforces the canonical generator exactly; appending notices manually fails the full pipeline even when all geometry checks pass. Preserve the assertion.

Use `--city-buildings` and `python tools/verify_city_architecture_sources.py` for 128 ordinary/128 strange original spatial designs, real generated floor/stair/gallery samples, source-key uniqueness and the third Bistro facade with all-LOD clearance. Keep `DetailedCityBuildings` false in historical Core and true in unified/native profiles. The 256 designs are not external models; only one new external building subtree is added, alongside two preserved ones. Reuse native 4K/2K Journey finishes without upscaling or duplicate media. Keep the six-metre roads, pavilion and ground pickup supports. Styled physical geometry has a visual finish owner resolved before upload; test that ownership rather than displaying duplicate faces. Unity native checks remain separate.

Use `--forest39` and `python tools/verify_forest39_resources.py` for the appended Level39 forest: shared curved/looping paths, 12 clearings, terrain-clipped shallow water, three canopy LODs, alpha-cut scanned foliage, three keepsake items and eight optional activities. `EnableForest39` defaults false in historical Core and true in unified/native profiles. Preserve the previous 63 discovery assignments; the forest uses the single former duplicate slot. The 4K ground/bark/rock bank and 512² foliage keep native dimensions. Foliage uses alpha-aware material ownership; never opaque-trim it. Reuse the regional sun and listener-wide audio sources, preserve render distance, and report original architecture, existing source reuse and newly licensed scan IDs separately. Native Unity checks remain pending until actually executed.

Use `--travel` for normal exploration mobility: C selects normal/agile/express pace, J automatic advance, K/middle button held grappling, sprint boosts swimming. Preserve raw input, menu/photo gates, collision movement, static-anchor occlusion and stopping before unsupported auto-advance. `TravelTests` are native tests and remain separate from reduced API/source compilation.

The user requires every gameplay change from Travesía libre onward to be recorded in all three handoff TXT documents. Add an entry to `CAMBIOS_JUEGO_DESDE_RELEVO.json`, then run `python tools/update_project_texts.py`. The generator maintains a clearly dated update block in `GUIA_MAESTRA_PROYECTO_POOLROOMS.txt`, `LO_QUE_LE_FALTA_AL_JUEGO_50_PAGINAS.txt` and `LEER_PRIMERO_PARA_LA_SIGUIENTE_IA.txt`, and produces `CAMBIOS_JUEGO_DESDE_RELEVO.txt`. Keep historical document snapshots identifiable; the original 50-page PDF is a dated assessment. Record implemented changes and actual checks, with Unity limits explicit.
