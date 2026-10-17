# R8: aeródromos City11 y primer avión civil

Esta etapa reconstruye parte de los vehículos solicitados; no recupera archivos originales desaparecidos de v40–v47. Conserva R1–R7 y la linterna original.

## Encuentro e integración

`PoolroomsProfile.enableAircraft` activa aeródromos en el mundo continuo City11/Expanded, con celdas de al menos 28 m. Cada emplazamiento ocupa tres por siete celdas, solo cuando **todas** pertenecen realmente a City11 y no invaden reservas, accesos de Nivel 94 ni entradas comerciales. No es una escena aislada ni sustituye biomas para forzar una pista. La ubicación depende de la semilla y no aparece en todos los distritos. Desactivar la opción antes de generar conserva las manzanas anteriores.

La pista comparte suelo, alineación de marcas e iluminación solar regional; sus conexiones internas son recíprocas y de ancho completo. Los cierres temporales de streaming usan ese ancho mientras falta el vecino. La terminal tiene marquesina, bancos, luz cálida y emisor del mismo tono. El asfalto añade tres mapas originales de 1024 px; no añade sondas de reflexión. Modo maestro → **Visitar aeródromo de City11** busca un emplazamiento existente y deja al jugador junto al avión.

## Modelo y controles

Un modelo nuevo distinto: **Cesium Air**, procedente del repositorio oficial CesiumJS, revisión fijada en `Sources/Aircraft/sources.lock.json`. Licencia **Apache-2.0**, no CC0 ni dominio público; licencia y aviso de modificaciones se distribuyen dentro de Resources. Original GLB conservado fuera de Assets. La adaptación tiene 5.984 triángulos, PBR, dos motores con pivotes nativos y cuatro grupos materiales de hélice. No hay cabina interior modelada: se usa cámara exterior con retracción ante obstáculos.

- **E**: pilotar; volver a pulsar tras aterrizar y detenerse para salir por un lugar con suelo y espacio para el jugador.
- **W/S**: aumentar/reducir potencia persistente; soltar W mantiene la potencia.
- **A/D**: girar; **Espacio**: ascender; **Ctrl**: descender y frenar.
- Escape/F7 y chat mantienen sus bloqueos existentes. Abrir menús congela el cuerpo y restaura su velocidad al continuar.

Vuelo asistido con gravedad, sustentación gradual, fuerzas acotadas, límite de altura suave, colliders compuestos y contactos de baja fricción. El controlador vigila la carga del suelo próximo y se detiene si falta. El avión ocupado sigue el origen flotante; los coches conservan sus WheelColliders y controles. No hay daño, muerte, combustible ni desafíos.

## Rendimiento y límites

Solo se simula el avión pilotado. Un avión usado se separa del chunk para que descargar la pista no lo destruya; al abandonarlo queda cinemático y se elimina cuando está muy lejos. Como los coches actuales, la marca de recogido evita duplicarlo en su punto original durante esa sesión: **no existe guardado persistente de vehículos aparcados**. No se mantiene activo un aeropuerto entero fuera de la distancia de carga configurada.

Catálogo acumulado: seis modelos externos distintos (cinco CC0 y un Apache-2.0): tres objetos pequeños, un sillón, un coche y un avión. Dos clases de vehículo implementadas; 35 vehículos distintos y el helicóptero **siguen pendientes**. Las 21 celdas, repeticiones de aviones y LOD no son modelos nuevos. Se revisó el kit CC0 de Kenney, accesible nuevamente; su aspecto estilizado no se integró en esta etapa.

## Comprobaciones ejecutadas

`dotnet run --project Verification/Poolrooms.Checks.csproj`: núcleo compilado, 189 celdas de nueve aeródromos (tres tamaños/tres semillas), soporte en uniones, conexiones, determinismo, reservas de mundo mixto y controles/fuerzas en cinco pasos temporales. Se mantienen las 108 huellas SoftPlay y 80 de otros biomas. Sintaxis C# 9 de 184 scripts en cuatro configuraciones. Resultado: 16.922.764 aserciones del núcleo; no son pruebas nativas de Unity.

Auditorías de cinco fuentes CC0 y una Apache-2.0, licencias, hashes, PNG/OBJ y mapas; 27 mapas PBR originales verificados y pruebas analíticas del horneador UV. Pruebas PlayMode nuevas de importación/materiales/hélices y paso físico entre celdas incluidas **sin ejecutar**.

Faltan Unity 6.3/HDRP 17.3: compilación de APIs, importación OBJ/materiales, física real de despegue/aterrizaje, controles/cámara, shaders/RT, apariencia, memoria y FPS. Las fuerzas matemáticas y los hashes no prueban esos resultados.
