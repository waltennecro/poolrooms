# Invernadero de Cristal

Se reconstruye el antiguo bioma Conservatory como un distrito botánico continuo. Conserva su ID; el catálogo mantiene 63 biomas. Es un diseño original de ambiente liminal, inspirado en grandes invernaderos de hierro y cristal, no una réplica canónica de un número de la wiki.

## Arquitectura y generación

Una cubierta de cristal curvada conecta distritos de 4 × 4 sectores. Sus juntas coinciden entre sectores y entre distritos con distinta semilla. En un distrito completo, cuatro cuartos forman un estanque central compartido. En los bordes irregulares aparecen anexos amueblados: no se reemplazan otros biomas para completar una cúpula.

Las pasarelas elevadas tienen barandillas, puentes, huecos sobre los jardines y escaleras físicas en cada sector. La escalera cambia de esquina para no ocupar el agua de la rotonda. Los estanques tienen fondo, bordes y tres peldaños de salida; los cierres temporales del streaming también cubren su parte sumergida. El techo y las fachadas acristaladas tienen colisión, compatible con el vuelo con alas.

Ocho programas alternan palmerales, helechos, terrazas minerales, flores, pabellones de té, viveros, herbarios y jardines de agua. Cambian los jardines y equipamientos: pérgolas, pabellones, bancales, mesas de cultivo, bandejas, estantes y terrazas. La cubierta, la distribución y los recorridos sustituyen la antigua caja de cuatro rincones.

## Explorar e interactuar

Exploración unificada activa `rebuildConservatory`; se encuentra caminando entre los biomas existentes. Una escena personalizada puede activar ese campo o seleccionar Conservatory. No requiere modo maestro. Se conservan hotel 188, Archivo 906, skins, linterna y distancia de renderizado.

Cada sector tiene tres actividades, distribuidas entre ambas alturas: semillas, herbario o té abajo; reloj solar o canales de agua y dibujo arriba. Se usan juegos y controles existentes. Hay asientos, objetos recogibles y un armario con puerta móvil y contenido. Las notas cambian según el jardín y cuentan preparativos para próximas visitas. Las plantas decorativas no se anuncian como miles de objetos independientes recogibles; el minijuego de riego no controla una simulación hidráulica de todo el estanque.

La ambientación usa el sistema espacial existente, con hojas, viento suave y sonidos de interacción, sus controles de volumen y oclusión. No se añaden voces, susurros ni sustos.

## Materiales, luz y coste

Nuevo suelo **ambientCG Ground037, CC0**, con albedo, normal OpenGL y máscara PBR a 2048². La máscara empaqueta AO y suavidad; no se promete desplazamiento geométrico. Procedencia, declaración original de licencia y hashes están en `Resources/ConservatorySources`.

Se reutilizan siete modelos atribuidos: mesa, silla, sofá, planta, maceta, taza y botella. Cero modelos descargados nuevos. Palmeras, helechos, flores y cactus son mallas procedurales originales con hojas curvadas; no son modelos externos distintos por cada copia.

La iluminación utiliza el sol regional existente, con intensidad y exposición específicas. Los reflejos/RT conservan los requisitos y opciones del renderizador. Hay como máximo cuatro luminarias auxiliares y 28 colocaciones de fuente por sector; hojas agrupadas sin GameObjects, colliders ni Update individuales. Los límites de triángulos se comprueban en tamaños de 28, 36 y 60 m. El alcance de renderizado no se reduce.

## Referencias y comprobaciones

`LevelResearch/Conservatory.json` documenta dos descripciones públicas y tres fotografías de referencia con atribución. Se consultaron cúpulas, galerías, agua, climas y espacios de trabajo botánico. Las páginas primarias están bloqueadas desde este entorno; las fotografías no se redistribuyen dentro de Assets.

`--crystal-conservatory` comprueba sectores, apoyos, escaleras, huecos, agua, muebles, juntas de cubierta, actividades y conexiones en un mundo mixto. `tools/verify_conservatory_resources.py` audita licencia, hashes y preparación PBR. Los casos PlayMode quedan preparados.

**Unity/Play/HDRP, shaders, apariencia, PhysX, audio y FPS no se han ejecutado aquí.** Las vistas opcionales de Blender son geometría técnica del núcleo, no capturas de Unity; omiten muebles importados y no certifican iluminación nativa.

Tercer programa reconstruido individualmente, después del hotel 188 y Archivo 906. Quedan 46 programas genéricos pendientes; no se presentan como completados por existir en el catálogo.
