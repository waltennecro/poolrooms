# Horizonte · Alas

Vuelo normal para el jugador, independiente del modo maestro. Al crear una demo
o usar un `PoolroomsExplorer` existente se añade automáticamente `PoolroomsWings`.
No requiere otra escena, prefabs nuevos ni ejecutar un menú de Tools.

## Controles

| Acción | Control |
| --- | --- |
| Activar o detener vuelo | G |
| Cambiar velocidad | H |
| Moverse según la mirada | WASD |
| Subir / bajar | Espacio / Ctrl izquierdo |
| Ver las alas | F5, cámaras externas |
| Fotografiar | F6; «Mostrar personaje» también controla las alas |

Hay cuatro velocidades: lento (3 m/s), medio (8), rápido (18), muy rápido (36).
Soltar la dirección frena suavemente hasta quedar suspendido. G devuelve la
gravedad; desequipar las alas también. El vuelo usa el CharacterController, con
pasos de hasta 18 cm y comprobación de generación entre pasos. Las alas son
cosméticas: las colisiones usan el cuerpo, sin añadir un collider por pluma.

La pausa, el chat y el modo foto detienen el movimiento y conservan el vuelo.
Un viaje, vehículo, recorrido acuático, desactivación del componente o cambio
del vuelo maestro cancela el vuelo normal. No modifica permisos ni velocidad
del administrador. Se mantienen salto doble, dash, deslizamiento y linterna.

## Skins → Alas

Equipa o quita las alas, selecciona familia/diseño y velocidad, ajusta tamaño,
altura del anclaje, separación de la espalda, plegado y sombras. Los cambios se
guardan en `Poolrooms.Player.Wings`; cambiar de skin no destruye las alas.

144 variantes de geometría, distribuidas en 12 familias de 12: ángel, serafín,
cuervo, demonio, dragón, mariposa, libélula, robot, neón, luz, cristal y mecánico.
Cambian envergadura, disposición, plumas, lóbulos, capas o paneles. La comprobación
exige 144 fingerprints geométricos distintos, sin contar colores. Se construye
solo el diseño equipado: dos mallas de alas y un arnés. El máximo comprobado es
7.744 triángulos por ala, más 12 del arnés, sin luces ni sondas adicionales.

Las mallas usan materiales Lit, sombras, normales, UV y reflejos disponibles en
la calidad actual; RT dinámico solo con HDRP y hardware compatible. No se reduce
la distancia de renderizado. Las alas no convierten una GPU integrada en una GPU RT.

## OBJ sin esqueleto

En Skins activa **OBJ sencillo · sin huesos** antes de importar, o actívalo y
pulsa **Recalcular adaptación** para el OBJ ya guardado. Conserva escala, eje,
orientación, materiales, sombras y movimiento con el jugador, sin deformación
de brazos/piernas ni emotes corporales. Al desactivarlo vuelve la adaptación
humanoide existente. El modo sencillo solo afecta OBJ; no elimina los otros
formatos ni cambia el esqueleto de los modelos preparados en Unity.

## Licencias y verificación

Los 12 iconos descargados son de Lorc / Game-icons.net, CC BY 3.0; se utilizan
en el selector. Sus originales, revisión fija y hashes están en `Sources/Wings`;
los textos legales viajan en `Resources/WingSources/Licenses` y los créditos se
muestran dentro del juego. No son 144 modelos 3D descargados: las alas 3D son
construcciones geométricas nuevas para este proyecto.

`dotnet run --project Verification/Poolrooms.Checks.csproj -- --wings` comprueba
geometría, simetría, orientación, presupuesto, ajustes y vuelo a seis tasas de FPS.
`python tools/verify_wing_sources.py` verifica fuentes, licencias, PNG y créditos.
`WingFlightTests` y `StaticObjSkinTests` comprueban integración nativa en Unity.
En el entorno cloud no hay Unity Editor: quedan pendientes PlayMode, materiales,
sombras/reflejos, apariencia de las skins y medición real de FPS.
