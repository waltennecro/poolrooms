using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum RetailVariant { QuietMarket, LongAisles, Wholesale }

    public sealed class RetailStore
    {
        public readonly RetailVariant Variant;
        public readonly int QuarterTurns;
        public readonly float Width,Depth,StockBoundary,FrontCross,BackCross,StockDensity;
        public readonly float[] Aisles;
        public readonly int ProductBudget;
        public RetailStore(RetailVariant variant,int turns,float width,float depth,float stock,float front,float back,float density,float[] aisles,int budget)
        {Variant=variant;QuarterTurns=turns;Width=width;Depth=depth;StockBoundary=stock;FrontCross=front;BackCross=back;StockDensity=density;Aisles=aisles;ProductBudget=budget;}
        public V2 World(float x,float z){return (QuarterTurns&1)==0?new V2(x,z):new V2(z,-x);}
        public V2 Local(float x,float z){return (QuarterTurns&1)==0?new V2(x,z):new V2(-z,x);}
        public int ProductLimit(LayoutConfig c){return Math.Min(ProductBudget,c.CurveSegments<=12?40:c.CurveSegments<=32?80:128);}
        public bool Valid(ProceduralPlan plan,Room room,LayoutConfig c)
        {
            if(!Enum.IsDefined(typeof(RetailVariant),Variant)||float.IsNaN(StockDensity)||StockDensity<0||StockDensity>1||plan.Circulation==null||plan.Circulation.Cuts.Length!=0)return false;
            if(plan.Environment!=LiminalEnvironment.Supermarket||plan.Pools.Length!=0||plan.Circulation==null||!plan.Circulation.Valid(room,c))return false;
            if(room.Height<c.DoorHeight+.5f||room.Width>c.CellSize-6||room.Depth>c.CellSize-6||ProductBudget<0||ProductBudget>128||plan.Modules.Length>32)return false;
            if(Math.Abs(Width-((QuarterTurns&1)==0?room.Width:room.Depth))>.001f||Math.Abs(Depth-((QuarterTurns&1)==0?room.Depth:room.Width))>.001f)return false;
            bool shelves=false,checkout=false,stock=false,cold=false;
            for(int i=0;i<plan.Modules.Length;i++)
            {
                PlannedModule a=plan.Modules[i];if(a.Kind<LiminalModule.Shelf||a.Width<.5f||a.Depth<.5f||a.Rise<.5f||a.Rise>room.Height-.6f)return false;
                var bounds=new PlanRect(a.X-a.Width*.5f,a.Z-a.Depth*.5f,a.X+a.Width*.5f,a.Z+a.Depth*.5f);
                if(Math.Abs(a.X)+a.Width*.5f>room.Width*.5f-.25f||Math.Abs(a.Z)+a.Depth*.5f>room.Depth*.5f-.25f||!plan.Circulation.Fits(bounds,.05f))return false;
                V2 local=Local(a.X,a.Z);float halfDepth=((QuarterTurns&1)==0?a.Depth:a.Width)*.5f;
                if(local.Y-halfDepth<StockBoundary&&local.Y+halfDepth>StockBoundary-.18f)return false;
                for(int j=0;j<i;j++){PlannedModule b=plan.Modules[j];if(Math.Abs(a.X-b.X)<(a.Width+b.Width)*.5f+.1f&&Math.Abs(a.Z-b.Z)<(a.Depth+b.Depth)*.5f+.1f)return false;}
                shelves|=a.Kind==LiminalModule.Shelf;checkout|=a.Kind==LiminalModule.Checkout;stock|=a.Kind==LiminalModule.StockRack;cold|=a.Kind==LiminalModule.RefrigeratedCase;
            }
            return shelves&&checkout&&stock&&cold;
        }
    }

    public static class RetailLighting
    {
        public static ZoneLightStyle Style(Room room,LayoutConfig c)
        {
            uint value=LiminalRegions.Hash(c.Seed,room.X,room.Z,0xF17u)%10;
            return value<5?ZoneLightStyle.SparseFluorescent:value<8?ZoneLightStyle.HalfPower:value==8?ZoneLightStyle.BlueDusk:ZoneLightStyle.EmergencyGuides;
        }
    }

    public static class RetailPlanner
    {
        public static void Describe(Room room,LayoutConfig c)
        {
            room.ZoneId=-1;room.DetailSeed=LiminalRegions.Hash(c.Seed,room.X,room.Z,0xF01u);
            var random=new StableRandom(room.DetailSeed);
            float max=Math.Min(44,c.CellSize-8),min=Math.Min(24,max-2);
            room.Width=random.Range(min,max);room.Depth=random.Range(min,max);
            RetailVariant variant=(RetailVariant)random.Range(3);int turn=random.Range(2);
            room.Height=variant==RetailVariant.Wholesale?random.Range(7.2f,8.4f):random.Range(Math.Max(5.6f,c.DoorHeight+.7f),6.4f);
            float width=turn==0?room.Width:room.Depth,depth=turn==0?room.Depth:room.Width,hx=width*.5f,hz=depth*.5f;
            float stock=hz-random.Range(3.4f,Math.Min(5,hz*.38f)),front=-hz+4.2f,back=stock-1.35f;
            float density=variant==RetailVariant.QuietMarket?random.Range(.05f,.25f):variant==RetailVariant.LongAisles?random.Range(.5f,.9f):random.Range(.3f,.65f);
            int budget=128;
            float pitch=variant==RetailVariant.QuietMarket?5.4f:variant==RetailVariant.Wholesale?4:3.5f,start=variant==RetailVariant.QuietMarket?2.7f:variant==RetailVariant.Wholesale?2.4f:2;
            var shelfXs=new List<float>();for(float x=start;x<hx-2.3f;x+=pitch){shelfXs.Add(-x);shelfXs.Add(x);}
            if(shelfXs.Count==0){shelfXs.Add(-2);shelfXs.Add(2);}
            var aisles=new List<float>{0};
            for(int sign=-1;sign<=1;sign+=2)
            {
                float previous=start;
                for(float x=start+pitch;x<hx-2.3f;x+=pitch){aisles.Add(sign*(previous+x)*.5f);previous=x;}
                aisles.Add(sign*(previous+hx-.25f)*.5f);
            }
            // Product count depends on quality, but the layout metadata never does.
            var store=new RetailStore(variant,turn,width,depth,stock,front,back,density,aisles.ToArray(),budget);
            var routes=new List<WalkingRoute>();var reserved=new List<PlanRect>{new PlanRect(-1.25f,-1.25f,1.25f,1.25f)};
            for(int exit=0;exit<4;exit++)if(room.HasExit(exit))
            {
                Add(routes,reserved,store,exit,new[]{new V2(0,0),store.Local(Directions.DX[exit]*room.Width*.5f,Directions.DZ[exit]*room.Depth*.5f)},1.1f,room);
                float mouth=c.DoorWidth*.5f+.45f,ex=room.Width*.5f,ez=room.Depth*.5f;
                reserved.Add(exit%2==0?new PlanRect(-mouth,exit==0?ez-2:-ez,mouth,exit==0?ez:-ez+2):
                    new PlanRect(exit==1?ex-2:-ex,-mouth,exit==1?ex:-ex+2,mouth));
            }
            foreach(float aisle in aisles)
                Add(routes,reserved,store,-1,new[]{new V2(0,0),new V2(aisle,0),new V2(aisle,front),new V2(0,front),new V2(0,back),new V2(aisle,back),new V2(aisle,0)},1.1f,room);
            var circulation=new PlanCirculation(variant==RetailVariant.Wholesale?InternalLayout.RetailWarehouse:variant==RetailVariant.QuietMarket?InternalLayout.RetailCross:InternalLayout.RetailParallel,routes.ToArray(),reserved.ToArray(),new PlanRect[0]);
            var modules=new List<PlannedModule>();
            foreach(float x in shelfXs)
            {
                float moduleWidth=variant==RetailVariant.QuietMarket?2.8f:variant==RetailVariant.Wholesale?1.5f:1.1f;
                foreach(int sign in new[]{-1,1})
                {
                    float lo=sign<0?front+1.25f:1.25f,hi=sign<0?-1.25f:back-1.25f;
                    if(hi-lo<.6f)continue;
                    LiminalModule kind=LiminalModule.Shelf;
                    if(sign<0&&x==shelfXs[0])kind=LiminalModule.ProduceIsland;
                    if(sign>0&&x==shelfXs[1]&&shelfXs.Count>2)kind=LiminalModule.ServiceCounter;
                    // Keep an ordinary shelf on the other side in the smallest complete stores.
                    Place(modules,circulation,store,kind,x,(lo+hi)*.5f,moduleWidth,Math.Min(variant==RetailVariant.QuietMarket?1.1f:9,hi-lo),kind==LiminalModule.ProduceIsland?.85f:kind==LiminalModule.ServiceCounter?1.1f:variant==RetailVariant.Wholesale?2.7f:variant==RetailVariant.QuietMarket?1.4f:2.1f,ref random,variant==RetailVariant.QuietMarket?1:0);
                }
            }
            // Checkout forecourt, perimeter refrigeration, and back-of-house racks.
            for(int sign=-1;sign<=1;sign+=2)
            {
                Place(modules,circulation,store,LiminalModule.Checkout,sign*Math.Max(2.65f,c.DoorWidth*.5f+1.35f),-hz+1.7f,1.5f,2.25f,1.05f,ref random);
                if(hx>12)Place(modules,circulation,store,LiminalModule.Checkout,sign*6,-hz+1.7f,1.5f,2.25f,1.05f,ref random);
                Place(modules,circulation,store,LiminalModule.CartBay,sign*(hx-1.7f),-hz+1.65f,1.5f,1.9f,.95f,ref random);
                // Cold cases stay in the front forecourt, away from the outside shelf aisle.
                Place(modules,circulation,store,LiminalModule.RefrigeratedCase,sign*(hx-3.2f),-hz+1.7f,1,2.2f,2.35f,ref random);
                float rackLo=c.DoorWidth*.5f+.85f,rackHi=hx-.55f,rackWidth=Math.Min(5,rackHi-rackLo);
                Place(modules,circulation,store,LiminalModule.StockRack,sign*(rackLo+rackHi)*.5f,stock+(hz-stock)*.55f,rackWidth,1.2f,2.6f,ref random,1);
            }
            room.Plan=new ProceduralPlan(LiminalEnvironment.Supermarket,modules.ToArray(),new WaterRegion[0],c.DoorWidth*.5f+.8f,random.Range(3),random.Range(3),false,circulation,store);
            if(!room.Plan.Valid(room,c))throw new ArgumentException("Invalid retail plan at "+room.X+", "+room.Z+".");
        }
        private static void Place(List<PlannedModule> modules,PlanCirculation circulation,RetailStore store,LiminalModule kind,float x,float z,float width,float depth,float height,ref StableRandom random,int turn=0)
        {
            if(modules.Count>=32)return;V2 p=store.World(x,z);float w=(store.QuarterTurns&1)==0?width:depth,d=(store.QuarterTurns&1)==0?depth:width;
            var bounds=new PlanRect(p.X-w*.5f,p.Y-d*.5f,p.X+w*.5f,p.Y+d*.5f);
            if(!circulation.Fits(bounds,.05f))return;
            foreach(PlannedModule m in modules)if(bounds.Overlaps(new PlanRect(m.X-m.Width*.5f,m.Z-m.Depth*.5f,m.X+m.Width*.5f,m.Z+m.Depth*.5f),.1f))return;
            modules.Add(new PlannedModule(kind,p.X,p.Y,w,d,height,0,(turn+store.QuarterTurns)&3,random.Next()));
        }
        private static void Add(List<WalkingRoute> routes,List<PlanRect> reserved,RetailStore store,int exit,V2[] local,float half,Room room)
        {
            var points=new List<V2>();foreach(V2 p in local){V2 world=store.World(p.X,p.Y);if(points.Count==0||(world.X-points[points.Count-1].X)*(world.X-points[points.Count-1].X)+(world.Y-points[points.Count-1].Y)*(world.Y-points[points.Count-1].Y)>.00001f)points.Add(world);}
            if(points.Count<2)return;routes.Add(new WalkingRoute(exit,points.ToArray(),half));
            for(int i=1;i<points.Count;i++){V2 a=points[i-1],b=points[i];reserved.Add(new PlanRect(Math.Max(-room.Width*.5f,Math.Min(a.X,b.X)-half),Math.Max(-room.Depth*.5f,Math.Min(a.Y,b.Y)-half),Math.Min(room.Width*.5f,Math.Max(a.X,b.X)+half),Math.Min(room.Depth*.5f,Math.Max(a.Y,b.Y)+half)));}
        }
    }
}
