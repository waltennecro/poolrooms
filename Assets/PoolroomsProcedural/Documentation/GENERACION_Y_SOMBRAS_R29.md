# Generación detenida, accesos y sombras · R29

## Causa confirmada en la traza de Unity

`MallItemCatalog.Get(id) → PoolroomsWorld.AttachRoomExtras → Attach`
interrumpía la corrutina principal al generar los objetos del parque acuático.
El catálogo tiene 256 artículos, IDs 0–255. Dos objetos de cada receta solicitaban
IDs 256–263 mediante `(31 + i % 2) * 8 + variante`. La geometría pura se podía
calcular correctamente mientras la carga real de Unity se detenía al añadir los
objetos; por eso las comprobaciones anteriores del núcleo no lo detectaban.

R29 selecciona explícitamente **Toalla / Jabón** (familias 30/31), mantiene todos
los artículos válidos y comprueba sus IDs en `AquaticParkPlan.Valid`. No amplía ni
renumera el catálogo, no sustituye IDs incorrectos por otro objeto y no omite los
artículos para esconder el fallo.

La nueva regresión reproduce `ArgumentOutOfRangeException (id)` antes del arreglo.
Después resuelve los artículos y genera sus modelos en las 5.600 recetas a seis
combinaciones de tamaño/puerta: 33.600 casos. Incluye el control negativo de ID 256.
Esto es verificación del núcleo, no carga de objetos dentro de Unity.

## Conexiones y arranque

Los centros comerciales y el terreno reservan varias celdas. Si solo se cargaba
el radio alrededor del jugador, al estar en el centro del edificio no había
ninguna sala ordinaria candidata: sus accesos exteriores podían quedar descargados.
Ahora la carga conserva también los vecinos inmediatos de los dos accesos del
centro comercial y los cuatro del distrito de colinas. Los cierres de seguridad
siguen abriéndose únicamente cuando existe el soporte correspondiente. No se
reducen distancias de renderizado ni se carga todo el mundo infinito simultáneamente.

La opción **Exploración unificada** del componente World se respeta incluso con
perfiles antiguos de catálogo: se migra una copia temporal a mundo continuo.
El perfil original no se modifica. Las herramientas de catálogo/receta explícita
ya desactivan esa opción y conservan sus escenas de prueba independientes.
Se puede resolver automáticamente un Explorer único cuya referencia de seguimiento
se hubiera perdido; la cámara libre no pasa a ser el centro de generación.

La corrutina de salas registra las excepciones y muestra su estado; una carga
fallida retira su objeto parcial para no dejar una estructura sólida sin registrar.
El aviso de carga solo aparece mientras el jugador espera suelo, no durante la
exploración normal o el vuelo noclip que ya permite atravesar zonas no cargadas.
En Play, el menú contextual de World ofrece **Reanudar generación en la posición
actual** para diagnóstico. El arreglo del catálogo evita el fallo conocido;
reanudar no sustituye la corrección de cualquier error nuevo.

## Sombras direccionales

El sol conectado del Nivel 94 se apaga fuera de su región. Un árbitro al final del
frame conserva una sola luz direccional con sombras y evita que un sol de plantilla
se sume a la fuente regional. Devuelve las propiedades prestadas al salir/desactivar.
Las luces ambientales sin sombras, la linterna, materiales PBR y los modos RT/SSR
se mantienen. La resolución, muestras y distancia no se rebajan para eliminar el
error de atlas de cascadas.

## Validación pendiente en Unity

`Tests/PlayMode/WorldStreamingRegressionTests.cs` añade pruebas de arbitraje y
restauración del sol, migración del perfil sin alterar el original, carga real de
los cuatro artículos del parque y selección de salas de acceso desde el centro
de una reserva comercial. **No ejecutadas en la nube: falta Unity Editor.**

Verifica con tu escena: iniciar exploración, atravesar un parque acuático, continuar
a otras salas, cruzar ambos accesos del centro comercial y entrar/salir del Nivel 94.
La consola debe dejar de mostrar el fallo de catálogo y el de cascadas. Apariencia,
compilación Unity/HDRP, física y FPS siguen necesitando validación nativa.
