using System;

namespace Poolrooms.Core
{
    public static class SceneryMoods
    {
        public static SceneryMood For(LiminalEnvironment environment)
        {
            switch (environment)
            {
                case LiminalEnvironment.Poolrooms:
                case LiminalEnvironment.Coast48:
                case LiminalEnvironment.ThermalBaths:
                case LiminalEnvironment.AquaticPark:
                case LiminalEnvironment.CanalGalleries:
                case LiminalEnvironment.IndoorMarina:
                case LiminalEnvironment.AquariumGalleries:
                case LiminalEnvironment.CloudBaths:
                case LiminalEnvironment.WatermillHalls:
                case LiminalEnvironment.MosaicCistern:
                    return SceneryMood.Water;
                case LiminalEnvironment.Conservatory:
                case LiminalEnvironment.Orangerie:
                case LiminalEnvironment.WinterGarden:
                case LiminalEnvironment.BambooRetreat:
                case LiminalEnvironment.RailwayWintergarden:
                case LiminalEnvironment.LunarGreenhouse:
                case LiminalEnvironment.HangingOrchards:
                    return SceneryMood.Garden;
                case LiminalEnvironment.Supermarket:
                case LiminalEnvironment.DiningHall:
                case LiminalEnvironment.CoveredMarket:
                case LiminalEnvironment.AmberRestaurant:
                case LiminalEnvironment.IndoorBoardwalk:
                case LiminalEnvironment.PaperLanternTown:
                    return SceneryMood.Retail;
                case LiminalEnvironment.Hotel:
                case LiminalEnvironment.Courtyard188:
                case LiminalEnvironment.RainHouses:
                case LiminalEnvironment.LinenLaundries:
                case LiminalEnvironment.CeramicVillage:
                case LiminalEnvironment.DesertCaravanserai:
                    return SceneryMood.Domestic;
                case LiminalEnvironment.IndoorPlay:
                case LiminalEnvironment.SoftPlay:
                    return SceneryMood.Play;
                default:
                    return SceneryMood.Civic;
            }
        }

        public static int Affinity(SceneryRecipe recipe, LiminalEnvironment environment)
        {
            if (recipe == null) return 0;
            SceneryMood mood = For(environment);
            if ((recipe.Moods & mood) == 0) return 0;
            if (recipe.Moods == SceneryMood.Any) return 1;
            switch (environment)
            {
                case LiminalEnvironment.MusicStudios:
                case LiminalEnvironment.RadioSalon:
                case LiminalEnvironment.VelvetTheatre:
                    return recipe.Story == SceneryStory.QuietMusic ? 4 : 2;
                case LiminalEnvironment.Archive906:
                case LiminalEnvironment.SunsetLibrary:
                case LiminalEnvironment.SunkenLibrary:
                case LiminalEnvironment.RainyBookshops:
                case LiminalEnvironment.MapmakersArchive:
                    return recipe.Story == SceneryStory.ReadingCircle ? 4 : 2;
                case LiminalEnvironment.Orangerie:
                case LiminalEnvironment.HangingOrchards:
                case LiminalEnvironment.LunarGreenhouse:
                    return recipe.Story == SceneryStory.PottingLesson ||
                        recipe.Story == SceneryStory.PlantExchange ? 4 : 2;
                case LiminalEnvironment.DiningHall:
                case LiminalEnvironment.AmberRestaurant:
                case LiminalEnvironment.TeaPavilions:
                    return recipe.Story == SceneryStory.TeaForTwo ||
                        recipe.Story == SceneryStory.SharedBreakfast ? 4 : 2;
                case LiminalEnvironment.CloudTerminal:
                case LiminalEnvironment.TransitGallery:
                case LiminalEnvironment.AeronauticalGallery:
                    return recipe.Story == SceneryStory.WaitingFriends ||
                        recipe.Story == SceneryStory.PackedLunch ? 4 : 2;
                default:
                    return 2;
            }
        }

        public static bool Accept(SceneryRecipe recipe, Room room, uint rank)
        {
            int affinity = Affinity(recipe, room.Environment);
            if (affinity == 0) return false;
            // Generic arrangements remain available, but the first search pass
            // prefers local activities instead of a cafe in every single biome.
            return affinity >= 2 || rank % 3 == 0;
        }

        public static string Place(SceneryMood mood)
        {
            switch (mood)
            {
                case SceneryMood.Water: return "junto al agua";
                case SceneryMood.Garden: return "bajo las hojas";
                case SceneryMood.Retail: return "en la galería";
                case SceneryMood.Domestic: return "en casa";
                case SceneryMood.Play: return "después de jugar";
                default: return "en el camino";
            }
        }

        public static string Time(uint hash)
        {
            switch (hash % 6)
            {
                case 0: return "esta tarde";
                case 1: return "después del paseo";
                case 2: return "antes de la próxima apertura";
                case 3: return "a la hora del té";
                case 4: return "cuando termine la lluvia";
                default: return "al volver por aquí";
            }
        }
    }
}
