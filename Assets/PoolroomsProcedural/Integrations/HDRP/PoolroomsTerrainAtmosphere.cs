#if POOLROOMS_HDRP_17
using System;
using System.Threading;
using System.Threading.Tasks;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsTerrainAtmosphere : MonoBehaviour,IPoolroomsTerrainAtmosphere
    {
        private PoolroomsWorld world;
        private PoolroomsTerrainSystem terrain;
        private GameObject root;
        private Volume volume;
        private VolumeProfile profile;
        private HDRISky sky;
        private VisualEnvironment visual;
        private Fog fog;
        private Exposure exposure;
        private Cubemap clouds,upload;
        private Task<V3[][]> bake;
        private CancellationTokenSource cancellation;
        private V3[][] uploadPixels;
        private int cachedSeed,requestedSeed,uploadFace,shadowResolution;
        private bool hasSky;
        private TerrainVisualQuality cachedQuality,requestedQuality;
        private HDAdditionalLightData sun;
        private Light preparedSun;
        private float currentEV,previousOffset;
        public float CurrentExposure {get{return currentEV>0?currentEV:12;}}
        public Cubemap CurrentSky {get{return clouds;}}
        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            world=GetComponent<PoolroomsWorld>();root=new GameObject("Nivel 94 independent daylight atmosphere");root.transform.SetParent(transform,false);
            volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=12;volume.enabled=false;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
            visual=profile.Add<VisualEnvironment>();visual.skyType.Override((int)SkyType.HDRI);visual.skyType.overrideState=false;visual.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            sky=profile.Add<HDRISky>();sky.exposure.Override(9);fog=profile.Add<Fog>();exposure=profile.Add<Exposure>();exposure.mode.Override(ExposureMode.Fixed);
        }
        private void Update()
        {
            if(volume==null)return;terrain=world!=null?world.Terrain:null;
            bool present=terrain!=null&&terrain.RegionRoot!=null&&terrain.Sun!=null;
            if(present)
            {
                if(preparedSun!=terrain.Sun)
                {
                    preparedSun=terrain.Sun;sun=preparedSun.GetComponent<HDAdditionalLightData>();if(sun==null)sun=preparedSun.gameObject.AddComponent<HDAdditionalLightData>();
#pragma warning disable CS0618
                    sun.SetIntensity(terrain.Plan.Daylight==TerrainDaylight.SoftClouds?18000:terrain.Plan.Daylight==TerrainDaylight.WarmAfternoon?32000:45000,LightUnit.Lux);
                    sun.SetShadowResolutionOverride(true);
#pragma warning restore CS0618
                    sun.normalBias=.12f;sun.slopeBias=.5f;shadowResolution=0;
                }
                int resolution=TerrainAppearance.ShadowSize(terrain.VisualQuality);
                if(shadowResolution!=resolution)
                {
#pragma warning disable CS0618
                    sun.SetShadowResolution(resolution);
#pragma warning restore CS0618
                    shadowResolution=resolution;
                }
                int budget=world.shadowLightBudgetOverride>=0?world.shadowLightBudgetOverride:world.profile.maxShadowLights;
                terrain.Sun.shadows=budget>0?LightShadows.Soft:LightShadows.None;
                // A shadowless directional light would leak through the other biomes' roofs.
                if(terrain.IsConnected)terrain.Sun.enabled=terrain.RegionRoot.gameObject.activeInHierarchy&&budget>0;
            }
            bool active=present&&terrain.IsInside;
            PumpSky(active);if(volume==null)return; // A failed job can disable and release this component.
            // Retain the previous environment until the new sky is completely uploaded.
            volume.enabled=active;
            bool skyReady=active&&hasSky&&cachedSeed==terrain.Plan.Seed;
            visual.skyType.overrideState=skyReady;sky.hdriSky.overrideState=skyReady;
            if(!active)return;
            if(currentEV<=0)currentEV=TerrainAppearance.Exposure(terrain.Plan.Daylight,terrain.InteriorShelter,terrain.Sun.enabled);
            float baseHeight=terrain.RegionRoot.position.y;
            fog.baseHeight.Override(baseHeight+20);fog.maximumHeight.Override(baseHeight+220);
            fog.meanFreePath.Override(terrain.Plan.Daylight==TerrainDaylight.SoftClouds?650:terrain.Plan.Daylight==TerrainDaylight.WarmAfternoon?800:950);
            fog.colorMode.Override(FogColorMode.ConstantColor);fog.color.Override(terrain.Plan.Daylight==TerrainDaylight.WarmAfternoon?new Color(.77f,.76f,.70f):new Color(.69f,.8f,.88f));
            var graphics=world.GetComponent<PoolroomsGraphics>();float offset=graphics!=null?graphics.exposureEV-5.5f:0;
            if(terrain.Explorer!=null&&terrain.Explorer.IsPaused&&Mathf.Abs(offset-previousOffset)>.0001f)currentEV=Mathf.Clamp(currentEV+offset-previousOffset,4,18);
            previousOffset=offset;
            float target=TerrainAppearance.Exposure(terrain.Plan.Daylight,terrain.InteriorShelter,terrain.Sun.enabled)+offset;
            currentEV=Mathf.Lerp(currentEV,Mathf.Clamp(target,4,18),1-Mathf.Exp(-Time.deltaTime*2.5f));exposure.fixedExposure.Override(currentEV);
        }
        private bool Requested(bool active)
        {return active&&terrain.Plan.Seed==requestedSeed&&terrain.VisualQuality==requestedQuality;}
        private void PumpSky(bool active)
        {
            if(bake!=null)
            {
                if(!Requested(active))cancellation.Cancel();
                if(!bake.IsCompleted)return;
                bool valid=Requested(active)&&bake.Status==TaskStatus.RanToCompletion;
                if(valid)uploadPixels=bake.Result;
                if(bake.IsFaulted){Debug.LogException(bake.Exception,this);enabled=false;return;}
                bake=null;cancellation.Dispose();cancellation=null;
                if(valid)
                {
                    int size=TerrainAppearance.SkySize(requestedQuality);upload=new Cubemap(size,TextureFormat.RGBAHalf,true){name="Nivel 94 baked PBR sky",filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};uploadFace=0;
                }
            }
            if(upload!=null)
            {
                if(!Requested(active)){Destroy(upload);upload=null;uploadPixels=null;return;}
                V3[] source=uploadPixels[uploadFace];var pixels=new Color[source.Length];
                for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(source[i].X,source[i].Y,source[i].Z,1);
                upload.SetPixels(pixels,(CubemapFace)uploadFace);uploadPixels[uploadFace]=null;uploadFace++;
                if(uploadFace<6)return; // One face upload per frame; no Unity access in the worker.
                upload.Apply(true,true);if(clouds!=null)Destroy(clouds);clouds=upload;upload=null;uploadPixels=null;
                cachedSeed=requestedSeed;cachedQuality=requestedQuality;hasSky=true;sky.hdriSky.Override(clouds);
                if(currentEV<=0)currentEV=TerrainAppearance.Exposure(terrain.Plan.Daylight,terrain.InteriorShelter,terrain.Sun.enabled);
                world.ExternalGeometryChanged();return;
            }
            if(!active||hasSky&&cachedSeed==terrain.Plan.Seed&&cachedQuality==terrain.VisualQuality)return;
            requestedSeed=terrain.Plan.Seed;requestedQuality=terrain.VisualQuality;
            var snapshot=terrain.Plan;int resolution=TerrainAppearance.SkySize(requestedQuality);
            cancellation=new CancellationTokenSource();CancellationToken token=cancellation.Token;
            bake=Task.Run(()=>
            {
                var faces=new V3[6][];
                for(int f=0;f<6;f++){token.ThrowIfCancellationRequested();faces[f]=TerrainSkyBake.Face(snapshot,resolution,f);}
                token.ThrowIfCancellationRequested();return faces;
            },token);
        }
        public void Release()
        {
            if(volume!=null)volume.enabled=false;if(cancellation!=null)cancellation.Cancel();
            if(upload!=null)Destroy(upload);upload=null;uploadPixels=null;sun=null;preparedSun=null;
            // Keep only the last cubemap and integer seed, not the departed district's houses.
        }
        private void OnDisable()
        {
            Release();if(bake!=null)bake.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
            bake=null;if(cancellation!=null)cancellation.Dispose();cancellation=null;
            if(root!=null)Destroy(root);if(clouds!=null)Destroy(clouds);
            if(profile!=null){foreach(var c in profile.components)if(c!=null)Destroy(c);Destroy(profile);}
            root=null;volume=null;profile=null;clouds=null;hasSky=false;currentEV=previousOffset=0;
        }
    }
}
#endif
