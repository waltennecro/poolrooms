using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsLiminalMenu
    {
        [MenuItem("Tools/Poolrooms/Áreas infantiles v39/Integrar al mundo infinito")]
        public static void EnableSoftPlay()
        {
            if(Application.isPlaying){Debug.LogWarning("Activa el bioma fuera de Play para guardar la configuración.");return;}
            var world=FindWorld();if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con su perfil.");return;}
            Undo.RecordObject(world.profile,"Integrar áreas infantiles");world.profile.enableSoftPlay=true;
            if(world.profile.softPlayShare<=0)world.profile.softPlayShare=.18f;
            EditorUtility.SetDirty(world.profile);Apply(LiminalGeneration.Expanded);AssetDatabase.SaveAssets();
        }
        [MenuItem("Tools/Poolrooms/Áreas infantiles v39/Visitar áreas multinivel (Play)")]
        public static void VisitSoftPlay(){Visit(LiminalEnvironment.SoftPlay);}
        [MenuItem("Tools/Poolrooms/Áreas infantiles v39/Visitar áreas multinivel (Play)",true)]
        public static bool CanVisitSoftPlay(){return Application.isPlaying;}
        [MenuItem("Tools/Poolrooms/Generador liminal/Activar Poolrooms y juegos interiores v13")]
        public static void Mixed(){Apply(LiminalGeneration.Mixed);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Activar Poolrooms, juegos y supermercados v14")]
        public static void AllEnvironments(){Apply(LiminalGeneration.MixedWithRetail);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Activar todos los ambientes del archivo v17")]
        public static void Expanded(){Apply(LiminalGeneration.Expanded);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo oficinas vacías v17")]
        public static void Offices(){Apply(LiminalGeneration.Offices);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo hoteles v17")]
        public static void Hotels(){Apply(LiminalGeneration.Hotels);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo salas de espera v17")]
        public static void Waiting(){Apply(LiminalGeneration.WaitingHalls);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo comedores v17")]
        public static void Dining(){Apply(LiminalGeneration.DiningHalls);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar oficinas (Play)")]
        public static void VisitOffice(){Visit(LiminalEnvironment.Office);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar hotel (Play)")]
        public static void VisitHotel(){Visit(LiminalEnvironment.Hotel);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar sala de espera (Play)")]
        public static void VisitWaiting(){Visit(LiminalEnvironment.WaitingHall);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar comedor (Play)")]
        public static void VisitDining(){Visit(LiminalEnvironment.DiningHall);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo supermercados liminales v14")]
        public static void Supermarkets(){Apply(LiminalGeneration.Supermarkets);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo Poolrooms procedurales v13")]
        public static void Pools(){Apply(LiminalGeneration.Poolrooms);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Solo juegos interiores procedurales v13")]
        public static void Playrooms(){Apply(LiminalGeneration.IndoorPlay);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Usar únicamente recetas existentes")]
        public static void Authored(){Apply(LiminalGeneration.Authored);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar juegos interiores (Play)")]
        public static void VisitPlay(){Visit(LiminalEnvironment.IndoorPlay);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar supermercado (Play)")]
        public static void VisitSupermarket(){Visit(LiminalEnvironment.Supermarket);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar Poolrooms (Play)")]
        public static void VisitPools(){Visit(LiminalEnvironment.Poolrooms);}
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar juegos interiores (Play)",true)]
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar supermercado (Play)",true)]
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar Poolrooms (Play)",true)]
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar oficinas (Play)",true)]
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar hotel (Play)",true)]
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar sala de espera (Play)",true)]
        [MenuItem("Tools/Poolrooms/Generador liminal/Visitar comedor (Play)",true)]
        public static bool CanVisit(){return Application.isPlaying;}
        private static void Visit(LiminalEnvironment environment)
        {
            var world=FindWorld();
            if(world==null||!world.VisitEnvironment(environment))Debug.LogWarning("No se encontró ese ambiente cercano con el perfil actual.");
        }
        private static PoolroomsWorld FindWorld()
        {
            var selected=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(selected!=null)return selected;
            var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);return worlds.Length==1?worlds[0]:null;
        }
        [MenuItem("Tools/Poolrooms/Generador liminal/Conservar plantas v12")]
        public static void CrossV12(){var world=FindWorld();if(world!=null&&world.profile!=null)Apply(world.profile.liminalGeneration,ProceduralGrammar.CrossV12);}
        private static void Apply(LiminalGeneration mode,ProceduralGrammar grammar=ProceduralGrammar.Varied)
        {
            if(Application.isPlaying){Debug.LogWarning("Configura el generador fuera de Play para guardar su perfil.");return;}
            var world=FindWorld();
            if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con un perfil asignado.");return;}
            if(grammar==ProceduralGrammar.Varied&&world.profile.proceduralGrammar==ProceduralGrammar.SolarAtlas&&mode!=LiminalGeneration.Authored)grammar=ProceduralGrammar.SolarAtlas;
            if(world.profile.liminalGeneration==mode&&world.profile.proceduralGrammar==grammar&&world.profile.continuousWorld)
            {Debug.Log("El generador ya está configurado. Guarda la escena y pulsa Play.",world);return;}
            string source=AssetDatabase.GetAssetPath(world.profile);
            string folder=string.IsNullOrEmpty(source)?"Assets":System.IO.Path.GetDirectoryName(source).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+(mode==LiminalGeneration.Expanded?" - Ambientes v17":" - Liminal");
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/LiminalGeneration.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Perfil generador liminal");Undo.RecordObject(world,"Activar generador liminal");
            world.profile=copy;copy.liminalGeneration=mode;copy.proceduralGrammar=grammar;copy.continuousWorld=true;
            if(mode!=LiminalGeneration.Authored)
            {
                copy.zoneMode=ZoneMode.Random;copy.zoneFamilyFilter=-1;
                copy.composition=SpatialComposition.Exploration;
                if(copy.lighting==LightingDistribution.ZoneDefaults||copy.lighting==LightingDistribution.Seeded)
                {copy.compositionPreviousLighting=copy.lighting;copy.compositionRemembersLighting=true;copy.lighting=LightingDistribution.Composed;}
            }
            copy.Snapshot();world.generateOnStart=true;world.streaming=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Selection.activeGameObject=world.gameObject;
            Debug.Log("Generador liminal "+mode+" preparado. Guarda y pulsa Play. El perfil anterior se conserva; no se requieren plugins.",world);
        }
    }
}
