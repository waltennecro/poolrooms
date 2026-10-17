#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ExplorerSlideTests
    {
        [UnityTest]
        public IEnumerator SlideRespectsWallsLowCeilingsAndDoubleJump()
        {
            GameObject floor=null,actor=null,wall=null,roof=null;
            string key=PoolroomsGameSettings.PreferencesKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
            float time=Time.timeScale,capture=Time.captureDeltaTime;bool audio=AudioListener.pause,sync=Physics.autoSyncTransforms,visible=Cursor.visible;var cursor=Cursor.lockState;
            int rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            try
            {
                PlayerPrefs.DeleteKey(key);Time.timeScale=1;Time.captureDeltaTime=.02f;AudioListener.pause=false;Physics.autoSyncTransforms=false;
                floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,99.8f,0);floor.transform.localScale=new Vector3(500,.4f,500);
                actor=new GameObject("v31 slide actor");actor.SetActive(false);actor.transform.position=new Vector3(0,100.05f,0);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                var body=actor.GetComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.3f;
                var eyes=new GameObject("slide eyes");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.65f;eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                Physics.SyncTransforms();actor.SetActive(true);yield return null;yield return null;
                var stance=actor.GetComponent<PoolroomsSlide>();var effects=eyes.GetComponent<PoolroomsCameraEffects>();
                effects.preferences=new CameraPreferences{reduceMotion=true}; // Physical stance still applies with shake disabled.
                for(int f=0;f<20;f++){Input(explorer,false);yield return null;}
                foreach(float frame in new[]{.1f,.05f,1f/60})
                {
                    Time.captureDeltaTime=frame;explorer.Teleport(new Vector3(0,100.05f,0));
                    for(int f=0;f<10;f++){Input(explorer,false);yield return null;}
                    Assert.That(body.isGrounded,Is.True);Assert.That(explorer.RemainingJumps,Is.EqualTo(2));
                    for(int f=0;f<10;f++)
                    {
                        Input(explorer,true);yield return null;
                        Assert.That(explorer.IsSliding,Is.True);Assert.That(body.height,Is.LessThan(1));
                        Assert.That(body.stepOffset,Is.EqualTo(.3f).Within(.001f),"Shortening the capsule removed normal stair clearance.");
                        Assert.That(body.bounds.min.y,Is.GreaterThan(100-body.skinWidth-.03f),"Resizing sank the feet.");
                        Assert.That(eyes.transform.localPosition.y,Is.LessThan(1.1f),"View did not follow the low capsule.");
                    }
                    Input(explorer,true,true);yield return null;
                    Assert.That(explorer.RemainingJumps,Is.EqualTo(1),"First slide jump consumed double jump.");
                    Assert.That(new Vector2(explorer.ViewVelocity.x,explorer.ViewVelocity.z).magnitude,Is.GreaterThan(8));
                    Input(explorer,true);yield return null;
                    Input(explorer,true,true);yield return null;Assert.That(explorer.RemainingJumps,Is.Zero);
                    Input(explorer,true,true);yield return null;Assert.That(explorer.RemainingJumps,Is.Zero);
                    for(int f=0;f<120&&!body.isGrounded;f++){Input(explorer,false);yield return null;}
                    Assert.That(body.isGrounded,Is.True);Assert.That(explorer.RemainingJumps,Is.EqualTo(2));
                }
                Time.captureDeltaTime=.02f;
                // A frontal wall stops the boost using CharacterController collision, not a teleport.
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,101.5f,10);wall.transform.localScale=new Vector3(8,3,.3f);Physics.SyncTransforms();
                explorer.Teleport(new Vector3(0,100.05f,0));for(int f=0;f<10;f++){Input(explorer,false);yield return null;}
                for(int f=0;f<70;f++){Input(explorer,true);yield return null;}
                Assert.That(actor.transform.position.z,Is.LessThan(9.8f));Assert.That(explorer.IsSliding,Is.False);
                Object.DestroyImmediate(wall);wall=null;
                // Release under the ceiling must leave a short body until physically clear.
                roof=GameObject.CreatePrimitive(PrimitiveType.Cube);roof.transform.position=new Vector3(0,101.45f,20);roof.transform.localScale=new Vector3(8,.5f,12);Physics.SyncTransforms();
                explorer.Teleport(new Vector3(0,100.05f,7));for(int f=0;f<10;f++){Input(explorer,false);yield return null;}
                for(int f=0;f<40;f++){Input(explorer,true);yield return null;}
                Assert.That(actor.transform.position.z,Is.GreaterThan(14));
                for(int f=0;f<10;f++){Input(explorer,false);yield return null;}
                Assert.That(stance.IsLow,Is.True);Assert.That(stance.CanStand(),Is.False);Assert.That(explorer.IsSliding,Is.False);
                for(int f=0;f<200;f++){Input(explorer,false);yield return null;}
                Assert.That(actor.transform.position.z,Is.GreaterThan(27));Assert.That(stance.IsLow,Is.False);Assert.That(body.height,Is.EqualTo(1.8f).Within(.001f));
                Assert.That(eyes.transform.localPosition.y,Is.EqualTo(1.65f).Within(.01f),"View anchor drifted after standing.");
                Input(explorer,true);yield return null;Assert.That(explorer.IsSliding,Is.True);
                explorer.SetPaused(true);Vector3 position=actor.transform.position;yield return null;
                Assert.That(explorer.IsSliding,Is.False);Assert.That(actor.transform.position,Is.EqualTo(position));
                explorer.SetPaused(false);Input(explorer,true);yield return null;Assert.That(explorer.IsSliding,Is.False,"Closing a menu replayed held Ctrl.");
                explorer.Teleport(new Vector3(0,100.05f,0));Assert.That(explorer.IsSliding,Is.False);Assert.That(stance.CameraDrop,Is.Zero);
                // The fallback also lowers the view if the optical effects component is disabled.
                effects.enabled=false;for(int f=0;f<10;f++){Input(explorer,false);yield return null;}
                Input(explorer,true);yield return null;Assert.That(eyes.transform.localPosition.y,Is.LessThan(1.1f));
                effects.enabled=true;Input(explorer,true);yield return null;
                Assert.That(eyes.transform.localPosition.y,Is.InRange(.6f,1.1f),"Re-enabling optics applied stance twice.");
                for(int f=0;f<20;f++){Input(explorer,false);yield return null;}
                Assert.That(eyes.transform.localPosition.y,Is.EqualTo(1.65f).Within(.01f),"Optics toggle changed the standing anchor.");
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(wall!=null)Object.DestroyImmediate(wall);if(roof!=null)Object.DestroyImmediate(roof);if(floor!=null)Object.DestroyImmediate(floor);
                if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Time.timeScale=time;Time.captureDeltaTime=capture;AudioListener.pause=audio;Physics.autoSyncTransforms=sync;Cursor.lockState=cursor;Cursor.visible=visible;
                Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;
            }
        }
        private static void Input(PoolroomsExplorer explorer,bool slide,bool jump=false)
        {Cursor.lockState=CursorLockMode.Locked;explorer.FeedInput(Vector2.up,Vector2.zero,jump,false,false,false);explorer.FeedSlideInput(slide);}
    }
}
#endif
