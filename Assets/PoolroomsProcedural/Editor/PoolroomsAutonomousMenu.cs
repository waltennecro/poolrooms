using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public static class PoolroomsAutonomousMenu
    {
        [MenuItem("Tools/Poolrooms/Generación autónoma v22/Activar composición por semilla")]
        public static void Enable()
        {
            if(Application.isPlaying){Debug.LogWarning("Activa la generación fuera de Play para guardar el perfil.");return;}
            var selected=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            var worlds=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
            var world=selected!=null?selected:worlds.Length==1?worlds[0]:null;
            if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con su perfil.");return;}
            string path=AssetDatabase.GetAssetPath(world.profile);
            string folder=string.IsNullOrEmpty(path)?"Assets":System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Generación autónoma v22";
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/AutonomousV22.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Generación autónoma v22");Undo.RecordObject(world,"Generación autónoma v22");
            copy.autonomousGeneration=true;copy.continuousWorld=true;copy.proceduralGrammar=ProceduralGrammar.SolarAtlas;
            if(copy.liminalGeneration==LiminalGeneration.Authored||copy.liminalGeneration==LiminalGeneration.Mixed||copy.liminalGeneration==LiminalGeneration.MixedWithRetail)
                copy.liminalGeneration=LiminalGeneration.Expanded;
            copy.independentBiomeMaterials=true;copy.biomeMaterials=PoolroomsBiomeMaterialSetup.Build();
            copy.enhancedMallArchitecture=true;copy.refinedMallArchitecture=true;
            if(copy.liminalGeneration==LiminalGeneration.Expanded||copy.liminalGeneration==LiminalGeneration.Supermarkets||copy.liminalGeneration==LiminalGeneration.MixedWithRetail)
            {copy.enableGiantMalls=true;copy.mallDistricts=true;if(copy.mallEntranceChance<=0)copy.mallEntranceChance=.2f;}
            world.profile=copy;world.generateOnStart=true;world.streaming=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("v22 activo: programas autónomos por semilla, muebles con rutas protegidas y tiendas con stock colocado sobre sus soportes. Guarda la escena. El perfil anterior se conserva.",world);
        }
    }
}
