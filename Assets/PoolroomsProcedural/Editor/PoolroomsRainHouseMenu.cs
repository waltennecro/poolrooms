using Poolrooms.Core;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsRainHouseMenu
    {
        [MenuItem("Tools/Poolrooms/Casas de lluvia/Visitar barrio tranquilo (Play)")]
        public static void Visit()
        {
            var world=Object.FindFirstObjectByType<PoolroomsWorld>();
            if(world==null||!world.VisitEnvironment(LiminalEnvironment.RainHouses))Debug.LogWarning("Casas de lluvia requiere Expanded con Enable Rain Houses activo, o el modo RainHouses.");
        }
        [MenuItem("Tools/Poolrooms/Casas de lluvia/Visitar barrio tranquilo (Play)",true)]
        public static bool CanVisit(){return Application.isPlaying;}
    }
}
