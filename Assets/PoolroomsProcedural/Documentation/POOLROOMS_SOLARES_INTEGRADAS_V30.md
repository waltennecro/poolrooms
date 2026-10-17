# v30 — Poolrooms originales, solares y amplias en la misma generación

Las Poolrooms solares y amplias ahora participan en la selección habitual de salas, incluso en el generador Authored con CrossV12. No necesitan una demo solar, una escena diferente, un portal ni activar una herramienta. Las regiones cerradas usan la arquitectura original y las regiones con cielo usan el atlas solar existente.

## Arquitectura y frecuencia

- Mezcla activada por defecto en el perfil. Dentro del ambiente Poolrooms, aproximadamente la mitad de las regiones son solares y amplias y la otra mitad conserva las recetas originales. No cambia el peso de los demás ambientes del mundo ni significa que la mitad de todo el mapa sea Poolrooms solar.
- Regiones climáticas pequeñas, con tamaño inicial 3 en lugar de 5. Se seleccionan mediante coordenadas, semilla y regiones Voronoi; no hay una secuencia de salas, contador de visitas o cuadrícula alternada. El tamaño es una escala de generación, no una obligación de crear grupos de exactamente tres habitaciones.
- Se mantienen las 170 recetas originales y las siete familias clásicas. Una sala original conserva su familia, dimensiones y semilla de detalles anteriores. Las nuevas salas utilizan las 768 recetas del atlas: salas transversales, galerías largas, patios recortados y salas de doble altura, con piscinas, escaleras, arcos y columnas.
- La arquitectura solar conserva los contornos curvos, tragaluces y otras aperturas reales de malla, sol cálido, luz diurna difusa y atardecer. Las luminarias solares y su rebote usan las integraciones HDRP y presupuestos existentes. El aspecto final, las sombras y la exposición deben comprobarse dentro de Unity.
- Entre salas solares se conservan los nueve tipos de conexión del atlas, incluidos arcos compartidos, salas abiertas sin túnel, galerías solares, bóvedas, columnas y recorridos junto al agua. La probabilidad inicial de una unión directa entre dos salas solares elegibles sigue siendo 0.4. Las uniones con una sala original conservan el conector compatible con su puerta y desnivel, sin imponer una abertura ancha contra una pared estrecha.
- Las entradas a centros comerciales y Nivel 94 conservan sus propios planos y conectores. La mezcla no invade sus reservas ni sustituye sus vestíbulos por piscinas. Los otros ambientes conservan su generación, materiales y mobiliario independientes.
- Se corrigió una receta solar que podía fallar cuando cuatro accesos y esquinas redondeadas dejaban poco espacio para la piscina. Después de los intentos habituales, amplía la sala dentro de su celda y vuelve a distribuir la misma receta. Conserva la piscina y las rutas de paso; las aberturas compartidas mantienen el mismo ancho en ambos lados.
- El viaje a Poolrooms cerradas o solares admite la nueva mezcla. La búsqueda comprueba la sala realmente construida, además del clima, y conserva la exclusión de reservas comerciales y de terreno.

Las escenas Single/Catalog y los filtros de familia mantienen sus vistas de recetas, sin introducir el atlas en una muestra del catálogo. Exploración unificada desactivada conserva el modo anterior para comparación y pruebas. Desactivar solo la mezcla conserva la selección anterior del atlas, si ese atlas sigue activo en el perfil.

## Instalar y usar

1. Fuera de Play, actualiza una única carpeta Assets/PoolroomsProcedural con la carpeta del ZIP v30. Conserva tus .meta, perfil, materiales, escena y Explorer. Este ZIP es acumulativo y contiene las funciones anteriores; no importes Verification o un proyecto .NET dentro de Assets.
2. Usa tu escena habitual con Exploración unificada activada en Poolrooms World. No necesitas crear otra demo ni activar cada menú de Tools. Para empezar desde cero, Crear demo prepara el mundo normal; Crear demo de 170 zonas prepara una vista de catálogo.
3. En el perfil, el apartado «Poolrooms originales + solares / v30» contiene «Mezclar arquitectura original y solar» = activado, «Proporción de Poolrooms solares» = 0.5 y «Tamaño de regiones solares comunes» = 3. Comprueba esos valores antes de Play. El perfil guardado sigue intacto: el modo unificado trabaja sobre una copia temporal.
4. Explora o usa el viaje maestro a Poolrooms solares/cerradas para revisar ambas arquitecturas. En el modo mixto, los antiguos controles Sunlit Region Share y Pool Climate Region Size no gobiernan esta mezcla; sus tooltips lo explican. Los controles nuevos admiten proporción 0..1 y escala 2..24.
5. Se mantienen los radios y presupuestos de streaming, luces, sombras y reflejos. No se carga el mundo entero. Compara FPS/RAM con tus mismos ajustes en Unity: hacer más comunes las salas solares puede cambiar el coste visual y la nube no ha medido ese coste en tu i3 N305.

No se modifica el código de linterna, cámara de efectos ni Explorer. Se conservan doble salto, modo maestro, objetos interactuables y exploración sin muerte o desafíos. No se añaden criaturas ni anomalías.

## Comprobaciones

- Compilación real del núcleo .NET 8: cero errores y advertencias.
- Mezcla nueva: 2,992,345 aserciones aprobadas. Se inspeccionaron 33,124 salas en cuatro semillas: 16,509 solares y 16,615 originales, un 49.84 % solar. Aparecen las 768 recetas solares, las 170 originales y las cuatro variantes espaciales. Las siete familias clásicas también conservan su generación.
- En los cuatro inicios probados, ambas arquitecturas se encontraron siguiendo el grafo en tres conexiones o menos. Es un resultado de esas muestras, no una distancia máxima garantizada para cualquier semilla.
- Mallas comprobadas en tres escalas y tres modos de generación: 72 salas solares, 36 umbrales entre arquitectura original y solar y 36 uniones directas. Se comprobaron agua, rutas, anchura de puertas, soporte, aperturas de techo y coherencia de conexiones. La receta antes defectuosa también se construye y conserva agua transitable alrededor.
- Integración con el mundo: 36 vestíbulos comerciales/de terreno y sus salas vecinas, con mezcla y generación autónoma activas. Las entradas siguen secas, conservan paso y no reciben uniones solares incompatibles.
- Regresiones completas del núcleo: 220,413,481 aserciones aprobadas. Incluyen generación anterior, pasillos, recetas, iluminación, atlas, centros comerciales, terreno, mobiliario, cámara, sonido, agua, ajustes y movimiento.
- Sintaxis C#9: 137 scripts × cuatro configuraciones, seis asmdef válidos, 9,932 referencias explícitas a enumeraciones del núcleo y cero errores.
- CommonPoolroomWorldTests incluye comprobaciones nativas del perfil original sin activar el atlas y de streaming, llegada con colisión, fuentes solares y preservación del perfil. Están incluidas pero no ejecutadas; también se conservan las pruebas PlayMode anteriores.

**Pendiente en Unity 6.3 / HDRP 17.3:** compilación contra las API de Unity y HDRP, shaders, PhysX, apariencia, exposición y transiciones de luz, reflejos y FPS/RAM/VRAM. Este entorno no dispone de Unity Editor. Las pruebas del núcleo usan mallas y rayos matemáticos; no ejecutan PhysX ni renderizan una captura del juego.
