# Mobiliario CC0 y adaptación de coordenadas — R7

Se añade una fuente distinta: **SheenChair**, Eric Chadwick/Wayfair (2020), licencia CC0-1.0 para todo el recurso según sus metadatos. El GLB original, URL fijada a una revisión, licencia, atribución y SHA-256 quedan en `Sources/City11` y `Resources/City11/Licenses`. La documentación/metadatos de Khronos conservan su atribución CC-BY-4.0.

## Integración real

Un sillón agarrable por edificio de City11, seis en cada biblioteca y cuatro en cada patio residencial, en planta baja. Se apoyan en suelo real, junto a mesas y fuera de escaleras/umbrales. Se conserva la posición y clave de los objetos anteriores; los nuevos se añaden al final. Usan E/Q, el sistema previo de objetos transportados y física al soltarlos, con masa de 6,5 kg. No son un inventario nuevo ni NPC.

El catálogo acumulado tiene **cinco modelos externos distintos**: tres objetos pequeños, un sillón y un coche. Hay dos LOD derivados del coche y dos del sillón; no cuentan como modelos nuevos. En la muestra planificada de 324 manzanas: 1.296 edificios procedurales, 5.185 espacios de planta, 2.592 instancias de objetos pequeños, 1.296 sillones y 40 puntos de aparición del mismo coche. No son objetos cargados simultáneamente, modelos de edificios descargados ni vehículos distintos. Los 15.555 conjuntos de mobiliario anteriores siguen siendo procedurales.

## Materiales y coste

La fuente utiliza UV0 con rotación/repetición/desplazamiento de texturas y UV1 para oclusión. La adaptación separa islas superpuestas, vuelve a empaquetar sus coordenadas, hornea nueve mapas intermedios a 2048 px, interpola el color en espacio lineal, conserva AO y convierte normales entre las bases tangentes. Añade ocho píxeles de margen para mipmaps. No elimina la comprobación de solapamientos para admitir la fuente.

Se entregan OBJ, texturas PBR y máscaras HDRP nativas; no hace falta un plugin glTF en el juego. La geometría principal tiene **39.936 triángulos**, con derivados de **6.169** y **1.210**. Las instancias comparten recursos; Rendimiento comienza en el primer LOD reducido. Las texturas se importan comprimidas con mipmaps. El coste y las transiciones deben medirse en Unity.

Se adapta el modelo de material metálico/rugoso. La extensión opcional de brillo textil `KHR_materials_sheen` no tiene una reproducción exacta en el adaptador HDRP/Lit; no se afirma equivalencia visual con el visor glTF. Solo se integra la apariencia predeterminada del sillón, no sus variantes alternativas de material.

## Verificación

Pruebas analíticas del horneador: rotación/desplazamiento, repetición y clamp, interpolación sRGB correcta, separación de islas superpuestas, horneado GLB completo con AO de UV1 y normales bajo coordenadas rotadas. Las herramientas usan las versiones de NumPy/Pillow fijadas en `Verification/requirements-art-tools.txt`; CI ejecuta estas comprobaciones además del núcleo, hashes y empaquetado anteriores.

Comprobación acumulada del núcleo: 16.828.897 aserciones; 179 scripts en cuatro configuraciones. 540 apoyos de objetos en los niveles nuevos y 288 instancias de fuentes en 24 manzanas; 188 huellas anteriores conservadas. La auditoría comprueba cinco fuentes CC0, los OBJ/índices/LOD y los hashes de todos los recursos adaptados. Las aserciones no son pruebas nativas de Unity.

Faltan compilación Unity/HDRP, importación y revisión de materiales/orientación, físicas, imágenes reales, memoria y FPS. El catálogo urbano amplio y los vehículos de otras clases, incluido avión/helicóptero, continúan pendientes; no se presentan como recuperados.
