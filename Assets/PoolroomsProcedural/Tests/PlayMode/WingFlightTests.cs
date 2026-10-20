#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class WingFlightTests
    {
        private const string Key="Poolrooms.Player.Wings";
        [Test]
        public void NormalWingFlightStopsAtWallsAndCeilingsWithoutMasterUnlock()
        {
            GameObject actor=null,wall=null,ceiling=null;
            bool had=PlayerPrefs.HasKey(Key);string saved=PlayerPrefs.GetString(Key,"");
            var locked=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                actor=new GameObject("Normal wing collision regression");actor.SetActive(false);actor.transform.position=Vector3.up;
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.3f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var wings=actor.AddComponent<PoolroomsWings>();actor.SetActive(true);
                wings.Preferences.equipped=true;wings.Preferences.speed=WingSpeed.VeryFast;wings.SavePreferences();
                Assert.That(explorer.MasterMode,Is.Null,"No master component/session should be needed before Start.");
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,2,2);wall.transform.localScale=new Vector3(20,10,.1f);
                ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(0,4,0);ceiling.transform.localScale=new Vector3(20,.1f,20);
                Physics.SyncTransforms();
                Assert.That(wings.ToggleFlight(),Is.True);
                for(int i=0;i<30;i++)wings.TickFlight(Vector3.forward,.1f);
                Assert.That(body.bounds.max.z,Is.LessThan(wall.GetComponent<Collider>().bounds.min.z+body.skinWidth+.02f),"Very fast flight crossed a thin wall beyond the configured contact margin.");
                for(int i=0;i<30;i++)wings.TickFlight(Vector3.up,.1f);
                Assert.That(body.bounds.max.y,Is.LessThan(ceiling.GetComponent<Collider>().bounds.min.y+body.skinWidth+.02f),"Wing flight crossed a ceiling beyond the configured contact margin.");
                Vector3 before=actor.transform.position;
                explorer.SetPaused(true);for(int i=0;i<10;i++)wings.TickFlight(Vector3.forward,.1f);
                Assert.That(actor.transform.position,Is.EqualTo(before));Assert.That(wings.Flying,Is.True,"Pausing must preserve hover mode.");
                explorer.SetPaused(false);Assert.That(wings.ToggleFlight(),Is.False);Assert.That(wings.Flying,Is.False);
                Assert.That(body.enabled,Is.True);Assert.That(body.detectCollisions,Is.True);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(wall!=null)Object.DestroyImmediate(wall);if(ceiling!=null)Object.DestroyImmediate(ceiling);
                if(had)PlayerPrefs.SetString(Key,saved);else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();
                Cursor.lockState=locked;Cursor.visible=visible;
            }
        }
        [Test]
        public void UnequippingOrDisablingWingsStopsNormalFlightAndPhotoVisibilityHidesAllRenderers()
        {
            GameObject actor=null;bool had=PlayerPrefs.HasKey(Key);string saved=PlayerPrefs.GetString(Key,"");
            var locked=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                actor=new GameObject("Wing lifecycle regression");actor.SetActive(false);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var wings=actor.AddComponent<PoolroomsWings>();actor.SetActive(true);wings.Preferences.equipped=true;wings.SavePreferences();
                Assert.That(wings.ToggleFlight(),Is.True);wings.SetVisible(false,true);
                var renderers=actor.GetComponentsInChildren<MeshRenderer>();Assert.That(renderers.Length,Is.EqualTo(3));
                foreach(var renderer in renderers)Assert.That(renderer.forceRenderingOff,Is.True,"Photo hide leaves floating wings.");
                wings.SetVisible(true,false);foreach(var renderer in renderers)Assert.That(renderer.forceRenderingOff,Is.False);
                wings.Preferences.equipped=false;wings.SavePreferences();Assert.That(wings.Flying,Is.False);
                Assert.That(wings.ToggleFlight(),Is.False,"Unequipped wings still permit flight.");
                wings.Preferences.equipped=true;wings.SavePreferences();Assert.That(wings.ToggleFlight(),Is.True);
                wings.enabled=false;Assert.That(wings.Flying,Is.False);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                if(had)PlayerPrefs.SetString(Key,saved);else PlayerPrefs.DeleteKey(Key);PlayerPrefs.Save();
                Cursor.lockState=locked;Cursor.visible=visible;
            }
        }
    }
}
#endif
