using System;

namespace Poolrooms.Core
{
    // Appended, opt-in settings keep all existing authored worlds intact.
    public enum LiminalGeneration { Authored, Poolrooms, IndoorPlay, Mixed, Supermarkets, MixedWithRetail, Expanded, Offices, Hotels, WaitingHalls, DiningHalls, SoftPlay, City11, Archive906, Courtyard188, Coast48, Conservatory, TransitGallery, Orangerie, ThermalBaths, AquaticPark, RainHouses, SunsetLibrary, CloudTerminal, TeaPavilions, CeramicGalleria, WinterGarden, CanalGalleries, MusicStudios, LanternCourts, LinenLaundries, MineralSanctuary, SolarObservatory, CoveredMarket, TextileAtelier, PostalArcade, IndoorMarina, ClockGallery, PaperWorkshop, ChessPavilion, RadioSalon, MirrorArcade, AquariumGalleries, BambooRetreat, DesertCaravanserai, CrystalMuseum, CloudBaths, SunkenLibrary, RailwayWintergarden, CeramicVillage, LunarGreenhouse, AmberRestaurant, MapmakersArchive, SculptureCourts, PaperLanternTown, WatermillHalls, IndoorBoardwalk, VelvetTheatre, HangingOrchards, RainyBookshops, MosaicCistern, AeronauticalGallery }
    public enum LiminalEnvironment { Poolrooms, IndoorPlay, Supermarket, Office, Hotel, WaitingHall, DiningHall, SoftPlay, City11, Archive906, Courtyard188, Coast48, Conservatory, TransitGallery, Orangerie, ThermalBaths, AquaticPark, RainHouses, SunsetLibrary, CloudTerminal, TeaPavilions, CeramicGalleria, WinterGarden, CanalGalleries, MusicStudios, LanternCourts, LinenLaundries, MineralSanctuary, SolarObservatory, CoveredMarket, TextileAtelier, PostalArcade, IndoorMarina, ClockGallery, PaperWorkshop, ChessPavilion, RadioSalon, MirrorArcade, AquariumGalleries, BambooRetreat, DesertCaravanserai, CrystalMuseum, CloudBaths, SunkenLibrary, RailwayWintergarden, CeramicVillage, LunarGreenhouse, AmberRestaurant, MapmakersArchive, SculptureCourts, PaperLanternTown, WatermillHalls, IndoorBoardwalk, VelvetTheatre, HangingOrchards, RainyBookshops, MosaicCistern, AeronauticalGallery }

    public static class LiminalRegions
    {
        // Separate 64-bit coordinate mixing avoids seed/x equivalences of the legacy hash.
        // Legacy seeds and authored meshes keep their existing hash function.
        public static uint Hash(int seed,int x,int z,uint salt)
        {
            unchecked
            {
                ulong key=(ulong)ContinuousTopology.CellKey(x,z)+(ulong)(uint)seed*0xD6E8FEB86659FD93UL;
                key^=(ulong)salt*0x9E3779B97F4A7C15UL;
                key=(key^(key>>30))*0xBF58476D1CE4E5B9UL;
                key=(key^(key>>27))*0x94D049BB133111EBUL;
                key^=key>>31;return (uint)(key^(key>>32));
            }
        }
        public static LiminalEnvironment At(LayoutConfig c,int x,int z)
        {
            LiminalEnvironment single;
            if(Single(c.Procedural,out single))return single;
            if(c.Procedural==LiminalGeneration.Supermarkets)return LiminalEnvironment.Supermarket;
            if(c.Procedural==LiminalGeneration.IndoorPlay)return LiminalEnvironment.IndoorPlay;
            if(c.Procedural!=LiminalGeneration.Mixed&&c.Procedural!=LiminalGeneration.MixedWithRetail&&c.Procedural!=LiminalGeneration.Expanded)return LiminalEnvironment.Poolrooms;
            // The same scaled Voronoi field drives coastlines and mixed biome selection.
            int selectedX,selectedZ;BiomeExtent.NearestRegion(c,x,z,out selectedX,out selectedZ);
            if(selectedX==0&&selectedZ==0)return LiminalEnvironment.Poolrooms;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableJourneyLevels&&!c.DiscoveryCoverage&&Unit(Hash(c.Seed,selectedX,selectedZ,0x510001u))<.32f)
                return (LiminalEnvironment)(JourneyLevelPlanner.FirstEnvironment+Hash(c.Seed,selectedX,selectedZ,0x510002u)%JourneyLevelPlanner.Count);
            LiminalEnvironment discovered;if(WalkableExploration.Region(c,selectedX,selectedZ,out discovered))return discovered;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableSpecialRegions&&Unit(Hash(c.Seed,selectedX,selectedZ,0x401011u))<.18f)
                return (LiminalEnvironment)(SpecialRegionPlanner.FirstEnvironment+Hash(c.Seed,selectedX,selectedZ,0x401012u)%SpecialRegionPlanner.Count);
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableRestfulLevels&&Unit(Hash(c.Seed,selectedX,selectedZ,0x330011u))<.3f)
                return (LiminalEnvironment)((int)LiminalEnvironment.SunsetLibrary+Hash(c.Seed,selectedX,selectedZ,0x330012u)%RestfulLevelPlanner.LevelCount);
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableRainHouses&&Unit(Hash(c.Seed,selectedX,selectedZ,0x240A03u))<c.RainHouseShare)return LiminalEnvironment.RainHouses;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableAquaticPark&&Unit(Hash(c.Seed,selectedX,selectedZ,0x220A03u))<c.AquaticParkShare)return LiminalEnvironment.AquaticPark;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableThermalBaths&&Unit(Hash(c.Seed,selectedX,selectedZ,0x210BA3u))<c.ThermalBathShare)return LiminalEnvironment.ThermalBaths;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableOrangerie&&Unit(Hash(c.Seed,selectedX,selectedZ,0x190A03u))<c.OrangerieShare)return LiminalEnvironment.Orangerie;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableQuietLevels)
            {
                int gx=ContinuousTopology.FloorDivide(selectedX,2),gz=ContinuousTopology.FloorDivide(selectedZ,2);
                uint q=Hash(c.Seed,gx,gz,0x480906u);
                if(Unit(q)<c.QuietLevelShare)
                {
                    int kind=(int)(Hash(c.Seed,gx,gz,0x480907u)%3);
                    if(kind==2)for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dz!=0)
                    {uint other=Hash(c.Seed,gx+dx,gz+dz,0x480906u);if(Unit(other)<c.QuietLevelShare&&Hash(c.Seed,gx+dx,gz+dz,0x480907u)%3==2&&other<q)kind=1;}
                    return (LiminalEnvironment)((int)LiminalEnvironment.Archive906+kind);
                }
            }
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableCivicLevels&&Unit(Hash(c.Seed,selectedX,selectedZ,0x160C01u))<c.CivicLevelShare)
                return Hash(c.Seed,selectedX,selectedZ,0x160C02u)%2==0?LiminalEnvironment.Conservatory:LiminalEnvironment.TransitGallery;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableCity11&&Unit(Hash(c.Seed,ContinuousTopology.FloorDivide(selectedX,2),ContinuousTopology.FloorDivide(selectedZ,2),0x1100A0u))<c.City11Share)return LiminalEnvironment.City11;
            if(c.Procedural==LiminalGeneration.Expanded&&c.EnableSoftPlay&&Unit(Hash(c.Seed,selectedX,selectedZ,0x39000u))<c.SoftPlayShare)return LiminalEnvironment.SoftPlay;
            if(c.Procedural==LiminalGeneration.Expanded&&Unit(Hash(c.Seed,selectedX,selectedZ,0x170Au))<c.AdditionalEnvironmentShare)
                return (LiminalEnvironment)((int)LiminalEnvironment.Office+Hash(c.Seed,selectedX,selectedZ,0x170Bu)%4);
            if((c.Procedural==LiminalGeneration.MixedWithRetail||c.Procedural==LiminalGeneration.Expanded)&&Unit(Hash(c.Seed,selectedX,selectedZ,0xE05u))<c.SupermarketShare)return LiminalEnvironment.Supermarket;
            return Unit(Hash(c.Seed,selectedX,selectedZ,0xE03u))<c.IndoorPlayShare?
                LiminalEnvironment.IndoorPlay:LiminalEnvironment.Poolrooms;
        }
        public static bool UsesAuthored(LayoutConfig c,int x,int z)
        {
            if(PoolroomArchitectureMix.Applies(c,x,z))return !PoolroomArchitectureMix.UsesSolar(c,x,z);
            return c.Procedural==LiminalGeneration.Authored||
                (At(c,x,z)==LiminalEnvironment.Poolrooms&&(x!=0||z!=0)&&
                 Unit(Hash(c.Seed,x,z,0xE04u))<c.AuthoredRoomChance);
        }
        public static bool Find(LayoutConfig c,LiminalEnvironment environment,int x,int z,out int foundX,out int foundZ)
        {
            foundX=x;foundZ=z;
            if(!Enum.IsDefined(typeof(LiminalEnvironment),environment))return false;
            bool coverage=c.DiscoveryCoverage&&c.Procedural==LiminalGeneration.Expanded;
            if(coverage){if(!WalkableExploration.Enabled(c,environment))return false;}
            else
            {
                if(JourneyLevelPlanner.IsLevel(environment)&&!JourneyLevelPlanner.IsMode(c.Procedural)&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableJourneyLevels))return false;
                if(SpecialRegionPlanner.IsLevel(environment)&&!SpecialRegionPlanner.IsMode(c.Procedural)&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableSpecialRegions))return false;
                if(!JourneyLevelPlanner.IsLevel(environment)&&!SpecialRegionPlanner.IsLevel(environment)&&RestfulLevelPlanner.IsLevel(environment)&&!RestfulLevelPlanner.IsMode(c.Procedural)&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableRestfulLevels))return false;
                if(environment==LiminalEnvironment.RainHouses&&c.Procedural!=LiminalGeneration.RainHouses&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableRainHouses||c.RainHouseShare<=0))return false;
                if(environment==LiminalEnvironment.AquaticPark&&c.Procedural!=LiminalGeneration.AquaticPark&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableAquaticPark||c.AquaticParkShare<=0))return false;
                if(environment==LiminalEnvironment.ThermalBaths&&c.Procedural!=LiminalGeneration.ThermalBaths&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableThermalBaths||c.ThermalBathShare<=0))return false;
                if(environment==LiminalEnvironment.Orangerie&&c.Procedural!=LiminalGeneration.Orangerie&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableOrangerie||c.OrangerieShare<=0))return false;
                if(QuietLevelPlanner.IsCivic(environment)&&c.Procedural!=LiminalGeneration.Conservatory&&c.Procedural!=LiminalGeneration.TransitGallery&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableCivicLevels||c.CivicLevelShare<=0))return false;
                if((environment==LiminalEnvironment.Archive906||environment==LiminalEnvironment.Courtyard188||environment==LiminalEnvironment.Coast48)&&c.Procedural!=LiminalGeneration.Archive906&&c.Procedural!=LiminalGeneration.Courtyard188&&c.Procedural!=LiminalGeneration.Coast48&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableQuietLevels||c.QuietLevelShare<=0))return false;
                if(environment==LiminalEnvironment.City11&&c.Procedural!=LiminalGeneration.City11&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableCity11||c.City11Share<=0))return false;
                if(environment==LiminalEnvironment.SoftPlay&&c.Procedural!=LiminalGeneration.SoftPlay&&(c.Procedural!=LiminalGeneration.Expanded||!c.EnableSoftPlay||c.SoftPlayShare<=0))return false;
                LiminalEnvironment single;
                if(Single(c.Procedural,out single))return environment==single;
                if(InteriorPlanner.IsInterior(environment)&&(c.Procedural!=LiminalGeneration.Expanded||c.AdditionalEnvironmentShare==0))return false;
                if(c.Procedural==LiminalGeneration.Expanded&&c.AdditionalEnvironmentShare==1&&!InteriorPlanner.IsInterior(environment)&&environment!=LiminalEnvironment.Poolrooms&&environment!=LiminalEnvironment.City11&&!QuietLevelPlanner.IsLevel(environment))return false;
                if(c.Procedural==LiminalGeneration.Supermarkets&&environment!=LiminalEnvironment.Supermarket)return false;
                if(environment==LiminalEnvironment.Supermarket&&
                    (c.Procedural!=LiminalGeneration.Supermarkets&&c.Procedural!=LiminalGeneration.MixedWithRetail&&c.Procedural!=LiminalGeneration.Expanded||(c.Procedural==LiminalGeneration.MixedWithRetail||c.Procedural==LiminalGeneration.Expanded)&&c.SupermarketShare==0))return false;
                if((c.Procedural==LiminalGeneration.MixedWithRetail||c.Procedural==LiminalGeneration.Expanded)&&environment==LiminalEnvironment.IndoorPlay&&(c.SupermarketShare==1||c.IndoorPlayShare==0))return false;
                if(c.Procedural==LiminalGeneration.Authored)return environment==LiminalEnvironment.Poolrooms;
                if(c.Procedural==LiminalGeneration.Poolrooms&&environment!=LiminalEnvironment.Poolrooms)return false;
                if(c.Procedural==LiminalGeneration.IndoorPlay&&environment!=LiminalEnvironment.IndoorPlay)return false;
                if(c.Procedural==LiminalGeneration.Mixed&&c.IndoorPlayShare==0&&environment==LiminalEnvironment.IndoorPlay)return false;
            }
            if(LargeRegions.Enabled(c))return LargeRegions.Find(c,environment,x,z,out foundX,out foundZ);
            for(int radius=0;radius<=128;radius++)for(int offset=-radius;offset<=radius;offset++)for(int side=0;side<4;side++)
            {
                long xx=(long)x+(side==0||side==2?offset:side==1?radius:-radius);
                long zz=(long)z+(side==1||side==3?offset:side==0?radius:-radius);
                if(xx<int.MinValue||xx>int.MaxValue||zz<int.MinValue||zz>int.MaxValue)continue;
                if(At(c,(int)xx,(int)zz)!=environment)continue;foundX=(int)xx;foundZ=(int)zz;return true;
            }
            return false;
        }
        private static bool Single(LiminalGeneration mode,out LiminalEnvironment environment)
        {
            if(JourneyLevelPlanner.IsMode(mode)){environment=JourneyLevelPlanner.ForMode(mode);return true;}
            if(SpecialRegionPlanner.IsMode(mode)){environment=SpecialRegionPlanner.ForMode(mode);return true;}
            if(RestfulLevelPlanner.IsMode(mode)){environment=RestfulLevelPlanner.ForMode(mode);return true;}
            switch(mode){case LiminalGeneration.RainHouses:environment=LiminalEnvironment.RainHouses;return true;case LiminalGeneration.AquaticPark:environment=LiminalEnvironment.AquaticPark;return true;case LiminalGeneration.ThermalBaths:environment=LiminalEnvironment.ThermalBaths;return true;case LiminalGeneration.Orangerie:environment=LiminalEnvironment.Orangerie;return true;case LiminalGeneration.Conservatory:environment=LiminalEnvironment.Conservatory;return true;case LiminalGeneration.TransitGallery:environment=LiminalEnvironment.TransitGallery;return true;case LiminalGeneration.Archive906:environment=LiminalEnvironment.Archive906;return true;case LiminalGeneration.Courtyard188:environment=LiminalEnvironment.Courtyard188;return true;case LiminalGeneration.Coast48:environment=LiminalEnvironment.Coast48;return true;case LiminalGeneration.City11:environment=LiminalEnvironment.City11;return true;case LiminalGeneration.SoftPlay:environment=LiminalEnvironment.SoftPlay;return true;case LiminalGeneration.Offices:environment=LiminalEnvironment.Office;return true;
                case LiminalGeneration.Hotels:environment=LiminalEnvironment.Hotel;return true;
                case LiminalGeneration.WaitingHalls:environment=LiminalEnvironment.WaitingHall;return true;
                case LiminalGeneration.DiningHalls:environment=LiminalEnvironment.DiningHall;return true;
                default:environment=LiminalEnvironment.Poolrooms;return false;}
        }
        private static float Unit(uint value){return (value>>8)*(1f/16777216f);}
    }
}
