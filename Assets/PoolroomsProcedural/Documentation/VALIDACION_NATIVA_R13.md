# R13: validación nativa y medición de rendimiento

El paquete no incluye Unity Editor, su licencia ni un proyecto HDRP completo. `dotnet` compila el núcleo y analiza sintaxis; no certifica APIs de Unity, shaders, contactos PhysX, gráficos ni FPS.

## Ejecutar las pruebas reales

1. Importar `Assets/PoolroomsProcedural` en un proyecto Unity **6000.3.x / HDRP 17.3.x**, con Unity Test Framework instalado. Guardar y cerrar el editor para evitar dos procesos abriendo el mismo proyecto.
2. Desde la raíz del repositorio, en Windows, ejecutar con Python:

```powershell
python tools/run_unity_validation.py --unity "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe" --project "D:\MiJuego" --results "D:\Validacion\Intento01"
```

Ajustar las rutas reales. El programa verifica versiones y bytes del paquete, sin sobrescribir el proyecto ni instalar paquetes. Ejecuta la suite PlayMode del proyecto y conserva log, XML NUnit y resumen JSON. Un directorio nuevo evita aceptar resultados antiguos. Fallos de compilación/licencia, XML ausente, cero pruebas, fallos y pruebas omitidas tienen estados separados. `--filter` permite diagnóstico parcial y se registra como tal. `--headless` puede impedir comprobar shaders; nunca equivale a una validación gráfica.

Los tests del propio lanzador utilizan un proceso **falso y explícitamente identificado** para comprobar esos estados. No son ejecuciones de Unity ni aumentan las pruebas nativas realizadas.

## Medir en una ejecución gráfica real

Con el mundo cargado y Play activo: **Tools → Poolrooms → Validación → Capturar 30 segundos de rendimiento**. Elegir un archivo nuevo y recorrer la zona. Se descartan tres segundos de calentamiento y los períodos de pausa, captura de entrada o destino sin soporte cargado. No aparece un HUD.

El JSON guarda equipo, API gráfica, soporte RT, calidad seleccionada, regiones observadas, cuadros, mediana/p95/p99, peor cuadro, memoria administrada y recolecciones GC. Detener Play antes de terminar produce `completed=false`. Los tiempos son intervalos de cuadros observados, no tiempo exclusivo de GPU ni promesa de FPS. El informe identifica si se ejecutó en Editor; para comparar rendimiento de una build se puede añadir el componente de diagnóstico y llamar a `Begin(world, ruta)`.

Inspeccionar además colisiones, conducción/vuelo, agua, sombras, transiciones y cambios de LOD. Probar Rendimiento y Alto sin RT en el i3 N305; RT requiere otra GPU compatible. Una captura de tiempos no certifica por sí sola la apariencia ni la ausencia de errores.

## Resultado real de esta reconstrucción

El preflight cloud terminó en `blocked_unity_editor_missing`, con `nativeExecuted=false`. Tampoco hay proyecto HDRP ni acceso al registro `packages.unity.com` desde esta máquina (el túnel devolvió 403). No se ejecutó la suite nativa ni se midieron FPS. Las herramientas dejan preparada la comprobación en un entorno con Unity; no convierten ese pendiente en un resultado aprobado.
