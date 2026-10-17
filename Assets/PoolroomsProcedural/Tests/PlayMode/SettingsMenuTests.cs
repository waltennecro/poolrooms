#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Poolrooms.Tests
{
    public sealed class SettingsMenuTests
    {
        [UnityTest]
        public IEnumerator MenuAppliesValuesSearchesWithoutAccentsAndRestoresPause()
        {
            string key=PoolroomsGameSettings.PreferencesKey;bool hadKey=PlayerPrefs.HasKey(key);string backup=PlayerPrefs.GetString(key);
            int oldRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;float oldScale=Time.timeScale;bool oldAudio=AudioListener.pause;
            var oldLock=UnityEngine.Cursor.lockState;bool oldVisible=UnityEngine.Cursor.visible;GameObject actor=null;
            try
            {
                PlayerPrefs.DeleteKey(key);actor=new GameObject("Minimal settings test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                var eyes=new GameObject("Settings eyes");eyes.transform.SetParent(actor.transform,false);var camera=eyes.AddComponent<Camera>();camera.fieldOfView=73;explorer.eyes=eyes.transform;
                yield return null;
                var menu=actor.GetComponent<PoolroomsPauseMenu>();var settings=actor.GetComponent<PoolroomsGameSettings>();
                Assert.That(settings,Is.Not.Null);Assert.That(settings.Preferences.fieldOfView,Is.EqualTo(73).Within(.01f));
                menu.OpenSettings(SettingsCategory.Audio);Assert.That(menu.IsSettingsOpen,Is.True);Assert.That(Time.timeScale,Is.Zero);
                var document=actor.GetComponentInChildren<UIDocument>(true);Assert.That(document,Is.Not.Null);
                var search=document.rootVisualElement.Q<TextField>("settings-search");Assert.That(search,Is.Not.Null);
                search.value="exposicion";yield return null;
                Assert.That(document.rootVisualElement.Q<VisualElement>("settings-group-1").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                Assert.That(document.rootVisualElement.Q<VisualElement>("settings-group-3").style.display.value,Is.EqualTo(DisplayStyle.None));
                settings.Preferences.masterVolume=.27f;settings.Change(SettingsChange.Audio);
                Assert.That(actor.GetComponent<PoolroomsAmbientAudio>().masterVolume,Is.EqualTo(.27f));
                settings.Preferences.fieldOfView=91;settings.Change(SettingsChange.Camera);
                var effects=eyes.GetComponent<PoolroomsCameraEffects>();effects.ClearTransientEffects();Assert.That(camera.fieldOfView,Is.EqualTo(91).Within(.01f));
                settings.Preferences.sensitivity=.16f;settings.Preferences.invertY=true;settings.Preferences.toggleSprint=true;settings.Change(SettingsChange.Controls);
                Assert.That(explorer.lookSensitivity,Is.EqualTo(.16f));Assert.That(explorer.invertLookY&&explorer.toggleSprint,Is.True);
                settings.Reset(SettingsCategory.Audio);Assert.That(settings.Preferences.fieldOfView,Is.EqualTo(91));
                settings.Flush();Assert.That(PlayerPrefs.HasKey(key),Is.True);
                menu.enabled=false;Assert.That(explorer.IsPaused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(oldScale));Assert.That(AudioListener.pause,Is.EqualTo(oldAudio));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                if(hadKey)PlayerPrefs.SetString(key,backup);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Application.targetFrameRate=oldRate;QualitySettings.vSyncCount=oldVSync;Time.timeScale=oldScale;AudioListener.pause=oldAudio;
                UnityEngine.Cursor.lockState=oldLock;UnityEngine.Cursor.visible=oldVisible;
            }
        }
    }
}
#endif
