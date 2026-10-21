# Rincones procedurales habitables

Esta actualización añade una capa de mobiliario contextual al generador de salas existente. No sustituye los niveles ni modifica los IDs de biomas, sus recetas anteriores, la linterna o la distancia de renderizado. Se activa con la exploración interactiva que ya utiliza la demo conectada.

## Contenido integrado

- 48 composiciones originales medidas en metros: desayunos, conversación, escucha musical, intercambio de plantas, recepción, lectura, fotografía y descanso después de nadar.
- 12 historias cálidas de viajeros, con notas distintas según semilla y contexto. Las notas se leen en mesas/carteles/cestas; no sustituyen sentarse, cuidar plantas o escuchar música.
- Selección contextual para los 58 biomas del catálogo. Los montajes generales conviven con preferencias propias de jardines, agua, comercio, viviendas, juegos y galerías.
- Reutiliza la geometría, PBR, materiales, LOD y licencias del banco existente de 36 fuentes. Son composiciones nuevas, no 48 modelos descargados. Las atribuciones de esos recursos permanecen en sus manifiestos originales.
- Objetos agarrables mediante el sistema existente, inventario y claves estables por semilla/sala/ancla/receta/pieza. Una pieza recogida no reaparece al volver a cargar la sala dentro de los límites de memoria del sistema actual.

No se añade un bioma ni se modifica la generación especial de casas del Nivel 94 o los interiores del centro comercial completo; esta capa trabaja sobre las salas del generador compartido, incluidas sus variantes comerciales.

## Colocación y correcciones

Se reserva el cruce principal de ambos ejes y el acceso a estaciones existentes. Cada propuesta comprueba muebles previos, estructura, cristal, colisión, altura libre, agua, nueve apoyos y un camino físico hacia el cruce. El índice local acelera las consultas sin usar Physics en el trabajador de geometría.

Las tazas y otros objetos pequeños utilizan la altura y huella real de su mesa, también cuando está girada. No se permite usar el rectángulo exterior como apoyo en las esquinas de una mesa redonda. Si falta la mesa al cargar, no aparecen objetos suspendidos. Al moverla o recogerla, un monitor activa la física de sus objetos apoyados.

Se corrige además la comprobación anterior de muebles importados que solo reconocía 14 de las 36 fuentes al buscar solapamientos. Las consultas de malla rechazan referencias de fuente, transformaciones y triángulos inválidos. El agua se comprueba por intersección de áreas: mirar solo esquinas podía omitir una franja estrecha bajo un asiento.

## Estabilidad y coste

Máximo de dos conjuntos y 10–14 objetos adicionales por sala, según la abundancia existente; techo absoluto de 16. Las propuestas se aceptan completas o se rechazan. Un presupuesto determinista limita triángulos, enlaces del índice, consultas y visitas; agotarlo omite mobiliario adicional y conserva la arquitectura.

La presentación crea como máximo dos objetos entre fotogramas y también cede al superar 1,5 ms entre creaciones. Una única importación puede superar ese tiempo: no es una garantía de FPS. Un conjunto no se muestra hasta terminar su carga. Los fallos opcionales se aíslan y se avisan sin parar el suelo y las conexiones. No se crean nuevas luces, sondas ni materiales propios de esta capa. La suspensión del menú independiente detiene su incorporación de objetos.

## Verificación

Ejecuta `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --scenery`. Se comprueban las 48 composiciones aisladas, 696 combinaciones de bioma/semilla, determinismo, claves, presupuestos y casos adversos: agua estrecha, suelo ausente, volumen sólido, mallas corruptas, fuentes inexistentes y mesas giradas. La muestra de arquitectura real integra 798 objetos adicionales en 95 salas. Son instancias de la muestra, no modelos distintos ni promesas de aparición en cada sala.

Los recuentos de líneas se guardan en `GENERACION_RINCONES_ESTADO.json`: líneas físicas de C# del juego, excluyendo pruebas y documentación. No son un número de errores corregidos ni una medida de calidad visual. La compilación y ejecución nativas en Unity/HDRP, físicas de los objetos, importación, interacción y FPS siguen pendientes en el editor.
