# Reconstrucción desde v39

Este trabajo reconstruye funciones solicitadas después de v39. No recupera exactamente los archivos desaparecidos de v40–v47 ni presupone que su apariencia hubiera sido validada. Base original: `20311f824ebda9ecbf553f28915f100e8e252995`; inventario inmutable de origen: `RECUPERACION_V39.json`.

## Lo que ya contiene v39

- Generación por semilla, streaming y mundo conectado; Poolrooms originales, solares y amplias; interiores de oficinas, hotel, espera y comedor; supermercados pequeños y centros comerciales completos de varios pisos.
- Nivel 94, variantes de casas, interiores e interacción, pasto con ajustes de calidad; biomas infantiles anteriores y el nuevo SoftPlay multinivel transitable.
- Agua HDRP, iluminación regional, Rendimiento/Alto/Cinemático RT con alternativa compatible, cámara y destellos, sonido, pausa, configuración y acceso F7 a renderizado.
- Modo maestro/chat, vuelo y TP, linterna, objetos agarrables, doble salto y deslizamiento.

## Funciones posteriores: inventario y estado

| Solicitud del historial | Contraste con v39 / siguiente trabajo | Estado |
| --- | --- | --- |
| Acabado visual del nuevo bioma infantil (v40 mencionada) | R1: 21 mapas originales PBR de 1024 px; redes oscuras separadas, fundas de postes, esferas con normales suaves, piso continuo y tonalidad coherente de luminarias. | Implementado; validación visual Unity pendiente |
| Calidad gráfica adicional y RT mejorado sin quitar los modos previos | R2 añade Ultra: hasta 8 muestras, dos rebotes de reflejos y cuatro de GI; alternativa SSR/SSGI sin RT, sombras 2048, sin cambiar API gráfica. | Implementado; render Unity pendiente |
| Nivel 11: ciudad extensa, 300 edificios/zonas, 500 espacios, 400 muebles, 2000 objetos y 35 vehículos, incluido avión/helicóptero; recursos en línea con licencia | R4 integra manzanas/edificios/interiores/sol, dos objetos importados y un primer coche con WheelColliders. Cuatro fuentes CC0 adaptadas; las instancias no cuentan como modelos distintos. R8 añade un avión Apache-2.0 y aeródromos conectados. R9 incorpora helicóptero y helipuerto. Faltan más modelos/variedad y refinamiento urbano. | Parcial; validación Unity pendiente |
| Nivel 94 más compacto y más pasto con tres opciones | R3 reduce distritos, agrupa casas en vecindarios y casi duplica candidatos de pasto económico; conserva Off/Económico/Alto y límites de parches. | Implementado; apariencia/FPS pendientes |
| Mejor física al caer objetos | R2 añade contactos por material, menor resistencia al aire, interpolación, CCD, solver y herencia limitada del movimiento al soltar. | Implementado; PhysX pendiente |
| Dash con Shift y correr con `|` | R2: tres cargas, recarga, avance planar con colisiones y gravedad originales; cambio en ambos sistemas de entrada y ayuda. | Implementado; controles nativos pendientes |
| 150 % más contenido y 100 % más decoración realista global (v47 mencionada) | R6 añade herrajes PBR en salas, centros comerciales y casas cercanas: ocho familias/16 geometrías; hasta 12 o 24 por fragmento con controles gráficos. No duplica todos los catálogos anteriores. | Parcial; catálogos y validación Unity pendientes |
| Biblioteca inspirada en 906, patio residencial 188 y costa 48; entradas naturales; preparar entradas antiguas sin modificarlas aún | R5 integra los tres ambientes mediante el grafo regional, arquitectura, objetos y luz propia. Los accesos futuros de zonas anteriores son solo metadatos inactivos. | Implementado; validación Unity pendiente |

No se puede determinar el reparto exacto de funciones entre los números v41–v47, ni recuperar sus pruebas o apariencia originales. Las cifras recordadas de catálogos de versiones perdidas sirven como contexto, no como código recuperado ni requisitos nuevos.

## Reglas de continuidad

Exploración tranquila, sin muerte, enemigos, caras, ojos, hongos ni desafíos. Conservar piscinas, escaleras, variedad, iluminación propia por bioma, modos económicos, configuración y controles salvo el cambio expresamente pedido. No modificar la linterna. Añadir IDs serializados al final. Respetar los recorridos, suelo y carga segura antes de activar transiciones.

Cada etapa debe registrar archivos, pruebas ejecutadas, límites, commit y paquete reproducible; respaldar en GitHub sin reescribir historial. `tools/verify_recovery.py` comprueba la copia **original**, no una versión modificada: conservar el manifiesto original y publicar manifiestos nuevos para reconstrucciones.

## Verificación disponible

Integridad de los 326 archivos v39 comprobada nuevamente. Unity Editor no está instalado y faltan `Packages`/`ProjectSettings`: todavía no hay compilación Unity/HDRP, importación de shaders, PhysX, capturas reales ni medición de FPS. Los resultados históricos incluidos en v39 no cuentan como pruebas ejecutadas nuevamente.

### Etapa R1

Cambios reales en `Core/SoftPlayArchitecture.cs`, temas y materiales independientes, importación de mapas y carga de dos lotes visuales por sala. Se conservan los identificadores anteriores, las bibliotecas personalizadas, los apoyos, las escaleras, las barreras y la linterna. El detalle del piso usa dos triángulos; las redes respetan la calidad de curvas del perfil.

Verificación nueva: núcleo compilado con .NET 8/C# 9; comparación contra las huellas v39 de 108 salas infantiles y 80 muestras de otros biomas; validación de mallas, colores de fuentes y sintaxis de los scripts. Mapas nuevos reproducibles mediante `tools/generate_play_finish.py`, paquete completo mediante `tools/package_assets.py`. No equivale a una certificación de acabado AAA ni ausencia de errores visuales: falta ejecutar Unity.

### Etapa R2

Dash de 16 m/s durante 0,17 s, tres cargas y recarga de 0,8 s. No añade invulnerabilidad ni modifica el doble salto: el avance sigue pasando por CharacterController y los bloqueos de carga. Se cancela al pausar, entrar en menús, volar o esperar suelo. `|` mantiene la opción de alternar carrera; Input System busca la tecla en la distribución activa y usa barra inversa como alternativa. El gestor antiguo admite las posiciones habituales de barra, acento y tecla ISO.

Ultra se añade al final de la enumeración y conserva los tres modos previos. Solicita mayor muestreo solo cuando el hardware/API/Asset satisfacen la política existente. En GPU incompatible usa SSR/SSGI; no es una afirmación de RT ejecutado. Los rebotes configurados dependen del modo de trazado admitido por el HDRP Asset. El agua sigue usando su sistema HDRP nativo.

Los objetos sueltos reciben perfiles de contacto distintos para embalaje, rígidos, espuma, pelota y goma. Se conserva el límite de 32 objetos sueltos y la física se activa solo al soltar. Nuevas pruebas PlayMode de caída y pared incluidas, **no ejecutadas**. Verificación real: 9.754.412 comprobaciones del núcleo/mallas/huellas y sintaxis de 168 scripts en cuatro configuraciones. APIs de teclado contrastadas con Input System 1.11.2 y PhysicsMaterial con UnityCsReference 6000.3; esto no sustituye la compilación en Unity.

Respaldos ya publicados: `709436f` (R1) y `66be421` (exclusión de binarios de comprobación). Paquete R1 completo generado desde un árbol limpio: 347 archivos, 51.349.127 bytes, SHA-256 `5c619b2e028f0eccc62b3e163ab907bb0577025c85abd23bff2fa48997d3049f`. No incluye Roslyn ni herramientas dentro de Assets.

### Etapa R3

Nivel 94 pasa de envolventes nominales de 2,56–4,096 km por eje a 1,536–2,56 km; las reservas conectadas usan la misma fórmula y mantienen sus cuatro umbrales. Las casas se agrupan en lotes de cuatro con calles/descansos entre grupos; siguen teniendo una casa por celda de 64 m, conservando búsqueda, streaming, sótanos y soporte. La semilla sigue siendo determinista, pero esta distribución nueva cambia intencionalmente el terreno generado respecto a v39.

Pasto económico: de 300 a un máximo de 588 triángulos por parche, sin ampliar los nueve parches próximos. Alto mantiene 25 parches con detalle según distancia; Off no genera hojas. No se añaden colliders, luces, sondas ni GameObjects por hoja. No se promete una cifra de FPS.

Comprobación nueva: ocho distritos, propietarios de lotes, soporte finito y separaciones entre casas; límites de triángulos y desactivación del pasto; las 188 huellas anteriores se conservan. Resultado antes del ajuste de expectativa de la prueba nativa: 9.847.565 comprobaciones y sintaxis de 168 scripts × cuatro configuraciones. La prueba nativa de presupuesto económico se actualiza a 5.292 triángulos (nueve parches), sin eliminar su comprobación de suelo ni su prueba de desactivación.

R2 publicado en `dd85ec1`; paquete completo de 351 archivos y 51.353.662 bytes, SHA-256 `96acbcc7ce5a2f785da31568e46df020cfcf57e922f6f6a5ec673d5e8a5f6470`.

R3 publicado en `f2a201f`; paquete completo de 351 archivos y 51.353.863 bytes, SHA-256 `27a07f11411dccc105b6a6f632c7d571d32ac57e87651f778f1dbf90f2288b3b`. `RECONSTRUCCION_ESTADO.json` conserva una cola legible por herramientas. El workflow nuevo reproduce las comprobaciones locales y el paquete; no se presupone que GitHub Actions ya lo haya ejecutado.

### Corrección de CI después de R3

El primer run falló con CS1705: instalar SDK 8 no impedía que el runner eligiera SDK 10 y sus ensamblados Roslyn. `global.json` fija 8.0.408 sin roll-forward; setup-dotnet usa ese archivo y comprueba la versión elegida. El run [38999993092](https://github.com/waltennecro/poolrooms/actions/runs/38999993092) terminó correctamente, incluidos compilación/sintaxis, hashes, empaquetado y upload del artefacto de 49 MB. Evidencia y digest en `Verification/ci_sdk_fix.json`. La descarga autenticada del artefacto no se ha ejecutado desde este entorno; la página pública sí confirma su publicación. Las cifras del núcleo son aserciones, no pruebas nativas de Unity.

### Etapa R4 — City11 inicial

Ver `Assets/PoolroomsProcedural/Documentation/CITY11_RECONSTRUCCION_R4.md`. Integración real por regiones del generador, edificios físicos de 2–6 plantas, ocho programas de mobiliario, cuatro variantes de fachada, cristales con colisión, iluminación independiente y TP desde el modo maestro. El coche usa un modelo CC0 adaptado con dos LOD derivados; control, suspensión/freno, salida segura, streaming y origen flotante tienen código, pero PhysX y controles siguen pendientes en Unity.

Catálogo verificado: cuatro fuentes distintas, tres objetos y un coche; dos objetos y el coche tienen colocación, el farol queda preparado. No se cuentan texturas, LOD, repeticiones o combinaciones de parámetros como modelos descargados. La muestra de 324 manzanas contiene 1.296 edificios generados, 5.185 espacios de planta, 2.592 instancias de objetos, 15.555 conjuntos de mobiliario y 40 puntos del mismo coche. Los edificios/muebles son procedurales, no nuevos modelos de internet; no se declara cumplido el catálogo completo solicitado.

Verificación nueva: 11.731.000 aserciones del núcleo/mallas/huellas/sintaxis, 174 scripts en cuatro configuraciones y auditoría de las cuatro fuentes/licencias/OBJ/LOD. Se mantienen las 188 huellas protegidas, la linterna y las entradas anteriores. Nuevos tests PlayMode de fuentes y recorridos incluidos, sin ejecutar. Regeneración de fuentes opcional con NumPy/Pillow; CI audita los archivos ya adaptados con Python estándar.

### Etapa R5 — biblioteca, patio y costa

Integración detallada en `Assets/PoolroomsProcedural/Documentation/TRES_NIVELES_R5.md`. El generador ya produce los tres ambientes y el TP los encuentra. Galerías/plantas físicas con escaleras y huecos reales; costa continua por distrito con pasarela seca, agua nativa y pabellones. Se conservan las entradas anteriores y los candidatos futuros permanecen inactivos. La arena incorpora tres mapas originales; no se añade ningún modelo descargado: el farol CC0 previamente preparado ya se coloca.

Comprobación acumulada: 16.290.728 aserciones, 177 scripts × cuatro configuraciones, 72 salas nuevas y 300 apoyos de objetos, continuidad costera, 24 mapas PBR y cuatro fuentes. Las 188 huellas protegidas se mantienen. Se incluyen recorridos PlayMode, pendientes de ejecutar junto con shaders, APIs HDRP, físicas y FPS. CI de R4 confirmado correcto; evidencia en `Verification/ci_city11_R4.json`.

### Etapa R6 — detalle arquitectónico común

Ver `Assets/PoolroomsProcedural/Documentation/DECORACION_R6.md`. Monturas apoyadas en paredes, dos tandas visuales con presupuesto y opción Original/Realista/Doble en Gráficos/F7; sin nuevas colisiones ni luces. Salas de todos los ambientes, ambos sistemas de centros comerciales y casas detalladas comparten este acabado. No hay nuevos modelos externos: son ocho familias y 16 geometrías originales.

Comprobación acumulada: 16.828.561 aserciones, 179 scripts × cuatro configuraciones, 24 mapas y cuatro fuentes CC0; linterna byte a byte igual a v39, y `Preparacion_V48` intacta. CI R5 terminó correctamente con artefacto publicado: `Verification/ci_quiet_levels_R5.json`. ZIP R5 completo limpio: 409 archivos, 88.539.175 bytes, SHA-256 `2dd9f8532a5e7793d94864ca497c9550d28aab6734e32a1225bcae59523d1d27`. Quedan los catálogos externos y más decoración urbana, vehículos/avión/helicóptero y validación nativa.

### Etapa R7 — primer mueble externo y horneado de UV

Ver `Assets/PoolroomsProcedural/Documentation/MOBILIARIO_CC0_R7.md`. SheenChair CC0 se integra como objeto agarrable de suelo en City11/biblioteca/patio. Cinco fuentes distintas en total, un vehículo y un modelo de mueble externo. Dos LOD del sillón (6.169/1.210 frente a 39.936 triángulos), horneado de UV0/UV1 con AO y normales, sin modificar originales. No se reproducen exactamente las extensiones opcionales de brillo textil.

Núcleo: 16.828.897 aserciones y sintaxis de 179 scripts × cuatro configuraciones; los apoyos nuevos pasan. Se añaden comprobaciones analíticas del horneador a CI con dependencias fijadas; las auditorías previas no se omiten. R6 tiene CI correcto y paquete completo de 412 archivos, 88.545.632 bytes, SHA-256 `5fb0b9ffea381026c0eb826375b9781da218ddebb2b97a1f90f4ce682112dd0e`; evidencia en `Verification/ci_decoration_R6.json`.

Sigue pendiente ampliar recursos urbanos/vehículos y validar en Unity. `kenney.nl` está bloqueado por la política de red; se guardó un borrador que añade ese dominio y conserva `api.github.com`, pendiente de revisión, guardado y publicación del entorno. No se da por activado ni se descargaron recursos por rutas alternativas.

### Respaldo comprobado de R7

Publicado en `12fd6ad`. CI [39005054928](https://github.com/waltennecro/poolrooms/actions/runs/39005054928) terminó correctamente, incluidos SDK, núcleo, texturas, fuentes/licencias, pruebas analíticas de UV y empaquetado/upload. Artefacto `12091190427`; digest y resultados en `Verification/ci_sources_R7.json`. La descarga autenticada del artefacto de Actions sigue sin ejecutarse aquí.

Sí se descargó de nuevo el archivo público del repositorio para ese commit: 157.704.602 bytes, CRC correcto y los 428 archivos de Assets coinciden por SHA-256. Este ZIP de código fuente incluye carpetas externas a Assets; copiar únicamente `Assets/PoolroomsProcedural` a Unity. El paquete local completo para Assets tiene 98.580.007 bytes y SHA-256 `2e98412c969f836c34e7c7c43d7c0c1ba59a66ab68ea12ca30ba776927ca4a97`. Incluye todas las etapas R1–R7, no una recuperación exacta de v47.

### Etapa R8 — avión civil y pistas conectadas

Ver `Assets/PoolroomsProcedural/Documentation/AVIACION_R8.md`. Aeródromos de 21 celdas en City11, sin invadir reservas/entradas, pista de ancho completo, terminal, asfalto PBR y TP a un emplazamiento real. Primer avión pilotable con fuente Apache-2.0, dos hélices, vuelo asistido, cámara exterior y salida terrestre comprobada por código. Comparte streaming/origen flotante y exposición exterior; coches, controles a pie y linterna se conservan.

Catálogo: seis modelos externos distintos, cinco CC0 y un Apache-2.0. Son dos clases de vehículo implementadas; no se dan por completados los 35 vehículos ni el helicóptero. Los aviones usados siguen la política acotada de los coches: no hay guardado persistente de vehículos aparcados. Kenney ahora es accesible: se revisaron sus kits fuera del repositorio y se descartó integrarlos por su estilo visual. Quaternius continúa dando 403; no se descargaron recursos por rutas alternativas.

Verificación real: 16.922.766 aserciones del núcleo y sintaxis de 184 scripts × cuatro configuraciones; nueve aeródromos/189 celdas, soporte de uniones, conexiones recíprocas, determinismo, reservas y vuelo matemático en cinco pasos temporales. Las 188 huellas protegidas y los tres archivos de linterna originales se conservan; Preparacion_V48 permanece intacta. Auditorías de fuentes/licencias, 27 mapas PBR y horneador UV correctas. Nuevas pruebas nativas incluidas, sin ejecutar: falta compilación Unity/HDRP, importación, PhysX, shaders, aspecto y FPS. CI de la primera revisión R8 correcto: núcleo, mapas, cinco fuentes CC0, avión/licencia Apache-2.0, horneador, empaquetado y upload del artefacto. Evidencia en `Verification/ci_aircraft_R8.json`. Después se reforzó la prueba de reservas activando centros comerciales y Nivel 94 y rechazando explícitamente una pista que caería dentro del terreno reservado.

Respaldo R8 publicado en `3767d04`. Paquete limpio completo: 448 archivos, 103.150.479 bytes, SHA-256 `504320d40891bfdb0eeebb1a76429b29de1c2437e621a5151528affa9e733fb4`. Nueva descarga pública del repositorio para ese commit: 162.815.878 bytes, CRC correcto y los 448 archivos de Assets coinciden por SHA-256. El ZIP público contiene también herramientas externas a Assets; importar únicamente `Assets/PoolroomsProcedural`. No se ha probado la descarga autenticada del artefacto de Actions.

Cierre de verificación R8: la revisión reforzada `492fe0e` también pasó CI completo, run [39042575321](https://github.com/waltennecro/poolrooms/actions/runs/39042575321), incluido empaquetado/upload. El paquete Assets limpio sigue teniendo 448 archivos y 103.150.479 bytes; SHA-256 `b3446a6d054c3e4cdc84fd9fe6a238a8c9b9f0a24ef510bb2c7da1377ac4b415`. Se volvió a descargar ese commit desde GitHub: 162.817.438 bytes, CRC y los 448 hashes de Assets correctos. Evidencia actualizada en `Verification/ci_aircraft_R8.json`. Los cambios de este registro final no modifican Assets.

### Etapa R9 — helicóptero y plataforma integrada

Ver `Assets/PoolroomsProcedural/Documentation/HELICOPTERO_R9.md`. Fuente derivada de MH-6 de AnirudhRao, CC BY 4.0, sin reutilizar combate ni sombreado sin iluminación. Normales/UV y nueve mapas PBR originales, dos LOD y ejes conservados por marcadores. Helipuerto real en la reserva City11, controles independientes y asistencia de estacionario. Avión/helicóptero comparten seguridad de streaming, pausa, origen y salida; fábrica del avión R8 intacta.

Siete modelos externos distintos y tres clases de vehículo. La muestra de 324 celdas con aeródromos activos contiene 37 puntos del mismo coche, un avión y un helicóptero: son instancias, no 39 modelos diferentes. 1.212 edificios procedurales y 4.846 espacios de planta; no se presentan como edificios descargados. Quedan arquitectura externa, más mobiliario/variedad y validación nativa.

Núcleo: 16.935.773 aserciones; 188 scripts × cuatro configuraciones; las 188 huellas protegidas y la linterna se mantienen. Fuentes/licencias/LOD/marcadores, 36 mapas PBR y UV verificados. Nuevos tests PlayMode incluidos, sin ejecutar; APIs Unity, PhysX, shaders, calidad visual y FPS siguen pendientes.

R9 está publicado en `f0bc28b`. CI [39044589411](https://github.com/waltennecro/poolrooms/actions/runs/39044589411) correcto, incluidos auditoría del helicóptero, empaquetado y upload. Evidencia en `Verification/ci_helicopter_R9.json`. ZIP acumulativo limpio: 472 archivos, 113.021.213 bytes, SHA-256 `d920acd635a36a984229fa7cac74bc16f64703c8826fb64abca337ff83a09a4d`.

### Etapa R10 — mobiliario atribuido y apoyo completo

Ver `Assets/PoolroomsProcedural/Documentation/MOBILIARIO_R10.md`. Sofá nuevo y variante damasco de la silla ovalada existente, CC BY 4.0 de Eric Chadwick/Wayfair. Nueve fuentes/adaptaciones externas en total, **ocho familias geométricas y una variante adicional**; no se cuenta la silla recoloreada como un modelo nuevo. Dos familias geométricas de muebles importados: silla y sofá.

Colocación determinista en City11, biblioteca y patio, dimensiones verificadas contra las mallas y márgenes de entrada/escalera. E/Q y física al soltar, con masas/volúmenes apropiados. Trece mapas horneados del texturizado original y normal/AO, UV1 conservada en silla y atlas generado desde UV0 en sofá. Solo la variante material predeterminada del sofá; sheen/specular opcional no se presenta como réplica exacta. Corrección del factor de color base glTF y descarte de muebles lejanos por tamaño en pantalla.

Núcleo: 16.939.230 aserciones, 190 scripts × cuatro configuraciones; 336 apoyos completos, rutas y selección comprobados. Fuentes/licencias/trece mapas y contrato de dimensiones auditados; regeneración artística reproducida byte a byte. Las 188 huellas protegidas, linterna y Preparacion_V48 se conservan. Pruebas nativas nuevas incluidas, sin ejecutar. Quedan más arquitectura/decoración externa y cobertura de otros biomas, más variedad urbana, compilación Unity, PhysX, shaders, apariencia y FPS.

R10 publicado en `030debc`; CI [39045905663](https://github.com/waltennecro/poolrooms/actions/runs/39045905663) correcto, incluidos todos los catálogos, UV, empaquetado y upload. Evidencia en `Verification/ci_furniture_R10.json`. Paquete Assets limpio completo: 502 archivos, 130.954.768 bytes, SHA-256 `787042e2ac68c722422ee51ca89bbbf585ae0f3a92b82157d47d0cfa16378bae`.

Se descargó de nuevo el archivo público del repositorio de ese commit: 196.021.021 bytes, SHA-256 `b7b2c6942d2b66c7ff83785ac9275d3e4524371bb8934eac20b1951394f52f51`, CRC correcto y los 502 archivos de Assets coinciden por SHA-256. Importar únicamente `Assets/PoolroomsProcedural`; el archivo del repositorio también contiene herramientas y originales fuera de Assets. La descarga autenticada del artefacto de Actions no se ejecutó; sí se comprobó esta descarga pública. Este registro final no cambia Assets.

## R11 · Coche y calles

Ruedas originales separadas y animadas en tres LOD, contactos medidos, vista exterior con esfera ante obstáculos y física suspendida al capturar entrada/faltar chunks. Las aperturas de City11 y sus barreras comparten anchos recíprocos de 6 m, respetando los enlaces y entradas existentes. No se añade un modelo al catálogo. Núcleo: 16.950.397 aserciones, 687 uniones y 63 bordes; 192 scripts × 4 configuraciones de sintaxis. Fuentes CC0 auditadas; prueba nativa del rig añadida pero sin ejecutar. Detalle: `Assets/PoolroomsProcedural/Documentation/COCHE_Y_CALLES_R11.md`.

## R12 · Mobiliario en otras regiones

Sofás y sillas con licencia sustituyen asientos provisionales de centros comerciales refinados y salones/salas de espera de casas variadas del nivel 94. Máximo dos por sector/casa detallada; las casas lejanas no incorporan recursos ni Rigidbody. Las claves sobreviven a la recarga de chunks durante la sesión. Núcleo: 34 asientos en 36 casas y 60 en 30 sectores, soporte completo y márgenes ante escaleras/frentes; 16.951.790 aserciones, 192 scripts × 4 configuraciones. No son modelos nuevos ni prueba nativa de Unity. Detalle: `Assets/PoolroomsProcedural/Documentation/MOBILIARIO_REGIONAL_R12.md`.

Respaldo R11–R12: ambos CI completaron todos los pasos y publicaron artefactos. Descarga pública nueva de R12 contrastada con los 506 archivos de Assets; ZIP acumulativo local de 130.904.025 bytes, CRC y hashes comprobados. Se conservan evidencias en `Verification/ci_city_driving_R11.json` y `Verification/ci_regional_furniture_R12.json`. No se probó la descarga autenticada de Actions ni se ejecutó Unity.

## R13 · Validación nativa preparada, ejecución bloqueada

Lanzador PlayMode para un proyecto real Unity 6.3/HDRP 17.3 que contrasta el paquete y exige resultados NUnit nuevos/no vacíos. Distingue fallos, omisiones y filtros. Captura opcional de 30 segundos de exploración real (sin HUD): percentiles de tiempo de cuadro, equipo, calidad, regiones, memoria y GC. El preflight cloud confirma que falta Unity Editor; no se ejecutan pruebas nativas ni se inventan FPS. Los tests del lanzador usan un proceso falso y no validan Unity. Ver `Assets/PoolroomsProcedural/Documentation/VALIDACION_NATIVA_R13.md`.

## R14 · Catálogo, apoyos automáticos y arquitectura

CarConcept, AntiqueCamera y SheenWoodLeatherSofa integrados con originales/licencias, PBR, LOD y colisiones de interacción. Catálogo acumulado: 12 fuentes/adaptaciones, 11 familias y una variante de silla; cuatro tipos de vehículo por recurso y tres clases de control. Colocación por apoyos en ocho biomas (190 muestras válidas), cámaras de pie, sofá costero y seis tipologías de fachada/detalle de calles. Corregida la barandilla que cerraba la galería de planta baja del patio; entradas de los tres niveles refinadas sin activar sockets históricos. Ver `Assets/PoolroomsProcedural/Documentation/AMPLIACION_REGIONAL_R14.md`.

No se declara terminada toda la petición: no se ejecutó Unity, no se midieron FPS ni se descargaron edificios completos. La muestra City11 cumple cantidades mediante instancias procedurales/objetos externos, no mediante miles de modelos distintos. Sponza se descartó por no establecer permiso general de reutilización bajo su acuerdo específico. Esos límites siguen explícitos en el inventario.

R13–R14 respaldadas: CI final `39053442886` correcto, artefacto `12108422910` publicado; descarga pública nueva de `29c5b7b` contrastada con los 607 archivos de Assets (CRC/SHA-256). Paquete local de 192.937.277 bytes, creado desde árbol limpio. Evidencia: `Verification/ci_regional_expansion_R14.json`. Sin prueba nativa de Unity ni descarga autenticada del artefacto de Actions.

## R15 — Terrazas de ciudad y materiales originales de Bistro

Se integran automáticamente un toldo, una mesa y dos sillas como máximo por bloque elegible, conservando calles, puertas, porches existentes y claves anteriores de objetos. Mesa y sillas son agarrables; masas de 12/4 kg y sin simulación mientras no se sueltan. Tres familias geométricas de Amazon Lumberyard Bistro, CC BY 4.0, con procedencia fijada, 16 PNG de hasta 1024 px y dos LOD por pieza. La conversión mantiene el eje vertical original y el flujo HDRP Specular Color con F0 y gloss-alfa originales; no se importa toda la escena Bistro ni se ejecuta código del origen.

Comprobación del núcleo: 32 terrazas/96 objetos en 96 bloques; apoyo de nueve puntos, separación entre muebles, espacio de calle/puerta y ausencia de colisión con la fachada final. Auditoría offline de licencia, hashes, texturas, geometría/LOD y dimensiones. Pruebas nativas ampliadas pero pendientes de ejecución en Unity. **Continúan pendientes los edificios completos descargados y la validación nativa**, sin certificar apariencia ni FPS. Se amplía el catálogo a 15 fuentes/adaptaciones, 14 familias geométricas y una variante de silla; no son 15 modelos de edificios.

Evidencia R15: revisión `e0e386ae43c7dae1326bd38f2d86e1701b59fa7d`, [CI 39070290885](https://github.com/waltennecro/poolrooms/actions/runs/39070290885) completa, artefacto `12114402973`. Paquete Assets: 646 archivos, 200.730.454 bytes, SHA-256 `8cb203497345fd803147379df8639bb79dd20f0dc0baeab14590b8a92becde70`. Respaldo público descargado: 304.195.508 bytes, SHA-256 `aa09f87a6aefd5ac9116c4e3a17012bfc7fd424b84b71b8b6727f6f3815fdbba`; CRC correcto y coincidencia de Assets y procedencia Bistro. La normalización inicial del glTF se corrigió preservando sus bytes de origen. Todo el historial permanece; no se reescribió.


## R16 — actualización amplia de exploración

Se añade portada integrada con ajustes y pausa, regreso al inicio sin teletransporte, inspección de objetos con R + ratón, cámara de aterrizaje opcional y estilo Arquitectura. Dos nuevos biomas originales, Jardín de cristal y Galería de tránsito, se encuentran en Expanded y el TP existente: tragaluces abiertos físicamente, sol regional, materiales independientes, mobiliario reutilizado con licencia y acústica propia. El menú no promete guardado de partida; retoma la sesión abierta.

El streaming comprueba como máximo cinco salas al adjuntar una sala, conservando revisiones completas para cambios de regiones y descargas. Las ópticas usan la lista activa de luces, sin cambiar selección, presupuestos ni distancia. Se añade antialiasing especular a las fábricas de materiales. Ningún radio, LOD, mapa o muestreo RT se reduce.

[Detalle, instalación y límites](Assets/PoolroomsProcedural/Documentation/ACTUALIZACION_R16.md). R16 no agrega modelos externos al catálogo ni recupera exactamente v47. Continúan pendientes los edificios externos completos y la validación nativa/visual/de rendimiento; no se certifica acabado AAA.

Evidencia R16: revisión `3ae3da067ff571f61b33da509db942f7b73c8f74`, [CI 39073904650](https://github.com/waltennecro/poolrooms/actions/runs/39073904650) completa y artefacto `12116605789` publicado. Paquete Assets: 649 archivos, 200.739.256 bytes, SHA-256 `cfbb1765af8b3904161f8a2d6c7d8b88fb93cc692bca03b0955b16c87619a726`. Respaldo público descargado y verificado: 304.209.169 bytes, SHA-256 `699d7e040da05f720c59268d07c4cfc7fea479de6db41c3c0c58b1ae1ac5aefe`; CRC y hashes de los 649 archivos correctos. Núcleo: 18.354.240 aserciones, 200 scripts en cuatro configuraciones sintácticas. Son comprobaciones matemáticas y sintácticas, no pruebas nativas de Unity.


## R17 — edificios licenciados y cierre de auditoría de recuperación disponible

Se integran dos fuentes de edificios CC BY 4.0 de Bistro como exteriores adaptados sobre arquitectura transitable de cuatro plantas. Se mantienen los edificios procedurales anteriores, el resto de alturas y los sistemas existentes. Hay recortes de entrada/ventanas/interiores, marcador oculto de orientación por LOD, materiales PBR de origen y selección limitada por manzana. [Detalles, cifras reales e instalación](Assets/PoolroomsProcedural/Documentation/RECONSTRUCCION_R17.md).

Se vuelven a comprobar los 326 archivos originales de v39 en su revisión de Git. Los originales v40–v47 no aparecen entre los archivos/respaldos disponibles ni los objetos recuperables de Git: R1–R17 son reconstrucción y mejoras. La comprobación nativa se intentó mediante preflight y quedó bloqueada por ausencia de Unity Editor; no se ha ejecutado Unity. La integración de los tres niveles y la decoración permanece en código; su revisión de aspecto, física y rendimiento requiere el Editor.

Evidencia R17: revisión `adb646bdd414238a982e19ff3b717ea48e133553`, [CI 39091032444](https://github.com/waltennecro/poolrooms/actions/runs/39091032444) completa, artefacto `12122752613` publicado. Paquete Assets: 718 archivos, 245.959.660 bytes, SHA-256 `a134b2d9d2cbe137a451651cfe07c459cca185a161328b5556b8426032bd894c`. Respaldo público descargado: 392.160.655 bytes, SHA-256 `39834816ed1c778f5accab8650ef046ea171d2be4b936b70c238ead82957e812`; CRC y SHA de todos los archivos versionados, incluidos Assets, fuentes y licencias, coinciden. La generación limpia reproduce los 66 recursos de edificios sin mapas sobrantes. Núcleo: 24.043.795 aserciones y 202 scripts en cuatro configuraciones sintácticas; no son pruebas nativas de Unity.

## R18 — Mejora del movimiento reconstruido

Tolerancia al salir de bordes (100 ms) y memoria de pulsación antes de
aterrizar (120 ms), manteniendo dos saltos. Interfaces y carga descartan
intenciones pendientes; la pausa conserva el impulso vertical. Sin cambios
en gráficos, alcance de streaming ni linterna. Detalles y límites en
`Assets/PoolroomsProcedural/Documentation/ACTUALIZACION_R18.md`.

R18 publicada en `9d29cccededc2df7976fe736cf899daae13680cb`. CI
[39092613980](https://github.com/waltennecro/poolrooms/actions/runs/39092613980)
completada correctamente, incluido empaquetado y publicación del artefacto.
Descarga pública independiente: 392.166.106 bytes, CRC válido y 868 archivos
versionados coincidentes por SHA-256. Evidencias en
`Verification/ci_movement_R18.json`; Unity sigue sin ejecutarse.

## R19 — Expansión relajante, nivel 1 de 10: Orangerie

Nuevo bioma de jardines amueblados integrado en Expanded y en el TP.
2.500 huellas estructurales distintas con coordenadas y semilla idénticas;
16 objetos interactuables por receta (40.000 instancias distribuidas del
catálogo, no modelos nuevos ni objetos simultáneos), cuatro acabados PBR
originales a 1024 px y sol regional con la alternativa RT existente.
Rutas anchas y ocho luces candidatas respetando los presupuestos actuales.
Documentación: `Assets/PoolroomsProcedural/Documentation/EXPANSION_01_ORANGERIE.md`.
Objetivo completo y conteos separados: `EXPANSION_NIVELES.json`. Los otros
nueve niveles y las 10.000 zonas globales adicionales siguen pendientes.
Las pruebas nativas añadidas no se han ejecutado; no hay certificación visual
ni mediciones FPS. Esta etapa es nueva construcción, no recuperación exacta.

R19: CI [39095375276](https://github.com/waltennecro/poolrooms/actions/runs/39095375276)
completada correctamente, incluido catálogo, huellas antiguas, sintaxis,
texturas, licencias y empaquetado. Assets acumulativos: 735 archivos,
257.632.818 bytes; respaldo público descargado: 403.843.476 bytes, CRC válido
y 888 archivos versionados coincidentes por SHA-256. Evidencias completas
en `Verification/ci_orangerie_R19.json`. Unity no se ha ejecutado.

## R20 — Planta con licencia y créditos visibles

Una familia nueva (original CC0 de Rico Cilliers, materiales CC BY 4.0 de
Eric Chadwick / DGG), adaptada sin insectos, luces ni animación de la demo.
Dos plantas interactuables por sala de lectura/té de la Orangerie. Se añaden
recorte alfa, seis mapas originales y dos LOD; regeneración desde cero idéntica.
Créditos y licencias disponibles en inicio/pausa. 42.000 instancias planificadas
en las 2.500 recetas; 2.000 son repeticiones de esa planta, no modelos nuevos.
La transmisión difusa glTF no se reproduce exactamente; nativo pendiente.
Documentación: `Assets/PoolroomsProcedural/Documentation/ACTUALIZACION_R20_RECURSOS.md`.

R20: CI [39096093133](https://github.com/waltennecro/poolrooms/actions/runs/39096093133)
completada correctamente, incluido el nuevo auditor de la planta/licencias
y el empaquetado. Assets acumulativos: 755 archivos, 268.127.124 bytes.
Respaldo público descargado: 425.123.968 bytes, CRC válido y 919 archivos
coincidentes por SHA-256. Evidencias en `Verification/ci_garden_R20.json`.
Las pruebas nativas de aspecto, recorte, interacción y FPS siguen sin ejecutar.

## R21 — Baños de travertino, expansión 2 de 10

Bioma conectado con 2.500 composiciones estructurales verificadas, piscinas
con fondos/escalones reales, tumbonas, lucernarios y 16 objetos por sala.
Se descargan e integran una lámpara de DGG (CC BY 4.0) y un material de
mimbre de Eric Chadwick (CC0), con fuentes, licencias y atribuciones.
6.000 piscinas y 40.000 instancias distribuidas del catálogo, sin carga
simultánea ni conteo de LOD como modelos nuevos. Ocho luces candidatas
por sala y reflejos del sistema compartido, sin reducir streaming.
Documentación: `Assets/PoolroomsProcedural/Documentation/EXPANSION_02_BANOS_TRAVERTINO.md`.
Núcleo y sintaxis verificados; Unity, aspecto, física y FPS pendientes.
Quedan ocho niveles y las 10.000 zonas globales separadas.

R21: CI [39099486585](https://github.com/waltennecro/poolrooms/actions/runs/39099486585) completada correctamente, incluidos
los auditores de fuentes/licencias y el paquete acumulativo. Assets: 791 archivos,
288895774 bytes. Respaldo público descargado: 455319180 bytes,
CRC válido y 970 archivos coincidentes por SHA-256. Evidencia:
`Verification/ci_baths_R21.json`. El preflight nativo confirma Editor Unity ausente;
compilación Unity/HDRP, aspecto, PhysX y FPS siguen pendientes.

## R22 — Parque acuático interior del atardecer

Bioma conectado con 5.600 composiciones, ocho familias de toboganes a escala
del jugador, torres de dos tramos, piscinas con fondos/escalones reales y válvulas
de corriente suave. Sol/cielo sin lámparas de relleno; agua HDRP clara, PBR
original y mobiliario ya licenciado. Los descensos son guiados, no simulación
hidráulica. Núcleo, 188 huellas protegidas y sintaxis C# verificados; Unity nativo pendiente.
Documentación: `Assets/PoolroomsProcedural/Documentation/PARQUE_ACUATICO_R22.md`.
La petición concreta sustituye la propuesta del tercer nivel; quedan siete
y las 10.000 zonas globales separadas.

R22: CI [39144441223](https://github.com/waltennecro/poolrooms/actions/runs/39144441223) completada correctamente, incluido
el barrido adicional de 33.600 planes, auditores de recursos y paquete acumulativo.
Assets: 809 archivos, 290354841 bytes. Respaldo público descargado:
456787694 bytes, CRC válido y 991 archivos coincidentes por SHA-256.
Evidencia: `Verification/ci_aquatic_R22.json`. Compilación Unity/HDRP, aspecto,
PhysX y FPS siguen pendientes; el Editor no está instalado en la nube.

## R23 — 250 familias de toboganes y agua interior

Diez trazados, cinco cubiertas y cinco perfiles, con colores independientes del
conteo. Cristal con colisión física, naves en cuatro orientaciones y película
cóncava animada de agua sobre cada canal. Se conservan piscinas HDRP y distancias;
la lámina es aproximación visual, no simulación hidráulica. Núcleo, 188 huellas
antiguas y sintaxis C# verificados;
Unity, shaders, aspecto, transparencia, física y FPS pendientes.
Documentación: `Assets/PoolroomsProcedural/Documentation/TOBOGANES_250_R23.md`.

R23: CI [39147700285](https://github.com/waltennecro/poolrooms/actions/runs/39147700285) completada correctamente, incluidos
33.600 planes, auditores de texturas/fuentes y paquete acumulativo. Assets: 818 archivos,
290568748 bytes. Respaldo público descargado: 457009405 bytes, CRC
válido y 1003 archivos coincidentes por SHA-256. Evidencia: `Verification/ci_flumes_R23.json`.
Unity/HDRP, shaders, transparencia, aspecto, PhysX y FPS siguen sin ejecutar.

## R24 — Casas de lluvia, expansión 4 de 10

Barrio conectado con 2.500 recetas de cuatro casas accesibles, cinco programas
de interior y cinco tejados. Mobiliario licenciado agarrable, porches y galerías
con cristal físico, doce mapas PBR originales, cielo nublado, lluvia bajo un
presupuesto compartido y bucle de audio original. No se cuentan las instancias
como modelos nuevos. Núcleo, regresión acumulativa, 188 huellas anteriores y sintaxis C# comprobados.
Documentación: `Assets/PoolroomsProcedural/Documentation/CASAS_DE_LLUVIA_R24.md`.
Compilación Unity/HDRP, apariencia, partículas, audio, PhysX y FPS sin ejecutar.
Quedan seis niveles y las 10.000 zonas globales separadas.

R24: CI [39150783950](https://github.com/waltennecro/poolrooms/actions/runs/39150783950) completada correctamente, incluidos
catálogos y auditor de lluvia/audio, fuentes licenciadas, texturas y paquete acumulativo.
Assets: 838 archivos, 296228351 bytes. Respaldo público descargado:
462679134 bytes, CRC válido y 1028 archivos coincidentes por SHA-256.
Evidencia: `Verification/ci_rain_R24.json`. No se ha ejecutado Unity nativo.

## R25 — Exploración de todos los niveles a pie

La exploración unificada incluye los 18 ambientes implementados y los distritos
de centro comercial/nivel 94 incluso con perfiles antiguos que los desactivaban.
Cobertura regional barajada sin orden obligatorio, umbrales de lluvia reparados
y columnas apartadas de salidas cívicas. Pasan 110 rutas de grafo, 148 pares de
conexiones físicas y 10 casos de puentes; regresión acumulativa y sintaxis C# verificadas.
Documentación: `Assets/PoolroomsProcedural/Documentation/EXPLORACION_CAMINANDO_R25.md`.
El modo maestro es opcional; pruebas nativas Unity y FPS siguen pendientes.

R25: CI [39153744898](https://github.com/waltennecro/poolrooms/actions/runs/39153744898) completada correctamente, incluidos
catálogos, fuentes/licencias, medios y paquete acumulativo. Assets: 841 archivos,
296235276 bytes. Respaldo público descargado: 462693803 bytes, CRC
válido y 1033 archivos coincidentes por SHA-256. Evidencia: `Verification/ci_walkable_R25.json`.
Un muestreo adicional con composición Exploration pasa 555.390 aserciones sobre
los mismos archivos Core. Unity nativo y FPS siguen sin ejecutar.

## R26 — Compatibilidad de luces y refinamiento de agua

Se corrigen los tres CS0103 de LightUnit mediante dos importaciones de namespace;
la linterna conserva sus intensidades y efectos. Agua con menor distorsión del
fondo y suavizado conservado a media distancia, capturas HDRP 128/256/512 según
detalle y asignaciones estables entre habitaciones cercanas. SSR con más pasos
en Alto/Ultra, manteniendo Rendimiento y las distancias. El límite compartido
de sondas se conserva; no se instala una sonda por habitación.
Documentación: `Assets/PoolroomsProcedural/Documentation/AGUA_REFLEJOS_R26.md`.
Compilación Unity/HDRP, shaders, imagen, PhysX y FPS siguen pendientes; las
comprobaciones reducidas de nombres no sustituyen una compilación nativa.

R26: regresión local completa correcta, 188 huellas conservadas y 224 scripts en
cuatro configuraciones. Son 1.807.458.667 aserciones matemáticas acumulativas,
no pruebas nativas Unity. Auditoría de los 82 mapas PBR y medios de lluvia correcta.

R26: CI [39169093136](https://github.com/waltennecro/poolrooms/actions/runs/39169093136) correcta, incluidos catálogos, fuentes/licencias,
medios, paquete y artefacto. Assets: 842 archivos, 296237986 bytes.
Respaldo público descargado: 462703146 bytes, CRC válido y 1037
archivos coincidentes por SHA-256. Evidencia: `Verification/ci_water_R26.json`.
Compilación Unity/HDRP, apariencia, shaders y FPS sin ejecutar.

## R27 — Ocho perspectivas, modo foto y apariencias 3D

F5 recorre ocho posiciones de cámara, con obstrucciones y ancla de interacción
independiente. F6 abre fotografía local con pausa restaurable, encuadre,
inclinación y captura PNG sin interfaz. Selección de apariencias en ajustes;
OBJ local con MTL/texturas y preparación de FBX/OBJ/DAE/modelos importados
mediante herramientas Unity. GLB/glTF requieren un importador compatible.
No se descargan modelos ni se promete animación automática de OBJ.
Se conservan fuentes de linterna, geometrías y distancia de renderizado.
Documentación: `Assets/PoolroomsProcedural/Documentation/CAMARAS_APARIENCIAS_R27.md`.

Pasan la regresión acumulativa, 188 huellas y sintaxis de 232 scripts en cuatro
configuraciones, además de 17.899 aserciones de la comprobación final de cámara,
OBJ y contratos de fuentes. Tres pruebas PlayMode añadidas sin ejecutar.
Compilación Unity/HDRP, colisión nativa de cámara, fotos, importación y render de
modelos, menús, animación y FPS siguen pendientes de Editor/GPU.

R27: CI [39172162093](https://github.com/waltennecro/poolrooms/actions/runs/39172162093) correcta, incluidos catálogos, fuentes/licencias,
medios, paquete y artefacto. Assets: 851 archivos, 296261592 bytes.
Respaldo público descargado: 462733354 bytes, CRC válido y 1049
archivos coincidentes por SHA-256. Evidencia: `Verification/ci_camera_avatar_R27.json`.
Compilación Unity/HDRP, apariencias, animación, fotos, física y FPS sin ejecutar.

## R28 · Skins humanoides con un movimiento común

Se reemplaza la silueta neutral por importación de skins en una categoría Skins
propia. Humanoid conserva pesos/huesos; OBJ/Generic legibles reciben 21 huesos y
cuatro influencias anatómicas aproximadas. Marcha, carrera, saltos, caída, dash,
deslizamiento, agua, vuelo, postura sentada y seis emotes usan el mismo generador
de pose e IK; no cambia root motion ni cápsula. El ajuste es aproximado y permite
corregir pose, eje, orientación, hombros y caderas. No es rig universal perfecto.

OBJ directo con MTL; FBX/DAE/3DS/DXF/BLEND se preparan mediante el Editor, con
carpetas de usuario fuera del paquete reemplazable. GLB/glTF tiene un adaptador
opcional para Unity glTFast 6.7.1, sin instalarlo ni afirmar pruebas nativas.
Los materiales compatibles se conservan y las modificaciones usan copias; RT
dinámico solo con modo y hardware compatibles. Linterna y distancia conservadas.

Documentación: `SKINS_HUMANOIDES_R28.md`. Añadidas comprobaciones matemáticas
y pruebas PlayMode de bind pose/deformación; compilación Unity, apariencia,
importadores, selector Windows, RT y FPS pendientes de ejecutar en el Editor.

Comprobación local R28: suite completa correcta (188 fingerprints conservados),
238 scripts con sintaxis C# 9 en cinco configuraciones y 82 mapas PBR sin cambios.
Comprobaciones específicas de humanoides: pesos, IK, emotes y 10.000 pasos sin
asignaciones administradas en el generador de pose. Son aserciones del núcleo,
no pruebas nativas de Unity. Preflight nativo: Editor ausente; ninguna prueba
PlayMode ejecutada. Respaldo y CI se registran aparte con la revisión publicada.

R28 final: CI [39175805957](https://github.com/waltennecro/poolrooms/actions/runs/39175805957) correcta para `89447c8b7777391bda60b9b6219ec8e07111094f`,
incluidos núcleo completo, geometría protegida, catálogos acuáticos, texturas,
fuentes/licencias y artefacto. Assets: 859 archivos, 296283776
bytes. Respaldo público descargado: 462763192 bytes, CRC válido y
1060 archivos coincidentes por SHA-256. La revisión final
preserva además offsets de cadera/columna de Humanoid importados. Evidencia:
`Verification/ci_humanoid_skins_R28.json`. Unity, importadores, RT, deformación
y FPS siguen sin ejecutar.

## R29 · Generación detenida por un artículo inexistente

La traza del usuario confirmó `MallItemCatalog.Get(id)` durante AttachRoomExtras.
Dos objetos por receta del parque acuático usaban IDs 256–263 fuera del catálogo
0–255 y detenían la corrutina de generación del mundo completo. Se corrige la
selección a toallas/jabón (30/31), se añade validación de los artículos en el plan
y una regresión que falló antes y pasa con los 33.600 planes y sus modelos.
Las pruebas anteriores comprobaban geometría y no esa instanciación diferida.

El sol del terreno se restringe a su región; un árbitro de luces mantiene una sola
fuente direccional con sombras y restaura el sol de plantilla. El streamer precarga
las salas vecinas de los accesos comerciales y de terreno, aun cuando el foco esté
dentro de sus celdas reservadas. Se respetan explícitamente Exploración unificada
y las copias temporales del perfil; las escenas de catálogo optan por desactivarla.
Los fallos de carga ahora se registran y limpian la sala parcial. No se reduce
calidad/distancia ni se modifica la linterna. Documentación: GENERACION_Y_SOMBRAS_R29.md.
Cuatro regresiones PlayMode añadidas, sin ejecutar por falta del Editor Unity.

R29 final: CI [39179530261](https://github.com/waltennecro/poolrooms/actions/runs/39179530261) correcta para `93e15b733fdb9acc140ad0fcd8c4789574353645`.
Núcleo completo: 1810355734 aserciones, 188 geometrías protegidas y 241 scripts
en cinco configuraciones de sintaxis; además 33600 planes acuáticos con consulta
y modelos del catálogo y controles negativos de ID. Comprobados texturas/licencias
y empaquetado. Assets: 863 archivos, 296291529 bytes.
Descarga pública independiente: 462774512 bytes, CRC válido y los
1065 archivos versionados coinciden por SHA-256.
Evidencia: `Verification/ci_generation_R29.json`. Cuatro pruebas PlayMode nuevas
aún sin ejecutar; tampoco compilación Unity/HDRP, sombras visuales, física ni FPS.

## R30 · Auditoría ampliada antes de pruebas del usuario

La auditoría ejecutada resuelve 81.768 planes, 396 mallas completas de sala/conexión
y 1.395.345 colocaciones a través de catálogos y recursos reales. No aparecen
nuevas excepciones de datos. La búsqueda anterior reproducía 625 planes para elegir
una sala con radio 12; la nueva conserva la elección preparando uno. Incluye 384
comparaciones de orden, mapas finitos, accesos y extremos de coordenadas.

Se corrigen también consultas que usaban la posición del jugador en lugar del punto
solicitado, reservas comerciales vacías tratadas como soporte y simulación de objetos
sin zona cargada. Estos cambios de ejecución necesitan validación nativa: tres
pruebas PlayMode añadidas, todas las anteriores conservadas. El árbitro de sombras
filtra su lista por frame a luces direccionales. Linterna y distancia sin cambios.
Documento: `Assets/PoolroomsProcedural/Documentation/AUDITORIA_GENERACION_R30.md`.
Unity/HDRP, física, gráficos y FPS siguen sin ejecutar en la nube.

R30 final: CI [39197528937](https://github.com/waltennecro/poolrooms/actions/runs/39197528937) correcta para `4c03ecb082067ea525dc53fb9048f2caf7c58d21`;
incluye núcleo completo (1810355850 aserciones), auditoría ampliada, catálogos,
188 geometrías protegidas, 243 scripts en cinco configuraciones, recursos,
licencias y artefacto. Assets: 866 archivos, 296297218 bytes.
Descarga pública: 462787545 bytes, CRC válido; coinciden por SHA-256 los
1070 archivos versionados. Evidencia:
`Verification/ci_generation_audit_R30.json`. Los cambios de física/carga revisados
necesitan sus tres pruebas nuevas en Unity, aún no ejecutadas.

## R31 · Un 50 % más de variedad por bioma

Se amplían los ocho catálogos finitos de 14.888 a 22.332 recetas/envolventes (+50 %):
Poolrooms 170→255, solares 768→1.152, parque acuático 5.600→8.400, Orangerie/baños/
casas de lluvia 2.500→3.750 cada uno, casas Nivel94 800→1.200 y envolventes de
centros comerciales 50→75. Las cifras describen recetas de construcción, no modelos
descargados, salas simultáneas ni un aumento de extensión territorial.

Biblioteca/Patio/Costa pasan de seis a nueve programas; conservatorio/tránsito de
ocho a doce; fachadas City11 y módulos del infantil original de seis a nueve.
Interiores, supermercados y cubiertas infantiles de tres a cinco (+66,7 %, redondeo).
Se construyen nuevas cubiertas, plantas conectadas de casas, fachadas y equipamientos;
las selecciones de muebles respetan rutas y reutilizan los recursos con licencia.
La profundidad comercial de 128 m participa tanto en el edificio como en su reserva.

Exploración unificada activa estas variantes sin recrear la demo. Los IDs antiguos,
la linterna, la distancia y los límites de carga se conservan. Los mundos de prueba
pueden usar ExpandedVariety=false. No se añaden nuevos niveles en esta etapa.
Detalle: `Assets/PoolroomsProcedural/Documentation/VARIEDAD_BIOMAS_R31.md`.
Doce casos de física nativa añadidos, aún sin ejecutar; también pendientes Unity/HDRP,
aspecto y FPS. El aumento de geometría tiene coste y necesita medición en Unity.

Validación local R31: 188 geometrías protegidas, 245 scripts × cinco configuraciones,
1.810.347.597 aserciones acumulativas del núcleo; 42.904 planes de variedad y 800
selecciones adicionales de exploración, 606 muestras de malla y 1.818 sondeos de
espacio libre en 60 salas con las nuevas alturas acuáticas. La auditoría ampliada
resuelve 121.068 planes y 2.083.044 colocaciones; 50.400 planes acuáticos a seis
escalas y 82 mapas PBR verificados. Estos números no son pruebas nativas Unity.

Revisión R31: una prueba negativa reprodujo cubiertas heredadas en accesos regionales
convertidos por la topología después de planificar la sala. El constructor final
las descarta; la regresión pasa para los dos tipos de acceso. Variedad ampliada:
37.886.937 aserciones. Se repite el núcleo completo con esta corrección.

R31 final: CI [39201718996](https://github.com/waltennecro/poolrooms/actions/runs/39201718996) correcta para `e5b5a29a674bf88a32d8bb79159d04f98d4d9bda`;
incluye regresiones completas, variedad ampliada, auditoría de generación, recursos,
licencias, empaquetado y artefacto. Paquete Assets: 869 archivos,
296304732 bytes. Descarga pública independiente: 462803676 bytes,
CRC correcto y 1075 archivos versionados coincidentes
por SHA-256. Evidencia: `Verification/ci_variety_expansion_R31.json`.
Las doce regresiones nativas nuevas y Unity/HDRP, aspecto y FPS siguen pendientes.

## R32 · Aclaración: ambas ampliaciones

La respuesta pendiente del usuario pidió tanto más variedad como más salas por
bioma. Se conserva R31 y se amplía el campo de regiones con sqrt(1,5) por eje.
Una muestra emparejada de 6.502 regiones completas, en tres semillas y los 18
ambientes, mide ~1,497–1,503 veces las celdas del campo. No descuenta reservas.
Costa y climas solares/cerrados comparten la escala; no se impone un orden de niveles.

Los centros tienen un 50 % más de superficie bruta por planta y al menos un 50 %
más de tiendas accesibles (12–18 módulos de frente, 2–6 plantas). Reservas y
edificios comparten dimensiones. Nivel 94 amplía reservas y población: hay casas
en el terreno añadido. Se comprueban superposiciones, claves de sectores y accesos.
Los radios y presupuestos activos se conservan; las mallas y planes mayores tienen
un coste que necesita medición en Unity. La linterna no cambia.

Prueba específica: 384 pares de centros, 29.358→45.024 tiendas; doce casos de
Nivel94, 6.761→10.868 casas planificadas, 1.536 en las áreas nuevas; 156 muestras
de malla y 13.382.463 aserciones del núcleo. Son muestras acumuladas, no objetos
cargados a la vez ni modelos nuevos. Documento: EXTENSION_BIOMAS_R32.md.
Se amplía una prueba PlayMode existente, aún sin ejecutar; Unity/HDRP, PhysX,
aspecto y FPS siguen pendientes. No se añaden otros niveles en esta etapa.

R32 final: CI [39208189331](https://github.com/waltennecro/poolrooms/actions/runs/39208189331) correcta para `7986923f9a6927a387baf07b4c69bbe737d15f0a`;
incluye núcleo (1810356013 aserciones), 188 geometrías protegidas, 246 scripts
en cinco configuraciones, extensión regional, variedad conservada, auditoría,
recursos, licencias, empaquetado y artefacto. Assets: 871 archivos,
296308354 bytes. Descarga pública comprobada: 462814876 bytes,
CRC válido y 1079 archivos versionados coincidentes
por SHA-256. Evidencia: `Verification/ci_biome_extents_R32.json`.
Unity/HDRP, navegación/física real, aspecto y FPS siguen pendientes.

## R33 · Ampliación y cinco revisiones

Se integran diez ambientes nuevos con acceso caminable, 131.072 recetas por nivel
y 200.010 mallas de sala distintas comprobadas sin colores, luces ni objetos.
Hay 28 ambientes más los distritos físicos del centro comercial y Nivel 94.
La muestra de coordenadas [-64,64] de la semilla 481516 contiene 4.569 salas nuevas,
274.140 claves de objetos y 109.656 instancias de ocho modelos licenciados. Son
planes del mundo; el streaming carga un subconjunto, no todos simultáneamente.

Tres modelos nuevos en línea, seis LOD derivados, licencias y créditos auditados.
El catálogo comparte visuales con referencias contadas; el agua tiene alternativa
Lit transparente y ondas de contacto dispersivas; sonido ambiental con anclajes
y oclusión acotada, y controles propios en Audio. Se mantiene distancia de
renderizado, IDs, 188 geometrías protegidas y la linterna sin modificar.

Las revisiones corrigieron solape de silla/planta junto a mesas y sincronizaron
el generador de créditos. Pruebas de jugador usan SyncTransforms sin acceder al
interruptor obsoleto. Núcleo estricto: cero errores/advertencias; 252 scripts
comprobados sintácticamente en cinco configuraciones. Esto no compila APIs Unity.

Inventario: AMPLIACION_R33_ESTADO.json; evidencia: Verification/expansion_R33_local.json.
Pendientes expresos: 100.001 familias estructurales diferentes por nivel, 20.001
modelos descargados distintos si ese era el significado, y validación nativa
Unity/HDRP/PhysX/audio/apariencia/FPS. No se promete equivalencia AAA ni ausencia
absoluta de defectos. CI y publicación se anotarán tras verificar sus resultados.

R33, última pasada: una prueba negativa reprodujo un emisor de sonido colocado
en la esquina seca de una piscina redondeada. Se corrigió la proyección sobre
el contorno real; los casos rectangulares, redondeados y de cápsula pasan.
La prueba dirigida reúne 87.344 aserciones de agua/audio y contratos sintácticos.
CI pasa a Release para ejecutar las mismas comprobaciones con menor coste;
no se rebajan ni omiten catálogos, huellas, licencias, empaquetado o artefactos.

R33 final: código `8f9df12f3f895f41bfa0e3fd894cda90cb1d5da6`, CI [39318842965](https://github.com/waltennecro/poolrooms/actions/runs/39318842965) correcta
incluyendo compilación Release con cero errores/advertencias, catálogos completos,
200.010 huellas nuevas distintas, exploración, recursos, licencias, ZIP y artefacto
no vacío. 1831471808 aserciones acumuladas del núcleo; 188 geometrías protegidas.
Descarga pública real: 492801660 bytes, CRC válido y 1149
archivos versionados coincidentes por SHA-256. Assets: 921 archivos,
314287961 bytes, paquete reproducible de árbol limpio.
Evidencia: Verification/expansion_R33_ci.json. Cinco revisiones cloud completadas;
las APIs nativas, shaders, PhysX, audio real, aspecto, avisos y FPS siguen sin
validar en Unity. Los límites de familias/modelos y densidad están en el inventario.

R33, revisión geométrica adicional dentro de la quinta pasada: una prueba sobre
la decoración real reprodujo caras exteriores de jarrones orientadas hacia
dentro. Se corrigió su orientación y suavidad; ahora tienen pared interior,
borde abierto y fondo. Rayos desde fuera, dentro y arriba validan ambas caras
y el hueco. La plantilla inmutable evita recalcular el perfil por cada jarrón.
Las comprobaciones acumuladas y el respaldo de esta última corrección se repiten.

R33 final: código `634f967ae2329ae48e981035e153a1d474a272b3`, CI [39320778179](https://github.com/waltennecro/poolrooms/actions/runs/39320778179) correcta
incluyendo compilación Release con cero errores/advertencias, catálogos completos,
200.010 huellas nuevas distintas, exploración, recursos, licencias, ZIP y artefacto
no vacío. 1833854219 aserciones acumuladas del núcleo; 188 geometrías protegidas.
Descarga pública real: 492809130 bytes, CRC válido y 1150
archivos versionados coincidentes por SHA-256. Assets: 921 archivos,
314288391 bytes, paquete reproducible de árbol limpio.
Evidencia: Verification/expansion_R33_ci.json. Cinco revisiones cloud completadas;
las APIs nativas, shaders, PhysX, audio real, aspecto, avisos y FPS siguen sin
validar en Unity. Los límites de familias/modelos y densidad están en el inventario.

## R34 — refinado en lotes de 10.000

Los diez generadores de R33 incorporan biseles físicos, remates propios, plantas
sobre parterres, puentes de 1,80–2,16 m y lavadoras con cristal y tambor profundo.
Las 200.010 mismas recetas se compararon contra R33 en 20 lotes de 10.000 y
uno de 10: todas cambian de estructura y siguen siendo distintas. El registro
por lote conserva rangos, SHA-256 y coste; no son nuevas familias ni modelos.

Se conservaron claves previas, linterna, distancias y 188 huellas protegidas.
Compilación del núcleo estricta sin errores/advertencias; 253 scripts en cinco
configuraciones sintácticas; 1.310.720 planes, 1.320 muestras de ejes y 240 muestras
específicas de seguridad. Hay una prueba PlayMode de los nuevos puentes sin
ejecutar: Unity no está instalado. Apariencia, APIs, física y FPS siguen pendientes.

Inventario: REFINADO_R34_ESTADO.json; comparación reproducible y límites en
Assets/PoolroomsProcedural/Documentation/REFINADO_R34.md. CI y descarga pública
se anotarán cuando se verifiquen. Los pendientes cuantitativos de R33 se mantienen.

R34, revisión de interiores: se reprodujeron contra el primer código R34 un atril
oculto dentro del piano y un estante que invadía la aproximación al banco. Se
adelantó el atril, se retranquearon los estantes y se añadió comprobación geométrica
de ambas condiciones a las 240 muestras. Los 21 lotes, planes completos y
regresiones se repitieron sobre esta corrección; el primer CI fue cancelado para
verificar el código final en una ejecución nueva. Unity sigue sin ejecutarse.

R34 final: código `7429aecdd397c7ad2b9099006a456036ba8d6995`, [CI 39340633032](https://github.com/waltennecro/poolrooms/actions/runs/39340633032)
correcta, incluida la comparación reproducida de los 21 lotes contra el núcleo
inmutable de R33, auditorías, paquete y ambos artefactos no vacíos. ZIP de Assets
con 923 archivos y 314294407 bytes; descarga pública real de
492836992 bytes, CRC válido y 1157 archivos versionados
coincidentes por SHA-256. Evidencia: Verification/refinement_R34_ci.json.
La compilación y pruebas nativas de Unity siguen sin ejecutar; no se certifican
apariencia, avisos HDRP, PhysX o FPS.


## Horizonte · Alas

Nuevo trabajo sobre R34, sin cambiar nombres de versiones históricas: vuelo del
jugador normal con G, cuatro velocidades 3/8/18/36 m/s con H, ascenso Espacio y
descenso Ctrl. Conserva CharacterController, pasos de 18 cm y espera de streaming;
pausa/foto/chat mantienen hover sin inercia y no activan privilegios maestros.
Viajes, vehículos y cambios del vuelo maestro cancelan el vuelo normal.

Skins incorpora una sección de alas con 12 familias, 144 geometrías comprobadas
distintas, plegado/aletear, anclaje y tamaño, sombras y RT según la calidad
compatible. Solo se construye el diseño equipado (dos alas y arnés), sin luces
ni sondas nuevas. Se importaron 12 iconos externos de Lorc, CC BY 3.0, fijados
a revisión y con originales, hashes y créditos. Son iconos, no modelos 3D; las
144 alas son construcciones nuevas del proyecto, no 144 modelos descargados.

OBJ sigue disponible con su adaptación y materiales. Se añade un modo estático
optativo que evita ajustar huesos; no requiere rediseñar el importador existente.

Compilación estricta del núcleo sin errores ni avisos; suite completa con
1.847.091.627 aserciones del núcleo y contratos, 188 huellas preservadas y
258 scripts en cinco configuraciones sintácticas. Auditorías de fuentes y
créditos correctas. Tres pruebas nativas añadidas, aún sin ejecutar por faltar
Unity Editor. No se certifican APIs Unity, PhysX, aspecto, shaders, RT ni FPS.
Evidencia persistente: Verification/wings_horizonte.json.

Horizonte, revisión geométrica: una proyección de las mallas reales detectó
plumas separadas del soporte y membranas fragmentadas. El código inicial
reproduce el fallo «Feather roots float below the supporting spar: 0» ante la
nueva comprobación. La corrección une las raíces al larguero, cierra membranas,
diferencia los lóbulos de mariposa/libélula y calcula normales del volumen
afilado. Las alas siguen el pecho animado y vuelven al anclaje corporal para
OBJ estático. Se repitió la suite del núcleo y los contratos de código sobre
la corrección. El primer CI fue cancelado para validar una revisión nueva.

La sincronización pendiente de notas R34 quedó resuelta al publicar 4e19356;
se preservaron los commits locales anteriores sin forzar el historial.

Horizonte, compatibilidad del verificador histórico: el CI 39345076315 detectó
que copiar las pruebas actuales de alas al núcleo R33 inmutable producía CS0246
porque ese núcleo todavía no tenía esas clases. Se reprodujo localmente.
El proyecto histórico ahora declara explícitamente su alcance y rechaza
--wings con error; el proyecto actual conserva y ejecuta todas las pruebas de
alas. El Core archivado, las comparaciones de salas y sus 21 hashes revisados
no se modifican. La compilación histórica vuelve a pasar sin avisos, sigue
reproduciendo el fallo geométrico esperado y no puede informar alas verificadas.
Las pruebas actuales de alas y contratos también pasan. CI completo pendiente
de una ejecución nueva; no se presenta la ejecución fallida como aprobada.

Horizonte · Alas, cierre: código `8a0213701344fe8f8ab7defe2bd6e752446ac20a`,
[CI 39345741322](https://github.com/waltennecro/poolrooms/actions/runs/39345741322)
correcto en sus 38 pasos. Se regeneró la comparación de las 200.010 salas en
21 lotes y se mantuvieron los hashes revisados. El verificador histórico sigue
reproduciendo el fallo esperado y rechaza alas; el actual comprueba sus 144
geometrías y cuatro velocidades. Auditorías de licencias, recursos, paquete y
subidas completadas. Ambos artefactos tienen tamaño no nulo según la API; su
descarga desde Azure está bloqueada con Forbidden en este entorno y no se
presenta como descargada. La descarga pública de codeload sí se realizó y
verificó: 492.996.717 bytes, CRC correcto, 1.201 archivos versionados coincidentes
por SHA-256, incluidos los 946 de Assets. ZIP local de Assets: 314.402.192 bytes,
CRC y hashes de sus 946 archivos correctos.

El código de juego no cambió durante la corrección del verificador. La revisión
de Assets es idéntica a 32e755c; se conserva linterna y geometría protegida.
Detalle de entrega: ALAS_HORIZONTE_ESTADO.json y Verification/wings_horizonte_ci.json.
Siguen pendientes Unity Editor, APIs HDRP, tres pruebas PlayMode nuevas, apariencia
y rendimiento real. No se certifican estos aspectos por el resultado del núcleo.

## Resonancia · sonido inmersivo y música local

Implementación nueva sobre Horizonte, sin presentarla como recuperación exacta.
Se añaden modo tranquilo predeterminado e inquietante opcional sin voces,
susurros ni sustos; capas ambientales, contactos por superficies, movimiento,
caídas de objetos y mezcla con atenuación musical. Pool fijo de 19 voces incluyendo
la lluvia existente y dos de música; no hay emisores por objeto. Se conservan
linterna, IDs, alas y la arquitectura de generación.

Biblioteca local en Music junto al juego, MP3/OGG/WAV/AIFF, selección, búsqueda,
pausa, anterior/siguiente, aleatorio, repetición, fundido y reproducción automática
opcional. Escaneo limitado y separado del hilo principal; archivos dañados no
eliminan la canción anterior. No se suben canciones ni se incluyen en el ZIP.
48 clips PCM (23 reemplazos + 25 nuevos), dos grabaciones fuente CC0 de Kenney,
resto síntesis no vocal original. Licencias, fuentes y auditoría de hashes incluidas.
Detalles: `Documentation/Sonido_y_musica_local.md` dentro del paquete.

Comprobaciones del núcleo y recursos aprobadas; el cierre de evidencia se añadirá
tras empaquetado y CI. Unity Editor, decodificadores de plataformas, escucha y
rendimiento real siguen pendientes; dos pruebas PlayMode nuevas aún sin ejecutar.


Resonancia, entrega de código `0eddb5c384a7ae79f3d88adb94057c38696dae3c`:
se corrigieron la actualización de nombres al sustituir canciones con el mismo
recuento, los controles de volumen en contactos activos y el tiempo de pausa de
los pasos remotos. El Core conserva el mismo árbol `1b5f4eb6` entre las tres
revisiones de esta etapa. Compilación estricta sin avisos, regresión completa
con las 188 geometrías protegidas, contratos sintácticos actuales y recursos
aprobados. Los 48 PCM se regeneraron reproduciendo sus hashes.

ZIP de Assets: 981 archivos, 325.402.227 bytes, CRC y SHA-256 correctos.
Una descarga pública nueva del código final produjo 504.034.141 bytes;
sus 1.247 archivos versionados, incluidos los 981 de Assets, coinciden por SHA-256.
La reproducibilidad binaria del empaquetador se comprobó en b3780c7; el ZIP
posterior verifica el contenido final y no incluye cambios sin guardar.

El CI final [39360910225](https://github.com/waltennecro/poolrooms/actions/runs/39360910225)
sigue en curso al registrar esta entrega; no se informa como aprobado.
No se ha ejecutado Unity, los dos tests PlayMode nuevos, decodificación MP3/OGG/AIFF
en Windows, escucha ni medición del mezclador/FPS. Evidencia y enlaces exactos:
`Verification/resonancia_delivery.json` y `SONIDO_RESONANCIA_ESTADO.json`.


## Encuentro · exploración, interacción y recuerdos

Se integra automáticamente al mundo unificado: regiones más cercanas y conexiones
arquitectónicas entre ambientes, seis estilos de transición y orientaciones a pie
por el grafo real. El primer nivel 94 queda más cerca y sus distritos son compactos.
Inventario de 5 × 50 espacios, cantidades BigInteger sin tope fijo, barra inicialmente
oculta con 5 y controles separados para usar muebles (E), moverlos (T), guardarlos
(Z) y usar equipo (X). El inventario/cuaderno no se persisten por petición del usuario.

Rincones con soporte real, mobiliario con licencia existente, lectura, asientos,
plantas, limpieza, lámparas, armarios, piano, parejas, señales de tiza y fotografías.
144 combinaciones de recuerdos cálidos y siete invitaciones opcionales sin NPC.
Tres muebles y tres herramientas originales; cinco nuevos juegos PBR y ocho notas
originales; once iconos externos CC BY 3.0. Cero modelos 3D descargados nuevos;
no se cuentan colores o repeticiones como modelos distintos. Linterna y distancia
de renderizado conservadas. Más detalles y controles en
`Documentation/Encuentro_exploracion_e_inventario.md` dentro del paquete.

Auditoría inicial: compilación estricta 0 avisos/errores, regresión del núcleo y
188 geometrías protegidas aprobadas. La auditoría de esta implementación corrigió
identificadores de pilas equivalentes, soporte de objetos de mesa, límites físicos
de herramientas, normales de perfiles, clasificación del nivel 94 en el cuaderno,
reapertura de partidas y restauración de colisiones de asientos. Las comprobaciones
finales y entrega se registran en `Verification/encuentro_delivery.json`.
No se ha ejecutado Unity: cuatro pruebas PlayMode nuevas y revisión nativa pendientes.

El CI final de Resonancia 39360910225 ya terminó correctamente; esto no certifica
la compilación nativa de Unity ni el sonido/renderizado de Encuentro.

Cierre local de Encuentro: compilación estricta sin errores ni advertencias,
1.848.196.873 aserciones del núcleo, 188 huellas de geometría protegidas y análisis
sintáctico de 278 scripts en cinco configuraciones aprobados. También pasaron las
auditorías explícitas de generación y extensión de biomas, compilación del núcleo
histórico y rechazos esperados, y nueve verificadores de recursos/licencias.
El muestreo nuevo revisa 215 salas, 80 rincones amueblados, 206 notas apoyadas
en paredes, 153 transiciones distintas y 27 rutas a pie. No son tests nativos.

Entrega de código de Encuentro: `19caf55f628f210a8179193259de8cda97b602c5`.
ZIP de Assets completo: 1.035 archivos, 329.441.001 bytes, CRC y hashes correctos.
Se descargó nuevamente el repositorio público: ZIP de 508.120.298 bytes,
1.329 archivos versionados y los 1.035 de Assets coinciden por SHA-256.
El CI 39383935148 continúa en curso al registrar esta evidencia; no se declara
aprobado. Pruebas nativas de Unity, apariencia, audio y FPS siguen sin ejecutar.


## Agua cristalina

Mejora de las paletas claras de piscinas, costa, baños y parque acuático. Se reduce
la dispersión verdosa y la distorsión del fondo, manteniendo Fresnel, profundidad,
ondas y presupuestos existentes. Se añade «Cristalina» al final de la lista de
apariencias sin cambiar IDs; Custom conserva sus valores manuales. El material
ligero reduce opacidad y elimina mapas/emisión heredados que ocultaban el fondo.
Compilación estricta del núcleo sin errores/avisos y 97.095 aserciones de óptica
y contratos de integración aprobadas. Unity y revisión visual siguen pendientes.

Cierre de la óptica: 97.115 aserciones de óptica/contratos de integración,
regresión completa del núcleo con 1.848.203.400 aserciones y las 188 geometrías
protegidas aprobadas. Se añaden dos pruebas nativas de materiales aún sin ejecutar.
Detalles, límites y referencias oficiales en `Verification/crystal_water_delivery.json`.

Entrega de Agua cristalina: `628a3ec9bf25ff8d341d60858cb286d0e543a42c`.
Paquete completo de Assets: 1.037 archivos, 329.444.542 bytes, CRC y SHA-256
verificados. Una descarga pública nueva del código produjo 508.131.391 bytes;
los 1.335 archivos versionados coinciden por SHA-256, incluidos todos los Assets.
El CI 39455673680 sigue en ejecución. Los CI de Encuentro 39383935148 y
39383502982 ya terminaron correctamente. No se ha ejecutado Unity.


## Sala viva · inmersión y actividades

Doce tipos de puntos de actividad se añaden automáticamente sobre paredes
comprobadas, hasta cuatro por sala. Incluyen té y agua, libros, postales y notas,
dibujos visibles en el mundo, campanillas, caja musical, luz, paquetes y dos
rompecabezas nuevos. Se integran también en casas detalladas del nivel 94 y
sectores compatibles de centros comerciales. Se reutiliza una taza de porcelana
con licencia existente; no se añaden modelos 3D ni grabaciones descargados.

32 efectos originales CC0, 16 sonidos con dos variaciones, usan las cuatro voces
de detalle existentes. Ambiente tranquilo inicial, oclusión/distancia/reverberación
y control de detalles nuevos en Sonido. Agua cristalina, linterna y distancia de
renderizado conservadas. Cuaderno, regalos, dibujos y juegos siguen siendo locales
a la sesión; la persistencia de partida queda aplazada.

Muestreo real: 233 salas, 198 con actividades y 586 puntos apoyados; 12 tipos
presentes. Se comprueban mil mezclas resolubles de cada rompecabezas. No son
586 zonas nuevas ni modelos distintos. Tres pruebas nativas nuevas aún sin ejecutar.

Sala viva: compilación estricta sin errores/avisos, 1.848.304.592 aserciones del
núcleo y las 188 geometrías protegidas aprobadas. Los efectos nuevos y recursos
anteriores conservan hashes/licencias correctos. El verificador actual también
compila con el núcleo histórico y conserva sus rechazos esperados. Se añadió
migración para activar los detalles nuevos en ajustes antiguos, respetando
una desactivación explícita. Las tres pruebas nativas siguen sin ejecutar.

Entrega de código de Sala viva: `2736398b116b31bd4c0e9b3a5ac1581bfadbfc8d`.
Paquete acumulativo de Assets: 1.076 archivos, 330.368.617 bytes, CRC y SHA-256
verificados. Una descarga nueva del repositorio público produjo 509.075.014 bytes;
sus 1.384 archivos versionados, incluidos todos los Assets, coinciden por SHA-256.
CI 39530026448 en ejecución al registrar esta evidencia; no se declara aprobado.
Los CI de Agua cristalina 39455673680 y 39454870664 ya finalizaron correctamente.
Unity, las tres pruebas nuevas, escucha y FPS de Sala viva siguen pendientes.


## Diez regiones especiales — ampliación del mundo continuo

Se añaden Observatorio solar, Mercado cubierto, Taller textil, Galería postal,
Marina interior, Galería de relojes, Taller de papel, Pabellón de ajedrez, Salón de
radio y Galería de espejos. Diez programas arquitectónicos originales, materiales
PBR y mobiliario con licencias existentes, estaciones de Sala viva, notas propias
y accesos caminables. Catálogo: 38 biomas + nivel 94 y centro comercial completo
como destinos independientes = 40 ambientes. Ningún modelo externo nuevo.

Los 56 candidatos originales por bloque completo se conservan; ocho huecos antes
ponderados albergan los especiales, cubriendo sus diez tipos entre dos bloques
horizontales vecinos. No se reduce la distancia de renderizado ni se añaden soles,
cámaras de espejo o voces de sonido. Los espejos aprovechan SSR/RT y probes existentes;
las barcas, relojes, ajedrez y otros detalles estructurales son decorativos y las
actividades jugables se indican en la documentación. Sin persistencia añadida.

Compilación estricta: cero avisos y errores. Regresión completa: 1.862.485.205
aserciones del núcleo, 188 geometrías protegidas y sintaxis de 286 scripts en cinco
configuraciones. 300 salas nuevas en tres escalas y dos anchuras, 1.103 colocaciones
de actividades; 210 rutas testigo y 368 pares físicos entre biomas en cinco semillas.
El muestreo de coordenadas contiene 6.920 salas de los veinte programas del reposo
y especiales, 417.208 claves de objetos y 168.088 instancias de diez modelos con
licencia existentes; no son objetos simultáneos ni modelos descargados nuevos.

Las comprobaciones de extensión, modelos, mapas y audio reutilizados aprobaron.
Se corrigió también que la búsqueda de un modo especial dedicado se confundiera
con un bioma tranquilo desactivado. El CI anterior de Sala viva 39530026448 ya
finalizó correctamente. Dos pruebas nativas nuevas; Unity, HDRP, aspecto, sonido
y FPS sin ejecutar. Evidencia en `Verification/special_regions_delivery.json`.

Respaldo funcional: `db65e160d00a07add367e36be53b156cfe873e0e`. ZIP acumulativo de Assets:
1,080 archivos, 330,379,789 bytes, CRC y SHA-256 verificados.
Descarga pública nueva: 509,099,076 bytes, los 1,395 archivos
versionados y 1,080 Assets coinciden por SHA-256 con el código publicado.
CI 39537031131 en ejecución al registrar la entrega; no se declara aprobado.


## Regiones extensas — ampliación de todos los ambientes

Los 38 biomas reciben campo regional procedural extenso: multiplicador lineal 8
por defecto, aproximadamente 64× el área nominal y unos 1.058 m entre centros con
la celda habitual. La escala de perfil se configura entre 4 y 12 y la exploración
unificada respeta su desactivación. No se escala al jugador ni se modifica el radio
de renderizado. El núcleo compacto conserva sus comparaciones históricas; la
opción se activa por defecto en los perfiles de Unity y se prueba por separado.

Cinco sectores por coordenadas; los veinte programas de galerías y especiales
abren salones de hasta 4×4 chunks con techos alineados, uniones anchas y piezas
estructurales propias. Los demás biomas mantienen sus arquitecturas específicas
dentro de regiones mayores. Los centros comerciales crecen a 24–28 módulos,
440–504 × 180–228 m y 2–6 plantas; las reservas coinciden con el edificio generado
y permanecen fuera del inicio y de otros edificios/terreno. El nivel 94 amplía
su terreno poblado conservando las viviendas cercanas, caminos y pasto actuales.

Búsqueda por centros de regiones y rutas A* limitadas a 16.384 coordenadas; las
avenidas siguen conexiones recíprocas reales. El streaming describe sólo el plan
seleccionado y recorta sus cachés, incluyendo las nuevas conexiones de salón.
Se conserva el BFS compacto de 4.096 coordenadas. Sin nuevos modelos descargados,
mapas, cámaras de reflexión ni persistencia de partida.

Comprobación: 114 destinos y mallas de habitación, 47 rutas/5.034 pasos y 60
uniones de salón de los veinte programas en tres escalas. Regresión completa:
1.870.132.520 aserciones del núcleo, 188 geometrías protegidas y sintaxis de 290
scripts en cinco configuraciones. Compilación estricta sin avisos ni errores.
Cuatro pruebas nativas nuevas, incluyendo carga real de conectores sin malla y
sellado de límites de streaming; expectativa de tamaño comercial anterior actualizada;
Unity, PhysX, shaders, imágenes, escucha y FPS siguen pendientes.

El CI de las diez regiones especiales 39537031131 finalizó correctamente.

Respaldo funcional de regiones extensas: `b33bfb050d0002f44c9a6a6414d63f7a7a1f6ee6`.
ZIP acumulativo: 1,085 Assets, 330,391,348 bytes, CRC/SHA-256
verificados. Descarga pública nueva: 509,124,854 bytes; los 1,406
archivos versionados, incluidos 1,085 Assets, coinciden por SHA-256
individual y CRC. El verificador de descarga trabaja por streaming y no necesita
almacenar otro ZIP. CI 39540890186 en ejecución al registrar la entrega; las
cuatro pruebas nuevas, Unity, PhysX, shaders, imágenes y FPS siguen pendientes.


## Centro comercial renovado

Las dos rutas de generación comercial activan interiores refinados independientes
del tamaño regional, conservando las 2–6 plantas, reservas, entradas a pie y radios
de vista. Mobiliario según superficie mediante paquetes locales acotados, con
96 conjuntos como máximo por tienda, pasos y bahías reservados. Seis programas
de techo, revestimientos y luminarias visibles por local. Catorce modelos fuente
nuevos, 28 derivados LOD y veinte acabados externos CC0/CC BY con procedencia,
licencias y originales fijados; 161 mapas. Son modelos/derivados diferenciados,
no miles de modelos descargados. Parte del catálogo paramétrico sigue en uso.

Sol comercial reutilizando la luz regional existente, con desactivación del
foco de tragaluz alternativo y reserva de presupuesto. Iluminación distribuida
por planta; reflejos del sistema SSR/RT/sondas actual, sin más cámaras ni sondas.
Mipmaps en streaming y materiales compartidos; selección de mercancía girada
y descarte temprano de sectores fuera de alcance. Objetos recogibles, lámparas,
asientos, fruta usable, menús cálidos y actividad en cajas registradoras. No se
añade persistencia, ni se cambia la linterna.

Comprobación: seis planos compactos/extensos describen 860 tiendas y 25.258
conjuntos de mobiliario; 136 sectores construidos contienen 2.850 instancias de
21 IDs fuente (incluidos anteriores), 26.295 artículos del catálogo y 4.874
descripciones de luz local. No son objetos cargados simultáneamente ni modelos
distintos. Se conservan 188 geometrías protegidas. Compilación de núcleo estricta
sin avisos/errores, regresión de 1.887.360.876 aserciones y sintaxis C#9 de 296
scripts en cinco configuraciones. Son comprobaciones del núcleo, no pruebas Unity.

34 imágenes de referencia y seis planos reales en
`Documentation/References/Mall/README.md`; ninguna captura Unity. Tres pruebas
PlayMode nuevas, sin ejecutar. APIs Unity/HDRP, shaders, aspecto, interacción
física y FPS quedan pendientes del editor. La animación de puerta del frigorífico
no se importa: su exposición es estática. El CI anterior 39540890186 terminó
correctamente; este avance guarda evidencias en `Verification/mall_retail_*`.

Guía: `Assets/PoolroomsProcedural/Documentation/Centro_comercial_renovado.md`.
