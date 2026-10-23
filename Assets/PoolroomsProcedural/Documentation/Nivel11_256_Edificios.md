# Nivel 11: edificios con espacios interiores

La generación normal incorpora **128 diseños urbanos y 128 extraños**, organizados en 32 familias y ocho configuraciones espaciales por familia. Son recetas arquitectónicas originales: no son 256 modelos descargados ni simples cambios de color. Su selección depende de la semilla y la parcela; no hace falta el modo maestro.

Los diseños combinan patios de varias alturas, galerías perimetrales, pozos de luz divididos por puentes, pórticos interiores, ventanas con huecos y marcos profundos, cubiertas con lucernarios, casetones, lamas y linternas. Cambian las dimensiones y posición de los vacíos, el recorrido de las galerías, la sección y la modulación. La estructura sigue una retícula humana con plantas de 3,2 m, escaleras de dos tramos y descansos conectados. No se presentan como 256 monumentos completamente modelados a mano.

Se conservan la cruz de calles de seis metros, los vehículos, los pabellones dreamcore públicos y los accesos del mundo. Cada manzana ordinaria tiene cuatro parcelas; el pabellón ocupa una de ellas. Las otras alojan edificios recorribles de dos a seis plantas, con bancos, armarios y objetos externos donde hay apoyo y espacio. Se conservan los alimentos y bebidas de la entrega anterior.

## Modelos y texturas con licencia

Se añade **una tercera fachada externa distinta**, `Paris_Building_010` de Amazon Lumberyard Bistro (Amazon y afiliados, **CC BY 4.0**), adaptada como `C_BuildingArcade`. Las dos fachadas anteriores permanecen disponibles. Esta fachada se ajusta a cuatro plantas; se eliminan interiores falsos, vidrios opacos y partes que cerrarían entradas o ventanas. Tiene dos LOD, orientación nativa comprobable y licencia incluida en `Resources/CityArchitectureSources/Licenses`.

La arquitectura utiliza los materiales físicos atribuidos de Bistro existentes, incluyendo albedo y normales **nativos de 4096 × 4096**, y algunos conjuntos completos a 4K. Otras superficies conservan su resolución original de 1K/2K. No se amplían imágenes pequeñas ni se duplican texturas para inflar el tamaño. El mobiliario conserva sus licencias y LOD anteriores. Se utilizan los sistemas existentes de luz regional, reflejos y HDRP; esta entrega no cambia el algoritmo de ray tracing ni elimina sus requisitos de hardware.

## Uso y comprobación

Con Unity detenido, actualiza tu única carpeta `PoolroomsProcedural` y regenera la demo. La exploración unificada habilita `detailedCityBuildings`; los perfiles personalizados pueden activarlo junto a `enableCity11`. Desactivarlo conserva la arquitectura histórica. Los IDs anteriores no cambian.

`--city-buildings` verifica geometrías distintas, apoyos, espacio sobre peldaños, galerías, tamaños mínimos/grandes y aparición en coordenadas reales. `tools/verify_city_architecture_sources.py` verifica procedencia, licencias, hashes, mapas nativos y huecos de acceso de todos los LOD mediante un cálculo independiente. Los acabados se resuelven con el adaptador de propiedad de superficies antes de subir las mallas, manteniendo la colisión original sin dibujar dos acabados coplanares.

**Pendiente de comprobación en Unity:** importación nativa, PlayMode/PhysX, shaders, imagen final, ray tracing y FPS. El entorno de desarrollo no tiene Unity; las comprobaciones matemáticas no sustituyen esa prueba.
