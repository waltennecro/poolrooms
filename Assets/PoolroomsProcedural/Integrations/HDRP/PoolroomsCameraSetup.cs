#if UNITY_EDITOR && POOLROOMS_HDRP_17
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    public static class PoolroomsCameraSetup
    {
        [MenuItem("Tools/Poolrooms/Cámara/Instalar efectos y menú de pausa")]
        public static void Install()
        {
            if(Application.isPlaying){Debug.LogWarning("Sal de Play para guardar la cámara y su menú.");return;}
            var world=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(world==null)
            {
                var worlds=UnityEngine.Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
                if(worlds.Length==1)world=worlds[0];
            }
            Camera camera=world!=null&&world.viewer!=null?world.viewer.GetComponentInChildren<Camera>():null;
            if(world==null||camera==null){Debug.LogWarning("Selecciona tu Poolrooms World con Explorer y cámara asignados.");return;}
            InstallForWorld(world,camera);EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Cámara y pausa instaladas. Guarda la escena. Esc abre la pausa y Ajustes > Gráficos permite elegir la visualización.",world);
        }
        public static void InstallForWorld(PoolroomsWorld world,Camera camera)
        {
            var explorer=world.viewer!=null?world.viewer.GetComponent<PoolroomsExplorer>():null;
            if(explorer==null){Debug.LogWarning("Se necesita el PoolroomsExplorer existente para detectar pausa y contacto con agua.");return;}
            var effects=camera.GetComponent<PoolroomsCameraEffects>();
            if(effects==null)effects=Undo.AddComponent<PoolroomsCameraEffects>(camera.gameObject);
            Undo.RecordObject(effects,"Efectos de cámara Poolrooms");effects.explorer=explorer;effects.world=world;
            var pause=explorer.GetComponent<PoolroomsPauseMenu>();
            if(pause==null)pause=Undo.AddComponent<PoolroomsPauseMenu>(explorer.gameObject);
            Undo.RecordObject(pause,"Pausa Poolrooms");pause.cameraEffects=effects;
            if(camera.GetComponent<PoolroomsCameraPresentation>()==null)Undo.AddComponent<PoolroomsCameraPresentation>(camera.gameObject);
            EditorUtility.SetDirty(effects);EditorUtility.SetDirty(pause);
            RegisterOptics();
        }
        private static void RegisterOptics()
        {
            // HDRP 17.3 makes its orders settings type internal. Edit the existing serialized
            // settings through public Editor APIs, adding a single entry and preserving all others.
            var global=GraphicsSettings.GetSettingsForRenderPipeline<HDRenderPipeline>();
            if(global==null){ManualRegistration();return;}
            var serialized=new SerializedObject(global);var iterator=serialized.GetIterator();
            SerializedProperty list=null;
            while(iterator.Next(true))
                if(iterator.propertyType==SerializedPropertyType.ManagedReference&&
                    iterator.managedReferenceFullTypename.EndsWith(".CustomPostProcessOrdersSettings",StringComparison.Ordinal))
                {list=iterator.FindPropertyRelative("m_AfterPostProcessCustomPostProcesses.m_CustomPostProcessTypesAsString");break;}
            if(list==null||!list.isArray){ManualRegistration();return;}
            string type=typeof(PoolroomsCameraPostProcess).AssemblyQualifiedName;
            for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).stringValue==type)return;
            Undo.RecordObject(global,"Registrar óptica Poolrooms");
            int index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).stringValue=type;
            serialized.ApplyModifiedProperties();EditorUtility.SetDirty(global);AssetDatabase.SaveAssets();
            // Rebuild HDRP's cached type list after adding the serialized entry.
            EditorApplication.delayCall+=EditorUtility.RequestScriptReload;
            Debug.Log("Óptica añadida a After Post Process. Unity recargará los scripts; se conservan los demás efectos del proyecto.");
        }
        private static void ManualRegistration()
        {
            Debug.LogWarning("Registra PoolroomsCameraPostProcess en Project Settings > Graphics > Pipeline Specific Settings > HDRP > Custom Post Process Orders > After Post Process. "+
                "La estructura de ajustes no permitió el registro automático. Los efectos nativos de color y el menú siguen disponibles.");
        }
    }
}
#endif
