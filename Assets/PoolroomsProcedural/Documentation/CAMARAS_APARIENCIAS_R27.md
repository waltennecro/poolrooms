# R27 — Perspectivas, fotografía y apariencias 3D

> Las apariencias evolucionan en [R28](SKINS_HUMANOIDES_R28.md): sección Skins, rig común y adaptación de modelos sin huesos. Las instrucciones de apariencia de esta página describen R27.

## Uso

Reemplaza únicamente `Assets/PoolroomsProcedural` fuera de Play, conservando
escenas y perfiles. El jugador instala el sistema automáticamente al iniciar.
No se regeneran habitaciones ni se cambia la distancia de renderizado.

**F5** recorre ocho perspectivas: primera persona, tercera cercana, tercera
lejana, frontal, hombro derecho, hombro izquierdo, cenital e isométrica.
También se seleccionan en **Pausa > Ajustes > Cámara**. Se guardan por separado
de los estilos ópticos Normal/VHS/etc.; los IDs anteriores se conservan.

La cámara externa comprueba obstrucciones con una esfera que contempla el
plano cercano y el encuadre; se contrae inmediatamente y recupera distancia
suavemente. Las consultas saturadas se cierran de forma conservadora.
Interacciones, dirección de vuelo y evaluación del ambiente sonoro usan un ancla
del personaje. El AudioListener conserva su ubicación en la cámara visible.
En vistas externas la linterna emite desde esa ancla; su código, potencia,
sombras y efectos se conservan. La cámara real sigue siendo la misma, con
sus estilos y HDRP. Primera persona oculta el cuerpo conservando sus sombras.

## Modo foto

**F6**, o el botón de pausa, abre fotografía una vez iniciado el recorrido.
Pausa movimiento, físicas y audio; conserva el estado anterior al salir,
desactivar el componente o perder el foco con pausa automática habilitada.
La cámara se desplaza por un radio local de 24 m sin mover al jugador ni
generar sectores desde su posición. Respeta geometría y no necesita modo maestro.

| Control | Acción |
| --- | --- |
| WASD; E/Q | Desplazamiento; subir/bajar |
| Botón derecho + ratón | Mirar |
| Shift | Acelerar cámara |
| Z/C | Inclinar encuadre |
| Rueda | Campo de visión |
| F8 | Ocultar/mostrar panel |
| F9 | Guardar PNG sin panel, a resolución de pantalla |
| F6 / Esc | Volver a explorar |

El panel ofrece encuadre, inclinación, velocidad, visibilidad del personaje
y apertura de la carpeta de fotos. Los PNG se guardan en
`Application.persistentDataPath/Photos`, con nombres únicos. No hay exportación
de vídeo ni superresolución; se conserva la calidad gráfica seleccionada.

## Importar skins

**En Unity:** arrastra FBX, OBJ, DAE u otro modelo que Unity importe a Assets,
selecciónalo en Project y usa **Tools > Poolrooms > Personaje > Crear apariencia
desde modelo seleccionado**. El importador genera una definición y un prefab
en `Assets/Resources/PoolroomsAvatars`, fuera de la carpeta reemplazable del
paquete. Los formatos 3DS/DXF dependen del soporte del editor. GLB/glTF requieren
un importador compatible, por ejemplo Unity glTFast; no viene instalado ni es
necesario para FBX/OBJ/DAE. Un prefab ya importado puede usarse igualmente.

La definición permite altura visual, corrección frontal, atribución y un
`RuntimeAnimatorController`. El conversor copia materiales Standard/URP Lit
simples a HDRP, incluyendo albedo, normal, corte/transparencia y valores escalares,
sin editar los originales. Materiales HDRP existentes se conservan. Revisa mapas
metálicos/roughness avanzados y shaders personalizados en el editor.

**Dentro del juego:** en Ajustes > Cámara > Apariencia abre la carpeta
`Application.persistentDataPath/Avatars`. Copia el OBJ, su MTL y texturas locales,
pulsa Actualizar lista y selecciónalo. OBJ usa lectura/triangulación fuera del
hilo principal, materiales difusos MTL y PNG/JPEG; convierte coordenadas,
preserva UV/normales y admite índices negativos y polígonos cóncavos.
Límites: 16 MB de OBJ, 150.000 esquinas, 200.000 triángulos, 16 materiales,
8 texturas de hasta 4096 px y 16 megapíxeles combinados. No lee URLs ni texturas
fuera de su carpeta. Una importación fallida conserva la apariencia anterior.

Las apariencias se ajustan al suelo y a escala del jugador **visualmente**;
sus colisiones, luces y cámaras no sustituyen las del juego. Un OBJ es estático:
no genera huesos ni animaciones. FBX con rig puede usar su controlador; se
actualizan `Speed` (float), `Grounded` y `Sliding` (bool) si existen. Root motion
queda desactivado. Las mallas con esqueleto solicitan DynamicGeometry para
actualizar su deformación en RT; las rígidas usan DynamicTransform. La silueta neutral es un sustituto básico, sin cara ni ojos.
La selección y corrección frontal se conservan entre sesiones. Usa modelos
cuya licencia permita incorporarlos; este cambio no descarga modelos externos.

## Verificación y límites

`dotnet run --project Verification/Poolrooms.Checks.csproj -- --camera-avatar`
comprueba ocho poses distintas, saneamiento de preferencias, recuperación de
distancia en siete frecuencias y OBJ, incluidos 480 polígonos/sentidos de
triangulación. También comprueba sintaxis y contratos de importaciones.
La regresión completa conserva las geometrías anteriores.

Tres pruebas PlayMode cubren pared/ancla, restauración de pausa/foto y escala
de avatar sin cambiar el controlador físico. Están pendientes de ejecutar,
igual que captura PNG, menús, importación real FBX/OBJ, animación, ray tracing,
apariencia y FPS. Las declaraciones nuevas se contrastaron con fuente oficial
Unity 6000.3 (`Verification/camera_avatar_R27_sources.json`); eso no equivale
a compilar contra Unity. Aquí no hay Editor ni GPU para comprobarlo nativamente.
