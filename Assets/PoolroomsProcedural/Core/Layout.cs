using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum RoomKind { Colonnade, Arcade, Lagoon, Rotunda, Meander, Waterpark, Atrium }
    public enum Direction { North, East, South, West }
    public enum LayoutStyle { Maze, Galleries, Districts, Network, BySeed }
    public enum CorridorStyle { Mixed, Straight, Curved, Serpentine, WideGallery, Arcade, BarrelVault, Colonnade, Narrow,
        Coffered, PortalSequence, OffsetScreens, PipeGallery, LightSlots, DoubleColonnade }

    public static class Directions
    {
        public static readonly int[] DX = { 0, 1, 0, -1 };
        public static readonly int[] DZ = { 1, 0, -1, 0 };
        public static int Opposite(int d) { return (d + 2) & 3; }
    }

    // Local integer PRNG: never touches UnityEngine.Random or System.Random.
    public struct StableRandom
    {
        private uint state;
        public StableRandom(uint seed) { state = seed == 0 ? 0x9e3779b9u : seed; }
        public uint Next()
        {
            uint x = state; x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state = x; return x;
        }
        public int Range(int max) { return (int)(Next() % (uint)max); }
        public float Value() { return (Next() >> 8) * (1f / 16777216f); }
        public float Range(float min, float max) { return min + (max - min) * Value(); }
        public static uint Hash(int seed, int x, int z, uint salt = 0)
        {
            unchecked
            {
                uint h = (uint)seed ^ salt ^ 0x85ebca6bu;
                h = (h ^ (uint)x) * 0x9e3779b1u;
                h = (h ^ (uint)z) * 0x85ebca77u;
                h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15;
                h *= 0x846ca68bu; return h ^ (h >> 16);
            }
        }
    }

    [Serializable]
    public sealed class LayoutConfig
    {
        public bool ExpandedVariety;
        public bool ExpandedBiomeAreas;
        public bool EnableRestfulLevels;
        public bool EnableSpecialRegions;
        public bool EnableJourneyLevels;
        public bool EnableHorizonLevels;
        public bool ExpandedLevelDetails;
        public bool LargeProceduralRegions;
        public int LargeRegionScale=8;
        // 1 preserves old placement budgets. 3 is +200%, never an unbounded multiplier.
        public int ObjectAbundance=1;
        public bool IndependentBiomeMaterials,MallDistricts;
        public bool DiscoveryCoverage;
        public bool EncounterExploration;
        public bool CommonPoolrooms;
        public float CommonSolarShare=.5f;
        public int CommonClimateRegionSize=3;
        public bool ConnectedTerrain;
        public float ConnectedTerrainShare=.25f;
        public bool ConnectedMalls;
        public float ConnectedMallShare=.6f;
        // Opt-in: old seeds remain byte-compatible until the profile is upgraded.
        public bool AutonomousGeneration;
        public bool EnableSoftPlay;
        public bool EnableCity11;
        public bool DreamCity11;
        public bool DetailedCityBuildings;
        public bool EnableProvisions;
        public bool EnableAircraft;
        public bool EnableImportedBuildings;
        public float City11Share=.15f;
        public bool EnableQuietLevels;
        public bool RebuildCourtyard188;
        public bool RebuildArchive906;
        public bool RebuildConservatory;
        public bool RebuildSky63;
        public bool EnableWheatFields10;
        public bool EnableForest39;
        public bool EnableCivicLevels;
        public bool EnableOrangerie;
        public bool EnableRainHouses;
        public float RainHouseShare=.1f;
        public bool EnableAquaticPark;
        public float AquaticParkShare=.12f;
        public bool EnableThermalBaths;
        public float ThermalBathShare=.11f;
        public float OrangerieShare=.14f;
        public float CivicLevelShare=.12f;
        public float QuietLevelShare=.18f;
        public float SoftPlayShare=.18f;
        public int Seed = 481516, Columns = 5, Rows = 5;
        public bool Continuous;
        public LiminalGeneration Procedural=LiminalGeneration.Authored;
        public ProceduralGrammar Grammar=ProceduralGrammar.CrossV12;
        public float SunlitRegionShare=.48f;
        public int PoolClimateRegionSize=5;
        public float DirectRoomJoinChance=.4f;
        public float SupermarketShare=.3f;
        public float AdditionalEnvironmentShare=.5f;
        public int EnvironmentRegionSize=8;
        public float IndoorPlayShare=.45f,AuthoredRoomChance=.12f;
        public float CellSize = 36, LoopChance = .45f;
        public LayoutStyle Distribution = LayoutStyle.Network;
        public int DistrictSize = 3;
        public CorridorStyle Corridors = CorridorStyle.Mixed;
        public float CorridorBend = 1, CorridorLightSpacing = 7;
        public float[] CorridorWeights = { .8f, 1.7f, 1.5f, 1, 1.5f, 1.5f, 1, .7f, 1,1,.8f,.6f,1,.8f };
        public float DoorWidth = 4.4f, DoorHeight = 4.8f;
        public float WallThickness = .3f, WaterLevel = -.08f;
        public int CurveSegments = 32;
        public float TextureWorldSize = 1.2f;
        public float[] Weights = { 3, 2, 2, 1.5f, 2, .8f, 1 };
        public ZoneMode Zones = ZoneMode.Legacy;
        public int SingleZone = 0, ZoneFamilyFilter = -1;
        public SpatialComposition Composition = SpatialComposition.None;
        public LightingDistribution Lighting=LightingDistribution.ZoneDefaults;
        public ZoneLightStyle UniformLighting=ZoneLightStyle.SoftDaylight;

        public void Validate()
        {
            if(LargeRegionScale<4||LargeRegionScale>12)throw new ArgumentOutOfRangeException(nameof(LargeRegionScale));
            if(ObjectAbundance<1||ObjectAbundance>3)throw new ArgumentOutOfRangeException(nameof(ObjectAbundance));
            if(!Finite(RainHouseShare)||RainHouseShare<0||RainHouseShare>1)throw new ArgumentOutOfRangeException(nameof(RainHouseShare));
            if(!Finite(AquaticParkShare)||AquaticParkShare<0||AquaticParkShare>1)throw new ArgumentOutOfRangeException(nameof(AquaticParkShare));
            if(!Finite(ThermalBathShare)||ThermalBathShare<0||ThermalBathShare>1)throw new ArgumentOutOfRangeException(nameof(ThermalBathShare));
            if(!Finite(OrangerieShare)||OrangerieShare<0||OrangerieShare>1)throw new ArgumentOutOfRangeException(nameof(OrangerieShare));
            if(!Finite(CivicLevelShare)||CivicLevelShare<0||CivicLevelShare>1)throw new ArgumentOutOfRangeException(nameof(CivicLevelShare));
            if(!Finite(QuietLevelShare)||QuietLevelShare<0||QuietLevelShare>1)throw new ArgumentOutOfRangeException(nameof(QuietLevelShare));
            if(!Finite(City11Share)||City11Share<0||City11Share>1)throw new ArgumentOutOfRangeException(nameof(City11Share));
            if(!Finite(CommonSolarShare)||CommonSolarShare<0||CommonSolarShare>1||CommonClimateRegionSize<2||CommonClimateRegionSize>24)throw new ArgumentOutOfRangeException("Common Poolrooms mix requires a share 0..1 and region size 2..24.");
            if(!Finite(ConnectedTerrainShare)||ConnectedTerrainShare<0||ConnectedTerrainShare>1)throw new ArgumentOutOfRangeException("Connected terrain density must be 0..1.");
            if(!Finite(ConnectedMallShare)||ConnectedMallShare<0||ConnectedMallShare>1)throw new ArgumentOutOfRangeException("Connected mall density must be 0..1.");
            if (Columns < 1 || Rows < 1 || Columns > 64 || Rows > 64)
                throw new ArgumentOutOfRangeException("Grid must be 1..64 per axis.");
            if (!Finite(CellSize) || CellSize < 28 || CellSize > 60)
                throw new ArgumentOutOfRangeException("CellSize must be 28..60 metres.");
            if (!Finite(DoorWidth) || DoorWidth < 3 || DoorWidth > 6 ||
                !Finite(DoorHeight) || DoorHeight < DoorWidth * .5f + 2 || DoorHeight > 5.5f)
                throw new ArgumentOutOfRangeException("Door must have at least 2 m straight headroom.");
            if (!Finite(WallThickness) || WallThickness < .15f || WallThickness > .6f ||
                !Finite(WaterLevel) || WaterLevel < -.18f || WaterLevel > -.03f ||
                !Finite(TextureWorldSize) || TextureWorldSize < .1f || TextureWorldSize > 10)
                throw new ArgumentOutOfRangeException("Invalid wall, water or UV scale.");
            if (!Finite(LoopChance) || LoopChance < 0 || LoopChance > 1 ||
                CurveSegments < 12 || CurveSegments > 96) throw new ArgumentOutOfRangeException("Quality/topology out of range.");
            if (!Enum.IsDefined(typeof(LayoutStyle), Distribution) || !Enum.IsDefined(typeof(CorridorStyle), Corridors) ||
                DistrictSize < 2 || DistrictSize > 8 || !Finite(CorridorBend) || CorridorBend < 0 || CorridorBend > 2 ||
                !Finite(CorridorLightSpacing) || CorridorLightSpacing < 3 || CorridorLightSpacing > 16)
                throw new ArgumentOutOfRangeException("Invalid distribution or corridor settings.");
            if(!Finite(SunlitRegionShare)||SunlitRegionShare<0||SunlitRegionShare>1||PoolClimateRegionSize<2||PoolClimateRegionSize>24||!Finite(DirectRoomJoinChance)||DirectRoomJoinChance<0||DirectRoomJoinChance>1)
                throw new ArgumentOutOfRangeException("Invalid solar architecture settings.");
            if(!Finite(SoftPlayShare)||SoftPlayShare<0||SoftPlayShare>1)throw new ArgumentOutOfRangeException(nameof(SoftPlayShare));
            if(!Finite(AdditionalEnvironmentShare)||AdditionalEnvironmentShare<0||AdditionalEnvironmentShare>1||!Finite(SupermarketShare)||SupermarketShare<0||SupermarketShare>1||!Enum.IsDefined(typeof(ProceduralGrammar),Grammar)||!Enum.IsDefined(typeof(LiminalGeneration),Procedural)||EnvironmentRegionSize<3||EnvironmentRegionSize>32||
                !Finite(IndoorPlayShare)||IndoorPlayShare<0||IndoorPlayShare>1||!Finite(AuthoredRoomChance)||AuthoredRoomChance<0||AuthoredRoomChance>1)
                throw new ArgumentOutOfRangeException("Invalid liminal generation settings.");
            if(!Enum.IsDefined(typeof(SpatialComposition),Composition))
                throw new ArgumentOutOfRangeException("Invalid spatial composition.");
            if(!Enum.IsDefined(typeof(LightingDistribution),Lighting)||!Enum.IsDefined(typeof(ZoneLightStyle),UniformLighting))
                throw new ArgumentOutOfRangeException("Invalid lighting selection.");
            // Old profiles with eight weights retain exactly their original distribution.
            ValidateWeights(CorridorWeights, CorridorWeights!=null&&CorridorWeights.Length==8?8:14, "corridor");
            ValidateWeights(Weights, 7, "room");
            if(!Enum.IsDefined(typeof(ZoneMode),Zones)||SingleZone<0||SingleZone>=ZoneCatalog.Count||ZoneFamilyFilter<-1||ZoneFamilyFilter>=ZoneCatalog.FamilyCount)
                throw new ArgumentOutOfRangeException("Invalid zone catalogue selection.");
        }
        private static void ValidateWeights(float[] weights, int count, string label)
        {
            if (weights == null || weights.Length != count) throw new ArgumentException(count + " " + label + " weights required.");
            float sum = 0;
            foreach (float w in weights) { if (!Finite(w) || w < 0) throw new ArgumentException("Invalid weight."); sum += w; }
            if (!Finite(sum) || sum <= 0) throw new ArgumentException("Enable at least one " + label + " family with finite total weight.");
        }
        private static bool Finite(float v) { return !float.IsNaN(v) && !float.IsInfinity(v); }
    }

    public sealed class Room
    {
        public int Id, X, Z, Exits;
        public int ZoneId = -1;
        public int ExpansionProgram;
        public LiminalEnvironment Environment;
        public ProceduralPlan Plan;
        public bool MallGateway,TerrainGateway;
        public SpatialRole Role;
        public int RouteDepth = -1, ArrivalDirection = -1, InvitationDirection = -1;
        public RoomKind Kind;
        public float Width, Depth, Height;
        public uint DetailSeed;
        public bool HasExit(int d) { return (Exits & (1 << d)) != 0; }
        public string DisplayName { get { return Plan!=null&&Plan.Solar!=null?SolarCatalog.Get(Plan.Solar.Recipe).Name+" / "+X+", "+Z:Plan!=null?InteriorPlanner.Name(Environment)+" / "+X+", "+Z:ZoneId>=0?ZoneCatalog.Get(ZoneId).Code+" / "+ZoneCatalog.Get(ZoneId).Name:Kind.ToString(); } }
        public bool IsDeep { get { return Plan!=null?Plan.Pools.Length>0:ZoneId>=0?ZoneCatalog.Get(ZoneId).Pools!=PoolPlan.Shallow&&ZoneCatalog.Get(ZoneId).Pools!=PoolPlan.Dry:
            Kind == RoomKind.Lagoon || Kind == RoomKind.Waterpark || Kind == RoomKind.Atrium; } }
        public float Floor { get { return Plan!=null?0:ZoneId>=0?ZoneCatalog.Get(ZoneId).Floor:IsDeep ? 0 : -.26f; } }
    }

    public sealed class Layout
    {
        public readonly LayoutConfig Config;
        public readonly Room[] Rooms;
        public readonly LayoutStyle Distribution;
        private readonly object cacheLock=new object();
        private readonly Dictionary<long,Room> neighbourhood=new Dictionary<long,Room>();
        private readonly List<long> retiredCells=new List<long>();
        private readonly ContinuousTopology continuousTopology;
        public int CachedRoomCount { get { lock(cacheLock)return neighbourhood.Count; } }
        public int CachedTileCount { get { lock(cacheLock)return continuousTopology!=null?continuousTopology.Count:0; } }
        public Layout(LayoutConfig config)
        {
            config.Validate(); Config = config; Distribution = Topology.Resolve(config);
            if(config.Continuous)
            {
                continuousTopology=new ContinuousTopology(config);
                Rooms=new[]{At(0,0)};return; // Anchor only, not a precomputed giant map.
            }
            Rooms = new Room[config.Columns * config.Rows];
            int[] zoneIds=ZoneCatalog.Assign(config,Rooms.Length);
            for (int z = 0; z < config.Rows; z++) for (int x = 0; x < config.Columns; x++)
            {
                int id = z * config.Columns + x;
                var random = new StableRandom(StableRandom.Hash(config.Seed, x, z));
                RoomKind kind = Pick(config.Weights, ref random);
                // Starting room is shallow and has a clear central spawn.
                if (id == 0) kind = RoomKind.Colonnade;
                Rooms[id] = new Room { Id = id, X = x, Z = z, Kind = kind,
                    Width = random.Range(20.5f, 23.5f), Depth = random.Range(20.5f, 23.5f),
                    Height = (kind == RoomKind.Atrium || kind == RoomKind.Waterpark) ? 9f : random.Range(6f, 7.4f),
                    DetailSeed = random.Next() };
                Rooms[id].ZoneId=zoneIds[id];
                if(zoneIds[id]>=0)Rooms[id].Height=Math.Max(config.DoorHeight+.6f,ZoneCatalog.Get(zoneIds[id]).Height);
            }
            Topology.Generate(this);
            if(config.RebuildCourtyard188)foreach(var room in Rooms)
                for(int side=0;side<4;side++)if(CourtyardDistrictPlanner.Same(config,room.X,room.Z,side))room.Exits|=1<<side;
            if(config.RebuildArchive906)foreach(var room in Rooms)
                for(int side=0;side<4;side++)if(ArchiveDistrictPlanner.Same(config,room.X,room.Z,side))room.Exits|=1<<side;
            if(config.EnableWheatFields10||config.Procedural==LiminalGeneration.WheatFields10)foreach(var room in Rooms)
                for(int side=0;side<4;side++)if(Level10Planner.Same(config,room.X,room.Z,side))room.Exits|=1<<side;
            if(config.EnableForest39||config.Procedural==LiminalGeneration.EnchantedForest39)foreach(var room in Rooms)
                for(int side=0;side<4;side++)if(Forest39Planner.Same(config,room.X,room.Z,side))room.Exits|=1<<side;
            if(config.RebuildSky63)foreach(var room in Rooms)
                for(int side=0;side<4;side++)if(Sky63Planner.Same(config,room.X,room.Z,side))room.Exits|=1<<side;
            if(config.RebuildConservatory)foreach(var room in Rooms)
                for(int side=0;side<4;side++)if(ConservatoryDistrictPlanner.Same(config,room.X,room.Z,side))room.Exits|=1<<side;
            foreach(Room room in Rooms)ProceduralPlanner.Describe(room,config);
            SpaceComposition.Apply(this);
        }
        private static RoomKind Pick(float[] weights, ref StableRandom random)
        {
            float sum = 0; foreach (float w in weights) sum += w;
            float target = random.Value() * sum;
            for (int i = 0; i < weights.Length; i++) { target -= weights[i]; if (target < 0) return (RoomKind)i; }
            return RoomKind.Atrium;
        }
        public Room At(int x, int z)
        {
            if(!Config.Continuous)return x < 0 || z < 0 || x >= Config.Columns || z >= Config.Rows ? null : Rooms[z * Config.Columns + x];
            lock(cacheLock)
            {
                long key=ContinuousTopology.CellKey(x,z);Room room;
                if(neighbourhood.TryGetValue(key,out room))return room;
                room=DescribeAt(x,z);
                if(Config.Composition!=SpatialComposition.None)SpaceComposition.Invite(room,this);
                neighbourhood.Add(key,room);return room;
            }
        }
        // Raw neighbours avoid recursive invitation calculations; coordinates completely describe a room.
        public Room DescribeAt(int x,int z)
        {
            if(!Config.Continuous)return At(x,z);
            lock(cacheLock)
            {
                Room cached;if(neighbourhood.TryGetValue(ContinuousTopology.CellKey(x,z),out cached))return cached;
                var random=new StableRandom(StableRandom.Hash(Config.Seed,x,z));
                RoomKind kind=Pick(Config.Weights,ref random);if(x==0&&z==0)kind=RoomKind.Colonnade;
                var room=new Room{Id=(int)(StableRandom.Hash(Config.Seed,x,z,0x1Du)&0x3fffffffu),X=x,Z=z,Kind=kind,
                    Width=random.Range(20.5f,23.5f),Depth=random.Range(20.5f,23.5f),
                    Height=kind==RoomKind.Atrium||kind==RoomKind.Waterpark?9:random.Range(6f,7.4f),DetailSeed=random.Next()};
                room.ZoneId=Config.Zones==ZoneMode.Legacy?-1:Config.Zones==ZoneMode.Single?Config.SingleZone:
                    Config.Zones==ZoneMode.Mixed&&StableRandom.Hash(Config.Seed,x,z,0x81u)%8==0?-1:ZoneCatalog.RandomAt(Config,x,z);
                if(room.ZoneId>=0)room.Height=Math.Max(Config.DoorHeight+.6f,ZoneCatalog.Get(room.ZoneId).Height);
                room.Exits=ConnectedTerrainTopology.Exits(Config,x,z,ConnectedMallTopology.Exits(Config,x,z,continuousTopology.Exits(x,z)));
                ProceduralPlanner.Describe(room,Config);
                ConnectedMallSite gate;
                if(ConnectedMallTopology.Gateway(Config,x,z,out gate))
                {
                    room.MallGateway=true;room.ZoneId=-1;room.Environment=LiminalEnvironment.Supermarket;
                    room.Width=room.Depth=Math.Min(22,Config.CellSize-6);room.Height=Math.Max(6,Config.DoorHeight+.6f);
                    room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],Config.DoorWidth*.5f+.8f,0,3);
                }
                ConnectedTerrainSite terrainGate;
                if(ConnectedTerrainTopology.Gateway(Config,x,z,out terrainGate))
                {
                    room.TerrainGateway=true;room.ZoneId=-1;room.Environment=LiminalEnvironment.WaitingHall;
                    room.Width=room.Depth=Math.Min(22,Config.CellSize-6);room.Height=Math.Max(6,Config.DoorHeight+.6f);
                    room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],Config.DoorWidth*.5f+.8f,0,0);
                }
                if(Config.Composition!=SpatialComposition.None)SpaceComposition.Describe(room);
                return room;
            }
        }
        public void TrimNeighbourhood(int x,int z,int radius)
        {
            if(!Config.Continuous)return;
            lock(cacheLock)
            {
                retiredCells.Clear();
                foreach(var pair in neighbourhood)
                    if(Math.Abs((long)pair.Value.X-x)>radius||Math.Abs((long)pair.Value.Z-z)>radius)retiredCells.Add(pair.Key);
                foreach(long key in retiredCells)neighbourhood.Remove(key);
                retiredCells.Clear();continuousTopology.Trim(x,z,radius);
            }
        }
        public Room Neighbour(Room r, int d) { return At(r.X + Directions.DX[d], r.Z + Directions.DZ[d]); }
    }
}
