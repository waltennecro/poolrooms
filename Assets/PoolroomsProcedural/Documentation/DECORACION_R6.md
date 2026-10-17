# Detalle arquitectónico común — R6

Primera reconstrucción de decoración global, acumulativa sobre R1–R5. No equivale a recuperar exactamente la decoración v47 ni a duplicar todos los muebles anteriores.

## Qué cambia

Ocho familias procedurales: impulsión de aire, rejillas de retorno, registros de inspección, cajas de conexión, colectores protegidos, pasos de cables, rejillas de servicio y paneles de protección. Cada una tiene dos geometrías distintas: **16 variantes originales**, sin nuevos modelos descargados. Los bastidores, lamas, bisagras, cierres y tornillos tienen volumen; usan los mapas PBR existentes de acero/pintura/madera.

Se integran en la generación habitual de salas de todos los ambientes, en los centros comerciales conectados y externos, y en las casas detalladas del Nivel 94. Las casas lejanas conservan su geometría anterior. Se añaden detalles, sin modificar los apoyos, el agua, la linterna, los accesos o los objetos interactivos anteriores.

## Colocación y presupuesto

Se seleccionan caras verticales reales de más de dos metros de altura. Las cuatro esquinas de cada pieza deben caber dentro de un triángulo de pared existente; se rechazan piezas que salvan una abertura o se solapan. Las monturas están separadas al menos 1,1 m. La selección es determinista y usa hasta 768 intentos, con una sola exploración previa de la estructura.

Límite por fragmento: 24 monturas, 216 triángulos por pieza y 5.184 en total, agrupados en dos mallas. No se crea un objeto por tornillo/lama, ni nuevos colliders, cuerpos físicos, luces o sondas. Las mallas adicionales consumen memoria aun cuando su renderer está oculto; medir su coste real en Unity antes de elevar distancias de carga.

## Ajustes

Pausa → Gráficos → Renderizado, o **F7**:

- Original: oculta estos nuevos detalles.
- Realista: hasta 12 monturas por fragmento.
- Doble: hasta 24 monturas, según paredes disponibles. Duplica el límite de Realista, no afirma duplicar todos los objetos de v39.

Los cambios actúan sin regenerar ni teletransportar. Los detalles se ocultan a más de 48 m de sus límites; con Rendimiento se limita a la primera tanda y 22 m. La visibilidad se revisa con la cadencia existente del mundo, sin Update por pieza. Las preferencias antiguas migran conservando sus otras opciones.

## Evidencia y límites

Núcleo compilado y 16 geometrías distintas verificadas; límites de piezas, 300 colocaciones junto a arcos en 16 muestras, ausencia de cambios en apoyos, determinismo, cobertura de rutas de salas/centros/casas, casas lejanas sin crecimiento y migración de preferencias. Resultado acumulativo: 16.828.561 aserciones y sintaxis de 179 scripts en cuatro configuraciones. Permanecen las 188 huellas protegidas; se auditan 24 mapas y cuatro fuentes CC0.

Falta compilar/importar/renderizar en Unity, revisar oclusión visual, memoria y FPS. Esto no completa los catálogos urbanos externos ni los 35 vehículos, avión y helicóptero. La descarga desde kenney.nl fue bloqueada por la política de red del entorno; no se incorporaron recursos de ese sitio ni se presumió su licencia individual.
