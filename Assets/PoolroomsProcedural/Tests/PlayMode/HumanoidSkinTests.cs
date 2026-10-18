#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class HumanoidSkinTests
    {
        static Mesh Geometry()
        {
            var rig=new HumanoidRig(1.75f,HumanoidRestPose.T);var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<rig.Rest.Length;i++)
            {
                var p=rig.Rest[i];int start=vertices.Count;Vector3 centre=new Vector3(p.X,p.Y,p.Z);
                for(int corner=0;corner<8;corner++)vertices.Add(centre+new Vector3((corner&1)==0?-.025f:.025f,(corner&2)==0?-.025f:.025f,(corner&4)==0?-.025f:.025f));
                foreach(int index in new[]{0,1,2,2,1,3,4,6,5,5,6,7})triangles.Add(start+index);
            }
            var mesh=new Mesh{name="Humanoid test source"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        [Test]
        public void AutomaticSkinPreservesBindGeometryAndMaterialsAcrossMirroredTransforms()
        {
            var root=new GameObject("Shared skeleton test");Mesh source=null,baked=null;
            try
            {
                source=Geometry();var child=new GameObject("Source mesh");child.transform.SetParent(root.transform,false);
                child.transform.localScale=new Vector3(-1,1,1);child.AddComponent<MeshFilter>().sharedMesh=source;var original=child.AddComponent<MeshRenderer>();
                var expected=source.vertices;for(int i=0;i<expected.Length;i++)expected[i]=child.transform.TransformPoint(expected[i]);
                var skin=root.AddComponent<PoolroomsHumanoidSkin>();skin.Build(new SkinPreferences{restPose=HumanoidRestPose.T},1.75f);
                var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();Assert.That(renderer,Is.Not.Null);Assert.That(original.enabled,Is.False);
                baked=new Mesh();renderer.BakeMesh(baked);var actual=baked.vertices;Assert.That(actual.Length,Is.EqualTo(expected.Length));
                for(int i=0;i<actual.Length;i++)Assert.That(Vector3.Distance(renderer.transform.TransformPoint(actual[i]),expected[i]),Is.LessThan(.0002f),"Bind pose must preserve mirrored source geometry.");
                Assert.That(renderer.sharedMesh.uv,Is.EqualTo(source.uv));Assert.That(renderer.sharedMesh.normals,Is.EqualTo(source.normals));
                Assert.That(renderer.sharedMesh.bindposes.Length,Is.EqualTo(HumanoidRig.Count));Assert.That(renderer.sharedMesh.boneWeights.Length,Is.EqualTo(source.vertexCount));
                foreach(var weight in renderer.sharedMesh.boneWeights)Assert.That(weight.weight0+weight.weight1+weight.weight2+weight.weight3,Is.EqualTo(1).Within(.00001f));
                skin.Motion.Play(HumanoidEmote.Wave,true);for(int i=0;i<60;i++)skin.Motion.Step(skin.Rig,new HumanoidMotion{Grounded=true},1f/60);
                skin.ApplyPose();renderer.BakeMesh(baked);actual=baked.vertices;float moved=0;for(int i=0;i<actual.Length;i++)moved+=Vector3.Distance(renderer.transform.TransformPoint(actual[i]),expected[i]);Assert.That(moved,Is.GreaterThan(.1f),"Shared pose must actually deform the skin.");
                Assert.That(source.boneWeights.Length,Is.Zero,"Original imported mesh must remain unchanged.");
            }
            finally{Object.DestroyImmediate(root);if(source!=null)Object.DestroyImmediate(source);if(baked!=null)Object.DestroyImmediate(baked);}
        }
        [Test]
        public void EmptySkinDoesNotAddPlaceholderOrChangeController()
        {
            var root=new GameObject("Skin default test");root.SetActive(false);var explorer=root.AddComponent<PoolroomsExplorer>();explorer.enabled=false;explorer.showStartMenu=false;
            var avatar=root.AddComponent<PoolroomsAvatar>();
            try{Assert.That(root.GetComponentsInChildren<Renderer>(true).Length,Is.Zero);Assert.That(avatar.Apply(null),Is.False);Assert.That(root.GetComponent<CharacterController>(),Is.Not.Null);}
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
