# v37 — Plantas y subdivisiones por semilla

La generación activa ahora calcula plantas de casas y divisiones de interiores. Esta entrega continúa sobre v36 e incluye sus correcciones de colisiones, carga por fases y sonido del deslizamiento.

## Qué genera

Las casas del Nivel 94 crecen desde el núcleo de entrada y escalera. El algoritmo añade módulos vecinos hasta formar una planta conectada de 6 a 12 módulos, dentro de una envolvente máxima de 28 × 28 m. Puede crear alas, retranqueos y huecos interiores; no selecciona una de las veinte plantas anteriores. Se conserva el módulo de 7 m para mantener la compatibilidad entre suelos, tejados, mobiliario y escaleras.

Cada planta conserva de uno a cinco pisos reales, la posibilidad de sótano y ático, el color anterior, las habitaciones y los objetos agarrables. Los pasos entre módulos pueden ser portales de distintas anchuras o aperturas que unen habitaciones en salas amplias; estas decisiones varían por planta vertical. La escalera y su desembarco mantienen sus medidas protegidas. La inclinación del tejado también varía por semilla.

En oficinas, hoteles, salas de espera y comedores, el generador subdivide las fachadas interiores con cortes desiguales y profundidades variables. Cuando hay espacio, añade una galería lateral y una transversal para acceder a un ala trasera. En las salas habituales de 36 m puede usar un vestíbulo abierto delante del ala, para que esta arquitectura no dependa de recintos grandes. Las habitaciones pueden abrirse a estas galerías, en vez de estar todas sobre el cruce principal. Hay un máximo de 16 habitaciones por recinto; en la muestra de pruebas aparecieron hasta 15.

El mobiliario se coloca después de reservar esas galerías y las entradas de cada habitación. Las habitaciones mantienen el programa de su ambiente: camas en hoteles, puestos de trabajo en oficinas, etc. El cruce que conecta las puertas del mundo sigue libre y al mismo nivel.

La arquitectura propia de centros comerciales, supermercados anteriores, juegos infantiles y Poolrooms sigue integrada en el mismo mundo. Esta modificación amplía las plantas de casas y los cuatro programas de interiores mencionados; no reemplaza la estructura comercial ni las piscinas por estos planos.

## Semillas y carga

Cada dirección calcula siempre la misma planta en v37, incluso al descargarla y volver a cargarla. No existe un catálogo global que guarde todas las plantas visitadas: las casas pertenecen al plan de su región y se liberan con ella. La geometría continúa preparándose con el sistema existente de trabajos y publicación por fases.

Las casas lejanas mantienen el mismo exterior, pero no generan muebles, objetos, escaleras interiores ni luces propias. Se conservan los límites de carga, pasto y luces de v36; no se incrementa automáticamente la distancia de renderizado. La muestra comprobada de nuevas casas llegó a 40 988 triángulos próximas y 9 088 lejanas, dentro de los límites existentes de las pruebas.

Las semillas de v37 no conservan exactamente las plantas nuevas de v36: es una modificación del algoritmo. Los 800 modelos del catálogo anterior siguen disponibles para selección explícita y mantienen sus pruebas. Los identificadores de catálogo describen el perfil de una casa generada; su forma completa depende también de la semilla y las coordenadas. Los objetos conservan claves por región, casa y posición lógica.

La variedad nace de reglas, con límites para cada edificio. Pueden volver a aparecer formas parecidas; esta actualización no promete que ninguna habitación vuelva a repetirse.

## Usarlo en Unity

1. Fuera de Play, copia la carpeta del ZIP sobre **Assets/PoolroomsProcedural**, sobrescribiendo los archivos y conservando los **.meta** existentes para mantener las referencias de tu escena. No pongas la carpeta de verificación de .NET dentro de Assets.
2. Usa tu escena habitual. Las casas nuevas aparecen en los distritos del Nivel 94 y en su TP actual. Los interiores nuevos aparecen en sus biomas habituales con generación autónoma activa; exploración unificada la activa al jugar y **Crear demo** ya la habilita.
3. Si estás probando una escena antigua con exploración unificada desactivada, activa **Autonomous Generation** en su perfil para ver las subdivisiones nuevas. La opción desactivada conserva los interiores anteriores.

No requiere otro plugin. Se conservan la linterna, sus intensidades, los efectos de cámara, doble salto, deslizamiento, modo maestro y ajustes gráficos.

## Comprobación

Se comprobaron 16 384 direcciones, con 2 513 formas de ocupación distintas contando solo la planta —sin pinturas, muebles, pisos ni tejados—, regeneración paralela, conectividad y límites. Se construyeron 256 casas próximas y lejanas, incluidas 64 de máxima altura: 12 301 conexiones de habitaciones, 726 tramos de escalera y 2 951 objetos con soporte geométrico.

Se comprobaron 4 608 planos interiores, 2 012 con galerías traseras, y 5 108 segmentos de acceso contra los triángulos reales de la geometría. Con la configuración habitual de celdas de 36 m y puertas de 4,4 m, 252 de 512 casos presentaron un ala trasera. También pasaron las regresiones de los 800 modelos anteriores, interiores anteriores, mundo conectado, centros comerciales, Poolrooms solares, pasto y seguridad de carga/sonido.

El núcleo compila con .NET 8. La verificación de sintaxis C# 9 cubre cuatro configuraciones de símbolos. Esto **no sustituye la compilación completa de Unity/HDRP**. Se incluyen dos pruebas nuevas de PhysX para galerías y escaleras con terreno excavado, pero no se han ejecutado: aquí no hay Unity Editor. Compilación Unity, shaders, apariencia, física nativa y FPS deben comprobarse allí.

Resultados: [VERIFICACION_V37.txt](VERIFICACION_V37.txt). Esquemas calculados por el núcleo, no capturas del juego: [Plantas_generadas_v37.png](Plantas_generadas_v37.png). Los datos de esos esquemas están en [Plantas_generadas_v37.json](Plantas_generadas_v37.json).
