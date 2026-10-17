using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    public interface IPoolroomsMasterViewBridge { void Apply(PoolroomsMasterView view); }

    [DefaultExecutionOrder(180),DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsMasterView : MonoBehaviour
    {
        private PoolroomsGameSettings settings;
        private readonly MasterViewOptions fallback=new MasterViewOptions();
        private MasterViewOptions Options { get { return settings!=null&&settings.Preferences!=null?settings.Preferences.rendering:fallback; } }
        private PoolroomsExplorer explorer;
        private PoolroomsWorld ownedWorld;
        private MasterViewOptions ownedOptions;
        private Camera cameraView;
        private float baseFar,appliedFar;
        private bool ownsClip;
        private GameObject fillRoot;
        private IPoolroomsMasterViewBridge bridge;
        public Light KeyFill { get; private set; }
        public Light ReverseFill { get; private set; }
        public PoolroomsWorld World { get { return explorer!=null?explorer.world:null; } }
        public float Strength { get { return Options.lightStrength; } }
        private void Awake(){settings=GetComponent<PoolroomsGameSettings>();explorer=GetComponent<PoolroomsExplorer>();}
        public void ApplySettings()
        {
            // Explorer creates this component before its settings component in Start.
            if(settings==null)settings=GetComponent<PoolroomsGameSettings>();
            var world=World;
            bool active=explorer!=null&&explorer.isActiveAndEnabled&&isActiveAndEnabled;
            if(ownedWorld!=null&&(world!=ownedWorld||!active))ReleaseWorld();
            if(active&&world!=null)
            {
                ownedOptions=Options;ownedOptions.Sanitize();ownedWorld=world;
                world.renderingView=ownedOptions;
            }
            bool lit=active&&Options.inspectionLight;
            if(lit&&fillRoot==null)CreateLights();
            if(KeyFill!=null){KeyFill.enabled=lit;ReverseFill.enabled=lit;}
        }
        private void LateUpdate()
        {
            ApplySettings();
            Camera current=explorer!=null&&explorer.eyes!=null?explorer.eyes.GetComponent<Camera>():null;
            if(current!=cameraView){ReleaseCameraDistance();cameraView=current;}
            float distance=RequestedDistance();
            if(cameraView!=null&&distance>0)
            {
                // Observe external changes; release before the terrain system captures/restores its clip.
                if(!ownsClip){baseFar=cameraView.farClipPlane;ownsClip=true;}
                else if(Mathf.Abs(cameraView.farClipPlane-appliedFar)>.01f)baseFar=cameraView.farClipPlane;
                appliedFar=Mathf.Max(baseFar,distance);cameraView.farClipPlane=appliedFar;
            }
            else ReleaseCameraDistance();
            if(KeyFill==null||!KeyFill.enabled)return;
            KeyFill.transform.rotation=Quaternion.Euler(40,45,0);
            ReverseFill.transform.rotation=Quaternion.Euler(-35,225,0);
            KeyFill.intensity=ReverseFill.intensity=Mathf.Lerp(.12f,.65f,Strength);
            bridge?.Apply(this);
        }
        private float RequestedDistance()
        {
            if(!isActiveAndEnabled||World==null||World.CurrentLayout==null)return 0;
            var options=Options;
            if(World.Terrain!=null&&World.Terrain.IsInside)
                return options.terrainRadius>0?TerrainRegionPlan.TileSize*(World.TerrainRadius+1)*1.8f:0;
            if(World.InsideMall)
                return options.mallRadius>0||options.mallFloorRadius>0?World.ActiveMallPlan.Width+World.ActiveMallPlan.Depth:0;
            return options.roomRadius>0?World.CurrentLayout.Config.CellSize*(World.LoadRadius+1)*1.8f:0;
        }
        private void CreateLights()
        {
            fillRoot=new GameObject("Graphics visibility fill (no shadows)");fillRoot.transform.SetParent(transform,false);
            KeyFill=CreateLight("Soft key fill");ReverseFill=CreateLight("Soft reverse fill");
            var pipeline=GraphicsSettings.currentRenderPipeline;
            var type=Type.GetType("Poolrooms.HDRP.PoolroomsMasterViewHDRP, Poolrooms.HDRP");
            if(type!=null&&pipeline!=null&&pipeline.GetType().Name.Contains("HDRenderPipeline"))bridge=gameObject.AddComponent(type) as IPoolroomsMasterViewBridge;
        }
        private Light CreateLight(string name)
        {
            var root=new GameObject(name);root.transform.SetParent(fillRoot.transform,false);
            var light=root.AddComponent<Light>();light.type=LightType.Directional;light.enabled=false;
            light.color=new Color(.94f,.97f,1);light.shadows=LightShadows.None;light.bounceIntensity=0;
            return light;
        }
        public void ReleaseCameraDistance()
        {
            if(ownsClip&&cameraView!=null&&Mathf.Abs(cameraView.farClipPlane-appliedFar)<.01f)cameraView.farClipPlane=baseFar;
            ownsClip=false;
        }
        private void ReleaseWorld()
        {
            if(ownedWorld!=null&&ReferenceEquals(ownedWorld.renderingView,ownedOptions))ownedWorld.renderingView=null;
            ownedWorld=null;ownedOptions=null;
        }
        public void Release()
        {
            ReleaseWorld();ReleaseCameraDistance();
            if(KeyFill!=null)KeyFill.enabled=false;if(ReverseFill!=null)ReverseFill.enabled=false;
        }
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();if(fillRoot!=null)Destroy(fillRoot);}
    }
}
