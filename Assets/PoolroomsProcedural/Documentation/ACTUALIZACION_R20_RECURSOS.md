# R20 — Recurso con licencia e integración real

La Orangerie incorpora **una familia de modelo nueva**, la planta en maceta de
Rico Cilliers (original CC0, 2021), con materiales de Eric Chadwick / Darmstadt
Graphics Group GmbH (CC BY 4.0, 2024). Fuente: Khronos glTF Sample Assets,
revisión `edc7c9e67c639d230715049ee31f9a96a6babbbe`.

Se incluyen el original, metadatos, URL, hashes, licencia completa, atribución y
nota de modificaciones. Los recursos de Unity contienen únicamente maceta,
hojas y sustrato: sin insectos, luces, trayectorias ni animación de la demostración.
La escena original permanece solo en `Sources/Garden` para trazabilidad.

## Integración

- Dos plantas interactuables en salas de lectura y té, apoyadas en jardineras.
- Sustituyen las frondas procedurales correspondientes; no se superponen con ellas.
- Las salas mantienen 16 objetos y añaden dos plantas cuando corresponde: 18.
  Catálogo completo: 42.000 instancias planificadas, incluidas 2.000 plantas;
  no se cargan todas simultáneamente. Sigue habiendo 2.500 recetas estructurales.
- Una familia nueva, tres mallas de detalle de esa misma familia, seis mapas PBR;
  no se presentan los LOD ni las repeticiones como modelos adicionales.
- Créditos y licencias accesibles desde el inicio y la pausa, con las
  atribuciones de los recursos anteriores y del nuevo modelo.

## Adaptación y límites

Se conservan maceta, hojas, UV y mapas originales. Solo se reduce la malla
escaneada del sustrato: modelo nativo 37.251 triángulos, LOD1 29.772 y LOD2
19.629; el original de la planta tenía 68.273. Importación reproducible desde
una carpeta de destino vacía, con salida idéntica byte por byte.

Se añade recorte alfa para las hojas y preservación de cobertura en mipmaps;
las hojas usan caras dobles y los sombreadores Lit existentes. La extensión
opcional glTF `KHR_materials_diffuse_transmission` **no se reproduce exactamente**
en HDRP: se conserva el PBR base, sin afirmar equivalencia de transmisión o SSS.

El coste de la arquitectura/vidrio/vegetación procedural ahora llega a 10.058
triángulos por receta, **excluyendo** plantas y muebles importados y decoración
común. No se reducen el alcance de streaming ni los gráficos anteriores.

## Comprobaciones

`python tools/verify_garden_sources.py` comprueba fuentes, licencias, hashes,
selección estática de nodos, mapas, canal alfa, tres mallas y dimensiones físicas.
`python tools/import_garden_sources.py` reproduce los recursos y
`python tools/generate_asset_credits.py` actualiza los créditos visibles.
El núcleo comprueba las 2.500 recetas y la colocación con el nuevo modelo.

Las pruebas nativas de carga, recorte de hojas, LOD e interacción están añadidas,
pero no ejecutadas. Compilación Unity/HDRP, apariencia, sombras y FPS quedan
pendientes de Unity real. Los otros nueve niveles y las 10.000 zonas globales
siguen pendientes en el plan de expansión.
