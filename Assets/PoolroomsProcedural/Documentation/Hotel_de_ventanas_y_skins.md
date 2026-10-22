# Patio de ventanas: primer nivel reconstruido

Esta etapa sustituye la arquitectura del **Patio de ventanas / Nivel 188**, conservando su ID y todos los destinos anteriores. No convierte los otros niveles recientes en reconstrucciones terminadas.

## Arquitectura y exploración

Un distrito de 4 × 4 sectores forma un hotel de cinco plantas. Los cuatro sectores centrales comparten un único patio abierto: sin paredes ni techo entre ellos. Alrededor hay fachadas con ventanas y alféizares, habitaciones individuales y dobles, recepción, zonas de lectura/desayuno y lavanderías. Cuatro escaleras de dos tramos, con huecos reales en los forjados, llegan hasta la azotea. Hay barandillas, servicios de cubierta, drenajes, camas con colchones redondeados, escritorios y espejos agrupados en una sola malla.

Cuatro céspedes rodean un círculo central de madera. El pasto usa los mapas PBR existentes del Nivel 94 y un pequeño lote de briznas, sin un objeto por hoja. Se reutilizan muebles con procedencia y licencias conservadas. Las puertas se abren lateralmente; algunas persianas se pueden mover. El vidrio mantiene su barrera física. Hay lectura, postales, libros y actividades del hotel.

Con celdas de 36 m el conjunto ocupa 144 × 144 m; el patio mide 72 × 72 m. No son dimensiones tomadas de otro juego. Cada habitación tiene luminarias físicas; la selección de luces usa la altura real de cada planta, incluyendo el vestíbulo alto. La iluminación HDRP distingue patio/interior y azotea y utiliza el sol regional existente, sin agregar soles por sector. La distancia de renderizado y la linterna se conservan.

La exploración unificada activa esta reconstrucción. Un perfil independiente puede usar `rebuildCourtyard188`. En bordes donde no caben dieciséis sectores del mismo bioma se generan anexos hoteleros; nunca se invade otro nivel para completar un edificio. Los accesos son caminables. Los sectores sin cargar conservan barreras de streaming de altura completa.

## Referencias

Se contrastaron la descripción original de 2020 y la versión hotelera de Fandom mediante referencias públicas que transcriben y atribuyen sus descripciones. Los dominios originales devolvieron HTTP 403 desde este entorno. [LevelResearch/Level188.json](LevelResearch/Level188.json) distingue fuentes, variantes y adaptaciones. No se copió código, diseño ni contenido multimedia de otro juego. La fotografía original no se redistribuye.

Esta adaptación mantiene la arquitectura y el ambiente tranquilo solicitados: no añade entidades ni desapariciones. El círculo de madera no se presenta como una trampilla funcional hacia otro nivel.

## Skins y error de interacción

En **Ajustes → Skins**, la importación está antes de las alas. El resultado se muestra junto a **Importar**. Una importación correcta durante la partida activa tercera persona para que veas el modelo al cerrar los ajustes. También puedes usar **Ver skin en tercera persona** durante la partida; en primera persona el cuerpo se oculta intencionalmente.

OBJ funciona directamente, hasta 64 MiB de archivo, con los límites anteriores de vértices, triángulos y materiales. Conserva `.mtl` y texturas junto al modelo. Los modelos externos cargados aparecen en el selector. Si falla la adaptación anatómica, se conserva la geometría como skin estática. Una importación fallida conserva la skin anterior. GLB/glTF requiere glTFast; los demás formatos indicados utilizan el importador del Editor, no un lector universal en ejecutables.

El error `CreateImpl` provenía de construir `MaterialPropertyBlock` en un inicializador de `PoolroomsInteractable`. Ahora se crea cuando se utiliza, en el hilo principal. Se incorporan un control de código y pruebas nativas de regresión.

## Verificación y siguientes niveles

`--courtyard-hotel` comprueba los dieciséis sectores a tres escalas, dos anchos de puerta y alturas mínima/máxima admitidas (192 sectores de muestra): apoyos, muebles sin intersección con arquitectura, pasos y descansillos, accesos entre plantas, patio sin techo y bordes del mundo mixto. No sustituye Unity/PhysX/HDRP. Las pruebas PlayMode añadidas deben ejecutarse en Unity; este entorno no tiene Editor y no permite garantizar el resultado visual ni FPS.

`RECONSTRUCCION_NIVELES_ESTADO.json` conserva la cola: los demás niveles genéricos siguen pendientes de una reconstrucción individual con investigación y arquitectura propia.

## Vista técnica reproducible

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --courtyard-atlas /tmp/hotel-atlas` exporta las mallas reales de un distrito de dieciséis sectores y su inventario. `blender -b --python tools/render_courtyard_preview.py -- /tmp/hotel-atlas /tmp/hotel-preview` genera dos vistas de arquitectura. Son vistas técnicas en Blender, no capturas de Unity: no representan puertas/persianas nativas, muebles importados, HDRP, postprocesado ni rendimiento del juego.
