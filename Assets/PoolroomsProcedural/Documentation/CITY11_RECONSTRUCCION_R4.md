# City11 — integración incremental R4

Esta es una reconstrucción nueva sobre v39, no el código desaparecido de v47. Aún no es una réplica terminada del Nivel 11 ni se ha ejecutado en Unity.

## Lo implementado

- Bioma City11 añadido al final de los IDs. Expanded lo distribuye por grupos regionales; `enableCity11` y `city11Share` se guardan en el perfil. No modifica las entradas existentes del centro comercial o Nivel 94. El modo maestro ofrece **Visitar Nivel 11 · Ciudad vacía** automáticamente. La demo habitual sirve.
- Manzanas con cuatro edificios, calles, puertas reales, ventanas con cristal/colisión, azoteas, 2–6 pisos y escaleras de dos tramos. Ocho programas de mobiliario y cuatro composiciones de fachada; las plantas altas tienen soporte y mobiliario reales. El streamer y las barreras de carga anteriores siguen controlando el acceso.
- Materiales independientes de ciudad y un único sol regional HDRP con cielo físico/exposición propia; respeta el control de sombras y solicita sombras RT solo cuando el modo y el Asset lo permiten. No crea una sonda por edificio.
- Botellas y radios CC0 colocadas sobre mesas/mostradores: E toma, Q suelta; conservan los límites, identidad e interacción existentes.
- Primer coche importado CC0 con entrada/salida E, WASD, freno Ctrl, cuatro WheelColliders y límites de velocidad. La simulación se activa al entrar; el coche espera si no hay suelo cargado delante. Salir requiere detenerse y encontrar suelo/altura libres. El TP y desactivar al jugador liberan el vehículo. No hay daño ni retos.

## Recursos y cifras reales

Cuatro fuentes distintas descargadas de **KhronosGroup/glTF-Sample-Assets**, revisión `edc7c9e67c639d230715049ee31f9a96a6babbbe`: WaterBottle, BoomBox, Lantern y ToyCar. Los modelos son **CC0-1.0**; el texto de metadatos/licencias es CC-BY-4.0. Se conservan autores, URLs, licencias y SHA-256 en `Resources/City11/Licenses`, `catalog.json` y `Sources/City11/sources.lock.json`.

Son tres objetos y un coche de origen. La botella, radio y coche tienen colocación en City11; el farol está adaptado, pero su colocación queda pendiente. No hay todavía modelos externos de edificios, muebles, helicóptero o avión. Los edificios y muebles actuales son geometría procedural propia/reutilizada de v39, no modelos descargados.

ToyCar contenía una peana de tela: se retiró únicamente de la adaptación, conservando el GLB original. El coche usa 91.331 triángulos cerca, 14.988 en LOD intermedio y 3.319 lejos. Rendimiento empieza por el intermedio. Los dos LOD son derivados del mismo coche, no dos modelos distintos. Las extensiones opcionales de transmisión/tejido/capa de barniz necesitan revisión visual HDRP; la adaptación de materiales no se presenta como reproducción exacta del glTF.

Una muestra planificada de 18×18 manzanas (no objetos cargados simultáneamente) contiene 1.296 edificios generados, 5.185 espacios de planta, 2.592 instancias de objetos, 15.555 conjuntos de mobiliario y 40 puntos de aparición del **mismo modelo de coche**. No satisface un catálogo de 35 vehículos distintos ni incluye el avión y helicóptero solicitados. La distribución de calles todavía sigue las conexiones y celdas del generador anterior; hace falta ampliar su urbanismo y variedad arquitectónica.

## Verificación y pendientes

Comprobaciones del núcleo sobre 24 manzanas: 96 edificios y 192 instancias de objetos; apoyos de todos los peldaños y mostradores, validación de mallas, determinismo, umbrales y búsqueda del TP. Máximo observado: 15.172 triángulos estructurales/detalle/cristal por manzana, sin modelos externos. Las 188 huellas de las etapas anteriores se conservan. `tools/verify_city_sources.py` comprueba licencias, fuentes/hashes, geometría OBJ y LOD; no necesita descargar ni regenerar modelos en CI.

Pruebas PlayMode incluidas para importación/materiales, puertas y escaleras, sin ejecutar aquí. Siguen pendientes la compilación Unity/HDRP, orientación/apariencia del coche, tacto y estabilidad de WheelCollider, sombras/reflejos, carga de recursos, memoria y FPS. No se afirma ausencia de errores visuales ni calidad AAA validada.
