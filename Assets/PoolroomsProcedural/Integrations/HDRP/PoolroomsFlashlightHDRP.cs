#if POOLROOMS_HDRP_17
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Poolrooms.HDRP
{
    [DisallowMultipleComponent,RequireComponent(typeof(Light))]
    public sealed class PoolroomsFlashlightHDRP : MonoBehaviour,IPoolroomsFlashlightBridge
    {
        private PoolroomsFlashlight flashlight;
        private HDAdditionalLightData data;
        private void OnEnable()
        {
            data=GetComponent<HDAdditionalLightData>();if(data==null)data=gameObject.AddComponent<HDAdditionalLightData>();
            Bind(GetComponentInParent<PoolroomsFlashlight>());
        }
        public void Bind(PoolroomsFlashlight value)
        {
            if(flashlight!=value){if(flashlight!=null)flashlight.Changed-=Apply;flashlight=value;if(flashlight!=null)flashlight.Changed+=Apply;}
            Apply();
        }
        private void Apply()
        {
            if(flashlight==null||data==null)return;
#pragma warning disable CS0618
            data.SetIntensity(FlashlightModel.Look(flashlight.Level).Lumens,LightUnit.Lumen);
            data.SetShadowResolutionOverride(true);data.SetShadowResolution(512);
            data.SetShadowNearPlane(.05f);data.SetShadowFadeDistance(80);
            data.normalBias=.1f;data.slopeBias=.5f;data.rayTraceContactShadow=false;
#pragma warning restore CS0618
        }
        private void OnDisable(){if(flashlight!=null)flashlight.Changed-=Apply;}
    }
}
#endif
