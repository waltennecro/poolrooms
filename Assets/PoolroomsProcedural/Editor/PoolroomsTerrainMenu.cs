using System.IO;
using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Poolrooms.Editor
{
    public sealed class PoolroomsTerrainTextureImporter : AssetPostprocessor
    {
        public override uint GetVersion(){return 34;}
        private void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/PoolroomsTerrainTextures/"))return;
            var p=(TextureImporter)assetImporter;bool normal=assetPath.EndsWith("_Normal.png"),mask=assetPath.EndsWith("_Mask.png");
            p.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;p.sRGBTexture=!normal&&!mask;
            p.mipmapEnabled=true;p.wrapMode=TextureWrapMode.Repeat;p.anisoLevel=4;p.maxTextureSize=512;p.textureCompression=TextureImporterCompression.Compressed;
        }
    }
    public static class PoolroomsTerrainMenu
    {
        [MenuItem("Tools/Poolrooms/Nivel 94 v24/Activar colinas y portales")]
        public static void Enable()
        {
            if(Application.isPlaying){Debug.LogWarning("Activa Nivel 94 fuera de Play y guarda la escena.");return;}
            var world=Find();if(world==null||world.profile==null){Debug.LogWarning("Selecciona Poolrooms World con su perfil.");return;}
            BuildMaterials();
            string path=AssetDatabase.GetAssetPath(world.profile),folder=string.IsNullOrEmpty(path)?"Assets":Path.GetDirectoryName(path).Replace('\\','/');
            var copy=Object.Instantiate(world.profile);copy.name=world.profile.name+" - Colinas Nivel 94 v24";
            AssetDatabase.CreateAsset(copy,AssetDatabase.GenerateUniqueAssetPath(folder+"/TerrainRegionV24.asset"));
            Undo.RegisterCreatedObjectUndo(copy,"Perfil exterior Nivel 94");Undo.RecordObject(world,"Activar colinas exteriores");
            copy.enableTerrainRegions=true;copy.terrainEntranceChance=.08f;copy.terrainViewRadius=2;
            world.profile=copy;world.generateOnStart=true;world.ClearPreview();
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Nivel 94 activado: terrenos por semilla y casas accesibles. Exploración unificada conecta las colinas al mapa; el modo antiguo conserva portales. Guarda la escena. Perfil anterior conservado. Visitar colinas (Play) permite probarlo directamente.",world);
        }
        [MenuItem("Tools/Poolrooms/Nivel 94 v24/Visitar colinas (Play)")]
        public static void Visit(){var w=Find();if(w==null||w.Terrain==null||!w.Terrain.VisitNearest())Debug.LogWarning("Activa Exploración unificada (o Nivel 94 en el modo antiguo) y asigna el Explorer.");}
        [MenuItem("Tools/Poolrooms/Nivel 94 v24/Visitar colinas (Play)",true)]
        public static bool CanVisit(){return Application.isPlaying;}
        [MenuItem("Tools/Poolrooms/Nivel 94 v24/Volver al interior (Play)")]
        public static void Exit(){var w=Find();if(w!=null&&w.Terrain!=null){if(!w.Terrain.ReturnToConnectedGateway())w.Terrain.Exit();}}
        private static PoolroomsWorld Find()
        {var w=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;if(w!=null)return w;var list=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);return list.Length==1?list[0]:null;}
        [MenuItem("Tools/Poolrooms/Nivel 94 v24/Preparar materiales exteriores")]
        public static void PrepareMaterials(){BuildMaterials();}
        private static void BuildMaterials()
        {
            const string folder="Assets/PoolroomsProcedural/Resources/TerrainMaterials";
            const string path="Assets/PoolroomsProcedural/Resources/PoolroomsTerrainMaterials.asset";
            if(AssetDatabase.LoadAssetAtPath<PoolroomsTerrainMaterials>(path)!=null)return;
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=new GameObject("Temporary terrain material builder");var owner=root.AddComponent<PoolroomsChunk>();
            try
            {
                var set=new TerrainMaterialSet(owner);var library=ScriptableObject.CreateInstance<PoolroomsTerrainMaterials>();
                library.appearanceVersion=34;library.roof=set.Roof[(int)Surface.Accent];library.houseInterior=set.Interior;
                library.ground=set.Ground;library.interior=set.Houses[0];library.pastelPaint=new Material[6];
                for(int i=0;i<6;i++)library.pastelPaint[i]=set.Houses[i][(int)Surface.Plaster];library.glass=set.Glass[0];library.portal=set.Portal[0];
                int index=0;foreach(var m in owner.ownedMaterials){AssetDatabase.CreateAsset(m,folder+"/Terrain_"+(index++)+".mat");EditorUtility.SetDirty(m);}
                owner.ownedMaterials.Clear();AssetDatabase.CreateAsset(library,path);AssetDatabase.SaveAssets();
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
