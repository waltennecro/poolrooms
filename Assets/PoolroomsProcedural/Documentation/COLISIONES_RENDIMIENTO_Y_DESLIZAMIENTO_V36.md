# v36 — Colisiones, carga del mundo y sonido del deslizamiento

Actualización acumulativa sobre v35. Conserva los biomas conectados, las 800 casas, piscinas, escaleras, objetos, doble salto, deslizamiento, modo maestro y ajustes. La linterna y la cámara conservan sus archivos de v35.

## Errores corregidos

- **Resultados atrasados del Nivel 94:** una malla lejana podía terminar después de regresar a su zona y sustituir una versión cercana con colisión. Ahora se comprueba posición, distancia, resolución, contexto y generación antes de empezar la subida, entre fases y antes de publicar.
- **Reemplazo de chunks:** el anterior permanece visible y con colisión mientras el nuevo se construye inactivo. Solo al completar todo se cambia el registro y se sincroniza la física. Cancelar o salir libera la construcción pendiente. Los objetos tomados durante la construcción se comprueban otra vez al publicar.
- **Bordes con regeneración continua:** una banda de retención de ocho metros evita alternar detalle al oscilar cerca del cambio de celda. El vecindario obligatorio sigue siendo de nueve tiles; puede conservar temporalmente hasta dieciséis detallados, y luego libera el detalle adicional. Se conserva el radio elegido en Gráficos.
- **Centros comerciales:** las versiones antiguas esperan las colisiones de los sectores y pisos vecinos antes de permitir avanzar. El centro conectado vuelve a comprobar el destino después de esperar su turno de subida, evitando cargar un sector obsoleto después de moverse o hacer TP.
- **Objetos frente a paredes:** si las consultas de interacción o espacio se saturan, se rechaza la acción; una lista incompleta de impactos ya no se considera un camino libre.

## Trabajo que podía producir tirones

La carga de los chunks cercanos del Nivel 94 reparte terreno, colisión, caminos, partes de la casa y objetos entre fotogramas. Los chunks lejanos, más sencillos y sin mobiliario interior, conservan la subida de un tile por fotograma para no multiplicar el tiempo de llenar una vista extensa. Se crean como máximo cuatro objetos por lote. La primera entrada puede tardar más en terminar; durante ese tiempo se espera el soporte local, sin retirar el que ya existe.

MeshFactory reutiliza buffers de posiciones, normales, UV, colores, materiales e índices de física. Los setters de Unity copian sus contenidos a mallas independientes. Los colliders del terreno contienen solo posiciones e índices de soporte, sin clonar normales, tangentes y colores del renderizador ni incluir las faldas visuales. Los límites de la malla visual se calculan una vez después de asignar todos sus materiales.

Una ventana de salas que ya está completa deja de buscar trabajo cada fotograma. Se vuelve a buscar al cambiar de celda, radio, conjunto cargado, modo de carga o regeneración. Las consultas de agua dejan de crear recetas interiores en las celdas reservadas del terreno exterior.

Los cambios de geometría agrupan la actualización de las mallas de agua HDRP una vez por fotograma. Cambiar de chunks no vuelve a escribir todas las propiedades de simulación del agua. La configuración completa sigue aplicándose al cambiar ajustes, perfil o generación. Se conserva el límite compartido de reflection probes.

No se ha medido una mejora de FPS. Cocinar un collider grande, compilar un shader o aumentar mucho la distancia puede seguir teniendo coste; estas correcciones no sustituyen un perfil de CPU/GPU en el PC real.

## Sonido del deslizamiento

El sonido anterior era la cadencia de pasos que seguía activa al deslizar. Ahora se silencian esos pasos y se usa un roce continuo original, con variantes para baldosa/suelo duro, metal, superficies suaves y suelo mojado. El terreno y los interiores domésticos usan la variante suave. Son texturas sonoras, no grabaciones de pasos ni golpes repetidos.

Volumen y tono siguen el desplazamiento real. Hay entrada, salida y cambio de superficie graduales; al saltar, nadar, volar o detenerse se desvanece el roce. Pausa, teleport y cambio de origen conservan el manejo correcto del sonido. El control existente de pasos en Audio regula también este roce. El sistema reserva ocho AudioSources: seis anteriores y dos para cruzar suavemente entre variantes; las nuevas fuentes se detienen cuando no se usan.

## Instalación y comprobación

1. Sal de Play y actualiza **Assets/PoolroomsProcedural** con la carpeta del ZIP. Incluye Runtime/Resources/PoolroomsAudio completa. Conserva tus escenas, perfiles y ajustes.
2. Usa la misma escena y el mismo botón Play. No hace falta activar nuevas herramientas ni crear otra demo. En un proyecto vacío, el menú Crear demo sigue siendo el punto de partida.
3. Desliza manteniendo Ctrl; prueba suelo seco, mojado y exterior, luego suelta, salta y pausa. Comprueba que se oye un roce continuo y que no se añaden pasos rápidos.
4. Camina varias veces por los límites de chunks y escaleras. Prueba TP al centro comercial y Nivel 94, regreso inmediato y cambios de distancia con F7. Mantén primero radios pequeños en el i3 N305.
5. En Window > Analysis > Profiler, compara CPU Timeline, GC Alloc, Physics y Rendering con v35 usando la misma semilla y ajustes. Las marcas **LiminalWorld.MeshUpload** y **LiminalWorld.ColliderCook** permiten localizar las subidas y la creación de colliders. Mide también en una build, porque el Editor añade coste.

El núcleo .NET y la sintaxis se comprueban fuera de Assets; sus proyectos de verificación no se incluyen en el ZIP. VERIFICACION_V36.txt contiene los resultados. StreamingSafetyTests añade pruebas nativas de independencia de mallas, exclusión de faldas visuales, soporte durante el reemplazo/cancelación, integración real de sonido del deslizamiento y búsqueda de trabajo en reposo. Estas pruebas están incluidas **sin ejecutar**: aquí no hay Unity Editor ni PhysX.

Compilación completa Unity/HDRP, shaders, audio escuchado dentro del juego, apariencia, colisiones nativas y FPS siguen pendientes de esa comprobación. No hace falta instalar un plugin.
