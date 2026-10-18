# R26 — Compatibilidad HDRP y agua

## Instalación

Sustituye `Assets/PoolroomsProcedural` con Unity fuera de Play, conservando tus
escenas y perfiles. No copies `Verification` ni `tools` dentro de Assets.
Se conserva la exploración conectada R25 y la distancia de renderizado.

Los tres CS0103 de `LightUnit` se corrigen importando `UnityEngine.Rendering`
en los adaptadores HDRP de linterna y modo maestro. El cambio en la linterna es
exclusivamente esa importación: intensidades, sombras y efectos conservados.
También se incluye la corrección anterior del `Tooltip` duplicado.

## Agua y reflejos

- `PoolroomsHDRP.refinedWaterOptics`, activo por defecto, reduce un 40 % el
  límite de distorsión de los estilos predeterminados. Custom conserva su valor.
  No cambia profundidad, colisiones ni las interacciones de agua.
- El suavizado lejano conserva mejor los reflejos en piscinas amplias. La
  transición pasa de 12/35 m a 25/70, 40/120 o 60/160 m según detalle.
  No se fuerza acabado espejo si el usuario reduce `waterSmoothness`.
- Las capturas locales usan la resolución **HDRP efectiva**, mediante
  `settingsRaw.cubeResolution`: 128 Rendimiento, 256 Alto, 512 Cinemático.
  Cambiar solamente `ReflectionProbe.resolution` no garantiza ese resultado.
- Las sondas conservan su habitación cuando cambia el orden de proximidad,
  evitando recapturas y cambios innecesarios. Se explicita proyección por caja
  con el volumen arquitectónico. La cola sigue realizando una solicitud por
  intervalo y conserva el límite compartido de sondas (dos por defecto).
- SSR conserva 32 pasos en Rendimiento; Alto/Cinemático pasan a 128 y Ultra
  a 192. Las pérdidas de información usan sondas/cielo donde HDRP lo admite.
  Los presupuestos existentes de RT y su alternativa sin RT se conservan.

El agua nativa HDRP usa su sistema de reflejos; esto no añade intersecciones
DXR sobre el agua ni una simulación hidráulica nueva. El atlas compartido con
otras escenas/sondas sigue dependiendo de la configuración HDRP del proyecto.

## Comprobación

`dotnet run --project Verification/Poolrooms.Checks.csproj -- --water-optics`
comprueba presupuestos, transmisión, sintaxis en cuatro configuraciones,
atributos duplicados e importaciones de LightUnit mediante un contrato reducido
de nombres. Este último reproduce CS0103 al quitar la importación; no compila
los adaptadores contra las bibliotecas reales de Unity.

Se contrastaron las declaraciones modificadas con fuentes oficiales Unity
6000.3/HDRP: `Verification/hdrp_optics_R26_sources.json`.
`ReflectionPoolTests` incorpora regresiones nativas de resolución, reasignación
y liberación, pendientes de ejecutar. Sin Editor/GPU aquí, quedan pendientes
compilación Unity, shaders, imágenes, movimiento de reflejos y medición de FPS.
En Unity verifica una piscina con bordes visibles, un tragaluz a contraluz y
agua en sombra; compara Alto/Rendimiento sin cambiar exposición o distancia.
