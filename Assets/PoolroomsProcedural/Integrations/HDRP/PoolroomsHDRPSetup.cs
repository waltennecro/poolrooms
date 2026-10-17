#if UNITY_EDITOR && POOLROOMS_HDRP_17
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    // Called reflectively by the pipeline-independent setup menu.
    public static class PoolroomsHDRPSetup
    {
        public static void CreateEnvironment(GameObject root,Camera camera,string folder)
        {
            var go=new GameObject("Poolrooms Atmosphere");go.transform.SetParent(root.transform,false);
            var volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Poolrooms Atmosphere";
            AssetDatabase.CreateAsset(profile,AssetDatabase.GenerateUniqueAssetPath(folder+"/Atmosphere.asset"));
            var environment=profile.Add<VisualEnvironment>();environment.skyType.Override((int)SkyType.Gradient);
            environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            var sky=profile.Add<GradientSky>();
            sky.top.Override(new Color(.55f,.72f,.72f));sky.middle.Override(new Color(.7f,.82f,.78f));sky.bottom.Override(new Color(.42f,.55f,.5f));
            sky.exposure.Override(-1.5f);
            volume.sharedProfile=profile;
            var graphics=root.GetComponent<PoolroomsGraphics>();
            if(graphics==null)graphics=root.AddComponent<PoolroomsGraphics>();
            graphics.atmosphere=volume;graphics.targetCamera=camera;
            graphics.Apply();SaveVolume(profile);
            PoolroomsCameraSetup.InstallForWorld(root.GetComponent<PoolroomsWorld>(),camera);
            if(root.GetComponent<PoolroomsWaterInteractions>()==null)root.AddComponent<PoolroomsWaterInteractions>();
        }
        public static void SaveVolume(VolumeProfile profile)
        {
            // New overrides must be subassets to survive closing Unity.
            foreach(var component in profile.components)
            {
                if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
                EditorUtility.SetDirty(component);
            }
            EditorUtility.SetDirty(profile);
        }
    }
}
#endif
