# R23 — 250 familias de toboganes y agua interior

Amplía el parque acuático R22 dentro de la exploración `Expanded`. Conserva
las 5.600 recetas de salas, sus piscinas HDRP, accesos y exploración pacífica.

## Familias y colocación

El catálogo combina **10 trazados × 5 cubiertas × 5 perfiles = 250 familias**.
Los diez trazados incluyen canal directo, S, carriles paralelos, espiral,
ondulación suave, canal familiar, eslalon, curva amplia, cascada desplazada y
doble barrido. Los colores no se cuentan como familias o estructuras nuevas.

Cubiertas: abierto, cerrado, tubo de cristal, cubierta con franja de cristal y
tramos alternados abiertos/cerrados. Se generan paredes interiores, exteriores
y cantos en los cambios de cubierta. El cristal tiene su malla de colisión;
no es un hueco invisible ni una pared opaca pintada.

Los cinco perfiles cambian radio, altura de torre y desplazamiento de la boca.
Radios de 1,05–1,32 m; torres de 5,40–7,92 m según receta/perfil, con peldaños
de 18 cm y tramos de 1,25 m de ancho. Las espirales separan sus vueltas según
el diámetro para dejar espacio al jugador. Hay doce colores de gelcoat; los
carriles gemelos usan colores distintos. No son 250 modelos descargados ni
250 trazados dibujados a mano: son familias procedurales verificables.

La nave completa puede orientarse en cuatro direcciones por semilla. Piscinas,
toboganes, escaleras, muebles, válvula y mallas se transforman juntos; las
puertas conservan su dirección real y su conexión con el mundo. Las posiciones
de activación, cancelación y contacto con el agua usan esa orientación.

## Agua que corre dentro

Cada canal tiene una lámina cóncava de agua sobre su fondo, incluidas las curvas,
espirales y partes transparentes. La película se separa unos 3,5 cm del radio
interior nominal para mantenerse visible sobre la superficie poligonal sin
parpadeos por caras superpuestas. No es una piscina horizontal puesta encima
del tobogán y no introduce volúmenes de natación dentro de los tubos.

La animación desplaza las coordenadas de textura en la dirección descendente,
a 0,90–1,50 m/s según perfil. Color/espuma suave, normales y máscara PBR se
mueven sobre la misma malla. Usa Lit transparente con reflejos y sombreado del
pipeline; las piscinas grandes siguen usando agua HDRP nativa. Es una
aproximación visual de agua corriente, **no una simulación hidráulica**.

La pausa detiene su fase; cambiar el origen flotante no mueve la textura
respecto al canal. Se comparten hasta cinco materiales animados por mundo,
con una sola actualización, sin un componente por segmento ni nuevas sondas
de reflejo. No se reduce la distancia de renderizado ni se modifica la linterna.
En el catálogo se planifican 6.160 descensos y 67.200 objetos distribuidos, no
cargados simultáneamente. El máximo observado es 26.952 triángulos de nave,
detalles, cristal y película de agua con resolución transversal 16; excluye
piscinas HDRP, válvula, objetos importados y decoración global. Es un coste
geométrico del núcleo; el rendimiento real sigue pendiente de medir en Unity.

## Materiales y comprobaciones

Cuatro imágenes originales nuevas de 1024 px: gelcoat neutro para los doce
colores y color RGBA/normal/máscara del agua. El gelcoat reutiliza sus mapas de
normal y máscara anteriores. Se reproducen con `tools/create_flume_textures.py`
y se auditan en `Verification/flume_textures.json`. No se incorporan modelos
externos nuevos; se conservan las fuentes y licencias del mobiliario existente.

Para probarlo, importa `Assets/PoolroomsProcedural` en el proyecto HDRP y usa
Crear demo con `Expanded` y `Enable Aquatic Park` activo. Durante Play puedes
visitarlo desde Tools → Poolrooms → Parque acuático → Visitar atardecer interior,
o desde el destino correspondiente del modo maestro. `AquaticPark` en el perfil
permite recorrer solamente este bioma. Las familias aparecen por generación;
no hace falta activar 250 opciones en Tools. Se mantienen los controles de
interacción y descenso del parque anterior.

Comandos del SDK 8.0.408:

- `dotnet run --project Verification/Poolrooms.Checks.csproj`: regresión completa,
  incluidas familias, salas y sintaxis en cuatro configuraciones.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --slide-families`:
  huellas de las 250 familias, cubiertas, agua, colores y espacios del jugador.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --aquatic-plans`:
  catálogo completo en los seis extremos de tamaño/puerta.
- `python tools/verify_play_textures.py`: hashes, tamaño, CRC y PNG completos.

Los tests PlayMode incluyen materiales transparentes, avance y pausa de su fase,
paredes físicas de cristal y las pruebas anteriores de escaleras/descenso.
No se han ejecutado: faltan Editor Unity 6.3, proyecto HDRP real y GPU en la nube.
Compilación Unity/HDRP, shaders, orden de transparencia, apariencia, PhysX y FPS
siguen pendientes. Los recuentos del núcleo son aserciones matemáticas, no
pruebas nativas ni certificación visual.

Resultado local R23: 1.568.944.916 aserciones matemáticas, 188 huellas anteriores
conservadas y 217 archivos C# en cuatro configuraciones de sintaxis. Pasan
33.600 planes, 500 casos de familia/tamaño con 5.400 posiciones muestreadas,
32 contratos de rotación y 16 salas de una sola salida. Además pasan 60 salas
extremas de recorrido con 1.818 posiciones de cuerpo/cámara y las 144 salas de
borde del parque. Los 70 mapas PBR base pasan sus hashes y validación PNG.
