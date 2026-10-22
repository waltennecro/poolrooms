using System;

namespace Poolrooms.Core
{
    public readonly struct MallShopLight
    {
        public readonly bool Lit;
        public readonly float Power,Temperature;
        public MallShopLight(bool lit,float power,float temperature){Lit=lit;Power=power;Temperature=temperature;}
    }
    public static class MallLighting
    {
        public static Fixture Sun(MallPlan p,V3 position)
        {
            float height=p.Floors*MallPlan.FloorHeight;
            // Compensate for the roof-to-ground distance, rather than making six floors
            // receive the same weak flux as a two-floor building.
            return new Fixture{Position=position,Width=5,Depth=5,Power=Math.Max(60000,Math.Min(650000,70000*height*height/144)),
                Temperature=5700,Skylight=true,SolarBeam=true,MallSunlight=MallRealism.Enabled(p),Aim=new V3(0,-1,.1f).Unit,ConeAngle=55,Range=height+18};
        }
        public static MallShopLight Shop(MallPlan p,MallShop s)
        {
            MallCondition condition=MallArchitectureV20.Condition(p,s.Floor,s.Sector);
            int threshold=condition==MallCondition.Maintained?8:condition==MallCondition.PartialLighting?5:1;
            bool lit=LiminalRegions.Hash(p.Seed,p.AddressX,p.AddressZ,0xCA320u+(uint)s.Id)%10<threshold;
            float temperature=s.Kind==MallShopKind.Cafe||s.Kind==MallShopKind.FoodCourt?3200:s.Kind==MallShopKind.Electronics||s.Kind==MallShopKind.Pharmacy?4800:4100;
            float power=lit?18000:condition==MallCondition.Abandoned?350:1400;
            if(s.Kind==MallShopKind.Hypermarket&&lit)power=26000;
            if(s.Kind==MallShopKind.Cinema||s.Kind==MallShopKind.Arcade){power=lit?3200:600;temperature=3300;}
            return new MallShopLight(lit,power,temperature);
        }
        public static void Sector(MallSectorGeometry result,MallPlan p,int floor,int sector)
        {
            var fixtures=result.Geometry.Fixtures;
            for(int i=0;i<fixtures.Count;i++)
            {
                Fixture f=fixtures[i];f.MallLocal=true;
                if(Math.Abs(f.Position.Z)<MallPlan.ShopFront)
                {
                    // Distributed sky bounce reuses the two existing gallery lamps, not two
                    // range-32 lamps at x=0 for the entire 200-metre building.
                    f.SolarFill=true;f.Position.Y=floor*MallPlan.FloorHeight+3.8f;
                    f.Width=14;f.Depth=5;f.Power=10500;f.Temperature=5200;f.Range=f.ShadowlessRange=4.9f;
                }
                else foreach(var s in p.Shops)if(s.Floor==floor&&s.Sector==sector&&Math.Sign(f.Position.Z)==s.Side&&Math.Abs(f.Position.X-s.X)<=s.Width*.5f+.01f)
                {
                    MallShopLight look=Shop(p,s);bool extra=Math.Abs(f.Position.X-s.X)>.1f;
                    if(!extra)f.Position=s.World(0,4.6f,s.Depth*.4f);
                    f.Power=look.Power*(extra?.65f:1);f.Temperature=look.Temperature;
                    f.Width=Math.Min(14,s.Width*.75f);f.Depth=Math.Min(14,s.Depth*.65f);f.Range=Math.Min(35,s.Depth+4);f.ShadowlessRange=5.0f;break;
                }
                f.FloorOffset=f.Position.Y-floor*MallPlan.FloorHeight;fixtures[i]=f;
            }
        }
        public static bool FloorEligible(float viewerMinusLampY,float offset,float storeyHeight=0)
        {
            float relative=viewerMinusLampY+offset;
            float height=storeyHeight>0?storeyHeight:MallPlan.FloorHeight;
            return !float.IsNaN(relative)&&!float.IsInfinity(relative)&&relative>=-.45f&&relative<height-.45f;
        }
        public static float Range(float full,float shadowless,bool shadows)
        {return !shadows&&shadowless>0?Math.Min(full,shadowless):full;}
    }
}
