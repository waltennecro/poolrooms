# R25 — Todos los niveles en la exploración a pie

La exploración unificada incluye los **18 biomas implementados**, los centros
comerciales completos y el nivel 94. El modo maestro conserva sus atajos, pero
no activa los niveles ni es necesario para acceder a ellos.

## Qué cambia

Al crear la distribución, `WalkableExploration.Configure` trabaja sobre la copia
de configuración del mundo. Activa los biomas existentes, restablece frecuencias
positivas si un perfil antiguo las tenía a cero y conecta centros comerciales
y terreno del nivel 94. Conserva el archivo de perfil original, los materiales,
las semillas de las recetas, los controles y la distancia de renderizado.

Se incluyen Poolrooms cerradas y solares, juegos infantiles originales y áreas
infantiles multinivel, supermercados pequeños, oficinas, hoteles, salas de espera,
comedores, City11, Archivo 906, Patio 188, Costa 48, Jardín de cristal, Galería de
tránsito, Orangerie, baños, parque acuático y Casas de lluvia. Los centros
comerciales de varios pisos y el nivel 94 mantienen sus distritos extensos y
entradas físicas. Las seis propuestas de niveles futuros no están implementadas.

La selección de biomas reserva dos regiones candidatas por ambiente habilitado
en cada bloque de 8 × 8 regiones. Una permutación Feistel barajada por semilla
y bloque evita un orden lineal. Las demás regiones conservan su selección
ponderada y todas conservan sus bordes Voronoi irregulares. El inicio sigue en
Poolrooms. No hay una ruta obligatoria, contador de visitas, orden de niveles ni
teletransporte automático. Una reserva de terreno o centro comercial puede ocupar
una región candidata; no se promete que cada paseo corto encuentre todos los
biomas ni una distancia máxima para cualquier semilla.

## Conexiones físicas

El mismo grafo de puertas conecta las salas y sus regiones. Los centros
comerciales tienen galerías de entrada continuas hacia sus accesos de planta
baja, y el nivel 94 tiene umbrales que desembocan en el paisaje. No se utiliza la
probabilidad de los antiguos portales para habilitar estas entradas. Las tapas
temporales de streaming se abren cuando existe suelo cargado al otro lado.

Se corrigen las uniones directas de Casas de lluvia: no pasan por un generador
de pasillos curvos con longitud cero, que podía generar valores no finitos y una
conexión incompleta. También se corrige una columna de las galerías cívicas de
60 m que coincidía con el centro de las salidas este/oeste. Los pasillos nuevos
en la exploración unificada usan el material de su ambiente de origen.

## Uso y perfiles

Importa `Assets/PoolroomsProcedural`, crea la demo normal o vuelve a entrar en
Play con **Exploración unificada** activa en `PoolroomsWorld`. No hace falta abrir
el modo maestro ni activar cada zona desde Tools. La preparación usa una copia
en ejecución y la vista previa usa la misma regla de distribución.

Los catálogos de prueba `Catalog` y `Single` conservan su propósito de inspección.
Para probar exclusivamente un bioma, desactiva Exploración unificada y selecciona
su generación específica. Los perfiles personalizados del núcleo con
`DiscoveryCoverage=false` mantienen la selección anterior.

La nueva regla cambia dónde aparecen los ambientes al regenerar un mundo mixto.
No modifica los IDs serializados de los biomas ni los archivos de linterna.

## Comprobaciones

- `dotnet run --project Verification/Poolrooms.Checks.csproj -- --walkable-only`:
  cobertura regional, migración de flags antiguos, búsquedas, grafo y conexiones.
- `dotnet run --project Verification/Poolrooms.Checks.csproj`: regresión acumulativa,
  huellas protegidas, todos los catálogos y sintaxis C#.

La comprobación independiente encuentra 110 rutas testigo con cinco semillas
(0, 31, 481516, 87 y 1234) y sectores de 28, 36 y 60 m. En cada caso alcanza los
18 biomas, los dos distritos y las dos variantes climáticas de Poolrooms. No
atraviesa celdas reservadas ni usa un estado de administrador o teletransporte.
Comprueba reciprocidad de puertas, 148 pares de conexión con mallas reales y
10 accesos de distrito. Muestrea suelo y espacio para piernas, cuerpo y cabeza
en entradas y pasillos curvos. Las piscinas y columnas interiores pueden
rodearse; la prueba no presupone caminar en línea recta por el centro de ellas.

Los 324 emplazamientos candidatos de nueve bloques pasan la comprobación de
cobertura y barajado. El muestreo físico se realiza sobre triángulos del núcleo,
no sobre PhysX. Los dos tests PlayMode nuevos comprueban la migración del perfil
y el cruce de umbrales con CharacterController; están escritos y sin ejecutar.

Faltan Editor Unity 6.3, proyecto HDRP completo y GPU en este entorno. Compilación
Unity/HDRP, travesía nativa, streaming real, aspecto y FPS siguen pendientes. Los
recuentos de rutas y aserciones matemáticas no certifican todas las posibles
semillas ni sustituyen esa validación.

Al integrar un nuevo bioma hay que incluirlo en `WalkableExploration.Configure`,
`Enabled`, el rango de candidatos y la comprobación de rutas. Un menú de Tools
o un botón de TP por sí solos no completan la integración en el mundo.

Resultado local R25: 1.807.442.803 aserciones matemáticas, 188 huellas anteriores
conservadas y 224 archivos C# en cuatro configuraciones. Los 82 mapas PBR base
y los medios de lluvia pasan sus auditores; los tres archivos de linterna
siguen idénticos a la v39 recuperada. No son pruebas nativas de Unity.
