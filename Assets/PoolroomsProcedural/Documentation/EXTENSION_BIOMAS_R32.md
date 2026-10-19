# R32 · Más variedad y más salas al explorar

El usuario aclaró que quería **ambas cosas**. R31 conserva su aumento del 50 % de
recetas/programas; R32 amplía además la superficie de los biomas y sus regiones
especiales. No añade nuevos niveles ni modelos externos.

## Regiones con más salas

La separación del campo de regiones se multiplica por `sqrt(1.5)` en cada eje:
~22,47 % más de anchura y profundidad producen ~50 % más de superficie. Los tamaños
de las salas y los pasos entre celdas se mantienen. Esto permite permanecer en un
mismo bioma recorriendo más salas distintas. No significa un 50 % más de longitud
en ambos ejes, ni cambiar la cantidad de salas cargadas simultáneamente.

Se conservan las identidades lógicas, semillas y asignaciones aleatorias de los
biomas. No hay un itinerario obligatorio. El mismo campo espacial guía la costa;
las regiones solares, de luz difusa, cerradas, oscuras y de atardecer usan también
el aumento. Al cambiar el tamaño sí cambian las fronteras en coordenadas conocidas.
Las áreas reales rasterizadas varían por forma, redondeo, regiones adyacentes del
mismo tipo y reservas de edificios/terreno; el 50 % es una escala de superficie.

## Centros comerciales y Nivel 94

Los centros mantienen dos a seis plantas. Los frentes pasan de 8–12 a 12–18 módulos,
con más tiendas accesibles. Se ajusta la profundidad para que la superficie bruta
por planta crezca exactamente un 50 %; el número de tiendas aumenta al menos un
50 %, con redondeo de módulos enteros. Se conserva el espacio comercial individual,
las categorías, las escaleras y la arquitectura autónoma. El edificio y su reserva
usan una única selección de dimensiones. Los sectores continúan dentro del rango
que evita colisiones en sus claves por planta.

Las reservas del Nivel 94 crecen aproximadamente un 50 %, eligiendo conjuntamente
las dimensiones enteras más cercanas al objetivo. Se amplía también el ámbito
poblado: hay casas en los nuevos terrenos, con la misma cuadrícula y densidad por
lote, rutas, césped y mobiliario existentes. El recuento de casas puede aumentar más
del 50 % por el margen exterior fijo y el redondeo. Las regiones no se superponen
entre sí ni con los centros comerciales.

## Activación y rendimiento

Exploración unificada activa `ExpandedVariety` y `ExpandedBiomeAreas` automáticamente.
Para mundos personalizados, el perfil expone ambas opciones. No hace falta crear
otra demo. Las vistas explícitas de recetas Single/Catalog mantienen su propósito;
con `ExpandedBiomeAreas=false`, el núcleo conserva el campo espacial anterior.

Se conservan las preferencias gráficas, radios de carga/descarga y presupuestos
activos de luces y sectores. La linterna no cambia. Los edificios globales mayores
y los planes regionales tienen un coste adicional; no se promete un FPS idéntico
sin medirlo. El streaming sigue construyendo el entorno cercano al jugador.

## Comprobaciones

`--biome-extents` compara 6.502 pares de regiones completas en tres semillas:
los 18 ambientes presentan relaciones de área entre ~1,497 y ~1,503. Son celdas
del campo regional, antes de descontar reservas; no una medición de superficie
navegable de todas las regiones posibles. Se comprueban además los cinco climas
solares tanto en el atlas como en la mezcla común.

En 384 pares de planes comerciales, las tiendas pasan de 29.358 a 45.024. En doce
casos de Nivel 94, las casas planificadas pasan de 6.761 a 10.868, incluidas 1.536
fuera del antiguo ámbito poblado. Son sumas de muestras, no objetos simultáneos ni
modelos distintos. Se validan reservas, accesos físicos, claves de sectores y
156 muestras de malla. Se conserva la suite de variedad R31 y la auditoría ampliada.

La prueba nativa de migración/carga del centro comercial se amplía para comprobar
ambas opciones y el edificio mayor. **No está ejecutada en este entorno**: faltan
el Editor Unity y la validación real de API HDRP, física, aspecto, navegación y FPS.
