#if POOLROOMS_HDRP_17
using UnityEngine;
using Poolrooms.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    public enum PoolroomsGraphicsQuality { Rendimiento, Alto, CinematicoRT }

    // Owns only the Poolrooms volume/camera. Never changes Graphics APIs or pipeline assets at runtime.
    [DisallowMultipleComponent, RequireComponent(typeof(PoolroomsHDRP))]
    public sealed class PoolroomsGraphics : MonoBehaviour,IPoolroomsGraphicsSettings
    {
        public PoolroomsGraphicsQuality quality=PoolroomsGraphicsQuality.Alto;
        public Camera targetCamera;
        public Volume atmosphere;
        [Range(0,12)] public float exposureEV=5.5f;
        [Range(.25f,2)] public float lightMultiplier=1;
        [TextArea,SerializeField] private string status;
        public string Status => status;
        // A configured request, not a claim that the GPU rendered RT successfully.
        public bool RayTracingRequested { get; private set; }
        private VolumeProfile runtimeProfile,previousProfile;
        private FrameSettings savedFrames;
        private FrameSettingsOverrideMask savedMask;
        private bool savedCustom,cameraSaved,refresh;
        private HDAdditionalCameraData savedCamera;
        private HDAdditionalCameraData.AntialiasingMode savedAA;
        private int savedLightBudget,savedShadowBudget;
        private bool budgetSaved;
        private GamePreferences menuPreferences;
        public bool SettingsReady {get{return isActiveAndEnabled&&targetCamera!=null&&atmosphere!=null&&atmosphere.sharedProfile!=null&&GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;}}
        public bool RayTracingAvailable {get{return CanRayTrace(out _);}}
        public string SettingsStatus {get{return status;}}
        public bool Supports(SettingsGraphicsFeature feature)
        {
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;if(asset==null)return false;
            var s=asset.currentPlatformRenderPipelineSettings;
            switch(feature){case SettingsGraphicsFeature.Reflections:return true;case SettingsGraphicsFeature.IndirectLight:return s.supportSSGI;
                case SettingsGraphicsFeature.AmbientOcclusion:return s.supportSSAO;case SettingsGraphicsFeature.Volumetrics:return s.supportVolumetrics;case SettingsGraphicsFeature.WaterInteractions:return s.supportWater&&s.supportWaterDecals;default:return s.supportWater;}
        }
        public void CaptureSettings(GamePreferences values)
        {
            values.graphicsQuality=(int)quality;values.exposureEV=exposureEV;var adapter=GetComponent<PoolroomsHDRP>();
            if(adapter!=null){values.waterAppearance=adapter.appearance;values.waterRipples=adapter.interactiveWater;values.waterFoam=adapter.impactFoam;values.underwater=adapter.underwaterEffects;values.reflections=adapter.reflectionProbes;}
        }
        public void ApplySettings(GamePreferences values)
        {menuPreferences=values;if(values.graphicsQuality>=0)quality=(PoolroomsGraphicsQuality)values.graphicsQuality;exposureEV=values.exposureEV;Apply();}

        private void OnEnable(){if(Application.isPlaying)Apply();}
        private void OnValidate(){refresh=true;}
        private void Update(){if(refresh){refresh=false;Apply();}}
        private void OnDisable()
        {
            if(runtimeProfile!=null)
            {
                if(atmosphere!=null)atmosphere.profile=previousProfile;
                foreach(var c in runtimeProfile.components)Destroy(c);
                Destroy(runtimeProfile);runtimeProfile=null;
            }
            if(cameraSaved&&savedCamera!=null)
            {
                savedCamera.renderingPathCustomFrameSettings=savedFrames;
                savedCamera.renderingPathCustomFrameSettingsOverrideMask=savedMask;
                savedCamera.customRenderingSettings=savedCustom;savedCamera.antialiasing=savedAA;
            }
            cameraSaved=false;RayTracingRequested=false;
            var world=GetComponent<PoolroomsWorld>();
            if(budgetSaved&&world!=null){world.activeLightBudgetOverride=savedLightBudget;world.shadowLightBudgetOverride=savedShadowBudget;}
            budgetSaved=false;
        }
        public void Apply()
        {
            RayTracingRequested=false;
            if(atmosphere==null||atmosphere.sharedProfile==null||targetCamera==null)return;
            var profile=atmosphere.sharedProfile;
            if(Application.isPlaying)
            {
                if(runtimeProfile==null)
                {
                    previousProfile=atmosphere.HasInstantiatedProfile()?atmosphere.profile:null;
                    runtimeProfile=ScriptableObject.CreateInstance<VolumeProfile>();
                    runtimeProfile.name="Poolrooms graphics (runtime)";
                    foreach(var c in profile.components)runtimeProfile.components.Add(Instantiate(c));
                    atmosphere.profile=runtimeProfile;
                }
                profile=runtimeProfile;
            }
            bool capable=CanRayTrace(out string reason);
            bool rt=quality==PoolroomsGraphicsQuality.CinematicoRT&&capable;
            status=quality==PoolroomsGraphicsQuality.CinematicoRT
                ?(rt?"RT solicitado: reflejos e iluminación indirecta. Comprueba Rendering Debugger para confirmar el render.":"Alternativa Alto: "+reason)
                :quality+": reflejos SSR + sondas; ray tracing desactivado.";
            var pipeline=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if(pipeline==null){status="Se requiere un HDRP Asset activo.";return;}
            var settings=pipeline.currentPlatformRenderPipelineSettings;
            RayTracingRequested=rt;
            bool low=quality==PoolroomsGraphicsQuality.Rendimiento;
            ConfigureVolume(profile,low,rt,settings,exposureEV);
            if(menuPreferences!=null)
            {
                Get<ScreenSpaceReflection>(profile).enabled.Override(settings.supportSSR&&menuPreferences.reflections);
                Get<ScreenSpaceReflection>(profile).enabledTransparent.Override(settings.supportSSRTransparent&&menuPreferences.reflections);
                Get<GlobalIllumination>(profile).enable.Override(!low&&settings.supportSSGI&&menuPreferences.indirectLight);
                Get<ScreenSpaceAmbientOcclusion>(profile).active=menuPreferences.ambientOcclusion;
                Get<Fog>(profile).enableVolumetricFog.Override(!low&&settings.supportVolumetrics&&menuPreferences.volumetricFog);
            }
            var cameraData=targetCamera.GetComponent<HDAdditionalCameraData>();
            if(cameraData==null)cameraData=targetCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            if(Application.isPlaying&&!cameraSaved)
            {
                savedCamera=cameraData;savedFrames=cameraData.renderingPathCustomFrameSettings;
                savedMask=cameraData.renderingPathCustomFrameSettingsOverrideMask;savedCustom=cameraData.customRenderingSettings;
                savedAA=cameraData.antialiasing;cameraSaved=true;
            }
            cameraData.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            cameraData.customRenderingSettings=true;
            Frame(cameraData,FrameSettingsField.Water,settings.supportWater);
            Frame(cameraData,FrameSettingsField.WaterDecals,settings.supportWater&&settings.supportWaterDecals);
            Frame(cameraData,FrameSettingsField.SSR,settings.supportSSR);
            Frame(cameraData,FrameSettingsField.TransparentSSR,settings.supportSSRTransparent);
            Frame(cameraData,FrameSettingsField.SSGI,!low&&settings.supportSSGI);
            Frame(cameraData,FrameSettingsField.SSAO,settings.supportSSAO);
            Frame(cameraData,FrameSettingsField.RayTracing,rt);
            Frame(cameraData,FrameSettingsField.ScreenSpaceShadows,rt&&settings.hdShadowInitParams.supportScreenSpaceShadows);
            Frame(cameraData,FrameSettingsField.ReflectionProbe,true);
            Frame(cameraData,FrameSettingsField.ShadowMaps,true);
            Frame(cameraData,FrameSettingsField.ContactShadows,!low);
            Frame(cameraData,FrameSettingsField.Volumetrics,!low&&settings.supportVolumetrics);
            Frame(cameraData,FrameSettingsField.ReprojectionForVolumetrics,!low&&settings.supportVolumetrics);
            Frame(cameraData,FrameSettingsField.MotionVectors,settings.supportMotionVectors);
            Frame(cameraData,FrameSettingsField.Postprocess,true);
            Frame(cameraData,FrameSettingsField.ExposureControl,true);
            Frame(cameraData,FrameSettingsField.Antialiasing,true);
            if(menuPreferences!=null)
            {
                Frame(cameraData,FrameSettingsField.SSR,settings.supportSSR&&menuPreferences.reflections);
                Frame(cameraData,FrameSettingsField.TransparentSSR,settings.supportSSRTransparent&&menuPreferences.reflections);
                Frame(cameraData,FrameSettingsField.ReflectionProbe,menuPreferences.reflections);
                Frame(cameraData,FrameSettingsField.SSGI,!low&&settings.supportSSGI&&menuPreferences.indirectLight);
                Frame(cameraData,FrameSettingsField.SSAO,settings.supportSSAO&&menuPreferences.ambientOcclusion);
                Frame(cameraData,FrameSettingsField.Volumetrics,!low&&settings.supportVolumetrics&&menuPreferences.volumetricFog);
                Frame(cameraData,FrameSettingsField.ReprojectionForVolumetrics,!low&&settings.supportVolumetrics&&menuPreferences.volumetricFog);
            }
            var adapter=GetComponent<PoolroomsHDRP>();
            adapter.lowCost=low;adapter.lightMultiplier=lightMultiplier;
            adapter.waterDetail=low?PoolWaterDetail.Performance:rt?PoolWaterDetail.Cinematic:PoolWaterDetail.High;
            adapter.shadowResolution=low?512:1024;adapter.contactShadows=!low;
            adapter.probeUpdateInterval=low?.75f:.25f;
            if(menuPreferences!=null)
            {
                if(menuPreferences.waterDetail>=0)adapter.waterDetail=(PoolWaterDetail)menuPreferences.waterDetail;
                // lowCost intentionally caps expensive water effects in Rendimiento.
                adapter.appearance=menuPreferences.waterAppearance;adapter.interactiveWater=menuPreferences.waterRipples;
                adapter.impactFoam=menuPreferences.waterFoam;adapter.underwaterEffects=menuPreferences.underwater;
                adapter.reflectionProbes=menuPreferences.reflections;
            }
            adapter.ApplyVisualSettings();
            var world=GetComponent<PoolroomsWorld>();
            if(Application.isPlaying&&!budgetSaved)
            {savedLightBudget=world.activeLightBudgetOverride;savedShadowBudget=world.shadowLightBudgetOverride;budgetSaved=true;}
            world.activeLightBudgetOverride=low?10:24;world.shadowLightBudgetOverride=low?2:6;
            if(menuPreferences!=null)
            {
                if(menuPreferences.lightBudget>=0)world.activeLightBudgetOverride=menuPreferences.lightBudget;
                if(menuPreferences.shadowBudget>=0)world.shadowLightBudgetOverride=Mathf.Min(menuPreferences.shadowBudget,world.activeLightBudgetOverride);
            }
            if(rt&&!settings.hdShadowInitParams.supportScreenSpaceShadows)status+=" Sombras solares: mapas convencionales; activa Screen Space Shadows en el HDRP Asset para sombras RT.";
            if(!settings.supportWater)status+=" Activa Water en el HDRP Asset: ahora se muestra la malla de previsualización opaca.";
            if(!settings.supportSSR||!settings.supportSSGI||!settings.supportSSAO)
                status+=" Faltan funciones del HDRP Asset: usa Preparar HDRP Asset en el menú Gráficos.";
        }
        public static bool CanRayTrace(out string reason)
        {
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            var s=asset!=null?asset.currentPlatformRenderPipelineSettings:default(RenderPipelineSettings);
            return PoolroomsRayTracingPolicy.CanUse(asset!=null,SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12,
                SystemInfo.supportsRayTracing,s.supportRayTracing,s.supportSSR,s.supportSSGI,out reason);
        }
        private static void Frame(HDAdditionalCameraData camera,FrameSettingsField field,bool value)
        {
            camera.renderingPathCustomFrameSettings.SetEnabled(field,value);
            camera.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field]=true;
        }
        private static T Get<T>(VolumeProfile p) where T:VolumeComponent
        {return p.TryGet<T>(out var v)?v:p.Add<T>();}
        private static void Custom(VolumeComponentWithQuality v)
        {v.quality.levelAndOverride=(0,true);v.quality.overrideState=true;}
        private static void ConfigureVolume(VolumeProfile p,bool low,bool rt,RenderPipelineSettings settings,float ev)
        {
            var exposure=Get<Exposure>(p);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(ev);
            Get<WaterRendering>(p).enable.Override(settings.supportWater);
            var ssr=Get<ScreenSpaceReflection>(p);Custom(ssr);
            ssr.enabled.Override(settings.supportSSR);ssr.enabledTransparent.Override(settings.supportSSRTransparent);
            ssr.usedAlgorithm.Override(ScreenSpaceReflectionAlgorithm.PBRAccumulation);
            ssr.tracing.Override(rt?RayCastingMode.RayTracing:RayCastingMode.RayMarching);
            var mode=settings.supportedRayTracingMode==RenderPipelineSettings.SupportedRayTracingMode.Performance?RayTracingMode.Performance:RayTracingMode.Quality;
            ssr.mode.Override(mode);ssr.sampleCount.Override(rt?4:2);ssr.bounceCount.Override(1);
            ssr.minSmoothness=.35f;ssr.smoothnessFadeStart=.6f;ssr.rayMaxIterations=low?32:96;
            ssr.fullResolution=!low;ssr.rayLength=60;ssr.denoise=true;ssr.denoiserRadius=.5f;
            ssr.depthBufferThickness.Override(.12f);ssr.screenFadeDistance.Override(.08f);
            ssr.SetAllOverridesTo(true);
            var gi=Get<GlobalIllumination>(p);Custom(gi);gi.enable.Override(!low&&settings.supportSSGI);
            gi.tracing.Override(rt?RayCastingMode.RayTracing:RayCastingMode.RayMarching);gi.mode.Override(mode);
            gi.maxRaySteps=64;gi.fullResolutionSS.Override(false);gi.denoiseSS=true;gi.secondDenoiserPassSS=true;
            gi.rayLength=45;gi.sampleCount.Override(rt?4:2);gi.bounceCount.Override(rt?3:2);gi.denoise=true;
            gi.secondDenoiserPass=true;gi.fullResolution=false;gi.SetAllOverridesTo(true);
            // HDRP 17: AmbientOcclusion is an obsolete empty migration class, not a VolumeComponent.
            var ao=Get<ScreenSpaceAmbientOcclusion>(p);Custom(ao);ao.intensity.Override(.65f);
            ao.radius.Override(.45f);ao.directLightingStrength.Override(.15f);ao.temporalAccumulation.Override(true);
            ao.rayTracing.Override(false);ao.fullResolution=!low;ao.stepCount=low?4:8;
            ao.SetAllOverridesTo(true);
            var contact=Get<ContactShadows>(p);Custom(contact);contact.enable.Override(!low);
            contact.length.Override(.15f);contact.maxDistance.Override(25);contact.fadeDistance.Override(5);
            contact.opacity.Override(.8f);contact.sampleCount=16;contact.SetAllOverridesTo(true);
            var fog=Get<Fog>(p);fog.enabled.Override(true);fog.meanFreePath.Override(140);
            fog.maximumHeight.Override(12);fog.enableVolumetricFog.Override(!low&&settings.supportVolumetrics);
            fog.colorMode.Override(FogColorMode.ConstantColor);fog.color.Override(new Color(.76f,.82f,.8f));
            Get<Tonemapping>(p).mode.Override(TonemappingMode.ACES);
            var bloom=Get<Bloom>(p);bloom.intensity.Override(.1f);bloom.scatter.Override(.6f);
            var balance=Get<WhiteBalance>(p);balance.temperature.Override(-2);balance.tint.Override(-1);
            var color=Get<ColorAdjustments>(p);color.contrast.Override(4);color.saturation.Override(-3);color.postExposure.Override(0);
            Get<Vignette>(p).intensity.Override(.1f);Get<FilmGrain>(p).intensity.Override(0);
            Get<ChromaticAberration>(p).intensity.Override(0);
        }
    }
}
#endif
