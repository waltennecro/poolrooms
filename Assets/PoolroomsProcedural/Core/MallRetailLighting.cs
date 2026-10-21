using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class MallRetailLighting
    {
        public const int MaximumFixturesPerShop=64;
        public static IEnumerable<V3> Grid(MallShop shop)
        {
            int columns=Math.Max(2,(int)Math.Ceiling(shop.Width/8));
            int rows=Math.Max(1,Math.Min(MaximumFixturesPerShop/columns,(int)Math.Ceiling((shop.Depth-6)/8)));
            for(int row=0;row<rows;row++)for(int column=0;column<columns;column++)
                yield return new V3(-shop.Width*.5f+(column+.5f)*shop.Width/columns,4.97f,3+(row+.5f)*(shop.Depth-6)/rows);
        }
        public static bool Open(MallPlan p,MallShop s)
        {
            var condition=MallArchitectureV20.Condition(p,s.Floor,s.Sector);
            return condition!=MallCondition.Abandoned||s.Id%5==0;
        }
        public static V3 Emission(MallShopKind kind)
        {float t=Temperature(kind);return t<3500?new V3(1,.73f,.49f):t<4000?new V3(1,.79f,.61f):new V3(1,.86f,.73f);}
        public static float Temperature(MallShopKind kind)
        {return kind==MallShopKind.Cafe||kind==MallShopKind.FoodCourt||kind==MallShopKind.Home||kind==MallShopKind.Books?3400:kind==MallShopKind.Pharmacy||kind==MallShopKind.Electronics||kind==MallShopKind.Hypermarket?4300:3800;}
        public static void Apply(MallSectorGeometry sector,MallPlan p,int floor,int index)
        {
            var geometry=sector.Geometry;var lights=geometry.Fixtures;
            // Replace the two deep-store floodlights, rather than adding another
            // overlapping layer. Gallery fills remain diffuse, floor-local bounce.
            for(int i=lights.Count-1;i>=0;i--)
            {
                var light=lights[i];
                if(Math.Abs(light.Position.Z)>=MallPlan.ShopFront){lights.RemoveAt(i);continue;}
                light.AmbientSupport=true;light.Power=8500;light.Temperature=4900;
                light.Width=14;light.Depth=5;light.Range=light.ShadowlessRange=8;
                lights[i]=light;
            }
            foreach(var shop in p.Shops)
            {
                if(shop.Floor!=floor||shop.Sector!=index)continue;
                bool open=Open(p,shop),dim=shop.Kind==MallShopKind.Cinema||shop.Kind==MallShopKind.Arcade;
                foreach(var point in Grid(shop))
                {
                    float power=!open?260:dim?1400:12000;
                    var position=shop.World(point.X,point.Y,point.Z);
                    lights.Add(new Fixture{Position=position,Width=2.28f,Depth=.11f,Power=power,
                        Temperature=Temperature(shop.Kind),Range=11,ShadowlessRange=7.8f,
                        MallLocal=true,FloorOffset=4.97f});
                }
                // Window light is shallow, warm and colocated with the reveal.
                foreach(int side in new[]{-1,1})
                    lights.Add(new Fixture{Position=shop.World(side*(shop.Width*.5f-1.3f),3.3f,.7f),
                        Width=.9f,Depth=.35f,Power=open?850:180,Temperature=Temperature(shop.Kind),
                        Range=6,ShadowlessRange=4.8f,MallLocal=true,FloorOffset=3.3f});
            }
        }
    }
}
