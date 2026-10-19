#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class RestfulLevelWorldTests
    {
        [UnityTest]
        public IEnumerator PlayerCrossesEveryNewLevelThresholdWithOrdinaryController()
        {
            for(int level=0;level<RestfulLevelPlanner.LevelCount;level++)
            {
                var c=new LayoutConfig{CellSize=28,DoorWidth=6,DoorHeight=5.1f,CurveSegments=12,ObjectAbundance=3};
                var a=new Room{Environment=(LiminalEnvironment)(18+level),Exits=15};var b=new Room{Environment=(LiminalEnvironment)(18+(level+1)%10),Exits=15};
                RestfulLevelPlanner.DescribeRecipe(a,c,131071);RestfulLevelPlanner.DescribeRecipe(b,c,0);
                var root=new GameObject("Restful walking regression");root.transform.position=new Vector3(40000,1800,40000);var actor=new GameObject("Walker");
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();
                    MeshFactory.CreateCollider("First level",Architecture.Build(a,c).Structure,root.transform,owner);
                    MeshFactory.CreateCollider("Shared threshold",Corridors.Build(a,b,0,c).Structure,root.transform,owner);
                    var next=new GameObject("Next level");next.transform.SetParent(root.transform,false);next.transform.localPosition=Vector3.forward*c.CellSize;
                    MeshFactory.CreateCollider("Second level",Architecture.Build(b,c).Structure,next.transform,owner);
                    actor.transform.position=root.transform.position+Vector3.forward*(c.CellSize*.5f-1)+Vector3.up*.04f;
                    var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.3f;body.skinWidth=.025f;Physics.SyncTransforms();
                    for(int step=0;step<60;step++){body.Move(Vector3.forward*.05f-Vector3.up*.02f);if(step%15==0)yield return null;}
                    Assert.That(actor.transform.position.z-root.transform.position.z,Is.GreaterThan(c.CellSize*.5f+1.7f));
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.InRange(-.1f,.15f));
                }
                finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
            }
        }
        [UnityTest]
        public IEnumerator SharedPickupVisualSurvivesAnotherInstancesDestructionAndReleasesLastReference()
        {
            var p=ScriptableObject.CreateInstance<PoolroomsProfile>();var root=new GameObject("Shared carry visual regression");
            try
            {
                var a=PoolroomsPickup.CreateCatalog("cache:a",0,Vector3.zero,root.transform,p);
                var b=PoolroomsPickup.CreateCatalog("cache:b",0,Vector3.right,root.transform,p);
                var mesh=b.GetComponentInChildren<MeshFilter>().sharedMesh;
                Assert.That(a.GetComponentInChildren<MeshFilter>().sharedMesh,Is.SameAs(mesh));
                Assert.That(a.GetComponent<Collider>(),Is.Not.SameAs(b.GetComponent<Collider>()));
                Object.Destroy(a.gameObject);yield return null;
                Assert.That(mesh,Is.Not.Null);Assert.That(b.GetComponentInChildren<MeshFilter>().sharedMesh,Is.SameAs(mesh));
                Object.Destroy(b.gameObject);yield return null;yield return null;
                Assert.That(mesh==null,Is.True,"Last visual lease leaked the shared mesh");
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(p);}
        }
    }
}
#endif
