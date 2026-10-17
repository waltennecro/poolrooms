# R10: sofá y variante de tapizado con recursos atribuidos

## Qué se integra

`GlamVelvetSofa` y `ChairDamaskPurplegold`, modelos/texturas de Eric Chadwick para Wayfair (2021), se adaptan desde una revisión fijada de Khronos glTF-Sample-Assets. Ambos se distribuyen bajo **CC BY 4.0**, con licencias, atribución y cambios dentro de Resources. Los GLB originales, metadatos, URLs y hashes permanecen en `Sources/Interiors`.

El sofá aporta **una familia geométrica nueva**. La silla damasco comparte la familia de la silla ovalada SheenChair existente: se cuenta como **variante de esa silla**, no como otro modelo distinto. El catálogo acumulado contiene nueve fuentes externas/adaptaciones con procedencia (cinco CC0, una Apache-2.0 y tres CC BY 4.0), correspondientes a **ocho familias geométricas y una variante adicional**. Solo hay dos familias de muebles importados: silla ovalada y sofá. No se presentan tapizados, LOD o repeticiones como modelos nuevos.

## Colocación e interacción

- City11 selecciona de forma determinista la silla anterior, el tapizado damasco o el sofá. Este último solo aparece en locales de al menos 10 × 10 m; conserva un paso lateral de 66 cm respecto al hueco de escalera y más de 70 cm entre sofá y mostrador delantero.
- Biblioteca 906 conserva sillas de ambas variantes. Patio 188 añade sofás solamente en celdas de al menos 36 m, dentro de habitaciones amuebladas.
- Todos usan **E agarrar / Q soltar**, sin inventario. Los volúmenes son cajas de dimensiones del modelo; masa de sofá 24 kg y silla 6,5 kg. La física se activa al soltar y conserva las reglas/límites existentes. Hace falta espacio para soltar muebles grandes.
- No cambian los IDs 0–2 de objetos urbanos; se añaden 3–4. No se alteran escaleras, paredes, rutas, linterna ni arquitectura de otros biomas.

Los 336 emplazamientos revisados incluyen apoyo bajo toda la base y márgenes de circulación en tiendas. Una muestra de 324 celdas City11 con aeródromo activo contiene **421 sillas anteriores, 359 sofás y 432 variantes damasco**; son instancias planificadas, no objetos cargados simultáneamente ni modelos diferentes.

## Acabado y rendimiento

Se hornean **13 mapas PNG** a partir del texturizado original: rotaciones, repeticiones, AO y normales con conversión de marco tangente y margen entre islas. La silla usa su UV1 de AO; el sofá no tiene UV1 original y usa UV0 como semilla para un atlas nuevo sin solapamientos. Solo se integra la variante material predeterminada del sofá. La adaptación registra las extensiones sheen/specular opcionales; no afirma reproducirlas exactamente con HDRP/Lit.

Se corrige la transferencia del factor de color base lineal glTF a las propiedades Color de Unity, conservando alfa. Texturas de color importadas como sRGB; máscaras y normales como datos lineales. No se reutilizan los materiales de las Poolrooms para estos muebles.

Sofá: **4.196 triángulos**; variante de silla: **9.984**. No se inventan LOD adicionales para contarlos como modelos. Se añade descarte por tamaño en pantalla a estos dos recursos, sin un Update por objeto, luces nuevas o simulación permanente de cuerpos rígidos. Los modos gráficos anteriores se conservan; no se promete una cifra de FPS.

## Verificación real y límites

Núcleo compilado: **16.939.230 aserciones**, huellas de 108 SoftPlay y 80 muestras anteriores conservadas; sintaxis C# 9 de **190 scripts × cuatro configuraciones**. Comprobados 336 apoyos completos, entradas, escaleras y selección de las tres opciones. Auditoría de las dos fuentes, licencias, 13 mapas, índices, normales, límites y contrato de escala del núcleo. Regeneración de OBJ/MTL/PNG reproducida byte a byte. Se mantienen las auditorías anteriores y 36 mapas PBR originales.

Se incluyen pruebas PlayMode de importación, materiales, escala y penetración física de muebles contra tiendas, **no ejecutadas**. Faltan Unity 6.3/HDRP 17.3: compilación completa de APIs, importación, colisiones PhysX, shaders/RT, apariencia y FPS. El render no se valida con hashes ni con estas aserciones matemáticas.
