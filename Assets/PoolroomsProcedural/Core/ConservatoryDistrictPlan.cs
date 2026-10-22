using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum ConservatoryUse { PalmGrove, FernGarden, RockGarden, OrchidHouse, TeaPavilion, SeedNursery, Herbarium, WaterGarden }
    public readonly struct ConservatoryBed
    {
        public readonly float X,Z,Width,Depth,Height;
        public readonly int Species;
        public PlanRect Bounds {get{return new PlanRect(X-Width*.5f,Z-Depth*.5f,X+Width*.5f,Z+Depth*.5f);}}
        public ConservatoryBed(float x,float z,float width,float depth,float height,int species)
        {X=x;Z=z;Width=width;Depth=depth;Height=height;Species=species;}
    }
    public sealed class ConservatoryDistrictPlan
    {
        public const int Span=4;
        public const float Gallery=4.6f,Bridge=1.6f,Flight=5.2f,Landing=1.3f,StairWidth=3.6f;
        public readonly int Column,Row;
        public readonly float Cell,Upper,Aisle,Lift;
        public readonly uint Seed,DistrictSeed;
        public readonly bool Complete,Court;
        public readonly ConservatoryUse Use;
        public readonly PlanRect Stair;
        public readonly ConservatoryBed[] Beds;
        public readonly WaterRegion Pond;
        public bool StairEast {get{return Court&&Column==2;}}
        public bool StairNorth {get{return Court&&Row==2;}}
        public bool PavilionEast {get{return StairNorth&&!StairEast;}}
        public float RoofBase {get{return Math.Max(10.5f,Upper+3.2f);}}
        public float Roof {get{return RoofBase+Lift;}}
        public int Steps {get{return (int)Math.Ceiling(Upper/.36f);}}
        public ConservatoryDistrictPlan(LayoutConfig c,int x,int z,bool complete)
        {
            Column=(int)(((long)x%4+4)%4);Row=(int)(((long)z%4+4)%4);Complete=complete;
            Court=complete&&(Column==1||Column==2)&&(Row==1||Row==2);
            Cell=c.CellSize;Upper=Math.Max(5.8f,c.DoorHeight+.6f);Aisle=Math.Max(2.5f,c.DoorWidth*.5f+.65f);
            Seed=LiminalRegions.Hash(c.Seed,x,z,0xC0A501u);
            DistrictSeed=LiminalRegions.Hash(c.Seed,ContinuousTopology.FloorDivide(x,4),ContinuousTopology.FloorDivide(z,4),0xC0A502u);
            Lift=6+(DistrictSeed%4)*1.4f;
            Use=(ConservatoryUse)((Column+Row*4+(int)(DistrictSeed%8))%8);
            float h=Cell*.5f,sx0=StairEast?h-.8f-StairWidth:-h+.8f,sz0=StairNorth?h-.8f-Flight-2*Landing:-h+.8f;
            Stair=new PlanRect(sx0,sz0,sx0+StairWidth,sz0+Flight+2*Landing);
            if(Court)
            {
                float width=Cell*.17f;
                Pond=new WaterRegion{CenterX=(Column==1?1:-1)*(h-width*.5f),CenterZ=(Row==1?1:-1)*(h-width*.5f),Width=width,Depth=width,Bottom=-.65f,Level=-.07f};
            }
            else if(Use==ConservatoryUse.WaterGarden)
                Pond=new WaterRegion{CenterX=Cell*.25f,CenterZ=Cell*.24f,Width=Cell*.18f,Depth=Cell*.15f,Bottom=-.65f,Level=-.07f};
            var beds=new List<ConservatoryBed>();
            float center=(Aisle+h-Gallery)*.5f,available=h-Gallery-Aisle-1;
            float radius=Math.Min(4.2f,available*.43f);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                // The stair occupies the corner opposite the shared quarter pond.
                if(sx==(StairEast?1:-1)&&sz==(StairNorth?1:-1)||Use==ConservatoryUse.TeaPavilion&&sx==(PavilionEast?1:-1)&&sz>0)continue;
                float bx=sx*center,bz=sz*center;
                int species=Use==ConservatoryUse.PalmGrove?0:Use==ConservatoryUse.FernGarden?1:Use==ConservatoryUse.RockGarden?3:Use==ConservatoryUse.OrchidHouse?2:(int)((Seed+(uint)(sx+sz+4))%4);
                float w=radius*(Use==ConservatoryUse.FernGarden?1.6f:2),d=radius*(Use==ConservatoryUse.FernGarden?2.3f:1.65f);
                var bed=new ConservatoryBed(bx,bz,w,d,Use==ConservatoryUse.RockGarden?.62f:.38f,species);
                if(Pond.Width>0&&bed.Bounds.Overlaps(PondBounds.Expanded(.45f)))continue;
                beds.Add(bed);
            }
            Beds=beds.ToArray();
        }
        public PlanRect PondBounds {get{return new PlanRect(Pond.CenterX-Pond.Width*.5f,Pond.CenterZ-Pond.Depth*.5f,Pond.CenterX+Pond.Width*.5f,Pond.CenterZ+Pond.Depth*.5f);}}
        public float RoofY(float x,float z)
        {
            // Zero lift at every 4x4 district edge, independent of the neighbour's
            // seed. Adjacent chunks share exactly the same curved glass boundary.
            double dx=((Column-1.5)*Cell+x)/(2*Cell),dz=((Row-1.5)*Cell+z)/(2*Cell);
            double a=Math.Cos(dx*Math.PI*.5),b=Math.Cos(dz*Math.PI*.5);
            return RoofBase+Lift*(float)(a*a*b*b);
        }
        public bool UpperFloor(float x,float z)
        {return Math.Abs(x)>=Cell*.5f-Gallery||Math.Abs(z)>=Cell*.5f-Gallery||Math.Abs(x)<=Bridge||Math.Abs(z)<=Bridge;}
    }
    public static class ConservatoryDistrictPlanner
    {
        public static bool Applies(LayoutConfig c,LiminalEnvironment e){return c.RebuildConservatory&&e==LiminalEnvironment.Conservatory;}
        public static bool Available(LayoutConfig c,int x,int z)
        {
            if(!c.Continuous&&(x<0||z<0||x>=c.Columns||z>=c.Rows))return false;
            ConnectedTerrainSite terrain;ConnectedMallSite mall;
            return LiminalRegions.At(c,x,z)==LiminalEnvironment.Conservatory&&!ConnectedTerrainTopology.Reserved(c,x,z)&&!ConnectedTerrainTopology.Gateway(c,x,z,out terrain)&&!ConnectedMallTopology.At(c,x,z,out mall)&&!ConnectedMallTopology.Gateway(c,x,z,out mall);
        }
        public static bool Same(LayoutConfig c,int x,int z,int side)
        {
            if(!c.RebuildConservatory||!Available(c,x,z))return false;
            long xx=(long)x+Directions.DX[side],zz=(long)z+Directions.DZ[side];
            return xx>=int.MinValue&&xx<=int.MaxValue&&zz>=int.MinValue&&zz<=int.MaxValue&&Available(c,(int)xx,(int)zz);
        }
        public static ConservatoryDistrictPlan Plan(LayoutConfig c,int x,int z)
        {
            long ox=(long)ContinuousTopology.FloorDivide(x,4)*4,oz=(long)ContinuousTopology.FloorDivide(z,4)*4;bool complete=true;
            for(int row=0;row<4&&complete;row++)for(int col=0;col<4;col++)
            {
                long xx=ox+col,zz=oz+row;
                if(xx<int.MinValue||xx>int.MaxValue||zz<int.MinValue||zz>int.MaxValue||!Available(c,(int)xx,(int)zz)){complete=false;break;}
            }
            return new ConservatoryDistrictPlan(c,x,z,complete);
        }
        public static void Describe(Room r,LayoutConfig c)
        {
            var p=Plan(c,r.X,r.Z);r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=p.Roof+.3f;
            var quiet=new QuietLevelPlan(r.Environment,2,(int)p.Use,p.Seed,Array.Empty<QuietPropPlan>(),new WaterRegion(),conservatory:p);
            r.Plan=new ProceduralPlan(r.Environment,Array.Empty<PlannedModule>(),p.Pond.Width>0?new[]{p.Pond}:Array.Empty<WaterRegion>(),p.Aisle,0,0,quiet:quiet);
        }
    }
}
