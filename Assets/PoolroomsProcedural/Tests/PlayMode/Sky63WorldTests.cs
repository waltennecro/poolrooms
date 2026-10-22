#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
namespace Poolrooms.Tests
{
    public sealed class Sky63WorldTests
    {
        [UnityTest]
        public IEnumerator SkyDeckHasRealVoidGuardsAndWalkableStairs()
        {
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.CloudTerminal,RebuildSky63=true,CellSize=28,DoorWidth=6,DoorHeight=5.5f,EncounterExploration=true};
                var room=new Layout(c).At(0,0);var p=room.Plan.Quiet.Sky63;var g=Sky63Architecture.Build(room,c);
                root=new GameObject("Sky63 native traversal regression");root.transform.position=Vector3.up*14000;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var owner=root.AddComponent<PoolroomsChunk>();
                var body=MeshFactory.Create("Suspended timber paths",g.Structure,root.transform,profile,owner,true).GetComponent<MeshCollider>();
                MeshFactory.CreateCollider("Sky guard rails",g.CollisionOnly,root.transform,owner);Physics.SyncTransforms();yield return null;
                float start=p.Stair.MinZ+Sky63Plan.Landing,tread=Sky63Plan.Flight/p.Steps;
                for(int flight=0;flight<2;flight++)for(int step=0;step<p.Steps;step++)
                {
                    float expected=flight*p.Upper*.5f+(step+1)*p.Upper/(p.Steps*2);
                    var at=new Vector3(p.Stair.X+(flight==0?-.9f:.9f),expected+.15f,flight==0?start+(step+.5f)*tread:start+Sky63Plan.Flight-(step+.5f)*tread);
                    Assert.That(body.Raycast(new Ray(root.transform.TransformPoint(at),Vector3.down),out var hit,.3f),Is.True);
                    Assert.That(hit.point.y-root.transform.position.y,Is.EqualTo(expected).Within(.02f));
                    Assert.That(body.Raycast(new Ray(root.transform.TransformPoint(at+Vector3.up*.1f),Vector3.up),out _,1.8f),Is.False);
                }
                var corner=root.transform.TransformPoint(new Vector3(-p.Half+.3f,1,-p.Half+.3f));
                Assert.That(body.Raycast(new Ray(corner,Vector3.down),out _,6),Is.False,"A square floor fills the sky void.");
                var edge=root.transform.TransformPoint(new Vector3(p.Clear-.5f,.9f,3));
                Assert.That(Physics.Raycast(edge,Vector3.right,1,~0,QueryTriggerInteraction.Ignore),Is.True,"The suspended ground path has no safety rail.");
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void OriginalCloudDensityIsPresentAndHasRealVariation()
        {
            var data=Resources.Load<TextAsset>("Sky63/CloudDensity");Assert.That(data,Is.Not.Null);
            var bytes=data.bytes;Assert.That(bytes.Length,Is.EqualTo(64*64*64));int empty=0,dense=0;
            foreach(byte b in bytes){if(b==0)empty++;if(b>180)dense++;}
            Assert.That(empty,Is.GreaterThan(100));Assert.That(dense,Is.GreaterThan(100));
        }
    }
}
#endif
