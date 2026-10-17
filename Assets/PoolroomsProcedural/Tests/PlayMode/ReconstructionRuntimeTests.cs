#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ReconstructionRuntimeTests
    {
        [UnityTest]
        public IEnumerator ReleasedItemFallsAndSettlesOnNativeCollider()
        {
            var root=new GameObject("Reconstruction pickup physics test");root.transform.position=new Vector3(22000,1000,22000);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform,false);floor.transform.localPosition=new Vector3(0,-.25f,0);floor.transform.localScale=new Vector3(8,.5f,8);
            var item=new GameObject("Dropped object");item.transform.SetParent(root.transform,false);item.AddComponent<BoxCollider>().size=Vector3.one*.4f;
            var pickup=item.AddComponent<PoolroomsPickup>();
            try
            {
                Physics.SyncTransforms();pickup.Release(root.transform,root.transform.position+Vector3.up*2);
                var body=item.GetComponent<Rigidbody>();
                Assert.That(body.collisionDetectionMode,Is.EqualTo(CollisionDetectionMode.ContinuousDynamic));
                yield return new WaitForSeconds(.3f);
                Assert.That(item.transform.localPosition.y,Is.LessThan(1.9f),"Object is suspended instead of falling");
                yield return new WaitForSeconds(3);
                Assert.That(item.transform.localPosition.y,Is.InRange(.18f,.24f),"Item crossed the floor or failed to settle");
                Assert.That(body.linearVelocity.magnitude,Is.LessThan(.15f));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void DashDisplacementRemainsBlockedByNativeWall()
        {
            var root=new GameObject("Dash wall test");root.transform.position=new Vector3(24000,1000,24000);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform,false);wall.transform.localPosition=new Vector3(0,1,1.5f);wall.transform.localScale=new Vector3(4,3,.25f);
            var actor=new GameObject("Dash capsule");actor.transform.SetParent(root.transform,false);var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.minMoveDistance=0;
            try
            {
                Physics.SyncTransforms();var dash=new ExplorerDash();
                for(int i=0;i<12;i++){var velocity=dash.Step(1f/30,i==0,true,new V3(),new V3(0,0,1));body.Move(new Vector3(velocity.X,0,velocity.Z)/30);}
                Assert.That(actor.transform.localPosition.z,Is.LessThan(1.2f),"Dash bypassed the CharacterController wall");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
