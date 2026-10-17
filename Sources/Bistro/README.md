# Bistro component provenance

Three components of Amazon Lumberyard Bistro v5.2 (Amazon.com, Inc. or its affiliates), CC BY 4.0: one awning, one cafe table and one chair. Full attribution/license and the upstream derivative MIT notice accompany the package. These are **three component geometry families, zero complete building models**.

Geometry is extracted from `zeux/niagara_bistro` at `2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e`; upstream scene metadata is retained as `bistro_source.gltf`. Its large binary and DDS textures remain in an external import cache, with byte counts, immutable URLs and SHA-256 in `sources.lock.json`. The metadata alone is not a standalone scene. No upstream code is executed.

`*_adapted.glb` are selected subtree derivatives, with metric up-axis retained, scene placement removed, and original BC7 textures converted to bounded PNGs. They are not untouched original files. Authored diffuse, specular colour and gloss-alpha factors are preserved in the native HDRP Specular Color workflow. Source specular colour is stored separately from the derived metal/roughness representation; this is not a claim that all Bistro materials are metallic.

Rebuild with the pinned NumPy/Pillow environment:

```
python tools/import_bistro_sources.py --cache /workspace/research-poolrooms/Bistro_input
python tools/verify_bistro_sources.py
```

The compact web conversion reviewed during research was not used: its binary differed from the original. Only the pinned original geometry/DDS inputs above feed the shipped adaptations. Native shaders, appearance and performance still require Unity validation.
