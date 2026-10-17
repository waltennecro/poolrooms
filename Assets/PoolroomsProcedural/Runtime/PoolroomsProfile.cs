using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [CreateAssetMenu(menuName="Poolrooms/Generation Profile",fileName="PoolroomsProfile")]
    public sealed class PoolroomsProfile : ScriptableObject
    {
        [Header("Nivel 11 · reconstrucción incremental")]
        public bool enableCity11=true;
        [Range(0,.5f)] public float city11Share=.15f;
        [Header("Biblioteca 906 · Patio 188 · Costa 48")]
        public bool enableQuietLevels=true;
        [Range(0,.5f)] public float quietLevelShare=.18f;
        [Header("Áreas infantiles multinivel / v39")]
        [Tooltip("Bioma independiente del infantil anterior. Aparece como regiones amplias en Expanded.")] public bool enableSoftPlay=true;
        [Range(0,.5f),Tooltip("Probabilidad por región; conserva los juegos interiores anteriores.")] public float softPlayShare=.18f;
        [Header("Poolrooms originales + solares / v30")]
        [InspectorName("Mezclar arquitectura original y solar")]
        [Tooltip("Integra piscinas solares, salas amplias, arcos y uniones directas en la generación habitual, incluso con arquitectura Authored. Single/Catalog y filtros de familia conservan las vistas de recetas.")]
        public bool commonPoolrooms=true;
        [InspectorName("Proporción de Poolrooms solares"),Range(0,1)] public float commonSolarShare=.5f;
        [InspectorName("Tamaño de regiones solares comunes"),Range(2,24)] public int commonClimateRegionSize=3;
        [Header("Distritos conectados / v29")]
        [InspectorName("Nivel 94 conectado")] public bool connectedTerrainGeneration=true;
        [Tooltip("Probabilidad por región espacial de 192 celdas. Reserva terreno sin solaparlo con edificios. Hay un primer distrito al sur del inicio; los siguientes dependen de la semilla.")]
        [Range(0,1)] public float connectedTerrainShare=.25f;
        [InspectorName("Centros comerciales conectados")] public bool connectedMallGeneration=true;
        [Tooltip("Probabilidad por bloque espacial de unos 24×24 celdas. Reserva el edificio completo y dos accesos físicos; 0 desactiva centros comerciales conectados.")]
        [InspectorName("Frecuencia de centros comerciales"),Range(0,1)] public float connectedMallShare=.6f;
        [Header("Mundo continuo / exploración")]
        [Tooltip("Genera más salas al explorar en cualquier dirección. Columns/Rows solo limitan el modo finito. Se mantiene una ventana de streaming y un origen flotante.")]
        public bool continuousWorld;
        [Header("Centros comerciales 2010 / v16")]
        [Tooltip("Modo anterior: entradas por portal a un centro de 2–6 pisos. Exploración unificada v28 añade edificios físicos y lo activa automáticamente al jugar.")]
        public bool enableGiantMalls;
        [Header("Centro comercial completo / v20")]
        public bool enhancedMallArchitecture=true;
        [Tooltip("Atrios variables, puentes y mobiliario detallado v21. Requiere arquitectura mejorada.")]
        public bool refinedMallArchitecture=true;
        [Tooltip("Sustituye las salas de estantes por vestíbulos de entrada a centros comerciales completos. Los supermercados anteriores siguen disponibles al desactivarlo.")]
        public bool mallDistricts;
        public bool independentBiomeMaterials=true;
        public PoolroomsBiomeMaterials biomeMaterials;
        [Range(0,1)] public float mallEntranceChance=.2f;
        [Tooltip("Columnas detalladas próximas, más una planta arriba/abajo. La estructura completa permanece cargada.")]
        [Range(1,3)] public int mallDetailRadius=1;
        [Header("Colinas exteriores / Nivel 94 v24")]
        public bool enableTerrainRegions;
        [Range(0,1)] public float terrainEntranceChance=.08f;
        [Range(2,4)] public int terrainViewRadius=3;
        [Header("Generación autónoma / v22")]
        [Tooltip("Compone muebles y distribuciones por semilla. Desde v37 subdivide interiores con galerías y alas traseras; el Nivel 94 genera sus propias plantas. Exploración unificada lo activa al jugar; conserva rutas, presupuestos y materiales.")]
        public bool autonomousGeneration;
        [Header("Generador liminal / diseños nuevos")]
        [Tooltip("Authored conserva recetas. Poolrooms/IndoorPlay/Mixed mantienen los ambientes anteriores. Supermarkets construye tiendas completas; MixedWithRetail combina los tres ambientes sin orden fijo. Expanded añade oficinas, hoteles, salas de espera y comedores.")]
        public LiminalGeneration liminalGeneration=LiminalGeneration.Authored;
        [Tooltip("CrossV12 conserva v12; Varied añade recodos y patios. SolarAtlas construye 768 recetas nuevas de Poolrooms, luz solar regional, contornos curvos y salas unidas directamente; conserva las demás ambientaciones.")]
        public ProceduralGrammar proceduralGrammar=ProceduralGrammar.CrossV12;
        [Header("Poolrooms solares / atlas v19")]
        [Tooltip("Proporción del atlas anterior, cuando Mezclar arquitectura original y solar está desactivado. Con la mezcla activa usa Proporción de Poolrooms solares.")]
        [Range(0,1)] public float sunlitRegionShare=.48f;
        [Tooltip("Tamaño del atlas anterior. Con la mezcla activa usa Tamaño de regiones solares comunes.")]
        [Range(2,24)] public int poolClimateRegionSize=5;
        [Tooltip("Probabilidad de unir dos Poolrooms del atlas mediante una abertura compartida, sin túnel. Las otras ambientaciones conservan sus conectores.")]
        [Range(0,1)] public float directRoomJoinChance=.4f;
        [Range(3,32)] public int environmentRegionSize=8;
        [Range(0,1)] public float indoorPlayShare=.45f;
        [Tooltip("Solo MixedWithRetail: probabilidad de región supermercado. Indoor Play Share reparte los otros dos ambientes entre las regiones restantes.")]
        [Range(0,1)] public float supermarketShare=.3f;
        [Tooltip("Solo Expanded: proporción de regiones nuevas; se reparte entre oficinas, hoteles, salas de espera y comedores. El resto conserva los pesos anteriores.")]
        [Range(0,1)] public float additionalEnvironmentShare=.5f;
        [Tooltip("Probabilidad de conservar una receta conocida dentro de regiones Poolrooms. No afecta a los juegos interiores.")]
        [Range(0,1)] public float authoredRoomChance=.12f;
        [Header("Layout / metres")]
        public int seed=481516;
        [Range(1,64)] public int columns=5,rows=5;
        [Range(28,60)] public float cellSize=36;
        [Tooltip("Maze: laberinto. Galleries: galerías largas. Districts: sectores. Network: más cruces. BySeed: alterna por semilla.")]
        public LayoutStyle distribution=LayoutStyle.Network;
        [Tooltip("0: árbol conectado. 1: todas las conexiones posibles. Los valores intermedios dependen de Distribution.")]
        [Range(0,1)] public float loopChance=.45f;
        [Range(2,8)] public int districtSize=3;
        [Range(3,6)] public float doorWidth=4.4f;
        [Range(4,5.5f)] public float doorHeight=4.8f;
        [Range(.15f,.6f)] public float wallThickness=.3f;
        [Range(-.18f,-.03f)] public float waterLevel=-.08f;
        [Header("Composición / ritmo del recorrido")]
        [Tooltip("Exploration: adapta pasillos y luz a las salas vecinas, sin etapas ni ruta principal. None conserva los enlaces originales.")]
        public SpatialComposition composition=SpatialComposition.None;
        [HideInInspector] public LightingDistribution compositionPreviousLighting=LightingDistribution.ZoneDefaults;
        [HideInInspector] public bool compositionRemembersLighting;
        [Header("Corridors / pasillos")]
        public CorridorStyle corridorStyle=CorridorStyle.Mixed;
        [Tooltip("Pesos: Straight / Curved / Serpentine / WideGallery / Arcade / BarrelVault / Colonnade / Narrow / Coffered / PortalSequence / OffsetScreens / PipeGallery / LightSlots / DoubleColonnade. Los perfiles antiguos con 8 pesos siguen siendo válidos.")]
        public float[] corridorWeights={.8f,1.7f,1.5f,1,1.5f,1.5f,1,.7f,1,1,.8f,.6f,1,.8f};
        [Tooltip("Curvatura solicitada. Se limita según longitud y ancho para evitar que las paredes se crucen.")]
        [Range(0,2)] public float corridorBend=1;
        [Range(3,16)] public float corridorLightSpacing=7;
        [Header("Architecture weights: Colonnade / Arcade / Lagoon / Rotunda / Meander / Waterpark / Atrium")]
        public float[] roomWeights={3,2,2,1.5f,2,.8f,1};
        [Header("170 zonas / arquitectura ampliada")]
        [Tooltip("Random: selección independiente por coordenadas, sin ciclo. Catalog: muestra todas las recetas antes de repetir. Mixed: conserva una sala clásica por cada ocho. Single: prueba una zona. Legacy: las siete familias originales.")]
        public ZoneMode zoneMode=ZoneMode.Random;
        [Header("Atmósfera por zona")]
        [Tooltip("Composed: luz aleatoria por coordenada y transiciones locales. ZoneDefaults: ambiente propio de cada receta. Seeded: varía con semilla. Uniform: un ambiente. Classic: luz de v5.")]
        public LightingDistribution lighting=LightingDistribution.ZoneDefaults;
        public ZoneLightStyle uniformLighting=ZoneLightStyle.SoftDaylight;
        [Tooltip("Índice 0–169 (PR-001 a PR-170). Solo se usa en Single. También puedes elegir por nombre en Tools > Poolrooms > Catálogo de 170 zonas.")]
        [Range(0,169)] public int singleZone;
        [Tooltip("-1: todas. 0–29: una familia del catálogo. No afecta al modo Single ni a las salas clásicas.")]
        [Range(-1,29)] public int zoneFamilyFilter=-1;
        [Range(12,96)] public int curveSegments=32;
        [Tooltip("Metres covered by one repetition of the whole tile texture (8 tiles).")]
        [Range(.1f,10)] public float textureWorldSize=1.2f;
        [Header("PBR materials (shared across rooms)")]
        public Material tile,accent,plaster,metal,glow,slide,waterPreview;
        [Header("Streaming / lighting budgets")]
        [Range(1,4)] public int loadRadius=1;
        [Range(2,5)] public int unloadRadius=2;
        [Range(1,48)] public int maxActiveLights=18;
        [Range(0,8)] public int maxShadowLights=4;
        public bool colliders=true;
        public LayoutConfig Snapshot()
        {
            var c=new LayoutConfig{EnableQuietLevels=enableQuietLevels,QuietLevelShare=quietLevelShare,EnableCity11=enableCity11,City11Share=city11Share,EnableSoftPlay=enableSoftPlay,SoftPlayShare=softPlayShare,CommonPoolrooms=commonPoolrooms,CommonSolarShare=commonSolarShare,CommonClimateRegionSize=commonClimateRegionSize,ConnectedTerrain=connectedTerrainGeneration&&enableTerrainRegions&&continuousWorld,ConnectedTerrainShare=connectedTerrainShare,ConnectedMalls=connectedMallGeneration&&continuousWorld,ConnectedMallShare=connectedMallShare,AutonomousGeneration=autonomousGeneration,IndependentBiomeMaterials=independentBiomeMaterials,MallDistricts=mallDistricts,Seed=seed,Columns=columns,Rows=rows,Continuous=continuousWorld,Procedural=liminalGeneration,Grammar=proceduralGrammar,SunlitRegionShare=sunlitRegionShare,PoolClimateRegionSize=poolClimateRegionSize,DirectRoomJoinChance=directRoomJoinChance,SupermarketShare=supermarketShare,AdditionalEnvironmentShare=additionalEnvironmentShare,EnvironmentRegionSize=environmentRegionSize,
                IndoorPlayShare=indoorPlayShare,AuthoredRoomChance=authoredRoomChance,CellSize=cellSize,LoopChance=loopChance,
                Distribution=distribution,DistrictSize=districtSize,Corridors=corridorStyle,
                CorridorBend=corridorBend,CorridorLightSpacing=corridorLightSpacing,
                CorridorWeights=corridorWeights==null?null:(float[])corridorWeights.Clone(),
                DoorWidth=doorWidth,DoorHeight=doorHeight,WallThickness=wallThickness,WaterLevel=waterLevel,
                CurveSegments=curveSegments,TextureWorldSize=textureWorldSize,Weights=roomWeights==null?null:(float[])roomWeights.Clone(),
                Composition=composition,Lighting=lighting,UniformLighting=uniformLighting,Zones=zoneMode,SingleZone=singleZone,ZoneFamilyFilter=zoneFamilyFilter};
            c.Validate();return c;
        }
        public Material MaterialFor(int surface)
        {
            switch((Surface)surface){case Surface.Accent:return accent;case Surface.Plaster:return plaster;
                case Surface.Metal:return metal;case Surface.Glow:return glow;case Surface.Slide:return slide;default:return tile;}
        }
    }
}
