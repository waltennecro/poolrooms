using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum AssemblyKind { Bench, Locker, Desk, SeatGroup, Table, Cabinet, Cubbies, SoftBlocks, Bed, DisplayRack, ClothesRail, ArcadeCabinet,
        TowelRack, ShowerStand, DrinkingStation, EquipmentCart, Bookcase, PrinterStation, FilingCabinet,
        LuggageBench, Vanity, VendingMachine, NoticeStand, Sideboard, Checkout, Showcase, PlayTable, ShoeBench }
    public readonly struct GeneratedAssembly
    {
        public readonly AssemblyKind Kind;
        public readonly PlanRect Bounds;
        public readonly int Turns;
        public readonly uint Seed;
        public float Width { get { return (Turns&1)==0?Bounds.Width:Bounds.Depth; } }
        public float Depth { get { return (Turns&1)==0?Bounds.Depth:Bounds.Width; } }
        public GeneratedAssembly(AssemblyKind kind,PlanRect bounds,int turns,uint seed){Kind=kind;Bounds=bounds;Turns=turns;Seed=seed;}
        public V3 Point(float x,float y,float z)
        {
            switch(Turns&3){case 1:return new V3(Bounds.X+z,y,Bounds.Z-x);case 2:return new V3(Bounds.X-x,y,Bounds.Z-z);
                case 3:return new V3(Bounds.X-z,y,Bounds.Z+x);default:return new V3(Bounds.X+x,y,Bounds.Z+z);}
        }
    }
    // Bounded rejection packing with a separate stream: detail cannot change topology or climate.
    public static class AssemblyPacking
    {
        public const int AttemptLimit=240,RoomLimit=24;
        public static GeneratedAssembly[] Fill(PlanRect bounds,List<PlanRect> blocked,AssemblyKind[] kinds,uint seed,int limit,float separation=.85f)
        {
            var random=new StableRandom(seed);var result=new List<GeneratedAssembly>();
            limit=Math.Min(RoomLimit,Math.Max(0,limit));
            for(int attempt=0;attempt<AttemptLimit&&result.Count<limit;attempt++)
            {
                AssemblyKind kind=kinds[random.Range(kinds.Length)];int turns=random.Range(4);
                float w,d;Size(kind,ref random,out w,out d);
                float worldW=(turns&1)==0?w:d,worldD=(turns&1)==0?d:w;
                if(worldW>bounds.Width||worldD>bounds.Depth)continue;
                float x=random.Range(bounds.MinX+worldW*.5f,bounds.MaxX-worldW*.5f),z=random.Range(bounds.MinZ+worldD*.5f,bounds.MaxZ-worldD*.5f);
                // Some candidates align to a real perimeter; others become islands.
                if(attempt%3==0)x=(random.Range(2)==0?bounds.MinX+worldW*.5f:bounds.MaxX-worldW*.5f);
                if(attempt%5==0)z=(random.Range(2)==0?bounds.MinZ+worldD*.5f:bounds.MaxZ-worldD*.5f);
                var rect=new PlanRect(x-worldW*.5f,z-worldD*.5f,x+worldW*.5f,z+worldD*.5f);bool fits=true;
                foreach(var obstacle in blocked)if(rect.Overlaps(obstacle,.3f)){fits=false;break;}
                if(!fits)continue;
                foreach(var old in result)if(rect.Overlaps(old.Bounds,separation)){fits=false;break;}
                if(fits)
                {
                    // Turn the open face towards the usable interior, preserving the footprint.
                    float fx=turns==1?-1:turns==3?1:0,fz=turns==0?-1:turns==2?1:0;
                    if(fx*(bounds.X-x)+fz*(bounds.Z-z)<0)turns=(turns+2)&3;
                    result.Add(new GeneratedAssembly(kind,rect,turns,random.Next()));
                }
            }
            return result.ToArray();
        }
        private static void Size(AssemblyKind k,ref StableRandom r,out float w,out float d)
        {
            w=r.Range(1.25f,2.9f);d=r.Range(.7f,1.2f);
            if(k==AssemblyKind.Table){w=r.Range(2.7f,3.2f);d=r.Range(2.7f,3.2f);}
            if(k==AssemblyKind.Desk){w=r.Range(1.3f,2.2f);d=r.Range(1.5f,1.85f);}
            if(k==AssemblyKind.SeatGroup)d=r.Range(.85f,1.25f);
            if(k==AssemblyKind.SoftBlocks){w=r.Range(1.2f,2.6f);d=r.Range(1.2f,2.2f);}
            if(k==AssemblyKind.Bed){w=r.Range(1.65f,2.1f);d=2.4f;}
            if(k==AssemblyKind.ArcadeCabinet){w=1.05f;d=1.15f;}
            if(k==AssemblyKind.ClothesRail){w=r.Range(1.5f,2.8f);d=.8f;}
            switch(k)
            {
                case AssemblyKind.TowelRack:w=r.Range(1.3f,2.2f);d=.8f;break;
                case AssemblyKind.ShowerStand:w=r.Range(1.5f,2.4f);d=r.Range(1.1f,1.5f);break;
                case AssemblyKind.DrinkingStation:w=1.15f;d=.85f;break;
                case AssemblyKind.EquipmentCart:w=r.Range(1.2f,1.7f);d=.8f;break;
                case AssemblyKind.Bookcase:w=r.Range(1.4f,2.4f);d=.7f;break;
                case AssemblyKind.PrinterStation:w=r.Range(1.25f,1.7f);d=.95f;break;
                case AssemblyKind.FilingCabinet:w=r.Range(1.3f,2);d=.75f;break;
                case AssemblyKind.LuggageBench:w=r.Range(1.5f,2.5f);d=.95f;break;
                case AssemblyKind.Vanity:w=r.Range(1.4f,2);d=1.55f;break;
                case AssemblyKind.VendingMachine:w=r.Range(1.1f,1.5f);d=.9f;break;
                case AssemblyKind.NoticeStand:w=r.Range(1.2f,1.8f);d=.75f;break;
                case AssemblyKind.Sideboard:w=r.Range(1.5f,2.6f);d=.95f;break;
                case AssemblyKind.Checkout:w=r.Range(2.2f,2.8f);d=1.4f;break;
                case AssemblyKind.Showcase:w=r.Range(1.5f,2.4f);d=.95f;break;
                case AssemblyKind.PlayTable:w=r.Range(2.1f,2.8f);d=2.1f;break;
                case AssemblyKind.ShoeBench:w=r.Range(1.4f,2.2f);d=.95f;break;
            }
        }
    }
    public static class AutonomousPlanner
    {
        public static AssemblyKind[] Kinds(LiminalEnvironment environment)
        {
            switch(environment)
            {
                case LiminalEnvironment.Poolrooms:return new[]{AssemblyKind.Bench,AssemblyKind.Locker,AssemblyKind.TowelRack,AssemblyKind.ShowerStand,AssemblyKind.DrinkingStation,AssemblyKind.EquipmentCart};
                case LiminalEnvironment.IndoorPlay:return new[]{AssemblyKind.Cubbies,AssemblyKind.SoftBlocks,AssemblyKind.Bench,AssemblyKind.PlayTable,AssemblyKind.ShoeBench,AssemblyKind.NoticeStand};
                case LiminalEnvironment.Office:return new[]{AssemblyKind.Desk,AssemblyKind.Desk,AssemblyKind.Cabinet,AssemblyKind.Table,AssemblyKind.PrinterStation,AssemblyKind.FilingCabinet,AssemblyKind.Bookcase,AssemblyKind.DrinkingStation};
                case LiminalEnvironment.Hotel:return new[]{AssemblyKind.Cabinet,AssemblyKind.Bench,AssemblyKind.Desk,AssemblyKind.LuggageBench,AssemblyKind.Vanity,AssemblyKind.Bookcase};
                case LiminalEnvironment.WaitingHall:return new[]{AssemblyKind.SeatGroup,AssemblyKind.Bench,AssemblyKind.VendingMachine,AssemblyKind.NoticeStand,AssemblyKind.DrinkingStation};
                case LiminalEnvironment.DiningHall:return new[]{AssemblyKind.Table,AssemblyKind.Table,AssemblyKind.SeatGroup,AssemblyKind.Sideboard,AssemblyKind.EquipmentCart,AssemblyKind.DrinkingStation};
                default:return new[]{AssemblyKind.DisplayRack,AssemblyKind.DisplayRack,AssemblyKind.Checkout,AssemblyKind.Showcase,AssemblyKind.EquipmentCart,AssemblyKind.NoticeStand};
            }
        }
        public static List<PlanRect> Obstacles(Room room,LayoutConfig c,ProceduralPlan plan=null)
        {
            var p=plan??room.Plan;var result=new List<PlanRect>();
            if(p.Circulation!=null){result.AddRange(p.Circulation.Reserved);result.AddRange(p.Circulation.Cuts);}
            else
            {
                float clear=p.ClearHalfWidth;
                result.Add(new PlanRect(-clear,-room.Depth*.5f,clear,room.Depth*.5f));
                result.Add(new PlanRect(-room.Width*.5f,-clear,room.Width*.5f,clear));
            }
            foreach(var m in p.Modules)result.Add(new PlanRect(m.X-m.Width*.5f,m.Z-m.Depth*.5f,m.X+m.Width*.5f,m.Z+m.Depth*.5f));
            foreach(var water in p.Pools)result.Add(new PlanRect(water.CenterX-water.Width*.5f,water.CenterZ-water.Depth*.5f,water.CenterX+water.Width*.5f,water.CenterZ+water.Depth*.5f));
            if(p.Solar!=null)
            {
                foreach(var d in p.Solar.Details)result.Add(d.Bounds);
                float radius=p.Solar.OutlineRadius,hx=room.Width*.5f,hz=room.Depth*.5f;
                if(radius>0)foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                    result.Add(new PlanRect(sx<0?-hx:hx-radius,sz<0?-hz:hz-radius,sx<0?-hx+radius:hx,sz<0?-hz+radius:hz));
            }
            return result;
        }
        public static bool Valid(ProceduralPlan plan,Room room,LayoutConfig c)
        {
            if(plan.Assemblies==null||plan.Assemblies.Length>AssemblyPacking.RoomLimit)return false;
            if(plan.Assemblies.Length==0)return true;
            var blocked=Obstacles(room,c,plan);
            var bounds=new PlanRect(-room.Width*.5f+.8f,-room.Depth*.5f+.8f,room.Width*.5f-.8f,room.Depth*.5f-.8f);
            for(int i=0;i<plan.Assemblies.Length;i++)
            {
                var a=plan.Assemblies[i];
                if(!Enum.IsDefined(typeof(AssemblyKind),a.Kind)||a.Turns<0||a.Turns>3||a.Width<=0||a.Depth<=0||
                    !bounds.Contains(a.Bounds.MinX,a.Bounds.MinZ)||!bounds.Contains(a.Bounds.MaxX,a.Bounds.MaxZ))return false;
                foreach(var b in blocked)if(a.Bounds.Overlaps(b,.29f))return false;
                for(int j=0;j<i;j++)if(a.Bounds.Overlaps(plan.Assemblies[j].Bounds,.84f))return false;
            }
            return true;
        }
        public static void Decorate(Room room,LayoutConfig c)
        {
            // Interior programs furnish their own bays; mall gateways must stay uncluttered.
            if(room.Plan.Aquatic!=null||room.Plan.Baths!=null||room.Plan.Garden!=null||room.Plan.Quiet!=null||room.Plan.City!=null||room.Plan.SoftPlay!=null||room.Plan.Interior!=null||c.MallDistricts&&room.Environment==LiminalEnvironment.Supermarket)return;
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x22001u));
            var bounds=new PlanRect(-room.Width*.5f+.85f,-room.Depth*.5f+.85f,room.Width*.5f-.85f,room.Depth*.5f-.85f);
            room.Plan.Assemblies=AssemblyPacking.Fill(bounds,Obstacles(room,c),Kinds(room.Environment),random.Next(),3+random.Range(10));
        }
        public static GeneratedAssembly[] Interior(InteriorBay bay,LiminalEnvironment environment)
        {
            float w=bay.LocalWidth,d=bay.LocalDepth;var bounds=new PlanRect(-w*.5f+.4f,-d*.5f+.4f,w*.5f-.4f,d*.5f-.4f);
            var blocked=new List<PlanRect>{new PlanRect(-1.2f,-d*.5f,1.2f,d*.5f-(environment==LiminalEnvironment.Hotel?3.5f:.4f))};
            var random=new StableRandom(bay.Seed^0x22002u);var result=new List<GeneratedAssembly>();
            if(environment==LiminalEnvironment.Hotel)
            {
                float bw=random.Range(1.65f,2.1f),z=d*.5f-1.6f;
                var bed=new PlanRect(-bw*.5f,z-1.2f,bw*.5f,z+1.2f);
                result.Add(new GeneratedAssembly(AssemblyKind.Bed,bed,0,random.Next()));blocked.Add(bed);
            }
            result.AddRange(AssemblyPacking.Fill(bounds,blocked,Kinds(environment),random.Next(),3+random.Range(6)));
            return result.ToArray();
        }
    }
}
