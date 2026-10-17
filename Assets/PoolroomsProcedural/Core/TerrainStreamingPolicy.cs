using System;

namespace Poolrooms.Core
{
    // Hysteresis keeps collision support while crossing back and forth over a tile boundary.
    public static class TerrainStreamingPolicy
    {
        public const float RetentionMargin=8;
        public static bool Detailed(int distance,float tileDistance,bool existingDetailed)
        {return distance<=1||existingDetailed&&tileDistance<=TerrainRegionPlan.TileSize*1.5f+RetentionMargin;}
        public static int Resolution(int distance,float tileDistance,int existingResolution,bool existingDetailed)
        {return Detailed(distance,tileDistance,existingDetailed)?32:distance<=2||existingResolution>=16&&tileDistance<=TerrainRegionPlan.TileSize*2.5f+RetentionMargin?16:8;}
        public static bool CanPublish(int distance,float tileDistance,int radius,int resolution,bool detailed,int existingResolution,bool existingDetailed)
        {
            return distance<=radius&&detailed==Detailed(distance,tileDistance,existingDetailed)&&
                resolution==Resolution(distance,tileDistance,existingResolution,existingDetailed);
        }
    }
}
