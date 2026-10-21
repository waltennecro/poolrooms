# Regiones extensas de todos los biomas

Los **38 biomas del catálogo** comparten ahora un campo de regiones mucho mayor.
Los dos destinos adicionales, nivel 94 y centro comercial completo, también
crecen. El total sigue siendo **40 ambientes**, no cuarenta niveles nuevos.

## Tamaño real y generación

`PoolroomsProfile.largeProceduralRegions` viene activado. `largeRegionScale`
permite elegir entre 4 y 12; el valor inicial 8 multiplica por ocho el espacio
entre centros del campo regional, aproximadamente **64 veces el área nominal**.
Con la celda habitual de 36 m, las regiones ampliadas y el tamaño lógico de 3,
los centros quedan separados unos **1.058 m**. Los límites Voronoi son irregulares:
no se promete que cada región mida exactamente 1×1 km. Las regiones se repiten
con nuevas recetas y direcciones conforme avanza la exploración continua.

No se escala al jugador, los modelos, las texturas ni los colliders. El radio de
renderizado tampoco cambia. La ventana de chunks, el origen flotante y la carga
en segundo plano existentes siguen funcionando; no se genera la región completa
para mostrar su primera sala. Los catálogos de recetas y modelos conservan sus IDs.

## Zonas internas y espacios continuos

Cada bioma recibe sectores deterministas de **galerías, salón principal, talleres,
exposición y descanso**. Su contexto aparece en las notas de las salas. Conservan
las recetas, muebles con licencia y actividades de su propio bioma; no se añaden
modelos externos nuevos ni se cuenta la repetición como modelos distintos.

Los veinte programas del reposo y especiales pueden formar salones de hasta
**4×4 chunks**: 144×144 m nominales con la celda habitual. Se abren uniones anchas
entre partes compatibles del mismo sector y bioma, con techos alineados a 10,4 m.
Las uniones abiertas no añaden paredes invisibles ni duplicaciones de suelo.
Las tapas temporales de streaming sellan todo el ancho y alto mientras falta
el chunk vecino y desaparecen cuando éste está cargado. Los
límites de bioma y las reservas pueden recortar estos salones. Columnas, vigas,
celosías, paneles suspendidos y cartelas distinguen físicamente sus cinco sectores.
Los otros biomas conservan sus arquitecturas especializadas y reciben extensión
regional, variación e itinerarios continuos; no todos adoptan el mismo salón.

Hay conexiones continuas de orientación cada seis celdas, respetando las reservas
de edificios y terreno. El cuaderno usa A* limitado a 16.384 coordenadas para los
trayectos largos; el BFS de 4.096 coordenadas sigue en mundos compactos. Los caminos
siguen puertas reales, no teletransportan. La búsqueda de biomas inspecciona centros
lógicos sin recorrer millones de salas físicas. Su coste y memoria están acotados;
una búsqueda puede fallar si el destino excede el presupuesto.

## Destinos especiales anteriores

Los centros comerciales grandes tienen **24–28 módulos longitudinales**, unos
**440–504 m por 180–228 m**, y conservan sus **2–6 plantas reales**. Generación,
reservas y streaming usan las mismas dimensiones. Los sectores permanecen por
debajo de los 32 bits de su máscara existente. El primer edificio se desplaza un
poco para no ocupar el inicio; una reserva vecina se omite para evitar solapamientos.

El nivel 94 amplía sus reservas y rellena el terreno con los mismos barrios de
casas cercanas, pasto configurable, caminos e interiores. En los tres casos
predeterminados comprobados mide más de un kilómetro por lado y tiene más de
veinte viviendas planificadas. Sus límites exactos dependen de la semilla y la
celda; su ancho no usa el multiplicador de los otros 38 biomas. El primer distrito
está más al sur para dejar libres el inicio y los accesos comerciales.

## Uso y verificación

Actualiza `Assets/PoolroomsProcedural` y vuelve a generar la demo o entra de nuevo
en Play. En el perfil, la sección **Regiones procedurales extensas** permite ajustar
el tamaño. Cambia estos ajustes fuera de Play. Desactivar la opción conserva el
campo compacto anterior; Exploración unificada respeta esta preferencia. Las
pruebas históricas del núcleo conservan el modo compacto anterior para sus
comparaciones históricas; las comprobaciones nuevas activan el modo extenso real.

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --large-regions`
localiza 114 destinos reales (38 por tres semillas), construye 114 salas, recorre
47 rutas recíprocas con 5.034 pasos y comprueba 60 umbrales anchos de los veinte
programas en tres escalas. También comprueba los sectores, las reservas y la
selección de un solo plan de habitación con radio de vista 12.

Compilación estricta del núcleo: cero errores y avisos; 188 geometrías históricas
protegidas conservadas. Cuatro pruebas PlayMode nuevas y la expectativa del centro
comercial anterior actualizada. **Unity no está disponible en este entorno**:
compilación de sus APIs, física real, shaders, imágenes, escucha y FPS pendientes.
