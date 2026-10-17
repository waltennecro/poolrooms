#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class City11WorldTests
    {
        [Test]
        public void NativeCc0ModelsImportWithMappedMaterialsAndVehicleLods()
        {
            var root=new GameObject("City11 native source import");
            try
            {
                foreach(string id in new[]{"WaterBottle","BoomBox","Lantern","ToyCar"})
                {
                    var model=City11Sources.Create(id,root.transform,Vector3.zero,1,out var size);
                    Assert.That(model,Is.Not.Null,id);Assert.That(size.magnitude,Is.GreaterThan(.1f));
                    var renderers=model.GetComponentsInChildren<MeshRenderer>();Assert.That(renderers.Length,Is.GreaterThan(0));
                    foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials)
                    {Assert.That(material,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);Assert.That(material.shader.name,Is.Not.EqualTo("Hidden/InternalErrorShader"));}
                    if(id=="ToyCar")Assert.That(model.GetComponent<LODGroup>().GetLODs().Length,Is.EqualTo(3));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator CityDoorStairsAndGlassKeepNativeSupport()
        {
            var c=new LayoutConfig{CellSize=36,Procedural=LiminalGeneration.City11,CurveSegments=12};
            var room=new Room{X=3,Z=8,Exits=15};City11Planner.Describe(room,c);var data=Architecture.Build(room,c);
            var root=new GameObject("City11 physical traversal");root.transform.position=new Vector3(18000,1000,18000);
            var actor=new GameObject("City11 walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("City floors/stairs",data.Structure,root.transform,owner);MeshFactory.CreateCollider("City glazing",data.CollisionOnly,root.transform,owner);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.34f;body.minMoveDistance=0;
                var building=room.Plan.City.Buildings[3]; // north plot faces its entrance towards the street
                actor.transform.position=root.transform.position+new Vector3(building.X, .16f,building.Z-building.Depth*.5f-1);
                Physics.SyncTransforms();
                for(int i=0;i<23;i++){body.Move(new Vector3(0,-.025f,.1f));yield return null;}
                Assert.That(actor.transform.position.z-root.transform.position.z,Is.GreaterThan(building.Z-building.Depth*.5f+.9f),"Shop entrance is blocked");
                float sx=building.X-building.Width*.5f+2.2f;
                body.enabled=false;actor.transform.position=root.transform.position+new Vector3(sx-.95f,.16f,building.Z-1.65f);body.enabled=true;
                for(int i=0;i<35;i++){body.Move(new Vector3(0,-.025f,.1f));yield return null;}
                for(int i=0;i<19;i++){body.Move(new Vector3(.1f,-.025f,0));yield return null;}
                for(int i=0;i<39;i++){body.Move(new Vector3(0,-.025f,-.1f));yield return null;}
                Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan(3.1f),"The upper floor is not physically reachable");
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
    }
}
#endif
