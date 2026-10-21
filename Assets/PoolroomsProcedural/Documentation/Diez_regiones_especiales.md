# Diez regiones especiales

El catálogo pasa de 28 a **38 biomas**. Sumando los dos destinos especiales
anteriores —nivel 94 y centro comercial completo— hay **40 ambientes explorables**.
Las diez incorporaciones son regiones del mundo continuo; no son diez escenas
separadas ni requieren teletransporte o modo maestro.

| Región | Arquitectura propia | Actividad temática disponible |
| --- | --- | --- |
| Observatorio solar | Telescopios sobre trípodes, anillos de suelo y mesas bajo tragaluces | Dibujar y consultar libros |
| Mercado cubierto | Puestos con toldos de franjas, cajones y pequeños productos | Abrir paquetes y recoger objetos |
| Taller textil | Telares con urdimbre/trama, bobinas y bancos de trabajo | Dibujar combinaciones de colores |
| Galería postal | Casilleros con ranuras y tiradores, mostradores para cartas | Escribir y leer notas locales |
| Marina interior | Cuatro vasos de agua cristalina, embarcaderos y pequeñas barcas | Preparar té y descansar |
| Galería de relojes | Relojes de pie con esfera, marcas, agujas y péndulos | Tocar campanillas |
| Taller de papel | Mesas, hojas y bastidores de secado | Dibujar sobre el tablón |
| Pabellón de ajedrez | Tableros físicos con piezas y bancos | Resolver rompecabezas deslizantes |
| Salón de radio | Consolas con altavoz, dial y mandos; paneles textiles | Tocar la caja musical |
| Galería de espejos | Paños reflectantes enmarcados y bancos | Resolver mosaicos y fotografiar |

Las actividades son estaciones con **E**, colocadas sólo donde existe una pared
real y accesible. Se reutilizan las interacciones de Sala viva, el inventario,
los efectos 3D y el cuaderno. Las barcas, los telares, los telescopios, los relojes,
los aparatos de radio y el ajedrez son arquitectura decorativa: no se promete
navegación, simulación astronómica, reloj animado, sintonización ni ajedrez completo.
Cada región incluye una nota propia y señales de una próxima visita sin NPC.
El estado de interacción sigue limitado a la sesión; no se añade guardado.

## Aparición y configuración

Actualiza `Assets/PoolroomsProcedural` conservando los `.meta` locales existentes,
abre la demo habitual y deja **Exploración unificada** activada. No hay que crear
una demo distinta. Se conservan dos candidatos de cada uno de los 28 biomas
anteriores por bloque de 8×8 regiones. Los ocho huecos que quedaban se reparten
entre los diez nuevos; dos bloques horizontales vecinos cubren los diez tipos.
La disponibilidad espacial no garantiza distancias concretas ni aparición en la
primera sala: las reservas de terreno/centros comerciales y el grafo siguen vigentes.

En mundos personalizados, `enableSpecialRegions`/`EnableSpecialRegions` controla
su inclusión en `Expanded`; sin cobertura su probabilidad regional es 18 % antes
de las selecciones ponderadas anteriores. Los diez modos dedicados permiten
inspeccionarlos, pero los accesos del juego son físicos y sus materiales cambian
con los umbrales existentes. La exploración unificada activa los diez juntos.

## Materiales, recursos y coste

Geometría original batida en las mallas de cada sala, cuatro variantes de programa
por región y los ejes dimensionales del generador existente. Las 131.072 tuplas
posibles por región no son modelos descargados ni 131.072 familias distintas.
No se alteran los IDs anteriores de bioma, materiales ni recetas. Se mantiene la
cruz central libre, el radio de carga y los presupuestos de luces/sonido/probes.
La marina recorta el suelo para sus piscinas; conserva el agua cristalina previa.

Se reutilizan modelos y mapas con procedencia/licencias ya incluidas en
`City11/Licenses`, `Interiors/Licenses`, `Urban/Licenses`, `Bistro/Licenses`,
`GardenSources/Licenses` y `RestfulSources/Licenses`, así como los acabados originales
de Encuentro. **Cero modelos, mapas o grabaciones nuevos descargados en esta etapa.**
Los espejos usan Lit metálico liso, los reflejos SSR/RT habilitados y probes como
alternativa. No hay cámaras de espejo adicionales. Un probe no reproduce con
exactitud personajes u objetos móviles; RT requiere hardware y configuración HDRP
compatibles. No se garantiza un espejo planar perfecto con RT desactivado.

## Verificación

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --special-regions`
comprueba 300 salas en tres escalas/dos anchuras de puerta, diez geometrías
principales distintas, apoyos de mobiliario, pasos centrales y rutas reales desde
el origen hacia los 38 biomas más los dos destinos anteriores en cinco semillas.
Se mantienen las 188 huellas protegidas y las comprobaciones del resto del núcleo.
Dos nuevas pruebas nativas cubren umbrales con CharacterController y material
reflectante; **Unity no está instalado aquí**. Compilación de APIs HDRP, ejecución
PlayMode, imágenes, escucha, shaders y FPS permanecen pendientes de Unity.
