#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ExplorerUtilitiesTests
    {
        [UnityTest]
        public IEnumerator ChatCapturesInputAndNoclipSurvivesRelocationAndPause()
        {
            float scale=Time.timeScale;bool audio=AudioListener.pause;
            var cursor=Cursor.lockState;bool visible=Cursor.visible;GameObject actor=null;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                actor=new GameObject("Master mode test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                var eyes=new GameObject("Test eyes");eyes.transform.SetParent(actor.transform,false);
                eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                yield return null;yield return null;
                var master=explorer.MasterMode;var body=actor.GetComponent<CharacterController>();
                Assert.That(master,Is.Not.Null);Assert.That(master.IsActive,Is.False);
                master.ToggleMenu();Assert.That(master.IsInterfaceOpen,Is.False);
                Assert.That(master.Submit("/noclip").Success,Is.False);Assert.That(body.enabled,Is.True);
                master.OpenChat();Assert.That(explorer.InputCaptured,Is.True);
                Vector3 position=actor.transform.position;Quaternion rotation=eyes.transform.rotation;
                explorer.FeedInput(Vector2.one,Vector2.one,true,true,false,false);
                explorer.FeedUtilityInput(false,true,true,true,true,false);
                yield return null;
                Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(eyes.transform.rotation,Is.EqualTo(rotation));
                Assert.That(master.IsInterfaceOpen,Is.True);Assert.That(explorer.Flashlight.IsOn,Is.False);
                Assert.That(master.Submit("/unlock mastermode").Success,Is.True);
                Assert.That(master.IsActive,Is.True);Assert.That(master.Flying,Is.True);Assert.That(master.Noclip,Is.True);
                Assert.That(body.enabled,Is.False);
                // M typed as a chat character must leave the chat open, even after unlocking.
                master.OpenChat();explorer.FeedUtilityInput(false,true,false,false,false,false);
                yield return null;Assert.That(master.IsInterfaceOpen,Is.True);
                explorer.Teleport(Vector3.up*200);Assert.That(body.enabled,Is.False);
                explorer.ShiftOrigin(new Vector3(-40,0,20));Assert.That(body.enabled,Is.False);
                Assert.That(actor.transform.position,Is.EqualTo(new Vector3(-40,200,20)));
                var pause=actor.GetComponent<PoolroomsPauseMenu>();pause.Pause();
                Assert.That(Time.timeScale,Is.Zero);Assert.That(master.IsInterfaceOpen,Is.False);
                master.ToggleMenu();Assert.That(master.IsInterfaceOpen,Is.False);
                position=actor.transform.position;
                explorer.FeedInput(Vector2.one,Vector2.one,true,true,false,false);
                explorer.FeedUtilityInput(false,false,false,false,true,false);
                yield return null;Assert.That(actor.transform.position,Is.EqualTo(position));
                pause.TogglePause();Assert.That(Time.timeScale,Is.EqualTo(1));
                master.ToggleMenu();Assert.That(master.IsInterfaceOpen,Is.True);
                master.DisableMasterMode();Assert.That(body.enabled,Is.True);
                Assert.That(master.IsActive,Is.False);Assert.That(master.IsInterfaceOpen,Is.False);
                Assert.That(actor.transform.position,Is.EqualTo(position),"Disabling cheats must preserve the current location.");
                master.ToggleMenu();Assert.That(master.IsInterfaceOpen,Is.False);
                master.Submit("/unlock mastermode");master.CloseInterface();
                master.enabled=false;Assert.That(body.enabled,Is.True);Assert.That(master.IsActive,Is.False);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                Time.timeScale=scale;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }

        [UnityTest]
        public IEnumerator NormalPlayerGetsOneAttachedLightWithSixPresetsAndCleanDisable()
        {
            const string key="Poolrooms.Flashlight.v1";bool saved=PlayerPrefs.HasKey(key);string previous=PlayerPrefs.GetString(key);
            var cursor=Cursor.lockState;bool visible=Cursor.visible;GameObject actor=null;
            try
            {
                PlayerPrefs.DeleteKey(key);
                actor=new GameObject("Flashlight test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                var eyes=new GameObject("Test eyes");eyes.transform.SetParent(actor.transform,false);
                eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                yield return null;yield return null;
                var flashlight=explorer.Flashlight;
                Assert.That(explorer.MasterMode.IsActive,Is.False);Assert.That(flashlight.Source,Is.Not.Null);
                Assert.That(flashlight.IsOn,Is.False);Assert.That(flashlight.Source.enabled,Is.False);
                flashlight.SetOn(true);float range=0;
                foreach(FlashlightLevel level in System.Enum.GetValues(typeof(FlashlightLevel)))
                {
                    flashlight.SetLevel(level);Light source=flashlight.Source;
                    Assert.That(source.enabled,Is.True);Assert.That(source.type,Is.EqualTo(LightType.Spot));
                    Assert.That(source.transform.parent,Is.EqualTo(eyes.transform));Assert.That(source.range,Is.GreaterThan(range));
                    Assert.That(source.range,Is.EqualTo(FlashlightModel.Look(level).Range));range=source.range;
                    Assert.That(eyes.GetComponentsInChildren<Light>().Length,Is.EqualTo(1));
                }
                flashlight.SetShadows(false);Assert.That(flashlight.Source.shadows,Is.EqualTo(LightShadows.None));
                var oldLight=flashlight.Source;flashlight.enabled=false;
                Assert.That(oldLight.enabled,Is.False);Assert.That(flashlight.Source,Is.Null);
                yield return null;Assert.That(oldLight==null,Is.True);
                flashlight.enabled=true;yield return null;
                Assert.That(flashlight.Level,Is.EqualTo(FlashlightLevel.SolarRay));Assert.That(flashlight.CastShadows,Is.False);
                Assert.That(eyes.GetComponentsInChildren<Light>().Length,Is.EqualTo(1));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                if(saved)PlayerPrefs.SetString(key,previous);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
