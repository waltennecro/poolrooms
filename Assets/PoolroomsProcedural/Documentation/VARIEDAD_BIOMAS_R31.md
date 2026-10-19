# R31 · Más salas y zonas por bioma

Esta ampliación interpreta el 50 % como **variedad de recetas y programas construidos**.
El mundo ya continúa generando salas al explorar. No se aumenta el radio de carga,
la distancia de renderizado, el tamaño territorial de los biomas ni se multiplican los objetos/lámparas
por sala para simular ese porcentaje; se conservan sus presupuestos.

## Catálogos ampliados

| Generador | Antes | Ahora | Cambio físico |
| --- | ---: | ---: | --- |
| Poolrooms de recetas | 170 | 255 | 85 cubiertas de arcos segmentales o cornisas escalonadas; nuevas alturas |
| Poolrooms solares | 768 | 1.152 | Dos clases espaciales nuevas, anchuras, alturas y contornos diferentes |
| Parque acuático | 5.600 | 8.400 | Dos clases de altura más: torres, escaleras, recorridos y bóvedas |
| Orangerie | 2.500 | 3.750 | Dos alturas de jardín nuevas y cubiertas construidas |
| Baños de travertino | 2.500 | 3.750 | Salones más altos y nuevos programas de cubierta; profundidades seguras |
| Casas de lluvia | 2.500 | 3.750 | Dos perfiles nuevos de paredes y pendiente de tejado |
| Casas del Nivel 94 | 800 | 1.200 | 30 plantas conectadas × 40 perfiles verticales; antes eran 20 plantas |
| Envolventes comerciales | 50 | 75 | 5 alturas × 5 frentes × 3 fondos: 96, 112 y 128 m |

Son **22.332 recetas/envolventes de estos ocho catálogos**, frente a 14.888: +50 %.
No son 22.332 modelos descargados ni salas cargadas a la vez. Los catálogos usan
mobiliario, materiales e interacciones ya existentes; se conservan sus licencias.
Los centros mantienen entre dos y seis pisos reales.

Otros generadores no tienen un catálogo finito de habitaciones: combinan dimensiones,
plantas y mobiliario por dirección. En ellos se amplían los programas principales:

- Biblioteca, Patio 188 y Costa 48: 6 → 9. Nuevas cubiertas, ancho del patio y pérgolas.
- Conservatorio y Galería de tránsito: 8 → 12, con cubiertas diferenciadas.
- Ciudad 11: 6 → 9 tipologías de fachada, lucernarios de cubierta, parasoles y petos escalonados.
- Juegos interiores originales: 6 → 9 módulos disponibles, con escaleras acolchadas,
  postes de eslalon y galerías de aros transitables.
- Áreas infantiles multinivel: 3 → 5 programas de cubierta.
- Oficinas, hoteles, esperas y comedores: 3 → 5 programas, nuevas particiones y
  selecciones de mobiliario, manteniendo camas y accesos protegidos.
- Supermercados anteriores: 3 → 5 programas de pasillos y amueblado.

Donde el conjunto anterior tenía tres elementos se redondea hacia arriba: cinco
(+66,7 %), para alcanzar al menos el 50 % sin fracciones de un programa.
Las 250 familias de toboganes permanecen: se amplían las salas que las contienen.

## Integración y compatibilidad

Exploración unificada activa la ampliación automáticamente, incluso al abrir la
escena anterior. Los mundos personalizados usan `PoolroomsProfile.expandedVariety`.
No hace falta recrear la demo. Las herramientas de catálogo muestran 255 recetas,
pero no son necesarias para encontrarlas al caminar.

Los IDs previos conservan su significado. El núcleo mantiene selectores antiguos
con `ExpandedVariety=false` para comprobar las 188 geometrías protegidas. Las nuevas
casas conservan sus nuevas plantas al pasar por la generación por dirección; no se
pierden al generar la estructura. Centro comercial y reserva territorial comparten
la misma selección de profundidad. Se mantienen entradas y rutas hacia otros niveles.
La semilla es determinista, pero al activar la ampliación cambia la selección de
recetas en direcciones conocidas. No se modifica la linterna.

## Validación

`--variety-expansion` comprueba los nuevos planes en seis escalas, permutaciones
completas con coordenadas negativas, nuevas familias alcanzables, plantas conectadas,
75 envolventes comerciales y muestras de geometría física diferentes. Incluye
pruebas de espacio libre para cuerpo/cámara en las nuevas alturas de tobogán. La auditoría
`--generation-audit` incluye ahora todos los 8.400/3.750 IDs de estos niveles.

Las regresiones nativas añaden diez casos de escaleras/sótanos/áticos de las nuevas
casas y dos de espacio libre bajo las cubiertas infantiles. **No se han ejecutado**:
este entorno no tiene Unity. También quedan pendientes la compilación Unity/HDRP,
la física real de los nuevos módulos, el aspecto, la carga y los FPS. El número de
aserciones del núcleo no es un número de pruebas nativas. La geometría adicional
sí tiene un coste; no se promete un FPS idéntico sin medirlo en el equipo del usuario.
