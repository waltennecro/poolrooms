using System;
using System.Collections.Generic;
namespace Poolrooms.Core
{
    public enum Sky63Use { Meditation,Tea,Wind,Sketch,Observatory,Letters,Chimes,Garden }
    public sealed class Sky63Plan
    {
        public const float Flight=5.2f,Landing=1.3f,StairWidth=3.6f,Gallery=3;
        public readonly uint Seed;
        public readonly int Column,Row;
        public readonly float Cell,Half,Clear,Upper,Q,Terrace;
        public readonly Sky63Use Use;
        public readonly bool EastWest;
        public readonly PlanRect Stair;
        public readonly PlanRect[] Ground,Above;
        public int Steps {get{return (int)Math.Ceiling(Upper/.36f);}}
        public Sky63Plan(LayoutConfig c,int x,int z)
        {
            Seed=LiminalRegions.Hash(c.Seed,x,z,0x630001u);Cell=c.CellSize;Half=Cell*.5f;
            Column=(int)(((long)x%4+4)%4);Row=(int)(((long)z%4+4)%4);
            uint district=LiminalRegions.Hash(c.Seed,ContinuousTopology.FloorDivide(x,4),ContinuousTopology.FloorDivide(z,4),0x630002u);
            Use=(Sky63Use)((Column+Row*4+(int)(district%8))%8);EastWest=(Column+Row)%2==0;
            Clear=Math.Max(2.2f,c.DoorWidth*.5f+.7f);Upper=Math.Max(5.8f,c.DoorHeight+.6f);Q=Half*.57f;Terrace=Math.Min(3.8f,Half-Q-.9f);
            Stair=new PlanRect(Half-.8f-StairWidth,-Half+.8f,Half-.8f,-Half+.8f+Flight+2*Landing);
            var ground=new List<PlanRect>();Add(ground,new PlanRect(-Half,-Clear,Half,Clear));Add(ground,new PlanRect(-Clear,-Half,Clear,Half));
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float radius=Terrace-(Use==Sky63Use.Meditation&&sx<0&&sz<0?0:(Seed%3)*.12f);
                Add(ground,new PlanRect(sx*Q-radius,sz*Q-radius,sx*Q+radius,sz*Q+radius));
                Add(ground,new PlanRect(Math.Min(0,sx*Q),sz*Q-1.4f,Math.Max(0,sx*Q),sz*Q+1.4f));
            }
            Add(ground,new PlanRect(-Clear,Stair.MinZ,Stair.MaxX,Stair.MinZ+Landing));
            Add(ground,Stair.Expanded(.15f));Ground=ground.ToArray();
            var above=new List<PlanRect>();
            Add(above,new PlanRect(-Half,-Half,Half,-Half+Gallery));Add(above,new PlanRect(-Half,Half-Gallery,Half,Half));
            Add(above,new PlanRect(-Half,-Half+Gallery,-Half+Gallery,Half-Gallery));Add(above,new PlanRect(Half-Gallery,-Half+Gallery,Half,Half-Gallery));
            Add(above,EastWest?new PlanRect(-Half,-1.4f,Half,1.4f):new PlanRect(-1.4f,-Half,1.4f,Half));
            // Keep the top landing; remove only the open shaft and intermediate landing.
            var shaft=new PlanRect(Stair.MinX,Stair.MinZ+Landing,Stair.MaxX,Stair.MaxZ+.03f);PlanRect.Subtract(above,shaft);Above=above.ToArray();
        }
        public static void Add(List<PlanRect> union,PlanRect next)
        {
            var parts=new List<PlanRect>{next};foreach(var prior in union)PlanRect.Subtract(parts,prior);union.AddRange(parts);
        }
        public bool Supports(float x,float z,bool upper)
        {foreach(var p in upper?Above:Ground)if(p.Contains(x,z))return true;return false;}
        public static bool Recover(float feet,bool flying,bool paused)
        {return !flying&&!paused&&ScenerySafety.Finite(feet)&&feet< -24;}
    }
    public static class Sky63Planner
    {
        public static bool Applies(LayoutConfig c,LiminalEnvironment e){return c.RebuildSky63&&e==LiminalEnvironment.CloudTerminal;}
        public static bool Available(LayoutConfig c,int x,int z)
        {
            if(!c.Continuous&&(x<0||z<0||x>=c.Columns||z>=c.Rows))return false;
            ConnectedTerrainSite terrain;ConnectedMallSite mall;
            return LiminalRegions.At(c,x,z)==LiminalEnvironment.CloudTerminal&&!ConnectedTerrainTopology.Reserved(c,x,z)&&!ConnectedTerrainTopology.Gateway(c,x,z,out terrain)&&!ConnectedMallTopology.At(c,x,z,out mall)&&!ConnectedMallTopology.Gateway(c,x,z,out mall);
        }
        public static bool Same(LayoutConfig c,int x,int z,int side)
        {
            if(!c.RebuildSky63||!Available(c,x,z))return false;
            long xx=(long)x+Directions.DX[side],zz=(long)z+Directions.DZ[side];
            return xx>=int.MinValue&&xx<=int.MaxValue&&zz>=int.MinValue&&zz<=int.MaxValue&&Available(c,(int)xx,(int)zz);
        }
        public static void Describe(Room r,LayoutConfig c)
        {
            var p=new Sky63Plan(c,r.X,r.Z);r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=18;
            var quiet=new QuietLevelPlan(r.Environment,2,(int)p.Use,p.Seed,Array.Empty<QuietPropPlan>(),new WaterRegion(),sky63:p);
            r.Plan=new ProceduralPlan(r.Environment,Array.Empty<PlannedModule>(),Array.Empty<WaterRegion>(),p.Clear,0,0,quiet:quiet);
        }
    }
}
