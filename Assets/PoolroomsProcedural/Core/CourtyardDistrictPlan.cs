using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum CourtyardRoomUse { Guest, Twin, Reading, Breakfast, Laundry, Reception }
    public readonly struct CourtyardSuite
    {
        public readonly PlanRect Bounds;
        public readonly int Floor,Number,DoorSide;
        public readonly CourtyardRoomUse Use;
        public readonly uint Seed;
        public CourtyardSuite(PlanRect bounds,int floor,int number,int doorSide,CourtyardRoomUse use,uint seed)
        {Bounds=bounds;Floor=floor;Number=number;DoorSide=doorSide;Use=use;Seed=seed;}
    }
    public readonly struct CourtyardOpening
    {
        public readonly V3 Center;
        public readonly float Width,Height,Yaw;
        public readonly bool Door,Operable;
        public readonly int Number;
        public CourtyardOpening(V3 center,float width,float height,float yaw,bool door,bool operable,int number=0)
        {Center=center;Width=width;Height=height;Yaw=yaw;Door=door;Operable=operable;Number=number;}
    }
    // Four central chunks form ONE open court, twelve surrounding chunks form a
    // five-storey hotel. A partial district becomes a furnished hotel annex;
    // a missing neighbouring biome is never overwritten to complete a building.
    public sealed class CourtyardDistrictPlan
    {
        public const int Span=4,Floors=5;
        public const float Storey=3.4f,Landing=1.25f,Flight=3.9f,StairWidth=3.6f;
        public readonly int Column,Row,OriginX,OriginZ;
        public readonly bool Complete,Court,StairCore;
        public readonly float Cell,GroundHeight,Aisle;
        public readonly uint Seed;
        public readonly PlanRect Stair;
        public readonly CourtyardSuite[] Suites;
        public float Roof {get{return GroundHeight+(Floors-1)*Storey;}}
        public float FloorY(int floor){return floor==0?0:GroundHeight+(floor-1)*Storey;}
        public float Rise(int floor){return floor==0?GroundHeight:Storey;}
        public int Steps(int floor){return (int)Math.Ceiling(Rise(floor)/.36f);}
        public float DistrictX {get{return (Column-1.5f)*Cell;}}
        public float DistrictZ {get{return (Row-1.5f)*Cell;}}
        public CourtyardDistrictPlan(LayoutConfig c,int x,int z,bool complete)
        {
            OriginX=ContinuousTopology.FloorDivide(x,Span)*Span;OriginZ=ContinuousTopology.FloorDivide(z,Span)*Span;
            Column=x-OriginX;Row=z-OriginZ;Complete=complete;Cell=c.CellSize;
            Court=complete&&Column>=1&&Column<=2&&Row>=1&&Row<=2;
            StairCore=!Court&&(Column==0||Column==3)&&(Row==0||Row==3);
            GroundHeight=Math.Max(3.6f,c.DoorHeight+.28f);Aisle=Math.Max(1.5f,c.DoorWidth*.5f+.35f);
            Seed=LiminalRegions.Hash(c.Seed,OriginX,OriginZ,0x188A01u);
            float half=Cell*.5f;
            Stair=new PlanRect(-half+1.1f,-half+1.1f,-half+1.1f+StairWidth,-half+1.1f+Flight+Landing*2);
            var suites=new List<CourtyardSuite>();
            if(!Court)for(int floor=0;floor<Floors;floor++)for(int qz=-1;qz<=1;qz+=2)for(int qx=-1;qx<=1;qx+=2)
            {
                var area=new PlanRect(qx<0?-half+.35f:Aisle,qz<0?-half+.35f:Aisle,qx<0?-Aisle:half-.35f,qz<0?-Aisle:half-.35f);
                bool horizontal=Row==0||Row==3;
                int count=Math.Max(1,(int)((horizontal?area.Width:area.Depth)/5.1f));
                for(int i=0;i<count;i++)
                {
                    var b=horizontal?new PlanRect(area.MinX+area.Width*i/count,area.MinZ,area.MinX+area.Width*(i+1)/count,area.MaxZ):
                        new PlanRect(area.MinX,area.MinZ+area.Depth*i/count,area.MaxX,area.MinZ+area.Depth*(i+1)/count);
                    if(StairCore&&b.Overlaps(Stair.Expanded(.5f)))continue;
                    int doorSide=horizontal?(qz<0?0:2):(qx<0?1:3);
                    uint h=LiminalRegions.Hash(unchecked((int)Seed),x,z,0x188B00u+(uint)(floor*128+suites.Count));
                    var use=floor==0?(CourtyardRoomUse)(2+h%4):(h%5==0?CourtyardRoomUse.Twin:CourtyardRoomUse.Guest);
                    suites.Add(new CourtyardSuite(b,floor,10000*(floor+1)+100*(Row*4+Column)+1+suites.Count%100,doorSide,use,h));
                }
            }
            Suites=suites.ToArray();
        }
    }
    public static class CourtyardDistrictPlanner
    {
        public static bool Applies(LayoutConfig c,LiminalEnvironment e){return c.RebuildCourtyard188&&e==LiminalEnvironment.Courtyard188;}
        public static bool Available(LayoutConfig c,int x,int z)
        {
            if(!c.Continuous&&(x<0||z<0||x>=c.Columns||z>=c.Rows))return false;
            ConnectedTerrainSite terrain;ConnectedMallSite mall;
            return LiminalRegions.At(c,x,z)==LiminalEnvironment.Courtyard188&&!ConnectedTerrainTopology.Reserved(c,x,z)&&
                !ConnectedTerrainTopology.Gateway(c,x,z,out terrain)&&!ConnectedMallTopology.At(c,x,z,out mall)&&!ConnectedMallTopology.Gateway(c,x,z,out mall);
        }
        public static bool Same(LayoutConfig c,int x,int z,int side)
        {
            if(!c.RebuildCourtyard188||!Available(c,x,z))return false;
            long nx=(long)x+Directions.DX[side],nz=(long)z+Directions.DZ[side];
            return nx>=int.MinValue&&nx<=int.MaxValue&&nz>=int.MinValue&&nz<=int.MaxValue&&Available(c,(int)nx,(int)nz);
        }
        public static CourtyardDistrictPlan Plan(LayoutConfig c,int x,int z)
        {
            int ox=ContinuousTopology.FloorDivide(x,4)*4,oz=ContinuousTopology.FloorDivide(z,4)*4;bool complete=true;
            for(int row=0;row<4&&complete;row++)for(int col=0;col<4;col++)if(!Available(c,ox+col,oz+row)){complete=false;break;}
            return new CourtyardDistrictPlan(c,x,z,complete);
        }
        public static bool CourtNeighbour(CourtyardDistrictPlan p,int side)
        {
            int x=p.Column+Directions.DX[side],z=p.Row+Directions.DZ[side];
            return p.Complete&&x>=1&&x<=2&&z>=1&&z<=2;
        }
        public static void Describe(Room room,LayoutConfig c)
        {
            var p=Plan(c,room.X,room.Z);room.ZoneId=-1;room.Width=room.Depth=c.CellSize;room.Height=p.Roof+1.2f;
            var quiet=new QuietLevelPlan(room.Environment,CourtyardDistrictPlan.Floors,(int)(p.Seed%12),p.Seed,Array.Empty<QuietPropPlan>(),new WaterRegion(),courtyard:p);
            room.Plan=new ProceduralPlan(room.Environment,Array.Empty<PlannedModule>(),Array.Empty<WaterRegion>(),p.Aisle,0,0,quiet:quiet);
        }
    }
}
