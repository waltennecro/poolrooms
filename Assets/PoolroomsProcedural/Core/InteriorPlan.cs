using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct InteriorBay
    {
        public readonly float X,Z,Width,Depth;
        public readonly int QuarterTurns,Variant;
        public readonly uint Seed;
        public float LocalWidth {get{return (QuarterTurns&1)==0?Width:Depth;}}
        public float LocalDepth {get{return (QuarterTurns&1)==0?Depth:Width;}}
        public InteriorBay(float x,float z,float width,float depth,int turns,int variant,uint seed)
        {X=x;Z=z;Width=width;Depth=depth;QuarterTurns=turns;Variant=variant;Seed=seed;}
        public V3 World(float x,float y,float z)
        {
            switch(QuarterTurns&3){case 1:return new V3(X+z,y,Z-x);case 2:return new V3(X-x,y,Z-z);
                case 3:return new V3(X-z,y,Z+x);default:return new V3(X+x,y,Z+z);}
        }
    }

    // Architectural programs, not a fixed list of room recipes. All four connector axes
    // remain level; side rooms are entered through actual openings, never closed doors.
    public sealed class InteriorPlan
    {
        public readonly InteriorBay[] Bays;
        public readonly int Variant;
        public readonly PlanRect[] Galleries;
        public readonly WalkingRoute[] AccessRoutes;
        public InteriorPlan(InteriorBay[] bays,int variant,PlanRect[] galleries=null,WalkingRoute[] accessRoutes=null)
        {Bays=bays;Variant=variant;Galleries=galleries??new PlanRect[0];AccessRoutes=accessRoutes??new WalkingRoute[0];}
        public bool Valid(ProceduralPlan plan,Room room,LayoutConfig c)
        {
            if(!InteriorPlanner.IsInterior(plan.Environment)||plan.Environment!=room.Environment||Variant<0||Variant>2||Bays==null||(c.AutonomousGeneration?(Bays.Length<4||Bays.Length>InteriorSubdivision.MaximumBays):Bays.Length!=4)||
                plan.Pools.Length!=0||plan.Modules.Length!=0||plan.Store!=null||plan.Circulation!=null||
                plan.ClearHalfWidth<c.DoorWidth*.5f+.7f||room.Height<c.DoorHeight+.5f||room.Width>c.CellSize-6||room.Depth>c.CellSize-6)return false;
            for(int i=0;i<Bays.Length;i++)
            {
                InteriorBay b=Bays[i];
                if(!Finite(b.X)||!Finite(b.Z)||!Finite(b.Width)||!Finite(b.Depth)||b.LocalWidth<5.2f||b.LocalDepth<5.2f||
                    b.QuarterTurns<0||b.QuarterTurns>3||b.Variant<0||b.Variant>2||
                    Math.Abs(b.X)+b.Width*.5f>room.Width*.5f-.6f||Math.Abs(b.Z)+b.Depth*.5f>room.Depth*.5f-.6f||
                    Math.Abs(b.X)-b.Width*.5f<plan.ClearHalfWidth-.001f||Math.Abs(b.Z)-b.Depth*.5f<plan.ClearHalfWidth-.001f)return false;
                V3 front=b.World(0,0,-b.LocalDepth*.5f);
                bool access=Math.Min(Math.Abs(front.X),Math.Abs(front.Z))<=plan.ClearHalfWidth+.001f;
                var rect=new PlanRect(b.X-b.Width*.5f,b.Z-b.Depth*.5f,b.X+b.Width*.5f,b.Z+b.Depth*.5f);
                foreach(var gallery in Galleries)
                {
                    if(gallery.Overlaps(rect,-.001f))return false;
                    if(gallery.Expanded(.001f).Contains(front.X,front.Z))access=true;
                }
                if(!access)return false;
                for(int j=0;j<i;j++)if(Math.Abs(b.X-Bays[j].X)<(b.Width+Bays[j].Width)*.5f&&Math.Abs(b.Z-Bays[j].Z)<(b.Depth+Bays[j].Depth)*.5f)return false;
            }
            if(Galleries.Length>8||AccessRoutes.Length!=0&&AccessRoutes.Length!=Bays.Length)return false;
            foreach(var g in Galleries)if(g.Width<GalleryMinimum||g.Depth<GalleryMinimum||
                !Finite(g.MinX)||!Finite(g.MinZ)||!Finite(g.MaxX)||!Finite(g.MaxZ)||
                Math.Max(Math.Abs(g.MinX),Math.Abs(g.MaxX))>room.Width*.5f-.6f||
                Math.Max(Math.Abs(g.MinZ),Math.Abs(g.MaxZ))>room.Depth*.5f-.6f)return false;
            foreach(var route in AccessRoutes)
            {
                if(route.Points.Length<2||route.HalfWidth<.6f||Math.Abs(route.Points[0].X)+Math.Abs(route.Points[0].Y)>.001f)return false;
                for(int i=0;i<route.Points.Length;i++)
                {
                    V2 p=route.Points[i];if(!Finite(p.X)||!Finite(p.Y)||Math.Abs(p.X)>room.Width*.5f-.6f||Math.Abs(p.Y)>room.Depth*.5f-.6f)return false;
                    if(i>0&&Math.Abs(p.X-route.Points[i-1].X)>.001f&&Math.Abs(p.Y-route.Points[i-1].Y)>.001f)return false;
                }
            }
            return true;
        }
        private const float GalleryMinimum=InteriorSubdivision.GalleryWidth-.001f;
        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
    }

    public static class InteriorPlanner
    {
        private static readonly ZoneLightStyle[][] LightStyles={
            new[]{ZoneLightStyle.CleanWhite,ZoneLightStyle.SparseFluorescent,ZoneLightStyle.HalfPower,ZoneLightStyle.WarmAmber},
            new[]{ZoneLightStyle.WarmAmber,ZoneLightStyle.WarmAmber,ZoneLightStyle.HalfPower,ZoneLightStyle.SunsetRose},
            new[]{ZoneLightStyle.SoftDaylight,ZoneLightStyle.CleanWhite,ZoneLightStyle.SparseFluorescent,ZoneLightStyle.HalfPower},
            new[]{ZoneLightStyle.WarmAmber,ZoneLightStyle.CleanWhite,ZoneLightStyle.HalfPower,ZoneLightStyle.SunsetRose}};
        public static bool IsInterior(LiminalEnvironment environment)
        {return environment>=LiminalEnvironment.Office&&environment<=LiminalEnvironment.DiningHall;}
        public static string Name(LiminalEnvironment environment)
        {
            switch(environment){case LiminalEnvironment.SoftPlay:return "Áreas infantiles multinivel";case LiminalEnvironment.Office:return "Oficinas vacías";case LiminalEnvironment.Hotel:return "Hotel silencioso";
                case LiminalEnvironment.WaitingHall:return "Salas de espera";case LiminalEnvironment.DiningHall:return "Comedores antiguos";
                case LiminalEnvironment.IndoorPlay:return "Juegos interiores";case LiminalEnvironment.Supermarket:return "Supermercado liminal";default:return "Poolrooms generadas";}
        }
        public static void Describe(Room room,LayoutConfig c)
        {
            if(!IsInterior(room.Environment))throw new ArgumentException("Not an interior environment.");
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x1701u));
            room.ZoneId=-1;room.DetailSeed=LiminalRegions.Hash(c.Seed,room.X,room.Z,0x1702u);
            float max=Math.Min(44,c.CellSize-(c.AutonomousGeneration?6.2f:8)),min=Math.Min(c.AutonomousGeneration?22:26,max);
            room.Width=random.Range(min,max);room.Depth=random.Range(min,max);room.Height=random.Range(c.DoorHeight+.65f,c.DoorHeight+1.4f);
            float clear=c.DoorWidth*.5f+.9f,margin=.65f;
            var bays=new List<InteriorBay>();var galleries=new List<PlanRect>();var routes=new List<WalkingRoute>();int variant=random.Range(3);
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
            {
                float w=room.Width*.5f-clear-margin,d=room.Depth*.5f-clear-margin;
                // Door orientation varies independently, joining the horizontal or vertical gallery.
                int turns=random.Range(2)==0?(sz>0?0:2):(sx>0?1:3);
                var bay=new InteriorBay(sx*(clear+w*.5f),sz*(clear+d*.5f),w,d,turns,random.Range(3),random.Next());
                if(c.AutonomousGeneration)InteriorSubdivision.Add(bay,ref random,bays,galleries,routes);
                else bays.Add(bay);
            }
            int palette=4+((int)room.Environment-(int)LiminalEnvironment.Office)*3+variant;
            room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],clear,random.Range(3),palette,
                interior:new InteriorPlan(bays.ToArray(),variant,galleries.ToArray(),routes.ToArray()));
            if(!room.Plan.Valid(room,c))throw new InvalidOperationException("Interior planner violated protected circulation.");
        }
        public static ZoneLightStyle Lighting(Room room,LayoutConfig c)
        {
            int v=(int)(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x1710u)%4);
            int family=(int)room.Environment-(int)LiminalEnvironment.Office;
            return LightStyles[family>=0&&family<4?family:3][v];
        }
    }
}
