# Preparación de tres niveles — integración bloqueada

**Estado real:** diseño y selección de recursos. Todavía no se han implementado ni integrado estos niveles en el juego.

Al revisar `waltennecro/poolrooms`, el checkout y la rama remota `main` contenían únicamente `README.md`, en el commit `84b23266bbcf90d5d32dc38969f56fd4be74d746`. No estaban los scripts, escenas, perfiles, recursos o ZIP de v47. El único árbol Git adicional recuperable estaba vacío.

Esta carpeta se conserva como respaldo de la preparación disponible. No contiene el juego completo, una versión v48 ni niveles implementados. La nueva comprobación del historial no recuperó código y el servicio de archivos no pudo resolver o autorizar la descarga del adjunto original `v1.zip`.

Para integrar sin perder funciones hace falta el código actual de `Assets/PoolroomsProcedural`. Un ZIP sin los modelos y texturas de `Resources` y `Textures` permite comenzar; no hace falta incluir `Library`, `Temp` u `obj`.

## Niveles elegidos

### Biblioteca de luz cálida — inspirada en el Nivel 906

Galerías amplias, estanterías de madera, salas de lectura, archivo fotográfico y una sala central con dos o tres pisos y tragaluz. Libros y papelería se colocarán sobre soportes reales; las mesas dejarán libre la circulación. Tonos crema, madera, verde y azul apagado, con luz cálida moderada. Sin personajes, desafíos ni peligro.

Entrada propuesta: una nueva sala de archivo o galería urbana que gana libros y desemboca en un vestíbulo con recodo. Permite preparar la región sin mostrar el cambio. No implica transformar todavía los edificios o entradas actuales.

### Patio residencial de ventanas — inspirado en el Nivel 188

Un patio central luminoso con tres a cinco pisos transitables, galerías, escaleras amplias, bancos, lavandería y pequeños apartamentos amueblados. Paredes de pintura o ladrillo claro; plantas contenidas en macetas, no vegetación masiva. Ninguna ventana mostrará caras, ojos o siluetas.

Entrada propuesta: un nuevo portal residencial de la ciudad, seguido por dos puertas y una escalera corta. El patio aparecerá tras la segunda esquina. Las puertas de los apartamentos tendrán habitaciones reales detrás.

### Costa tranquila al atardecer — inspirada en el Nivel 48

Una región costera extensa con playa, dunas, paseo de madera, muelle y pequeños pabellones o cafeterías amuebladas. El mar se apoyará en el agua HDRP existente y una alternativa compatible, sin añadir un simulador costoso. Atardecer estable, colores naturales y horizonte sencillo. Sin ahogamiento, muerte ni objetivos.

Entrada propuesta: una nueva calle costera o paso peatonal que sale hacia el paseo. La curva o el túnel ocultarán la carga; sonido y luz cambiarán gradualmente. No se reemplazarán las entradas de los niveles anteriores.

## Integración prevista, aún sin ejecutar

- Añadir identificadores al final de las enumeraciones existentes después de revisar el código real; conservar todos los valores serializados.
- Generar regiones por semilla, con agrupaciones espaciales y pesos ajustables. Los tres niveles no formarán una ruta obligatoria.
- Crear salas de transición nuevas que usen salidas normales del generador, conservando las arquitecturas anteriores.
- Registrar puntos de conexión futuros en los biomas actuales como datos desactivados; no cortar sus paredes ni sustituir sus portales todavía.
- Preparar el destino y comprobar suelo, cápsula, orientación y retorno antes del cruce. Conservar los objetos sostenidos y su identidad.
- Reutilizar carga por proximidad, límites de luces, objetos sueltos, interacción y preferencias de v47.
- Mantener un sol regional activo; evitar una sonda de reflexión por sala o ventana. Los reflejos compartirán el presupuesto existente.
- Colocar muebles en lotes estáticos y promover a física solo los objetos interactuados. Aplicar LOD y límites de textura para el equipo sin GPU dedicada.

## Recursos originales descargados

`Modelos_origen` contiene fuentes glTF 2.0 de Khronos, con licencia CC0 para los modelos seleccionados: una silla tapizada, un farol y una botella. Se conservan procedencia, autores, metadatos y licencias en `Licencias` y `recursos_verificados.json`.

Son **archivos de origen**, no prefabs Unity ni objetos ya añadidos al juego. La silla utiliza transformaciones de textura y propiedades de tejido que necesitan adaptación al material HDRP. No se supone que una importación genérica reproduzca su aspecto correctamente. La adaptación, LOD, colisiones y vínculo con la interacción se harán al recuperar el proyecto.

Las demás piezas se buscarán primero en el catálogo licenciado que ya tenga v47. Se comprobarán sus nombres, materiales y medidas antes de seleccionarlas. La arquitectura propia cubrirá las conexiones y espacios que no tengan un modelo adecuado.

## Comprobaciones pendientes

Compilación del núcleo e integración con sus APIs reales; pruebas de recorridos, escaleras y soportes; carga y retorno; preferencias; importación Unity; shaders HDRP; apariencia y FPS. No se considera ninguna de estas comprobaciones realizada por haber preparado este documento.

## Fuentes y atribución

Modelos y metadatos: [KhronosGroup/glTF-Sample-Assets](https://github.com/KhronosGroup/glTF-Sample-Assets), consultados el 16 de octubre de 2026. Los modelos seleccionados son CC0-1.0; sus archivos de metadatos y licencia están publicados bajo CC-BY-4.0. Se preservan los autores originales y enlaces en cada registro. Los archivos descargados se mantienen sin modificaciones.

Poly Haven y ambientCG no fueron accesibles desde esta red. No se han contado como modelos descargados ni se han inventado licencias para ellos.
