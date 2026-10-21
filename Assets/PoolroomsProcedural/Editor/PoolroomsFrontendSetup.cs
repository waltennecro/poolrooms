using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Poolrooms.Editor
{
    public static class PoolroomsFrontendSetup
    {
        [MenuItem("Tools/Poolrooms/Menú principal/Crear escena de inicio independiente")]
        public static void CreateEntryScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var source=SceneManager.GetActiveScene();var roots=source.GetRootGameObjects();
            bool found=false;
            foreach(var root in roots)if(root.GetComponentInChildren<PoolroomsWorld>(true)!=null)found=true;
            if(!found)throw new InvalidOperationException("Abre primero la escena del recorrido que quieres conectar al menú.");
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/PoolroomsFrontend");
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var wrapper=new GameObject("Journey template");
            GameObject prefab;
            PoolroomsFrontendEnvironment environment;
            try
            {
                foreach(var root in roots)root.transform.SetParent(wrapper.transform,true);
                environment=PoolroomsFrontendEnvironment.Capture(wrapper.transform);
                wrapper.SetActive(false);
                prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,folder+"/JourneyTemplate.prefab");
                if(prefab==null)throw new InvalidOperationException("No se pudo guardar la plantilla del recorrido.");
            }
            finally
            {
                for(int i=0;i<roots.Length;i++){roots[i].transform.SetParent(null,true);roots[i].transform.SetSiblingIndex(i);}
                UnityEngine.Object.DestroyImmediate(wrapper);
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var node=new GameObject("Poolrooms Frontend");var frontend=node.AddComponent<PoolroomsFrontend>();
            frontend.journeyTemplate=prefab;frontend.sceneEnvironment=environment;
            string path=folder+"/PoolroomsMainMenu.unity";
            EditorSceneManager.SaveScene(scene,path);
            var build=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(path,true)};
            foreach(var item in EditorBuildSettings.scenes)if(item.path!=path)build.Add(item);
            EditorBuildSettings.scenes=build.ToArray();AssetDatabase.SaveAssets();Selection.activeGameObject=node;
            Debug.Log("Menú independiente creado en "+path+". Es la primera escena de compilación. La plantilla permanece inactiva hasta elegir comenzar.");
        }
    }

    public sealed class PoolroomsFrontendArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Runtime/Resources/FrontendArt/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;
            importer.npotScale=TextureImporterNPOTScale.None;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
