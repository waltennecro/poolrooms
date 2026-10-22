#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class StaticObjSkinTests
    {
        [UnityTest]
        public IEnumerator UnreadableHumanoidMeshRemainsVisibleAsStaticFallback()
        {
            const string selection="Poolrooms.Avatar.Selection",animation="Poolrooms.Avatar.Animation";
            bool hadSelection=PlayerPrefs.HasKey(selection),hadAnimation=PlayerPrefs.HasKey(animation);
            string oldSelection=PlayerPrefs.GetString(selection,""),oldAnimation=PlayerPrefs.GetString(animation,"");
            GameObject actor=null,source=null;Mesh mesh=null;PoolroomsAvatarDefinition definition=null;
            var locked=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                PlayerPrefs.DeleteKey(selection);PlayerPrefs.DeleteKey(animation);
                source=GameObject.CreatePrimitive(PrimitiveType.Cube);source.SetActive(false);
                mesh=UnityEngine.Object.Instantiate(source.GetComponent<MeshFilter>().sharedMesh);mesh.UploadMeshData(true);source.GetComponent<MeshFilter>().sharedMesh=mesh;
                definition=ScriptableObject.CreateInstance<PoolroomsAvatarDefinition>();definition.name="Unreadable fallback regression";definition.model=source;definition.sharedHumanoid=true;
                actor=new GameObject("Transactional skin fallback");actor.SetActive(false);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.enabled=false;
                var avatar=actor.AddComponent<PoolroomsAvatar>();actor.SetActive(true);
                Assert.That(avatar.Apply(definition),Is.True,"Rig fitting must not discard renderable geometry.");
                yield return null;
                Assert.That(avatar.HasModel,Is.True);StringAssert.Contains("estático",avatar.Status);
                Assert.That(actor.GetComponentInChildren<PoolroomsHumanoidSkin>(),Is.Null,"Failed rig draft survived as the visible skin.");
                avatar.SetVisible(true,false);
                Assert.That(actor.GetComponentInChildren<MeshRenderer>().bounds.size.y,Is.EqualTo(1.75f).Within(.01f));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);if(source!=null)UnityEngine.Object.DestroyImmediate(source);
                if(mesh!=null)UnityEngine.Object.DestroyImmediate(mesh);if(definition!=null)UnityEngine.Object.DestroyImmediate(definition);
                if(hadSelection)PlayerPrefs.SetString(selection,oldSelection);else PlayerPrefs.DeleteKey(selection);
                if(hadAnimation)PlayerPrefs.SetString(animation,oldAnimation);else PlayerPrefs.DeleteKey(animation);PlayerPrefs.Save();
                Cursor.lockState=locked;Cursor.visible=visible;
            }
        }
        [UnityTest]
        public IEnumerator StaticObjDoesNotRequireHumanoidAnatomyOrBones()
        {
            const string selection="Poolrooms.Avatar.Selection",animation="Poolrooms.Avatar.Animation";
            bool hadSelection=PlayerPrefs.HasKey(selection),hadAnimation=PlayerPrefs.HasKey(animation);
            string oldSelection=PlayerPrefs.GetString(selection,""),oldAnimation=PlayerPrefs.GetString(animation,"");
            GameObject actor=null;string path=Path.Combine(Application.temporaryCachePath,"static-skin-"+Guid.NewGuid().ToString("N")+".obj");
            var locked=Cursor.lockState;bool visible=Cursor.visible;float previousTimeScale=Time.timeScale;
            try
            {
                PlayerPrefs.DeleteKey(selection);PlayerPrefs.DeleteKey(animation);
                // An intentionally non-humanoid box: static import must not invoke rig fitting.
                File.WriteAllText(path,"v -.3 0 -.2\nv .3 0 -.2\nv .3 1.8 -.2\nv -.3 1.8 -.2\nv -.3 0 .2\nv .3 0 .2\nv .3 1.8 .2\nv -.3 1.8 .2\nf 1 4 3 2\nf 5 6 7 8\nf 1 2 6 5\nf 4 8 7 3\nf 1 5 8 4\nf 2 3 7 6\n");
                actor=new GameObject("Static OBJ regression");actor.SetActive(false);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.enabled=false;
                var avatar=actor.AddComponent<PoolroomsAvatar>();actor.SetActive(true);
                avatar.Preferences.objStatic=true;Time.timeScale=0;avatar.LoadFile(path);
                float deadline=Time.realtimeSinceStartup+10;
                while(avatar.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(avatar.Loading,Is.False,"OBJ parser did not complete.");
                StringAssert.Contains("OBJ estático",avatar.Status);
                Assert.That(actor.GetComponentInChildren<PoolroomsHumanoidSkin>(),Is.Null);
                Assert.That(avatar.TryBackFrame(out var back,out var up),Is.False,"Static skins must use the body attachment fallback.");
                var mesh=actor.GetComponentInChildren<MeshFilter>();Assert.That(mesh,Is.Not.Null);
                Assert.That(mesh.sharedMesh.triangles.Length,Is.EqualTo(36));
                Assert.That(PlayerPrefs.GetString(selection),Is.EqualTo("file:"+path));
                CollectionAssert.Contains(avatar.ObjFiles(),path,"A successful external import must remain selectable in the skin library.");
                avatar.SetVisible(true,false);
                var renderer=actor.GetComponentInChildren<MeshRenderer>();
                Assert.That(renderer.bounds.size.y,Is.EqualTo(1.75f).Within(.01f),"Inactive import normalization must use mesh bounds.");
                avatar.LoadFile(path+".missing");
                Assert.That(avatar.HasModel,Is.True,"A failed replacement must preserve the loaded model.");
            }
            finally
            {
                if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);if(File.Exists(path))File.Delete(path);
                if(hadSelection)PlayerPrefs.SetString(selection,oldSelection);else PlayerPrefs.DeleteKey(selection);
                if(hadAnimation)PlayerPrefs.SetString(animation,oldAnimation);else PlayerPrefs.DeleteKey(animation);PlayerPrefs.Save();
                Time.timeScale=previousTimeScale;Cursor.lockState=locked;Cursor.visible=visible;
            }
        }
    }
}
#endif
