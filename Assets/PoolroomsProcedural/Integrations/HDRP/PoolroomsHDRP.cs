#if POOLROOMS_HDRP_17
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsHDRP : MonoBehaviour
    {
        [Tooltip("Enable Water in the active HDRP Asset and Camera Frame Settings first.")]
        public bool nativeWater=true;
        [Header("Agua v9")]
        public PoolWaterAppearance appearance=PoolWaterAppearance.ClearCeramic;
        public bool interactiveWater=true,impactFoam=true,underwaterEffects=true;
        [Range(0,1)] public float interactionIntensity=.8f;
        [Tooltip("Reduce las cáusticas en salas de luz tenue. No añade luz al agua.")]
        public bool lightingAwareCaustics=true;
        [HideInInspector] public PoolWaterDetail waterDetail=PoolWaterDetail.High;
        [Range(0,15)] public float rippleWindSpeed=1.2f;
        [Tooltip("Se usa con Appearance = Custom. Los otros modos aportan su paleta.")]
        public Color refraction=new Color(.83f,.96f,.94f);
        public Color scattering=new Color(.08f,.3f,.25f);
        [Range(0,4)] public float causticsIntensity=.35f;
        [Range(.5f,20)] public float absorptionDistance=4.5f;
        [Range(.1f,5)] public float refractionDistance=1.2f;
        [Range(.1f,2)] public float waterTimeScale=.65f;
        [Range(0,1)] public float waterSmoothness=.97f;
        [HideInInspector] public bool lowCost,contactShadows=true;
        [HideInInspector] public int shadowResolution=1024;
        [HideInInspector] public float lightMultiplier=1;
        [Tooltip("Shared realtime reflection pool for nearby rooms/sectors. Disable when using your own probes.")]
        public bool reflectionProbes=true;
        [Range(0,8),Tooltip("Maximum generated realtime probes across all biomes. Start at 2 on integrated graphics; 0 uses sky/SSR only.")]
        public int maxRealtimeProbes=2;
        [Min(.1f)] public float probeUpdateInterval=.25f;
        public WaterSurface water;
        private const string ProbeName="Room reflection probe";
        private readonly List<HDAdditionalReflectionData> ownedProbes=new List<HDAdditionalReflectionData>();
        private readonly List<HDAdditionalReflectionData> pendingProbes=new List<HDAdditionalReflectionData>();
        private float nextProbeUpdate,nextViewBudgetUpdate;
        private bool probesDirty=true;
        private Transform probeRoot;
        private readonly List<ProbeCandidate> probeCandidates=new List<ProbeCandidate>();
        private readonly List<int> probeAssignments=new List<int>();
        private struct ProbeCandidate { public PoolroomsChunk Chunk;public MeshRenderer Renderer;public float Distance; }
        private static readonly System.Comparison<ProbeCandidate> ByProbeDistance=(a,b)=>a.Distance!=b.Distance?a.Distance.CompareTo(b.Distance):a.Chunk.GetInstanceID().CompareTo(b.Chunk.GetInstanceID());
        public int GeneratedProbeCount { get { return ownedProbes.Count; } }
        public int ActiveGeneratedProbeCount { get { int count=0;foreach(var p in ownedProbes)if(p!=null&&p.isActiveAndEnabled)count++;return count; } }
        private PoolroomsWorld world;
        private bool refreshRequested;
        private bool waterGeometryDirty;
        private bool coastalLook,thermalLook;
        private Layout waterLayout;
        private float causticGain=1;
        private void OnValidate(){refreshRequested=true;}
        private void Update()
        {
            var current=world!=null?world.RoomAtViewer():null;bool coast=current?.Environment==LiminalEnvironment.Coast48,bath=current?.Environment==LiminalEnvironment.ThermalBaths;
            if(coast!=coastalLook||bath!=thermalLook){coastalLook=coast;thermalLook=bath;RefreshWater();}
            if(refreshRequested)
            {
                refreshRequested=false;ApplyVisualSettings();
                if(world!=null)foreach(var chunk in world.Chunks)ConfigureProbe(chunk);
                QueueProbeUpdates();
            }
            if(waterGeometryDirty)
            {
                waterGeometryDirty=false;
                if(world!=null&&waterLayout!=world.CurrentLayout)RefreshWater();
                else RefreshWaterMeshes();
            }
            // The same bounded pool serves rooms and every mall floor; widening the
            // streaming window must never allocate a cubemap for each loaded sector.
            if(world!=null&&(probesDirty||Time.realtimeSinceStartup>=nextViewBudgetUpdate))
            {
                RefreshProbePool();probesDirty=false;nextViewBudgetUpdate=Time.realtimeSinceStartup+.5f;
            }
            // Defer captures until after construction, connection gates and the light budget settle.
            RenderPendingProbes();
            if(water!=null&&water.enabled&&water.caustics&&world!=null&&world.CurrentLayout!=null)
            {
                Room room=world.RoomAtViewer();float target=1;
                if(lightingAwareCaustics&&room!=null&&world.CurrentLayout.Config.Lighting!=LightingDistribution.Classic)
                    target=Mathf.Clamp01(ZoneLighting.Get(ZoneLighting.Resolve(room,world.CurrentLayout.Config)).Power);
                float dt=Application.isPlaying?Time.deltaTime:.05f;
                causticGain=Mathf.Lerp(causticGain,target,1-Mathf.Exp(-dt*2));
                water.causticsIntensity=causticsIntensity*causticGain;
            }
        }
        private void OnEnable()
        {
            // Reuse saved/generated slots after a script reload as well as component toggles.
            probeRoot=transform.Find("Poolrooms bounded reflection pool");
            if(probeRoot!=null&&ownedProbes.Count==0)foreach(Transform child in probeRoot)
            {var existing=child.GetComponent<HDAdditionalReflectionData>();if(existing!=null){ownedProbes.Add(existing);probeAssignments.Add(int.MinValue);}}
            world=GetComponent<PoolroomsWorld>();world.GeometryChanged+=QueueWaterGeometry;world.GeometryChanged+=QueueProbeUpdates;
            world.LightsChanged+=QueueProbeUpdates;world.ChunkCreated+=ConfigureLights;world.ChunkCreated+=ConfigureProbe;
            foreach(var chunk in world.Chunks){ConfigureLights(chunk);ConfigureProbe(chunk);}
            RefreshWater();RefreshProbePool();QueueProbeUpdates();nextProbeUpdate=0;
            if(Application.isPlaying&&GetComponent<PoolroomsSolarAtmosphere>()==null)gameObject.AddComponent<PoolroomsSolarAtmosphere>();
            if(Application.isPlaying&&GetComponent<PoolroomsWaterInteractions>()==null)gameObject.AddComponent<PoolroomsWaterInteractions>();
        }
        private void OnDisable()
        {
            var solar=GetComponent<PoolroomsSolarAtmosphere>();if(solar!=null)solar.SuspendLighting();
            if(world!=null)
            {
                world.GeometryChanged-=QueueWaterGeometry;world.GeometryChanged-=QueueProbeUpdates;
                world.LightsChanged-=QueueProbeUpdates;world.ChunkCreated-=ConfigureLights;world.ChunkCreated-=ConfigureProbe;
                foreach(var chunk in world.Chunks)if(chunk.water!=null)chunk.water.enabled=true;
            }
            if(water!=null)water.enabled=false;
            foreach(var probe in ownedProbes)if(probe!=null)probe.enabled=false;
            pendingProbes.Clear(); // Retain slot identities across disable/re-enable; no growing texture pool.
            waterGeometryDirty=false;
        }
        private void QueueWaterGeometry(){waterGeometryDirty=true;}
        [ContextMenu("Refresh HDRP water")]
        public void RefreshWater()
        {
            if(world==null)world=GetComponent<PoolroomsWorld>();
            if(world.profile==null)return;
            waterLayout=world.CurrentLayout;
            var hdrpAsset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            bool available=hdrpAsset!=null&&hdrpAsset.currentPlatformRenderPipelineSettings.supportWater;
            if(!nativeWater||!available)
            {
                if(water!=null)water.enabled=false;
                foreach(var chunk in world.Chunks)if(chunk.water!=null)chunk.water.enabled=true;
                return;
            }
            if(water==null)
            {
                var go=new GameObject("HDRP Pool Water");go.transform.SetParent(transform,false);
                water=go.AddComponent<WaterSurface>();
                water.surfaceType=WaterSurfaceType.Pool;water.geometryType=WaterGeometryType.Custom;
            }
            float level=world.CurrentLayout!=null?world.CurrentLayout.Config.WaterLevel:world.profile.waterLevel;
            water.transform.localPosition=new Vector3(0,level,0);
            water.surfaceType=WaterSurfaceType.Pool;water.ripples=true;water.ripplesWindSpeed=rippleWindSpeed;
            var look=PoolWaterModel.Look(appearance);var budget=PoolWaterModel.Budget(lowCost?PoolWaterDetail.Performance:waterDetail);
            bool custom=appearance==PoolWaterAppearance.Custom;
            water.refractionColor=custom?refraction:new Color(look.Refraction.X,look.Refraction.Y,look.Refraction.Z);
            water.scatteringColor=custom?scattering:new Color(look.Scattering.X,look.Scattering.Y,look.Scattering.Z);
            water.absorptionDistance=custom?Mathf.Max(.001f,absorptionDistance):look.Absorption;
            if(coastalLook&&appearance==PoolWaterAppearance.ClearCeramic)
            {water.refractionColor=new Color(.75f,.9f,.96f);water.scatteringColor=new Color(.035f,.15f,.22f);water.absorptionDistance=7;}
            if(thermalLook&&appearance==PoolWaterAppearance.ClearCeramic)
            {water.refractionColor=new Color(.79f,.92f,.89f);water.scatteringColor=new Color(.075f,.23f,.2f);water.absorptionDistance=4.2f;}
            water.maxRefractionDistance=custom?Mathf.Clamp(refractionDistance,0,3.5f):look.RefractionDistance;
            water.ambientScattering=.025f;water.heightScattering=.05f;water.displacementScattering=.12f;
            water.directLightTipScattering=.18f;water.directLightBodyScattering=.22f;
            water.timeMultiplier=waterTimeScale;
            water.startSmoothness=waterSmoothness;water.endSmoothness=Mathf.Min(waterSmoothness,.945f);
            water.smoothnessFadeStart=12;water.smoothnessFadeDistance=35;
            water.ripplesChaos=.35f;water.ripplesOrientationValue=25;
            water.ripplesMotionMode=WaterPropertyOverrideMode.Custom;
            water.tessellation=budget.Tessellation>0;water.maxTessellationFactor=budget.Tessellation;
            water.tessellationFactorFadeStart=6;water.tessellationFactorFadeRange=24;
            bool decals=interactiveWater&&hdrpAsset.currentPlatformRenderPipelineSettings.supportWaterDecals;
            water.deformation=decals;water.deformationRes=(WaterSurface.WaterDecalRegionResolution)budget.SurfaceResolution;
            water.decalRegionSize=Vector2.one*budget.RegionSize;water.decalRegionAnchor=world.viewer;
            water.maximumHeightOverride=PoolWaterModel.MaxHeight;
            water.foam=decals&&budget.Foam&&impactFoam;
            water.foamResolution=(WaterSurface.WaterDecalRegionResolution)Mathf.Min(budget.SurfaceResolution,512);
            water.foamPersistenceMultiplier=.12f;water.simulationFoamAmount=0;
            water.foamColor=new Color(.94f,.98f,.98f);water.foamSmoothness=.45f;water.foamTextureTiling=2.5f;
            water.causticsResolution=lowCost?WaterSurface.WaterCausticsResolution.Caustics256:WaterSurface.WaterCausticsResolution.Caustics512;
            water.caustics=budget.Caustics;water.causticsIntensity=causticsIntensity*causticGain;
            water.causticsBand=0;water.virtualPlaneDistance=1.25f;water.causticsPlaneBlendDistance=.35f;
            RefreshWaterMeshes();
        }
        private void RefreshWaterMeshes()
        {
            if(world==null)return;
            if(water==null||!nativeWater)
            {foreach(var chunk in world.Chunks)if(chunk.water!=null)chunk.water.enabled=true;return;}
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if(asset==null||!asset.currentPlatformRenderPipelineSettings.supportWater)
            {water.enabled=false;foreach(var chunk in world.Chunks)if(chunk.water!=null)chunk.water.enabled=true;return;}
            if(water.meshRenderers==null)water.meshRenderers=new List<MeshRenderer>();
            water.meshRenderers.Clear();
            foreach(var chunk in world.Chunks)if(chunk.water!=null)
            {
                // HDRP draws the custom meshes itself. Hide the ordinary preview material.
                chunk.water.enabled=false;water.meshRenderers.Add(chunk.water);
            }
            water.enabled=water.meshRenderers.Count>0;
        }
        private void ConfigureProbe(PoolroomsChunk chunk)
        {
            if(chunk==null)return;
            // Remove only the legacy generated per-room probe; keep user-authored probes.
            Transform legacy=chunk.transform.Find(ProbeName);
            if(legacy!=null)
            {
                legacy.gameObject.SetActive(false);
                if(Application.isPlaying)Destroy(legacy.gameObject);else DestroyImmediate(legacy.gameObject);
            }
            probesDirty=true;
        }
        private void RefreshProbePool()
        {
            if(world==null)return;
            int limit=reflectionProbes?Mathf.Clamp(maxRealtimeProbes,0,8):0;
            if(lowCost)limit=Mathf.Min(limit,2);
            probeCandidates.Clear();Vector3 viewer=world.viewer!=null?world.viewer.position:world.transform.position;
            if(limit>0)foreach(var chunk in world.Chunks)
            {
                if(chunk==null||!chunk.gameObject.activeInHierarchy||chunk.GetComponent<PoolroomsTerrainChunk>()!=null||!world.UseNearEffects(chunk))continue;
                // Mall sectors provide local captures; the shell spans all floors and cannot.
                if(chunk.gameObject.name=="Mall structural shell")continue;
                if(chunk.GetComponentInChildren<HDAdditionalReflectionData>(false)!=null)continue;
                Transform architecture=chunk.transform.Find("Architecture");var renderer=architecture!=null?architecture.GetComponent<MeshRenderer>():null;
                if(renderer==null)continue;
                probeCandidates.Add(new ProbeCandidate{Chunk=chunk,Renderer=renderer,Distance=renderer.bounds.SqrDistance(viewer)});
            }
            probeCandidates.Sort(ByProbeDistance);int needed=Mathf.Min(limit,probeCandidates.Count);
            // Release surplus slots when lowering the cap, not merely their scheduled captures.
            while(ownedProbes.Count>limit)
            {
                int last=ownedProbes.Count-1;var probe=ownedProbes[last];ownedProbes.RemoveAt(last);probeAssignments.RemoveAt(last);
                pendingProbes.Remove(probe);
                if(probe!=null){probe.gameObject.SetActive(false);if(Application.isPlaying)Destroy(probe.gameObject);else DestroyImmediate(probe.gameObject);}
            }
            for(int i=0;i<needed;i++)
            {
                if(probeRoot==null){var root=new GameObject("Poolrooms bounded reflection pool");root.transform.SetParent(transform,false);probeRoot=root.transform;}
                if(i>=ownedProbes.Count)
                {
                    var go=new GameObject("Shared reflection slot "+i);go.SetActive(false);go.transform.SetParent(probeRoot,false);
                    var legacy=go.AddComponent<ReflectionProbe>();legacy.resolution=128;
                    var hd=go.GetComponent<HDAdditionalReflectionData>();if(hd==null)hd=go.AddComponent<HDAdditionalReflectionData>();
                    ownedProbes.Add(hd);probeAssignments.Add(int.MinValue);go.SetActive(true);
                }
                var probe=ownedProbes[i];if(probe==null)continue;
                var target=probeCandidates[i];Bounds bounds=target.Renderer.bounds;
                bool changed=probeAssignments[i]!=target.Chunk.GetInstanceID()||(probe.transform.position-bounds.center).sqrMagnitude>.0001f||!probe.enabled;
                probeAssignments[i]=target.Chunk.GetInstanceID();probe.transform.position=bounds.center;
                probe.transform.localRotation=Quaternion.identity;probe.transform.localScale=Vector3.one;
                probe.mode=ProbeSettings.Mode.Realtime;probe.realtimeMode=ProbeSettings.RealtimeMode.OnDemand;
                probe.influenceVolume.shape=InfluenceShape.Box;probe.influenceVolume.boxSize=bounds.size;
                Vector3 capture=new Vector3(bounds.center.x,target.Chunk.transform.position.y+1.7f,bounds.center.z);
                probe.settingsRaw.proxySettings.capturePositionProxySpace=probe.transform.InverseTransformPoint(capture);
                probe.PrepareCulling();probe.enabled=true;
                if(changed&&!pendingProbes.Contains(probe))pendingProbes.Add(probe);
            }
            for(int i=needed;i<ownedProbes.Count;i++)if(ownedProbes[i]!=null){ownedProbes[i].enabled=false;probeAssignments[i]=int.MinValue;}
            pendingProbes.RemoveAll(p=>p==null||!p.isActiveAndEnabled);
        }
        private void OnDestroy()
        {
            if(probeRoot!=null){probeRoot.gameObject.SetActive(false);if(Application.isPlaying)Destroy(probeRoot.gameObject);else DestroyImmediate(probeRoot.gameObject);}
        }
        private void QueueProbeUpdates()
        {
            probesDirty=true;
            pendingProbes.RemoveAll(p=>p==null||!p.isActiveAndEnabled);
            if(!reflectionProbes){pendingProbes.Clear();return;}
            foreach(var probe in ownedProbes)
                if(probe.isActiveAndEnabled&&!pendingProbes.Contains(probe))pendingProbes.Add(probe);
        }
        private void RenderPendingProbes()
        {
            if(!reflectionProbes||Time.realtimeSinceStartup<nextProbeUpdate)return;
            while(pendingProbes.Count>0)
            {
                var probe=pendingProbes[0];pendingProbes.RemoveAt(0);
                if(probe==null||!probe.isActiveAndEnabled)continue;
                probe.RequestRenderNextUpdate();
                nextProbeUpdate=Time.realtimeSinceStartup+Mathf.Max(.1f,probeUpdateInterval);
                break;
            }
        }
        public void ApplyVisualSettings()
        {
            if(world==null)world=GetComponent<PoolroomsWorld>();
            foreach(var chunk in world.Chunks)
            {
                ConfigureLights(chunk);
            }
            RefreshWater();RefreshProbePool();QueueProbeUpdates();
        }
        private void ConfigureLights(PoolroomsChunk chunk)
        {
            foreach(var renderer in chunk.GetComponentsInChildren<MeshRenderer>(true))
                renderer.rayTracingMode=renderer==chunk.water?UnityEngine.Experimental.Rendering.RayTracingMode.Off:
                    (renderer.GetComponentInParent<PoolroomsPickup>()!=null||renderer.GetComponentInParent<PoolroomsVehicle>()!=null||world.CurrentLayout!=null&&world.CurrentLayout.Config.Continuous)?UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform:UnityEngine.Experimental.Rendering.RayTracingMode.Static;
            foreach(var lamp in chunk.GetComponentsInChildren<PoolroomsLuminaire>(true))
            {
                Light l=lamp.source;if(l==null)continue;
                l.type=lamp.solarBeam?LightType.Spot:LightType.Rectangle;
                var hd=l.GetComponent<HDAdditionalLightData>();if(hd==null)hd=l.gameObject.AddComponent<HDAdditionalLightData>();
                l.areaSize=new Vector2(lamp.width,lamp.depth);
                // Virtual bounce must not appear as a floating rectangular lamp in reflections.
                hd.affectSpecular=!lamp.ambientSupport;
                // Supported across HDRP 17; Unity marks this convenience overload deprecated, not removed.
#pragma warning disable CS0618
                hd.SetIntensity(lamp.lumens*lightMultiplier,LightUnit.Lumen);
                hd.SetShadowResolutionOverride(true);hd.SetShadowResolution(shadowResolution);
                hd.SetShadowFadeDistance(lamp.solarBeam?Mathf.Max(90,lamp.fullRange+24):45);hd.SetShadowNearPlane(.05f);
                hd.normalBias=.2f;hd.slopeBias=.5f;
                hd.useContactShadow.useOverride=true;hd.useContactShadow.@override=contactShadows;
                hd.rayTraceContactShadow=false;
#pragma warning restore CS0618
            }
        }
    }
}
#endif
