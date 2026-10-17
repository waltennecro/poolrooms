# v35 — Casas variables del Nivel 94

Entrega acumulativa sobre v34 para Unity 6.3 / HDRP 17.3. Las casas nuevas forman parte de la generación habitual, tanto en el mundo conectado como al visitar el Nivel 94 desde el TP. No hay que ejecutar una herramienta de versión ni activar otro generador.

## Arquitectura y variedad

El catálogo contiene **800 variantes estructurales**: veinte plantas conectadas, cada una con cuarenta perfiles. Los perfiles combinan uno a cinco pisos sobre el terreno, presencia de un sótano, presencia de un ático y dos configuraciones de alcobas y pendiente del tejado. El ático es un nivel adicional; una casa máxima puede tener cinco pisos, ático y sótano. Los pisos están apilados físicamente, no repartidos en habitaciones del mismo nivel.

Las plantas incluyen alas a izquierda/derecha, casas en cruz, zigzag, galerías, claustros, patios, pabellones y extensiones ramificadas. Ocupan de 21 a 28 metros en cada eje. En varias familias, habitaciones contiguas se abren para formar salas domésticas más grandes. La arquitectura conserva las seis pinturas de v34 y sus materiales propios de fachada, madera, tela y techo. La selección de colores por semilla no se usa para aumentar la cifra de variantes.

Las habitaciones tienen dieciséis programas: sala, cocina, dormitorio, estudio, baño, lavandería, archivo, sala de espera, taller, despensa, comedor, almacenamiento, sala de música, guardarropa, salón casi vacío y cocina duplicada. Las familias convencionales asignan funciones domésticas; las más extrañas pueden mezclar archivos y salas de espera con cocinas repetidas o habitaciones de almacenamiento. Los sótanos y áticos reciben sus propios programas. La semilla cambia esos interiores manteniendo estable la estructura de cada dirección del mundo. Se incluyen muebles y objetos agarrables mediante el sistema existente, sin inventario.

Los perfiles de uno, dos, tres, cuatro y cinco pisos tienen probabilidades aproximadas del 40%, 30%, 15%, 10% y 5%. El sótano aparece aproximadamente en el 30% y el ático en el 45%. Las casas grandes son menos frecuentes. Se mantiene el número de parcelas de cada distrito y la semilla de cada casa. La casa junto a la entrada se mueve seis metros por eje y se ajustan los vecinos que lo necesiten para conservar el claro del portal y seis metros libres entre envolventes.

## Accesos reales y terreno

Cada planta tiene un vestíbulo de 7 metros con escaleras en U. Cada tramo tiene 1.6 metros de ancho, nueve peldaños y una subida de 1.6 metros; dos tramos conectan los pisos separados 3.2 metros. Los descansos y las aberturas del suelo usan las mismas medidas. Los peldaños tienen estructura delgada para dejar espacio para la cabeza bajo la escalera del siguiente piso. Hay pasamanos y barandillas alrededor del hueco.

Para descender al sótano se recorta la malla del terreno justo bajo el hueco de escalera. El resto del suelo de la casa cubre el sótano. La excavación se reproduce en las resoluciones de terreno 8/16/32 y en ambos lados de un fragmento. El collider usa la cantidad real de índices del suelo, excluyendo faldones visuales; ya no supone que todas las parcelas tienen una cuadrícula completa.

Los caminos llegan a la posición real de la puerta, incluso cuando está desplazada del centro. Las consultas de interior buscan parcelas vecinas para reconocer una casa que cruza un borde. La exposición se suaviza también verticalmente cerca de la entrada para que no salte al subir o bajar entre sótano y pisos superiores.

## Carga y rendimiento

La geometría se construye con el trabajador existente. Las casas cercanas tienen interiores, colisiones, muebles y objetos; las lejanas conservan la silueta, ventanas y tejado, sin muebles, objetos ni lámparas interiores. Las ventanas cercanas usan marcos de caras dobles compactas; las lejanas omiten ese detalle.

Cada casa tiene un máximo de 32 objetos agarrables y dos puntos de iluminación por nivel. El presupuesto global existente elige las luces activas más cercanas. Se mantienen los objetos de patio y las torres de agua ocasionales, buscando ubicaciones que eviten otras casas. No se añade una sonda de reflexión por casa.

Se mantienen materiales PBR, cielo, sol y exposición del Nivel 94, pasto Desactivado/Económico/Alto, sus límites por distancia y los ajustes de F7. También se conservan las demás zonas y las funciones del jugador, cámara y linterna. En tu i3 N305 conviene comenzar con Rendimiento, acabado Económico, pasto Económico o apagado y distancia baja. Las casas ahora son más complejas: no se han medido FPS en ese equipo.

## Actualizar y comprobar en Unity

1. Sal de Play y actualiza **Assets/PoolroomsProcedural** con la carpeta del ZIP. Conserva escenas, perfiles y los .meta existentes. No copies Verification ni proyectos .NET dentro de Assets.
2. Espera a que Unity importe y compile. Abre tu escena habitual y entra al Nivel 94. Si ya tienes una demo, no hace falta crear otra.
3. Comprueba varias casas: puertas, ambos tramos de la escalera, ático, sótano y objetos. Acércate y aléjate para revisar la transición de detalle. F7 mantiene los controles gráficos.
4. En Test Runner ejecuta **TerrainHouseVarietyTests**, **TerrainRegionTests**, **TerrainAppearanceTests**, **TerrainRenderingSettingsTests** y **ConnectedTerrainWorldTests**. Se incluyen pruebas PhysX y de recorrido con CharacterController, pero no se han ejecutado en esta nube.

## Evidencia y límites

Casas_Nivel94_v35.json incluye los 800 IDs, plantas, alturas, perfiles y huellas SHA-256 de triángulos estructurales, excluyendo pintura y mobiliario. Se comprobaron las 800 configuraciones cercanas y lejanas, conectividad de las plantas, pasos con ancho de cápsula, apoyo de suelos, 27,520 conexiones de puertas, 2,400 vuelos completos de escalera y 7,892 objetos apoyados. Aparecieron las 800 variantes en 58,810 casas de 32 distritos. Se añadieron 384 casas grandes con distintas semillas para revisar el presupuesto, además de casas reales con patios y torres.

El núcleo compila con cero errores y advertencias. Pasaron 130,648,125 aserciones; 153 scripts se analizaron en cuatro configuraciones de símbolos, con cero errores de sintaxis. Los máximos muestreados fueron 43,116 triángulos por casa cercana y 9,192 por casa lejana. VERIFICACION_V35.txt contiene los resultados. Las pruebas históricas de las casas rectangulares se conservan explícitamente con variedHouses=false para mantener su comparación con v33; las nuevas casas se verifican en HouseVarietyChecks. Las comprobaciones matemáticas de triángulos no sustituyen a PhysX.

**Pendiente en Unity:** compilación contra las API de Unity/HDRP, shaders, apariencia, colisiones nativas y FPS. Unity Editor no está instalado en este entorno. El catálogo acredita variantes geométricas procedurales; no es una colección de 800 modelos artísticos hechos a mano ni capturas del juego.
