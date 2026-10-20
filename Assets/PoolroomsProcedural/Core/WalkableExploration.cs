using System;

namespace Poolrooms.Core
{
    // The mixed exploration mode includes every implemented destination. Dedicated
    // single-biome/catalogue scenes and explicitly custom worlds keep their settings.
    public static class WalkableExploration
    {
        public const int RegionBlock=8;
        public static void Configure(LayoutConfig c)
        {
            c.EncounterExploration=true;c.EnvironmentRegionSize=3;c.LoopChance=Math.Max(c.LoopChance,.65f);
            c.ExpandedVariety=true;c.ExpandedBiomeAreas=true;c.EnableRestfulLevels=true;c.ObjectAbundance=3;
            c.Continuous=true;c.Procedural=LiminalGeneration.Expanded;c.DiscoveryCoverage=true;
            c.EnableRainHouses=c.EnableAquaticPark=c.EnableThermalBaths=c.EnableOrangerie=true;
            c.EnableQuietLevels=c.EnableCivicLevels=c.EnableCity11=c.EnableSoftPlay=true;
            c.ConnectedMalls=c.ConnectedTerrain=c.CommonPoolrooms=true;
            c.RainHouseShare=Positive(c.RainHouseShare,.1f);c.AquaticParkShare=Positive(c.AquaticParkShare,.12f);
            c.ThermalBathShare=Positive(c.ThermalBathShare,.11f);c.OrangerieShare=Positive(c.OrangerieShare,.14f);
            c.QuietLevelShare=Positive(c.QuietLevelShare,.18f);c.CivicLevelShare=Positive(c.CivicLevelShare,.12f);
            c.City11Share=Positive(c.City11Share,.15f);c.SoftPlayShare=Positive(c.SoftPlayShare,.18f);
            c.AdditionalEnvironmentShare=Positive(c.AdditionalEnvironmentShare,.5f);c.IndoorPlayShare=Positive(c.IndoorPlayShare,.45f);c.SupermarketShare=Positive(c.SupermarketShare,.3f);
            c.ConnectedMallShare=Positive(c.ConnectedMallShare,.6f);c.ConnectedTerrainShare=Positive(c.ConnectedTerrainShare,.25f);
            if(c.CommonSolarShare<=0||c.CommonSolarShare>=1)c.CommonSolarShare=.5f;
        }
        static float Positive(float value,float fallback){return value>0?value:fallback;}
        public static bool Enabled(LayoutConfig c,LiminalEnvironment e)
        {
            if(RestfulLevelPlanner.IsLevel(e))return c.EnableRestfulLevels;
            switch(e)
            {
                case LiminalEnvironment.RainHouses:return c.EnableRainHouses&&c.RainHouseShare>0;
                case LiminalEnvironment.AquaticPark:return c.EnableAquaticPark&&c.AquaticParkShare>0;
                case LiminalEnvironment.ThermalBaths:return c.EnableThermalBaths&&c.ThermalBathShare>0;
                case LiminalEnvironment.Orangerie:return c.EnableOrangerie&&c.OrangerieShare>0;
                case LiminalEnvironment.Archive906:case LiminalEnvironment.Courtyard188:case LiminalEnvironment.Coast48:return c.EnableQuietLevels&&c.QuietLevelShare>0;
                case LiminalEnvironment.Conservatory:case LiminalEnvironment.TransitGallery:return c.EnableCivicLevels&&c.CivicLevelShare>0;
                case LiminalEnvironment.City11:return c.EnableCity11&&c.City11Share>0;
                case LiminalEnvironment.SoftPlay:return c.EnableSoftPlay&&c.SoftPlayShare>0;
                case LiminalEnvironment.Office:case LiminalEnvironment.Hotel:case LiminalEnvironment.WaitingHall:case LiminalEnvironment.DiningHall:return c.AdditionalEnvironmentShare>0;
                case LiminalEnvironment.IndoorPlay:return c.IndoorPlayShare>0;
                case LiminalEnvironment.Supermarket:return c.SupermarketShare>0;
                default:return e==LiminalEnvironment.Poolrooms;
            }
        }
        // Two distinct candidate regions for each enabled environment in a shuffled
        // 8x8 region block. Remaining regions retain the existing weighted selection.
        // This is a spatial availability rule, never a required itinerary or level order.
        public static bool Region(LayoutConfig c,int x,int z,out LiminalEnvironment environment)
        {
            environment=LiminalEnvironment.Poolrooms;
            if(!c.DiscoveryCoverage||c.Procedural!=LiminalGeneration.Expanded)return false;
            int bx=ContinuousTopology.FloorDivide(x,RegionBlock),bz=ContinuousTopology.FloorDivide(z,RegionBlock);
            int lx=(int)(x-(long)bx*RegionBlock),lz=(int)(z-(long)bz*RegionBlock);
            uint h=LiminalRegions.Hash(c.Seed,bx,bz,0x250A01u);
            // A keyed small Feistel permutation is bijective without an allocated
            // shuffle table, and avoids a visible linear/diagonal biome sequence.
            int left=lx,right=lz;
            for(int round=0;round<4;round++)
            {int next=left^(int)(LiminalRegions.Hash(unchecked((int)h),right,round,0x250A02u)&7);left=right;right=next;}
            int rank=(left<<3)|right,count=0;
            for(int i=0;i<RestfulLevelPlanner.EnvironmentCount;i++)if(Enabled(c,(LiminalEnvironment)i))count++;
            if(rank>=2*count)return false;int pick=rank%count;
            for(int i=0;i<RestfulLevelPlanner.EnvironmentCount;i++)if(Enabled(c,(LiminalEnvironment)i)&&pick--==0){environment=(LiminalEnvironment)i;return true;}
            throw new InvalidOperationException("Invalid discovery coverage.");
        }
    }
}
