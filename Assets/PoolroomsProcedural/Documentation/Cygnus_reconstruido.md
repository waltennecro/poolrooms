# Archivo 906: reconstrucción arquitectónica

Se sustituye la biblioteca de una sola sala por un edificio continuo. Cada tres columnas de generación aparece una nave alta con dos vacíos de varias plantas; sus galerías laterales y puentes conectan con las alas. Las naves continúan entre sectores sin paredes cuadradas interiores. Todos los sectores tienen escaleras hasta la tercera planta, incluidos los bordes pequeños de un bioma.

Las alas alternan depósitos de libros, lectura, escritura, cocinas de té y alcobas de descanso. Cambian las particiones y equipamientos: hileras de estanterías, arcos, casilleros, encimeras, fregadero y alcobas con entradas físicas. Los libros tienen volúmenes, lomos y bandas doradas agrupados en mallas; no un componente por libro.

El techo es una bóveda construida, con huecos reales para vidrieras curvas y nervios. Los suelos conservan madera PBR; las alfombras y cojines usan tela con tinte azul y bordes de latón. Se reutilizan seis modelos atribuidos existentes: sillas, lámparas, botellas, tazas, jarrones y plantas. **Cero modelos externos nuevos**; no se cuentan copias como modelos distintos.

## Encontrarlo y jugar

Exploración unificada activa `rebuildArchive906` en el perfil de ejecución y mantiene las entradas caminables desde otros biomas. No requiere modo maestro. Una escena personalizada puede activar ese campo o generar Archive906 directamente. Los IDs anteriores, la linterna, el hotel reconstruido, skins y distancia de renderizado se conservan.

Hay intercambio de libros abajo, encuadernación o té en la segunda planta y un mapa estelar arriba. Son actividades existentes conectadas al sistema de interacción y al estado de sesión. Las sillas y objetos de origen conservan sus adaptadores habituales. No se anuncia persistencia nueva ni que cada libro decorativo sea un objeto recogible.

Las luces tienen luminarias y suspensión visibles, temperatura cálida coherente y elegibilidad por planta. Su distribución y alcance escalan con el tamaño del sector, con un máximo de veinte posiciones por sector; el presupuesto global de luces sigue mandando. Se conservan los reflejos/RT y sus requisitos del renderizador existente. No se añade un segundo sol ni una sonda por estantería.

## Referencias y licencias

`LevelResearch/Level906.json` identifica la descripción completa consultada, su copia fijada y las adaptaciones. Archivo Cygnus, de **SnomWriting y Blanche**, wiki de Backrooms, CC BY-SA 3.0; traducción consultada de **DigIntoGrave**. La adaptación narrativa/ambiental específica de este nivel se ofrece con esa atribución y licencia: https://creativecommons.org/licenses/by-sa/3.0/ . Código original y recursos anteriores mantienen sus licencias respectivas. No se redistribuyen fotografías, texto de libros ni imágenes del artículo.

Se adapta la biblioteca gótica/victoriana/barroca, madera, alfombras azules y doradas, lámparas de velas, vidrieras y áreas de té/escritura/descanso. El juego conserva la exploración tranquila: no incorpora entidades, teletransporte obligatorio por firmas ni un techo que se aleja infinitamente.

## Comprobaciones y límites

`--archive-library` construye sectores de 28/36/60 m y entradas de 3/6 m, incluidas alturas máximas; comprueba escaleras, huecos, apoyos, muebles, plantas, extremos de coordenadas y conexiones con otros biomas. Los perfiles históricos mantienen el generador anterior por defecto en Core para comparar sus huellas sin alterarlas.

Los casos PlayMode de escaleras, barandillas y materiales quedan preparados. **No se han ejecutado Unity, HDRP, PhysX, shaders ni medidas de FPS en este entorno.** Las previsualizaciones opcionales de Blender representan geometría del núcleo y no son capturas del juego; omiten muebles importados y no certifican iluminación nativa.

Esta entrega reconstruye Archive906. El hotel 188 ya estaba reconstruido; otros **47 programas genéricos** siguen en la cola individual, no se presentan como terminados por tener un nombre en el catálogo.
