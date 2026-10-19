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
        public IEnumerator StaticObjDoesNotRequireHumanoidAnatomyOrBones()
        {
            const string selection="Poolrooms.Avatar.Selection",animation="Poolrooms.Avatar.Animation";
            bool hadSelection=PlayerPrefs.HasKey(selection),hadAnimation=PlayerPrefs.HasKey(animation);
            string oldSelection=PlayerPrefs.GetString(selection,""),oldAnimation=PlayerPrefs.GetString(animation,"");
            GameObject actor=null;string path=Path.Combine(Application.temporaryCachePath,"static-skin-"+Guid.NewGuid().ToString("N")+".obj");
            var locked=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                PlayerPrefs.DeleteKey(selection);PlayerPrefs.DeleteKey(animation);
                // An intentionally non-humanoid box: static import must not invoke rig fitting.
                File.WriteAllText(path,"v -.3 0 -.2\nv .3 0 -.2\nv .3 1.8 -.2\nv -.3 1.8 -.2\nv -.3 0 .2\nv .3 0 .2\nv .3 1.8 .2\nv -.3 1.8 .2\nf 1 4 3 2\nf 5 6 7 8\nf 1 2 6 5\nf 4 8 7 3\nf 1 5 8 4\nf 2 3 7 6\n");
                actor=new GameObject("Static OBJ regression");actor.SetActive(false);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.enabled=false;
                var avatar=actor.AddComponent<PoolroomsAvatar>();actor.SetActive(true);
                avatar.Preferences.objStatic=true;avatar.LoadFile(path);
                float deadline=Time.realtimeSinceStartup+10;
                while(avatar.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(avatar.Loading,Is.False,"OBJ parser did not complete.");
                StringAssert.Contains("OBJ estático",avatar.Status);
                Assert.That(actor.GetComponentInChildren<PoolroomsHumanoidSkin>(),Is.Null);
                Assert.That(avatar.TryBackFrame(out var back,out var up),Is.False,"Static skins must use the body attachment fallback.");
                var mesh=actor.GetComponentInChildren<MeshFilter>();Assert.That(mesh,Is.Not.Null);
                Assert.That(mesh.sharedMesh.triangles.Length,Is.EqualTo(36));
                Assert.That(PlayerPrefs.GetString(selection),Is.EqualTo("file:"+path));
            }
            finally
            {
                if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);if(File.Exists(path))File.Delete(path);
                if(hadSelection)PlayerPrefs.SetString(selection,oldSelection);else PlayerPrefs.DeleteKey(selection);
                if(hadAnimation)PlayerPrefs.SetString(animation,oldAnimation);else PlayerPrefs.DeleteKey(animation);PlayerPrefs.Save();
                Cursor.lockState=locked;Cursor.visible=visible;
            }
        }
    }
}
#endif
