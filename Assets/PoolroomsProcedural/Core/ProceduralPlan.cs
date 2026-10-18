using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum LiminalModule { Pool, Columns, Arch, Screen, Bench, Platform, PlayFrame, Tunnel, BallPit, Slide, Shelf, RefrigeratedCase, Checkout, ProduceIsland, StockRack, CartBay, ServiceCounter }

    public readonly struct PlannedModule
    {
        public readonly LiminalModule Kind;
        public readonly float X,Z,Width,Depth,Rise,Radius;
        public readonly int QuarterTurns;
        public readonly uint Seed;
        public float LocalWidth { get { return (QuarterTurns&1)==0?Width:Depth; } }
        public float LocalDepth { get { return (QuarterTurns&1)==0?Depth:Width; } }
        public PlannedModule(LiminalModule kind,float x,float z,float width,float depth,float rise,float radius,int turns,uint seed)
        {Kind=kind;X=x;Z=z;Width=width;Depth=depth;Rise=rise;Radius=radius;QuarterTurns=turns;Seed=seed;}
    }
    public sealed class ProceduralPlan
    {
        public readonly LiminalEnvironment Environment;
        public readonly PlannedModule[] Modules;
        public readonly WaterRegion[] Pools;
        public readonly float ClearHalfWidth;
        public readonly int CeilingPattern,Palette;
        public readonly bool IsFallback;
        public readonly PlanCirculation Circulation;
        public readonly RetailStore Store;
        public readonly InteriorPlan Interior;
        public readonly SolarRoomPlan Solar;
        public readonly SoftPlayPlan SoftPlay;
        public readonly City11Plan City;
        public readonly QuietLevelPlan Quiet;
        public readonly OrangeriePlan Garden;
        public GeneratedAssembly[] Assemblies { get; internal set; } = new GeneratedAssembly[0];
        public ProceduralPlan(LiminalEnvironment environment,PlannedModule[] modules,WaterRegion[] pools,float clear,int ceiling,int palette,bool fallback=false,PlanCirculation circulation=null,RetailStore store=null,InteriorPlan interior=null,SolarRoomPlan solar=null,SoftPlayPlan softPlay=null,City11Plan city=null,QuietLevelPlan quiet=null,OrangeriePlan garden=null)
        {Environment=environment;Modules=modules;Pools=pools;ClearHalfWidth=clear;CeilingPattern=ceiling;Palette=palette;IsFallback=fallback;Circulation=circulation;Store=store;Interior=interior;Solar=solar;SoftPlay=softPlay;City=city;Quiet=quiet;Garden=garden;}

        public bool Valid(Room room,LayoutConfig c)
        {
            if(room.TerrainGateway)
            {
                ConnectedTerrainSite site;
                return ConnectedTerrainTopology.Gateway(c,room.X,room.Z,out site)&&Environment==LiminalEnvironment.WaitingHall&&Modules.Length==0&&Pools.Length==0&&room.Width<=c.CellSize-6&&room.Depth<=c.CellSize-6&&room.Height>=c.DoorHeight+.5f;
            }
            if(room.MallGateway)
            {
                ConnectedMallSite site;
                return ConnectedMallTopology.Gateway(c,room.X,room.Z,out site)&&Environment==LiminalEnvironment.Supermarket&&
                    Modules.Length==0&&Pools.Length==0&&room.Width<=c.CellSize-6&&room.Depth<=c.CellSize-6&&room.Height>=c.DoorHeight+.5f;
            }
            if(!AutonomousPlanner.Valid(this,room,c))return false;
            if(Garden!=null)return Garden.Valid(room,c);
            if(Quiet!=null)return Quiet.Valid(room,c);
            if(City!=null)return City.Valid(room,c);
            if(SoftPlay!=null)return SoftPlay.Valid(room,c);
            if(Solar!=null)return Solar.Valid(this,room,c);
            if(Interior!=null)return Interior.Valid(this,room,c);
            if(InteriorPlanner.IsInterior(Environment))return false;
            if(Store!=null)return Store.Valid(this,room,c);
            if(Environment==LiminalEnvironment.Supermarket)return false;
            if(Circulation!=null&&!Circulation.Valid(room,c))return false;
            if(ClearHalfWidth<c.DoorWidth*.5f+.65f||room.Height<c.DoorHeight+.5f||room.Width>c.CellSize-6||room.Depth>c.CellSize-6)return false;
            for(int i=0;i<Modules.Length;i++)
            {
                PlannedModule a=Modules[i];
                if(a.Width<2||a.Depth<2||Math.Abs(a.X)+a.Width*.5f>room.Width*.5f-1||Math.Abs(a.Z)+a.Depth*.5f>room.Depth*.5f-1)return false;
                // Leave a continuous, level cross joining all four doorway approaches and the spawn.
                if(Circulation==null)
                {if(Math.Abs(a.X)-a.Width*.5f<ClearHalfWidth||Math.Abs(a.Z)-a.Depth*.5f<ClearHalfWidth)return false;}
                else if(!Circulation.Fits(new PlanRect(a.X-a.Width*.5f,a.Z-a.Depth*.5f,a.X+a.Width*.5f,a.Z+a.Depth*.5f),.1f))return false;
                for(int j=0;j<i;j++)
                {PlannedModule b=Modules[j];if(Math.Abs(a.X-b.X)<(a.Width+b.Width)*.5f+.1f&&Math.Abs(a.Z-b.Z)<(a.Depth+b.Depth)*.5f+.1f)return false;}
                if(a.Kind==LiminalModule.Pool)
                {
                    int steps=(int)Math.Round(a.Rise/.21f);
                    if(steps<1||Math.Min(a.Width,a.Depth)-steps*.6f<1.2f||a.Radius>Math.Min(a.Width,a.Depth)*.5f)return false;
                }
                if(a.Kind==LiminalModule.Platform||a.Kind==LiminalModule.Slide||a.Kind==LiminalModule.PlayFrame)
                {if(a.LocalDepth<4||a.LocalWidth<3.4f||a.Rise>1.8f||a.Rise/(a.LocalDepth-2)> .5f||room.Height-a.Rise<2.2f)return false;}
                if(a.Kind==LiminalModule.Tunnel&&a.LocalWidth<3)return false;
            }
            if(Environment==LiminalEnvironment.IndoorPlay&&Pools.Length!=0)return false;
            foreach(WaterRegion p in Pools)
            {
                if(p.Width<=0||p.Depth<=0||p.Bottom>=p.Level||p.Level!=c.WaterLevel)return false;
                if(Circulation==null)
                {if(Math.Abs(p.CenterX)-p.Width*.5f<ClearHalfWidth||Math.Abs(p.CenterZ)-p.Depth*.5f<ClearHalfWidth)return false;}
                else if(!Circulation.Fits(new PlanRect(p.CenterX-p.Width*.5f,p.CenterZ-p.Depth*.5f,p.CenterX+p.Width*.5f,p.CenterZ+p.Depth*.5f),.1f))return false;
            }
            return true;
        }
    }

    public static class ProceduralPlanner
    {
        private static readonly LiminalModule[] poolChoices={LiminalModule.Pool,LiminalModule.Pool,LiminalModule.Columns,LiminalModule.Arch,
            LiminalModule.Screen,LiminalModule.Bench,LiminalModule.Platform};
        private static readonly LiminalModule[] playChoices={LiminalModule.PlayFrame,LiminalModule.Tunnel,LiminalModule.BallPit,LiminalModule.Slide,LiminalModule.Platform,LiminalModule.Bench};
        public static void Describe(Room room,LayoutConfig c)
        {
            DescribeBase(room,c);
            if(c.AutonomousGeneration&&room.Plan!=null)AutonomousPlanner.Decorate(room,c);
        }
        private static void DescribeBase(Room room,LayoutConfig c)
        {
            // Integrate the expanded solar builder before the original Authored-mode early return.
            // Closed regions retain the actual original recipe/legacy room, not a solar imitation.
            if(PoolroomArchitectureMix.Applies(c,room.X,room.Z))
            {
                room.Environment=LiminalEnvironment.Poolrooms;
                if(PoolroomArchitectureMix.UsesSolar(c,room.X,room.Z))SolarPlanner.Describe(room,c);
                return;
            }
            if(c.Procedural==LiminalGeneration.Authored)return;
            room.Environment=LiminalRegions.At(c,room.X,room.Z);
            if(QuietLevelPlanner.IsLevel(room.Environment)){QuietLevelPlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.City11){City11Planner.Describe(room,c);return;}
            if(LiminalRegions.UsesAuthored(c,room.X,room.Z))return;
            if(room.Environment==LiminalEnvironment.Poolrooms&&c.Grammar==ProceduralGrammar.SolarAtlas){SolarPlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.SoftPlay){SoftPlayPlanner.Describe(room,c);return;}
            if(room.Environment==LiminalEnvironment.Supermarket){RetailPlanner.Describe(room,c);return;}
            if(InteriorPlanner.IsInterior(room.Environment)){InteriorPlanner.Describe(room,c);return;}
            room.ZoneId=-1;room.DetailSeed=LiminalRegions.Hash(c.Seed,room.X,room.Z,0xB02u);
            var dimensions=new StableRandom(LiminalRegions.Hash(c.Seed,room.X,room.Z,0xB01u));
            float max=Math.Min(32,c.CellSize-8),min=Math.Min(20,max-2);
            room.Width=dimensions.Range(min,max);room.Depth=dimensions.Range(min,max);
            room.Height=dimensions.Range(Math.Max(6,c.DoorHeight+.7f),8.8f);
            // New circulation depends on the actual exits, assigned before description.
            if(c.Grammar==ProceduralGrammar.Varied||c.Grammar==ProceduralGrammar.SolarAtlas)
            {
                for(int attempt=0;attempt<4;attempt++)
                {
                    var varied=VariedPlanner.Plan(room,c,attempt);
                    if(varied.Valid(room,c)&&varied.Modules.Length>0&&
                        (room.Environment!=LiminalEnvironment.Poolrooms||varied.Pools.Length>0)){room.Plan=varied;return;}
                }
            }
            // Keep v12 as a bounded safe recovery and a selectable compatibility grammar.
            for(int attempt=0;attempt<4;attempt++)
            {
                var plan=Plan(room,c,attempt);
                if(plan.Valid(room,c)){room.Plan=plan;return;}
            }
            room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],c.DoorWidth*.5f+.8f,0,0,true);
        }
        private static ProceduralPlan Plan(Room r,LayoutConfig c,int attempt)
        {
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,r.X,r.Z,0xB10u+(uint)attempt));
            float clear=c.DoorWidth*.5f+.8f,outer=1.25f;
            float availableX=r.Width*.5f-clear-outer,availableZ=r.Depth*.5f-clear-outer;
            int nx=availableX>=8?1+random.Range(2):1,nz=availableZ>=8?1+random.Range(2):1;
            float cellX=availableX/nx,cellZ=availableZ/nz;
            var modules=new List<PlannedModule>();var pools=new List<WaterRegion>();
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
            for(int ix=0;ix<nx;ix++)for(int iz=0;iz<nz;iz++)
            {
                float width=cellX-.45f,depth=cellZ-.45f;
                float x=sx*(clear+(ix+.5f)*cellX),z=sz*(clear+(iz+.5f)*cellZ);
                bool first=modules.Count==0;
                if(!first&&random.Value()<.16f)continue;
                LiminalModule kind;
                if(r.Environment==LiminalEnvironment.Poolrooms)
                {
                    kind=first?LiminalModule.Pool:poolChoices[random.Range(poolChoices.Length)];
                }
                else
                {
                    kind=first?LiminalModule.PlayFrame:playChoices[random.Range(playChoices.Length)];
                }
                int turns=random.Range(4);float lw=(turns&1)==0?width:depth,ld=(turns&1)==0?depth:width;
                if((kind==LiminalModule.Platform||kind==LiminalModule.PlayFrame||kind==LiminalModule.Slide)&&(lw<3.4f||ld<4))kind=LiminalModule.Tunnel;
                if(kind==LiminalModule.Slide&&(lw<4.4f||ld<5))kind=LiminalModule.PlayFrame;
                if(kind==LiminalModule.Tunnel&&lw<3)kind=LiminalModule.Bench;
                float rise=0,radius=0;
                if(kind==LiminalModule.Pool)
                {
                    int maxSteps=Math.Min(6,(int)((Math.Min(width,depth)-1.4f)/.6f));
                    int steps=1+random.Range(Math.Max(1,maxSteps));rise=steps*.21f;
                    radius=random.Range(0,Math.Min(width,depth)*.48f);
                    pools.Add(new WaterRegion{CenterX=x,CenterZ=z,Width=width,Depth=depth,Radius=radius,Bottom=-rise,Level=c.WaterLevel});
                }
                if(kind==LiminalModule.Platform||kind==LiminalModule.PlayFrame||kind==LiminalModule.Slide)
                    rise=(3+random.Range(Math.Max(1,Math.Min(6,(int)((ld-2)*.4f/.2f))-2)))*.2f;
                modules.Add(new PlannedModule(kind,x,z,width,depth,rise,radius,turns,random.Next()));
            }
            return new ProceduralPlan(r.Environment,modules.ToArray(),pools.ToArray(),clear,random.Range(3),random.Range(3));
        }
    }
}
