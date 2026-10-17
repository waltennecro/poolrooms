> Documento histórico de v28. El Nivel 94 también está conectado en v29; consulta MUNDO_CONECTADO_V29.md.

# v28 — mundo liminal conectado, parte 1

Esta entrega integra físicamente los centros comerciales y activa la generación interior ya desarrollada en las demos normales. El proyecto sigue siendo un mundo liminal continuo con varios ambientes; Poolrooms es uno de ellos.

## Qué cambió

- Los centros comerciales completos de 2–6 pisos ocupan una huella reservada en las mismas coordenadas y altura que las salas. Las habitaciones dejan de generarse dentro de esa huella. No se oculta el mundo exterior ni se envía el edificio a y=-400 en el modo unificado.
- Dos vestíbulos y galerías transitables conectan los extremos del edificio con las salas. La fachada tiene aberturas reales; el suelo coincide con ambos umbrales. Puedes entrar caminando y salir por el otro extremo. Los accesos permanecen cerrados mientras falta el soporte físico del destino.
- Los edificios aparecen por semilla a lo largo del mapa; cambian sus pisos, tamaño, atrios, tragaluces, tiendas, mobiliario y mercancía usando los generadores v20–v25 existentes. La reserva se calcula con las mismas dimensiones que el edificio real. Se conserva un distrito inicial próximo a la zona de partida, y las demás posiciones varían por semilla.
- Se conserva la generación de supermercados pequeños con estantes. El centro comercial completo es otro tipo de edificio, no un reemplazo de todo el ambiente comercial.
- En Play, Exploración unificada crea una copia temporal del perfil y activa Expanded, SolarAtlas, composición y mobiliario autónomos, materiales independientes y arquitectura comercial detallada. El perfil guardado no se modifica. Poolrooms claras/oscuras, pasillos solares, uniones directas, juegos interiores, supermercados, oficinas, hoteles, salas de espera y comedores entran en el mismo generador de salas.
- El TP comercial del modo maestro y el botón comercial de Tools apuntan al edificio del mapa compartido. Son atajos de visita; los edificios no dependen de esos botones para generarse.
- El origen flotante desplaza edificios, salas y jugador juntos. Tiendas y plantas cercanas se cargan por sectores; los distritos lejanos se descargan. Puede haber varios edificios próximos cargados simultáneamente. Las subidas de geometría comercial se limitan a una por fotograma, además del presupuesto de salas existente.
- La iluminación del centro usa sus lámparas, tragaluces y haces solares locales existentes; los accesos nuevos también tienen aberturas y luz de día. No se añade una luz direccional global que ilumine todas las salas oscuras. Se conserva el grupo compartido de dos sondas de reflejos de v27.1.

## Instalar sin empezar desde cero

1. Sal de Play y actualiza Assets/PoolroomsProcedural con la carpeta del ZIP de v28. Conserva los archivos .meta y las carpetas que contienen tu escena, perfil y materiales.
2. Abre tu escena normal y pulsa Play. No necesitas crear otra demo ni ejecutar los menús v19/v21/v22/v25 uno por uno.
3. En Poolrooms World, Exploración unificada debe estar activa (valor inicial true). En el perfil, Centros comerciales conectados está activo por defecto y Frecuencia de centros comerciales vale 0.6. Un valor 0 desactiva los edificios.
4. Para comprobar el edificio rápidamente: Y, /unlock mastermode, menú M, viaje al centro comercial. Luego puedes salir andando por un acceso y continuar por las otras salas.

Si instalas desde cero, Crear demo ya prepara el modo unificado con las opciones nuevas. Crear demo de 170 zonas y las vistas Single/Catalog conservan su finalidad de prueba de recetas y no activan la migración automática. Para comparar con el funcionamiento v27, desactiva Exploración unificada. El paquete para Assets no incluye Verification ni código de Roslyn/.NET externo.

## Rendimiento y visibilidad

Frecuencia controla la probabilidad de un edificio en cada bloque espacial de 24×24 celdas; no es un orden obligatorio de ambientes. Las salas siguen usando sus regiones y semillas. El perímetro de cada reserva mantiene conectado el grafo de habitaciones para no cortar caminos anteriores.

Las salas que quedan fuera del radio se descargan normalmente. Mantener visible el mundo no significa cargarlo entero. Desde vuelo aumenta el radio de salas en el menú maestro para ver los alrededores del edificio; sectores y plantas comerciales conservan sus controles independientes. En el i3 N305 empieza con radios bajos. No se midió el coste real de estos cambios dentro de Unity.

## Alcance de esta parte

**El Nivel 94 todavía conserva su sistema de portales y espacio separado de v24/v27.** Sus colinas, casas, iluminación, objetos y TP se conservan, pero esta entrega no los coloca físicamente entre las habitaciones. Su integración al mapa compartido es la siguiente parte: necesita reservar una región exterior grande y resolver sus bordes de terreno sin atravesar edificios. No se presenta como algo ya terminado.

La iluminación solar de las habitaciones se activa por gramática y clima regional, no en todas las salas. La apariencia final del centro y del atlas solar aún debe comprobarse dentro de HDRP.

## Verificación

Compilación del núcleo .NET 8: cero errores y advertencias. Batería del núcleo existente: 220,413,481 comprobaciones aprobadas, incluyendo arquitectura, conexiones, recetas, solar, mobiliario, agua, cámara, sonido y movimiento. Prueba nueva: 393 emplazamientos deterministas sin solaparse, 24 galerías con suelo continuo y rutas libres (también en los sectores amueblados de entrada), 393,120 aristas recíprocas y grafos conectados en 12 combinaciones de semilla/escala. Total de la batería nueva: 2,703,781 comprobaciones.

Sintaxis: 131 scripts C#9 × 4 configuraciones, cero errores; seis asmdef válidos. Esto no compila las API de Unity/HDRP.

ConnectedMallWorldTests incluye una prueba PlayMode de migración automática, perfil guardado intacto, soporte físico de accesos, TP al edificio compartido, coexistencia con salas y origen flotante. Se incluyó pero no se ejecutó, porque este entorno no tiene Unity Editor. Las pruebas anteriores que verifican recetas o portales antiguos seleccionan explícitamente el modo anterior; no se deshabilitaron sus aserciones.

Pendiente en Unity 6.3 / HDRP 17.3: compilación completa, PhysX/controller al cruzar, interacción real, shaders, aspecto solar, reflejos y FPS/RAM. Pasar el núcleo no demuestra esos resultados.
