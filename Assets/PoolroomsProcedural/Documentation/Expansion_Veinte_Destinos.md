# Veinte destinos y ampliación de ambientes

La exploración unificada incorpora veinte biomas adicionales al campo procedural existente. Son **58 biomas de catálogo**, más el centro comercial completo y el Nivel 94. No hace falta usar el modo maestro: los pasajes, puertas y galerías del mundo conectan las regiones. Los menús de visita siguen siendo atajos.

| Destino | Arquitectura y uso |
|---|---|
| Galerías del acuario | Vitrinas de agua elevadas, vidrio separado, bancos y dibujo |
| Refugio de bambú | Columnas segmentadas, pabellones, esterillas y té |
| Caravasar del desierto | Secuencias de arcos, patios de sombra y fuentes de descanso |
| Museo del cristal | Pedestales biselados y piezas facetadas de diferentes alturas |
| Baños entre nubes | Cuatro vasos, entradas sumergidas y pérgolas ligeras |
| Biblioteca sumergida | Canales, puentes secos y estanterías laterales |
| Estación de invierno | Vías a escala, traviesas, marquesinas y bancos |
| Pueblo de cerámica | Pabellones abiertos con entradas arqueadas y cubiertas escalonadas |
| Invernadero lunar | Mesas de cultivo, canales elevados y marcos metálicos |
| Restaurante de ámbar | Mesas, asientos, pantallas de madera y pausa del té |
| Archivo de cartógrafos | Mesas de mapas, cajoneras y rollos de papel |
| Patios de escultura | Composiciones de anillos sobre plintos y espacios de observación |
| Villa de faroles | Calles interiores, pórticos y faroles suspendidos |
| Salones del molino | Ruedas de exposición con radios y paletas, canales y pasarelas |
| Paseo marítimo interior | Tarimas, barandillas y agua bajo el paseo |
| Teatro de terciopelo | Filas de bancos, cortinajes y cajas de música |
| Huertos suspendidos | Pérgolas, cestas colgantes y bancos junto a jardineras |
| Librerías de lluvia | Estanterías, divisiones acristaladas y marquesinas |
| Cisterna de mosaicos | Columnas, capiteles, arcos y vasos con acceso sumergido |
| Galería aeronáutica | Estudios aerodinámicos suspendidos, planos y mesas de taller |

Los programas usan cuatro zonas laterales con una cruz de circulación despejada. Tamaño, profundidad, cubierta, ritmo y distribución modifican la construcción. En regiones extensas, los sectores de foro abren salones continuos; los otros sectores conservan galerías, exposición, talleres y descanso. Las combinaciones paramétricas **no equivalen a salas hechas a mano ni a modelos descargados distintos**.

## Mejoras de los ambientes anteriores

Los veinte programas de galerías anteriores reciben cuatro tratamientos de techo, pilastras y detalles de carpintería. Se añaden ocho objetos de sobremesa por sala, después de los identificadores anteriores. Otros interiores cerrados reciben tratamientos en las esquinas del techo; las zonas exteriores y los accesos reservados conservan su geometría. No cambia la distancia de renderizado ni se aumenta el presupuesto global de luces.

Las nuevas salas incluyen objetos que pueden recogerse, moverse y guardarse en el inventario existente, estaciones de dibujo, té, postales, música y rompecabezas, y notas cálidas específicas de cada lugar. Los vasos reutilizan el sistema de agua cristalina existente. El molino y los estudios aeronáuticos son exposiciones estáticas.

## Recursos, iluminación y memoria

- Ocho modelos fuente adicionales de Bistro, con dieciséis LODs derivados: dos percheros, cesta, dos piezas de vajilla, servilletas, radiador y maceta. No se cuentan repeticiones ni LODs como modelos nuevos.
- Cuarenta y tres definiciones de superficies de Bistro con color, normales y brillo especular. Se conservan las resoluciones originales hasta 4K, sin ampliarlas artificialmente. Los mapas idénticos ya existentes se comparten.
- Procedencia fijada por revisión, atribución CC BY 4.0 y hashes en `Resources/JourneySources` y `Resources/JourneyFinishes`. Los originales de descarga quedan en caché externa; no inflan el contenido jugable.
- El sol regional existente ilumina los tragaluces; la temperatura cálida se aplica a los destinos de atardecer. Se mantiene el árbitro de sombras direccionales y la configuración de reflejos/RT disponible en el equipo.
- Mipmaps en streaming, materiales compartidos, mallas por sala y recursos cargados por demanda. El tamaño de `Assets` no representa memoria simultáneamente residente ni tamaño de una compilación final.

## Activación y comprobaciones

Importa `PoolroomsProcedural` dentro de `Assets` y crea la demo habitual. Exploración unificada activa la expansión. En perfiles personalizados, `Enable Journey Levels` habilita el nuevo banco y `Expanded Level Details` añade detalles y objetos a las galerías. Desactivar el nuevo banco conserva la política anterior de distribución de regiones; activarlo redistribuye los destinos del mundo para incluir los veinte nuevos. No hay persistencia nueva.

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --journey-levels` comprueba salas construidas, soporte, rutas y descubrimiento. `python tools/verify_journey_resources.py` audita recursos y licencias. Las pruebas nativas están en `Tests/PlayMode/JourneyMaterialTests.cs`.

El presupuesto es **hasta 6.000.000.000 bytes de Assets útiles**, no una cuota que se rellena con código repetido o texturas duplicadas. El registro raíz `MEGA_EXPANSION_ESTADO.json` indica el peso medido y los resultados. Compilación de APIs Unity/HDRP, shaders, aspecto, PhysX y FPS requieren ejecutar Unity; no se presentan como comprobados por las pruebas del núcleo.
