# Travesía libre: explorar con más opciones

Se instala automáticamente en el jugador de la escena actual y en demos nuevas.
No requiere modo maestro, recrear habitaciones ni cambiar la semilla.

| Acción | Control | Comportamiento |
| --- | --- | --- |
| Ritmo de travesía | C | Normal, Ágil (8 m/s), Exprés (12 m/s); aceleración suave hacia delante. |
| Avance automático | J | Avanza hacia la mirada; J o S lo detienen. Puedes dirigir con el ratón y combinar dirección lateral. |
| Gancho | Mantener K o botón central | Se acerca a arquitectura sólida cargada hasta 28 m; soltar lo cancela. |
| Nado rápido | Tecla de correr, normalmente `|` | Acelera hasta 5,2 m/s mientras nadas. |

Normal conserva las velocidades de caminar/correr configuradas. La carrera de
travesía acelera solo hacia delante; no aumenta la velocidad de retroceso. No
consume energía ni añade HUD permanente. Hay un aviso breve al cambiar de ritmo.

El avance automático se detiene al bloquearse, encontrar un borde sin apoyo,
abrir una interfaz, pausar, viajar o esperar generación. La comprobación de apoyo
es conservadora y puede detenerse ante desniveles: continúa manualmente si quieres
acercarte. Es una ayuda de avance, no navegación autónoma ni búsqueda de rutas.

El gancho es un tirón controlado con aceleración y separación del punto de anclaje;
no es teletransporte, noclip, péndulo físico ni transporte entre niveles. No engancha
pickups, vehículos, redes de seguridad ni puertas temporales invisibles del streaming.
Una pared interpuesta, apoyo descargado, bloqueo o límite de duración sueltan el
cable. Se mueve con CharacterController y pasos acotados a 18 cm. El cable reutiliza
el material metálico existente; no añade luces, sondas ni modelos externos.

En Ajustes → Controles, y en Controles del menú principal, puedes elegir ritmo,
permitir/desactivar avance automático, gancho y nado rápido. Las preferencias se
guardan aparte como `Poolrooms.Player.Travel.v1`; restablecer Controles las reinicia.
C y K/J no ejecutan estas acciones mientras otra interfaz o el modo foto usa la entrada.

Alas G/H, coches, avión, helicóptero, dash, salto doble y deslizamiento continúan.
El gancho/avance se cancelan al conducir, sentarse, usar toboganes o volar. No cambia
la distancia de renderizado, los materiales del mundo, la linterna ni los IDs existentes.

## Comprobación

`dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --travel`
comprueba límites de movimiento, seis frecuencias, aceleración integrada, cancelación,
obstáculos del modelo e inputs inválidos. También compila el componente real completo
con Core y sustitutos reducidos de las APIs Unity para detectar fallos semánticos.
Esto no equivale a compilar contra Unity ni a ejecutar PhysX.

`TravelTests` añade tres pruebas nativas de paredes, oclusión/barreras temporales y
apoyo del avance automático. Unity/PlayMode, sensación de movimiento, cámara, cable,
sonido y rendimiento reales siguen pendientes hasta ejecutar el proyecto Unity.
