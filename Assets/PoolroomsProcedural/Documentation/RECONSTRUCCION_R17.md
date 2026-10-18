# R17 — arquitectura licenciada y auditoría de recuperación

Paquete acumulativo sobre R16. Continúa la reconstrucción funcional de las solicitudes posteriores a v39; no equivale a recuperar los archivos originales de v40–v47.

## Edificios integrados en el nivel 11

Se adaptan dos familias de edificios de **Amazon Lumberyard Bistro**, con licencia **CC BY 4.0**, procedentes del conjunto publicado en `zeux/niagara_bistro`, revisión `2bdb6a410f8ebd475d3737e7c8e038ed2b00b02e`. Las atribuciones, licencia, GLB adaptados, URL de origen y hashes están en `Sources/Buildings`; los recursos nativos y sus avisos se incluyen en `Resources/Buildings`.

Son **fachadas exteriores adaptadas de dos modelos de edificios**, con plantas interiores generadas por el proyecto. No se conservan los interiores ficticios del conjunto original ni se presentan las dos adaptaciones como cientos de modelos distintos. La arquitectura primaria conserva colisiones, escalera en U, ventanas, techo, mobiliario y acceso al edificio.

- Aparecen en la generación habitual de City11; no requieren una demo separada.
- Hasta un exterior importado por manzana elegible. Las manzanas pequeñas conservan la arquitectura anterior.
- Los edificios seleccionados tienen una planta de 10 × 10 m y **cuatro pisos reales**. Los demás conservan la variación previa de dos a seis pisos.
- Se recortan fuera de línea los interiores y paneles que taparían entradas o ventanas. Los tres niveles de detalle reciben la misma comprobación de espacio libre.
- Un marcador oculto en cada LOD permite orientar la entrada según las coordenadas realmente importadas por Unity. No se presupone qué eje transforma el importador OBJ.
- Se reutilizan mallas y materiales; no se añaden actualizaciones por edificio, luces ni simulación física de fachadas. No se cambia la distancia de streaming ni se rebajan recursos anteriores.

El perfil expone `Enable Imported Buildings`, activado inicialmente. El núcleo lo mantiene como opción explícita para poder reproducir recetas anteriores. Las terrazas existentes evitan las fachadas importadas para no superponer sus toldos.

## Cifras comprobadas

La muestra de 324 manzanas con semilla 481516, coordenadas de −9 a 8 por eje y aeródromos activados contiene **1212 edificios generados, 4778 espacios de planta, 4565 instancias de objetos de origen, 67 exteriores importados y 38 lugares de vehículos**: 36 coches, un avión y un helicóptero. No son objetos cargados simultáneamente. Otra muestra sin aeródromos comprueba 73 exteriores, repartidos 35/38 entre las dos familias.

El catálogo acumulativo tiene **17 fuentes/adaptaciones, 16 familias geométricas y una variante de silla**. R17 añade dos familias; no cuenta los LOD ni las repeticiones como modelos nuevos.

Recursos de cada edificio, incluyendo un triángulo de marcador oculto por LOD:

| Fuente | Detalle original adaptado | LOD 1 | LOD 2 |
| --- | ---: | ---: | ---: |
| BuildingCourtyard | 14718 | 10332 | 7968 |
| BuildingCorner | 50114 | 31345 | 24835 |

Se auditan 50 mapas PNG, hashes, licencias, normales, UV y 139312 triángulos entre los tres LOD. El detector independiente mediante ejes separadores verifica que las mallas no invadan los volúmenes reservados para interiores, entrada y ventanas. Las pruebas analíticas de recorte comprueban áreas e interpolación de UV/normales. La generación desde una carpeta limpia reproduce byte a byte los 66 archivos de recursos; se eliminan mapas generados sin referencias; se verificó que regenerar los componentes Bistro anteriores tampoco modifica sus bytes.

## Qué se recuperó y qué sigue sin poder certificarse

Los **326 archivos originales de v39** siguen en Git y se han vuelto a contrastar con su inventario SHA-256. La búsqueda entre los respaldos disponibles y objetos recuperables de Git no encontró originales de v40–v47. Las funciones posteriores se han reconstruido por etapas documentadas, no recuperado como archivos idénticos.

El núcleo y las comprobaciones sintácticas se ejecutan en la nube. Se añaden pruebas PlayMode para escala, orientación, marcadores ocultos, materiales y propiedad de colisiones, pero **todavía no se ejecutaron en Unity**. El preflight R17 confirma que no hay Editor instalado; tampoco hay proyecto HDRP completo o dispositivo GPU accesible en este entorno, y el registro de paquetes Unity devuelve un rechazo de red 403.

Compilación de API Unity/HDRP, shaders, PlayMode, PhysX, aspecto final, memoria, ray tracing y FPS siguen pendientes de ejecución nativa. Las cifras del núcleo son aserciones matemáticas, no millones de pruebas de Unity. No se certifica calidad AAA ni ausencia de errores visuales.

## Instalación y comprobación nativa

Copia únicamente `Assets/PoolroomsProcedural` en tu proyecto **Unity 6.3 / HDRP 17.3**. Conserva una copia de tu escena y perfil. No metas `Sources`, `Verification` ni `tools` dentro de Assets. Los modelos y materiales se importan por los procesadores existentes; la generación utiliza el perfil actual.

En un equipo con el Editor y el proyecto disponibles, usa el ejecutor incluido, sustituyendo las rutas:

```powershell
python tools/run_unity_validation.py --unity "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Unity.exe" --project "C:\MiProyecto" --results "C:\Validacion_R17"
```

El ejecutor exige una carpeta de resultados nueva, compara los archivos importados y rechaza resultados ausentes, obsoletos, fallidos o vacíos. Después, utiliza **Tools → Poolrooms → Validación → Capturar 30 segundos de rendimiento** y revisa visualmente los biomas, interiores, reflejos y conducción. La captura no sustituye las pruebas ni certifica por sí sola el aspecto del juego.
