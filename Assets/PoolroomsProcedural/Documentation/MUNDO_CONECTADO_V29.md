# v29 — Nivel 94 dentro del mundo compartido

Esta entrega continúa la conexión de generadores de v28. Las colinas ya ocupan espacio dentro del mapa de salas: entrar caminando no cambia de ubicación, no oculta las salas ni envía el terreno a y=1200. Se conserva el modo antiguo para pruebas al desactivar Exploración unificada.

## Qué cambia

- Distritos de aproximadamente 2.6–4.3 km por lado, dimensionados por semilla y redondeados a la reserva del mapa. Hay un primer distrito al sur del inicio y otros aleatorios en bloques de 192 celdas. No se impone una secuencia de ambientes ni se cambia la región en cada visita.
- Una reserva impide construir salas o centros comerciales dentro de las colinas. Los centros comerciales también mantienen sus accesos fuera del terreno. El anillo de salas alrededor de cada reserva conserva rutas recíprocas y conecta los cuatro vestíbulos.
- Cuatro entradas reales, una por lado, unen los vestíbulos con el terreno. El suelo de los accesos coincide con una franja plana del paisaje; las colinas suben gradualmente en los siguientes 384 m. El terreno, los caminos y el horizonte se recortan al borde de la reserva. No hay una pared o plano de teletransporte dentro de la entrada; el paisaje continúa detrás y a ambos lados del umbral.
- La carga comienza al acercarte. Los accesos se cierran mientras falta el soporte del destino; el controlador conserva su espera ante sectores sin colisión. Se mantienen sectores cercanos con colisión y casas completas, sectores lejanos de menor detalle y horizonte visual. Las subidas de terreno se limitan a una por fotograma, además de los presupuestos independientes de salas y comercio.
- Se reutilizan las casas de uno y dos pisos, interiores, escaleras, patios, muebles y objetos que ya existían. Las bases de las casas siguen el nuevo perfil del terreno, y las identidades de objetos permanecen estables por semilla, distrito, casa y posición del catálogo.
- El TP maestro de Nivel 94 y Tools > Poolrooms > Nivel 94 v24 > Visitar colinas visitan un distrito real del mapa. La búsqueda inspecciona los bloques vecinos para no escoger una región lejana solo por estar en el mismo bloque. Volver al interior desde Tools lleva al vestíbulo más cercano; también puedes regresar andando.
- El origen flotante desplaza salas, edificios, paisaje, casas y objetos juntos. Solo se mantiene el distrito de terreno cercano: la separación mínima de reservas supera la suma de los radios máximos de precarga. El mapa no se construye entero ni carga cientos de casas completas a la vez.
- Se conservan materiales de césped, tierra, fachadas, madera, cubierta y cristales, separados de Poolrooms. El cielo, niebla, exposición exterior/interior y sol del Nivel 94 usan su plan de iluminación. El sol precargado deja ver el terreno desde la entrada y consume una plaza del presupuesto de sombras. Con sombras desactivadas, el modo conectado usa iluminación ambiental del cielo y desactiva el sol directo para evitar que atraviese los techos del mundo interior.
- La enumeración del mundo incluye salas, comercio y terreno a la vez. El paisaje utiliza reflejos del cielo y queda fuera del grupo de sondas; se conserva el límite compartido de dos sondas de v27.1.
- Frecuencia 0 o generador conectado desactivado no crean por accidente un distrito antiguo escondido mediante un portal o TP. El modo unificado respeta esa desactivación. Para las pruebas antiguas existe Exploración unificada = false.

Los interiores ampliados, juegos infantiles, supermercados pequeños, oficinas, hoteles, salas de espera, comedores, centros comerciales completos, Poolrooms solares/oscuras, pasillos variables y uniones directas siguen activos en el perfil temporal del modo unificado. Esta entrega conecta el terreno del Nivel 94; no añade nuevos tipos de ambiente ni reemplaza el mobiliario existente.

## Instalar y comprobar en Unity

1. Sal de Play. Actualiza Assets/PoolroomsProcedural con la carpeta del ZIP v29. Conserva .meta, tu escena, el Explorer, el perfil y sus materiales. No importes Verification ni otro proyecto .NET dentro de Assets.
2. Usa tu escena normal y activa Exploración unificada en Poolrooms World. No necesitas crear una demo nueva ni ejecutar cada menú de versión. Si instalas desde cero, Crear demo prepara el perfil unificado; Crear demo de 170 zonas sigue siendo una vista del catálogo.
3. En el perfil, Nivel 94 conectado debe estar activo y Connected Terrain Share debe ser mayor que 0 (valor inicial 0.25). Centros comerciales conectados conserva su frecuencia independiente. Estos controles se ajustan antes de Play; la activación del resto ocurre sobre una copia temporal sin editar el perfil guardado.
4. Para probar rápido: Y, /unlock mastermode, M y viaje a Nivel 94. Apareces en el centro del distrito de colinas, no en una sala de muestras. Para probar el acceso rápidamente usa Tools > Poolrooms > Nivel 94 v24 > Volver al interior (Play), que te deja en un vestíbulo del mismo distrito. Cruza andando en ambas direcciones y comprueba suelo, cambio de ambiente y coexistencia de salas/terreno.
5. En el menú maestro usa radios pequeños al empezar: salas, terreno y tiendas tienen ajustes separados. Terrain View Radius regula el terreno normal; el maestro conserva su rango 2–8. En el i3 N305/8 GB la carga completa de radios altos y la sombra solar necesitan medir FPS y RAM dentro de Unity.

En escenas antiguas con Exploración unificada desactivada se conserva la entrada por portal y la región separada, para comparar las versiones anteriores. La documentación v24/v28 describe esas entregas históricas; esta guía define el funcionamiento de v29.

## Comprobaciones ejecutadas

- Compilación del núcleo .NET 8: cero errores y advertencias.
- Regresiones completas: 220,413,481 aserciones aprobadas; incluyen arquitectura y pasillos, 170 recetas, iluminación, generación continua, mercados, centros completos v20/v21, interiores, atlas solar, generación autónoma, terreno anterior, mobiliario, agua, cámara, sonido, ajustes y movimiento.
- Pruebas nuevas de terreno conectado: 171 ubicaciones deterministas sin solaparse, 48 umbrales con soporte y paso libre, 2,762 bases de casas, recorte de caminos/suelo/horizonte a la reserva, bordes a altura 0, transición transitable, identidades de objetos y TP al distrito más cercano. Comprobaron 1,047,120 aristas recíprocas y conectividad combinada de salas, comercio y terreno en 12 combinaciones de semilla/escala. Total: 3,540,194 aserciones.
- Regresión específica de centros conectados: 2,703,781 aserciones aprobadas.
- Sintaxis C#9: 135 scripts × cuatro configuraciones, cero errores; seis asmdef válidos. Los rayos prueban mallas del núcleo, y las pruebas del grafo incluyen las reservas virtuales de distritos; esto no ejecuta PhysX.
- ConnectedTerrainWorldTests añade pruebas PlayMode de entrada y salida con CharacterController, perfil original intacto, casas y objetos, coexistencia de generadores, TP, origen flotante y ausencia de mapas ocultos cuando se desactiva un generador. Están incluidas, pero no ejecutadas. También siguen incluidas las pruebas nativas comerciales y del modo antiguo.
- Archivos de linterna, cámara de efectos y Explorer idénticos a v28. No se modifica la linterna ni el doble salto.

**Pendiente en Unity 6.3/HDRP 17.3:** compilación completa, shaders, aspecto y transiciones de iluminación, PhysX al caminar por umbrales/casas, interacción, reflejos, FPS, RAM y VRAM. Este entorno no tiene Unity Editor. Pasar el núcleo y analizar sintaxis no valida esos resultados.
