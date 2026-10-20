# Sonido inmersivo y música local · Resonancia

Se instala automáticamente al arrancar Explorer, también en escenas existentes.
No requiere activar opciones de administrador ni regenerar el mundo.

En **Ajustes → Sonido**:

- **Tranquila** es la atmósfera inicial: aire suave, jardines, circulación de agua,
  lluvia y reverberación según la sala. Los contactos distinguen baldosa, metal,
  superficie mojada, agua, madera, suelo acolchado y terreno con hierba.
- **Inquietante** añade un fondo tenue y detalles ambientales espaciados: pasos
  remotos, crujidos, aire y resonancias de tuberías. No incluye voces, susurros,
  sustos ni golpes de volumen. Primer detalle después de al menos 18 segundos;
  luego pausas de 18–38 segundos o más. Los pasos solo suenan sobre suelo
  detectado, a al menos ocho metros al iniciarse cada contacto. No son enemigos.
- Puedes ajustar intensidad, detalle y rango dinámico, quitar los pasos remotos,
  los movimientos o los contactos de objetos, y atenuar el ambiente con música.
- Dash, salto, aterrizaje y vuelo tienen capas discretas. Los objetos que tomas
  producen contactos según material, masa y velocidad al caer; se limita su frecuencia.
- Paredes y agua atenúan las fuentes; los ambientes cambian gradualmente.

## Tus canciones

1. Pulsa **Abrir carpeta** en Sonido → Tu música.
2. Copia archivos **MP3, OGG, WAV, AIF o AIFF**. En Windows es `Music/` junto al
   ejecutable; en Unity Editor, `Music/` en la raíz del proyecto. Si esa ubicación
   no permite escritura, se usa `Application.persistentDataPath/Music`.
3. Pulsa **Actualizar lista** y elige una canción. También puedes usar anterior,
   siguiente, pausa, detener, búsqueda temporal, aleatorio y repetición.

Hasta 1.024 pistas, tres niveles de subcarpetas y 128 MiB por archivo. Se omiten
archivos vacíos, enlaces simbólicos y extensiones no admitidas. Un archivo dañado
muestra un estado de error y conserva la canción anterior. Los archivos son
locales: no se suben ni se copian al paquete del juego. Los formatos dependen del
decodificador de Unity de la plataforma; formatos como FLAC no se anuncian.

El reproductor funciona con el menú pausado; su propio botón permite pausarlo.
El volumen general y el silencio en segundo plano también afectan a la música.
La reproducción automática es opcional y viene desactivada. Restablecer Sonido
restablece los ajustes, sin borrar canciones. El último archivo se recuerda localmente.

## Recursos y presupuesto

48 clips PCM de 24 kHz: 23 sustituyen recursos anteriores y 25 son nuevos.
Dos grabaciones originales CC0 de Kenney adaptadas para contactos; los otros
componentes son síntesis no vocal original, generada fuera del juego.
Fuentes, hashes, licencia y adaptaciones: `Sources/ImmersiveAudio/` y
`Resources/ImmersiveAudioSources/Licenses/`. No son 48 grabaciones descargadas.

Máximo fijo de 8 voces originales + 8 nuevas + 2 de música, y una voz de lluvia
existente: 19 en total con lluvia. Ningún objeto recibe su propio AudioSource.
El PCM decodificado de estos 48 clips ocupa aproximadamente 31,6 MiB, sin contar
los búferes de las canciones del usuario o la lluvia. No se sintetiza audio por
fotograma; la biblioteca se explora en un trabajo separado y tiene límites.

## Comprobación

`--immersive-audio` comprueba el núcleo, temporización y biblioteca;
`python tools/verify_immersive_audio.py` comprueba archivos y procedencia.
`tools/create_immersive_audio.py` reproduce los recursos con ffmpeg y numpy.
Se añaden dos pruebas PlayMode para carga PCM y reproducción/cancelación real.
Estas pruebas nativas, MP3/OGG/AIFF en Windows, la escucha, la oclusión y el
rendimiento necesitan Unity; no se certifican mediante los checks del núcleo.
