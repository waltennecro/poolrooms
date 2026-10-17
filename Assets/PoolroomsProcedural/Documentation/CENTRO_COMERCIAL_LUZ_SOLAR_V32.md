# v32 — Luz solar y tiendas del centro comercial

Actualización acumulativa sobre v31. Mejora la iluminación del centro comercial completo de 2 a 6 pisos, integrado en el mundo conectado. Conserva las tiendas, objetos interactuables, escaleras, biomas, Nivel 94, Poolrooms solares y deslizamiento anteriores.

## Qué cambia

- Los tragaluces siguen siendo aperturas reales con vidrio. La potencia de sus luces solares se ajusta a la altura del edificio; la dirección de los haces evita el eje de los puentes para alcanzar el atrio de la planta baja. La geometría y las colisiones no cambian.
- Dentro del centro comercial, el tragaluz más cercano conserva una plaza del presupuesto existente de luces y sombras. Antes las lámparas locales podían consumir todas las sombras y dejar apagado el sol. Se amplía la distancia de conservación de sombras de las fuentes solares en HDRP.
- Se reutilizan las dos lámparas de cada sector de galería para representar el relleno de luz del cielo. Ahora están distribuidas por los pasillos de cada piso, en vez de depender de dos fuentes en el centro del edificio. Este relleno es una aproximación controlada; la iluminación indirecta HDRP existente sigue dependiendo de sus ajustes y soporte.
- La iluminación de las tiendas se decide por semilla y estado del ala: aproximadamente 80% de tiendas iluminadas en alas mantenidas, 50% en alas parcialmente encendidas y 10% en alas abandonadas. Los porcentajes son probabilidades por tienda, no una cuota exacta por centro. Las demás quedan tenues. Las decisiones se conservan al regresar.
- Cafeterías y comida usan tonos cálidos de 3200 K; electrónica y farmacias, 4800 K; las otras tiendas, 4100 K. Cine y arcade mantienen una iluminación menor. Las fuentes principales cubren más superficie y las tiendas iluminadas usan 18,000 lúmenes, o 26,000 en el hipermercado; todo respeta el multiplicador de iluminación guardado.
- Las luces locales se seleccionan por el piso del jugador. Las del piso contiguo ya no ocupan plazas aunque su techo esté más cerca de sus pies. Al caminar por escaleras cambia el piso de referencia cerca del siguiente descansillo.
- Las fuentes locales sin sombras limitan su alcance para reducir filtraciones entre pisos; las que obtienen sombras recuperan su alcance completo. Con cero sombras se apagan los haces solares directos para impedir que atraviesen las losas, pero permanecen el relleno y las luces locales.
- No aumenta el límite de luces activas, de sombras ni de sondas de reflexión. Cada sector mantiene como máximo cinco luces y se eliminan las antiguas fuentes de relleno del centro del edificio. No se cambia la exposición, el código de linterna, los efectos de cámara o el movimiento del jugador.

## Instalación y uso

Fuera de Play, actualiza una sola carpeta Assets/PoolroomsProcedural con la del ZIP v32. Conserva tus .meta, escenas y perfiles. El ZIP ya tiene el nivel correcto para Assets: no importes Verification ni el proyecto completo de comprobación.

Usa tu escena y Explorer existentes. Al entrar al centro comercial o al usar su TP se generan las luces nuevas automáticamente; no necesitas crear una demo nueva ni activar un menú de versión en Tools. Se aplica a los centros completos mejorados v20/v21/v22 y a los centros conectados. Las antiguas salas pequeñas de supermercado conservan su comportamiento.

Para ver los haces solares, el presupuesto de sombras debe ser de al menos una luz. En Pausa > Ajustes > Gráficos, conserva sombras activadas; el sistema reserva una plaza disponible para el tragaluz. La integración HDRP y la configuración de sombras del HDRP Asset siguen siendo necesarias. Funciona con rasterización, sin exigir ray tracing. El aspecto depende también de los ajustes guardados de exposición, iluminación y calidad.

## Comprobaciones ejecutadas

- Núcleo .NET 8: compilación con cero errores y advertencias.
- 26,689 aserciones nuevas en 60 centros comerciales de 2 a 6 pisos, tres formas de techo y 15 categorías de tiendas. Se comprobaron 309 fuentes solares bajo vidrio y 309 rayos centrales libres hasta el atrio de planta baja, 675 sectores con luces locales por piso, y 2,961 tiendas iluminadas frente a 1,599 tenues. Son muestras de geometría y datos, no imágenes renderizadas ni mediciones fotométricas.
- Regresiones del centro completo v20 y arquitectura v21: 300 planes por comprobación; accesos, escaleras, geometría, catálogo de 256 objetos y sus identificadores preservados. La v21 verifica en detalle las 12 combinaciones de atrio/techo.
- Sintaxis C#9: 142 scripts en cuatro configuraciones de símbolos, seis asmdef válidos, 9,980 referencias explícitas a enums y cero errores.
- MallDaylightWorldTests incluido para PlayMode: reserva de sombra solar, límites de luces, selección del piso, alcance con/sin sombras y vidrio que no bloquea la luz. No ejecutado en la nube.
- Integridad CRC del ZIP y comparación byte a byte de su contenido con los archivos fuente. Linterna, cámara, Explorer y deslizamiento idénticos a v31.

## Pendiente en Unity

La nube no tiene Unity Editor: falta compilar contra las API reales de Unity 6.3/HDRP 17.3 y validar shaders, iluminación, PhysX y FPS. Ejecuta MallDaylightWorldTests junto a ConnectedMallWorldTests en PlayMode. Recorre el atrio de planta baja, los pisos superiores y tiendas mantenidas/tenues, y revisa con sombras 0, 1 y 4. Mira también el centro desde las entradas y en vuelo; la comprobación del núcleo no valida sombras o exposición renderizadas. Mantén el presupuesto de luces moderado en tu i3 N305 sin GPU dedicada y compara FPS dentro de Unity.
