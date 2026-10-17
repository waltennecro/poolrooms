#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Poolrooms.Tests
{
    public sealed class TerrainRenderingSettingsTests
    {
        [UnityTest]
        public IEnumerator F7OpensGraphicsAndRenderingWorksWithoutUnlockingCheats()
        {
            string key=PoolroomsGameSettings.PreferencesKey,backup=PlayerPrefs.GetString(key);bool had=PlayerPrefs.HasKey(key);
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause;var cursor=UnityEngine.Cursor.lockState;bool visible=UnityEngine.Cursor.visible;
            try
            {
                PlayerPrefs.DeleteKey(key);Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;profile.continuousWorld=false;profile.enableTerrainRegions=false;profile.enableGiantMalls=false;profile.curveSegments=12;
                root=new GameObject("v33 rendering world");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;world.unifiedExploration=false;world.generateOnStart=false;
                actor=new GameObject("v33 graphics explorer");actor.SetActive(false);var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.world=world;explorer.externalInput=true;world.viewer=actor.transform;
                var eyes=new GameObject("v33 eyes");eyes.transform.SetParent(actor.transform,false);var camera=eyes.AddComponent<Camera>();camera.farClipPlane=180;explorer.eyes=eyes.transform;
                world.BuildAllImmediate();actor.transform.position=world.SpawnWorldPosition;root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                var settings=actor.GetComponent<PoolroomsGameSettings>();var menu=actor.GetComponent<PoolroomsPauseMenu>();var master=explorer.MasterMode;
                Assert.That(master.IsActive,Is.False);explorer.FeedRenderingShortcut(true);yield return null;yield return null;
                Assert.That(menu.IsSettingsOpen&&explorer.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
                var document=actor.GetComponentInChildren<UIDocument>(true);
                Assert.That(document.rootVisualElement.Q<VisualElement>("rendering-controls"),Is.Not.Null);
                Assert.That(document.rootVisualElement.Q<VisualElement>("settings-group-1").style.display.value,Is.EqualTo(DisplayStyle.Flex));
                var search=document.rootVisualElement.Q<TextField>("settings-search");search.value="sonido";explorer.FeedRenderingShortcut(true);yield return null;
                Assert.That(document.rootVisualElement.Q<TextField>("settings-search").value,Is.Empty,"F7 retained a search filter hiding rendering.");
                settings.Preferences.rendering.roomRadius=4;settings.Preferences.rendering.inspectionLight=true;settings.Preferences.rendering.lightStrength=.3f;
                settings.Preferences.terrainGrass=TerrainGrassQuality.Off;settings.Change(SettingsChange.Graphics);settings.Flush();yield return null;
                Assert.That(world.LoadRadius,Is.EqualTo(4));var view=actor.GetComponent<PoolroomsMasterView>();Assert.That(view.KeyFill.enabled&&view.ReverseFill.enabled,Is.True);
                Assert.That(master.IsActive,Is.False);Vector3 position=actor.transform.position;
                master.Submit("/unlock mastermode");master.DisableMasterMode();yield return null;
                Assert.That(world.LoadRadius,Is.EqualTo(4));Assert.That(view.KeyFill.enabled,Is.True);Assert.That(Vector3.Distance(actor.transform.position,position),Is.LessThan(.2f),"Changing graphics/locking cheats teleported the player.");
                var saved=JsonUtility.FromJson<GamePreferences>(PlayerPrefs.GetString(key));Assert.That(saved.rendering.roomRadius,Is.EqualTo(4));Assert.That(saved.terrainGrass,Is.EqualTo(TerrainGrassQuality.Off));
                settings.Reset(SettingsCategory.Graphics);settings.Flush();yield return null;yield return null;
                Assert.That(world.LoadRadius,Is.EqualTo(profile.loadRadius));Assert.That(view.KeyFill.enabled,Is.False);Assert.That(camera.farClipPlane,Is.EqualTo(180).Within(.01f));
                menu.enabled=false;Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(explorer.IsPaused,Is.False);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                if(had)PlayerPrefs.SetString(key,backup);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Time.timeScale=time;AudioListener.pause=audio;UnityEngine.Cursor.lockState=cursor;UnityEngine.Cursor.visible=visible;
            }
        }
        [UnityTest]
        public IEnumerator GrassQualityChangesReleaseGeometryAndKeepTheTerrainColliders()
        {
            string key=PoolroomsGameSettings.PreferencesKey,backup=PlayerPrefs.GetString(key);bool had=PlayerPrefs.HasKey(key);
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause;var cursor=UnityEngine.Cursor.lockState;bool visible=UnityEngine.Cursor.visible;
            try
            {
                PlayerPrefs.DeleteKey(key);Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;profile.continuousWorld=false;profile.enableTerrainRegions=true;profile.enableGiantMalls=false;profile.terrainEntranceChance=0;profile.curveSegments=12;
                root=new GameObject("v33 grass world");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;world.unifiedExploration=false;
                actor=new GameObject("v33 grass explorer");actor.SetActive(false);var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.world=world;explorer.externalInput=true;world.viewer=actor.transform;
                var eyes=new GameObject("v33 eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                float deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.Terrain.VisitNearest(),Is.True);deadline=Time.realtimeSinceStartup+90;
                while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);var grass=root.GetComponent<PoolroomsTerrainGrass>();Assert.That(grass,Is.Not.Null);
                deadline=Time.realtimeSinceStartup+60;while(grass.PatchCount<9&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(grass.PatchCount,Is.EqualTo(9));int triangles=0,meshes=0;
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())if(mesh.gameObject.name=="Batched grass")
                {
                    meshes++;triangles+=mesh.sharedMesh.triangles.Length/3;
                    Assert.That(mesh.GetComponent<Collider>(),Is.Null);Assert.That(mesh.GetComponent<MeshRenderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
                }
                Assert.That(meshes,Is.GreaterThan(0));Assert.That(triangles,Is.LessThanOrEqualTo(5292),"Dense economy grass exceeded nine patches of 588 triangles");
                var settings=actor.GetComponent<PoolroomsGameSettings>();settings.Preferences.terrainGrass=TerrainGrassQuality.High;settings.Change(SettingsChange.Graphics);
                deadline=Time.realtimeSinceStartup+60;while(grass.PatchCount<25&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(grass.PatchCount,Is.EqualTo(25));settings.Preferences.terrainGrass=TerrainGrassQuality.Off;settings.Change(SettingsChange.Graphics);yield return null;yield return null;
                Assert.That(grass.PatchCount,Is.Zero);Assert.That(world.ViewerReady,Is.True);
                foreach(var mesh in root.GetComponentsInChildren<MeshFilter>())Assert.That(mesh.gameObject.name,Is.Not.EqualTo("Batched grass"));
                RaycastHit hit;Assert.That(Physics.Raycast(actor.transform.position+Vector3.up*.5f,Vector3.down,out hit,1,~0,QueryTriggerInteraction.Ignore),Is.True,"Disabling grass removed terrain support.");
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                if(had)PlayerPrefs.SetString(key,backup);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Time.timeScale=time;AudioListener.pause=audio;UnityEngine.Cursor.lockState=cursor;UnityEngine.Cursor.visible=visible;
            }
        }
    }
}
#endif
