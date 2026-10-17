# R14: catálogo urbano, decoración por apoyos y arquitectura accesible

Reconstrucción funcional sobre v39; no recuperación exacta de los archivos v40–v47. Conserva IDs anteriores, linterna, movimiento, agua, streaming y entradas existentes.

## Fuentes realmente integradas

Tres modelos adicionales, fijados al commit `edc7c9e67c639d230715049ee31f9a96a6babbbe` de Khronos glTF-Sample-Assets:

| Fuente | Licencia y procedencia | Triángulos: fuente adaptada / LOD1 / LOD2 | Uso |
| --- | --- | --- | --- |
| CarConcept | Eric Chadwick / DGG, CC BY 4.0; marcas con aviso separado | 213.195 / 23.612 / 12.331 | Segundo coche conducible en una parte de las plazas de City11 |
| AntiqueCamera | Maximillan Kamps / UX3D, CC0; marca UX3D con términos separados | 20.066 / 2.192 / 862 | Cámara de pie agarrable, a escala de 1,5 m, en interiores y tiendas urbanas |
| SheenWoodLeatherSofa | Original de Fran Calvente CC0; mejoras de Eric Chadwick / DGG CC BY 4.0 | 46.492 / 10.735 / 3.044 | Sofá agarrable en ciudad, centros comerciales, casas, patio y pabellón costero |

Originales, hashes, autores y textos completos: `Sources/Urban` y `Resources/Urban/Licenses`. Las licencias de modelos no otorgan derechos generales sobre las marcas. Se omiten las dos mallas identificadas como placa y emblema del coche. No se usa la licencia de una página web como sustituto de la licencia de un modelo.

El catálogo acumulado tiene **12 fuentes/adaptaciones, 11 familias geométricas y una variante adicional de silla**. Hay cuatro fuentes de vehículo (dos coches, avión, helicóptero derivado) y tres clases de conducción. No son 35 modelos únicos. Los edificios siguen siendo procedurales: **cero modelos completos de edificios descargados**. Sponza se revisó y se descartó de esta integración al declarar una licencia específica de CRYENGINE, sin establecer permiso general para este proyecto Unity.

## Materiales, LOD y vehículos

- El coche conserva UV0/UV1, oclusión y normales originales mediante atlas de 1024 px por material; carrocería agrupada por material y ruedas separadas. Las pinzas de freno acompañan suspensión/dirección sin girar con el neumático. La pintura usa la máscara de barniz HDRP disponible. Los factores de emisión se conservan.
- El sofá transcodifica sus imágenes WebP y hornea UV, colores de vértice lineales, normal y AO a PNG de 2048 px. La cámara conserva sus UV0 originales, ya compatibles. Los LOD son derivados de esos modelos, no nuevos modelos.
- Las normales opuestas de las piezas delgadas no se cancelan en la simplificación. Se auditan todas las normales referenciadas. Los niveles simplificados se pueden reconstruir sin rehacer texturas con `python tools/rebuild_urban_lods.py`.
- El modo Rendimiento empieza por LOD1; los umbrales de cambio reducen el uso de la malla de mayor detalle. Física solo al conducir o soltar un objeto. No hay combate, muerte ni combustible. No se certifica el coste real sin medirlo en Unity.

Las extensiones de sombreado opcionales se aproximan a HDRP/Lit; esta adaptación no es equivalencia exacta de todos los shaders glTF. La apariencia y las transiciones de LOD necesitan revisión nativa.

## Generación y accesibilidad

- City11 incorpora seis tipologías de remate/porche de fachada, cuatro descriptores de distrito, bordillos bajos, pasos de peatones, sumideros, papeleras y farolas con luz cálida coherente. Son geometrías procedurales combinadas, no modelos externos nuevos. Se conservan los 6 m del cruce de circulación, accesos y escaleras.
- El colocador regional busca superficies horizontales reales en ocho tipos de bioma; comprueba nueve apoyos y espacio libre contra estructura, cristales, decoración y seguridad. No necesita una receta nueva para cada habitación. Máximo 24 candidatos y uno/dos objetos por sala según curvas/calidad; rechaza zonas sin apoyo adecuado.
- Las cámaras de pie solo se proponen en rincones de interiores y tiendas amplias, sin convertirlas en miniaturas sobre una mesa. Las claves estables y los registros serializados conservan la interacción al recargar chunks.
- Se abre el paso a las galerías del patio 188 en planta baja: la barandilla que lo bloqueaba permanece solo arriba. Biblioteca, patio y costa reciben umbrales y marcos propios en sus entradas reales. Los accesos futuros de zonas antiguas siguen inactivos; no se activa un teletransporte oculto.
- El sofá costero sustituye el banco provisional del pabellón sobre suelo seco. No se colocan muebles en mar abierto.

## Qué se comprobó y qué falta

Núcleo: 190 colocaciones en ocho biomas, 28 cámaras urbanas y 18 sofás costeros en las muestras verificadas; soporte/espacio libre, seis tipologías, cuatro descriptores, pasos del patio y barandillas altas. Las 188 huellas protegidas permanecen. La muestra de 324 celdas con aeródromo planifica 1.212 edificios, 4.846 plantas/espacios, 4.040 objetos externos y 39 plazas de vehículos; son instancias, no modelos distintos ni objetos simultáneamente cargados.

Auditoría de las tres fuentes: 66 mapas PNG, originales/licencias, mallas, centros/tipos de ruedas, LOD y dimensiones. Pruebas analíticas del conversor cubren AO degenerado, tangentes originales, colores lineales, WebP y normales de caras opuestas. Se regeneró el coche anterior y todo su paquete City11 quedó idéntico byte por byte.

Las pruebas PlayMode de galería, muebles y ambos coches están incluidas pero **no ejecutadas aquí**. Siguen pendientes compilación completa en Unity, shader/render, física, apariencia, memoria y FPS, y ampliar la arquitectura importada. El lanzador y la medición de R13 permiten registrar esa evidencia; su existencia no equivale a una validación nativa.
