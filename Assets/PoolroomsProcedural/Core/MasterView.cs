using System;

namespace Poolrooms.Core
{
    [Serializable]
    public sealed class MasterViewOptions
    {
        // Zero preserves the scene's budget. Graphics preferences persist these overrides.
        public int roomRadius,terrainRadius,mallRadius,mallFloorRadius;
        public bool inspectionLight;
        public float lightStrength=.35f;
        public void Sanitize()
        {
            roomRadius=MasterViewPolicy.Clamp(roomRadius,0,12);
            terrainRadius=terrainRadius==0?0:MasterViewPolicy.Clamp(terrainRadius,2,8);
            mallRadius=MasterViewPolicy.Clamp(mallRadius,0,14);
            mallFloorRadius=MasterViewPolicy.Clamp(mallFloorRadius,0,5);
            lightStrength=float.IsNaN(lightStrength)||float.IsInfinity(lightStrength)?.35f:Math.Max(0,Math.Min(1,lightStrength));
        }
    }
    public static class MasterViewPolicy
    {
        public static int Clamp(int value,int min,int max){return Math.Max(min,Math.Min(max,value));}
        public static int Radius(int requested,int fallback,int min,int max){return Clamp(requested==0?fallback:requested,min,max);}
        public static int RoomCount(int radius){radius=Clamp(radius,1,12);return (radius*2+1)*(radius*2+1);}
        public static float FillLux(float strength,float exposureEV)
        {
            if(float.IsNaN(strength)||float.IsInfinity(strength))strength=.35f;
            if(float.IsNaN(exposureEV)||float.IsInfinity(exposureEV))exposureEV=5.5f;
            return (float)Math.Pow(2,Math.Max(-10,Math.Min(18,exposureEV)))*(.04f+.22f*Math.Max(0,Math.Min(1,strength)));
        }
    }
}
