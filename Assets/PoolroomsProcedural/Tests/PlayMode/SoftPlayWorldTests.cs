#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class SoftPlayWorldTests
    {
        private static Vector3 Vec(V3 p){return new Vector3(p.X,p.Y,p.Z);}
        private static Room Hall(LayoutConfig c){var room=new Room{X=17,Z=-23,Exits=15};SoftPlayPlanner.Describe(room,c);return room;}
        private static void Colliders(GameObject root,Room room,LayoutConfig c)
        {
            var data=Architecture.Build(room,c);var owner=root.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            MeshFactory.CreateCollider("Hall and play floors",data.Structure,root.transform,owner);
            MeshFactory.CreateCollider("Net safety barriers",data.CollisionOnly,root.transform,owner);Physics.SyncTransforms();
        }
        [UnityTest]
        public IEnumerator ControllerCanClimbActualUStairsToEveryUpperFloor()
        {
            var c=new LayoutConfig{Procedural=LiminalGeneration.SoftPlay,CellSize=36,CurveSegments=12};var room=Hall(c);
            var root=new GameObject("Soft play stair test");root.transform.position=new Vector3(10000,1000,10000);
            var actor=new GameObject("Play traversal capsule");
            try
            {
                Colliders(root,room,c);var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;
                body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.34f;body.minMoveDistance=0;
                foreach(var frame in room.Plan.SoftPlay.Frames)for(int level=0;level<frame.Levels-1;level++)
                {
                    body.enabled=false;actor.transform.position=root.transform.position+new Vector3(frame.StairX-.95f,level*3.2f+.05f,frame.StairZ-1.65f);body.enabled=true;
                    for(int i=0;i<35;i++){body.Move(new Vector3(0,-.025f,.1f));yield return null;}
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan(level*3.2f+1.48f),"First flight or midlanding is blocked.");
                    for(int i=0;i<19;i++){body.Move(new Vector3(.1f,-.025f,0));yield return null;}
                    for(int i=0;i<39;i++){body.Move(new Vector3(0,-.025f,-.1f));yield return null;}
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan((level+1)*3.2f-.12f),"Second flight did not reach the actual upper floor.");
                    for(int i=0;i<23;i++){body.Move(new Vector3(.1f,-.025f,0));yield return null;}
                    Assert.That(actor.transform.position.x-root.transform.position.x,Is.GreaterThan(frame.StairX+2.5f),"Stair shaft guard blocks the landing exit.");
                }
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
        [Test]
        public void HollowTubeAndWideGroundRoutesPassNativeCapsuleSweeps()
        {
            var c=new LayoutConfig{Procedural=LiminalGeneration.SoftPlay,Continuous=true,CellSize=36,CurveSegments=12};Room room=null;
            for(int seed=0;seed<32;seed++){c.Seed=seed;room=Hall(c);if(room.Plan.SoftPlay.EnclosedBridge)break;}
            Assert.That(room.Plan.SoftPlay.EnclosedBridge,Is.True);
            var root=new GameObject("Soft play tube physics test");root.transform.position=new Vector3(10000,1000,10000);
            try
            {
                Colliders(root,room,c);var p=room.Plan.SoftPlay;
                Vector3 start=root.transform.position+Vec(p.BridgeStart)+Vector3.up*.05f,end=root.transform.position+Vec(p.BridgeEnd)+Vector3.up*.05f,delta=end-start;RaycastHit hit;
                Assert.That(Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.5f,.32f,delta.normalized,out hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore),Is.False,"Tube is capped or its entrance net is closed.");
                for(int i=0;i<=16;i++)Assert.That(Physics.Raycast(Vector3.Lerp(start,end,i/16f)+Vector3.up*.1f,Vector3.down,out hit,.3f),Is.True,"Tube floor lacks native support.");
                for(int side=0;side<4;side++)
                {
                    Vector3 direction=new Vector3(Directions.DX[side],0,Directions.DZ[side]);Vector3 feet=root.transform.position+Vector3.up*.05f;
                    Assert.That(Physics.CapsuleCast(feet+Vector3.up*.4f,feet+Vector3.up*1.5f,.32f,direction,out hit,c.CellSize*.5f+.1f,~0,QueryTriggerInteraction.Ignore),Is.False,"Play equipment or wall blocks a wide ground route.");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void ToysSurviveRoomUnloadAndEnablePhysicsOnlyWhenReleased()
        {
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var room=new GameObject("Disposable play hall");var grip=new GameObject("Carry grip");
            PoolroomsPickup item=null;
            try
            {
                item=PoolroomsPickup.CreatePlayProp("softplay:test",0,1,Vector3.zero,room.transform,profile);
                Assert.That(item.GetComponent<Rigidbody>(),Is.Null);Assert.That(item.GetComponent<SphereCollider>().radius,Is.EqualTo(.22f));
                item.Attach(grip.transform);Object.DestroyImmediate(room);room=null;
                Assert.That(item,Is.Not.Null);Assert.That(item.GetComponentInChildren<MeshFilter>().sharedMesh,Is.Not.Null);
                Assert.That(item.GetComponent<Collider>().enabled,Is.False);
                item.Release(null,Vector3.up*100);Assert.That(item.GetComponent<Rigidbody>().isKinematic,Is.False);
                Assert.That(item.GetComponent<Collider>().enabled,Is.True);
            }
            finally{if(item!=null)Object.DestroyImmediate(item.gameObject);if(room!=null)Object.DestroyImmediate(room);Object.DestroyImmediate(grip);Object.DestroyImmediate(profile);}
        }
    }
}
#endif
