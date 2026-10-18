#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class CameraPerspectiveTests
    {
        static PoolroomsViewRig Create(out GameObject root,out PoolroomsExplorer explorer)
        {
            root=new GameObject("Perspective regression");root.SetActive(false);root.AddComponent<CharacterController>();
            explorer=root.AddComponent<PoolroomsExplorer>();explorer.enabled=false;explorer.showStartMenu=false;
            var camera=new GameObject("View");camera.transform.SetParent(root.transform,false);camera.transform.localPosition=Vector3.up*1.65f;
            camera.AddComponent<Camera>();explorer.eyes=camera.transform;var rig=camera.AddComponent<PoolroomsViewRig>();
            root.SetActive(true);rig.Bind(explorer);
            typeof(PoolroomsExplorer).GetProperty("Views").SetValue(explorer,rig);
            typeof(PoolroomsExplorer).GetField("ready",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(explorer,true);
            return rig;
        }
        [Test]
        public void WallContractsCameraWithoutMovingInteractionAim()
        {
            GameObject root=null,wall=null;
            try
            {
                var rig=Create(out root,out var explorer);Vector3 head=rig.Aim.position;
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,1.7f,-1.5f);wall.transform.localScale=new Vector3(4,4,.3f);Physics.SyncTransforms();
                rig.SetPerspective(CameraPerspective.FarThirdPerson);rig.ComposeView();
                Assert.That(rig.transform.position.z,Is.GreaterThan(-1.25f),"Camera must remain on the near side of the wall.");
                Assert.That(rig.Aim.position,Is.EqualTo(head));Assert.That(explorer.Aim.position,Is.EqualTo(head));
                Assert.That(explorer.transform.position,Is.EqualTo(Vector3.zero));
                foreach(var contact in Physics.OverlapSphere(rig.transform.position,CameraPerspectives.CollisionRadius,~0,QueryTriggerInteraction.Ignore))
                    Assert.That(contact.transform.IsChildOf(root.transform),Is.True,"Camera sphere overlaps world geometry.");
            }
            finally{if(wall!=null)Object.DestroyImmediate(wall);if(root!=null)Object.DestroyImmediate(root);}
        }
        [Test]
        public void PhotoExitAndDisableRestoreOwnedPauseState()
        {
            GameObject root=null;float before=Time.timeScale;bool audio=AudioListener.pause;var cursor=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                var rig=Create(out root,out var explorer);Time.timeScale=.5f;AudioListener.pause=true;
                Assert.That(rig.EnterPhoto(),Is.True);Assert.That(explorer.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
                rig.ExitPhoto();Assert.That(explorer.IsPaused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(.5f));Assert.That(AudioListener.pause,Is.True);
                Assert.That(rig.EnterPhoto(),Is.True);rig.enabled=false;
                Assert.That(explorer.IsPaused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(.5f));Assert.That(AudioListener.pause,Is.True);
            }
            finally{if(root!=null)Object.DestroyImmediate(root);Time.timeScale=before;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;}
        }
        [Test]
        public void AppearanceFitsHeightWithoutChangingPlayerCollider()
        {
            GameObject root=null,source=null;PoolroomsAvatarDefinition definition=null;
            bool saved=PlayerPrefs.HasKey("Poolrooms.Avatar.Selection");string selection=PlayerPrefs.GetString("Poolrooms.Avatar.Selection","");
            try
            {
                Create(out root,out var explorer);var controller=root.GetComponent<CharacterController>();float height=controller.height,radius=controller.radius;
                var avatar=root.AddComponent<PoolroomsAvatar>();source=GameObject.CreatePrimitive(PrimitiveType.Cube);source.transform.localScale=new Vector3(2,4,1);
                definition=ScriptableObject.CreateInstance<PoolroomsAvatarDefinition>();definition.name="Test model";definition.model=source;definition.height=1.75f;
                Assert.That(avatar.Apply(definition),Is.True);avatar.SetVisible(true,false);
                var owned=(GameObject)typeof(PoolroomsAvatar).GetField("modelRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(avatar);
                var renderer=owned.GetComponentInChildren<Renderer>();Assert.That(renderer.bounds.size.y,Is.EqualTo(1.75f).Within(.001f));Assert.That(renderer.bounds.min.y,Is.EqualTo(0).Within(.001f));
                foreach(var collider in owned.GetComponentsInChildren<Collider>())Assert.That(collider.enabled,Is.False);
                Assert.That(controller.height,Is.EqualTo(height));Assert.That(controller.radius,Is.EqualTo(radius));
                avatar.SetVisible(false,false);Assert.That(renderer.shadowCastingMode,Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly));
                definition.model=null;Assert.That(avatar.Apply(definition),Is.False);Assert.That(owned.activeSelf,Is.True,"Invalid import must retain the current appearance.");
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(source!=null)Object.DestroyImmediate(source);if(definition!=null)Object.DestroyImmediate(definition);
                if(saved)PlayerPrefs.SetString("Poolrooms.Avatar.Selection",selection);else PlayerPrefs.DeleteKey("Poolrooms.Avatar.Selection");PlayerPrefs.Save();
            }
        }
    }
}
#endif
