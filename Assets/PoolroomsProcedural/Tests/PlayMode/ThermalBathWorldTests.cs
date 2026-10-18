#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ThermalBathWorldTests
    {
        [UnityTest]
        public IEnumerator DeepestBathHasRealBottomAndClimbableExitSteps()
        {
            var cfg=new LayoutConfig{CellSize=28,DoorWidth=6,DoorHeight=5.1f,Procedural=LiminalGeneration.ThermalBaths,CurveSegments=16};
            var room=new Room{X=0,Z=0,Exits=15};ThermalBathPlanner.DescribeRecipe(room,cfg,1875);var geometry=ThermalBathArchitecture.Build(room,cfg);
            var root=new GameObject("Thermal native supports");root.transform.position=new Vector3(22000,1000,22000);var actor=new GameObject("Thermal walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Bath floor and steps",geometry.Structure,root.transform,owner);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.34f;body.minMoveDistance=0;
                foreach(var pool in geometry.Regions)
                {
                    var at=root.transform.position+new Vector3(pool.CenterX,pool.Bottom+.05f,pool.CenterZ);
                    body.enabled=false;actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
                    for(int step=0;step<48;step++){body.Move(new Vector3(.07f,-.025f,0));if(step%8==0)yield return null;}
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan(-.08f),"Basin stairs cannot be climbed");
                    Assert.That(Physics.Raycast(root.transform.position+new Vector3(pool.CenterX,2,pool.CenterZ),Vector3.down,out var hit,4),Is.True);
                    Assert.That(hit.point.y-root.transform.position.y,Is.EqualTo(pool.Bottom).Within(.025f),"Hidden floor covers the pool");
                }
                foreach(var direction in new[]{Vector3.right,Vector3.forward})
                {
                    body.enabled=false;actor.transform.position=root.transform.position-direction*13+Vector3.up*.04f;body.enabled=true;Physics.SyncTransforms();
                    for(int i=0;i<260;i++){body.Move(direction*.1f-Vector3.up*.02f);if(i%40==0)yield return null;}
                    Assert.That(Vector3.Dot(actor.transform.position-root.transform.position,direction),Is.GreaterThan(12));
                }
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
        [Test]
        public void AuthoredLampLoadsWithoutImportingPresentationLights()
        {
            var root=new GameObject("Bath licensed lamp test");
            try
            {
                var lamp=PoolroomsPickup.CreateSource("test:thermal-lamp","T_ArtNouveauLamp",Vector3.zero,root.transform,.55f);
                Assert.That(lamp,Is.Not.Null);Assert.That(lamp.GetComponentsInChildren<Light>(true).Length,Is.Zero);
                Assert.That(lamp.GetComponentsInChildren<LODGroup>().Length,Is.EqualTo(1));
                Assert.That(lamp.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
                foreach(var renderer in lamp.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials)Assert.That(material,Is.Not.Null);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
