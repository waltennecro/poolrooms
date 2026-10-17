# v39 — Áreas infantiles liminales multinivel

Nuevo bioma **Áreas infantiles multinivel / SoftPlay**, independiente de Juegos interiores / IndoorPlay. Se integra por regiones aleatorias en la exploración unificada de la demo habitual; conserva Poolrooms, juegos anteriores, supermercados pequeños, centros comerciales, Nivel 94 y los demás interiores.

## Instalación y acceso

1. Fuera de Play, copia la carpeta `PoolroomsProcedural` del ZIP dentro de `Assets`, reemplazando sus archivos y conservando tus `.meta`. El ZIP es acumulativo: no importes `Verification`, `bin`, `obj` o proyectos `.csproj` dentro de Assets.
2. Usa tu escena habitual. **Crear demo** también sirve si estás empezando desde cero; no necesitas una demo diferente para este bioma.
3. Pulsa Play y explora. El perfil nuevo activa `enableSoftPlay` y una probabilidad regional del 18 % en Expanded. No es una secuencia de niveles ni significa que exactamente el 18 % de las salas cercanas sean infantiles.
4. Para verlo directamente: Y → `/unlock mastermode` → menú maestro → **Visitar Áreas infantiles multinivel**. También está **Tools > Poolrooms > Áreas infantiles v39 > Visitar áreas multinivel (Play)**. Estas opciones visitan el bioma del mismo mundo.

En perfiles personalizados que no usen la exploración unificada, **Tools > Poolrooms > Áreas infantiles v39 > Integrar al mundo infinito**, fuera de Play, activa Expanded y el bioma. Puedes desactivarlo o cambiar su frecuencia en el perfil. `Solo juegos interiores procedurales v13` sigue correspondiendo al bioma anterior.

## Recorridos y ambiente

Las salas ocupan la celda completa: 36 × 36 m con el tamaño predeterminado, ajustables mediante el tamaño de celda existente de 28–60 m. Entre salas infantiles contiguas hay aberturas de aproximadamente 12–14 m y hasta 7 m de alto, con un umbral continuo en lugar de otro pasillo largo. Los bordes del bioma enlazan con la arquitectura de las otras zonas. Se respeta el grafo de puertas: no aparecen entradas hacia chunks que todavía no estén listos.

Cada sala contiene dos estructuras principales acolchadas con **3–4 niveles físicos**, contando la planta baja; las salas mayores pueden añadir una tercera estructura de 2–3 niveles. Los pisos están separados 3,2 m y se alcanzan mediante escaleras de dos tramos con descansos. Hay puentes elevados, túneles realmente huecos, redes, barandillas, toboganes, una piscina de pelotas y una pequeña zona de descanso. Las dimensiones, posiciones, alturas, puentes, pelotas, cubierta y luces varían por semilla. Las conexiones abiertas dan continuidad a conjuntos de salas; no se impone un orden de ambientes.

Las redes se dibujan como mallas finas agrupadas. Sus barreras físicas se generan aparte, dejando libres las bocas de túneles, accesos y descansos. El hueco de la escalera tiene protecciones con salida lateral al piso superior. Los recorridos principales en planta baja quedan despejados; el tobogán pasa por encima con espacio para caminar debajo.

La iluminación, el cielo y los materiales son propios del bioma. Hay ambientes claros, cálidos y de blanco frío, luz dentro de las torres y apoyo difuso moderado. Se incluyen 15 mapas PBR originales de 256 × 256 para espuma, pintura y plástico rojo, amarillo y azul; se añaden únicamente los materiales nuevos a una biblioteca existente. Las paredes no reutilizan el azulejo de las Poolrooms. Las referencias se usaron para diseñar arquitectura, no como texturas copiadas del juego.

## Interacción y rendimiento

**E** toma pelotas blandas, bloques de espuma o aros; **Q** los suelta. No hay inventario ni valor asociado. Los ocho objetos agarrables de cada sala tienen recursos propios para sobrevivir a la descarga del chunk mientras los llevas. Las demás pelotas son detalle visual agrupado, sin decenas de cuerpos físicos simulándose. La física de un juguete se activa al soltarlo.

Camina sobre el tobogán para deslizarte; puedes saltar o apartarte. El desplazamiento pasa por el CharacterController, respeta pausa, menús y vuelo, y no teletransporta ni controla la cámara. Se conserva el deslizamiento C del jugador, el doble salto, la linterna y los efectos de cámara existentes. Los pasos y el roce usan el sonido blando existente. No se añaden muerte, enemigos, desafíos, caras ni anomalías.

El bioma usa la carga por distancia y los límites de luces, sombras y reflejos del mundo. No crea sondas de reflexión por sala. Con 12 segmentos de calidad, se dibujan 48 pelotas de detalle; con mayor calidad, 96 y redes más finas. Las colisiones de las estructuras no cambian al subir el detalle visual. Para tu i3 N305, comienza con Rendimiento y pocas salas visibles; no se ha medido todavía su coste en tu equipo.

## Comprobación

Compilación del núcleo .NET 8: cero errores y avisos. Pruebas de geometría sobre 108 salas, tamaños 28/36/60 m y puertas 3/4,4/6 m: soporte de escaleras y descansos, altura libre, túneles abiertos, puente continuo, circulación de planta baja, calidad visual sin cambiar colisiones, objetos y transiciones a salas solares. También se comprueba el bioma dentro de un mundo con centros comerciales, terreno, arquitectura solar y generación autónoma activados. Los resultados exactos están en `VERIFICACION_V39.txt`.

La sintaxis C# 9 pasó para 164 scripts en cuatro configuraciones; los seis asmdef son válidos. Las pruebas anteriores de generación, Poolrooms comunes, iluminación, centros comerciales conectados, Nivel 94, audio y cámara pasaron. La huella de 144 muestras de la gramática antigua coincide con v38.

**Este entorno no tiene Unity Editor.** La compilación con Unity/HDRP, importación de shaders y materiales, PhysX, aspecto y FPS siguen pendientes. Se incluyen tres pruebas nativas en `Tests/PlayMode/SoftPlayWorldTests.cs` para caminar las escaleras con CharacterController, barrer el túnel con una cápsula y agarrar/soltar juguetes; están incluidas, pero no ejecutadas aquí. En Unity, comprueba también la entrada desde otro bioma, los pisos superiores, la pausa sobre un tobogán y regresar con un objeto sostenido. Prueba el TP en la demo habitual y revisa la consola antes de valorar el aspecto o medir FPS.
