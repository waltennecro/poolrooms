#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class CameraLocomotionTests
    {
        [UnityTest]
        public IEnumerator RunningComposesPoseWithoutDriftAndPauseFreezesFlashlight()
        {
            GameObject actor=null,floor=null;var oldLock=Cursor.lockState;bool oldVisible=Cursor.visible;float oldTime=Time.timeScale;bool oldAudioPause=AudioListener.pause;
            try
            {
                Time.timeScale=1;
                floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Camera gait test floor";
                floor.transform.position=new Vector3(0,99.8f,0);floor.transform.localScale=new Vector3(60,.4f,60);
                actor=new GameObject("Camera gait test actor");actor.transform.position=Vector3.up*100.05f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                var body=actor.GetComponent<CharacterController>();body.height=1.8f;body.center=Vector3.up*.9f;body.radius=.32f;body.stepOffset=.25f;
                var eyes=new GameObject("Camera gait eyes");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.65f;
                eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;Physics.SyncTransforms();
                yield return null;yield return null;
                var effects=eyes.GetComponent<PoolroomsCameraEffects>();effects.preferences=new CameraPreferences{locomotionMotion=true,motionIntensity=.7f};
                var torch=explorer.Flashlight;torch.SetOn(true);float power=torch.Source.intensity,range=torch.Source.range;var level=torch.Level;
                Vector3 rest=eyes.transform.localPosition;float maxBob=0,maxTorch=0;
                for(int i=0;i<40;i++)
                {
                    Cursor.lockState=CursorLockMode.Locked;
                    explorer.FeedInput(Vector2.up,Vector2.zero,false,true,false,false);yield return null;
                    maxBob=Mathf.Max(maxBob,(eyes.transform.localPosition-rest).magnitude);
                    maxTorch=Mathf.Max(maxTorch,Quaternion.Angle(torch.Source.transform.localRotation,Quaternion.identity));
                }
                Assert.That(maxBob,Is.GreaterThan(.0005f));Assert.That(maxBob,Is.LessThan(.04f));
                Assert.That(maxTorch,Is.GreaterThan(.01f));Assert.That(torch.Source.intensity,Is.EqualTo(power));Assert.That(torch.Source.range,Is.EqualTo(range));
                Vector3 frozen=eyes.transform.localPosition;Quaternion frozenTorch=torch.Source.transform.localRotation;
                explorer.SetPaused(true);Time.timeScale=0;yield return null;yield return null;
                Assert.That(eyes.transform.localPosition,Is.EqualTo(frozen));Assert.That(torch.Source.transform.localRotation,Is.EqualTo(frozenTorch));
                effects.preferences.reduceMotion=true;effects.RefreshPreferences();yield return null;
                Assert.That(eyes.transform.localPosition,Is.EqualTo(rest));Assert.That(Quaternion.Angle(torch.Source.transform.localRotation,Quaternion.identity),Is.LessThan(.001f));
                explorer.Teleport(Vector3.up*101);Assert.That(effects.FlashlightAngles,Is.EqualTo(Vector3.zero));
                Assert.That(torch.Level,Is.EqualTo(level));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(floor!=null)Object.DestroyImmediate(floor);
                Time.timeScale=oldTime;AudioListener.pause=oldAudioPause;Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            }
        }
    }
}
#endif
