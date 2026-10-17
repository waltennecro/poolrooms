# v31 — Deslizamiento del jugador

Añade un deslizamiento rápido inspirado en ULTRAKILL al Explorer existente. La actualización conserva el mundo conectado y la mezcla de Poolrooms originales/solares de v30, junto a las funciones anteriores.

## Uso

- Mantén Ctrl izquierdo en el suelo para deslizarte. WASD permite orientar el movimiento gradualmente; sin WASD empieza hacia donde mira el jugador. No es necesario mantener Shift. Soltar Ctrl al estar en el suelo devuelve el movimiento normal.
- Velocidad inicial 11 m/s, ajustable entre 8 y 18 m/s en Pausa > Ajustes > Controles. También puedes desactivar la función allí. Las preferencias quedan guardadas; los ajustes anteriores se migran sin borrar cámara, iluminación, audio o velocidades existentes.
- No tiene energía, duración máxima, daño ni desafíos. Una pared frontal detiene el impulso y requiere soltar Ctrl para empezar otro; un roce lateral permite avanzar junto a la pared. Los obstáculos siguen usando CharacterController.Move.
- Espacio salta desde el deslizamiento y conserva impulso horizontal con una pérdida gradual en el aire. Mantiene el segundo salto de v26 y rechaza un tercero. Ctrl por sí solo en el aire no añade velocidad; saltar y mantenerlo no genera un nuevo impulso al aterrizar sin soltarlo antes.
- La cápsula baja alrededor de los pies, normalmente hasta 0.95 m. Si el radio o skin del controlador necesitan más espacio, ajusta esa altura mínima. El deslizamiento no mueve al jugador verticalmente ni modifica la gravedad.
- Al soltar Ctrl debajo de un techo bajo se queda agachado físicamente hasta tener espacio para levantarse. Una comprobación de cápsula evita expandir el cuerpo dentro del techo; puedes seguir caminando para salir. Esto no constituye un modo separado de agacharse.
- La cámara baja con la postura y recupera suavemente la altura al levantarse. El balanceo de pasos se suprime durante el deslizamiento. La postura física funciona incluso con Reducir movimiento activado o con el componente de efectos desactivado. Activar de nuevo los efectos conserva el ancla de la cámara.
- Pausa, chat, menú maestro, pérdida de captura del cursor, nado, vuelo, teletransporte y espera de geometría cancelan el impulso. Después de esas cancelaciones debes soltar Ctrl antes de otro deslizamiento. En vuelo maestro Ctrl sigue descendiendo. El desplazamiento del origen flotante conserva el estado de impulso, como el salto anterior.
- Se mantienen las salpicaduras y el nado existentes. Los objetos agarrados siguen el sistema anterior. La linterna conserva su código, intensidad, rango y preferencias; su posición sigue a la cámara del jugador.

## Instalación

Fuera de Play, actualiza una única Assets/PoolroomsProcedural con la carpeta del ZIP v31, conservando .meta, escena, perfil y Explorer. Es una entrega acumulativa: no necesita crear otra demo ni activar un menú de versión en Tools. El Explorer añade PoolroomsSlide automáticamente al iniciar Play.

Funcionan las entradas Legacy Input Manager y el adaptador Input System existente. Si un controlador externo utiliza FeedInput, debe llamar también a FeedSlideInput(bool held) cada fotograma; se conserva la firma anterior de FeedInput.

## Qué se modificó

- Core/ExplorerSlide.cs separa la dirección, velocidad, giro limitado, fricción aérea y rearme del motor de Unity. No toca el presupuesto de saltos.
- Runtime/PoolroomsSlide.cs gestiona la altura del CharacterController, espacio para levantarse, colisiones frontales y postura de cámara, con consultas físicas sin asignaciones por fotograma.
- Runtime/PoolroomsExplorer.cs conecta el impulso a sus subpasos de Move, conserva el contacto del suelo antes de redimensionar la cápsula y cancela correctamente en los estados anteriores. Conserva el pivote de los pies y la reparación de suelo.
- Runtime/PoolroomsCameraEffects.cs compone la postura con el ancla y los efectos existentes; evita restar la altura dos veces cuando se desactiva/activa el componente estando bajo. No cambia destellos, shaders ni estilos de imagen.
- GamePreferences, PoolroomsGameSettings, PoolroomsPauseMenu y el adaptador Input System añaden la entrada y sus ajustes, incluido el restablecimiento y la migración de preferencias antiguas.
- No cambia ningún archivo del generador, agua HDRP, centros comerciales o Nivel 94. PoolroomsFlashlight.cs es idéntico byte a byte a v30.

## Comprobaciones

Compilación del núcleo .NET 8 con cero errores y advertencias. Pasaron 11,225 aserciones del movimiento nuevo y sus regresiones de doble salto, cámara y ajustes. Las simulaciones a 10/15/20/30/60/120/144 FPS recorren 33 m en tres segundos a velocidad inicial, con una diferencia inferior a 0.002 m entre muestras; prueban giro limitado, impulso de salto, ausencia de tercer salto, caducidad de impulso aéreo, entradas no finitas, cancelación/rearme, nado, desactivación y preferencias guardadas. Son simulaciones del núcleo, no FPS medidos del juego.

Sintaxis C#9: 140 scripts × cuatro configuraciones, seis asmdef válidos y cero errores. Las pruebas nuevas de ExplorerSlideTests están incluidas para PhysX: pies apoyados a 10/20/60 FPS, cámara baja, salto/doble salto, pared frontal, túnel bajo, levantarse al salir, pausa, TP y desactivar/activar efectos sin deriva. No se han ejecutado porque la nube no tiene Unity Editor.

En Unity 6.3/HDRP 17.3, ejecuta ExplorerSlideTests y ExplorerGroundingTests en PlayMode. Revisa Ctrl y Espacio sobre suelos, escaleras, rampas, piscinas y umbrales del mapa. Entra bajo un techo, suelta Ctrl y sal caminando. Prueba pausa/chat/vuelo/TP durante un deslizamiento y comprueba que no reaparece hasta soltar la tecla. Revisa altura de cámara, FPS y colisiones a velocidades 8, 11 y 18.

**Pendiente:** compilación completa contra Unity/HDRP, PhysX real, apariencia y FPS. La sintaxis y las simulaciones no sustituyen esas verificaciones. La regresión completa de generación de v30 fue aprobada en esa entrega; no se repitió en v31 porque la arquitectura no cambió.
