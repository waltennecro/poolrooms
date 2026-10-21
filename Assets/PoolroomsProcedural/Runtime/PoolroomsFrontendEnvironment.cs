using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // Scene-level lighting is not part of a GameObject prefab. Preserve it
    // separately when creating the gameplay scene; never edit shared assets.
    [Serializable]
    public sealed class PoolroomsFrontendEnvironment
    {
        public Material skybox;
        public Texture reflection;
        public AmbientMode ambientMode;
        public Color sky,equator,ground,fogColour;
        public float ambientIntensity,reflectionIntensity,fogDensity,fogStart,fogEnd;
        public int reflectionBounces;
        public DefaultReflectionMode reflectionMode;
        public bool fog;
        public FogMode fogMode;
        public int[] sunPath;

        public static PoolroomsFrontendEnvironment Capture(Transform source)
        {
            var result=new PoolroomsFrontendEnvironment
            {
                skybox=RenderSettings.skybox,reflection=RenderSettings.customReflectionTexture,
                ambientMode=RenderSettings.ambientMode,sky=RenderSettings.ambientSkyColor,
                equator=RenderSettings.ambientEquatorColor,ground=RenderSettings.ambientGroundColor,
                ambientIntensity=RenderSettings.ambientIntensity,reflectionIntensity=RenderSettings.reflectionIntensity,
                reflectionBounces=RenderSettings.reflectionBounces,reflectionMode=RenderSettings.defaultReflectionMode,
                fog=RenderSettings.fog,fogMode=RenderSettings.fogMode,fogColour=RenderSettings.fogColor,
                fogDensity=RenderSettings.fogDensity,fogStart=RenderSettings.fogStartDistance,fogEnd=RenderSettings.fogEndDistance
            };
            var sun=RenderSettings.sun;
            if(sun!=null&&source!=null&&sun.transform.IsChildOf(source))
            {
                var path=new List<int>();var at=sun.transform;
                while(at!=source){path.Add(at.GetSiblingIndex());at=at.parent;}
                path.Reverse();result.sunPath=path.ToArray();
            }
            return result;
        }

        public void Apply(Transform target)
        {
            RenderSettings.skybox=skybox;RenderSettings.ambientMode=ambientMode;
            RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;
            RenderSettings.ambientGroundColor=ground;RenderSettings.ambientIntensity=ambientIntensity;
            RenderSettings.defaultReflectionMode=reflectionMode;RenderSettings.customReflectionTexture=reflection;
            RenderSettings.reflectionIntensity=reflectionIntensity;RenderSettings.reflectionBounces=reflectionBounces;
            RenderSettings.fog=fog;RenderSettings.fogMode=fogMode;RenderSettings.fogColor=fogColour;
            RenderSettings.fogDensity=fogDensity;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;
            if(target==null||sunPath==null)return;
            var at=target;
            foreach(int index in sunPath)
            {
                if(index<0||index>=at.childCount)return;
                at=at.GetChild(index);
            }
            RenderSettings.sun=at.GetComponent<Light>();
        }
    }
}
