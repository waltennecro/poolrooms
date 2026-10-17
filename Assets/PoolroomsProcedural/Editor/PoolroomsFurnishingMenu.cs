using System.IO;
using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsFurnishingMenu
    {
        [MenuItem("Tools/Poolrooms/Mobiliario v25/Activar variedad por ambiente")]
        public static void Enable()
        {
            if(Application.isPlaying){Debug.LogWarning("Activa el mobiliario fuera de Play y guarda la escena.");return;}
            var selected=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
            var world=selected!=null?selected:worlds.Length==1?worlds[0]:null;
            if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con su perfil.");return;}
            string path=AssetDatabase.GetAssetPath(world.profile),folder=string.IsNullOrEmpty(path)?"Assets":Path.GetDirectoryName(path).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Mobiliario v25";
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/FurnishingV25.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Mobiliario v25");Undo.RecordObject(world,"Activar mobiliario v25");
            // Preserve the existing seed, terrain, light, water, camera and quality choices.
            copy.autonomousGeneration=true;copy.independentBiomeMaterials=true;copy.biomeMaterials=PoolroomsBiomeMaterialSetup.Build();
            if(copy.liminalGeneration==LiminalGeneration.Authored)
            {copy.liminalGeneration=LiminalGeneration.Expanded;copy.continuousWorld=true;copy.proceduralGrammar=ProceduralGrammar.SolarAtlas;}
            if(copy.enableGiantMalls){copy.enhancedMallArchitecture=true;copy.refinedMallArchitecture=true;}
            world.profile=copy;world.generateOnStart=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Mobiliario v25 activo. Nuevos conjuntos por ambiente; materiales propios; artículos sobre soportes y patios despejados. Guarda la escena. El perfil anterior se conserva. Los modos de salas de autor mantienen sus muebles originales.",world);
        }
    }
}
