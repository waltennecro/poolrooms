using System;

namespace Poolrooms.Core
{
    public static class SceneryStories
    {
        public static string Title(SceneryRecipe recipe, SceneryMood mood)
        {
            return recipe.Name + " · " + SceneryMoods.Place(mood);
        }

        public static string Note(SceneryRecipe recipe, int seed, int x, int z)
        {
            uint hash = LiminalRegions.Hash(seed, x, z, 0x5CE700u + (uint)recipe.Id);
            string opening;
            switch (recipe.Story)
            {
                case SceneryStory.TeaForTwo:
                    opening = "Hay dos tazas preparadas. Una nota doblada pide que dejemos sitio para quien llegue después.";
                    break;
                case SceneryStory.PlantExchange:
                    opening = "Las macetas están juntas para el intercambio. Cada visitante puede cuidar una planta y dejarla para el siguiente.";
                    break;
                case SceneryStory.ReadingCircle:
                    opening = "Las sillas siguen orientadas hacia la mesa. Alguien dejó señalado dónde continuará la lectura compartida.";
                    break;
                case SceneryStory.AfternoonMarket:
                    opening = "La mesa está preparada para abrir. El cartel invita a compartir lo que sobre al terminar la tarde.";
                    break;
                case SceneryStory.PackedLunch:
                    opening = "Dejaron agua y un pequeño lugar para el almuerzo. En la cesta hay una invitación para seguir juntos el paseo.";
                    break;
                case SceneryStory.BeforeOpening:
                    opening = "Todo está ordenado para la próxima apertura. Las sillas esperan a sus primeras visitas y queda un sitio libre.";
                    break;
                case SceneryStory.RainShelter:
                    opening = "El perchero y los asientos forman un refugio para esperar a que mejore el tiempo. Nadie tiene prisa aquí.";
                    break;
                case SceneryStory.PhotographyWalk:
                    opening = "La cámara apunta hacia la galería. Quedó anotado un paseo de fotografía para descubrir los rincones con mejor luz.";
                    break;
                case SceneryStory.QuietMusic:
                    opening = "Prepararon un pequeño rincón de escucha. Puedes usar tu música; guardaron un asiento para la siguiente visita.";
                    break;
                case SceneryStory.SharedBreakfast:
                    opening = "Los platos están separados y las sillas cerca. En la nota del desayuno alguien escribió: hay suficiente para compartir.";
                    break;
                case SceneryStory.WaitingFriends:
                    opening = "Hay varios sitios preparados para descansar. Una cesta guarda la invitación: nos vemos aquí, junto al camino.";
                    break;
                case SceneryStory.PottingLesson:
                    opening = "La mesa está lista para cuidar plantas. Las macetas esperan el próximo trasplante y una mano que ayude con el agua.";
                    break;
                default:
                    opening = "Este rincón espera a la próxima visita. Puedes descansar y dejarlo preparado para alguien más.";
                    break;
            }
            return opening + "\n\n" + Signature(hash) + ": volveremos " + SceneryMoods.Time(hash >> 6) + ".";
        }

        private static string Signature(uint hash)
        {
            switch (hash % 8)
            {
                case 0: return "El grupo de la mesa larga";
                case 1: return "Quienes cuidan el jardín";
                case 2: return "El círculo de lectura";
                case 3: return "Los amigos del paseo";
                case 4: return "El turno de la tarde";
                case 5: return "La próxima reunión";
                case 6: return "Quienes pasaron esta mañana";
                default: return "Unos viajeros de paso";
            }
        }

        public static bool CanBind(SceneryRole role)
        {
            // Do not replace sitting, planting, music or lamp use with a note.
            // Only reading props and previously passive furniture get a story.
            return role == SceneryRole.Story || role == SceneryRole.Table;
        }

        public static bool TryRead(int recipeId, LiminalEnvironment environment,
            int seed, int x, int z, out string title, out string note)
        {
            title = note = "";
            if (!SceneryRecipeCatalog.TryGet(recipeId, out var recipe)) return false;
            title = Title(recipe, SceneryMoods.For(environment));
            note = Note(recipe, seed, x, z);
            return true;
        }
    }
}
