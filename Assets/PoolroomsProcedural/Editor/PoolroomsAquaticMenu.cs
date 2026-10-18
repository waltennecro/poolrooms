using Poolrooms.Core;
using UnityEditor;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsAquaticMenu
    {
        [MenuItem("Tools/Poolrooms/Parque acuático/Visitar atardecer interior (Play)")]
        public static void Visit()
        {
            var world=Object.FindFirstObjectByType<PoolroomsWorld>();
            if(world==null||!world.VisitEnvironment(LiminalEnvironment.AquaticPark))
                Debug.LogWarning("El parque requiere Expanded con Enable Aquatic Park activo, o el modo AquaticPark. Los perfiles nuevos ya lo incluyen.");
        }
        [MenuItem("Tools/Poolrooms/Parque acuático/Visitar atardecer interior (Play)",true)]
        public static bool CanVisit(){return Application.isPlaying;}
    }
}
