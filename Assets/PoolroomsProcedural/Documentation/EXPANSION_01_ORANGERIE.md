# Expansión 1 de 10 — Orangerie · Jardines del reposo (R19)

Nuevo bioma independiente del Jardín de cristal ya existente. Se integra en
la exploración `Expanded`, con regiones aleatorias por semilla; no requiere
un portal ni seguir una secuencia. El inicio habitual conserva las Poolrooms.

## Qué se genera

2.500 composiciones procedurales: cinco programas (palmeras, lectura, té,
helechos y jardín mineral) × cinco cubiertas acristaladas × cinco huellas de
jardineras × cinco ritmos de columnas × cuatro alturas de cubierta.
No son 2.500 escenarios hechos a mano ni modelos descargados. Se comprueban
las 2.500 huellas de la estructura con semilla, coordenadas y salidas idénticas;
las diferencias de objetos, colores o iluminación no cuentan para esa cifra.

Cada composición tiene cuatro patios interiores, rutas anchas, umbrales
transitables, cubiertas abiertas bajo lucernarios de dos pendientes y mobiliario
propio de su programa: bibliotecas bajas, mostradores de té, celosías,
bancos, mesas o pedestales. Las salas del mismo bioma comparten aperturas
de 6–9 m; las fronteras conservan la medida del mundo vecino.

Cada sala coloca 16 objetos interactuables reutilizando modelos con procedencia
y licencia ya registradas: cuatro asientos, cuatro mesas y ocho objetos pequeños.
Son 40.000 instancias planificadas al recorrer todo el catálogo, no objetos
simultáneos ni modelos nuevos. Tazas y libros usan el catálogo procedural existente.
Los bancos y estanterías integrados en la estructura son estáticos.

## Materiales, luz y rendimiento

Cuatro acabados originales a 1024 px: caliza, cal, terracota y hojas, con
albedo, normal y máscara HDRP. Metal y madera reutilizan la biblioteca actual.
Generador reproducible: `tools/create_orangerie_textures.py`.
No contienen fotografías externas ni sustituyen licencias de muebles existentes.

Sol físico regional, sombras y solicitud de sombras RT cuando las opciones,
HDRP y hardware lo permiten; reflejos/GI siguen los ajustes Alto, Cinemático RT
y Ultra existentes. Las cubiertas tienen huecos reales y vidrio. Las hojas
son geometría curva opaca, con caras dobles explícitas, sin recorte alfa.

La luz artificial cambia suavemente de tonalidad según el programa de la sala;
los paneles emisivos comparten el color de sus luces. El apoyo difuso no produce
rectángulos flotantes en reflejos. Exposición suavizada según la cubierta.
No se fuerza RT, no se reducen distancia ni calidad y no se crean sondas por sala.

Máximo medido matemáticamente: 10.282 triángulos de arquitectura, detalle,
vidrio y vegetación por receta (antes del amueblado importado y decoración global).
Las hojas se agrupan en una malla visual sin colliders ni componentes por hoja.
Ocho fuentes candidatas por sala respetan el presupuesto de luces del mundo.
No hay 2.500 salas precargadas: se usa el streaming existente.

## Cómo encontrarlo y comprobarlo

- Con el paquete importado, `Tools → Poolrooms → Crear demo` usa `Expanded`.
- Perfil: `Enable Orangerie` activo y `Orangerie Share` > 0 (predeterminado 0,14).
  La proporción es una probabilidad regional, no un porcentaje exacto del mapa.
- También se puede elegir `Liminal Generation → Orangerie` para una prueba aislada.
- Modo maestro: chat `Y`, `/unlock mastermode`, menú `M`, destino
  `Orangerie · Jardines del reposo`. El TP busca una región generada de verdad.
- Ejecutar PlayMode `OrangerieWorldTests` para recorrido, lucernarios,
  ausencia de collider en vegetación y disponibilidad de las doce texturas.

Se han ejecutado 188.699.920 aserciones del núcleo, con 188 huellas antiguas
protegidas y 205 scripts comprobados en cuatro configuraciones de sintaxis.
Las aserciones son comprobaciones matemáticas, no pruebas nativas de Unity; las pruebas PlayMode
están añadidas pero no ejecutadas. Compilación Unity/HDRP, shaders, aspecto,
física, interacción y FPS requieren Unity real. No se certifica calidad AAA.

Las otras nueve entregas y las 10.000 zonas adicionales del mundo anterior
siguen separadas y pendientes en `EXPANSION_NIVELES.json`. Esta entrega aporta
un nivel nuevo; no se presenta el objetivo completo como terminado.

Actualización R20: las salas de lectura y té incorporan dos plantas con
licencia, alcanzando 18 objetos por sala elegible y 42.000 instancias
planificadas en todo el catálogo. Ver `ACTUALIZACION_R20_RECURSOS.md`;
el coste de esas plantas y sus LOD se cuenta por separado de la arquitectura.
