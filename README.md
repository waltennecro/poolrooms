# Poolrooms: mundo liminal procedural

Actualización más reciente: [Talleres compartidos](https://github.com/waltennecro/poolrooms/releases/tag/talleres-compartidos). Doce actividades nuevas en los 58 biomas, estados visibles, sonidos espaciales existentes, pistas, deshacer y cuaderno. **2.056 líneas netas de C# del juego**, además de pruebas. [Guía](Assets/PoolroomsProcedural/Documentation/Talleres_Compartidos.md) · [Paquete completo](https://github.com/waltennecro/poolrooms/releases/download/talleres-compartidos/Poolrooms_Talleres_Compartidos_Assets.zip) · [Evidencia](Verification/workshop_delivery.json). Compilación estricta y CI completo superados; Unity/HDRP, controles, aspecto, audio y FPS pendientes.

## Refinado R34 por lotes

Actualización publicada: [Veinte destinos y ampliación del mundo](https://github.com/waltennecro/poolrooms/releases/tag/veinte-destinos). 58 biomas de catálogo, además del centro comercial completo y Nivel 94. [Lista y guía de instalación](Assets/PoolroomsProcedural/Documentation/Expansion_Veinte_Destinos.md). Assets: 1.295.917.482 bytes reales; ZIP: 1.037.034.618 bytes; presupuesto máximo 6 GB sin relleno. Comprobaciones completas del núcleo y recursos superadas; Unity/HDRP, aspecto, PhysX y FPS pendientes. Evidencia: [Verification/journey_delivery.json](Verification/journey_delivery.json).

Se refinan los diez niveles de R33: mobiliario con perfiles reales, jardines
plantados, puentes más anchos y lavadoras con cristal y profundidad de tambor,
además de remates propios de cada ambiente. La comparación conserva las mismas
200.010 recetas y las procesa en **20 lotes de 10.000 y uno de 10**.
[Cambios y límites](Assets/PoolroomsProcedural/Documentation/REFINADO_R34.md).
El inventario es `REFINADO_R34_ESTADO.json`; la evidencia se guarda por lote.
Con Play detenido, actualiza solo `Assets/PoolroomsProcedural`; no hace falta
recrear la demo. El aspecto y los FPS requieren validación en Unity.

## Ampliación R33

Diez niveles nuevos conectados caminando, 131.072 recetas por nivel, mayor densidad
acotada de objetos, tres modelos nuevos con licencias verificadas, agua transparente
alternativa y sonido ambiental espacial con oclusión. [Cambios, cifras y límites](Assets/PoolroomsProcedural/Documentation/AMPLIACION_R33.md).
El inventario de las cinco revisiones es `AMPLIACION_R33_ESTADO.json`. Los recuentos
de recetas/instancias no son modelos distintos; Unity, aspecto y FPS siguen pendientes.

# Poolrooms

R32 conserva el **50 % más de variedad de R31** y añade **~50 % más de salas por región al explorar**, centros comerciales mayores con más tiendas y nuevas áreas pobladas en el Nivel 94. [Cifras, funcionamiento y límites](Assets/PoolroomsProcedural/Documentation/EXTENSION_BIOMAS_R32.md). Actualiza únicamente `Assets/PoolroomsProcedural` con Play detenido; no hace falta recrear la demo. Unity/HDRP, física y FPS siguen pendientes.

## Código recuperado: v39

Este repositorio conserva el paquete **Poolrooms v39 — Áreas infantiles multinivel** recuperado de una copia local el 16 de octubre de 2026. Incluye los 326 archivos originales: código C#, shaders, texturas, sonidos, recursos y pruebas de Play Mode.

El código está en [`Assets/PoolroomsProcedural`](Assets/PoolroomsProcedural). El inventario con SHA-256 y la procedencia están en [`RECUPERACION_V39.json`](RECUPERACION_V39.json).

**No se ha recuperado la v47.** La carpeta [`Preparacion_V48`](Preparacion_V48) conserva el diseño y los modelos de tres niveles pendientes de integración. Sus documentos describen la investigación anterior a esta recuperación y permanecen como evidencia histórica. La integración reconstruida actual se describe abajo; no pertenece a la v39 original.

## Reconstrucciones posteriores

La rama actual continúa sobre la v39 recuperada. Consulta [`RECONSTRUCCION.md`](RECONSTRUCCION.md) para ver funciones existentes, avances reconstruidos y pendientes. El manifiesto original sigue identificando los bytes de v39; los cambios posteriores no se presentan como archivos originales recuperados.

La etapa R1 mejora los materiales y detalles del bioma infantil conservando su geometría transitable. Las verificaciones del núcleo están en [`Verification`](Verification/README.md), fuera de `Assets`. Para crear el paquete completo del estado actual:

```sh
python tools/package_assets.py --stage R34 --output /tmp/Poolrooms_R34_Desde_v39.zip
```

La rama incluye también R2 (Ultra, dash y física de objetos) y R3 (Nivel 94 compacto y pasto más denso). El empaquetador siempre incluye el estado actual completo; `--stage` solo etiqueta su manifiesto. El workflow de GitHub comprueba el núcleo y guarda un paquete como artefacto cuando se ejecuta correctamente; su resultado no sustituye las pruebas de Unity.

R4 añade la integración inicial de City11 y un primer coche. Consulta [sus funciones y límites](Assets/PoolroomsProcedural/Documentation/CITY11_RECONSTRUCCION_R4.md): el catálogo externo inicial no completa todos los vehículos ni recursos urbanos solicitados. Se conservan las funciones R1–R3.

R5 integra biblioteca 906, patio residencial 188 y costa 48 en el generador regional, con arquitectura transitable, objetos y materiales propios. Consulta [funciones, accesos y verificaciones](Assets/PoolroomsProcedural/Documentation/TRES_NIVELES_R5.md). Las pruebas nativas y la calidad visual continúan pendientes en Unity.

R6 añade [detalle arquitectónico configurable](Assets/PoolroomsProcedural/Documentation/DECORACION_R6.md) en salas, centros comerciales y casas próximas: ocho familias originales con 16 variantes. Se ajusta en Gráficos/F7 sin regenerar el mundo.

R7 incorpora un [sillón CC0 agarrable con PBR y LOD](Assets/PoolroomsProcedural/Documentation/MOBILIARIO_CC0_R7.md), colocado en City11, biblioteca y patio. El catálogo acumulado tiene cinco modelos externos distintos; las repeticiones y los LOD no se cuentan como modelos nuevos.

El [CI de R7](https://github.com/waltennecro/poolrooms/actions/runs/39005054928) pasó todas las comprobaciones y publicó su artefacto. Además se verificó una descarga nueva del repositorio: 428 archivos de Assets coinciden por SHA-256. [Evidencia](Verification/ci_sources_R7.json).

R8 añade [aeródromos conectados y un avión civil Apache-2.0](Assets/PoolroomsProcedural/Documentation/AVIACION_R8.md). El catálogo acumulado tiene seis modelos externos y dos clases de vehículo. El resto del catálogo y la validación de vuelo en Unity siguen pendientes. [CI y descarga pública de R8 verificados](Verification/ci_aircraft_R8.json): 448 archivos de Assets y ZIP no vacío.

R9 incorpora [helicóptero CC BY 4.0 y helipuerto conectado](Assets/PoolroomsProcedural/Documentation/HELICOPTERO_R9.md), con materiales PBR y dos LOD. El catálogo tiene siete modelos externos y tres clases de vehículo. La muestra de ciudad planifica 39 vehículos (37 coches repetidos, un avión y un helicóptero); no son modelos distintos ni instancias simultáneas.

R10 añade [sofá y variante tapizada de silla con texturas originales horneadas](Assets/PoolroomsProcedural/Documentation/MOBILIARIO_R10.md) en City11, biblioteca y patio. Nueve fuentes/adaptaciones en total corresponden a ocho familias geométricas y una variante adicional; las repeticiones no se cuentan como modelos nuevos.

El [CI de R10](https://github.com/waltennecro/poolrooms/actions/runs/39045905663) pasó todas las comprobaciones y publicó el paquete. Se verificó además una descarga pública nueva con los 502 archivos de Assets; [evidencia y hashes](Verification/ci_furniture_R10.json).

R11 mejora [ruedas, cámara y calles de City11](Assets/PoolroomsProcedural/Documentation/COCHE_Y_CALLES_R11.md), conservando las conexiones existentes y sus barreras de carga. Su CI completó compilación del núcleo, auditorías y empaquetado.

R12 extiende [el mobiliario PBR a centros comerciales y casas del nivel 94](Assets/PoolroomsProcedural/Documentation/MOBILIARIO_REGIONAL_R12.md), con sustituciones limitadas, soporte comprobado y claves de interacción estables. No aumenta artificialmente el catálogo contando instancias como modelos nuevos. Las pruebas nativas siguen pendientes.

El [CI de R12](https://github.com/waltennecro/poolrooms/actions/runs/39048181017) pasó todos los pasos. Una [descarga pública acumulativa](https://codeload.github.com/waltennecro/poolrooms/zip/fd53d4e257e7141c3f52ad6572d4142f95651460) fue comprobada de nuevo: 506 archivos de Assets coinciden por SHA-256; ZIP de 195.976.786 bytes con CRC válido. [Evidencia de R11](Verification/ci_city_driving_R11.json) y [R12, paquete y descarga](Verification/ci_regional_furniture_R12.json). Solo se importa `Assets/PoolroomsProcedural`.

R13 prepara [pruebas reales en Unity y captura de rendimiento](Assets/PoolroomsProcedural/Documentation/VALIDACION_NATIVA_R13.md). El preflight confirmó que falta Unity Editor; no hay FPS ni suite nativa aprobados.

R14 añade [otro coche, cámara antigua, sofá, colocación por apoyos y refinamiento urbano/de accesos](Assets/PoolroomsProcedural/Documentation/AMPLIACION_REGIONAL_R14.md). Doce fuentes/adaptaciones corresponden a once familias geométricas y una variante de silla. Los edificios siguen siendo procedurales; no se presentan como modelos externos descargados.

El [CI final de R14](https://github.com/waltennecro/poolrooms/actions/runs/39053442886) pasó todos los pasos. [Descarga acumulativa comprobada](https://codeload.github.com/waltennecro/poolrooms/zip/29c5b7b90e919ff92a55f9139204e75a8f573e70): 607 archivos de Assets coinciden por SHA-256 y el ZIP tiene CRC válido (289.162.324 bytes). [Evidencia, límites y hashes](Verification/ci_regional_expansion_R14.json).

## Descargar y abrir

1. Usa **Code → Download ZIP** en este repositorio y extrae el archivo.
2. Guarda una copia de tu proyecto Unity actual. Este repositorio contiene un **paquete para Assets**, no un proyecto con `Packages` y `ProjectSettings`.
3. Con Unity cerrado, copia la carpeta `Assets/PoolroomsProcedural` del ZIP a la carpeta `Assets` de un proyecto HDRP. Evita superponerla sobre otra versión del paquete; usa un proyecto nuevo o retira primero la copia anterior a una carpeta de respaldo fuera de `Assets`.
4. Abre el proyecto y espera a que termine la importación. Si compila sin errores, usa **Tools → Poolrooms → Crear demo**, guarda la escena y pulsa Play.

El paquete contiene integraciones opcionales para HDRP e Input System. La recuperación original no los modificó; las reconstrucciones posteriores sí incluyen cambios documentados. La importación, compilación y ejecución de esta copia todavía requieren comprobarse dentro de Unity; verificar los hashes no sustituye esa prueba.

## Verificar o crear otro ZIP

Con Python 3, desde la raíz del repositorio:

```sh
python tools/verify_recovery.py
python tools/verify_recovery.py --zip Poolrooms_v39_Recuperado_Para_Assets.zip
```

Estos dos comandos corresponden al commit original `20311f8`: el ZIP generado contiene el paquete original `PoolroomsProcedural/` para copiarlo en `Assets`. En la rama reconstruida, la verificación original detectará las modificaciones intencionales. Para el estado actual usa `tools/package_assets.py`, que crea un manifiesto nuevo y comprueba los bytes del ZIP.

## Sobre el aviso de Cloud

El aviso «No se pudo cargar este archivo de Cloud» corresponde al acceso al archivo del entorno del chat. Esta recuperación deja el código en GitHub para poder descargarlo o clonarlo aunque ese enlace de Cloud deje de funcionar. No repara el servicio de archivos de la aplicación ni recupera versiones posteriores que no estén en este respaldo.

R15 integra terrazas de café en City11 con tres componentes de Amazon Bistro, licencia CC BY 4.0: toldo, mesa y silla. Incluye 16 mapas PBR, materiales HDRP Specular Color y dos LOD por pieza; como máximo una terraza por bloque, con muebles agarrables y comprobación del espacio de calles/puertas. Son componentes, no edificios completos de origen externo. Véase [el detalle y los límites de validación](Assets/PoolroomsProcedural/Documentation/CIUDAD_TERRAZAS_R15.md).

Respaldo R15 verificado: [ZIP público acumulativo](https://codeload.github.com/waltennecro/poolrooms/zip/e0e386ae43c7dae1326bd38f2d86e1701b59fa7d), 646 archivos de Assets coincidentes por SHA-256, CRC correcto y 304195508 bytes descargados. Importa únicamente `Assets/PoolroomsProcedural`. [CI de la revisión comprobada](https://github.com/waltennecro/poolrooms/actions/runs/39070290885) completó todas las comprobaciones y publicó el paquete; esto no ejecuta Unity. Evidencia: `Verification/ci_bistro_R15.json`.
