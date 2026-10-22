#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace Poolrooms.HDRP
{
    // Two owned density volumes for the entire viewer, never per streamed chunk.
    // The 64^3 density is original CC0 data, decoded once on first entry.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsSky63Clouds : MonoBehaviour
    {
        private PoolroomsWorld world;
        private GameObject root;
        private Texture3D density;
        private readonly LocalVolumetricFog[] banks=new LocalVolumetricFog[2];
        private void OnEnable(){world=GetComponent<PoolroomsWorld>();}
        private void LateUpdate()
        {
            if(!Application.isPlaying||world==null||!world.isActiveAndEnabled||world.viewer==null){if(root!=null)root.SetActive(false);return;}
            var room=world.RoomAtViewer();bool active=room?.Plan?.Quiet?.Sky63!=null&&!world.InsideMall;
            var asset=GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            var graphics=world.GetComponent<PoolroomsGraphics>();var settings=world.viewer.GetComponentInParent<PoolroomsGameSettings>();
            active=active&&asset!=null&&asset.currentPlatformRenderPipelineSettings.supportVolumetrics&&graphics!=null&&graphics.quality!=PoolroomsGraphicsQuality.Rendimiento&&(settings==null||settings.Preferences.volumetricFog);
            if(!active){if(root!=null&&root.activeSelf)root.SetActive(false);return;}
            if(root==null)Create();if(root==null)return;if(!root.activeSelf)root.SetActive(true);
            float cell=world.profile.cellSize,span=Mathf.Max(256,(world.LoadRadius+2)*cell*2);
            // World-local position tracks origin rebases, independent of the player's altitude.
            var center=world.viewer.position;var origin=world.transform.position;center.y=origin.y;
            center.x=origin.x+Mathf.Floor((center.x-origin.x)/120)*120;center.z=origin.z+Mathf.Floor((center.z-origin.z)/120)*120;
            for(int i=0;i<2;i++)
            {
                var fog=banks[i];var p=fog.parameters;float width=span*(i==0?1:1.8f);
                p.size=new Vector3(width,i==0?28:44,width);p.textureTiling=new Vector3(width/120,1,width/120);
                // Whole-period recentering avoids clouds following each player footstep.
                fog.parameters=p;fog.transform.position=center+Vector3.down*(i==0?25:63);
            }
        }
        private void Create()
        {
            var data=Resources.Load<TextAsset>("Sky63/CloudDensity");if(data==null||data.bytes.Length!=64*64*64){enabled=false;return;}
            var raw=data.bytes;var colors=new Color32[raw.Length];for(int i=0;i<raw.Length;i++)colors[i]=new Color32(raw[i],raw[i],raw[i],raw[i]);
            density=new Texture3D(64,64,64,TextureFormat.RGBA32,false){name="Original Level63 cloud density",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            density.SetPixels32(colors);density.Apply(false,true);
            root=new GameObject("Level63 shared cloud sea");root.transform.SetParent(world.transform,false);
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Cloud bank "+i);go.transform.SetParent(root.transform,false);
                var fog=go.AddComponent<LocalVolumetricFog>();var p=fog.parameters;p.albedo=new Color(.96f,.98f,1);p.meanFreePath=i==0?5:12;
                p.volumeMask=density;p.textureScrollingSpeed=new Vector3(-.0008f,0,-.0003f);p.positiveFade=p.negativeFade=new Vector3(.12f,.2f,.12f);
                p.distanceFadeStart=20000;p.distanceFadeEnd=22000;fog.parameters=p;banks[i]=fog;
            }
        }
        private void OnDisable()
        {if(root!=null)Destroy(root);if(density!=null)Destroy(density);root=null;density=null;banks[0]=banks[1]=null;}
    }
}
#endif
