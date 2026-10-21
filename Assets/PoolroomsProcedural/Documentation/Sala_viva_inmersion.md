# Sala viva: sonidos y actividades

Esta actualización añade actividades automáticamente a **Exploración unificada**, también en los interiores detallados de las casas del nivel 94 y en sectores compatibles de centros comerciales. No requiere administrador ni seleccionar una demo distinta.

## Doce tipos de puntos de actividad

- **Té:** preparar una taza, esperar cuatro segundos y guardarla para beber después.
- **Agua:** llenar y guardar una botella.
- **Libros:** leer, tomar un libro y dejar otro del inventario.
- **Postales:** llevar una postal y escribir una nota local de hasta 240 caracteres.
- **Pizarra:** dibujar con seis colores en una cuadrícula de 8 × 8; el dibujo aparece también sobre la pizarra del mundo.
- **Campanillas:** tocar un sonido suave con una pequeña respuesta visual.
- **Caja musical:** tocar ocho notas desde el mueble.
- **Luz de bienvenida:** encender o apagar una luz incluida en el presupuesto existente.
- **Paquetería:** recoger galletas, pelota o cuaderno de compartimentos independientes.
- **Rompecabezas deslizante:** ordenar 15 fichas; las mezclas se hacen mediante movimientos válidos, por lo que son resolubles.
- **Mosaico:** girar nueve piezas para reproducir una referencia.
- **Campana de descanso:** dejar sonar una nota y seguir explorando sin cronómetro ni castigos.

Usa **E** mirando el punto de actividad. Tab/Esc cierran su panel. Se conserva el inventario de 250 espacios, la barra con 5, los objetos usables y las interacciones anteriores. Las recompensas comprueban primero que el inventario admita el objeto; un inventario lleno no consume el compartimento.

Las notas y dibujos son personales durante esta sesión: no se envían a otros jugadores ni se guardan en disco. El préstamo de libros consume el libro elegido al pulsar «Devolver». Cada recompensa se recoge una vez por compartimento y sesión. Los juegos y dibujos mantienen hasta 128 puntos registrados por tipo; al alcanzar ese límite se libera una entrada antigua. Las notas mantienen hasta 4.096 entradas y las preparaciones de té hasta 256. La persistencia de partida continúa aplazada.

## Sonido y ambiente

Se incluyen **32 clips PCM originales**, correspondientes a 16 efectos con dos variaciones: porcelana, agua, páginas, papel, escritura, campanillas, caja musical, pestillos, paquetes, fichas, interruptores, tela, gotas, hojas, conductos y cerámica.

Los efectos de las interacciones salen del objeto, con atenuación por distancia, oclusión y reverberación del sistema existente. Los puntos cercanos aportan detalles discretos cada 9–18 segundos; la búsqueda de emisores examina como máximo 64 registros por actualización. No hay un AudioSource por mueble: se reutilizan las cuatro voces de detalle y se conserva el límite previo de fuentes. La música y los nuevos objetos usan el volumen de ambiente; silenciar pasos ya no silencia el piano.

En **Ajustes → Sonido → Detalles sonoros de las salas** puedes desactivar los sucesos nuevos. «Detalle ambiental» controla su intensidad. El ambiente tranquilo sigue siendo el inicial. El modo inquietante conserva sus efectos opcionales; esta etapa no añade voces, susurros ni sustos.

## Colocación, materiales y licencias

Se colocan hasta cuatro puntos adicionales por sala sobre triángulos reales de pared, a una altura alcanzable. Se comprueba el apoyo, la separación entre puntos, los avisos y el mobiliario registrado. Los accesos reservados siguen libres. Hay doce actividades y tres acabados/variaciones por tipo; no son 36 modelos externos nuevos ni 36 biomas.

La geometría y los efectos son originales CC0. No se descargaron modelos ni grabaciones nuevos en esta etapa. Se reutilizan los materiales PBR, las licencias y los créditos anteriores. La claridad del agua, las alas, la linterna y la distancia de renderizado se conservan.

## Validación

El núcleo verifica las huellas geométricas anteriores, la colocación de puntos en salas reales, límites, modelos y resolubilidad de mil mezclas de cada juego nuevo. El verificador de recursos audita PCM, niveles, extremos silenciosos, hashes y licencia. Tres pruebas PlayMode nuevas comprueban registro/descarga de emisores, carga de los clips y migración de preferencias antiguas. **Unity no está instalado en el entorno:** esas pruebas nativas, la escucha, la apariencia y los FPS permanecen pendientes en el proyecto real.
