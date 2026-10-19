# R34 — refinado por lotes de 10.000

## Alcance

Se refina el generador de los diez niveles incorporados en R33. Se conserva su
identidad: misma permutación de recetas, enums, coordenadas y claves de objetos
anteriores. Las mejoras se aplican a las 131.072 recetas de cada nivel; la
comparación de mallas se hace sobre las mismas **200.010 arquitecturas** de R33,
en 20 lotes de 10.000 y uno de 10. No son 200.010 habitaciones dibujadas a mano,
ni familias nuevas, ni modelos descargados adicionales.

## Cambios reales

- Todos: tableros con biseles geométricos, travesaños y cornisas fuera de los
  accesos. Se conserva la superficie plana necesaria para apoyar los objetos.
- Biblioteca: respaldo y asiento perfilado en los bancos de lectura; estantes
  retranqueados para dejar libre la aproximación a los bancos.
- Terminal: marcos e indicadores con relieve en los paneles de información.
- Té: vigas que unen los postes y bandejas apoyadas sobre las mesas bajas.
- Cerámica: placas montadas en los pedestales y remates del muro de exposición.
  Se mantiene la corrección de caras exteriores/interiores de los jarrones.
- Jardín: cuatro plantas licenciadas adicionales sobre los parterres, bordes
  unidos sin superficies superiores superpuestas y acabado de terracota. Las
  plantas conservan materiales, recorte de hojas y LOD del recurso original.
- Canales: puentes de 1,80–2,16 m, postes y pasamanos separados, juntas del
  entablado. Los accesos permanecen abiertos; no son bloques de barandilla macizos.
- Música: tapa del piano, atril y pedales con volumen y apoyo.
- Faroles: bastidores laterales y base de las pantallas.
- Lavanderías: carcasa abierta alrededor del ojo de buey, aro con espesor,
  cristal independiente y tambor retranqueado; bandeja y tirador con relieve.
- Santuario: basas, collarines y cornisas sobre los pórticos de las piscinas.

## Coste y compatibilidad

Los detalles se integran en las mallas existentes; no añaden luces ni materiales
de sala, cámaras de reflejos o una actualización por cuadro por detalle. Cada
receta comparada admite como máximo 5.000 triángulos arquitectónicos adicionales;
el informe registra el coste real. Este límite excluye modelos de objetos:
el jardín pasa de 60 a 64 objetos, cuatro instancias reutilizadas de la planta
existente con sus LOD. No se promete conservar los FPS sin medirlos.

Las cuatro plantas se añaden **después** de los slots originales. La linterna,
los radios de carga y las geometrías históricas protegidas se conservan.
Al importar sobre R33 se actualizan únicamente los mapas predeterminados del
material generado de los parterres; se conservan GUID, ajustes de color y
materiales o mapas personalizados. No hay que recrear la demo.

## Verificación reproducible

`Verification/refinement_R34_batches.json` contiene rangos, huellas SHA-256 de los
registros, cambios de estructura y triángulos por lote. Los registros completos
se generan fuera de Assets para evitar importar herramientas .NET en Unity.

```sh
dotnet run -c Release --project Verification/Poolrooms.Checks.csproj -- --restful-refinement
python tools/refine_restful_batches.py --output /tmp/R34-review --expect-report Verification/refinement_R34_batches.json
```

La segunda orden necesita el núcleo ya compilado en Release y el objeto Git
`0a43903830cf694377118229a23dbeeed9a6346b`. Compila ese núcleo anterior por separado,
reproduce la ausencia del bisel, genera la referencia y compara cada receta con
la versión actual. Exige que **todas** cambien, sigan siendo distintas y respeten
los límites. Las muestras detalladas verifican apoyos, paso por los puentes y
profundidad real de las ventanas de lavadora. La última revisión también
comprueba que el atril sobresalga de la carcasa del piano y que los estantes no
invadan el paso delante del banco; ambos defectos se reprodujeron antes de corregirlos.

Las comprobaciones cloud no certifican aspecto, APIs Unity, sombras HDRP,
reflejos, PhysX o FPS. Hay una prueba PlayMode nueva para cruzar los puentes con
CharacterController; necesita ejecutarse en Unity 6.3/HDRP 17.3.
