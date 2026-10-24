#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class Level10WorldTests
    {
        [Test]
        public void NativeSidingDisplacementKeepsItsSixteenBitTextureFormat()
        {
            var height=Resources.Load<Texture2D>("Level10/Textures/Siding_Height");
            Assert.That(height,Is.Not.Null);Assert.That(height.width,Is.EqualTo(4096));Assert.That(height.height,Is.EqualTo(2048));
            Assert.That(height.format,Is.EqualTo(TextureFormat.R16),"Native displacement was compressed down to eight bits.");
        }
        [UnityTest]
        public IEnumerator RuralTracksHaveNativeCapsuleSupportWithoutCropColliders()
        {
            GameObject root=null;
            try
            {
                root=new GameObject("Level10 native rural tracks");root.transform.position=Vector3.up*17000;
                var owner=root.AddComponent<PoolroomsChunk>();var config=new LayoutConfig{CellSize=36,Continuous=true,Seed=10,Procedural=LiminalGeneration.WheatFields10,EncounterExploration=true};
                var world=new Layout(config);var room=world.At(0,0);var data=Architecture.Build(room,world);
                MeshFactory.CreateCollider("Rural ground",data.Structure,root.transform,owner);MeshFactory.CreateCollider("Rural architecture",data.CollisionOnly,root.transform,owner);
                Physics.SyncTransforms();yield return null;
                foreach(var path in room.Plan.Level10.Routes)foreach(var p in path.Points)
                {
                    var foot=root.transform.position+new Vector3(p.X,0,p.Y);
                    Assert.That(Physics.Raycast(foot+Vector3.up*.3f,Vector3.down,out var hit,.6f),Is.True);
                    Assert.That(Mathf.Abs(hit.point.y-foot.y),Is.LessThan(.03f));
                    Assert.That(Physics.CheckCapsule(foot+Vector3.up*.35f,foot+Vector3.up*1.5f,.27f,~0,QueryTriggerInteraction.Ignore),Is.False,"A rural track is obstructed.");
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator BarnStaircaseLetsARealControllerReachItsLoft()
        {
            GameObject root=null,player=null;
            try
            {
                var config=new LayoutConfig{CellSize=36,Continuous=true,Procedural=LiminalGeneration.WheatFields10};Level10Plan plan=null;Room room=null;Layout world=null;
                for(int seed=0;seed<256;seed++)
                {config.Seed=seed;world=new Layout(config);room=world.At(0,0);if(room.Plan.Level10.Loft){plan=room.Plan.Level10;break;}}
                Assert.That(plan,Is.Not.Null);var data=Architecture.Build(room,world);
                root=new GameObject("Level10 native barn stairs");root.transform.position=Vector3.up*17500;var owner=root.AddComponent<PoolroomsChunk>();
                MeshFactory.CreateCollider("Ground",data.Structure,root.transform,owner);MeshFactory.CreateCollider("Barn",data.CollisionOnly,root.transform,owner);
                float x=plan.Farm.MinX+.85f,start=plan.Farm.MinZ+.55f,run=Mathf.Min(4.8f,plan.Farm.Depth-1.4f);
                player=new GameObject("Native walking capsule");player.transform.position=root.transform.position+new Vector3(x,.03f,start-.45f);
                var body=player.AddComponent<CharacterController>();body.height=1.8f;body.center=Vector3.up*.9f;body.radius=.27f;body.stepOffset=.24f;body.skinWidth=.015f;
                Physics.SyncTransforms();yield return null;
                for(int i=0;i<180;i++)body.Move(new Vector3(0,-.03f,run/170));
                Assert.That(player.transform.position.y-root.transform.position.y,Is.GreaterThan(plan.LoftY-.2f));
                Assert.That(player.transform.position.z-root.transform.position.z,Is.GreaterThan(start+run-.15f));
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(player!=null)Object.DestroyImmediate(player);if(root!=null)Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator FieldKeepsakesCanBeSpawnedAsOwnedPhysicalInventoryObjects()
        {
            PoolroomsProfile profile=null;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                for(int id=0;id<3;id++)
                {
                    var pickup=PoolroomsLevel10.CreateItem(id,"level10-native-"+id,new Vector3(0,18000+id,0),profile);
                    Assert.That(pickup,Is.Not.Null);
                    try
                    {
                        Assert.That(pickup.InventoryKey,Is.EqualTo("level10:"+id));
                        Assert.That(pickup.GetComponent<PoolroomsChunk>(),Is.Not.Null);Assert.That(pickup.GetComponent<Collider>(),Is.Not.Null);
                        Assert.That(pickup.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount,Is.GreaterThan(0));yield return null;
                    }
                    finally{Object.DestroyImmediate(pickup.gameObject);}
                }
                Assert.That(PoolroomsLevel10.CreateItem(3,"invalid",Vector3.zero,profile),Is.Null);LogAssert.NoUnexpectedReceived();
            }
            finally{if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
