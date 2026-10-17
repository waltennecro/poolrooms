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
        private LiminalEnvironment previous;
        private bool previousMall,configured;
        private float previousHeight;
        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            world=GetComponent<PoolroomsWorld>();root=new GameObject("Independent biome sky and fog");root.transform.SetParent(transform,false);
            volume=root.AddComponent<Volume>();volume.isGlobal=true;volume.priority=4;volume.enabled=false;
            profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Runtime independent biome atmosphere";
            var environment=profile.Add<VisualEnvironment>();environment.skyType.Override((int)SkyType.Gradient);environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            sky=profile.Add<GradientSky>();fog=profile.Add<Fog>();volume.sharedProfile=profile;
        }
        private void Update()
        {
            if(volume==null)return;
            Room room=world!=null&&world.isActiveAndEnabled&&world.profile!=null&&world.profile.independentBiomeMaterials?world.RoomAtViewer():null;
            bool active=room!=null&&room.Environment!=LiminalEnvironment.Poolrooms&&(world.Terrain==null||!world.Terrain.IsInside);
            if(volume.enabled!=active){volume.enabled=active;if(!active&&world!=null&&world.isActiveAndEnabled)world.ExternalGeometryChanged();}
            if(!active){configured=false;return;}
            bool mall=world.InsideMall;
            float height=mall?world.ActiveMallRoot.position.y:world.transform.position.y;
            if(configured&&previous==room.Environment&&previousMall==mall&&Mathf.Abs(height-previousHeight)<.01f)return;
            previous=room.Environment;previousMall=mall;previousHeight=height;configured=true;
            Color top=new Color(.57f,.66f,.78f),middle=new Color(.84f,.87f,.9f),bottom=new Color(.7f,.73f,.76f);float ev=-1;
            if(mall){top=new Color(.48f,.65f,.84f);middle=new Color(.83f,.88f,.94f);bottom=new Color(.78f,.81f,.83f);ev=0;}
            else if(room.Environment==LiminalEnvironment.Hotel)
            {top=new Color(.6f,.57f,.51f);middle=new Color(.83f,.79f,.71f);bottom=new Color(.67f,.64f,.59f);ev=-1.4f;}
            else if(room.Environment==LiminalEnvironment.IndoorPlay)
            {top=new Color(.57f,.71f,.85f);middle=new Color(.87f,.9f,.9f);bottom=new Color(.75f,.81f,.82f);}
            else if(room.Environment==LiminalEnvironment.SoftPlay)
            {top=new Color(.63f,.73f,.83f);middle=new Color(.89f,.9f,.87f);bottom=new Color(.74f,.78f,.73f);ev=-.8f;}
            sky.top.Override(top);sky.middle.Override(middle);sky.bottom.Override(bottom);sky.exposure.Override(ev);
            fog.baseHeight.Override(height);fog.maximumHeight.Override(height+(mall?world.ActiveMallPlan.Floors*MallPlan.FloorHeight+12:room.Height+8));
            fog.meanFreePath.Override(mall?500:350);fog.colorMode.Override(FogColorMode.ConstantColor);fog.color.Override(middle);
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
