# R9: helicóptero y helipuerto de City11

## Integración

Cada aeródromo válido tiene una plataforma lateral con círculo y H, separada de la pista. Su suelo sigue siendo transitable y usa las reservas y conexiones de R8. `enableAircraft` controla ambos tipos aéreos. Modo maestro → **Visitar helipuerto de City11** localiza un emplazamiento real. No se añade una escena de prueba aislada ni se sustituyen las entradas anteriores.

**E** entra/sale al aterrizar y detenerse; **W/S** avanza/retrocede; **A/D** gira; **Espacio/Ctrl** sube/baja. Ctrl también frena el avance. El rotor adquiere régimen gradualmente; después, soltar las teclas de altura activa asistencia de estacionario. Límite suave de 250 m y velocidad nominal de 18 m/s. No hay armas, daño, combustible ni objetivos. Cámara exterior, cápsulas para patines/cola y volumen de cabina; las aspas son visuales.

Avión y helicóptero comparten el bloqueo por menús/carga, origen flotante, cámara con retracción y salida con suelo/espacio suficiente. La fábrica y geometría de contactos del avión R8 se conservan exactamente. Se usa la misma iluminación exterior regional al pilotar cualquiera de los dos.

## Procedencia y materiales

Una nueva fuente distinta: **MH-6 Little Bird de AnirudhRao**, licencia **CC BY 4.0**. Se adapta la geometría derivada publicada en `jdubba1/warbird`, revisión `251d6d85cbe5eedbebf7625a8df7fd2430f8d1a1`. Sus créditos identifican autor, licencia y modelo original. La copia original de Sketchfab/Objaverse no pudo verificarse desde este entorno: sus dominios respondieron 403. Esta etapa utiliza el derivado disponible, conserva sus bytes y atribución y no lo presenta como el GLB original ni como CC0. Fuentes y hashes en `Sources/Helicopter`; licencia completa y avisos dentro de Resources.

No se reutiliza código del juego de origen ni su sombreado sin iluminación. La adaptación añade normales con aristas, UV métricas, nueve mapas originales PBR de pintura/metal/goma y materiales Lit. El derivado carece de UV/texturas originales: estos acabados son nuevos y periódicos, no una recuperación del texturizado del autor. El cristal usa transparencia y rugosidad física propias. No se certifica una apariencia AAA.

De 40.642 triángulos de origen se eliminan 41 degenerados; se añaden tres marcadores diminutos para conservar los ejes exactos tras importar. Se retiran del render en runtime. Niveles: **40.604 / 10.771 / 5.998 triángulos**. Los dos reducidos son derivados del mismo modelo; Rendimiento empieza en el intermedio. Los ejes usan marcadores nativos, evitando girar los cinco brazos del rotor alrededor del centro incorrecto de su caja.

## Cifras y comprobaciones

Catálogo acumulado: siete modelos externos distintos — cinco CC0, un Apache-2.0 y un derivado CC BY 4.0. Tres clases de vehículo: coche, avión y helicóptero. Una muestra determinista de 324 celdas City11 con aeródromos activos contiene 1.212 edificios procedurales, 4.846 espacios de planta, 3.636 objetos importados y 39 puntos de vehículo: **37 del mismo coche, un avión y un helicóptero**. No son 39 modelos distintos ni objetos cargados simultáneamente. Los edificios siguen siendo procedurales; falta ampliar arquitectura/modelos y decoración.

Núcleo compilado: 16.935.773 aserciones, huellas de 108 SoftPlay y 80 muestras anteriores conservadas, sintaxis de 188 scripts × cuatro configuraciones; vuelo estacionario, ascenso, frenada, techo y entradas inválidas en cinco pasos temporales. Auditorías de geometría/LOD/marcadores, hashes y licencia, 36 mapas PBR y comprobaciones de UV correctas. Linterna original y Preparacion_V48 intactas. Nuevos tests PlayMode de importación, materiales y piezas incluidos, **sin ejecutar**.

Pendientes en Unity 6.3/HDRP 17.3: compilación completa de APIs, importación, rotación de piezas, aterrizaje, colisiones, cámara, shaders/RT, apariencia, memoria y FPS. El vuelo asistido es código de exploración; sus pruebas matemáticas no validan la simulación PhysX.
