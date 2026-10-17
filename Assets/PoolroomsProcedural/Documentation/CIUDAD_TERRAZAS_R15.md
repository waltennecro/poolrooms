# R15 · Terrazas de café con piezas de Bistro

Se incorporan a **la generación normal de City11**, sin un menú de demostración separado. Un bloque admite como máximo una terraza: una mesa redonda, dos sillas y un toldo. La selección por semilla conserva el orden de los objetos anteriores y añade los nuevos al final; sus claves de recogida siguen siendo estables. No aparecen en pistas de aterrizaje ni sobre las galerías con porche existentes.

La mesa y las sillas permiten E/Q mediante el sistema de agarre actual, con masas de 12/4 kg. No añaden Rigidbody hasta soltarlas. El toldo queda por encima de 3,05 m, sin un componente Update ni luces/reflection probes adicionales. Los objetos conservan las calles de seis metros, el acceso central a las tiendas y el apoyo sobre pavimento real.

## Procedencia y detalle

Son tres geometrías de **Amazon Lumberyard Bistro**, Amazon.com, Inc. o sus afiliadas, CC BY 4.0. Las licencias completas, atribución y modificaciones están en `Resources/Bistro/Licenses`. No son edificios completos descargados. La arquitectura de los edificios existentes continúa siendo procedural.

| Pieza | Triángulos cerca | LOD 1 | LOD 2 |
| --- | ---: | ---: | ---: |
| Toldo | 7.032 | 2.037 | 1.150 |
| Mesa de café | 1.692 | 598 | 432 |
| Silla | 3.328 | 1.388 | 807 |

Incluyen 16 mapas PNG de hasta 1024 px derivados de las texturas originales. El material HDRP usa Specular Color: conserva el color especular dieléctrico, su factor y la suavidad del canal alfa de la textura original. Las normales mantienen su UV y se importan con tangentes Mikk. Rendimiento comienza en LOD 1; los otros modos usan la geometría cercana y dos LOD inferiores.

## Comprobación y límites

El núcleo comprueba 32 terrazas/96 objetos en 96 bloques de distintos tamaños y semillas: apoyo de nueve puntos, separación de muebles, espacio de puertas, calles y fachada final. La auditoría externa verifica hashes, licencias, PNG, dimensiones verticales, índices, normales y LOD. Las huellas protegidas de v39 no se cambian.

Las pruebas PlayMode añaden importación, escala, materiales Specular Color y colisión contra la ciudad. **No se han ejecutado aquí**: sigue faltando Unity 6.3/HDRP 17.3. Tampoco se certifican shaders, apariencia, física nativa, memoria ni FPS. La linterna y `Preparacion_V48` permanecen intactas.
