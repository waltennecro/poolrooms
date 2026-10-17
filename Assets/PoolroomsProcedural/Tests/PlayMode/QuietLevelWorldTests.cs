#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class QuietLevelWorldTests
    {
        [UnityTest]
        public IEnumerator ArchiveAndCourtyardStairsReachTheirUpperFloor()
        {
            foreach(var environment in new[]{LiminalEnvironment.Archive906,LiminalEnvironment.Courtyard188})
            {
                var c=new LayoutConfig{CellSize=36,CurveSegments=12};var room=new Room{X=0,Z=0,Exits=15,Environment=environment};QuietLevelPlanner.Describe(room,c);var data=Architecture.Build(room,c);
                var root=new GameObject("Quiet level native stair check");root.transform.position=new Vector3(16000,1000,16000);var actor=new GameObject("Quiet walker");
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Quiet supports",data.Structure,root.transform,owner);MeshFactory.CreateCollider("Quiet safety",data.CollisionOnly,root.transform,owner);
                    var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.34f;body.skinWidth=.025f;body.minMoveDistance=0;
                    float x=environment==LiminalEnvironment.Archive906?15:13.5f,z=environment==LiminalEnvironment.Archive906?-13.8f:-14;
                    actor.transform.position=root.transform.position+new Vector3(x-.95f,.05f,z-1.65f);Physics.SyncTransforms();
                    for(int i=0;i<35;i++){body.Move(new Vector3(0,-.025f,.1f));yield return null;}
                    for(int i=0;i<19;i++){body.Move(new Vector3(.1f,-.025f,0));yield return null;}
                    for(int i=0;i<39;i++){body.Move(new Vector3(0,-.025f,-.1f));yield return null;}
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan(3.08f),environment+": stair headroom or upper landing blocked");
                }
                finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
            }
        }
        [UnityTest]
        public IEnumerator CoastalBoardwalkSupportsWalkingOverSea()
        {
            var c=new LayoutConfig{CellSize=36,Procedural=LiminalGeneration.Coast48,CurveSegments=12};var room=new Room{X=0,Z=0,Exits=15,Environment=LiminalEnvironment.Coast48};QuietLevelPlanner.Describe(room,c);var data=Architecture.Build(room,c);
            var root=new GameObject("Coast boardwalk native check");root.transform.position=new Vector3(14000,1000,14000);var actor=new GameObject("Coast walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Beach and boardwalk",data.Structure,root.transform,owner);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.34f;body.minMoveDistance=0;
                actor.transform.position=root.transform.position+new Vector3(-16,.12f,0);Physics.SyncTransforms();
                for(int i=0;i<300;i++){body.Move(new Vector3(.1f,-.025f,0));Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan(.02f));yield return null;}
                Assert.That(actor.transform.position.x-root.transform.position.x,Is.GreaterThan(13),"Sea crossing is obstructed");
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
    }
}
#endif
