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
