using System.Collections.Generic;
using UnityEngine;

namespace Poolrooms
{
    // HDRP has one cascaded directional shadow atlas, shared by every loaded biome.
    [DefaultExecutionOrder(32000),DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsDirectionalLighting : MonoBehaviour
    {
        struct Original {public LightShadows Shadows;public bool Enabled;}
        readonly Dictionary<Light,Original> originals=new Dictionary<Light,Original>();
        readonly List<Light> retired=new List<Light>();
        Light[] lights=System.Array.Empty<Light>();
        PoolroomsWorld world;float refreshAt;bool refresh=true;
        void OnEnable(){world=GetComponent<PoolroomsWorld>();world.GeometryChanged+=Invalidate;refresh=true;}
        void Invalidate(){refresh=true;}
        void LateUpdate()
        {
            if(!Application.isPlaying||world==null)return;
            if(refresh||Time.unscaledTime>=refreshAt)
            {
                retired.Clear();foreach(var pair in originals)if(pair.Key==null)retired.Add(pair.Key);foreach(var dead in retired)originals.Remove(dead);retired.Clear();
                lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None);refresh=false;refreshAt=Time.unscaledTime+.25f;
            }
            Light winner=null;int score=-1;
            bool shadows=world.profile!=null&&(world.shadowLightBudgetOverride>=0?world.shadowLightBudgetOverride:world.profile.maxShadowLights)>0;
            foreach(var light in lights)if(light!=null&&light.type==LightType.Directional)
            {
                if(!originals.TryGetValue(light,out var original)){original=new Original{Shadows=light.shadows,Enabled=light.enabled};originals.Add(light,original);}
                bool owned=light.GetComponentInParent<PoolroomsWorld>()==world;
                if(owned&&light.shadows!=LightShadows.None&&original.Shadows!=light.shadows){original.Shadows=light.shadows;originals[light]=original;}
                if(!owned)light.enabled=original.Enabled;
                if(!shadows||!light.enabled||!light.gameObject.activeInHierarchy||original.Shadows==LightShadows.None)continue;
                int priority=owned?2:1;if(world.Terrain!=null&&world.Terrain.IsInside&&light==world.Terrain.Sun)priority=3;
                if(priority>score||priority==score&&(winner==null||light.GetInstanceID()<winner.GetInstanceID())){winner=light;score=priority;}
            }
            foreach(var light in lights)if(light!=null&&light.type==LightType.Directional&&originals.TryGetValue(light,out var original))
            {
                light.shadows=light==winner?original.Shadows:LightShadows.None;
                // A template scene's sun must not add a second daylight contribution indoors.
                if(winner!=null&&score>=2&&light.GetComponentInParent<PoolroomsWorld>()!=world&&original.Shadows!=LightShadows.None)light.enabled=false;
            }
        }
        void OnDisable()
        {
            if(world!=null)world.GeometryChanged-=Invalidate;
            foreach(var pair in originals)if(pair.Key!=null){pair.Key.shadows=pair.Value.Shadows;if(pair.Key.GetComponentInParent<PoolroomsWorld>()!=world)pair.Key.enabled=pair.Value.Enabled;}
            originals.Clear();lights=System.Array.Empty<Light>();
        }
    }
}
