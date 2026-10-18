# Skins humanoides y animación común · R28

R28 sustituye la silueta neutral y la animación propia por skin de R27.
La apariencia inicial queda vacía hasta que importes tu modelo; el cuerpo físico
sigue siendo el mismo. Las ocho perspectivas y el modo foto continúan disponibles.

## Importar

Abre **Pausa > Ajustes > Skins**, pulsa **Examinar**, elige un modelo y pulsa
**Importar**. También puedes pegar su ruta completa. En Windows hay selector nativo;
en otros reproductores de escritorio se puede pegar la ruta. En el Editor se usa
el selector de Unity. La lista local incluye los archivos de la carpeta `Avatars`
en `Application.persistentDataPath` y las definiciones de Resources.

- **OBJ:** importación directa sin plugins, con MTL, color y mapas difusos locales
  PNG/JPEG. OBJ no contiene huesos: R28 calcula un rig y cuatro pesos por vértice.
  Conserva UV, normales, submallas y sentido de caras. Límite: 16 MB, 150.000
  esquinas, 200.000 triángulos, 16 materiales; hasta ocho imágenes de 4096 px y
  16 millones de píxeles en total. No convierte un MTL simple en un acabado PBR
  avanzado que el archivo no incluya.
- **FBX, DAE, 3DS, DXF, BLEND:** preparación en el Editor mediante sus importadores
  disponibles; no se promete un lector FBX en el ejecutable. Examinar/Importar copia
  el modelo y texturas adyacentes seleccionadas a `Assets/ImportedSkins`, habilita
  Read/Write y crea la definición/prefab en `Assets/Resources/PoolroomsAvatars`.
  Límite del archivo: 128 MB; copia de texturas acotada a 32/128 MB. BLEND necesita
  Blender instalado. Revisa dependencias/texturas que no estuvieran junto al modelo.
  También sigue disponible Tools > Poolrooms > Personaje > Crear apariencia desde
  modelo seleccionado. Estas carpetas están fuera del código reemplazable.
- **GLB/glTF:** adaptador opcional para **Unity glTFast 6.7.1**, paquete
  `com.unity.cloud.gltfast`. Instálalo con Package Manager > Add package by name;
  no hace falta para OBJ. R28 detecta el paquete mediante su API pública y preserva
  sus tipos para IL2CPP. Límite del archivo principal: 64 MB; prepara también
  texturas/LOD razonables porque ese límite no acota todas las dependencias.
  El paquete no está instalado ni probado en este entorno. Sin él se muestra una
  explicación y se mantiene la apariencia anterior.

## Un mismo movimiento para todas las skins

Un Avatar **Humanoid válido de Unity** conserva sus huesos, pesos, materiales y
blendshapes. El sistema común dirige sus articulaciones y desactiva su controlador
por modelo; `controller` se mantiene como campo serializado histórico.

Un humanoide sin rig recibe un esqueleto estimado de **21 huesos**, cuatro pesos
normalizados por vértice e IK de dos segmentos en brazos y piernas. Los rigs
Generic no reconocidos se hornean una vez antes del ajuste. El cálculo es local,
no necesita servicios ni sube tu archivo. La adaptación total admite 150.000
vértices; para un modelo más complejo prepara un Humanoid/LOD en Unity.

Hay descanso con respiración, caminar/correr con zancada según movimiento, salto,
caída, dash, deslizamiento, natación, vuelo y postura sentada para vehículos o
atracciones. Las transiciones principales mezclan objetivos antes de resolver IK;
los apoyos de pies consultan el suelo con dos raycasts sin asignaciones a 20 Hz.
No se aplica root motion ni se cambia la cápsula, velocidad o colisión del jugador.

**B** reproduce el emote elegido: saludar, estirarse, celebrar, bailar, reverencia
o sentarse. Movimiento, salto, dash, deslizamiento o conducir lo interrumpen.
Pausa/foto detienen la animación. Ajusta intensidad y ritmo por separado de cámara.

## Ajustar una skin sin huesos

Usa una figura de pie, centrada, con cabeza y extremidades distinguibles. Indica
pose T/A/brazos abajo, eje Z vertical si corresponde y orientación. Ajusta hombros
y altura de caderas y pulsa **Recalcular adaptación**. La pose original ayuda a
comparar. El modo de piezas rígidas sirve para modelos segmentados, no para una
sola malla de cuerpo entero. Un fallo de importación conserva la skin anterior.

El ajuste por geometría es aproximado: ropa holgada, manos pegadas al torso,
accesorios enormes, proporciones extremas o mala topología pueden necesitar rig
manual. No equivale a un rig artístico ni garantiza deformación impecable para
cualquier humanoide. Los dedos, expresiones faciales y tela no reciben animación
especializada. La calidad final de una skin depende también del modelo que importes.

## Luz y materiales

Se usan copias de materiales para no modificar el archivo original. Las texturas
compatibles se conservan. El ajuste opcional de metal/suavidad puede desactivarse
para restaurar los escalares originales. La conversión Standard/URP a HDRP del
Editor es básica; revisa mapas PBR avanzados y transparencias.

En Cinemático RT/Ultra con hardware compatible, las mallas animadas se registran
como `DynamicGeometry`; las rígidas, `DynamicTransform`. En otras calidades usan
los reflejos y la luz disponibles. La skin recibe sombreado y puede proyectar
sombras; no añade rayos propios, luces ni sondas. En primera persona el cuerpo
queda oculto conservando la sombra opcional; el modo foto permite ocultarlo entero.

## Verificación y límites reales

`dotnet run --project Verification/Poolrooms.Checks.csproj -- --humanoid` comprueba
ajuste, pesos, simetría lateral, IK, estados y emotes a varias frecuencias, pausa,
cancelación e IDs. La suite completa conserva 188 fingerprints de geometría.
`Tests/PlayMode/HumanoidSkinTests.cs` comprueba malla deformada, bind poses,
transformaciones reflejadas y ausencia de cambios en el original cuando se ejecute
con Unity. La prueba está añadida, **no ejecutada en la nube**.

Siguen pendientes compilación real Unity/HDRP, importaciones de archivos reales,
selector Windows, retarget de avatares artísticos, shaders, deformación visual,
sombras/RT y FPS. No hay mediciones ni capturas nativas que certifiquen su aspecto.
Conserva licencias y atribución de las skins que elijas; no se añadieron modelos
externos nuevos ni se modificaron las fuentes de la linterna o la distancia de carga.
