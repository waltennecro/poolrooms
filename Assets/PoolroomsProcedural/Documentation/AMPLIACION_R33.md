# R33 · Diez ambientes conectados y revisión del conjunto

Esta entrega es una ampliación de la reconstrucción actual. La v47 original no
se recuperó. Los cambios anteriores, sus IDs y las 188 geometrías protegidas se
conservan. No es un proyecto completo de Unity: se instala `PoolroomsProcedural`
dentro de `Assets` en un proyecto Unity 6.3/HDRP 17.3.

## Niveles y generación

Se añaden biblioteca del atardecer, terminal de nubes, pabellones de té, galería de
cerámica, jardín de invierno, galerías de canales, estudios del silencio, patios
de faroles, lavanderías de lino y santuario mineral. Cada programa construye su
arquitectura y mobiliario: estanterías, marquesinas, pérgolas, expositores curvos,
jardineras, puentes sobre vasos excavados, pianos, faroles, lavadoras y peristilos.

Hay 131.072 recetas por nivel: cuatro distribuciones y ocho valores para cada eje
de anchura, profundidad, altura, claraboya y ritmo estructural. Son **recetas de
diez familias**, no 100.000 familias estructurales distintas por nivel. La
permutación de coordenadas recorre todo el banco sin reducirlo a cambios de color.
El control `--restful-mesh-catalog` construye 20.001 mallas por nivel y exige
200.010 huellas diferentes a escala y coordenadas fijas, sin contar objetos,
colores ni iluminación. Su resultado real está en el inventario de comprobaciones.

Los diez niveles participan en el mundo unificado mediante regiones aleatorias,
puertas físicas y uniones caminables. No necesitan un menú de Tools, TP ni modo
maestro. Se conservan los demás ambientes; ahora son 28, además de los distritos
físicos de centros comerciales y Nivel 94. No existe un orden obligatorio.

## Objetos, licencias y coste

Cada sala nueva tiene 60 objetos que usan el sistema existente de agarrar/soltar,
con claves de persistencia por semilla, coordenada e índice. Los objetos de mesa
pasan de tres a nueve por alcoba: +200 %. El presupuesto solicitado de muebles
autónomos y los intentos de colocar recursos regionales también se multiplican
por tres; los límites de capacidad, apoyo, separación y circulación pueden reducir
la cantidad final en salas antiguas. Esto no triplica todos los muebles de cada
bioma ni elimina los presupuestos de seguridad.

El recuento de un área real se obtiene con `--restful-world`; excluye las reservas
de terreno y cuenta claves distintas, sin sumar repetidamente la misma sala de
prueba. Son instancias planificadas del mundo: el streaming carga un subconjunto.
No se modifica la distancia de renderizado ni se precargan millones de objetos.

Se descargan tres modelos nuevos: taza y florero CC0, y lámpara de Eric Chadwick /
Wayfair CC BY 4.0. Se incluyen originales, revisión fija, hashes, avisos,
atribuciones y dos LOD por modelo. Se reutilizan cinco fuentes licenciadas
anteriores. No son 20.000 modelos distintos. La anisotropía y la transmisión
difusa de glTF no se reproducen exactamente; el vidrio usa transparencia aproximada.

Los artículos idénticos del catálogo comparten mallas y materiales con referencias
contadas. Cada objeto mantiene su collider, identidad, agarre y física propios.
La última instancia libera el recurso; una instancia agarrada no depende de que
siga cargada su sala. Las pruebas nativas de esa duración de vida están añadidas,
pero no ejecutadas en este entorno.

## Agua, luz y sonido

El agua nativa obtiene tres bandas dispersivas de ondas de contacto con energía,
espuma y extensión limitadas. La alternativa Lit pasa de una superficie opaca a
agua transparente, dieléctrica y con micro normales periódicas originales de
1024 px. Aprovecha luces, SSR y sondas disponibles, sin añadir cámaras ni soles.
No es una certificación visual de equivalencia AAA.

Los nuevos niveles tienen aperturas reales en las cubiertas, materiales PBR
independientes y luz regional; conservan el árbitro de un único sol con sombras.
El modo Ultra y sus alternativas raster existentes siguen sujetos al hardware.
Un i3 N305 usa su GPU integrada: no obtiene ray tracing acelerado por añadir código.

Piscinas y ventilación tienen anclajes espaciales. Las paredes atenúan volumen y
agudos con transiciones suaves; la sumersión mantiene su filtrado. Se conservan
reverberación por sala, pasos, deslizamiento, pausa y origen flotante. Los dos
nuevos controles están en Configuración → Audio. Se realizan como máximo dos
consultas de oclusión cada 0,25 segundos; no se crea una fuente por objeto.

## Cinco pasadas y límites

Las cinco revisiones cubren contratos/compatibilidad, conexiones, recursos y
medios, colisión/regresiones y publicación reproducible. Se registran sus
resultados y correcciones en `AMPLIACION_R33_ESTADO.json` y el informe de
`Verification`. Las aserciones del núcleo no son pruebas nativas de Unity.

Se compila .NET con advertencias como errores y se verifica la sintaxis C# en
cinco configuraciones. Las pruebas de jugador usan `Physics.SyncTransforms`,
evitan el acceso obsoleto a `Physics.autoSyncTransforms` y no silencian sus avisos.
No se ha ejecutado el Editor: siguen pendientes compilación de APIs Unity/HDRP,
shaders, PhysX, audio real, apariencia, FPS y advertencias específicas del Editor.
No se garantiza ausencia absoluta de errores ni gráficos AAA sin GPU dedicada.
