#if UNITY_EDITOR && POOLROOMS_HDRP_17
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    public static class PoolroomsGraphicsMenu
    {
        [MenuItem("Tools/Poolrooms/Gráficos/1 - Aplicar Rendimiento")]
        public static void Performance(){Apply(PoolroomsGraphicsQuality.Rendimiento);}
        [MenuItem("Tools/Poolrooms/Gráficos/2 - Aplicar Alto")]
        public static void High(){Apply(PoolroomsGraphicsQuality.Alto);}
        [MenuItem("Tools/Poolrooms/Gráficos/3 - Aplicar Cinemático RT")]
        public static void Cinematic(){Apply(PoolroomsGraphicsQuality.CinematicoRT);}
        [MenuItem("Tools/Poolrooms/Gráficos/4 - Aplicar Ultra")]
        public static void Ultra(){Apply(PoolroomsGraphicsQuality.Ultra);}
        private static PoolroomsWorld FindWorld()
        {
            var world=Selection.activeGameObject!=null?Selection.activeGameObject.GetComponentInParent<PoolroomsWorld>():null;
            if(world!=null)return world;
            var all=Object.FindObjectsByType<PoolroomsWorld>(FindObjectsSortMode.None);
            if(all.Length==1)return all[0];
            Debug.LogWarning(all.Length==0?"Abre tu escena Poolrooms o usa Tools > Poolrooms > Crear demo.":"Selecciona el Poolrooms World que quieres mejorar.");return null;
        }
        private static void Apply(PoolroomsGraphicsQuality quality)
        {
            if(!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset))
            {Debug.LogWarning("Abre un proyecto HDRP 17.x y asigna el HDRP Asset antes de aplicar gráficos.");return;}
            var world=FindWorld();if(world==null)return;
            var graphics=world.GetComponent<PoolroomsGraphics>();
            if(Application.isPlaying)
            {
                if(graphics==null){Debug.LogWarning("Sal de Play y aplica una calidad para instalar la mejora en esta escena.");return;}
                graphics.quality=quality;graphics.Apply();Debug.Log(graphics.Status,world);return;
            }
            var camera=graphics!=null&&graphics.targetCamera!=null?graphics.targetCamera:
                world.viewer!=null?world.viewer.GetComponentInChildren<Camera>():Camera.main;
            if(camera==null){Debug.LogWarning("Asigna una cámara al Explorer o etiqueta tu cámara como MainCamera.");return;}
            // Upgrade owns a new copy of the atmosphere, leaving existing asset references elsewhere intact.
            if(graphics==null||graphics.atmosphere==null||graphics.atmosphere.sharedProfile==null)
            {
                string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/PoolroomsGraphics");
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                var previous=world.transform.Find("Poolrooms Atmosphere");
                var oldVolume=previous!=null?previous.GetComponent<Volume>():null;
                if(oldVolume!=null){Undo.RecordObject(oldVolume,"Actualizar atmósfera Poolrooms");oldVolume.enabled=false;}
                if(world.GetComponent<PoolroomsHDRP>()==null)Undo.AddComponent<PoolroomsHDRP>(world.gameObject);
                PoolroomsHDRPSetup.CreateEnvironment(world.gameObject,camera,folder);
                graphics=world.GetComponent<PoolroomsGraphics>();
            }
            Undo.RecordObject(graphics,"Calidad Poolrooms");graphics.quality=quality;
            Save(graphics);Selection.activeGameObject=world.gameObject;Debug.Log(graphics.Status,world);
        }
        public static void Save(PoolroomsGraphics graphics)
        {
            graphics.Apply();
            if(Application.isPlaying)return;
            if(graphics.atmosphere!=null&&graphics.atmosphere.sharedProfile!=null)
                PoolroomsHDRPSetup.SaveVolume(graphics.atmosphere.sharedProfile);
            EditorUtility.SetDirty(graphics);
            var adapter=graphics.GetComponent<PoolroomsHDRP>();if(adapter!=null)EditorUtility.SetDirty(adapter);
            if(graphics.targetCamera!=null)
            {
                var data=graphics.targetCamera.GetComponent<HDAdditionalCameraData>();if(data!=null)EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(graphics.gameObject.scene);
        }
        [MenuItem("Tools/Poolrooms/Gráficos/4 - Preparar HDRP Asset (agua y reflejos)")]
        public static void PrepareAsset()
        {
            if(Application.isPlaying){Debug.LogWarning("Sal de Play para preparar el HDRP Asset.");return;}
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if(asset==null){Debug.LogWarning("No hay un HDRP Asset activo.");return;}
            Undo.RecordObject(asset,"Funciones gráficas Poolrooms");var s=asset.currentPlatformRenderPipelineSettings;
            s.supportWater=true;s.supportWaterDecals=true;s.supportSSR=true;s.supportSSRTransparent=true;s.supportSSGI=true;
            s.supportSSAO=true;s.supportVolumetrics=true;s.supportMotionVectors=true;
            // DXR itself is still configured explicitly through the HDRP Wizard.
            if(s.supportRayTracing)s.hdShadowInitParams.supportScreenSpaceShadows=true;
            asset.currentPlatformRenderPipelineSettings=s;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
            Debug.Log("HDRP Asset preparado. Espera a que Unity recompile y vuelve a aplicar la calidad elegida. Esto afecta a todas las escenas que usan este Asset.",asset);
        }
        [MenuItem("Tools/Poolrooms/Gráficos/6 - Mejorar materiales de esta escena")]
        public static void Materials()
        {
            if(Application.isPlaying){Debug.LogWarning("Sal de Play para guardar materiales.");return;}
            var world=FindWorld();if(world==null||world.profile==null)return;
            string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/PoolroomsMaterials");
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var oldProfile=world.profile;
            var profile=Object.Instantiate(oldProfile);profile.name=oldProfile.name+" Graphics";
            AssetDatabase.CreateAsset(profile,folder+"/PoolroomsProfile.asset");
            var copies=new Dictionary<Material,Material>();
            Material Copy(Material source,bool ceramic)
            {
                if(source==null||source.shader==null||source.shader.name!="HDRP/Lit")return source;
                if(copies.TryGetValue(source,out var cached))return cached;
                var material=new Material(source){name=source.name+" Refined"};
                void Set(string property,float value){if(material.HasProperty(property))material.SetFloat(property,value);}
                Set("_EnableGeometricSpecularAA",1);Set("_SpecularAAScreenSpaceVariance",.1f);Set("_SpecularAAThreshold",.2f);
                if(ceramic)Set("_NormalScale",.55f);
                HDMaterial.ValidateMaterial(material);
                AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+material.name.Replace('/', '_')+".mat"));
                copies.Add(source,material);return material;
            }
            profile.tile=Copy(oldProfile.tile,true);profile.accent=Copy(oldProfile.accent,true);
            profile.plaster=Copy(oldProfile.plaster,false);profile.metal=Copy(oldProfile.metal,false);
            profile.slide=Copy(oldProfile.slide,false);
            Undo.RecordObject(world,"Mejorar materiales Poolrooms");world.profile=profile;
            // Frozen meshes keep their geometry/lightmap UVs; swap material references only.
            if(world.GeneratedRoot!=null)foreach(var renderer in world.GeneratedRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&copies.TryGetValue(materials[i],out var replacement))
                {materials[i]=replacement;changed=true;}
                if(changed){Undo.RecordObject(renderer,"Materiales Poolrooms");renderer.sharedMaterials=materials;EditorUtility.SetDirty(renderer);}
            }
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(world);AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);
            Debug.Log("Materiales refinados guardados en "+folder+". Conservan texturas y color; el perfil original sigue disponible. Si había lightmaps, vuelve a hornear tras modificar materiales.",world);
        }
        [MenuItem("Tools/Poolrooms/Gráficos/5 - Comprobar ray tracing")]
        public static void Diagnose()
        {
            PoolroomsGraphics.CanRayTrace(out string reason);
            Debug.Log("Poolrooms RT: "+reason+". GPU: "+SystemInfo.graphicsDeviceName+". API: "+SystemInfo.graphicsDeviceType+
                ". Para configurar DXR: Window > Rendering > HDRP Wizard > HDRP + DXR. Desactiva Static Batching y comprueba recursos DXR. "+
                "Después aplica Cinemático RT. El agua usa el sistema nativo HDRP, SSR y sondas; no se presenta como agua trazada por rayos.");
        }
    }
    [CustomEditor(typeof(PoolroomsGraphics))]
    public sealed class PoolroomsGraphicsInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var graphics=(PoolroomsGraphics)target;
            EditorGUILayout.HelpBox("Alto mejora reflejos, agua y sombras. CinematicoRT usa DXR cuando GPU, API y HDRP lo permiten; en otro caso aplica Alto. El menú Gráficos permite preparar el HDRP Asset.",MessageType.Info);
            if(GUILayout.Button("Aplicar gráficos a esta escena"))PoolroomsGraphicsMenu.Save(graphics);
        }
    }
}
#endif
