#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsCameraEffects))]
    public sealed class PoolroomsCameraPresentation : MonoBehaviour
    {
        private PoolroomsCameraEffects effects;
        private GameObject volumeObject;
        private VolumeProfile profile;
        private ColorAdjustments color;
        private WhiteBalance balance;
        private Bloom bloom;
        private FilmGrain grain;
        private ChromaticAberration chromatic;
        private Vignette vignette;
        private PoolroomsCameraPostProcess optics;
        private float temperature=-2,contrast=4,saturation=-3,bloomAmount=.1f,grainAmount,chromaticAmount,vignetteAmount=.08f;
        private Vector4 analog;
        private float diffusion;
        private HDAdditionalCameraData cameraData;
        private bool savedCustom;
        private int savedVolumeMask;
        private bool changedVolumeMask;
        private static readonly FrameSettingsField[] requiredFrames={FrameSettingsField.Postprocess,FrameSettingsField.CustomPostProcess,
            FrameSettingsField.ColorGrading,FrameSettingsField.Bloom,FrameSettingsField.FilmGrain,FrameSettingsField.ChromaticAberration,FrameSettingsField.Vignette};
        private readonly bool[] savedEnabled=new bool[requiredFrames.Length],savedOverrides=new bool[requiredFrames.Length];
        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            effects=GetComponent<PoolroomsCameraEffects>();
            cameraData=GetComponent<HDAdditionalCameraData>();
            if(cameraData==null)cameraData=gameObject.AddComponent<HDAdditionalCameraData>();
            savedCustom=cameraData.customRenderingSettings;
            savedVolumeMask=cameraData.volumeLayerMask.value;changedVolumeMask=savedVolumeMask==0;
            if(changedVolumeMask)cameraData.volumeLayerMask=1;
            for(int i=0;i<requiredFrames.Length;i++)
            {
                savedEnabled[i]=cameraData.renderingPathCustomFrameSettings.IsEnabled(requiredFrames[i]);
                savedOverrides[i]=cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)requiredFrames[i]];
                cameraData.renderingPathCustomFrameSettings.SetEnabled(requiredFrames[i],true);
                cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)requiredFrames[i]]=true;
            }
            cameraData.customRenderingSettings=true;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Poolrooms camera look (runtime)";
            color=profile.Add<ColorAdjustments>();balance=profile.Add<WhiteBalance>();bloom=profile.Add<Bloom>();
            grain=profile.Add<FilmGrain>();chromatic=profile.Add<ChromaticAberration>();vignette=profile.Add<Vignette>();
            optics=profile.Add<PoolroomsCameraPostProcess>();
            // Local volume follows this camera: other cameras and the Scene View keep their looks.
            volumeObject=new GameObject("Poolrooms camera volume");volumeObject.transform.SetParent(transform,false);
            int mask=cameraData.volumeLayerMask.value;
            for(int layer=0;layer<32;layer++)if((mask&(1<<layer))!=0){volumeObject.layer=layer;break;}
            var trigger=volumeObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=Vector3.one*.2f;
            var volume=volumeObject.AddComponent<Volume>();volume.isGlobal=false;volume.priority=40;volume.blendDistance=0;
            volume.sharedProfile=profile;
        }
        private void LateUpdate()
        {
            if(effects==null||profile==null)return;
            volumeObject.transform.position=cameraData.volumeAnchorOverride!=null?cameraData.volumeAnchorOverride.position:transform.position;
            var adapter=effects.world!=null?effects.world.GetComponent<PoolroomsHDRP>():null;
            effects.VisibleSourceBudget=adapter!=null&&adapter.lowCost?2:CameraPresentation.MaxFlares;
            effects.SourceIntensityScale=adapter!=null?Mathf.Max(0,adapter.lightMultiplier):1;
            var prefs=effects.preferences;
            var baseline=CameraPresentation.Look(CameraViewMode.Normal);var look=CameraPresentation.Look(prefs.viewMode);
            float t=prefs.styleIntensity,k=1-Mathf.Exp(-Time.unscaledDeltaTime*8);
            temperature=Mathf.Lerp(temperature,Mathf.Lerp(baseline.Temperature,look.Temperature,t),k);
            contrast=Mathf.Lerp(contrast,Mathf.Lerp(baseline.Contrast,look.Contrast,t),k);
            saturation=Mathf.Lerp(saturation,Mathf.Lerp(baseline.Saturation,look.Saturation,t),k);
            bloomAmount=Mathf.Lerp(bloomAmount,Mathf.Lerp(baseline.Bloom,look.Bloom,t)*prefs.bloomScale,k);
            grainAmount=Mathf.Lerp(grainAmount,look.Grain*t*prefs.grainScale,k);
            chromaticAmount=Mathf.Lerp(chromaticAmount,look.Chromatic*t*prefs.chromaticScale,k);
            vignetteAmount=Mathf.Lerp(vignetteAmount,Mathf.Lerp(baseline.Vignette,look.Vignette,t)*prefs.vignetteScale,k);
            analog=Vector4.Lerp(analog,new Vector4(look.Scanlines,look.Bleed,prefs.reduceMotion?0:look.Jitter,look.Matte)*t,k);
            diffusion=Mathf.Lerp(diffusion,look.Diffusion*t,k);
            balance.temperature.Override(temperature);color.contrast.Override(contrast);color.saturation.Override(saturation);
            bloom.intensity.Override(bloomAmount);bloom.scatter.Override(.64f);
            grain.intensity.Override(grainAmount);chromatic.intensity.Override(chromaticAmount);vignette.intensity.Override(vignetteAmount);
            optics.enabledEffect.Override((prefs.flareIntensity>0&&effects.FlareCount>0)||
                (prefs.waterEffects&&prefs.splashIntensity>0&&effects.Wetness>.0001f)||analog.sqrMagnitude>.00001f||diffusion>.0001f);
            effects.Analog=analog;effects.Diffusion=diffusion;
        }
        private void OnDisable()
        {
            if(cameraData!=null)
            {
                for(int i=0;i<requiredFrames.Length;i++)
                {
                    cameraData.renderingPathCustomFrameSettings.SetEnabled(requiredFrames[i],savedEnabled[i]);
                    cameraData.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)requiredFrames[i]]=savedOverrides[i];
                }
                cameraData.customRenderingSettings=savedCustom;
                if(changedVolumeMask)cameraData.volumeLayerMask=savedVolumeMask;
            }
            if(volumeObject!=null){volumeObject.SetActive(false);if(Application.isPlaying)Destroy(volumeObject);else DestroyImmediate(volumeObject);}
            if(profile!=null){foreach(var component in profile.components)CoreUtils.Destroy(component);CoreUtils.Destroy(profile);}
            volumeObject=null;profile=null;
        }
    }
}
#endif
