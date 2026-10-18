#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    // Sky and fog are biome-specific; exposure, optics and flashlight volumes remain owned by their existing systems.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsBiomeAtmosphere : MonoBehaviour
    {
        private PoolroomsWorld world;
        private GameObject root;
        private Volume volume;
        private VolumeProfile profile;
        private GradientSky sky;
        private Fog fog;
        private Light citySun;
        private HDAdditionalLightData citySunData;
        private Exposure cityExposure;
        private VisualEnvironment environment;
        private PhysicallyBasedSky citySky;
        private float regionalEV;
        private LiminalEnvironment previous;
        private bool previousMall,configured;
        private float previousHeight;
        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            world=GetComponent<PoolroomsWorld>();root=new GameObject("Independent biome sky and fog");root.transform.SetParent(transform,false);
            volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=4;volume.enabled=false;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Runtime independent biome atmosphere";
            environment=profile.Add<VisualEnvironment>();environment.skyType.Override((int)SkyType.Gradient);environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            sky=profile.Add<GradientSky>();fog=profile.Add<Fog>();volume.sharedProfile=profile;
            citySky=profile.Add<PhysicallyBasedSky>();citySky.active=false;citySky.groundTint.Override(new Color(.5f,.51f,.48f));
            cityExposure=profile.Add<Exposure>();cityExposure.active=false;cityExposure.mode.Override(ExposureMode.Fixed);cityExposure.fixedExposure.Override(11.7f);
            var sunObject=new GameObject("City11 regional sun");sunObject.transform.SetParent(root.transform,false);sunObject.transform.localRotation=Quaternion.Euler(43,-38,0);
            citySun=sunObject.AddComponent<Light>();citySun.type=LightType.Directional;citySun.color=new Color(1,.95f,.88f);citySun.shadows=LightShadows.Soft;citySun.enabled=false;
            citySunData=sunObject.AddComponent<HDAdditionalLightData>();citySunData.interactsWithSky=true;
#pragma warning disable CS0618
            citySunData.SetIntensity(24000,LightUnit.Lux);
#pragma warning restore CS0618
        }
        private void Update()
        {
            if(volume==null)return;
            Room room=world!=null&&world.isActiveAndEnabled&&world.profile!=null&&world.profile.independentBiomeMaterials?world.RoomAtViewer():null;
            bool aerial=room!=null&&world.IsPilotingAircraft&&(world.Terrain==null||!world.Terrain.IsInside);
            bool active=aerial||room!=null&&room.Environment!=LiminalEnvironment.Poolrooms&&(world.Terrain==null||!world.Terrain.IsInside);
            volume.priority=aerial?14:4;
            bool sunny=aerial||active&&(room.Environment==LiminalEnvironment.Orangerie||room.Environment==LiminalEnvironment.City11||room.Environment==LiminalEnvironment.Courtyard188||room.Environment==LiminalEnvironment.Coast48||QuietLevelPlanner.IsCivic(room.Environment));
            bool archive=!aerial&&active&&room.Environment==LiminalEnvironment.Archive906;
            bool sunset=!aerial&&sunny&&room.Environment==LiminalEnvironment.Coast48;
            if(citySun.enabled!=sunny)citySun.enabled=sunny;cityExposure.active=sunny||archive;citySky.active=sunny;
            if(sunny||archive)
            {
                var graphics=world.GetComponent<PoolroomsGraphics>();
                var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                int shadows=world.shadowLightBudgetOverride>=0?world.shadowLightBudgetOverride:world.profile.maxShadowLights;
                var shadowMode=shadows>0?LightShadows.Soft:LightShadows.None;if(citySun.shadows!=shadowMode)citySun.shadows=shadowMode;
                bool rt=shadows>0&&graphics!=null&&graphics.RayTracingRequested&&asset!=null&&asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.supportScreenSpaceShadows;
                if(citySunData.useScreenSpaceShadows!=rt)citySunData.useScreenSpaceShadows=rt;if(citySunData.useRayTracedShadows!=rt)citySunData.useRayTracedShadows=rt;
                if(citySunData.numRayTracingSamples!=(rt?8:1))citySunData.numRayTracingSamples=rt?8:1;if(!citySunData.filterTracedShadow)citySunData.filterTracedShadow=true;
                Vector3 local=world.viewer.position-world.RoomWorldPosition(room);bool shelter=archive;
                if(!aerial&&room.Plan!=null&&room.Plan.Garden!=null)shelter=true;
                if(room.Plan!=null&&room.Plan.Quiet!=null)shelter=room.Plan.Quiet.Sheltered(local.x,local.z,room.Width);
                if(room.Plan!=null&&room.Plan.City!=null)foreach(var building in room.Plan.City.Buildings)
                    if(Mathf.Abs(local.x-building.X)<building.Width*.5f&&Mathf.Abs(local.z-building.Z)<building.Depth*.5f)shelter=true;
                if(aerial)shelter=false;
                float offset=graphics!=null?graphics.exposureEV-5.5f:0,target=(!aerial&&room.Plan!=null&&room.Plan.Garden!=null?8.2f+room.Plan.Garden.Roof*.12f:archive?6.3f:sunset?10.8f:shelter?7.7f:11.7f)+offset;
                if(regionalEV<=0)regionalEV=target;
                regionalEV=Mathf.Lerp(regionalEV,target,1-Mathf.Exp(-Time.unscaledDeltaTime*2));cityExposure.fixedExposure.Override(regionalEV);
            }
            if(volume.enabled!=active){volume.enabled=active;if(!active&&world!=null&&world.isActiveAndEnabled)world.ExternalGeometryChanged();}
            if(!active){configured=false;return;}
            bool mall=!aerial&&world.InsideMall;
            var effectiveEnvironment=aerial?LiminalEnvironment.City11:room.Environment;
            float height=mall?world.ActiveMallRoot.position.y:world.transform.position.y;
            if(configured&&previous==effectiveEnvironment&&previousMall==mall&&Mathf.Abs(height-previousHeight)<.01f)return;
            previous=effectiveEnvironment;previousMall=mall;previousHeight=height;configured=true;
            environment.skyType.Override((int)(sunny?SkyType.PhysicallyBased:SkyType.Gradient));
            citySun.transform.localRotation=Quaternion.Euler(sunset?18:43,sunset?-65:-38,0);
            citySun.color=sunset?new Color(1,.73f,.46f):new Color(1,.95f,.88f);
#pragma warning disable CS0618
            citySunData.SetIntensity(sunset?15000:24000,LightUnit.Lux);
#pragma warning restore CS0618
            Color top=new Color(.57f,.66f,.78f),middle=new Color(.84f,.87f,.9f),bottom=new Color(.7f,.73f,.76f);float ev=-1;
            if(mall){top=new Color(.48f,.65f,.84f);middle=new Color(.83f,.88f,.94f);bottom=new Color(.78f,.81f,.83f);ev=0;}
            else if(room.Environment==LiminalEnvironment.Hotel)
            {top=new Color(.6f,.57f,.51f);middle=new Color(.83f,.79f,.71f);bottom=new Color(.67f,.64f,.59f);ev=-1.4f;}
            else if(room.Environment==LiminalEnvironment.IndoorPlay)
            {top=new Color(.57f,.71f,.85f);middle=new Color(.87f,.9f,.9f);bottom=new Color(.75f,.81f,.82f);}
            else if(room.Environment==LiminalEnvironment.SoftPlay)
            {top=new Color(.63f,.73f,.83f);middle=new Color(.89f,.9f,.87f);bottom=new Color(.74f,.78f,.73f);ev=-.8f;}
            else if(archive){top=new Color(.57f,.56f,.5f);middle=new Color(.82f,.8f,.74f);bottom=new Color(.65f,.62f,.55f);ev=-1;}
            sky.top.Override(top);sky.middle.Override(middle);sky.bottom.Override(bottom);sky.exposure.Override(ev);
            fog.baseHeight.Override(height);fog.maximumHeight.Override(height+(mall?world.ActiveMallPlan.Floors*MallPlan.FloorHeight+12:room.Height+8));
            fog.meanFreePath.Override(sunny?850:mall?500:350);fog.colorMode.Override(FogColorMode.ConstantColor);fog.color.Override(sunset?new Color(.91f,.79f,.67f):middle);
            // Refresh existing reflection probes through their bounded queue, not a capture every frame.
            world.ExternalGeometryChanged();
            // No volumetric toggle here: quality and pause-menu preferences keep control of it.
        }
        private void OnDisable()
        {
            if(volume!=null)volume.enabled=false;
            if(root!=null)Destroy(root);
            if(profile!=null){foreach(var component in profile.components)if(component!=null)Destroy(component);Destroy(profile);}
            root=null;volume=null;profile=null;configured=false;
        }
    }
}
#endif
