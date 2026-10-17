# Reconstrucción R11: coche y calles conectadas

Se conserva ToyCar de Khronos (CC0), sus texturas y sus 91.331 triángulos; no es un nuevo modelo descargado. La conversión separa neumáticos y cubos por componentes conectados, sin modificar atributos ni añadir caras. Los tres LOD mantienen cuatro ruedas identificables (14.988 y 3.319 triángulos en los derivados).

El coche mide 4,2 m. Los contactos de rueda usan los centros y radios de la geometría importada, no proporciones aproximadas de la carrocería. Los tres LOD siguen suspensión, giro y rodadura mediante `WheelCollider.GetWorldPose`. El frente se normaliza usando las ruedas delanteras y traseras importadas. La suspensión conserva muelles y amortiguadores; solo hay simulación al conducir.

La vista pasa al exterior: el recurso no tiene cabina interior. Una esfera de 22 cm limita la cámara ante obstáculos. Al abrir menús o faltar soporte cargado se conservan velocidades y se suspende la física; al reanudar se restituyen. La salida busca suelo estático y espacio libre para la cápsula. Se mantiene E para entrar/salir, WASD para conducir y Ctrl para frenar, sin combustible, daño ni muerte.

Las conexiones existentes entre bloques City11 tienen una apertura de 6 m, incluida la unión con la pista. Las uniones interiores del aeródromo mantienen toda su anchura. Las entradas a otros biomas conservan su medida anterior. El grafo no cambia: una calle cerrada sigue cerrada. Geometría, umbrales y barreras temporales usan la misma función; no se abre un paso mientras falta el chunk vecino.

## Comprobación

- Núcleo compilado con SDK 8.0.408: 687 uniones recíprocas de ciudad y 63 entradas a otros biomas comprobadas; 108 huellas SoftPlay y 80 de otros biomas intactas.
- Auditoría de fuentes: cuatro ruedas con centros/radios contrastados en el OBJ y presencia en los tres LOD. Conservados los cinco modelos CC0, sus hashes y licencias.
- `CityVehicleRigTests` comprueba escala de neumáticos, suspensión y renderers de los tres LOD dentro de Unity; **añadido pero no ejecutado aquí**.
- Pendiente en Unity: compilación de APIs, rodadura/contactos reales, cámara ante esquinas, estabilidad al frenar, cambios de origen y FPS. Los cálculos del núcleo no sustituyen PhysX.
