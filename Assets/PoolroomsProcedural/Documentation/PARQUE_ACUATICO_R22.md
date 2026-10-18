# R22 — Parque acuático del atardecer

Bioma interior nuevo, independiente de las Poolrooms y los baños de travertino.
Se integra en `Expanded` por regiones aleatorias, sin cambiar el inicio habitual
ni imponer una secuencia de niveles. Naves de 28–60 metros comparten límites y
entradas anchas: no se separan mediante largos pasillos. No hay enemigos, daño,
temporizadores, premios obligatorios ni desafíos de supervivencia.

## Catálogo y escala

5.600 recetas combinan siete distribuciones de piscinas/estructura, ocho familias
de toboganes, cinco formas de piscina, cinco cubiertas y cuatro alturas de torre.
Se comprueba que las mallas estructurales difieren usando idénticas coordenadas
y semilla; no se cuentan repintados, LOD ni objetos repetidos como zonas nuevas.
Con 16 segmentos de sección se verificó un máximo de 22.916 triángulos de
arquitectura/detalle/vidrio por receta; excluye agua, objetos importados, la válvula
y decoración global. La distancia de renderizado se conserva. Esto no es un
benchmark FPS ni un límite para otra resolución de curvas.

Es un catálogo procedural finito que se distribuye por un mundo continuo; no
significa 5.600 escenarios hechos a mano ni ausencia absoluta de repetición.

Cada nave incorpora tres piscinas con fondos reales y salidas por escalones,
islas para caminar/saltar, un trampolín bajo fijo, bancos, drenajes, barandillas,
muebles y objetos que se pueden agarrar. Todas las áreas son accesibles. Las
torres tienen dos tramos de escaleras de 1,25 m de ancho, peldaños de 18 cm,
huella de 27 cm y descansos. Alturas: 5,40 / 5,76 / 6,12 / 6,48 m.
Las rutas centrales conservan espacio libre para un jugador de pie.

## Atracciones utilizables

- Canal abierto, tubo en S, dos carriles paralelos, tubo en espiral, canal de
  ondulación suave, canal familiar ancho, tubo de eslalon y canal de curvas amplias.
- En la plataforma superior, **E** junto a la boca, con las manos libres, inicia
  un descenso sentado guiado por la misma curva que genera la malla. No es una
  simulación hidráulica ni un vehículo flotador. La cámara permite mirar libremente.
- La pausa y el chat suspenden el recorrido. Teletransportarse cancela la atracción;
  cambiar modo maestro restaura una plataforma apoyada, sin dejar al jugador dentro
  de un tubo con la cápsula de pie. Los cambios de origen conservan la ruta local.
- **E** junto a la válvula gira el mando y activa/desactiva una corriente suave
  en las piscinas de esa nave. No empuja al jugador fuera de su piscina.
- Las islas y el trampolín fijo se usan con caminar, salto y doble salto existentes;
  no se presentan como trampolín elástico ni piscina de olas simuladas.
- Los tubos tienen paredes interiores/exteriores y cantos cerrados. Radios de
  1,05 m, o 1,30 m en el canal familiar; no son tubos infantiles intransitables.

## Sol, materiales y agua

La iluminación del mundo en este bioma procede del sol y el cielo, sin lámparas,
emisión artificial ni luces de relleno ocultas por nave. Cubiertas acristaladas y
ventanales altos dejan entrar sol cálido a 24 grados. Exposición interior fija con
transición suavizada y control de brillo del usuario; no se ilumina uniformemente
cada rincón. La linterna y el modo maestro conservan sus controles originales.

Se integran los ajustes HDRP existentes: sombras RT cuando el hardware/configuración
lo admite, reflejos y GI según calidad, con alternativa compatible. No se añaden
sondas por nave ni se aumenta sin límite el atlas de reflejos.

Agua HDRP nativa, paleta clara y absorción de 11 m en el estilo ClearCeramic, con
reflexión, refracción y caústicas según la calidad seleccionada. Los estilos de
agua personalizados se conservan. El suelo se recorta alrededor de cada piscina;
no hay una losa invisible sobre el agua.

Doce mapas PBR originales de 1024 px: mosaico de 5 cm, cerámica de 25 cm y fibra
con acabado gelcoat en menta/coral. Color, normal y máscara HDRP comparten sus
juntas y microrrelieve. Se reutilizan la silla Bistro y la botella de agua ya
adquiridas con licencia y créditos; **cero modelos nuevos descargados** en R22.
Fuentes y licencias existentes siguen dentro del paquete. Las texturas nuevas se
reproducen con `python tools/create_aquatic_textures.py` y se auditan por SHA-256.

## Acceso y validación

Crear demo mantiene la exploración mixta. El perfil incluye `Enable Aquatic Park`
activo por defecto y probabilidad regional 0,12. `AquaticPark` permite una prueba
con el bioma aislado. En modo maestro aparece «Parque acuático del atardecer»;
Tools → Poolrooms → Parque acuático también permite visitarlo durante Play.
Los controles están descritos en pausa → Controles; no se añade un HUD al juego.

`dotnet run --project Verification/Poolrooms.Checks.csproj` ejecuta la regresión
completa. `-- --aquatic-only` limita el trabajo de desarrollo al núcleo del parque.
`-- --aquatic-plans` comprueba 33.600 planes en tamaños/puertas extremos;
`-- --aquatic-clearance` muestrea cuerpo sentado/cámara contra triángulos reales.
Las pruebas nativas añadidas comprueban subir las escaleras con CharacterController,
altura de fondo, espacio de cápsula por los accesos, pausa del descenso, postura
sentada sin efectos de cámara y devolución del controlador al cambiar noclip. **No se han ejecutado**:
faltan Editor Unity, proyecto HDRP real y GPU en la nube. Compilación Unity/HDRP,
shaders, apariencia, PhysX y FPS requieren esa validación. No se certifica una
apariencia AAA ni ausencia de fallos visuales mediante comprobaciones del núcleo.

Resultado local R22: 1.122.381.305 aserciones matemáticas, 188 huellas antiguas
protegidas y 213 archivos C# en cuatro configuraciones de sintaxis. Barrido
adicional de 33.600 planes; 144 salas de borde y 1.518 muestras de cuerpo/cámara
en 48 salas extremas. No son pruebas nativas de Unity ni certificación visual.
