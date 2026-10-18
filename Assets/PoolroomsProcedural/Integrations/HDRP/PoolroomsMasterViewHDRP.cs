#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsMasterView))]
    public sealed class PoolroomsMasterViewHDRP : MonoBehaviour,IPoolroomsMasterViewBridge
    {
        private Light previousKey,previousReverse;
        private HDAdditionalLightData key,reverse;
        public void Apply(PoolroomsMasterView view)
        {
            if(view==null||view.KeyFill==null||view.ReverseFill==null)return;
            if(previousKey!=view.KeyFill||previousReverse!=view.ReverseFill)
            {
                previousKey=view.KeyFill;previousReverse=view.ReverseFill;
                key=Data(previousKey);reverse=Data(previousReverse);
            }
            float ev=5.5f;var world=view.World;
            if(world!=null)
            {
                var graphics=world.GetComponent<PoolroomsGraphics>();if(graphics!=null)ev=graphics.exposureEV;
                var terrain=world.GetComponent<PoolroomsTerrainAtmosphere>();
                if(world.Terrain!=null&&world.Terrain.IsInside&&terrain!=null)ev=terrain.CurrentExposure;
                else
                {
                    var solar=world.GetComponent<PoolroomsSolarAtmosphere>();
                    if(solar!=null&&solar.DaylightActive)ev=solar.CurrentExposure;
                }
            }
            // Scale physical lux with the existing camera exposure, rather than replacing its volume.
            float lux=MasterViewPolicy.FillLux(view.Strength,ev);
#pragma warning disable CS0618
            key.SetIntensity(lux,LightUnit.Lux);reverse.SetIntensity(lux,LightUnit.Lux);
#pragma warning restore CS0618
            key.rayTraceContactShadow=false;reverse.rayTraceContactShadow=false;
            previousKey.shadows=previousReverse.shadows=LightShadows.None;
        }
        private static HDAdditionalLightData Data(Light source)
        {
            var data=source.GetComponent<HDAdditionalLightData>();return data!=null?data:source.gameObject.AddComponent<HDAdditionalLightData>();
        }
    }
}
#endif
