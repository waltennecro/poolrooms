#if UNITY_EDITOR && POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    public static class PoolroomsWaterSetup
    {
        [MenuItem("Tools/Poolrooms/Agua/Actualizar agua y ondas v9")]
        public static void Install()
        {
            if(Application.isPlaying){Debug.LogWarning("Sal de Play para guardar la actualización del agua.");return;}
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if(asset==null){Debug.LogWarning("Se requiere un HDRP Asset activo.");return;}
            var world=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(world==null)
            {
                var all=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);if(all.Length==1)world=all[0];
            }
            if(world==null){Debug.LogWarning("Selecciona tu Poolrooms World.");return;}
            var adapter=world.GetComponent<PoolroomsHDRP>();
            if(adapter==null)adapter=Undo.AddComponent<PoolroomsHDRP>(world.gameObject);
            Undo.RecordObject(adapter,"Mejorar agua Poolrooms");
            adapter.nativeWater=true;adapter.interactiveWater=true;
            if(world.GetComponent<PoolroomsWaterInteractions>()==null)Undo.AddComponent<PoolroomsWaterInteractions>(world.gameObject);
            var settings=asset.currentPlatformRenderPipelineSettings;
            if(!settings.supportWater||!settings.supportWaterDecals||(int)settings.waterDecalAtlasSize<1024||settings.maximumWaterDecalCount<1)
            {
                Undo.RecordObject(asset,"Activar agua interactiva HDRP");settings.supportWater=true;settings.supportWaterDecals=true;
                if((int)settings.waterDecalAtlasSize<1024)settings.waterDecalAtlasSize=WaterAtlasSize.AtlasSize1024;
                settings.maximumWaterDecalCount=Mathf.Max(1,settings.maximumWaterDecalCount);
                asset.currentPlatformRenderPipelineSettings=settings;EditorUtility.SetDirty(asset);
            }
            // Preserve larger atlases and existing decal budgets; reserve enough for both passes.
            var graphics=world.GetComponent<PoolroomsGraphics>();
            Camera camera=graphics!=null&&graphics.targetCamera!=null?graphics.targetCamera:
                world.viewer!=null?world.viewer.GetComponentInChildren<Camera>():null;
            if(camera!=null)
            {
                var data=camera.GetComponent<HDAdditionalCameraData>();
                if(data==null)data=Undo.AddComponent<HDAdditionalCameraData>(camera.gameObject);
                Undo.RecordObject(data,"Agua en cámara Poolrooms");data.customRenderingSettings=true;
                data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Water,true);
                data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Water]=true;
                data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.WaterDecals,true);
                data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.WaterDecals]=true;
                EditorUtility.SetDirty(data);
            }
            adapter.RefreshWater();
            EditorUtility.SetDirty(adapter);EditorSceneManager.MarkSceneDirty(world.gameObject.scene);AssetDatabase.SaveAssets();
            Debug.Log("Agua v9 instalada sin regenerar arquitectura. Guarda la escena y espera la recompilación de HDRP. "+
                "Appearance en PoolroomsHDRP permite elegir Clear Ceramic, Soft Turquoise, Blue Porcelain o Custom. Se conserva la calidad gráfica de la escena.",world);
        }
    }
}
#endif
