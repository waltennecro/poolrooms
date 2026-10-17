#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class MallDaylightWorldTests
    {
        private static void Refresh(PoolroomsWorld world)
        {
            typeof(PoolroomsWorld).GetMethod("LimitLights",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null);
        }
        private static void CheckBudget(PoolroomsWorld world,int lights,int shadows,bool daylight)
        {
            int active=0,shadowed=0,sun=0,local=0;
            foreach(var lamp in world.Luminaires)
            {
                if(lamp==null||lamp.source==null)continue;
                if(lamp.source.enabled&&lamp.gameObject.activeInHierarchy)
                {
                    active++;if(lamp.source.shadows!=LightShadows.None)shadowed++;
                    if(lamp.mallLocal)
                    {
                        local++;
                        Assert.That(MallLighting.FloorEligible(world.viewer.position.y-lamp.transform.position.y,lamp.floorOffset),Is.True,"An adjacent floor consumed a light slot.");
                        if(lamp.source.shadows==LightShadows.None)
                            Assert.That(lamp.source.range,Is.LessThanOrEqualTo(lamp.shadowlessRange+.001f));
                        else Assert.That(lamp.source.range,Is.EqualTo(lamp.fullRange).Within(.001f));
                    }
                    if(lamp.solarBeam&&lamp.transform.IsChildOf(world.ActiveMallRoot))
                    {
                        sun++;Assert.That(lamp.source.shadows,Is.Not.EqualTo(LightShadows.None),"Direct sun crosses floor slabs without shadows.");
                    }
                }
            }
            Assert.That(active,Is.LessThanOrEqualTo(lights));Assert.That(shadowed,Is.LessThanOrEqualTo(shadows));
            Assert.That(local,Is.GreaterThan(0));
            if(daylight)Assert.That(sun,Is.GreaterThan(0));else Assert.That(sun,Is.EqualTo(0));
        }
        [UnityTest]
        public IEnumerator SkylightKeepsOneShadowSlotAndLocalLampsFollowTheVisitedFloor()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.loadRadius=1;profile.curveSegments=12;
                profile.maxActiveLights=8;profile.maxShadowLights=1;string saved=JsonUtility.ToJson(profile);
                root=new GameObject("v32 daylight world");root.SetActive(false);root.transform.position=new Vector3(21,6,-17);
                var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;world.activeLightBudgetOverride=8;world.shadowLightBudgetOverride=1;
                actor=new GameObject("v32 daylight explorer");actor.SetActive(false);actor.transform.position=root.transform.position+Vector3.up*.1f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("v32 eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;world.viewer=actor.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                float deadline=Time.realtimeSinceStartup+90;
                while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);Assert.That(world.ConnectedMalls.VisitNearest(),Is.True);
                deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady&&world.InsideMall,Is.True);
                Refresh(world);CheckBudget(world,8,1,true);
                foreach(var chunk in world.Chunks)if(chunk.connectedMall)
                foreach(var renderer in chunk.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.gameObject.name=="Windows and skylight glazing")Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
                world.shadowLightBudgetOverride=0;Refresh(world);CheckBudget(world,8,0,false);
                // Move vertically to a real upper gallery; floor-local metadata survives sector normalization.
                explorer.Teleport(world.ActiveMallRoot.TransformPoint(new Vector3(0,MallPlan.FloorHeight+.1f,-18)));
                deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);world.shadowLightBudgetOverride=2;Refresh(world);CheckBudget(world,8,2,true);
                Assert.That(world.ActiveMallPlan.Floors,Is.InRange(2,6));Assert.That(JsonUtility.ToJson(profile),Is.EqualTo(saved),"Lighting changed the saved generation profile.");
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
