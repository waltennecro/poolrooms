# Talleres compartidos

Esta ampliación añade doce actividades jugables al mundo existente. No son otros doce biomas: los 58 biomas de catálogo conservan su arquitectura, rutas, materiales y distancia de renderizado. Los nuevos talleres comparten las ubicaciones de pared ya comprobadas por el generador, con un máximo total de cuatro estaciones por sala.

## Cómo encontrarlos y usarlos

Actualiza `Assets/PoolroomsProcedural` con Play detenido. No hace falta borrar tu perfil ni recrear la demo. La exploración con encuentros habilitados reparte los talleres entre las salas: aproximadamente un tercio de las selecciones utiliza el banco nuevo. Se favorecen actividades apropiadas para cada ambiente, pero ninguna de las doce queda excluida de un bioma.

Acércate y pulsa **E**. Cada tablero incluye instrucciones, referencia visible, deshacer, reinicio y pistas opcionales. No hay cronómetro, castigos, voces, sustos ni recursos consumibles obligatorios. **Tab / Esc** cierra la interfaz. Desde el inventario, **Talleres** muestra el cuaderno de esta sesión.

| Actividad | Acción |
| --- | --- |
| Circuito de faroles | Enciende una cuadrícula mediante interruptores que afectan a vecinos. |
| Canales del jardín | Gira dieciséis tuberías para formar un recorrido continuo. |
| Telar de viaje | Reconstruye una muestra de hilos y colores. |
| Herbario compartido | Ordena etiquetas mediante intercambios adyacentes. |
| Reloj de la tarde | Ajusta tres discos independientes a la referencia. |
| Balanza de paquetes | Combina pesas hasta igualar el paquete. |
| Carta de estrellas | Une nueve estrellas en orden. |
| Melodía de campanas | Escucha y reproduce una frase con ocho teclas. |
| Compuertas de marea | Transfiere agua entre cuatro depósitos, conservando el volumen. |
| Encuadernación de colores | Gira grupos de páginas hasta reconstruir la muestra. |
| Rosa de los vientos | Orienta cuatro agujas acopladas. |
| Vivero de semillas | Ajusta humedad sin desbordar los semilleros. |

## Detalles del mundo

Cada taller tiene una nota de preparación para una próxima visita, sin personajes añadidos. Los modelos originales incluyen marcos, tornillos, canales, campanas, aros y herramientas. Las piezas muestran cambios de estado en el mundo, además del menú: agujas, conexiones, indicadores de agua y marcas de progreso. Los sonidos reutilizan el banco espacial existente y sus notas originales; no se añaden grabaciones descargadas ni licencias nuevas.

Los doce modelos tienen tres estilos de montaje. Son **doce actividades y 36 combinaciones de cuerpo/montaje**, no 36 modelos externos distintos. Las superficies usan los materiales PBR compartidos del mobiliario existente. Los hilos y páginas reutilizan las texturas de tejido, con cuatro tintes guardados por chunk que comparten la misma paleta del menú; no usan el latón de los mecanismos para representar hilo azul. El brillo de los faroles es un indicador del tablero, no una nueva luz por pieza ni una modificación del sol del bioma. Los depósitos son un juego mecánico: no cambian el agua de las piscinas.

## Memoria y coste

Se conservan los 128 tableros usados más recientemente y 32 movimientos de deshacer por tablero. Un tablero descargado por streaming recupera su estado si sigue en esa memoria. Al salir de la caché, vuelve a su distribución inicial en la siguiente visita. El cuaderno, las piezas y sus estadísticas no se guardan en disco.

Cada estación añade como máximo una malla visual reutilizada, sin colisionador adicional, luces, sondas ni texturas propias. Solo se reconstruye al cambiar el tablero. Una referencia débil evita que un modelo visible retenga tableros expulsados de la caché. La reproducción de referencias musicales se cancela al cerrar/cambiar de interfaz o editar el tablero.

## Comprobaciones y límites

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --workshops` resuelve 3.072 tableros antes y después de entradas arbitrarias, comprueba reinicio/deshacer, memoria, geometría y disponibilidad en los 58 bancos. También comprueba las estaciones sobre 233 salas construidas del mundo real del núcleo. Las huellas de arquitectura anteriores no se regeneran.

La prueba nativa `WorkshopDisplayTests` verifica reutilización de malla, materiales, ausencia de luces/colisión extra y liberación al descargar. **Está pendiente de ejecutar en Unity**, al igual que la apariencia, los controles, el sonido y los FPS. Las comprobaciones de sintaxis y del núcleo no sustituyen esa ejecución.
