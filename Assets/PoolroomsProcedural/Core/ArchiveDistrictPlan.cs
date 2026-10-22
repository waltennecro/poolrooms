using System;

namespace Poolrooms.Core
{
    public enum ArchiveWingUse { Stacks, Reading, Scriptorium, Tea, Guest }
    // A three-column library: tall central naves continue along Z, flanked by
    // usable galleries and differently planned wings. No per-cell four-bay shell.
    public sealed class ArchiveDistrictPlan
    {
        public const int Floors=3;
        public const float Gallery=4.8f,Storey=4.2f,Landing=1.3f,Flight=5.2f,StairWidth=3.6f;
        public readonly int Column;
        public readonly bool Nave;
        public readonly float Cell,GroundHeight,Aisle;
        public readonly uint Seed;
        public readonly ArchiveWingUse Use;
        public readonly PlanRect Stair;
        public float Spring {get{return FloorY(2)+Storey;}}
        public float Roof {get{return Spring+(Nave?Cell*.17f:1.6f);}}
        public float FloorY(int f){return f==0?0:GroundHeight+(f-1)*Storey;}
        public float Rise(int f){return f==0?GroundHeight:Storey;}
        public int Steps(int f){return (int)Math.Ceiling(Rise(f)/.36f);}
        public ArchiveDistrictPlan(LayoutConfig c,int x,int z)
        {
            Column=x-ContinuousTopology.FloorDivide(x,3)*3;Nave=Column==1;Cell=c.CellSize;
            GroundHeight=Math.Max(4.8f,c.DoorHeight+.3f);Aisle=Math.Max(2.2f,c.DoorWidth*.5f+.55f);
            Seed=LiminalRegions.Hash(c.Seed,x,z,0x906A01u);
            long programme=ContinuousTopology.FloorDivide(z,2)+(long)(Seed%5);
            Use=(ArchiveWingUse)((programme%5+5)%5);
            float h=Cell*.5f;
            Stair=new PlanRect(-h+.65f,-h+1,-h+.65f+StairWidth,-h+1+Flight+2*Landing);
        }
        public bool OnGallery(float x,float z)
        {return !Nave||Math.Abs(x)>=Cell*.5f-Gallery||Math.Abs(z)<=1.6f;}
    }
    public static class ArchiveDistrictPlanner
    {
        public static bool Applies(LayoutConfig c,LiminalEnvironment e){return c.RebuildArchive906&&e==LiminalEnvironment.Archive906;}
        public static bool Available(LayoutConfig c,int x,int z)
        {
            if(!c.Continuous&&(x<0||z<0||x>=c.Columns||z>=c.Rows))return false;
            ConnectedTerrainSite terrain;ConnectedMallSite mall;
            return LiminalRegions.At(c,x,z)==LiminalEnvironment.Archive906&&!ConnectedTerrainTopology.Reserved(c,x,z)&&
                !ConnectedTerrainTopology.Gateway(c,x,z,out terrain)&&!ConnectedMallTopology.At(c,x,z,out mall)&&!ConnectedMallTopology.Gateway(c,x,z,out mall);
        }
        public static bool Same(LayoutConfig c,int x,int z,int side)
        {
            if(!c.RebuildArchive906||!Available(c,x,z))return false;
            long nx=(long)x+Directions.DX[side],nz=(long)z+Directions.DZ[side];
            return nx>=int.MinValue&&nx<=int.MaxValue&&nz>=int.MinValue&&nz<=int.MaxValue&&Available(c,(int)nx,(int)nz);
        }
        public static void Describe(Room r,LayoutConfig c)
        {
            var p=new ArchiveDistrictPlan(c,r.X,r.Z);r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=p.Roof+.3f;
            var quiet=new QuietLevelPlan(r.Environment,ArchiveDistrictPlan.Floors,(int)(p.Seed%5),p.Seed,Array.Empty<QuietPropPlan>(),new WaterRegion(),archive:p);
            r.Plan=new ProceduralPlan(r.Environment,Array.Empty<PlannedModule>(),Array.Empty<WaterRegion>(),p.Aisle,0,0,quiet:quiet);
        }
    }
}
