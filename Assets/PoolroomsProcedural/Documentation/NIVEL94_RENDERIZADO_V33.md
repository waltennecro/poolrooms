# v33 — Nivel 94 poblado, pasto y ajustes de renderizado

Entrega acumulativa sobre v32. Conserva el mundo conectado, centros comerciales con luz solar, Poolrooms originales/solares, objetos interactuables, Nivel 94, doble salto, deslizamiento, audio, cámara y linterna.

## Casas y terreno

Los fragmentos del Nivel 94 pasan de 128 a 64 m y la probabilidad inicial de poblarlos sube de 57% a 86%. Los caminos, el despeje de las entradas y los márgenes siguen filtrando solares para evitar invasiones. Los desplazamientos de las casas dentro del fragmento son menores, de forma que aparecen más próximas y no se superponen. En 12 distritos de muestra hubo 1,385–2,602 casas descritas por semilla y una distancia media a la casa vecina de 57.72 m.

Se conservan distritos extensos de 2.56–4.096 km por lado, además de la reserva física existente del mundo conectado. El número total describe el distrito: no se instancian miles de casas al mismo tiempo. Solo se cargan los fragmentos cercanos, y los interiores con muebles, colisiones y objetos agarrables siguen limitados al entorno inmediato. Las casas lejanas mantienen su versión de menor detalle.

Se actualizaron posiciones, suelos, caminos, uniones de fragmentos y detección de soporte a la medida nueva. El horizonte sigue usando una malla gruesa: se insertan los bordes exactos de los fragmentos cercanos para evitar huecos o solapamientos. Los claros de las casas pueden cruzar una unión; ambos lados consultan los mismos solares vecinos, conservando continuidad.

La distribución del Nivel 94 cambia en esta versión, aunque conserva la misma semilla del mundo. Sus decisiones nuevas siguen siendo deterministas al regresar.

## Pasto del Nivel 94

En Pausa > Ajustes > Gráficos > Renderizado puedes elegir:

- **Desactivado:** no genera mallas de pasto y elimina las que estaban cargadas. Conserva el terreno, su textura verde, sus colisiones, caminos y casas.
- **Económico, predeterminado:** grupos de tres hojas triangulares simples, con una sola superficie/material por malla. Máximo nueve parches de 16 m, hasta 300 triángulos cada uno: 2,700 triángulos añadidos como máximo. Cubre aproximadamente 24 m por lado del centro de su ventana; la posición del jugador dentro del parche hace variar el alcance efectivo. Sin emisión ni recepción de sombras propias del pasto.
- **Alto:** más densidad, grupos de cinco hojas curvas y puntas más claras. Máximo 25 parches de 16 m, hasta 8,640 triángulos cada uno: 216,000 triángulos como límite de detalle. La ventana llega aproximadamente a 40 m por lado del centro. Recibe sombras del escenario, pero no proyecta sombras de hojas.

Los parches se generan de cerca hacia fuera, en tareas del núcleo, y se sube como máximo una malla nueva por fotograma. No hay un GameObject por hoja, colliders, luces, sondas o ray tracing del pasto. Se usan materiales PBR opacos de doble cara: se evita transparencia con capas superpuestas. No se regenera el pasto por fotograma mientras estás quieto. El modo Alto es más costoso; no contiene animación de viento.

El pasto respeta caminos, entradas, casas, porches, límites de la región y pendientes pronunciadas. Sus bases usan la interpolación de los triángulos del terreno cercano, para reducir flotación. Cambiar calidad sustituye las mallas sin tocar soporte físico ni teletransportar al jugador. Salir de la región, cambiar de distrito o volar a mucha altura libera los parches. Las coordenadas locales siguen el origen flotante compartido.

## Controles trasladados a Gráficos

Los controles de distancia y visibilidad ya no están dentro del panel de trucos. Ahora pertenecen a las preferencias guardadas del jugador y funcionan sin desbloquear /unlock mastermode:

- Distancia de salas, desde nueve hasta una ventana de 625 salas, o automática según escena. La descarga conserva el anillo de margen existente.
- Distancia del Nivel 94 en fragmentos de 64 m, con horizonte de menor detalle.
- Distancia de sectores de tiendas y cantidad de pisos vecinos visibles.
- Pasto: desactivado, económico o alto.
- Visibilidad en zonas oscuras y brillo del relleno suave. Conserva la exposición y la linterna. La exposición HDRP continúa como ajuste independiente dentro de Gráficos.

Las preferencias se migran sin borrar los ajustes anteriores de cámara, sonido, movimiento, exposición o agua. Bloquear el modo maestro o restablecer los trucos conserva las preferencias gráficas. Restablecer Gráficos devuelve las distancias automáticas, desactiva el relleno y elige pasto Económico. El relleno es opcional y está apagado por defecto.

**F7** abre Pausa > Ajustes > Gráficos directamente en Renderizado. Funciona desde el juego, pausa, chat o panel maestro; cierra la interfaz anterior, borra búsquedas que podrían ocultar opciones y conserva el sistema normal de pausa/cursor. Esc y Continuar mantienen el comportamiento anterior. Están conectadas las entradas Legacy Input Manager e Input System. Un controlador externo puede llamar FeedRenderingShortcut(bool pressed) con la pulsación del fotograma.

## Instalación y ajustes sugeridos

Fuera de Play, actualiza una sola Assets/PoolroomsProcedural con la carpeta del ZIP v33. Conserva escenas, perfiles y .meta. Usa tu escena habitual: no necesitas crear otra demo ni activar una herramienta de versión. El componente de pasto se añade automáticamente al sistema del Nivel 94 en Play.

Para el i3 N305 con 8 GB y GPU integrada, empieza con Rendimiento, pasto Económico, salas 1–2 y distancia del Nivel 94 2. Si necesitas reducir más el coste, desactiva el pasto. Alto y radios grandes quedan disponibles para equipos con más capacidad. Son recomendaciones de presupuesto; no se han medido FPS en tu PC ni se garantiza una frecuencia concreta.

## Verificación

Núcleo .NET 8 compilado con cero errores y advertencias. Pasaron 36,533,635 aserciones en las comprobaciones de terreno/renderizado, terreno anterior, mundo conectado, mobiliario, movimiento/cámara/ajustes y políticas de visibilidad:

- 12 distritos, 300 pares de parches de pasto, límites de geometría, determinismo, casas sin superposición, altura real de los triángulos, exclusión de caminos/porches, opción apagada, guardado y migración.
- 48 distritos, 192 casas amuebladas y 2,722 objetos con soporte; bordes entre LOD, normales, pisos, entradas y escaleras conservados.
- 171 distritos conectados deterministas, 48 umbrales transitables, 17,895 casas con soporte y 1,047,120 aristas compartidas recíprocas en las muestras.
- Mobiliario y movimiento: deslizamiento, doble salto, preferencias y comportamiento de cámara anteriores preservados.
- Sintaxis C#9: 145 scripts en cuatro configuraciones, seis asmdef válidos, 10,096 referencias explícitas a enums y cero errores.

TerrainRenderingSettingsTests incluye pruebas PlayMode para F7, guardado, distancias y relleno sin trucos, cambios de calidad de pasto, eliminación de geometría y conservación del soporte físico. MasterNavigationTests se adapta a que los gráficos persistan después de bloquear los trucos. Estas pruebas están incluidas, no ejecutadas.

**Pendiente en Unity 6.3/HDRP 17.3:** compilación completa contra las API, materiales/shaders, apariencia, colisiones reales y FPS. La nube no tiene Unity Editor. Ejecuta TerrainRenderingSettingsTests, TerrainRegionTests y ConnectedTerrainWorldTests. Recorre uniones de terreno, patios y puertas; cambia el pasto durante pausa y vuelo; prueba F7 con una búsqueda abierta y desde chat; verifica que las preferencias se conservan después de reiniciar Play.

La linterna, efectos de cámara, motor de deslizamiento, arquitectura comercial/solar y agua son idénticos byte a byte a v32. Explorer solo añade la entrada de F7; el código de movimiento no cambia. El ZIP contiene únicamente PoolroomsProcedural para Assets, sin herramientas de verificación .NET/Roslyn.
