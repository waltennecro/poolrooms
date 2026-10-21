# Centro comercial renovado

El centro comercial completo recibe interiores con acabados por tienda, mobiliario
externo adaptado y luz natural. Sigue apareciendo en la exploración caminable;
no es una escena accesible únicamente desde Tools. El antiguo bioma de salas de
supermercado sigue siendo otro ambiente. Esta entrega se centra en el edificio
comercial completo y conserva los demás biomas, cámara, linterna e inventario.

## Arquitectura y distribución

Se conservan las 2–6 plantas físicas, escaleras/escaleras mecánicas transitables,
puentes, atrios y tragaluces. Los grandes distritos mantienen 440–504 por 180–228 m.
Las 15 categorías de tienda reciben revestimientos, zócalos, juntas, paneles y
seis construcciones de techo: listones transversales, longitudinales, tirantes,
discos suspendidos, baffles agrupados y paneles acústicos. Las luces tienen sus
correspondientes luminarias visibles. Los acabados del suelo evitan las caras
coplanares del umbral metálico.

La densidad sigue el área real de cada local. Paquetes de hasta 16×14 m reutilizan
el generador de mobiliario; como máximo hay 96 conjuntos por tienda. Se reservan
el pasillo central, las calles transversales, la entrada, el fondo y las bahías de
exposición. No se genera todo el edificio para visitar su primera tienda.

El nuevo acabado se activa en los dos sistemas comerciales de runtime cuando
están habilitadas arquitectura mejorada, refinamiento y generación autónoma.
Es independiente de la preferencia de tamaño regional. Las llamadas históricas
al núcleo conservan `realistic=false`; sus geometrías de referencia no cambian.

## Modelos y materiales con licencia

Hay **14 modelos fuente adicionales y 28 derivados LOD**: refrigerador, puf,
zapatilla, dos lámparas, mandarina, aguacate, plato de aceitunas, silla interior,
mesa interior, caja registradora, taburete, servilletero y cartel de menú.
Proceden de Khronos glTF Sample Assets y Amazon Lumberyard Bistro. Las licencias
CC0 o CC BY 4.0, autores, revisiones fijadas, cambios y hashes están incluidos en
`Resources/MallSources/Licenses`, `Sources/Mall` y `Sources/MallBistro`.

Se añaden **20 acabados Bistro**, con mapas de color, normales cuando existen,
especular y brillo originales. Se conserva la resolución real hasta 4096, sin
reescalar imágenes pequeñas ni duplicar mapas compartidos. Los 161 mapas nuevos
incluyen modelos y acabados. Metal cepillado y portadas no traen normal original:
no se inventa uno. Sus índices son independientes por tipo/variante de tienda.

Se sustituyen conjuntos compatibles de mesas, sillas y asientos; también hay
exposiciones, plantas licenciadas y objetos sobre apoyos medidos. Los artículos
del catálogo anterior y parte de las estanterías/arquitectura paramétrica siguen
en uso: no se afirma haber sustituido cada artículo por un modelo descargado.
Repeticiones, recolores, LODs y ensamblajes no cuentan como modelos fuente nuevos.

## Luz, reflejos y coste

El sol regional existente ilumina a través de aberturas reales y vidrio que no
proyecta sombra opaca. No se crea otro sol comercial. Su estado reserva una plaza
del presupuesto de luz/sombra y desactiva los focos de tragaluz alternativos.
Con sombras desactivadas quedan las luces interiores y el apoyo difuso. El cielo
y la exposición se adaptan al centro comercial, respetando el brillo de Gráficos.

La cuadrícula distribuye hasta 64 luminarias de techo y dos de escaparate por
local; es una descripción de fuentes, no una promesa de encenderlas todas a la
vez. El presupuesto global y el filtro por planta siguen mandando. Cafeterías,
librerías y hogar usan luz más cálida; tecnología, farmacia e hipermercado, neutra.
Las zonas abandonadas y las salas de cine/arcade conservan luz reducida.

Los materiales participan en SSR/sondas y en el RT existente cuando el equipo y
HDRP lo admiten. No se añaden cámaras de espejo, capturas ni sondas por mueble.
Se comparten materiales y mapas. Los nuevos importadores activan mipmaps en
streaming; Gráficos usa presupuestos de 256/512/1024 MiB según calidad y restaura
el estado anterior al desactivarse. La distancia de renderizado no se reduce.

Los productos permanecen en mallas combinadas, sin Rigidbody/Collider/Update por
artículo. La búsqueda descarta primero sectores fuera del rayo de alcance; la
selección fina respeta el giro de cada producto. Los muebles descargados conservan
LODs, colisión sencilla y las acciones/inventario existentes. Se puede descansar,
usar lámparas, leer menús, examinar la reserva y jugar en la caja registradora.
El refrigerador y su contenido son estáticos; no se promete animación de puerta
ni un sistema de compra. Algunas extensiones ópticas glTF son aproximaciones PBR.

## Referencias, instalación y comprobación

En el repositorio, `Documentation/References/Mall/README.md` reúne **34 referencias
de objetos/acabados y seis planos reales de generación**. Ninguna es captura de
Unity. Los planos muestran forjados, huecos y tiendas de cada planta.

Actualiza `Assets/PoolroomsProcedural` fuera de Play y deja que Unity importe los
modelos/texturas. Conserva `Verification`, `tools` y `Sources` fuera de Assets.
Entra de nuevo en Play; los centros comerciales se reconstruyen mediante sus
generadores actuales. Gráficos → Ultra conserva los controles de RT existentes.

Validación independiente: `--mall-retail`, regresión completa de núcleo/sintaxis
y `python tools/verify_mall_resources.py`. Se comprueban compactos y extensos,
apoyos, circulación, propiedad de objetos, cuadrículas, IDs y selección girada.
Tres pruebas PlayMode nuevas revisan carga nativa de los 14 modelos, enlace PBR y
coincidencia de colliders/acabados en la planta superior. **No se ejecutaron**:
Unity no está instalado aquí. Compilación Unity/HDRP, shaders, aspecto, PhysX y
FPS siguen pendientes; los datos del núcleo no demuestran calidad visual AAA.

El paquete completo de autoría contiene archivos reales, fuentes y licencias sin
recompresión para consulta/reconstrucción offline. Su tamaño no equivale al de
una descarga optimizada ni al consumo de RAM/VRAM. El ZIP comprimido de Assets
contiene solamente lo que se importa en Unity. Ambos llevan inventario y hashes.
