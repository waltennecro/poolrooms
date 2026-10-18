#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

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
        [UnityTest]
        public IEnumerator ObjWithZUpNormalizesRootMeshAndBuildsAnimatedSkin()
        {
            string file=Path.Combine(Application.temporaryCachePath,"Humanoid_"+Guid.NewGuid().ToString("N")+".obj");
            float previousYaw=PlayerPrefs.GetFloat("Poolrooms.Avatar.Yaw",0);bool yawSaved=PlayerPrefs.HasKey("Poolrooms.Avatar.Yaw");
            string previous=PlayerPrefs.GetString("Poolrooms.Avatar.Selection","");bool saved=PlayerPrefs.HasKey("Poolrooms.Avatar.Selection");
            GameObject root=null;Mesh source=null,baked=null;
            try
            {
                source=Geometry();var text=new StringBuilder();foreach(var vertex in source.vertices)text.AppendLine(FormattableString.Invariant($"v {vertex.x:R} {vertex.z:R} {vertex.y:R}"));
                var indices=source.triangles;for(int i=0;i<indices.Length;i+=3)text.AppendLine($"f {indices[i]+1} {indices[i+1]+1} {indices[i+2]+1}");File.WriteAllText(file,text.ToString());
                root=new GameObject("OBJ import regression");root.SetActive(false);var explorer=root.AddComponent<PoolroomsExplorer>();explorer.enabled=false;explorer.showStartMenu=false;
                var avatar=root.AddComponent<PoolroomsAvatar>();root.SetActive(true);avatar.Preferences.zUp=true;avatar.Preferences.restPose=HumanoidRestPose.T;avatar.SetYaw(0);avatar.LoadObj(file);
                float deadline=Time.realtimeSinceStartup+20;while(avatar.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(avatar.Loading,Is.False,"OBJ import timed out");Assert.That(avatar.Status,Does.Not.Contain("No se pudo"));
                var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();Assert.That(renderer,Is.Not.Null,avatar.Status);baked=new Mesh();renderer.BakeMesh(baked);
                float min=float.MaxValue,max=float.MinValue;foreach(var v in baked.vertices){float y=renderer.transform.TransformPoint(v).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
                Assert.That(min,Is.EqualTo(0).Within(.001f));Assert.That(max-min,Is.EqualTo(1.75f).Within(.001f));
                Assert.That(root.GetComponent<CharacterController>().height,Is.GreaterThan(1.5f));
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(source!=null)Object.DestroyImmediate(source);if(baked!=null)Object.DestroyImmediate(baked);if(File.Exists(file))File.Delete(file);
                if(yawSaved)PlayerPrefs.SetFloat("Poolrooms.Avatar.Yaw",previousYaw);else PlayerPrefs.DeleteKey("Poolrooms.Avatar.Yaw");
                if(saved)PlayerPrefs.SetString("Poolrooms.Avatar.Selection",previous);else PlayerPrefs.DeleteKey("Poolrooms.Avatar.Selection");PlayerPrefs.Save();
            }
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
