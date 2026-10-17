# R16 — Exploración, espacios cívicos y presentación

Actualización acumulativa sobre R15 y la base recuperada v39. Es una mejora y reconstrucción funcional; no recupera los archivos originales desaparecidos de v40–v47.

## Para jugar

Importa **solo `Assets/PoolroomsProcedural`** en el proyecto Unity 6.3 / HDRP 17.3. Conserva una copia de tu escena y perfil. No copies `Verification`, `Sources` ni `tools` dentro de Assets.

El jugador abre ahora **Espacios sin fin** al comenzar: explorar, ajustes, controles y salir. Desde pausa puedes volver al inicio y retomar exactamente la sesión abierta; no se regenera el mundo ni se cambia de posición. **No hay guardado de partida entre ejecuciones**. F7 conserva el acceso directo a renderizado. En `PoolroomsExplorer`, `Show Start Menu` permite desactivar la portada para escenas de prueba.

**R + ratón** gira el objeto sostenido sin girar la cámara; E y Q siguen tomando/interactuando y soltando. Pausa bloquea la inspección. Las consultas físicas saturadas ya no autorizan soltar objetos sin confirmar espacio libre.

## Generación integrada

Dos ambientes originales se añaden al modo Expanded, activados en el perfil con `Enable Civic Levels` y una probabilidad regional configurable (`Civic Level Share`, 0,12 por defecto, después de la selección de niveles tranquilos). No forman una secuencia obligatoria. También aparecen como destinos en el menú maestro y pueden seleccionarse individualmente en el perfil:

- **Jardín de cristal**: gran tragaluz, nervaduras metálicas, maceteros y hojas plegadas en una malla conjunta, mesas y sillas Bistro y botellas que puedes recoger.
- **Galería de tránsito**: vestíbulo amplio con tragaluz, bancos integrados, paneles de horarios, sofás y cámaras antiguas interactuables. Es un vestíbulo de circulación, sin trenes ni simulación ferroviaria.

Ambos reservan un cruce central despejado y se unen mediante las salidas reales del grafo. Tienen temas de materiales independientes, sol regional HDRP, relleno de luz moderado y acústica propia. Las dimensiones, mobiliario y aberturas varían por semilla. La vegetación nueva es geométrica original, no un modelo externo fotogramétrico.

## Imagen y coste

- Nuevo estilo **Arquitectura**: balance neutro, sin grano ni aberración cromática, resplandor contenido. Los estilos anteriores conservan sus IDs y valores.
- Respuesta opcional al aterrizar: desplazamiento breve de cámara, estable matemáticamente entre 10 y 240 FPS; se anula con reducir movimiento. No cambia el movimiento ni los archivos de la linterna.
- Antialiasing especular HDRP en materiales creados por las fábricas de biomas y objetos licenciados. Las bibliotecas existentes reciben los nuevos temas sin reemplazar materiales personalizados; reconstruir materiales explícitamente aplica la fábrica a los anteriores.
- Cada sala recién cargada revisa sus conexiones y las de cuatro vecinos como máximo. Las regiones externas y descargas conservan la revisión global necesaria. Los destellos consultan solamente las luminarias activadas por el selector existente.
- No se recortan radios de streaming, resolución de texturas, mallas, distancias de LOD ni muestras RT. No se garantiza un aumento concreto de FPS sin medición nativa.

## Procedencia y validación

Se reutilizan fuentes ya auditadas: Bistro (Amazon, CC BY 4.0), cámara antigua (CC0 con aviso de marca), sofá y botella según sus atribuciones empaquetadas. R16 añade **cero modelos externos**: el catálogo sigue en 15 fuentes/adaptaciones, 14 familias geométricas y una variante de silla.

El núcleo se compila con .NET 8. Se conservan 188 huellas geométricas; se comprueban 72 salas nuevas, 66 variaciones geométricas y 720 instancias de mobiliario, soporte, separación de rutas y huecos de techo. Se añaden pruebas PlayMode de portada, recorridos e inspección, pero **no se han ejecutado en Unity**. Compilación de API Unity/HDRP, shaders, PhysX, apariencia, ray tracing, FPS y consumo de memoria siguen pendientes. No se certifica calidad AAA ni ausencia de errores visuales.
