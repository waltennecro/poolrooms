#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class CameraPauseTests
    {
        [UnityTest]
        public IEnumerator PauseFreezesExplorerAndDisableRestoresGlobalState()
        {
            float oldScale=Time.timeScale;bool oldAudio=AudioListener.pause;
            var oldLock=Cursor.lockState;bool oldVisible=Cursor.visible;
            GameObject actor=null;
            try
            {
                Time.timeScale=.5f;AudioListener.pause=true;
                actor=new GameObject("Camera pause test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var camera=new GameObject("Test eyes");camera.transform.SetParent(actor.transform,false);
                camera.AddComponent<Camera>();explorer.eyes=camera.transform;
                yield return null;
                var menu=actor.GetComponent<PoolroomsPauseMenu>();Assert.That(menu,Is.Not.Null);
                menu.Pause();Assert.That(Time.timeScale,Is.Zero);Assert.That(explorer.IsPaused,Is.True);
                Vector3 position=actor.transform.position;
                explorer.FeedInput(Vector2.one,Vector2.one,true,true,false,false);
                yield return null;
                Assert.That(actor.transform.position,Is.EqualTo(position));
                menu.enabled=false;
                Assert.That(Time.timeScale,Is.EqualTo(.5f));Assert.That(AudioListener.pause,Is.True);
                Assert.That(explorer.IsPaused,Is.False);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                Time.timeScale=oldScale;AudioListener.pause=oldAudio;Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            }
        }
        [UnityTest]
        public IEnumerator TeleportClearsCameraTransientEffectsAndKeepsNoHud()
        {
            var oldLock=Cursor.lockState;bool oldVisible=Cursor.visible;GameObject actor=null;
            try
            {
                actor=new GameObject("Camera teleport test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var camera=new GameObject("Test eyes");camera.transform.SetParent(actor.transform,false);
                camera.AddComponent<Camera>();explorer.eyes=camera.transform;
                yield return null;
                var effects=camera.GetComponent<PoolroomsCameraEffects>();Assert.That(effects,Is.Not.Null);
                effects.flarePositions[0]=Vector4.one;
                explorer.Teleport(Vector3.up*200);
                Assert.That(effects.flarePositions[0],Is.EqualTo(Vector4.zero));
                Assert.That(effects.Wetness,Is.Zero);Assert.That(explorer.IsReady,Is.False);
                Assert.That(explorer.showHelp,Is.False);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            }
        }
    }
}
#endif
