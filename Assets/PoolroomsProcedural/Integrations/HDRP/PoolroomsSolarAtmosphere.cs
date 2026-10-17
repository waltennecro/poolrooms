#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    // One directional sun and one runtime profile, shared by all streamed solar rooms.
    // Closed rooms, malls and Level 94 retain their own lighting systems.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsHDRP))]
    public sealed class PoolroomsSolarAtmosphere : MonoBehaviour
    {
        private PoolroomsWorld world;
        private PoolroomsHDRP adapter;
        private GameObject root;
        private Light sun;
        private HDAdditionalLightData sunData;
        private Volume volume;
        private VolumeProfile profile;
        private PhysicallyBasedSky sky;
        private Exposure exposure;
        private Fog fog;
        private bool configured,lastLow,lastRT;
        private PoolClimateDescription lastClimate;
        private float lastEV,lastGain;
        public Light Sun { get { return sun; } }
        public bool DaylightActive { get { return sun!=null&&sun.enabled&&volume!=null&&volume.enabled; } }
        public float CurrentExposure { get { return exposure!=null?exposure.fixedExposure.value:5.5f; } }
        public bool RayTracedSunRequested { get; private set; }

        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            world=GetComponent<PoolroomsWorld>();adapter=GetComponent<PoolroomsHDRP>();
            root=new GameObject("Poolrooms physical sun and sky");root.transform.SetParent(transform,false);
            sun=root.AddComponent<Light>();sun.enabled=false;sun.type=LightType.Directional;
            sun.useColorTemperature=false;sun.shadows=LightShadows.Soft;
            sunData=root.AddComponent<HDAdditionalLightData>();sunData.interactsWithSky=true;
            sunData.flareSize=1.25f;sunData.flareMultiplier=.12f;sunData.flareFalloff=6;
            volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=5;volume.enabled=false;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Poolrooms physical daylight (runtime)";
            var environment=profile.Add<VisualEnvironment>();
            environment.skyType.Override((int)SkyType.PhysicallyBased);environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            sky=profile.Add<PhysicallyBasedSky>();sky.groundTint.Override(new Color(.55f,.58f,.53f));
            sky.horizonTint.Override(new Color(.96f,.97f,1));sky.zenithTint.Override(new Color(.8f,.89f,1));
            exposure=profile.Add<Exposure>();exposure.mode.Override(ExposureMode.Fixed);
            fog=profile.Add<Fog>();fog.colorMode.Override(FogColorMode.ConstantColor);
            volume.sharedProfile=profile;
        }
        private void Update()
        {
            if(volume==null)return;
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            Room room=world!=null&&world.isActiveAndEnabled&&world.viewer!=null&&adapter.isActiveAndEnabled&&asset!=null?world.RoomAtViewer():null;
            bool sunny=room!=null&&!world.IsPilotingAircraft&&world.CurrentLayout!=null&&world.CurrentLayout.Config.Lighting!=LightingDistribution.Classic&&room.Environment==LiminalEnvironment.Poolrooms&&room.Plan!=null&&room.Plan.Solar!=null&&SolarClimateRegions.HasSky(room.Plan.Solar.Climate);
            int shadows=world!=null&&world.profile!=null?(world.shadowLightBudgetOverride>=0?world.shadowLightBudgetOverride:world.profile.maxShadowLights):0;
            int lights=world!=null&&world.profile!=null?(world.activeLightBudgetOverride>=0?world.activeLightBudgetOverride:world.profile.maxActiveLights):0;
            bool terrainSun=world!=null&&world.Terrain!=null&&world.Terrain.Sun!=null&&world.Terrain.Sun.enabled;
            bool active=PoolroomSunPolicy.CanIlluminate(sunny,terrainSun,lights,shadows);
            if(!active){Deactivate();return;}
            var graphics=GetComponent<PoolroomsGraphics>();
            bool low=adapter.lowCost;
            bool rt=graphics!=null&&graphics.RayTracingRequested&&asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows;
            float ev=graphics!=null?graphics.exposureEV:5.5f,gain=adapter.lightMultiplier;
            var climate=room.Plan.Solar.ClimateDescription;
            bool changed=!configured||climate.Kind!=lastClimate.Kind||climate.RegionX!=lastClimate.RegionX||climate.RegionZ!=lastClimate.RegionZ||
                climate.SunDirection.X!=lastClimate.SunDirection.X||climate.SunDirection.Z!=lastClimate.SunDirection.Z||
                low!=lastLow||rt!=lastRT||Mathf.Abs(ev-lastEV)>.001f||Mathf.Abs(gain-lastGain)>.001f;
            if(changed)
            {
                Configure(climate,low,rt,ev,gain);
                lastClimate=climate;lastLow=low;lastRT=rt;lastEV=ev;lastGain=gain;configured=true;
                // Existing bounded probe queue; no new cubemaps and no per-frame captures.
                world.RequestLightingRefresh();
            }
            // Release the local aperture fallback before enabling the global source.
            world.SetPoolroomSunActive(true);sun.enabled=true;volume.enabled=true;
            RayTracedSunRequested=rt;
            float baseHeight=world.transform.position.y,topHeight=baseHeight+room.Height+8;
            if(Mathf.Abs(fog.baseHeight.value-baseHeight)>.01f)fog.baseHeight.Override(baseHeight);
            if(Mathf.Abs(fog.maximumHeight.value-topHeight)>.01f)fog.maximumHeight.Override(topHeight);
        }
        private void Configure(PoolClimateDescription climate,bool low,bool rt,float ev,float gain)
        {
            var look=PoolroomSunPolicy.Get(climate.Kind);var direction=climate.SunDirection;
            sun.transform.rotation=Quaternion.LookRotation(new Vector3(direction.X,direction.Y,direction.Z),Vector3.up);
            sun.color=new Color(climate.SunColor.X,climate.SunColor.Y,climate.SunColor.Z);
            sunData.angularDiameter=look.AngularDiameter;sunData.diameterOverride=look.AngularDiameter;
#pragma warning disable CS0618
            sunData.SetIntensity(look.Lux*gain,LightUnit.Lux);
            sunData.SetShadowResolutionOverride(true);sunData.SetShadowResolution(low?1024:2048);
            sunData.SetShadowFadeDistance(low?70:140);sunData.SetShadowNearPlane(.05f);
#pragma warning restore CS0618
            sunData.normalBias=.08f;sunData.slopeBias=.3f;
            sunData.useScreenSpaceShadows=rt;sunData.useRayTracedShadows=rt;
            sunData.numRayTracingSamples=rt?8:1;sunData.filterTracedShadow=true;
            sunData.rayTraceContactShadow=false;
            // Fixed location-dependent exposure, never driven by the torch or auto-adaptation.
            // The graphics brightness slider still supplies the same EV offset.
            exposure.fixedExposure.Override(ev+look.ExposureOffset);
            sky.airTint.Override(Color.white);sky.aerosolTint.Override(new Color(.92f,.95f,1));
            sky.aerosolDensity.Override(climate.Kind==PoolClimate.DiffuseDaylight?.035f:.012f);
            sky.aerosolAnisotropy.Override(.75f);
            fog.meanFreePath.Override(climate.Kind==PoolClimate.DiffuseDaylight?260:340);
            fog.color.Override(Color.Lerp(new Color(.86f,.93f,1),sun.color,.25f));
            // Volumetrics, SSR and indirect light remain controlled by Graphics preferences.
        }
        public void SuspendLighting(){Deactivate();}
        private void Deactivate()
        {
            bool changed=volume!=null&&volume.enabled;
            if(sun!=null)sun.enabled=false;
            if(volume!=null)volume.enabled=false;
            if(world!=null)world.SetPoolroomSunActive(false);
            RayTracedSunRequested=false;configured=false;
            if(changed&&world!=null&&world.isActiveAndEnabled)world.RequestLightingRefresh();
        }
        private void OnDisable()
        {
            Deactivate();
            if(root!=null)Destroy(root);
            if(profile!=null){foreach(var c in profile.components)if(c!=null)Destroy(c);Destroy(profile);}
            root=null;sun=null;sunData=null;volume=null;profile=null;
        }
    }
}
#endif
