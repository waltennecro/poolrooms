# Encuentro: lugares para quedarse

Esta etapa se activa automáticamente en **Exploración unificada** al iniciar el mundo. No requiere seleccionar niveles desde Tools ni desbloquear administrador. Las escenas personalizadas con esa opción desactivada conservan su generación anterior.

## Explorar caminando

Las regiones parten de 3 celdas, con la ampliación de área existente aplicada después, y tienen más conexiones en sus fronteras. No hay un orden obligatorio de niveles. Los accesos cambian entre vestuarios, galerías comerciales, porches, pasos de lectura, galerías de jardín y rellanos. Cada mitad conserva los acabados del ambiente al que pertenece; los marcos, bancos, percheros, estantes y luminarias forman parte de la arquitectura.

El primer acceso al nivel 94 queda al sur del origen, a aproximadamente 6–11 celdas en línea recta según la semilla y el radio. Sus distritos son más compactos. El cuaderno busca caminos conectados hacia otros ambientes, centros comerciales o las colinas y muestra orientaciones cardinales; no teletransporta. La búsqueda usa como máximo 4.096 celdas y no construye mallas. Si te desvías, puedes pedir otra ruta.

## Controles

| Acción | Tecla |
|---|---|
| Abrir/cerrar inventario | Tab |
| Mostrar/ocultar barra rápida | **5** |
| Seleccionar acceso rápido | 1–4 y 6–9; rueda con barra visible |
| Usar objeto seleccionado | X |
| Usar el mueble o tomar un objeto | E |
| Tomar/mover un mueble sin activar su función | T |
| Guardar lo que sostienes | Z |
| Soltar | Q |
| Salir de lectura, juego o inventario | Tab / Esc |
| Levantarse | E, Espacio o caminar |

La barra empieza oculta. Hay **5 páginas de 50 espacios: 250 tipos de objeto**. Cada pila usa cantidades enteras de precisión arbitraria, sin un límite numérico fijo impuesto por el juego. Puedes mover pilas entre páginas y asignarlas a ocho accesos rápidos. Una pila llena no existe: lo que puede llenarse son los 250 espacios para tipos distintos. Si no cabe otro tipo, el objeto sigue en tus manos.

## Cosas que hacer

El equipo inicial incluye cuaderno, cámara, regadera, tiza, paño, postales, pelota, agua, galletas y libro. Puedes guardar y sacar los objetos existentes del catálogo y los modelos de mobiliario compatibles; las variantes de color no se cuentan como modelos nuevos.

Los rincones admitidos por la geometría tienen hasta nueve piezas: mesa y asientos, planta, tazas y pequeños objetos apoyados en su superficie, con un mueble adicional cuando hay espacio. Algunos incluyen piano con ocho notas y teclas móviles, juego de ocho parejas sin cronómetro o armario con puerta y galletas que solo se recogen una vez por sesión. Las sillas permiten sentarse; las macetas, cuidarse; los muebles, limpiarse; las lámparas compatibles, encenderse o apagarse. Las contraventanas de algunas conexiones de jardín se abren. Los equipos de música usan tu biblioteca local.

Las notas, tazas y preparativos sugieren que otras personas volverán. Hay 144 combinaciones de títulos y recuerdos; no son 144 historias largas ni NPC simulados. El cuaderno ofrece siete propósitos opcionales, como leer notas, fotografiar ambientes, tocar o descansar. No hay hambre, castigos ni obligación de completarlos.

La cámara guarda una captura PNG real sin esta interfaz en `Application.persistentDataPath/Photos`. La tiza deja hasta 64 señales durante la sesión y acompaña los cambios de origen del mundo.

## Materiales, licencias y coste

Cinco juegos PBR originales de 512² —roble, latón, cerámica, papel y lino— incluyen albedo, normal y máscara HDRP. Los modelos originales nuevos son tres muebles funcionales y tres herramientas, además de accesorios arquitectónicos. Los rincones reutilizan siete modelos distintos de mobiliario ya licenciados; el inventario admite los 14 del catálogo de fuentes y el farol existente. No se han añadido modelos 3D descargados ni animaciones de captura de movimiento en esta etapa.

Once iconos de Lorc y Delapouite son CC BY 3.0, con fuentes, hashes y atribuciones incluidas. Los nuevos materiales, geometrías, gestos, textos y ocho muestras musicales son originales CC0. Las licencias anteriores de los muebles se conservan en Créditos.

La colocación se calcula con el trabajador de generación. Solo se crean rincones con apoyo y espacio; no se fuerzan muebles dentro de pasos o agua. Las luces respetan el presupuesto existente y el piano reutiliza las voces de sonido, sin añadir un AudioSource por tecla. No se reduce la distancia de renderizado ni se modifica la linterna.

## Estado y comprobaciones

Inventario, rutas, plantas, juegos y cuaderno son **de esta sesión**. Todavía no se guarda ni restaura la partida. Fotos, ajustes y música mantienen sus carpetas existentes.

La auditoría independiente comprueba transacciones, rutas del grafo, soporte de muebles y objetos de mesa, paso libre, normales, gestos, recursos y licencias. Se añaden cuatro pruebas PlayMode para transacciones, teclas, oclusión y asientos. **Unity no está disponible en el entorno de desarrollo**: esas pruebas nativas, la compilación de APIs HDRP, el acabado visual, el audio y los FPS necesitan ejecutarse en el proyecto real. Los números de aserciones del núcleo no son pruebas ejecutadas de Unity.
