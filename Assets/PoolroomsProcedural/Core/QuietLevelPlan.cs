using System;

namespace Poolrooms.Core
{
    public readonly struct QuietPropPlan
    {
        public readonly V3 Bottom;
        public readonly int CatalogId;
        public readonly string Source;
        public readonly float Yaw;
        public QuietPropPlan(V3 bottom,int catalogId,string source=null,float yaw=0){Bottom=bottom;CatalogId=catalogId;Source=source;Yaw=yaw;}
    }
    public readonly struct PreparedTransition
    {
        public readonly LiminalEnvironment Destination;
        public readonly V3 Position;
        public bool Active {get{return false;}}
        public PreparedTransition(LiminalEnvironment destination,V3 position){Destination=destination;Position=position;}
    }
    public sealed class QuietLevelPlan
    {
        public readonly LiminalEnvironment Environment;
        public readonly int Floors,Variant;
        public readonly uint Seed;
        public readonly QuietPropPlan[] Props;
        public readonly WaterRegion Sea;
        public readonly float Shore;
        public readonly bool Pavilion;
        public readonly CourtyardDistrictPlan Courtyard;
        public readonly ArchiveDistrictPlan Archive;
        public readonly ConservatoryDistrictPlan Conservatory;
        public readonly Sky63Plan Sky63;
        public QuietLevelPlan(LiminalEnvironment environment,int floors,int variant,uint seed,QuietPropPlan[] props,WaterRegion sea,float shore=0,bool pavilion=false,CourtyardDistrictPlan courtyard=null,ArchiveDistrictPlan archive=null,ConservatoryDistrictPlan conservatory=null,Sky63Plan sky63=null)
        {Environment=environment;Floors=floors;Variant=variant;Seed=seed;Props=props;Sea=sea;Shore=shore;Pavilion=pavilion;Courtyard=courtyard;Archive=archive;Conservatory=conservatory;Sky63=sky63;}
        public bool Valid(Room room,LayoutConfig c)
        {return QuietLevelPlanner.IsLevel(Environment)&&room.Environment==Environment&&room.Width==c.CellSize&&room.Depth==c.CellSize&&Props!=null&&Props.Length<=12&&Floors>=1&&Floors<=5;}
        public bool Sheltered(float x,float z,float cellSize)
        {if(Sky63!=null)return false;if(Courtyard!=null)return !Courtyard.Court;return QuietLevelPlanner.IsCivic(Environment)||Environment==LiminalEnvironment.Archive906||Environment==LiminalEnvironment.Courtyard188&&(Math.Abs(x)>cellSize*.25f||z>cellSize*.25f);}
    }
    public static class QuietLevelPlanner
    {
        public static bool IsCivic(LiminalEnvironment e)
        {return e==LiminalEnvironment.Conservatory||e==LiminalEnvironment.TransitGallery;}
        public static bool IsLevel(LiminalEnvironment e)
        {return RestfulLevelPlanner.IsLevel(e)||e==LiminalEnvironment.RainHouses||e==LiminalEnvironment.AquaticPark||e==LiminalEnvironment.ThermalBaths||e==LiminalEnvironment.Orangerie||e==LiminalEnvironment.Archive906||e==LiminalEnvironment.Courtyard188||e==LiminalEnvironment.Coast48||IsCivic(e);}
        public static void Describe(Room room,LayoutConfig c)
        {
            if(Sky63Planner.Applies(c,room.Environment)){Sky63Planner.Describe(room,c);return;}
            if(ConservatoryDistrictPlanner.Applies(c,room.Environment)){ConservatoryDistrictPlanner.Describe(room,c);return;}
            if(ArchiveDistrictPlanner.Applies(c,room.Environment)){ArchiveDistrictPlanner.Describe(room,c);return;}
            if(CourtyardDistrictPlanner.Applies(c,room.Environment)){CourtyardDistrictPlanner.Describe(room,c);return;}
            if(RestfulLevelPlanner.IsLevel(room.Environment)){RestfulLevelPlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.RainHouses){RainHousePlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.AquaticPark){AquaticParkPlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.ThermalBaths){ThermalBathPlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.Orangerie){OrangeriePlanner.Describe(room,c);return;}
            if(IsCivic(room.Environment)){DescribeCivic(room,c);return;}
            var e=room.Environment;room.ZoneId=-1;room.Width=room.Depth=c.CellSize;
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x480906u));uint seed=random.Next();
            int floors=e==LiminalEnvironment.Archive906?2:e==LiminalEnvironment.Courtyard188?3+random.Range(3):1;
            room.Height=e==LiminalEnvironment.Archive906?8.5f:e==LiminalEnvironment.Courtyard188?floors*3.2f+.24f:12;
            int variant=random.Range(c.ExpandedVariety?9:6);float half=c.CellSize*.5f,shore=0;
            if(e==LiminalEnvironment.Coast48)shore=CoastShore(room,c);
            bool pavilion=e==LiminalEnvironment.Coast48&&shore>-c.CellSize*.3f+2.4f&&(variant<4||variant>=6);
            var props=new QuietPropPlan[e==LiminalEnvironment.Archive906?6:e==LiminalEnvironment.Courtyard188?4:pavilion?4:0];
            for(int i=0;i<props.Length;i++)
            {
                if(e==LiminalEnvironment.Archive906)
                    props[i]=new QuietPropPlan(new V3((i%2==0?-1:1)*(c.CellSize*.5f-3.1f),.9f,-2+(i/2)*2),random.Range(8));
                else if(e==LiminalEnvironment.Courtyard188)
                    props[i]=new QuietPropPlan(new V3((i%2==0?-1:1)*(c.CellSize*.5f-2.5f),.92f,-half+11.3f+(i/2)*7),i%2==0?0:21*8);
                else props[i]=new QuietPropPlan(new V3(-c.CellSize*.3f+.7f,.9f,-c.CellSize*.3f+i*.52f),-1,i==0?"Lantern":"WaterBottle");
            }
            if(e!=LiminalEnvironment.Coast48)
            {
                int prior=props.Length;Array.Resize(ref props,prior*2);
                for(int i=0;i<prior;i++)
                {
                    var original=props[i].Bottom;
                    uint choice=LiminalRegions.Hash(unchecked((int)seed),room.X,room.Z,0xA48000u+(uint)i)%3;
                    string source=e==LiminalEnvironment.Archive906?(choice==0?"SheenChair":"I_ChairDamaskPurplegold"):
                        choice==1&&c.CellSize>=36?(i%2==0?"I_GlamVelvetSofa":"U_SheenWoodLeatherSofa"):choice==2?"I_ChairDamaskPurplegold":"SheenChair";
                    props[prior+i]=e==LiminalEnvironment.Archive906?
                        new QuietPropPlan(new V3((i%2==0?-1:1)*(half-4.4f),.02f,original.Z),-1,source,i%2==0?-90:90):
                        new QuietPropPlan(new V3(original.X,.02f,original.Z+.95f),-1,source,180);
                }
            }
            var sea=e==LiminalEnvironment.Coast48&&shore<half-.001f?new WaterRegion{CenterX=(shore+half)*.5f,CenterZ=0,Width=half-shore,Depth=c.CellSize,Radius=0,Bottom=-2,Level=c.WaterLevel}:new WaterRegion();
            var plan=new QuietLevelPlan(e,floors,variant,seed,props,sea,shore,pavilion);
            room.Plan=new ProceduralPlan(e,new PlannedModule[0],sea.Width>0?new[]{sea}:new WaterRegion[0],c.DoorWidth*.5f+.8f,0,0,quiet:plan);
            if(!plan.Valid(room,c))throw new ArgumentException("Invalid quiet level room.");
        }
        static void DescribeCivic(Room room,LayoutConfig c)
        {
            var e=room.Environment;room.ZoneId=-1;room.Width=room.Depth=c.CellSize;room.Height=e==LiminalEnvironment.Conservatory?9:8.5f;
            uint seed=LiminalRegions.Hash(c.Seed,room.X,room.Z,0x160C03u);var random=new StableRandom(seed);
            // Furniture lives in the four side bays, leaving a wide clear cross to every exit.
            float bay=c.CellSize*(.26f+(seed%3)*.01f);bool garden=e==LiminalEnvironment.Conservatory;var props=new QuietPropPlan[garden?12:8];
            for(int i=0;i<4;i++)
            {
                float x=(i%2==0?-1:1)*bay,z=(i<2?-1:1)*bay;
                int slot=i*(garden?3:2);
                props[slot]=new QuietPropPlan(new V3(x,.02f,z),-1,garden?"B_CafeTable":"U_SheenWoodLeatherSofa",i%2==0?90:-90);
                props[slot+1]=new QuietPropPlan(new V3(x,garden?.88249021f:.02f,z+(garden?0:1.8f)),-1,garden?"WaterBottle":"U_AntiqueCamera");
                if(garden)props[slot+2]=new QuietPropPlan(new V3(x+(i%2==0?-1:1)*1.05f,.02f,z),-1,"B_CafeChair",i%2==0?-90:90);
            }
            var plan=new QuietLevelPlan(e,1,random.Range(c.ExpandedVariety?12:8),seed,props,new WaterRegion());
            room.Plan=new ProceduralPlan(e,new PlannedModule[0],new WaterRegion[0],c.DoorWidth*.5f+.8f,0,0,quiet:plan);
            if(!plan.Valid(room,c))throw new ArgumentException("Invalid civic level room.");
        }
        static float CoastShore(Room r,LayoutConfig c)
        {
            if(c.Procedural==LiminalGeneration.Coast48)return (float)Math.Max(-c.CellSize*.5f,Math.Min(c.CellSize*.5f,-(double)r.X*c.CellSize));
            // Match the seed's Voronoi group. All rooms in a coastal district share one
            // coastline instead of repeating a separate beach in every room.
            int selectedX,selectedZ;BiomeExtent.NearestRegion(c,r.X,r.Z,out selectedX,out selectedZ);
            double size=BiomeExtent.RegionSpacing(c);
            int group=ContinuousTopology.FloorDivide(selectedX,2);double global=(2.0*group+1)*size*c.CellSize;
            return (float)Math.Max(-c.CellSize*.5f,Math.Min(c.CellSize*.5f,global-(double)r.X*c.CellSize));
        }
    }
}
