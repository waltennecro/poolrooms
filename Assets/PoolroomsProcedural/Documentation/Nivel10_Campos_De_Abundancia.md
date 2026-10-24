# Nivel 10 · Campos de abundancia

Adaptación tranquila de **Bounty**, de The Backrooms Wiki. La investigación y
atribución están en [Level10.json](LevelResearch/Level10.json). Se leyó la descripción
completa, sus estructuras, atmósfera, agua, caminos, entradas y salidas mediante una
traducción pública fijada por commit. No se incorporaron fotografías ni texto copiado.

## Cómo encontrarlo

La exploración unificada activa `enableWheatFields10` automáticamente. Los caminos de
las afueras orientales de los distritos del Nivel 11 continúan entre campos de trigo.
Se puede ir y volver andando o llevar un vehículo por las rodadas. Los 64 destinos
anteriores conservan sus etiquetas de descubrimiento; parte de la periferia urbana
se convierte en campo. Las reservas de centros comerciales y Nivel 94 mantienen su
prioridad. El localizador busca otros distritos cuando la primera periferia está reservada.

Un perfil personalizado puede elegir `WheatFields10` en generación o activar
`enableWheatFields10` en `Expanded`. El nivel no depende del modo maestro ni de un portal.
Actualiza tu única carpeta PoolroomsProcedural. La movilidad de Travesía libre continúa.
Una demo ya abierta debe regenerar su mundo/volver a iniciar Play para utilizar el código nuevo.

## Paisaje y arquitectura

- Campo continuo por sectores, con pequeñas ondulaciones, setos y cuatro caminos
  de borde recíproco y pequeños grupos de robles con tres LOD. Las rodadas dejan una franja de hierba en medio.
- Doce programas: trigo abierto, granero con galería, establos, granero de semillas,
  patio de bomba, laguna, taller, patio de cosecha, nave de heno, semillero acristalado,
  pérgola y porche de carga. No son doce modelos descargados.
- Plantas con dimensiones sembradas, tres secciones de tejado —dos aguas,
  quebrado y linterna acristalada—, cerchas, postes, ventanas con huecos reales,
  puertas aparcadas junto a la fachada y varias distribuciones interiores.
- Los edificios que llevan altillo tienen escalera de dieciséis peldaños,
  descansillo continuo, galería y barandillas. El suelo de arriba no tapa la escalera.
- Mesas, tazas, cestas y sillas reutilizan modelos con licencia y dimensiones
  auditadas; los altillos amplios incorporan un rincón de lectura.
- Lagunas de agua transparente con fondo físico y superficie recortada contra la
  triangulación real del terreno. No hay planos de agua sobre el terreno seco.
- En los bordes hacia la ciudad el asfalto continúa como un camino rural abierto:
  suelo compartido, bordillos bajos, sin techo artificial ni luces de túnel.

## Interacciones e inventario

**E** usa el objeto que miras. Molino manual con manivela, bomba con palanca y
chorro visible, veleta orientable y armario con puerta animada. Las actividades
son opcionales y nunca cierran una salida. El cuaderno describe el lugar y los
caminos vecinos. Los bancos permiten sentarse.

El haz de trigo, el sobre de semillas y la ficha de los campos se guardan en el
inventario, se acumulan y pueden volver a sacarse como objetos físicos propios.
El estado de cada actividad sobrevive a descargar y recargar su sector durante la
misma sesión. El premio se reclama una sola vez; si el inventario está lleno, el
trabajo queda guardado. La persistencia entre sesiones sigue pospuesta.

Hay comida y agua de almendra con los modelos ya acreditados, sobre apoyos reales.
No se añaden hambre, sed, NPC, HUD ni obligaciones de supervivencia.

## Materiales, luz y sonido

Tres conjuntos CC0 nuevos: Grass001, Planks037A y WoodSiding009. Los doce mapas
suman **239.739.811 bytes**. Se conservan dimensiones nativas de hasta 4096 píxeles;
la madera de revestimiento mantiene su relación 4096×2048 y el relieve de 16 bits, importado como R16.
Los mapas HDRP incluyen normal +Y, oclusión y suavidad derivada de la rugosidad.
No hay ampliación de imágenes, copias de originales sin usar ni archivos de relleno.
La tierra de los caminos reutiliza una fuente ya existente. Procedencia y hashes:
`Resources/Level10/manifest.json`; licencias en `Resources/Level10/Licenses`.

Se reutiliza el sol regional, con luz de día nublado, cielo gris y exposición suave
al entrar en un granero. No se añade un sol por sector ni nuevas sondas de reflexión.
Los edificios y los lotes de trigo se incorporan al renderizado habitual; el modo de
ray tracing depende de GPU, HDRP y ajustes existentes. No se promete ray tracing sin GPU.

Cuatro clips originales de viento y roce del trigo, **3.365.002 bytes**, sin voces,
susurros ni golpes sorpresa. Un par de fuentes 3D compartidas por el oyente respeta
volumen, pausa, ambiente y filtrado bajo el agua. Las actividades reutilizan foley
acreditado. La escucha real todavía necesita Unity.

## Rendimiento y comprobación

El trigo se construye en dieciséis lotes y tres LOD, con raíces apoyadas en el
terreno y colisión reservada para arquitectura y suelo. La brisa solo actualiza
lotes próximos y visibles: máximo dos actualizaciones globales por fotograma y
hasta ocho por segundo/lote. No se recocinan colliders. El muestreo de alturas usa
una cuadrícula cacheada. Se conserva la distancia de renderizado configurada.

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --level10`
comprueba geometría, trayectos, apoyos, escaleras, agua, LOD, interacciones,
coordenadas extremas y entradas desde el Nivel 11. `tools/verify_level10_resources.py`
audita imágenes, profundidad, máscaras, audio, licencias y enlaces al código nativo.
`Level10WorldTests` añade cuatro pruebas nativas: formato R16, cápsula, escaleras e inventario físico.

**Límite:** las comprobaciones de Core y recursos no equivalen a ejecutar Unity.
Compilación con las APIs reales de Unity/HDRP, shaders, PhysX, apariencia, audición,
sensación de movimiento y FPS siguen pendientes hasta usar el Editor.
