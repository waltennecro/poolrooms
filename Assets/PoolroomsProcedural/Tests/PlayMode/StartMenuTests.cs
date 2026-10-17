#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;
using Poolrooms.Core;

namespace Poolrooms.Tests
{
    public sealed class StartMenuTests
    {
        [UnityTest]
        public IEnumerator TitleSettingsAndResumePreserveTheCurrentJourney()
        {
            float scale=Time.timeScale;bool audio=AudioListener.pause;var cursor=Cursor.lockState;bool visible=Cursor.visible;
            GameObject actor=null;
            try
            {
                actor=new GameObject("Title flow test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                explorer.showStartMenu=true;
                var eyes=new GameObject("Eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                yield return null;
                var menu=actor.GetComponent<PoolroomsPauseMenu>();
                Assert.That(menu.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
                var before=actor.transform.position;menu.TogglePause();Assert.That(menu.IsPaused,Is.True,"Esc must not skip title");
                menu.OpenRenderingSettings();Assert.That(menu.IsSettingsOpen,Is.True);
                menu.TogglePause();Assert.That(menu.IsSettingsOpen,Is.False);Assert.That(menu.IsPaused,Is.True);
                menu.BeginExploration();Assert.That(menu.IsPaused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(scale));
                menu.Pause();menu.OpenStartMenu();menu.OpenSettings(SettingsCategory.Controls);menu.TogglePause();menu.BeginExploration();
                Assert.That(actor.transform.position,Is.EqualTo(before),"Returning to title reset the player");
                Assert.That(actor.GetComponentsInChildren<UIDocument>(true).Length,Is.EqualTo(1),"Title created duplicate UI input owners");
                Assert.That(AudioListener.pause,Is.EqualTo(audio));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                Time.timeScale=scale;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
