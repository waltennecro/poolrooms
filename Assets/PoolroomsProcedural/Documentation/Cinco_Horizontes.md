# Cinco horizontes conectados

Cinco niveles nuevos se añaden después de los 58 biomas anteriores. El catálogo pasa a **63 biomas**, además del centro comercial completo y el Nivel 94. Todos forman parte de la exploración normal a pie; el modo maestro y la selección inicial son atajos opcionales.

| Nivel | Arquitectura propia | Actividad característica |
| --- | --- | --- |
| Patios de magnolias | Árboles con ramas y flores de doble cara, jardineras circulares, pérgolas y bancos de listones. | Semillero, té y postales. |
| Terminal de funiculares | Cabinas acristaladas con puertas abiertas, asientos laterales, cables, traviesas y paso a escala humana. | Rosa de los vientos y pequeñas actividades de viaje. |
| Palacio de vitrales | Galerías altas, vidrios ámbar, verde azulado y rosa, retículas de plomo con profundidad y bancos. | Mosaicos y dibujo de postales. |
| Patios de campanas | Pórticos de piedra y madera, campanas metálicas con soporte y zonas de descanso. | Secuencia musical de campanas. |
| Patios del faro | Miradores accesibles por escaleras, plataformas, barandillas y linternas acristaladas cálidas. | Circuito de linternas y campana del descanso. |

Las cabinas son exposiciones estacionarias y accesibles: no se añade transporte conducible ni una simulación de cables. Los miradores tienen peldaños y apoyos físicos, no escaleras pintadas. Las actividades utilizan los sistemas de interacción, animación, sonido espacial e inventario existentes; cada sala recibe cuatro estaciones.

## Generación y acceso

Se utiliza el sistema de regiones extensas y streaming local: cada nivel puede continuar por muchas salas, con secciones de exposición, talleres, descanso y salones. La forma de las bahías, disposición, alcance, altura, tragaluz y ritmo constructivo varían mediante las recetas estructurales existentes. Su rango es de 131.072 combinaciones de parámetros por programa; esto no significa otros tantos modelos, edificios distintos ni diseños revisados individualmente.

`WalkableExploration.Configure` habilita los cinco niveles. En cada bloque regional completamente habilitado se ofrecen los 63 destinos antes de repetir. Los cinco nuevos consumen posiciones que antes repetían niveles: las asignaciones antiguas que permanecen, los IDs serializados y sus geometrías protegidas se conservan. Los perfiles personalizados pueden desactivar `enableHorizonLevels`; los modos individuales nuevos siguen disponibles.

La entrada a magnolias utiliza la transición de galería ajardinada; los demás aprovechan los umbrales y descansillos arquitectónicos del sistema actual. No se añaden portales negros ni se exige teletransporte. El menú de destinos incorpora sus nombres automáticamente.

## Materiales, luz y sonido

Cada nivel combina superficies PBR distintas del banco atribuido de acabados existente, con su material de suelo, pared, madera y metal. El mobiliario reutiliza las fuentes licenciadas ya integradas y sus LOD; esta entrega no anuncia nuevos modelos descargados ni duplica texturas para aumentar el tamaño.

Tragaluces, iluminación de apertura y la gestión existente de un único sol mantienen la iluminación regional. Magnolias, campanas y faros utilizan el tono cálido de tarde; las linternas de los miradores añaden luz local limitada por los presupuestos actuales. Se conservan las opciones de reflejos y ray tracing disponibles en el proyecto; no se fuerza DXR ni se garantiza su disponibilidad en una GPU determinada.

Los vitrales se agrupan en tres mallas por sala. Sus materiales tintados se reutilizan durante la vida del chunk y se destruyen al descargarlo. Las ventanas de exposición cuentan con barrera física. La transmisión coloreada depende del shader y de la configuración HDRP: no se simulan sombras coloreadas con manchas pintadas ni se afirma que estén comprobadas visualmente. Cada nivel tiene también un perfil acústico propio y actividades con audio espacial existente.

## Instalar y comprobar

Con Play detenido, actualiza `Assets/PoolroomsProcedural`. No necesitas reconstruir la demo ni cambiar la escena de menú. La exploración unificada incorpora los cinco destinos al generar; en un perfil personalizado, habilita Cinco horizontes.

Ejecuta `dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --horizons`. Comprueba 240 salas a tres tamaños y dos anchuras de puerta, soportes, pasos centrales, accesos de actividades, cabinas, peldaños y conservación de IDs. La búsqueda por cinco semillas traza rutas físicas de núcleo hacia los 63 ambientes, el centro comercial, el Nivel 94 y ambos climas de Poolrooms. No promete una distancia fija entre destinos ni reemplaza PhysX.

Se incluye `HorizonMaterialTests` para comprobar reutilización y liberación real del material tintado en Unity. Las pruebas nativas Unity/HDRP, shaders, apariencia, interacción, física, audio y FPS permanecen pendientes: no hay editor Unity disponible en este entorno.
