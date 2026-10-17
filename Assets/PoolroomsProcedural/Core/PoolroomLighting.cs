using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Readability belongs to the Poolrooms palette, not to every biome or the player's torch.
    public static class PoolroomLighting
    {
        public static bool IsDim(ZoneLightStyle style)
        {
            return style==ZoneLightStyle.BlueDusk||style==ZoneLightStyle.CyanDepth||
                style==ZoneLightStyle.SparseFluorescent||style==ZoneLightStyle.HalfPower||
                style==ZoneLightStyle.EmergencyGuides||style==ZoneLightStyle.MoonShaft;
        }
        public static LightingLook Look(Room room,LayoutConfig config)
        {
            var style=ZoneLighting.Resolve(room,config);var original=ZoneLighting.Get(style);
            if(room.Environment!=LiminalEnvironment.Poolrooms||config.Lighting==LightingDistribution.Classic||!IsDim(style))return original;
            float power=style==ZoneLightStyle.EmergencyGuides?.26f:Math.Max(.52f,original.Power);
            return new LightingLook(original.Name,original.Color,power,Math.Max(.28f,original.Emission),18);
        }
        public static void AddSupport(List<Fixture> fixtures,Room room,LightingLook look)
        {
            // One inexpensive broad source, bounded inside the room even without shadows.
            // No extra mesh/shadow slot and no spill into a neighbouring biome.
            float lumens=Math.Max(2200,Math.Min(4800,room.Width*room.Depth*6.4f));
            float height=room.Floor+Math.Min(3.4f,room.Height-room.Floor-.35f);
            float range=Math.Min(18,Math.Min(room.Width,room.Depth)*.5f-.85f);
            fixtures.Add(new Fixture{
                Position=new V3(0,height,0),Width=1.4f,Depth=1.4f,
                Power=lumens,Temperature=5200,CustomColor=true,LightColor=look.Color,
                Range=range,SolarFill=true,AmbientSupport=true});
        }
    }

    public readonly struct PoolroomSunLook
    {
        public readonly float Lux,ExposureOffset,AngularDiameter;
        public PoolroomSunLook(float lux,float exposure,float diameter)
        {Lux=lux;ExposureOffset=exposure;AngularDiameter=diameter;}
    }
    public static class PoolroomSunPolicy
    {
        public static PoolroomSunLook Get(PoolClimate climate)
        {
            switch(climate)
            {
                case PoolClimate.Sunlit:return new PoolroomSunLook(18000,4,.53f);
                case PoolClimate.DiffuseDaylight:return new PoolroomSunLook(9000,3.5f,.9f);
                case PoolClimate.Sunset:return new PoolroomSunLook(12000,3.75f,.65f);
                default:return new PoolroomSunLook(0,0,.53f);
            }
        }
        public static bool CanIlluminate(bool sunnyRoom,bool otherBiomeSun,int lights,int shadows)
        {return sunnyRoom&&!otherBiomeSun&&lights>0&&shadows>0;}
    }
}
