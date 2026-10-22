# Corrección de compilación de Runtime

La consola de Unity detectó tres errores de tipos y una advertencia que la revisión de sintaxis no detectaba:

- CS0200, `PoolroomsRoomLife`: `Room.Floor` es calculado y de solo lectura. La planificación de estaciones admite ahora una altura explícita, sin modificar la sala. En tiendas se conserva la elevación de la planta antes de aplicar el desplazamiento del objeto padre; en casas se usa el suelo local cero.
- CS1061, `PoolroomsPickup`: las dimensiones usan `Poolrooms.Core.V3.Length`, no `UnityEngine.Vector3.magnitude`.
- CS0266, `PoolroomsSoundscape`: las ráfagas de pasos usan el generador entero, entre dos y cuatro pasos. `StableRandom.Range(min, max)` devuelve decimal.
- CS0108, `PoolroomsStation`: el campo de la animación pasa a llamarse `swingAnimation`, evitando ocultar `Component.animation`. Es privado y no serializado.

Se conservan los cinco Horizontes, el menú independiente, los rincones procedurales, los IDs y la distancia de renderizado. No se retiran escenas ni contenido.

## Instalación

Con Unity detenido, sustituye la carpeta anterior `PoolroomsProcedural` por la del paquete completo. Conserva tus escenas y perfiles externos. Debe existir una sola copia del código dentro de `Assets`, aunque la versión anterior esté dentro de `Poolrooms_Rincones_Procedurales_Assets` u otra carpeta contenedora. Reabre Unity y deja terminar la compilación.

El parche pequeño contiene únicamente los cinco archivos de producción modificados, con las mismas rutas relativas desde `PoolroomsProcedural`; sirve tanto para Rincones procedurales como para Cinco horizontes. Sobrescribe esos archivos dentro de la copia existente. No lo importes como una segunda copia de los scripts.

## Comprobaciones

`Verification/RuntimeTypeChecks.cs`, fuera de Assets, compila expresiones reales contra el núcleo, reproduce los cuatro fallos originales y comprueba radios, ráfagas enteras y altura de estaciones. Las comprobaciones completas conservan las huellas geométricas anteriores. Se incluyen tres casos nativos de herramientas en `EncounterInteractionTests`.

No se ha ejecutado Unity en el entorno de desarrollo remoto. Los contratos reducidos no certifican la compilación nativa completa, el aspecto, HDRP, shaders, PhysX, audio ni FPS.
