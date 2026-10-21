using System;
using System.Collections.Generic;
namespace Poolrooms.Core
{
    public enum StationKind
    {
        Tea,Water,Books,Postcards,Sketch,Chimes,MusicBox,Light,Parcels,SlidingPuzzle,Mosaic,RestBell,
        LanternCircuit,WaterChannels,Weaving,Herbarium,Sundial,ParcelBalance,
        StarChart,BellSequence,TideLocks,Bookbinding,WindRose,SeedNursery
    }
    public enum StationSound { Cup,Pour,Page,Paper,Pencil,Chime,Music,Latch,Parcel,Tile,Switch,Fabric,WaterDrop,Leaves,Duct,Ceramic }
    public readonly struct StationPlacement
    {
        public readonly DressingAnchor Anchor;
        public readonly StationKind Kind;
        public readonly int Slot,Variant;
        public StationPlacement(DressingAnchor anchor,StationKind kind,int slot,int variant){Anchor=anchor;Kind=kind;Slot=slot;Variant=variant;}
    }
    public static class ImmersionStations
    {
        public const int MaximumPerRoom=4;
        private static readonly string[] names={"La pausa del té","Fuente de agua fresca","Intercambio de libros","Postales del camino","Pizarra de colores","Campanillas del jardín","Caja de música","Luz de bienvenida","Paquetes para viajeros","Rompecabezas deslizante","Mosaico de la tarde","Campana del descanso"};
        public static string Name(StationKind kind){return WorkshopCatalog.IsWorkshop(kind)?WorkshopCatalog.Name(kind):names[(int)kind];}
        private static readonly StationSound[] sounds={StationSound.Cup,StationSound.Pour,StationSound.Page,StationSound.Paper,StationSound.Pencil,StationSound.Chime,StationSound.Music,StationSound.Switch,StationSound.Parcel,StationSound.Tile,StationSound.Ceramic,StationSound.Chime};
        public static StationSound Sound(StationKind kind){return WorkshopCatalog.IsWorkshop(kind)?WorkshopCatalog.Sound(kind):sounds[(int)kind];}
        private static readonly StationKind[] waterBank={StationKind.Water,StationKind.Mosaic,StationKind.RestBell,StationKind.Postcards,StationKind.Light,StationKind.SlidingPuzzle};
        private static readonly StationKind[] gardenBank={StationKind.Tea,StationKind.Chimes,StationKind.Sketch,StationKind.Books,StationKind.Postcards,StationKind.Mosaic};
        private static readonly StationKind[] commercialBank={StationKind.Parcels,StationKind.Water,StationKind.Postcards,StationKind.MusicBox,StationKind.Light,StationKind.SlidingPuzzle};
        private static readonly StationKind[] quietBank={StationKind.Books,StationKind.Tea,StationKind.Sketch,StationKind.MusicBox,StationKind.SlidingPuzzle,StationKind.Mosaic,StationKind.Parcels,StationKind.Chimes};
        public static StationKind Choose(LiminalEnvironment environment,uint hash)
        {
            if(hash%3==0)return WorkshopCatalog.Choose(environment,hash/3);
            if(JourneyLevelPlanner.IsLevel(environment))return JourneyLevelPlanner.Activity(environment,hash);
            if(SpecialRegionPlanner.IsLevel(environment))return SpecialRegionPlanner.Activity(environment,hash);
            StationKind[] bank;
            switch(environment)
            {
                case LiminalEnvironment.Poolrooms:case LiminalEnvironment.AquaticPark:case LiminalEnvironment.ThermalBaths:bank=waterBank;break;
                case LiminalEnvironment.Orangerie:case LiminalEnvironment.Conservatory:case LiminalEnvironment.WinterGarden:case LiminalEnvironment.TeaPavilions:bank=gardenBank;break;
                case LiminalEnvironment.Supermarket:case LiminalEnvironment.City11:case LiminalEnvironment.TransitGallery:bank=commercialBank;break;
                default:bank=quietBank;break;
            }
            return bank[hash%(uint)bank.Length];
        }
        public static List<StationPlacement> Plan(Room room,RoomGeometry geometry,RoomLifePlan life,LayoutConfig config)
        {
            var result=new List<StationPlacement>();if(!config.EncounterExploration||geometry.Dressing==null)return result;
            for(int index=0;index<geometry.Dressing.Anchors.Count&&result.Count<MaximumPerRoom;index++)
            {
                var a=geometry.Dressing.Anchors[index];if(a.Center.Y<room.Floor+.95f||a.Center.Y>room.Floor+1.75f||V3.Dot(a.Normal,new V3(-a.Center.X,0,-a.Center.Z))<=.1f)continue;
                if(life.Notice.HasValue&&(life.Notice.Value.Center-a.Center).Length<1.05f)continue;
                bool blocked=false;
                foreach(var prior in result)if((prior.Anchor.Center-a.Center).Length<1.1f)blocked=true;
                // The reach volume must also be clear of existing movable furniture.
                foreach(var prior in geometry.SourceProps)if(Blocks(a,prior))blocked=true;
                foreach(var prior in geometry.SourceSeats)if(Blocks(a,prior))blocked=true;
                foreach(var prior in life.Furniture)if(prior.Kind==LifeFurnishing.Source&&Blocks(a,new SourceFurniturePlacement(0,prior.Source,prior.Bottom,prior.Yaw)))blocked=true;
                var extra=room.Plan?.Restful?.Props??room.Plan?.Rain?.Props??room.Plan?.Aquatic?.Props??room.Plan?.Baths?.Props??room.Plan?.Garden?.Props??room.Plan?.Quiet?.Props;
                if(extra!=null)foreach(var prop in extra)
                {
                    V3 size;
                    if(prop.CatalogId>=0)size=MallItemCatalog.Get(prop.CatalogId).Size;
                    else
                    {
                        int source=-1;for(int i=0;i<SourceFurniture.Count;i++)if(SourceFurniture.Id(i)==prop.Source)source=i;
                        size=source>=0?SourceFurniture.Dimensions(source):new V3(1.3f,1.3f,1.3f);
                    }
                    var at=a.Center+a.Normal*.2f;float half=Math.Max(size.X,size.Z)*.5f;
                    if(Math.Abs(at.X-prop.Bottom.X)<half+.3f&&Math.Abs(at.Z-prop.Bottom.Z)<half+.3f&&at.Y+.22f>prop.Bottom.Y&&at.Y-.22f<prop.Bottom.Y+size.Y)blocked=true;
                }
                if(blocked)continue;
                uint h=LiminalRegions.Hash(config.Seed,room.X,room.Z,0x150AC0u+(uint)index);var kind=Choose(room.Environment,h);
                // Prefer distinct activities in the same room.
                for(int attempt=0;attempt<WorkshopCatalog.StationCount;attempt++){bool repeat=false;foreach(var prior in result)if(prior.Kind==kind)repeat=true;if(!repeat)break;kind=(StationKind)(((int)kind+1)%WorkshopCatalog.StationCount);}
                result.Add(new StationPlacement(a,kind,index,(int)((h>>8)%3)));
            }
            return result;
        }
        private static bool Blocks(DressingAnchor anchor,SourceFurniturePlacement p)
        {var half=SourceFurniture.HalfExtents(p.Source,p.Yaw,.12f);var at=anchor.Center+anchor.Normal*.2f;return Math.Abs(at.X-p.Bottom.X)<half.X+.3f&&Math.Abs(at.Z-p.Bottom.Z)<half.Z+.3f&&at.Y+.22f>p.Bottom.Y&&at.Y-.22f<p.Bottom.Y+SourceFurniture.Dimensions(p.Source).Y;}
        public static MeshData Model(StationKind kind,int variant)
        {
            if(WorkshopCatalog.IsWorkshop(kind))return WorkshopModels.Build(kind,variant);
            var m=new MeshData{Theme=SurfaceTheme.EncounterFurniture,EmissionStrength=0};
            void B(float x,float y,float z,float w,float h,float d,Surface surface=Surface.Accent){m.Box(new V3(x-w*.5f,y-h*.5f,z-d*.5f),new V3(x+w*.5f,y+h*.5f,z+d*.5f),surface,.4f);}
            B(0,0,.018f,.54f,.4f,.035f);foreach(int sign in new[]{-1,1}){B(sign*.252f,0,.047f,.026f,.39f,.035f,Surface.Metal);B(0,sign*.185f,.047f,.49f,.025f,.035f,Surface.Metal);}
            B(0,-.155f,.095f,.48f,.035f,.16f);
            switch(kind)
            {
                case StationKind.Tea:case StationKind.Water:
                    B(-.1f,.015f,.085f,.12f,.24f,.1f,Surface.Metal);ZoneShapes.Beam(m,new V3(-.1f,.08f,.14f),new V3(.01f,.08f,.14f),.012f,Surface.Metal,.4f,12);ZoneShapes.Beam(m,new V3(.01f,.08f,.14f),new V3(.01f,.025f,.14f),.012f,Surface.Metal,.4f,12);
                    // A licensed porcelain cup is attached by the runtime adapter.
                    for(int i=0;i<3+variant;i++)B(.07f,.055f+i*.025f,.047f,.16f,.009f,.012f,Surface.Plaster);break;
                case StationKind.Books:case StationKind.Postcards:case StationKind.Parcels:
                    for(int i=0;i<4+variant;i++){float x=-.19f+i*(.38f/(3+variant));B(x,-.015f,.075f,.048f,.18f+(i%2)*.045f,kind==StationKind.Parcels?.105f:.055f,i%2==0?Surface.Plaster:Surface.Slide);B(x,.02f,.11f,.035f,.009f,.012f,Surface.Metal);}break;
                case StationKind.Chimes:case StationKind.RestBell:
                    for(int i=0;i<3+variant;i++){float x=-.17f+i*(.34f/(2+variant));ZoneShapes.Beam(m,new V3(x,.15f,.08f),new V3(x,-.02f,.08f),.003f,Surface.Accent,.3f,6);ZoneShapes.Beam(m,new V3(x,-.02f,.08f),new V3(x,-.1f+(i%2)*.03f,.08f),kind==StationKind.RestBell?.022f:.012f,Surface.Metal,.3f,12);}break;
                case StationKind.MusicBox:
                    B(0,-.025f,.09f,.36f,.2f,.11f);for(int i=0;i<8;i++)B(-.135f+i*.038f,.01f,.15f,.017f,.12f,.012f,Surface.Metal);ZoneShapes.Beam(m,new V3(.19f,-.03f,.08f),new V3(.19f,-.03f,.16f),.012f,Surface.Metal,.4f,10);break;
                case StationKind.Light:
                    B(0,.01f,.07f,.18f,.23f,.08f,Surface.Plaster);B(0,-.04f,.13f,.07f,.07f,.035f,Surface.Metal);B(0,.065f,.118f,.1f,.06f,.014f,Surface.Slide);break;
                default:
                    int columns=kind==StationKind.Sketch?8:kind==StationKind.SlidingPuzzle?4:3;
                    for(int x=0;x<columns;x++)for(int y=0;y<columns;y++)B(-.21f+(x+.5f)*.42f/columns,-.135f+(y+.5f)*.27f/columns,.06f,.4f/columns,.25f/columns,.025f,(x+y+variant)%3==0?Surface.Slide:Surface.Plaster);break;
            }
            return m;
        }
    }
    public sealed class SlidingTiles
    {
        public readonly int[] Tiles=new int[16];public int Moves{get;private set;}
        public bool Complete {get{for(int i=0;i<15;i++)if(Tiles[i]!=i+1)return false;return Tiles[15]==0;}}
        public SlidingTiles(uint seed){for(int i=0;i<15;i++)Tiles[i]=i+1;var random=new StableRandom(seed);int previous=-1;for(int i=0;i<100;i++){int blank=Array.IndexOf(Tiles,0);var choices=new List<int>();for(int p=0;p<16;p++)if(p!=previous&&Adjacent(blank,p))choices.Add(p);int next=choices[random.Range(choices.Count)];Tiles[blank]=Tiles[next];Tiles[next]=0;previous=blank;}if(Complete)Slide(14);Moves=0;}
        public static bool Adjacent(int a,int b){return a>=0&&b>=0&&a<16&&b<16&&Math.Abs(a%4-b%4)+Math.Abs(a/4-b/4)==1;}
        public bool Slide(int at){int blank=Array.IndexOf(Tiles,0);if(!Adjacent(at,blank))return false;Tiles[blank]=Tiles[at];Tiles[at]=0;Moves++;return true;}
    }
    public sealed class MosaicTiles
    {
        public readonly int[] Turns=new int[9],Target=new int[9];public bool Complete{get{for(int i=0;i<9;i++)if(Turns[i]!=Target[i])return false;return true;}}
        public MosaicTiles(uint seed){var random=new StableRandom(seed);for(int i=0;i<9;i++){Target[i]=random.Range(4);Turns[i]=(Target[i]+1+random.Range(3))%4;}}
        public bool Turn(int index){if(index<0||index>=9)return false;Turns[index]=(Turns[index]+1)%4;return true;}
    }
}
