# Auditoría de generación y carga · R30

## Fallos y cambios concretos

- **Selección de salas:** `FindNext` describía todas las candidatas antes de elegir una. Con radio 12, una selección creaba 625 planes en el hilo principal aunque solo enviaba una sala al trabajador de geometría. `RoomStreamingSearch` elige primero las coordenadas y describe únicamente la seleccionada. Conserva distancias, prioridad, desempates y accesos de R29. La comparación ejecutada reproduce 625 planes en la búsqueda anterior y uno en la nueva; no es una medición de FPS de Unity.
- **Bordes de coordenadas:** los bucles de búsqueda con contadores `int` podían desbordarse y no terminar cerca del límite de coordenadas. La selección nueva calcula y recorre límites recortados con `long`. Esto evita ese desbordamiento concreto; no convierte las coordenadas enteras en un dominio matemáticamente infinito.
- **Consultas de carga:** `IsPositionStreamed(point)` devolvía el estado del jugador dentro de centros comerciales/Nivel 94, incluso al consultar otro punto sin sus sectores o colisiones detalladas. Ahora cada sistema evalúa el punto solicitado. Una reserva comercial vacía tampoco se considera suelo de edificio: se comprueba la planta o el recorrido de sus accesos. Se conservan los vecinos de seguridad al cambiar de piso/bahía/tile.
- **Objetos soltados:** los objetos exteriores seguían simulándose cuando se retiraba la colisión de su zona. La protección antigua del Nivel 94 se ejecutaba solo mientras el jugador permanecía dentro y perdía el movimiento al reactivarlos. Carry supervisa ahora sus objetos soltados antes de la simulación, suspende los que no tienen carga de soporte y conserva sus velocidades lineal/angular y estado de reposo. Agarrar/soltar descarta el movimiento suspendido anterior. El límite de objetos sueltos sigue siendo 32; no añade guardado entre sesiones.
- **Sombras:** el árbitro de R29 conserva únicamente luces direccionales en su lista por frame. La búsqueda periódica de luces de escena sigue existiendo, pero las lámparas locales ya no se recorren en las dos pasadas de arbitraje de cada frame. No se alteran materiales, texturas, muestras, linterna ni distancia de renderizado.

## Comprobaciones ejecutables

`dotnet run --project Verification/Poolrooms.Checks.csproj -- --generation-audit`

- 78.600 recetas/escalas de los cuatro catálogos amplios: parque acuático, invernadero, termas y casas de lluvia.
- 3.168 planes espaciales adicionales: todos los 22 modos de generación, tres tamaños, dos puertas, tres semillas y ocho coordenadas por combinación.
- Total: **81.768 planes**, **396 salas completas con conexiones** y **1.395.345 colocaciones** resueltas mediante los catálogos/recursos reales. Se validan posiciones, normales, UV, índices y tamaños de buffers de las mallas muestreadas. Son instancias y combinaciones, no modelos nuevos.
- 384 comparaciones de orden de carga anterior/nuevo, mapas finitos completos, accesos de reserva y búsqueda en extremos enteros. Además se comprueba el recorrido de ambos accesos comerciales a seis escalas con tres semillas.
- La auditoría se añade a GitHub Actions junto al núcleo completo, las 188 geometrías protegidas, las 33.600 combinaciones acuáticas de R29, fuentes/licencias, texturas y empaquetado.

## Alcance real y pendiente

La auditoría del núcleo pasó sin nuevas excepciones de catálogo. Los fallos de consultas de carga y de objetos se identificaron revisando sus rutas de ejecución; **no se han reproducido dentro de Unity en la nube**. `RegionalStreamingSafetyTests.cs` añade tres pruebas nativas de consulta de reserva descargada, tile detallado solicitado y conservación/cancelación del movimiento de objetos. Las pruebas previas de `StreamingSafetyTests.cs` se conservan completas.

Faltan Unity Editor, proyecto HDRP y ejecución gráfica en este entorno. Siguen pendientes compilación de APIs Unity/HDRP, PlayMode, física real, apariencia y FPS. Cuando se pruebe el juego, comprobar: desplazamiento entre biomas, ambos accesos del centro comercial, cambios de piso, entrada/salida del Nivel 94 y dejar un objeto, alejarse hasta descargar la zona y regresar. No se declara el proyecto libre de todos los errores.
