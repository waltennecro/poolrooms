# v34 — Mejora gráfica del Nivel 94, con segunda pasada de refinamiento

Entrega acumulativa sobre v33. Se realizó una primera implementación y una segunda revisión de acabados, transiciones, carga y presupuesto antes de empaquetar esta única v34. Se conserva el mundo conectado, el Nivel 94 extenso y poblado, objetos interactuables, todos los demás biomas y las opciones de pasto anteriores.

## Terreno y materiales

Se sustituyen las 12 texturas originales de 256 px por 24 mapas PBR originales de 512 px: césped, tierra, grava, cubierta, revestimiento horizontal, madera, yeso y tela. Cada acabado incluye albedo, normal y máscara HDRP con metal/AO/detalle/suavidad. Son texturas repetibles generadas de forma determinista; no se incorporan fotos de referencia al juego.

El importador aplica mipmaps, compresión, repetición y anisotropía 4. Su versión cambia para que Unity reimporte también los mapas antiguos. Las máscaras y normales son lineales y el albedo usa sRGB. Se corrigió la doble multiplicación de suavidad de los materiales del terreno, que apagaba sus reflejos; los materiales no metálicos mantienen metal cero. La tela repite su tejido a menor escala y las piezas pintadas interiores tienen un acabado satinado independiente.

En acabado **Alto**, el suelo usa el shader nativo HDRP/LayeredLit con dos capas. La mezcla de tierra y césped combina variación amplia por semilla y hombros alrededor de caminos y accesos. Las mallas incluyen pesos de mezcla por vértice; no se añaden superficies transparentes sobre todo el terreno. Fragmentos y horizonte usan las mismas coordenadas del distrito para conservar continuidad. La consulta de caminos vecinos evita cortes en el material donde una entrada cruza un borde.

El acabado **Económico**, predeterminado, conserva el suelo de una sola capa HDRP/Lit con los nuevos mapas PBR. URP/Standard conservan el acabado compatible de una capa. Si el shader de mezcla no está disponible, se utiliza ese mismo material compatible. No se exige ray tracing.

## Casas

Se separan los materiales de cubierta, fachada e interior: el techo ya no pinta mesas y escaleras con su textura. Los muebles y pisos usan madera; sofás y cortinas usan tela; los lados interiores de las paredes exteriores usan yeso liso, sin superponer paredes físicas. La cara inferior del techo tiene madera y la cubierta exterior conserva las tejas.

Las casas cercanas incorporan remates inclinados de frontón, tapajuntas, cumbrera, canaletas, bajantes, ventilación de frontón, alféizares, dinteles, contraventanas por semilla, timbre y un pequeño aplique de entrada. El aplique no crea otra luz en tiempo real. Todo este acabado es geometría visual agrupada: no invade puertas, ventanas ni escaleras.

La estructura de colisión completa se mantiene separada de los acabados y se crea sin renderer, normales, tangentes o UV innecesarios. Las casas lejanas conservan el presupuesto anterior de tres renderers y no generan muebles, nuevos remates ni objetos agarrables. Solo el entorno inmediato utiliza la separación visual completa.

En 192 muestras detalladas/simplificadas, los triángulos físicos coinciden exactamente con los de la v33, independientemente del orden en la malla. Los recorridos de puertas, pasillos, escaleras y los soportes de objetos siguen pasando las pruebas del núcleo. Esto comprueba geometría, no sustituye las pruebas de PhysX en Unity.

## Cielo, sol y exposición

El cielo procedural incorpora nubes de cuatro escalas, sombreado horneado, bordes iluminados, halo solar, horizonte con bruma y un hemisferio inferior más oscuro. El disco solar corresponde a la dirección real de la luz. Se conservan las tres variantes por distrito: día claro, nublado suave y tarde cálida, con luz y bruma independientes del resto del mundo.

El cálculo del cielo se realiza en una tarea que usa solo el núcleo. Unity recibe como máximo una cara del cubemap por fotograma. Se conservan mipmaps y filtrado trilineal para las reflexiones del cielo. La actualización usa un único trabajo activo, cancela solicitudes antiguas y libera datos descartados. Al salir se conserva solamente el último cubemap y su semilla, sin retener la descripción de todas las casas del distrito. El cubemap se reutiliza si vuelves a la misma semilla/calidad.

La exposición exterior respeta el día elegido y cambia gradualmente hacia el interior. La entrada de una casa tiene una transición espacial: cruzar su umbral no cambia inmediatamente todo el brillo. Se preserva el control de exposición de Gráficos, incluida su respuesta durante pausa. No se cambian la linterna ni sus efectos de cámara.

El nuevo control **Acabado del Nivel 94** está en Pausa > Ajustes > Gráficos > Renderizado; también se abre con **F7**. Económico usa cielo de 128 px por cara y resolución solicitada de sombra solar de 512; Alto usa 256 y 2048 respectivamente, además del suelo mezclado. El presupuesto global de sombras y la alternativa sin RT siguen mandando. Si el presupuesto desactiva el sol del mundo conectado, la exposición exterior compensa la luz ambiental del cielo, conservando el objetivo interior para evitar que el modo económico quede excesivamente oscuro. La resolución efectiva depende de los límites del HDRP Asset. No se añaden nubes volumétricas en tiempo real ni sondas de reflexión por casa, evitando aumentar la presión del Reflection Probe Atlas.

Para conservar el shader nativo de mezcla en una compilación del juego, el editor prepara automáticamente un material de referencia en Resources cuando HDRP está disponible. También lo comprueba antes de compilar el jugador. No necesitas ejecutar otra herramienta de versión ni instalar un plugin. Los assets de materiales existentes no se sobrescriben: los materiales antiguos que conservan los valores automáticos se actualizan en memoria; una biblioteca personalizada conserva sus referencias y admite los nuevos campos roof/houseInterior.

## Pasto: segunda pasada de rendimiento

Las opciones Desactivado, Económico y Alto permanecen independientes del acabado del cielo/terreno. Desactivar sigue eliminando todas las mallas de pasto y conserva soporte, terreno y casas.

Económico mantiene nueve parches y un máximo total de 2,700 triángulos. Alto mantiene 25 parches pero ahora usa densidad por distancia: hasta 8,640 triángulos en el central, 3,888 en cada uno de los ocho vecinos y 1,296 en los dieciséis exteriores. El límite total es **60,480**, frente a los 216,000 de v33, aproximadamente un 72% menos en el máximo.

Al moverte se reducen primero los parches que pasan a estar lejos, se completan los que faltan y luego se aumenta el detalle cercano. Un contador impide superar el presupuesto incluso durante sustituciones. La nueva malla reemplaza la anterior al terminar, sin tocar colisiones. Se suavizan las normales de las hojas y sus tonos se acercan al césped base. No hay un GameObject por hoja, transparencia, colliders, sondas, RT ni animación de viento por fotograma.

## Instalar y probar

1. Sal de Play y actualiza **Assets/PoolroomsProcedural** con la carpeta del ZIP. Conserva escenas, perfiles y .meta existentes. No copies el repositorio completo ni Verification dentro de Assets.
2. Deja que Unity termine de importar y compilar. Usa tu escena habitual: no tienes que crear otra demo ni activar Nivel 94 de nuevo.
3. Abre F7 > Renderizado. En el i3 N305 con 8 GB y GPU integrada, comienza con Rendimiento, acabado Económico, pasto Económico o Desactivado y distancia del Nivel 94 2.
4. Visita Nivel 94 desde la exploración conectada o el TP existente. Revisa un camino que cruce un fragmento, la entrada y escalera de una casa, sus materiales y objetos; cambia calidad durante pausa y vuelve a salir al interior del mundo.

No se han medido FPS en tu PC. Alto se ofrece para equipos con más capacidad o para comparar; no promete mejorar el rendimiento de una GPU integrada.

## Comprobación

Compilación del núcleo .NET 8: cero errores y advertencias. Pasaron 45,331,507 aserciones entre las pruebas de acabado nuevo, terreno, pasto, conexión, mobiliario, movimiento/cámara/ajustes y políticas de renderizado. Se comprobaron 192 mallas físicas frente a la v33, 57,036 direcciones del cielo en las tres iluminaciones, caras y uniones de cubemap, pesos de mezcla, hombros continuos entre fragmentos, presupuestos y guardado/migración. Se validaron los 24 PNG, sus dimensiones, normales normalizadas, límites de relieve y canales de máscara.

Sintaxis: 150 scripts en cuatro configuraciones de símbolos y seis asmdef válidos; cero errores. Esto analiza sintaxis y enums, no compila contra las API de Unity.

TerrainAppearanceTests incluye comprobaciones nativas de subida de pesos, materiales separados, malla física sin renderer, cambio de calidad durante pausa con soporte intacto, guardado, pasto apagado y límite de geometría al moverse. Estas pruebas están **incluidas, no ejecutadas**.

**Pendiente dentro de Unity 6.3/HDRP 17.3:** compilación completa, shaders/variantes en una build, apariencia, sombras, reflexiones, PhysX y FPS. Unity Editor no está disponible en esta nube. Ejecuta TerrainAppearanceTests, TerrainRenderingSettingsTests, TerrainRegionTests y ConnectedTerrainWorldTests y prueba una build HDRP. El muestreo matemático del cielo y la revisión de texturas no son capturas del juego.

Linterna, efectos de cámara, controlador de movimiento, deslizamiento, arquitectura de centros comerciales/Poolrooms solares y agua se conservaron byte a byte respecto a v33. El paquete contiene únicamente la carpeta PoolroomsProcedural para Assets.
