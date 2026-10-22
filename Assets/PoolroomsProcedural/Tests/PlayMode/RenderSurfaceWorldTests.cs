#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
namespace Poolrooms.Tests
{
    public sealed class RenderSurfaceWorldTests
    {
        [UnityTest]
        public IEnumerator OpaqueFinishClipsRenderingButKeepsOriginalCollider()
        {
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                root=new GameObject("Surface ownership native regression");root.transform.position=Vector3.up*16000;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var owner=root.AddComponent<PoolroomsChunk>();var source=new MeshData();
                foreach(var slot in new[]{Surface.Tile,Surface.Accent})source.Quad(new V3(-2,0,-2),new V3(2,0,-2),new V3(2,0,2),new V3(-2,0,2),V3.Up,slot);
                var go=MeshFactory.Create("Overlaid floor",source,root.transform,profile,owner,true);
                var render=go.GetComponent<MeshFilter>().sharedMesh;var collision=go.GetComponent<MeshCollider>();
                Assert.That(render.triangles.Length,Is.EqualTo(6));Assert.That(collision.sharedMesh.triangles.Length,Is.EqualTo(12));
                Assert.That(source.TriangleCount,Is.EqualTo(4));Assert.That(render,Is.Not.SameAs(collision.sharedMesh));
                Physics.SyncTransforms();yield return null;
                Assert.That(collision.Raycast(new Ray(root.transform.position+Vector3.up,Vector3.down),out var hit,2),Is.True);
                Assert.That(hit.point.y,Is.EqualTo(root.transform.position.y).Within(.001f));LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);}
        }
        [UnityTest]
        public IEnumerator NeighbourLoadHidesOnlySharedRenderFaceAndUnloadRestoresIt()
        {
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                root=new GameObject("Streamed wall ownership native regression");profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                var left=new GameObject("Left room");left.transform.SetParent(root.transform,false);var owner=left.AddComponent<PoolroomsChunk>();
                var right=new GameObject("Right room");right.transform.SetParent(root.transform,false);right.transform.localPosition=Vector3.right*36;var otherOwner=right.AddComponent<PoolroomsChunk>();
                var wall=new MeshData();wall.Quad(new V3(18,0,-18),new V3(18,0,18),new V3(18,8,18),new V3(18,8,-18),new V3(-1,0,0),Surface.Plaster);
                var other=RenderBoundary.Translate(wall,new V3(-36,0,0));
                var type=typeof(MeshFactory).Assembly.GetType("Poolrooms.PoolroomsSurfaceSeams",true);var manager=Activator.CreateInstance(type,true);
                var register=type.GetMethod("Register",BindingFlags.Public|BindingFlags.Instance);var remove=type.GetMethod("Remove",BindingFlags.Public|BindingFlags.Instance);
                register.Invoke(manager,new object[]{0,0,36f,new[]{wall},left.transform,profile,owner});
                var first=left.GetComponentInChildren<MeshFilter>();Assert.That(first.sharedMesh.triangles.Length,Is.EqualTo(6));
                register.Invoke(manager,new object[]{1,0,36f,new[]{other},right.transform,profile,otherOwner});yield return null;
                var second=right.GetComponentInChildren<MeshFilter>();
                Assert.That(first.sharedMesh.triangles.Length,Is.EqualTo(6),"The left room must keep its inward-facing painted wall.");
                Assert.That(second.sharedMesh.triangles.Length,Is.Zero);Assert.That(second.GetComponent<MeshRenderer>().enabled,Is.False);
                remove.Invoke(manager,new object[]{0,0});left.SetActive(false);yield return null;
                Assert.That(second.sharedMesh.triangles.Length,Is.EqualTo(6));Assert.That(second.GetComponent<MeshRenderer>().enabled,Is.True);
                Assert.That(second.GetComponent<MeshRenderer>().sharedMaterials.Length,Is.EqualTo(6));LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);}
        }
    }
}
#endif
