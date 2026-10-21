# Menú principal independiente

El inicio deja de ser una pausa sobre una partida que ya arrancó. La interfaz tiene escena, cámara, entrada y fondos propios. El generador y el jugador solo arrancan después de elegir **Comenzar exploración**.

## Instalación y entrada

Actualiza `Assets/PoolroomsProcedural` con Play detenido. Las demos anteriores que tienen `showStartMenu` activado pasan automáticamente al menú independiente: se captura su configuración antes de `Start`, se descarga la escena original y queda una plantilla inactiva. No se generan salas mientras eliges opciones.

Para una entrada de juego dedicada, abre la escena de tu recorrido y usa **Tools → Poolrooms → Menú principal → Crear escena de inicio independiente**. La herramienta guarda una plantilla inactiva y una escena `PoolroomsMainMenu.unity`, y coloca esta última al principio de las escenas de compilación. Esa escena no instancia el recorrido mientras navegas: solo conserva la referencia a la plantilla. No requiere programar ni configurar cada botón a mano.

Los recorridos que desactivaron expresamente `showStartMenu` mantienen su entrada directa. Las pruebas que crean jugadores durante una escena no se interceptan automáticamente.

## Secciones

| Sección | Función |
| --- | --- |
| Inicio | Continuar una sesión abierta, navegar y salir con confirmación. |
| Nuevo recorrido | Elegir semilla numérica o textual, modo conectado o perfil personalizado y comienzo. |
| Destinos | Buscar los 58 biomas y seleccionar también Poolrooms solares, centro comercial completo o Nivel 94. |
| Ajustes | Pantalla con confirmación de 15 segundos, calidad, distancia, exposición, agua, pasto, sonido y controles. |
| Tu experiencia | Atmósfera tranquila/inquietante, campo de visión, contraste, movimiento, destellos y escala de interfaz. |
| Créditos | Procedencia de fondos y licencias de los recursos del proyecto. |

Cada sección tiene un fondo original diferente. Se funden en 0,42 segundos con tiempo independiente de la pausa; Reducir movimiento evita la transición. La composición se adapta a pantallas estrechas y los contenidos largos tienen desplazamiento. Los textos y controles son interfaz real, no están pintados dentro de las imágenes.

## Comenzar, cancelar y continuar

Al comenzar se crea una escena de exploración nueva, se clona la plantilla y se utiliza una copia del perfil. La semilla y las opciones no modifican los assets originales. Una pantalla de carga permanece visible hasta que el suelo del punto de partida está preparado. Elegir destino utiliza la navegación normal del mundo; no activa el modo maestro.

Cancelar invalida la carga pendiente y elimina sus objetos. Los tickets evitan que una carga antigua tome el control de un recorrido posterior. Un destino no disponible o una generación que falla vuelve al menú con un mensaje; no deja al jugador en el vacío.

**Volver al inicio**, desde la pausa, conserva la sesión abierta. Se suspenden las nuevas solicitudes de streaming, la entrada del jugador y la presentación del mundo; un trabajo ya en cálculo puede terminar. Continuar restaura sus cámaras, interfaz, tiempo y audio, manteniendo posición, inventario y objetos. Se conservan también los ajustes de iluminación de escena al crear el recorrido, incluida la referencia al sol de su propia copia.

Comenzar otro recorrido pide confirmación antes de sustituir esa sesión. **No hay guardado persistente del mundo ni opción ficticia de cargar partida.** Continuar está desactivado al abrir el juego sin sesión; salir avisa de que el mundo y el inventario se perderán. Los ajustes y las skins conservan su persistencia existente.

## Arte y coste

Se incluyen seis ilustraciones originales de 1672 × 941 píxeles: Poolrooms, ciudad, parque acuático, colinas del Nivel 94, centro comercial y biblioteca/invernadero. Se crearon para el menú mediante `image_gen`; no son capturas de Unity ni una promesa de que la partida tenga exactamente ese aspecto. Procedencia, tamaños y hashes: `Runtime/Resources/FrontendArt/Provenance.json`.

Se cargan por sección y se libera el fondo anterior al acabar el fundido: como máximo dos fondos referenciados por la interfaz. El importador conserva su proporción y resolución, sin mipmaps ni copia legible en CPU. Se usa importación sin compresión de bloques para evitar deformar las dimensiones no potencias de dos y artefactos en los fondos. Dos imágenes RGBA equivalen aproximadamente a 12,6 MB antes de otros costes del motor. No se reduce el renderizado del mundo.

## Verificación

`--frontend` comprueba 1.000 secuencias de navegación, carga, cancelación, reinicio y continuación, semillas reproducibles y transiciones acotadas. `tools/verify_frontend_art.py` valida los seis PNG, sus hashes, proporciones y enlaces al importador.

`FrontendSceneTests` comprueba en Unity que no hay generación antes de comenzar, que la interfaz vive en otra escena, que cada sección tiene un fondo y que el sol se remapea a la nueva copia del recorrido. **Estas pruebas nativas siguen pendientes de ejecutar**: el entorno de trabajo no contiene Unity. La compilación del núcleo y la sintaxis no verifican APIs de Unity/HDRP, control por teclado/mando, importación, apariencia, audio o FPS.
