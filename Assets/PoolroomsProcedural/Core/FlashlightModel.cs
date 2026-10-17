using System;

namespace Poolrooms.Core
{
    public enum FlashlightLevel { VeryLow, Low, Normal, SomewhatHigh, High, SolarRay }
    [Serializable]
    public sealed class FlashlightPreferences
    {
        public FlashlightLevel level=FlashlightLevel.Normal;
        public bool shadows=true;
        public void Sanitize(){if(!Enum.IsDefined(typeof(FlashlightLevel),level))level=FlashlightLevel.Normal;}
    }
    public readonly struct FlashlightLook
    {
        public readonly string Name;
        public readonly float Lumens,Range,Cone,InnerCone;
        public readonly V3 Color;
        public FlashlightLook(string name,float lumens,float range,float cone,float inner,V3 color)
        {Name=name;Lumens=lumens;Range=range;Cone=cone;InnerCone=inner;Color=color;}
    }
    public static class FlashlightModel
    {
        public const int Count=6;
        public static FlashlightLook Look(FlashlightLevel level)
        {
            var neutral=new V3(1,.97f,.9f);
            switch(level)
            {
                case FlashlightLevel.VeryLow:return new FlashlightLook("Muy baja",35,8,42,25,neutral);
                case FlashlightLevel.Low:return new FlashlightLook("Baja",150,12,46,28,neutral);
                case FlashlightLevel.SomewhatHigh:return new FlashlightLook("Algo alta",1800,25,54,32,neutral);
                case FlashlightLevel.High:return new FlashlightLook("Alta",6000,36,62,38,neutral);
                case FlashlightLevel.SolarRay:return new FlashlightLook("Rayo solar",250000,75,85,65,new V3(1,.91f,.66f));
                default:return new FlashlightLook("Normal",650,18,50,30,neutral);
            }
        }
        public static FlashlightLevel Next(FlashlightLevel level)
        {return (FlashlightLevel)((Enum.IsDefined(typeof(FlashlightLevel),level)?(int)level+1:2)%Count);}
    }
}
