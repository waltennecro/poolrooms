using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsSolarMenu
    {
        [MenuItem("Tools/Poolrooms/Atlas solar v19/Activar arquitectura y regiones solares")]
        public static void Enable()
        {
            if(Application.isPlaying){Debug.LogWarning("Activa el atlas fuera de Play para guardar el perfil.");return;}
            PoolroomsWorld world=World();if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con un perfil.");return;}
            if(PoolroomArchitectureMix.Enabled(world.profile.Snapshot())||(world.profile.proceduralGrammar==ProceduralGrammar.SolarAtlas&&world.profile.continuousWorld&&world.profile.liminalGeneration!=LiminalGeneration.Authored))
            {Debug.Log("La arquitectura solar ya participa en el perfil normal. Guarda y pulsa Play; frecuencia y tamaño de regiones se ajustan en el perfil.",world);return;}
            string source=AssetDatabase.GetAssetPath(world.profile),folder=string.IsNullOrEmpty(source)?"Assets":System.IO.Path.GetDirectoryName(source).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Atlas solar v19";
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/PoolroomsSolarAtlas.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Perfil solar v19");Undo.RecordObject(world,"Activar atlas solar");world.profile=copy;
            copy.expandedVariety=true;copy.proceduralGrammar=ProceduralGrammar.SolarAtlas;copy.continuousWorld=true;
            if(copy.liminalGeneration==LiminalGeneration.Authored)copy.liminalGeneration=LiminalGeneration.Poolrooms;
            copy.composition=SpatialComposition.Exploration;copy.lighting=LightingDistribution.ZoneDefaults;
            copy.Snapshot();world.generateOnStart=true;world.streaming=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);Selection.activeGameObject=world.gameObject;
            Debug.Log("Atlas de 1152 diseños estructurales activado. Conserva las otras ambientaciones, presupuestos y el perfil anterior. La linterna no cambia. Guarda y pulsa Play.",world);
        }
        [MenuItem("Tools/Poolrooms/Atlas solar v19/Visitar región soleada (Play)")]
        public static void VisitSun(){Visit(true);}
        [MenuItem("Tools/Poolrooms/Atlas solar v19/Visitar región cerrada (Play)")]
        public static void VisitClosed(){Visit(false);}
        [MenuItem("Tools/Poolrooms/Atlas solar v19/Visitar región soleada (Play)",true)]
        [MenuItem("Tools/Poolrooms/Atlas solar v19/Visitar región cerrada (Play)",true)]
        public static bool CanVisit(){return Application.isPlaying;}
        private static void Visit(bool sun)
        {
            var world=World();if(world==null||!world.VisitPoolClimate(sun))Debug.LogWarning("No se encontró esa región cercana; comprueba que el atlas esté activo.");
        }
        private static PoolroomsWorld World()
        {
            var selected=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;if(selected!=null)return selected;
            var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);return worlds.Length==1?worlds[0]:null;
        }
    }
}
