# R24 — Casas de lluvia

Cuarto nivel de la expansión relajante. Un barrio de casas de madera entre patios
mojados, porches y galerías acristaladas. Se integra en `Expanded`, con regiones
aleatorias y conexiones transitables al resto del mundo; no requiere un portal
separado. La región inicial conserva Poolrooms y no hay un orden de niveles.

## Arquitectura e interiores

2.500 recetas: cinco combinaciones de programas, cinco distribuciones de
huellas, cinco perfiles de tejado, cinco ritmos de galería y cuatro alturas y
proporciones de detalle. Las huellas se comparan a igual semilla y coordenadas;
no se cuentan colores ni instancias de objetos como zonas nuevas.

Cada sector tiene cuatro casas accesibles. Los programas alternan entre lectura,
cocina de té, dormitorio, taller y jardín interior. Incluyen camas con almohadas,
mesas de trabajo, estanterías con libros, aparadores con cajones y tiradores,
cocinas, teteras, macetas y percheros según el programa. Hay ventanas con huecos
reales, marcos, travesaños y alféizares, vigas interiores, fascias, esquinas de
carpintería, canalones y bajantes. Los perfiles de tejado cambian su geometría:
a dos aguas, pendiente única, mariposa, cumbrera elevada y faldón asimétrico.

Las puertas de 1,60 m dejan libre el eje interior. Los pisos y porches quedan a
1 cm del pavimento; no hay una plataforma alta que obligue a saltar. Cristales,
cubiertas y suelos tienen colisión. Las galerías principales conservan un cruce
amplio y conexiones recíprocas entre sectores. Las casas se dimensionan para
sectores de 28–60 m, con entradas exteriores de 3–6 m.

Cada sector coloca 32 objetos agarrables: asientos, mesas, lámparas, plantas,
botellas y doce artículos pequeños apoyados en aparadores. Reutiliza siete IDs
de recursos previamente licenciados: `B_CafeChair`, `I_ChairDamaskPurplegold`,
`U_SheenWoodLeatherSofa`, `B_CafeTable`, `G_PottedPlant`, `WaterBottle` y
`T_ArtNouveauLamp`. Las atribuciones existentes siguen vigentes y visibles en
los créditos. **No se descargaron modelos nuevos en R24**; los elementos
arquitectónicos nuevos son geometría procedural original.

El catálogo distribuye 10.000 casas y 80.000 objetos; no se cargan a la vez.
El streaming, los LOD de los modelos y la distancia de renderizado existentes
se conservan. El coste arquitectónico máximo observado con resolución 16 es
11.064 triángulos por sector, excluidos objetos importados y decoración global.

## Acabado, clima y sonido

Doce mapas originales de 1024 px: color, normal y máscara HDRP para cedro, roble,
pizarra y pavimento húmedo. Juntas, veta, microrrelieve y variación de rugosidad
comparten coordenadas; la piedra húmeda conserva reflejos irregulares. Los
materiales propios del nivel no reutilizan los azulejos de las Poolrooms.

Cielo degradado nublado, bruma suave y luz diurna fría de 2.600 lux, combinados
con lámparas cálidas de tonalidad común. Hay ocho luces candidatas por sector,
sometidas al presupuesto compartido. La exposición se adapta gradualmente al
entrar en las casas. Los reflejos, iluminación indirecta y sombras usan el HDRP
existente y su alternativa sin RT; no se afirma que una GPU incompatible ejecute
ray tracing. No se añaden sondas ilimitadas ni se cambia la linterna.

Un emisor por mundo, máximo 640 partículas vivas. En Rendimiento intenta emitir
180 gotas/s; en otras calidades, 420. El muestreo descarta posiciones bajo casas,
porches y galerías usando sus huellas, sin lanzar un raycast por gota ni crear
objetos por partícula. Solo emite sobre sectores cargados de este bioma. La
lluvia vertical es una aproximación visual; no simula acumulación ni escorrentía.

Una sola voz reproduce un bucle mono original de 16 s. Bajo techo baja su nivel
y se filtran agudos; respeta pausa, volumen maestro y ambiental. La acústica
reduce la reverberación de piscina. Los recursos se reproducen con
`python tools/create_rain_resources.py`, con hashes y auditor de medios propios.

## Acceso y validación

En el perfil: `Enable Rain Houses`, `Rain House Share` (0,10 por defecto en
perfiles nuevos). Crear demo conserva la exploración mixta. Para una prueba
local usa el modo `RainHouses`; durante Play puedes usar el destino «Casas de
lluvia» del modo maestro o Tools → Poolrooms → Casas de lluvia → Visitar barrio
tranquilo. No se añade un HUD ni se cambian los controles del jugador.

- `dotnet run --project Verification/Poolrooms.Checks.csproj`: regresión acumulativa.
- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --rain-only`:
  catálogo, geometría, suelos, puertas, mobiliario, refugios, regiones y TP.
- `python tools/verify_play_textures.py`: mapas PBR e integridad PNG.
- `python tools/verify_rain_resources.py`: hashes, alfa PNG y audio PCM.

El núcleo comprueba 15.000 planes a seis escalas, 40.000 posiciones de acceso y
96 sectores integrados. También comprueba que la máscara de lluvia no omita
tejados físicos y que el mobiliario deje libres los accesos a escalas pequeñas.
Los dos tests PlayMode nuevos cubren recorrido con CharacterController, tejados,
materiales y recursos; están escritos pero **no ejecutados**.

Este entorno no tiene Editor Unity 6.3, proyecto HDRP completo ni GPU. Compilación
de las APIs Unity/HDRP, shaders, partículas, mezcla de audio, aspecto, PhysX,
memoria y FPS están pendientes de comprobación nativa. Las cifras del núcleo
son aserciones matemáticas, no certificación visual ni pruebas de Unity.

Resultado local R24: 1.806.980.005 aserciones matemáticas, 188 huellas protegidas
y 222 archivos C# en cuatro configuraciones. Los 82 mapas PBR base y los dos
medios de lluvia pasan sus auditores; los 14 recursos nuevos se regeneran
idénticos por SHA-256. Tres archivos de linterna siguen idénticos a la v39.
