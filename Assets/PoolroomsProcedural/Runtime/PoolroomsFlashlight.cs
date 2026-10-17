using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    public interface IPoolroomsFlashlightBridge { void Bind(PoolroomsFlashlight flashlight); }

    [DefaultExecutionOrder(120),DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsFlashlight : MonoBehaviour
    {
        private const string Key="Poolrooms.Flashlight.v1";
        private PoolroomsExplorer explorer;
        private FlashlightPreferences preferences=new FlashlightPreferences();
        private GameObject lightObject;
        private PoolroomsCameraEffects cameraEffects;
        private static readonly Vector3 RestPosition=new Vector3(.13f,-.1f,.18f);
        private bool on;
        public Light Source { get; private set; }
        public bool IsOn { get { return on; } }
        public FlashlightLevel Level { get { return preferences.level; } }
        public bool CastShadows { get { return preferences.shadows; } }
        public event Action Changed;
        private void OnEnable()
        {
            explorer=GetComponent<PoolroomsExplorer>();
            try{if(PlayerPrefs.HasKey(Key))preferences=JsonUtility.FromJson<FlashlightPreferences>(PlayerPrefs.GetString(Key));}
            catch(ArgumentException){preferences=new FlashlightPreferences();}
            if(preferences==null)preferences=new FlashlightPreferences();preferences.Sanitize();
        }
        private void Start(){EnsureLight();Apply();}
        private void LateUpdate()
        {
            if(Source==null){EnsureLight();Apply();}
            if(lightObject==null)return;
            if(cameraEffects==null)cameraEffects=lightObject.transform.parent.GetComponent<PoolroomsCameraEffects>();
            bool motion=cameraEffects!=null&&cameraEffects.isActiveAndEnabled;
            lightObject.transform.localPosition=RestPosition+(motion?cameraEffects.FlashlightOffset:Vector3.zero);
            lightObject.transform.localRotation=motion?Quaternion.Euler(cameraEffects.FlashlightAngles):Quaternion.identity;
        }
        private void EnsureLight()
        {
            if(lightObject!=null||explorer==null)return;
            Transform eyes=explorer.eyes;Camera camera=eyes!=null?eyes.GetComponent<Camera>():GetComponentInChildren<Camera>();
            if(camera==null)return;
            lightObject=new GameObject("Poolrooms portable flashlight");lightObject.transform.SetParent(camera.transform,false);
            lightObject.transform.localPosition=RestPosition;cameraEffects=camera.GetComponent<PoolroomsCameraEffects>();
            Source=lightObject.AddComponent<Light>();Source.type=LightType.Spot;Source.enabled=false;Source.useColorTemperature=false;
            Source.renderMode=LightRenderMode.ForcePixel;Source.bounceIntensity=1;
            var pipeline=GraphicsSettings.currentRenderPipeline;
            var bridge=Type.GetType("Poolrooms.HDRP.PoolroomsFlashlightHDRP, Poolrooms.HDRP");
            if(bridge!=null&&pipeline!=null&&pipeline.GetType().Name.Contains("HDRenderPipeline")){var adapter=lightObject.AddComponent(bridge) as IPoolroomsFlashlightBridge;if(adapter!=null)adapter.Bind(this);}
        }
        public void Toggle(){SetOn(!on);}
        public void SetOn(bool value){on=value;EnsureLight();Apply();}
        public void SetLevel(FlashlightLevel level){preferences.level=level;preferences.Sanitize();Save();EnsureLight();Apply();}
        public void Cycle(){SetLevel(FlashlightModel.Next(Level));}
        public void SetShadows(bool value){preferences.shadows=value;Save();Apply();}
        public void ResetPreferences(){preferences=new FlashlightPreferences();on=false;Save();EnsureLight();Apply();}
        private void Save(){PlayerPrefs.SetString(Key,JsonUtility.ToJson(preferences));PlayerPrefs.Save();}
        private void Apply()
        {
            if(Source==null)return;FlashlightLook look=FlashlightModel.Look(Level);
            Source.spotAngle=look.Cone;Source.innerSpotAngle=look.InnerCone;Source.range=look.Range;
            Source.color=new Color(look.Color.X,look.Color.Y,look.Color.Z);
            // Unitless fallback for built-in/URP; HDRP bridge replaces it with real lumens.
            Source.intensity=look.Lumens/650f;Source.shadows=preferences.shadows?LightShadows.Soft:LightShadows.None;
            Source.enabled=on&&isActiveAndEnabled;Changed?.Invoke();
        }
        private void OnDisable()
        {
            if(Source!=null)Source.enabled=false;
            if(lightObject!=null){lightObject.SetActive(false);if(Application.isPlaying)Destroy(lightObject);else DestroyImmediate(lightObject);}
            Source=null;lightObject=null;cameraEffects=null;
        }
    }
}
