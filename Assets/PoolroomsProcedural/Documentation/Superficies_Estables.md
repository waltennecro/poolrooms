# Superficies estables en la generación

## Problema encontrado

Las mallas contenían porciones de rectángulos opacos coplanares orientados en la misma dirección: uniones de cajas, acabados y estructura competían por los mismos píxeles. También había paredes compartidas duplicadas entre sectores vecinos. Eso puede producir *z-fighting*: el material parece parpadear o cortarse al mover la cámara. No es una interacción física entre materiales.

## Corrección

- `RenderSurfaceOwnership` prepara la geometría para dibujarla sin modificar el generador ni sus mallas de colisión. El acabado opaco posterior conserva su superficie; recorta sólo la porción coincidente de la base. Elimina también triángulos idénticos con igual orientación, incluso girados.
- Conserva normales, UV afines, pesos de mezcla y metadatos de material. Comprueba el área y reconoce redondeos de coordenadas al girar piezas 90°. No aplana relieves separados 4 mm ni elimina caras de orientación opuesta. No aplica desplazamiento de profundidad a las paredes.
- Agua, cristales, película de los toboganes y redes quedan fuera del recorte opaco. Los mapeados no afines conservan su triangulación.
- `RenderBoundary` separa las primitivas opacas que tocan los 60 cm exteriores de cada sector (también las losas o vigas que llegan hasta allí). `PoolroomsSurfaceSeams` da prioridad al acabado del lado visible de cada sala; usa las coordenadas para desempatar en caras horizontales cuando hay vecinos cargados y restaura las caras al descargarlos. Sólo guarda estas primitivas del perímetro; actualiza las afectadas por un cambio de vecinos. No recorre el mundo por fotograma ni cambia la distancia de renderizado.
- HDRP clasifica estas franjas como `DynamicGeometry`, también después de aplicar opciones gráficas, para actualizar su geometría en el ray tracing.
- Las instalaciones comerciales resuelven revestimientos y estructura después de pasarlos a coordenadas locales del mismo piso. Las colisiones continúan usando la geometría original.

El recorte puede aumentar la triangulación local: cambia solapamiento por porciones separadas. No se promete una ganancia de FPS. La medición nativa queda pendiente.

## Instalación

Detén Play. Copia la carpeta del parche **sobre la carpeta PoolroomsProcedural existente**, aunque esté en una subcarpeta de Assets. Conserva los demás recursos y `.meta`; no instales una segunda copia de los scripts. El paquete completo también permite una instalación nueva. Regenera la demo para sustituir geometría horneada previamente. La exploración procedural nueva usa la corrección al cargar sectores.

## Comprobaciones y límites

`--render-surfaces` comprueba 126 salas generadas en 63 modos, 12 sectores comerciales, 24 casas cercanas/lejanas del Nivel94, uniones, restauración tras descarga, área, UV, mezclas, idempotencia, exclusiones transparentes e inmutabilidad del origen. `Verification/surface_ownership_audit.json` contiene las muestras; son comprobaciones matemáticas y de código, **no renderizados de Unity**.

Se mantienen los 188 fingerprints protegidos y la comparación con R33 inmutable. Hay pruebas PlayMode para el collider original y carga/descarga de vecinos, aún sin ejecutar: aquí no existe Unity Editor.

Pendiente comprobar visualmente en Unity 6.3/HDRP: cámaras cercanas/lejanas, SSR/RT, paredes de transición, vuelo, carga y descarga rápida, ausencia de huecos, agua/cristales, sombras y costes CPU/GPU. Superficies curvas parcialmente coincidentes, transparencias, mipmaps, aliasing especular y materiales personalizados pueden necesitar diagnósticos distintos; no se afirma corregir cualquier clase de parpadeo.
