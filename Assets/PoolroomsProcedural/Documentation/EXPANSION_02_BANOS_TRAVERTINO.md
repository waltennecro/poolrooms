# Expansión 2 de 10 — Baños de travertino (R21)

Bioma nuevo independiente de las Poolrooms y la Orangerie. Se encuentra en
regiones aleatorias de `Expanded`, conectado por entradas arquitectónicas
reales. El comienzo habitual sigue en Poolrooms; no hay un orden de niveles.

## Arquitectura y contenido

**2.500 composiciones procedurales**, comprobadas por huella de la estructura
con idénticas coordenadas y semilla: cinco programas (patio termal, galería
mineral, salón de té, claustro cálido y baño de lucernarios), cinco formas de
piscina, cinco cubiertas, cinco ritmos de columnas y cuatro profundidades.
No son 2.500 modelos descargados ni escenarios hechos a mano.

Cada sala ocupa una celda de 28–60 m. Tiene entre una y cuatro piscinas,
pasillos secos anchos, columnas de piedra, nervaduras cerradas, lucernarios
rectangulares o circulares, muebles de descanso y estantes con toallas.
Los patios secos incorporan tumbonas originales con respaldo inclinado,
almohadas de tela apoyadas en él y acabado de mimbre con licencia.

Las piscinas tienen fondo real y escalones alrededor: de 0,42 a 1,05 m de
profundidad, dos a cinco escalones de 21 cm y huella de 30 cm. La losa del
suelo se recorta antes de construirlas; no hay un suelo invisible sobre el agua.
Se preserva la exploración tranquila, sin daño ni desafíos.

Cada receta coloca 16 objetos interactuables: cuatro asientos, cuatro mesas,
cuatro botellas, dos lámparas decorativas y dos objetos pequeños (tazas/jabón).
Estanterías, toallas geométricas y tumbonas son mobiliario estático.
Catálogo completo: 6.000 piscinas y 40.000 instancias de objetos planificadas,
incluidas 5.000 repeticiones de la lámpara; no se cargan simultáneamente.

## Recursos buscados en línea

- Modelo **LightsPunctualLamp**, DGG, 2021, **CC BY 4.0**; conservado con sus
  tres mallas originales y versiones LOD: 6.000 / 3.952 / 2.012 triángulos.
  Las luces y emisión de la demostración se eliminan: es un objeto decorativo
  apagado, que se puede agarrar. El vidrio es una aproximación transparente
  HDRP al material glTF, sin afirmar equivalencia exacta de transmisión.
- Material **ClearcoatWicker**, Eric Chadwick, 2023, **CC0**. Se adaptan color,
  normales y AO/metal/rugosidad a las tumbonas propias. La esfera usada como
  muestra por el autor no aparece en el juego y no cuenta como modelo nuevo.
  Se solicita capa de barniz HDRP; la normal específica de esa capa no se reproduce.
- Fuente fijada: `KhronosGroup/glTF-Sample-Assets`, revisión
  `edc7c9e67c639d230715049ee31f9a96a6babbbe`. Originales y hashes en
  `Sources/Baths`, licencias y atribuciones también dentro de Assets.
- Los créditos visibles desde inicio/pausa incluyen estos recursos y los anteriores.
- Travertino seco y mojado: seis mapas PBR originales de 1024 px, generados
  por código. La cal, tela y otros materiales existentes se reutilizan.

Se consultaron Poly Haven y ambientCG para materiales de piedra; sus servidores
respondieron con denegación del proxy (403). No se presentan esos materiales
como descargados. Sí se descargaron e integraron los dos recursos de Khronos.

## Luz, agua y coste

Sol físico, tono cálido por sección y exposición suavizada. Cuatro fuentes de
apoyo difuso sin rectángulos especulares ficticios y cuatro apliques respetan
los presupuestos existentes. Sombras RT, reflejos y GI siguen las preferencias
Cinemático/Ultra y sus alternativas compatibles; no se fuerza RT ni se crean
sondas adicionales por sala.

Agua nativa HDRP mediante el adaptador existente, con caústicas/reflejos y una
paleta termal suave al entrar cuando se usa ClearCeramic. Los estilos de agua
personalizados se respetan; al salir vuelve la apariencia del bioma correspondiente.

Máximo verificado: **8.936 triángulos** de arquitectura, detalle, vidrio y
tumbonas por receta (curvas con 16 segmentos). Excluye muebles importados,
agua y decoración global; no es un benchmark FPS ni un límite para otra resolución
de curvas. El mimbre y las almohadas se agrupan en una malla por sala con patios
secos, sin colliders por fibra. El streaming conserva su distancia.

## Uso y comprobación

`Tools → Poolrooms → Crear demo` usa Expanded. En el perfil, activar
`Enable Thermal Baths` (predeterminado activo), con `Thermal Bath Share` > 0
(predeterminado 0,11, probabilidad regional). Para un mundo aislado elegir
`Liminal Generation → ThermalBaths`. En modo maestro aparece el destino
**Baños de travertino** junto a los demás biomas.

Se comprobaron las 2.500 estructuras, determinismo, geometría finita, normales,
escalones/fondos y 120 salas con tamaños y puertas distintos. Las 188 huellas
antiguas permanecen protegidas. Fuente, licencias, PNG y dimensiones del modelo
se auditan con `tools/verify_bath_sources.py`; importación y mapas de piedra
se reproducen idénticos desde una carpeta de destino vacía.

Se añadieron PlayMode `ThermalBathWorldTests` para subir desde el fondo, cruzar
rutas y cargar la lámpara sin luces de la demo. No se ejecutaron: faltan Editor
Unity, proyecto HDRP real y GPU en la nube. Compilación Unity/HDRP, shaders,
aspecto, PhysX y FPS siguen pendientes. No se afirma calidad AAA certificada.

Esta entrega completa la implementación del segundo nivel; quedan ocho niveles
y las 10.000 zonas globales adicionales, según `EXPANSION_NIVELES.json`.
