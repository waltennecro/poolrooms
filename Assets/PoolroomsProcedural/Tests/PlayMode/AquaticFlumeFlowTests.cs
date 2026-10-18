#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms.Tests
{
    public sealed class AquaticFlumeFlowTests
    {
        [Test]
        public void NativeFlowMaterialAdvectsAndPausePreservesPhase()
        {
            var root=new GameObject("Native flume film materials");root.SetActive(false);
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();var flow=PoolroomsFlumeFlow.For(world);
                var first=flow.Resolve(2);Assert.That(flow.Resolve(2),Is.SameAs(first));
                foreach(int i in Enumerable.Range(0,5))Assert.That(flow.Resolve(i),Is.Not.Null);
                foreach(string property in new[]{"_BaseColorMap","_NormalMap","_MaskMap"})if(first.HasProperty(property))Assert.That(first.GetTexture(property),Is.Not.Null,"Flow texture missing: "+property);
                if(first.HasProperty("_SurfaceType"))Assert.That(first.GetFloat("_SurfaceType"),Is.EqualTo(1),"Film rendered as opaque paint");
                Assert.That(first.renderQueue,Is.GreaterThanOrEqualTo(3000));
                string offset=first.HasProperty("_BaseColorMap")?"_BaseColorMap":first.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
                flow.Advance(.1f,false);var moved=first.GetTextureOffset(offset);Assert.That(moved.y,Is.LessThan(0),"Water moves uphill or not at all");
                flow.Advance(.1f,true);Assert.That(first.GetTextureOffset(offset),Is.EqualTo(moved),"Paused water still advances");
                flow.Advance(float.NaN,false);Assert.That(first.GetTextureOffset(offset),Is.EqualTo(moved));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void GlassTubeRetainsPhysicalRoofAndFloor()
        {
            var config=new LayoutConfig{CellSize=36,Continuous=true,Procedural=LiminalGeneration.AquaticPark,CurveSegments=16};var room=new Room{Exits=15};
            AquaticParkPlanner.DescribeRecipe(room,config,0,20,0);var geometry=AquaticParkArchitecture.Build(room,config);
            var root=new GameObject("Native clear tube collision");root.transform.position=new Vector3(26000,2200,26000);
            PoolroomsProfile profile=null;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var owner=root.AddComponent<PoolroomsChunk>();
                var collider=MeshFactory.CreateCollider("Glass tube collision",geometry.CollisionOnly,root.transform,owner);Assert.That(collider,Is.Not.Null);
                var visual=geometry.VisualMeshes.First(v=>v.Theme==SurfaceTheme.SlideGlass&&v.TriangleCount>0);
                var go=MeshFactory.Create("Clear tube",visual,root.transform,profile,owner,false);var renderer=go.GetComponent<MeshRenderer>();
                Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off),"Clear tube blocks sunlight as opaque concrete");
                Assert.That(renderer.sharedMaterials.All(m=>m!=null&&m.renderQueue>=3000),Is.True);
                var point=room.Plan.Aquatic.Slides[0].Points[30];Physics.SyncTransforms();
                Assert.That(Physics.Raycast(root.transform.position+new Vector3(point.X,point.Y+4,point.Z),Vector3.down,out var hit,6),Is.True,"Transparent tube has no physical shell");
                Assert.That(hit.collider,Is.EqualTo(collider));Assert.That(hit.point.y-root.transform.position.y,Is.GreaterThan(point.Y+.5f));
            }
            finally{Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
