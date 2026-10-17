using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsMallMenu
    {
        [MenuItem("Tools/Poolrooms/Centro comercial completo v20/Activar centro comercial y materiales propios")]
        public static void EnableComplete(){ConfigureComplete(false);}
        [MenuItem("Tools/Poolrooms/Centro comercial v21/Mejorar forma, mobiliario y detalles")]
        public static void EnableRefined(){ConfigureComplete(true);}
        [MenuItem("Tools/Poolrooms/Centro comercial v21/Visitar centro comercial (Play)")]
        public static void VisitRefined(){Visit();}
        [MenuItem("Tools/Poolrooms/Centro comercial v21/Visitar centro comercial (Play)",true)]
        public static bool CanVisitRefined(){return Application.isPlaying;}
        private static void ConfigureComplete(bool refined)
        {
            string version=refined?"v21":"v20";
            if(Application.isPlaying){Debug.LogWarning("Activa el centro comercial fuera de Play para guardar la escena.");return;}
            var world=Find();if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con su perfil.");return;}
            var library=PoolroomsBiomeMaterialSetup.Build();
            string path=AssetDatabase.GetAssetPath(world.profile),folder=string.IsNullOrEmpty(path)?"Assets":System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Centro comercial "+version;
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/MallComplete"+version.ToUpperInvariant()+".asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Centro comercial completo");Undo.RecordObject(world,"Centro comercial completo");
            copy.enableGiantMalls=true;copy.enhancedMallArchitecture=true;copy.refinedMallArchitecture=refined;copy.mallDistricts=true;copy.mallEntranceChance=1;
            copy.independentBiomeMaterials=true;copy.biomeMaterials=library;copy.mallDetailRadius=1;copy.continuousWorld=true;
            if(copy.liminalGeneration!=LiminalGeneration.Supermarkets&&copy.liminalGeneration!=LiminalGeneration.MixedWithRetail&&copy.liminalGeneration!=LiminalGeneration.Expanded)
                copy.liminalGeneration=LiminalGeneration.MixedWithRetail;
            if(copy.supermarketShare<=0)copy.supermarketShare=.3f;
            if(copy.liminalGeneration==LiminalGeneration.Expanded&&copy.additionalEnvironmentShare>=1)copy.additionalEnvironmentShare=.65f;
            world.profile=copy;world.generateOnStart=true;world.streaming=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log(version+" activo: las regiones comerciales son vestíbulos de centros completos; E en la entrada. Materiales PBR independientes. Guarda la escena. El perfil anterior se conserva.",world);
        }
        [MenuItem("Tools/Poolrooms/Centro comercial completo v20/Visitar centro comercial (Play)")]
        public static void VisitComplete(){Visit();}
        [MenuItem("Tools/Poolrooms/Centros comerciales v16/Activar entradas a centros de 2 a 6 pisos")]
        public static void Enable()
        {
            if(Application.isPlaying){Debug.LogWarning("Activa centros comerciales fuera de Play para guardar el perfil.");return;}
            var world=Find();if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con su perfil.");return;}
            if(world.profile.enableGiantMalls){Debug.Log("Los centros comerciales ya están activos.",world);return;}
            string path=AssetDatabase.GetAssetPath(world.profile);string folder=string.IsNullOrEmpty(path)?"Assets":System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Centros 2010 v16";
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/MallGeneration.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Perfil centros comerciales");Undo.RecordObject(world,"Activar centros comerciales");
            copy.enableGiantMalls=true;copy.mallEntranceChance=.2f;copy.mallDetailRadius=1;
            copy.continuousWorld=true;
            if(copy.liminalGeneration!=LiminalGeneration.Supermarkets&&copy.liminalGeneration!=LiminalGeneration.MixedWithRetail&&copy.liminalGeneration!=LiminalGeneration.Expanded)
            {copy.liminalGeneration=LiminalGeneration.MixedWithRetail;if(copy.proceduralGrammar!=ProceduralGrammar.SolarAtlas)copy.proceduralGrammar=ProceduralGrammar.Varied;copy.continuousWorld=true;}
            world.profile=copy;world.generateOnStart=true;world.streaming=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Centros comerciales nuevos activados. Se conserva el perfil anterior y los supermercados pequeños. Guarda, pulsa Play y usa Visitar centro comercial.",world);
        }
        [MenuItem("Tools/Poolrooms/Centros comerciales v16/Visitar centro comercial (Play)")]
        public static void Visit()
        {var world=Find();if(world==null||world.Malls==null||!world.Malls.VisitNearest())Debug.LogWarning("Activa centros comerciales antes de Play; se necesita una región supermercado con entradas habilitadas.");}
        [MenuItem("Tools/Poolrooms/Centros comerciales v16/Visitar centro comercial (Play)",true)]
        public static bool CanVisit(){return Application.isPlaying;}
        private static PoolroomsWorld Find()
        {
            var selected=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;if(selected!=null)return selected;
            var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);return worlds.Length==1?worlds[0]:null;
        }
    }
}
