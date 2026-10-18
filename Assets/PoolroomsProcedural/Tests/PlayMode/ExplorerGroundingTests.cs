#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ExplorerGroundingTests
    {
        [Test]
        public void EmbeddedFeetRepairChecksHeadroomBeforeMoving()
        {
            GameObject actor=null,floor=null,ceiling=null;
            var oldLock=Cursor.lockState;bool oldVisible=Cursor.visible;
            try
            {
                actor=new GameObject("Embedded feet headroom regression");
                actor.SetActive(false);actor.transform.position=Vector3.up*-.65f;
                var body=actor.AddComponent<CharacterController>();
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;
                actor.SetActive(true); // Awake configures the real controller; no frame runs Start.
                floor=GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.transform.position=Vector3.down*.1f;floor.transform.localScale=new Vector3(5,.2f,5);
                ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.transform.position=Vector3.up*1.2f;ceiling.transform.localScale=new Vector3(5,.2f,5);
                var repair=typeof(PoolroomsExplorer).GetMethod("RepairEmbeddedFeet",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                Assert.That(repair,Is.Not.Null);
                Physics.SyncTransforms();Vector3 before=actor.transform.position;
                repair.Invoke(explorer,null);
                Assert.That(actor.transform.position,Is.EqualTo(before),"Recovery must not lift the capsule into a ceiling.");
                Object.DestroyImmediate(ceiling);ceiling=null;Physics.SyncTransforms();
                repair.Invoke(explorer,null);
                Assert.That(actor.transform.position.y,Is.GreaterThan(0),"Removing the obstruction must permit floor recovery.");
                Assert.That(body.enabled,Is.True);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                if(floor!=null)Object.DestroyImmediate(floor);
                if(ceiling!=null)Object.DestroyImmediate(ceiling);
                Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            }
        }
        [UnityTest]
        public IEnumerator DefaultCapsuleAndDisabledPreviewCollidersStillSupportPlayerAndDoubleJump()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float scale=Time.timeScale,capture=Time.captureDeltaTime;bool sync=Physics.autoSyncTransforms,audio=AudioListener.pause,visible=Cursor.visible;var locked=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;Physics.autoSyncTransforms=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.colliders=false;
                profile.columns=profile.rows=1;profile.continuousWorld=false;profile.curveSegments=12;
                profile.liminalGeneration=LiminalGeneration.Offices;profile.authoredRoomChance=0;
                root=new GameObject("v26 dry grounding world");root.SetActive(false);root.transform.position=new Vector3(8,4,-6);
                var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;world.generateOnStart=false;world.streaming=false;
                actor=new GameObject("v26 default controller actor");actor.SetActive(false);actor.transform.position=root.transform.position+Vector3.up*.1f;
                var body=actor.AddComponent<CharacterController>(); // Reproduce default centre = zero.
                body.center=Vector3.zero;body.minMoveDistance=.1f;body.detectCollisions=false;
                var cameraObject=new GameObject("v26 eyes at feet");cameraObject.transform.SetParent(actor.transform,false);cameraObject.AddComponent<Camera>();cameraObject.AddComponent<PoolroomsCameraEffects>();
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;explorer.eyes=cameraObject.transform;world.viewer=actor.transform;
                world.BuildAllImmediate();actor.SetActive(true);root.SetActive(true);yield return null;yield return null;
                Assert.That(body.center.y,Is.EqualTo(body.height*.5f).Within(.0001f));Assert.That(body.detectCollisions,Is.True);
                Assert.That(body.minMoveDistance,Is.Zero);Assert.That(explorer.eyes.position.y-actor.transform.position.y,Is.GreaterThan(1.5f));
                Assert.That(profile.colliders,Is.False,"Runtime correction must not edit the shared asset.");
                Assert.That(world.GeneratedRoot.GetComponentsInChildren<MeshCollider>().Length,Is.GreaterThan(0));
                explorer.Teleport(root.transform.position-Vector3.up*.65f);
                Cursor.lockState=CursorLockMode.Locked;yield return null;yield return null;
                Assert.That(body.bounds.min.y,Is.GreaterThan(root.transform.position.y-body.skinWidth-.02f),"An embedded saved position was not restored to the physical floor.");
                foreach(float frame in new[]{.1f,1f/20,1f/60})
                {
                    Time.captureDeltaTime=frame;
                    for(int i=0;i<30;i++)
                    {
                        Cursor.lockState=CursorLockMode.Locked;explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);yield return null;
                        Assert.That(body.bounds.min.y,Is.GreaterThan(root.transform.position.y-body.skinWidth-.02f),"Controller sank into a dry floor.");
                    }
                    Assert.That(explorer.eyes.position.y-actor.transform.position.y,Is.GreaterThan(1.5f),"Cached camera motion restored the old feet-level anchor.");
                    Assert.That(body.isGrounded,Is.True);Assert.That(explorer.RemainingJumps,Is.EqualTo(2));
                    explorer.FeedInput(Vector2.zero,Vector2.zero,true,false,false,false);yield return null;
                    Assert.That(explorer.RemainingJumps,Is.EqualTo(1));Assert.That(explorer.ViewVelocity.y,Is.GreaterThan(0));
                    explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);yield return null;
                    explorer.FeedInput(Vector2.zero,Vector2.zero,true,false,false,false);yield return null;
                    Assert.That(explorer.RemainingJumps,Is.Zero);Assert.That(explorer.ViewVelocity.y,Is.GreaterThan(0));
                    explorer.FeedInput(Vector2.zero,Vector2.zero,true,false,false,false);yield return null;
                    Assert.That(explorer.RemainingJumps,Is.Zero,"Third press must not grant another jump.");
                    for(int i=0;i<180&&!body.isGrounded;i++){explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);yield return null;}
                    Assert.That(body.isGrounded,Is.True);Assert.That(explorer.RemainingJumps,Is.EqualTo(2));
                }
                // Unavailable geometry must discard a press rather than firing it on resume.
                world.ClearPreview();explorer.FeedInput(Vector2.zero,Vector2.zero,true,false,false,false);yield return null;
                Assert.That(explorer.IsReady,Is.False);world.BuildAllImmediate();yield return null;yield return null;
                Assert.That(explorer.RemainingJumps,Is.EqualTo(2));Assert.That(explorer.ViewVelocity.y,Is.LessThanOrEqualTo(.05f));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=scale;Time.captureDeltaTime=capture;Physics.autoSyncTransforms=sync;AudioListener.pause=audio;Cursor.lockState=locked;Cursor.visible=visible;
            }
        }
    }
}
#endif
