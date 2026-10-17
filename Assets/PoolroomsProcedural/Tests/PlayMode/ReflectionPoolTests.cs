#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ReflectionPoolTests
    {
        [UnityTest]
        public IEnumerator NearbyCapturesReuseTwoSlotsAndZeroReleasesThem()
        {
            Type adapterType=Type.GetType("Poolrooms.HDRP.PoolroomsHDRP, Poolrooms.HDRP");
            if(adapterType==null||GraphicsSettings.currentRenderPipeline==null||!GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline"))
                Assert.Ignore("Requires the HDRP integration and an active HDRP asset; not a core-only test.");
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                root=new GameObject("Reflection pool regression");root.SetActive(false);
                var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.enabled=false;world.generateOnStart=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();world.profile=profile;
                var geometry=new GameObject("Test chunks");geometry.transform.SetParent(root.transform,false);
                typeof(PoolroomsWorld).GetField("generatedRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,geometry.transform);
                var viewer=new GameObject("Reflection focus");viewer.transform.SetParent(root.transform,false);world.viewer=viewer.transform;
                for(int i=0;i<8;i++)
                {
                    var chunk=new GameObject("Sector "+i);chunk.transform.SetParent(geometry.transform,false);chunk.transform.localPosition=Vector3.right*i*40;
                    chunk.AddComponent<PoolroomsChunk>();var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Architecture";
                    cube.transform.SetParent(chunk.transform,false);cube.transform.localScale=Vector3.one*4;
                }
                var adapter=root.AddComponent(adapterType);adapterType.GetField("nativeWater").SetValue(adapter,false);
                adapterType.GetField("maxRealtimeProbes").SetValue(adapter,2);root.SetActive(true);
                yield return null;yield return null;
                Assert.That((int)adapterType.GetProperty("ActiveGeneratedProbeCount").GetValue(adapter),Is.EqualTo(2));
                var slots=(IList)adapterType.GetField("ownedProbes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(adapter);
                var first=(Component)slots[0];var second=(Component)slots[1];Vector3 capture=first.transform.position;
                viewer.transform.localPosition=Vector3.right*280;yield return new WaitForSecondsRealtime(.7f);
                Assert.That((int)adapterType.GetProperty("GeneratedProbeCount").GetValue(adapter),Is.EqualTo(2));
                Assert.That(slots[0],Is.SameAs(first));Assert.That(slots[1],Is.SameAs(second));
                Assert.That(first.transform.position,Is.Not.EqualTo(capture),"Pool must follow nearby sectors without allocating new captures.");
                adapterType.GetField("maxRealtimeProbes").SetValue(adapter,0);yield return new WaitForSecondsRealtime(.7f);
                Assert.That((int)adapterType.GetProperty("GeneratedProbeCount").GetValue(adapter),Is.Zero);
                Assert.That((int)adapterType.GetProperty("ActiveGeneratedProbeCount").GetValue(adapter),Is.Zero);
            }
            finally
            {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
#endif
