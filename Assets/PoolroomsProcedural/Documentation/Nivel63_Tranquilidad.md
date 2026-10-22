# Nivel 63 · Tranquilidad

Adaptación investigada del nivel **Tranquility**, de Sariastuff / The Backrooms Wiki. Sustituye el programa genérico CloudTerminal, conservando su ID; el catálogo sigue teniendo 63 biomas. No es una sala cuadrada con otro color: el suelo se interrumpe realmente bajo las pasarelas y el cielo queda abierto.

## Espacio y generación

Caminos suspendidos de roble se unen entre sectores sin paredes interiores. Terrazas laterales, plataformas de descanso, pasarelas superiores, mástiles, cables y escaleras de dos tramos crean dos alturas transitables. Las barandillas siguen el contorno real de los caminos y huecos, con aperturas hacia los vecinos cargados. Las juntas entre tablones no cambian la altura de pisada. Los cierres de streaming conservan colisión mientras falta el vecino, pero su pared opaca no se dibuja en el cielo.

Ocho programas se distribuyen por distritos de 4×4 sectores: meditación, té, viento, dibujo, observatorio, cartas, campanillas y jardín. Cambian pérgolas, estantes, instrumentos, esculturas y actividades; las variaciones por semilla no se anuncian como miles de modelos diferentes. Las paredes de enredaderas aparecen en los límites exteriores de la región; son vegetación estática, no una simulación de paredes que se reorganizan.

Exploración unificada activa `rebuildSky63`. Se encuentra caminando entre otros biomas y conserva las salidas caminables; no necesita modo maestro. Una escena personalizada puede seleccionar CloudTerminal y activar ese campo del perfil. Los modos históricos del núcleo conservan por defecto la versión anterior para las comparaciones de geometría.

## Descansar, jugar y volar

Hay asientos utilizables, mesa de té con recogibles, armario funcional y tres estaciones por sector: descanso, actividad del programa y rosa de los vientos en la galería superior. Se reutilizan los controles, inventario y juegos existentes; no son nuevos sistemas de astronomía o meteorología científica.

Las notas cuentan preparativos para próximas visitas, sin personajes, voces, susurros ni sustos nuevos. Las alas y cámaras existentes siguen disponibles. Si caes a más de 24 metros por debajo del sector sin estar volando, vuelves al cruce central seguro. La recuperación se suspende durante pausa y vuelo. Es una adaptación deliberada de la caída no mortal de la descripción original, no su transporte a un nivel aleatorio.

## Materiales, luz, nubes y sonido

Roble, latón y hojas PBR existentes; siete fuentes de mobiliario ya atribuidas. **Cero modelos externos nuevos y cero texturas externas nuevas.** No se cuentan las 1.536 instancias de la muestra como modelos distintos.

Dos volúmenes locales de HDRP representan el mar de nubes por debajo de las pasarelas, con densidad tridimensional original de 64³, CC0 y deriva suave. Son dos para el observador completo, no dos por sector. La textura se crea una vez al entrar; los recursos se liberan al desactivar el componente. No se usa una fotografía plana como nube.

Requieren HDRP con efectos volumétricos habilitados y calidad superior a Rendimiento. Si los desactivas, los caminos permanecen y el fondo es el cielo; no se fuerza el coste ni se cambia la distancia de renderizado. El tamaño de las capas acompaña al alcance configurado. Comprobar en Unity la integración, bordes de densidad y coste GPU.

Sol regional único, altura solar y exposición propias; se mantienen las opciones existentes de SSR/sondas/RT y sus requisitos de hardware. No se crea otro sol con sombras. El viento usa el mezclador ambiental existente; los pasos son de madera y se retira la reverberación de interior y ventilación en este nivel. Campanillas y acciones conservan sonido espacial y sus controles de volumen.

## Procedencia y estado real

`LevelResearch/Level63.json` registra la descripción, puntos de meditación, accesos/salidas, autor, licencia CC BY-SA 3.0 y cambios deliberados. Se revisó una traducción pública fijada; el dominio principal está bloqueado en este entorno. No se copian fotos, textos extensos ni modelos de otra adaptación.

`--sky63` verifica 192 sectores a tres escalas, dos anchuras y extremos de altura: escaleras, apoyos, guardas, huecos, estaciones y juntas entre sectores. Se preparan pruebas nativas de colisión y recursos bajo `Tests/PlayMode/Sky63WorldTests.cs`. `tools/verify_sky63_resources.py` audita el volumen original y los acabados existentes.

**Unity/Play/HDRP, render de nubes, shaders, física, audio y FPS no ejecutados aquí.** Las aserciones matemáticas y de código no son pruebas nativas. Esta es la cuarta reconstrucción individual; siguen pendientes 45 programas de la cola y se conservan los generadores dedicados anteriores.
