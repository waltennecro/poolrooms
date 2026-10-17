using System;

namespace Poolrooms.Core
{
    public readonly struct CityBuildingPlan
    {
        public readonly float X,Z,Width,Depth;
        public readonly int Floors,Program,Facade;
        public readonly uint Seed;
        public readonly bool Turn;
        public CityBuildingPlan(float x,float z,float width,float depth,int floors,int program,int facade,uint seed,bool turn)
        {X=x;Z=z;Width=width;Depth=depth;Floors=floors;Program=program;Facade=facade;Seed=seed;Turn=turn;}
        public V3 Point(float x,float y,float z){return new V3(X+(Turn?-x:x),y,Z+(Turn?-z:z));}
    }
    public readonly struct CityPropPlan
    {
        public readonly V3 Position;
        public readonly int Source;
        public CityPropPlan(V3 position,int source){Position=position;Source=source;}
    }
    public sealed class City11Plan
    {
        public readonly CityBuildingPlan[] Buildings;
        public readonly CityPropPlan[] Props;
        public City11Plan(CityBuildingPlan[] buildings,CityPropPlan[] props){Buildings=buildings;Props=props;}
        public bool Valid(Room room,LayoutConfig c)
        {
            if(room.Environment!=LiminalEnvironment.City11||room.Width!=c.CellSize||room.Depth!=c.CellSize||Buildings.Length!=4||Props.Length!=8)return false;
            foreach(var b in Buildings)
                if(b.Width<7.7f||b.Depth<7.7f||b.Floors<2||b.Floors>6||Math.Abs(b.X)+b.Width*.5f>room.Width*.5f-.5f||Math.Abs(b.Z)+b.Depth*.5f>room.Depth*.5f-.5f||Math.Abs(b.X)-b.Width*.5f<4.7f||Math.Abs(b.Z)-b.Depth*.5f<4.7f)return false;
            return true;
        }
    }
    public static class City11Planner
    {
        public static void Describe(Room room,LayoutConfig c)
        {
            room.ZoneId=-1;room.Environment=LiminalEnvironment.City11;room.Width=room.Depth=c.CellSize;room.Height=22;
            var rng=new StableRandom(LiminalRegions.Hash(c.Seed,room.X,room.Z,0x110001u));
            var buildings=new CityBuildingPlan[4];var props=new CityPropPlan[8];int index=0;
            for(int z=-1;z<=1;z+=2)for(int x=-1;x<=1;x+=2)
            {
                float maximum=Math.Min(18,c.CellSize*.5f-5.4f),minimum=Math.Max(7.8f,maximum-2);
                float w=rng.Range(minimum,maximum),d=rng.Range(minimum,maximum);
                var b=new CityBuildingPlan(x*(4.8f+w*.5f),z*(4.8f+d*.5f),w,d,2+rng.Range(5),rng.Range(8),rng.Range(4),rng.Next(),z<0);buildings[index]=b;
                props[index*2]=new CityPropPlan(b.Point(w*.23f,1.02f,0),0);
                props[index*2+1]=new CityPropPlan(b.Point(w*.23f,1.06f,d*.28f),1);index++;
            }
            var city=new City11Plan(buildings,props);
            room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],4.7f,0,0,city:city);
            if(!city.Valid(room,c))throw new ArgumentException("City blocks must preserve streets and building access.");
        }
    }
}
