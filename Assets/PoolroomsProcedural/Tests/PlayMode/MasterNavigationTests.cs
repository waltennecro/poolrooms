#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class MasterNavigationTests
    {
        private bool hadPreferences;private string savedPreferences;
        [SetUp] public void IsolateGraphicsPreferences()
        {hadPreferences=PlayerPrefs.HasKey(PoolroomsGameSettings.PreferencesKey);savedPreferences=PlayerPrefs.GetString(PoolroomsGameSettings.PreferencesKey);PlayerPrefs.DeleteKey(PoolroomsGameSettings.PreferencesKey);}
        [TearDown] public void RestoreGraphicsPreferences()
        {if(hadPreferences)PlayerPrefs.SetString(PoolroomsGameSettings.PreferencesKey,savedPreferences);else PlayerPrefs.DeleteKey(PoolroomsGameSettings.PreferencesKey);PlayerPrefs.Save();}
        [UnityTest]
        public IEnumerator RoomWindowExpandsAndShrinksWithoutEditingTheProfile()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.continuousWorld=true;profile.loadRadius=1;profile.unloadRadius=2;
                profile.liminalGeneration=LiminalGeneration.Offices;profile.authoredRoomChance=0;profile.curveSegments=12;
                profile.enableGiantMalls=false;profile.enableTerrainRegions=false;
                root=new GameObject("v27 radius world");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;
                actor=new GameObject("v27 radius explorer");actor.SetActive(false);actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("v27 radius eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;world.viewer=actor.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                var master=explorer.MasterMode;master.Submit("/unlock mastermode");master.SetRoomLoadRadius(3);
                float deadline=Time.realtimeSinceStartup+90;
                while(world.LoadedRoomCount<49&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.LoadedRoomCount,Is.EqualTo(49),"Streaming did not expand to the requested radius.");
                master.SetRoomLoadRadius(4);deadline=Time.realtimeSinceStartup+90;
                while(world.LoadedRoomCount<81&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.LoadedRoomCount,Is.EqualTo(81));
                master.SetRoomLoadRadius(1);deadline=Time.realtimeSinceStartup+20;
                while(world.LoadedRoomCount>25&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.LoadedRoomCount,Is.LessThanOrEqualTo(25),"Shrinking must release distant rooms, retaining one ring for hysteresis.");
                Assert.That(profile.loadRadius,Is.EqualTo(1));Assert.That(profile.unloadRadius,Is.EqualTo(2));
                master.SetRoomLoadRadius(12);master.DisableMasterMode();Assert.That(world.administratorView,Is.Null);
                Assert.That(world.LoadRadius,Is.EqualTo(12),"Graphics distance must remain set after locking master mode.");
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }

        [UnityTest]
        public IEnumerator CollisionTogglesKeepLocationAndRecoverOnlyLocalIntersections()
        {
            GameObject actor=null,wall=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                actor=new GameObject("v27 admin collision regression");actor.transform.position=new Vector3(23,100,-17);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var eyes=new GameObject("v27 eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                yield return null;yield return null;
                var master=explorer.MasterMode;var body=actor.GetComponent<CharacterController>();
                master.Submit("/unlock mastermode");Vector3 position=actor.transform.position;
                master.SetNoclip(false);Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(body.enabled,Is.True);
                master.SetFlight(false);Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(master.Flying,Is.False);
                master.SetNoclip(true);master.SetInspectionLight(true);master.SetRoomLoadRadius(12);
                master.DisableMasterMode();Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(body.enabled,Is.True);
                master.Submit("/unlock mastermode");
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=position+body.center;
                wall.transform.localScale=new Vector3(.2f,4,4);Physics.SyncTransforms();
                master.SetNoclip(false);Assert.That(master.Noclip,Is.False);Assert.That(body.enabled,Is.True);
                Assert.That(Vector3.Distance(actor.transform.position,position),Is.LessThan(3),"Local recovery became a long teleport.");
                Assert.That(Vector3.Distance(actor.transform.position,position),Is.GreaterThan(.05f),"Thin wall overlap was not corrected.");
                Vector3 resolved;Assert.That(PoolroomsCollisionRecovery.TryResolve(body,out resolved),Is.True);
                Assert.That(Vector3.Distance(resolved,actor.transform.position),Is.LessThan(.01f));
                master.SetNoclip(true);explorer.Teleport(position);wall.transform.localScale=Vector3.one*50;Physics.SyncTransforms();
                master.SetFlight(false);
                Assert.That(master.IsActive&&master.Noclip&&master.Flying,Is.True,"An unresolved thick wall must retain escape controls.");
                Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(body.enabled,Is.False);
                wall.SetActive(false);master.DisableMasterMode();Assert.That(actor.transform.position,Is.EqualTo(position));
                master.Submit("/unlock mastermode");master.enabled=false;
                Assert.That(actor.transform.position,Is.EqualTo(position),"Component disable returned to spawn.");
            }
            finally
            {
                if(wall!=null)Object.DestroyImmediate(wall);if(actor!=null)Object.DestroyImmediate(actor);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }

        [UnityTest]
        public IEnumerator DistrictTravelWorksWithoutNaturalPortalsAndRestoresViewOverrides()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;profile.continuousWorld=false;
                profile.liminalGeneration=LiminalGeneration.Offices;profile.authoredRoomChance=0;profile.curveSegments=12;
                profile.enableGiantMalls=false;profile.enableTerrainRegions=false;profile.mallEntranceChance=profile.terrainEntranceChance=0;
                root=new GameObject("v27 district world");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;world.generateOnStart=false;world.streaming=false;
                actor=new GameObject("v27 district explorer");actor.SetActive(false);var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("v27 district eyes");eyes.transform.SetParent(actor.transform,false);var camera=eyes.AddComponent<Camera>();camera.farClipPlane=180;explorer.eyes=eyes.transform;world.viewer=actor.transform;
                world.BuildAllImmediate();explorer.Teleport(world.SpawnWorldPosition);root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                string originalProfile=JsonUtility.ToJson(profile);var master=explorer.MasterMode;var view=actor.GetComponent<PoolroomsMasterView>();
                Assert.That(master.VisitMall(),Is.False,"Travel must require an unlocked session.");
                master.Submit("/unlock mastermode");master.SetRoomLoadRadius(12);master.SetTerrainViewRadius(8);master.SetMallVisibility(14,5);master.SetInspectionLight(true);
                yield return null;
                Assert.That(world.LoadRadius,Is.EqualTo(12));Assert.That(world.UnloadRadius,Is.EqualTo(13));Assert.That(camera.farClipPlane,Is.GreaterThan(180));
                Assert.That(view.KeyFill.enabled&&view.ReverseFill.enabled,Is.True);Assert.That(view.KeyFill.shadows,Is.EqualTo(LightShadows.None));
                Assert.That(master.VisitMall(),Is.True);Assert.That(world.Malls.IsInside,Is.True);Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.False);
                Vector3 destination=actor.transform.position;
                explorer.FeedInput(Vector2.one,Vector2.zero,false,true,false,false);yield return null;
                Assert.That(actor.transform.position,Is.EqualTo(destination),"Teleport moved before destination collision support was ready.");
                explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);
                float deadline=Time.realtimeSinceStartup+60;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True,"Mall never became usable.");Assert.That(world.Malls.Plan.Floors,Is.InRange(2,6));
                Assert.That(master.VisitLevel94(),Is.True);Assert.That(world.Terrain.IsInside,Is.True);Assert.That(world.Malls.IsInside,Is.False);
                destination=actor.transform.position;explorer.FeedInput(Vector2.one,Vector2.zero,false,true,false,false);yield return null;
                Assert.That(actor.transform.position,Is.EqualTo(destination));explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);
                deadline=Time.realtimeSinceStartup+60;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True,"Terrain never became usable.");
                Assert.That(world.Terrain.LoadedCount,Is.LessThan(289),"A huge distant window must not block local readiness.");
                Assert.That(master.VisitMall(),Is.True);Assert.That(world.Terrain.IsInside,Is.False);
                Assert.That(world.VisitEnvironment(LiminalEnvironment.Office),Is.True);Assert.That(world.Malls.IsInside,Is.False);
                yield return null;yield return null;
                Assert.That(JsonUtility.ToJson(profile),Is.EqualTo(originalProfile),"Administrator settings edited the shared profile.");
                master.SetInspectionLight(false);Assert.That(view.KeyFill.enabled||view.ReverseFill.enabled,Is.False);
                master.SetRoomLoadRadius(0);yield return null;Assert.That(world.LoadRadius,Is.EqualTo(profile.loadRadius));
                Assert.That(camera.farClipPlane,Is.EqualTo(180).Within(.01f));
                master.DisableMasterMode();Assert.That(world.administratorView,Is.Null);
                Assert.That(world.TerrainRadius,Is.EqualTo(8));Assert.That(world.MallRadius,Is.EqualTo(14),"Graphics settings now belong to the player, independently of cheats.");
                Assert.That(JsonUtility.ToJson(profile),Is.EqualTo(originalProfile));
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
