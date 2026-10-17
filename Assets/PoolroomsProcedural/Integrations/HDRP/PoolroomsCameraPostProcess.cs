#if POOLROOMS_HDRP_17
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [Serializable,VolumeComponentMenu("Poolrooms/Camera optics")]
    public sealed class PoolroomsCameraPostProcess : CustomPostProcessVolumeComponent,IPostProcessComponent
    {
        public BoolParameter enabledEffect=new BoolParameter(false);
        private Material material;
        public override CustomPostProcessInjectionPoint injectionPoint { get { return CustomPostProcessInjectionPoint.AfterPostProcess; } }
        public override bool visibleInSceneView { get { return false; } }
        public bool IsActive(){return material!=null&&enabledEffect.value;}
        public override void Setup()
        {
            Shader shader=Resources.Load<Shader>("PoolroomsCameraPostProcess");
            if(shader!=null&&shader.isSupported)material=CoreUtils.CreateEngineMaterial(shader);
        }
        public override void Render(CommandBuffer cmd,HDCamera camera,RTHandle source,RTHandle destination)
        {
            var effects=camera.camera.GetComponent<PoolroomsCameraEffects>();
            if(material==null||effects==null||!effects.isActiveAndEnabled)
            {HDUtils.BlitCameraTexture(cmd,source,destination);return;}
            material.SetTexture("_MainTex",source);
            material.SetVector("_Analog",effects.Analog);material.SetFloat("_Diffusion",effects.Diffusion);
            material.SetFloat("_EffectTime",effects.EffectTime);
            material.SetFloat("_Wetness",effects.preferences.waterEffects?effects.Wetness*effects.preferences.splashIntensity:0);
            material.SetFloat("_WetSeed",effects.WetSeed);material.SetFloat("_WetElapsed",effects.WetElapsed);
            material.SetFloat("_FlareIntensity",effects.preferences.flareIntensity);
            material.SetFloat("_FlareSize",effects.preferences.flareSize);material.SetFloat("_GhostIntensity",effects.preferences.ghostIntensity);
            material.SetInt("_GhostCount",effects.VisibleSourceBudget<=2?1:2);
            material.SetInt("_FlareCount",effects.FlareCount);
            material.SetVectorArray("_FlarePositions",effects.flarePositions);material.SetVectorArray("_FlareColors",effects.flareColors);
            HDUtils.DrawFullScreen(cmd,material,destination);
        }
        public override void Cleanup(){CoreUtils.Destroy(material);material=null;}
    }
}
#endif
