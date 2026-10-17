#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class CourtyardGalleryAccessTests
    {
        [UnityTest]
        public IEnumerator WalkerCanEnterGroundGalleryWhileUpperEdgesStayProtected()
        {
            var c=new LayoutConfig{CellSize=36,Continuous=true,Procedural=LiminalGeneration.Courtyard188};var r=new Layout(c).At(0,0);var g=Architecture.Build(r,c);
            var root=new GameObject("Courtyard real gallery access");root.transform.position=new Vector3(18000,1000,18000);var actor=new GameObject("Courtyard walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Court floors/walls",g.Structure,root.transform,owner);MeshFactory.CreateCollider("Upper safety rails",g.CollisionOnly,root.transform,owner);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.34f;body.minMoveDistance=0;
                float inner=9,z=-18+11.3f;actor.transform.position=root.transform.position+new Vector3(inner-1.5f,.04f,z);Physics.SyncTransforms();
                for(int i=0;i<60;i++){body.Move(new Vector3(.1f,-.025f,0));yield return null;}
                Assert.That(actor.transform.position.x-root.transform.position.x,Is.GreaterThan(inner+3.5f),"Ground-level guard rail still seals the gallery");
                Assert.That(Physics.Raycast(root.transform.position+new Vector3(inner-1,3.8f,z),Vector3.right,2,~0,QueryTriggerInteraction.Ignore),Is.True,"Upper courtyard edge lacks its physical railing");
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
    }
}
#endif
