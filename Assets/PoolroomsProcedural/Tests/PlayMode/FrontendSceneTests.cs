#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Poolrooms.Tests
{
    public sealed class FrontendSceneTests
    {
        [Test]
        public void SceneLightingSnapshotRemapsTheSunToTheNewJourney()
        {
            var original=PoolroomsFrontendEnvironment.Capture(null);var previousSun=RenderSettings.sun;
            var root=new GameObject("Lighting source");root.SetActive(false);GameObject clone=null;
            try
            {
                var node=new GameObject("Sun");node.transform.SetParent(root.transform,false);
                var sun=node.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.None;
                RenderSettings.sun=sun;RenderSettings.ambientSkyColor=new Color(.21f,.32f,.43f);
                var snapshot=PoolroomsFrontendEnvironment.Capture(root.transform);
                clone=Object.Instantiate(root);RenderSettings.ambientSkyColor=Color.black;
                snapshot.Apply(clone.transform);
                Assert.That(RenderSettings.ambientSkyColor,Is.EqualTo(snapshot.sky));
                Assert.That(RenderSettings.sun,Is.SameAs(clone.GetComponentInChildren<Light>(true)));
                Assert.That(RenderSettings.sun,Is.Not.SameAs(sun));
            }
            finally
            {
                original.Apply(null);RenderSettings.sun=previousSun;
                Object.DestroyImmediate(root);if(clone!=null)Object.DestroyImmediate(clone);
            }
        }

        [UnityTest]
        public IEnumerator DemoIsCapturedBeforeGenerationAndMenuLivesInItsOwnScene()
        {
            var previous=SceneManager.GetActiveScene();
            var source=SceneManager.CreateScene("Frontend source test");
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
            PoolroomsFrontend frontend=null;Scene menu=default;
            float scale=Time.timeScale;bool audio=AudioListener.pause;
            try
            {
                var worldNode=new GameObject("Source world");SceneManager.MoveGameObjectToScene(worldNode,source);
                var world=worldNode.AddComponent<PoolroomsWorld>();world.profile=profile;
                var player=new GameObject("Source player");SceneManager.MoveGameObjectToScene(player,source);
                var actor=player.AddComponent<PoolroomsExplorer>();actor.world=world;actor.showStartMenu=true;world.viewer=player.transform;
                frontend=PoolroomsFrontend.CreateForScene(source);Assert.That(frontend,Is.Not.Null);
                menu=frontend.gameObject.scene;
                yield return null;yield return null;
                Assert.That(menu,Is.Not.EqualTo(source));
                Assert.That(frontend.HasGeneratedSession,Is.False);
                Assert.That(frontend.SessionWorld,Is.Null);
                foreach(var node in menu.GetRootGameObjects())
                    foreach(var dormant in node.GetComponentsInChildren<PoolroomsWorld>(true))
                    {
                        Assert.That(dormant.isActiveAndEnabled,Is.False);
                        Assert.That(dormant.CurrentLayout,Is.Null,"Generator ran before choosing Play");
                        Assert.That(dormant.GetComponentsInChildren<PoolroomsChunk>(true).Length,Is.Zero);
                    }
                var document=frontend.GetComponentInChildren<UIDocument>(true);
                Assert.That(document,Is.Not.Null);
                Assert.That(document.rootVisualElement.Query<Image>().ToList().Exists(image=>image.image!=null),Is.True);
                Assert.That(frontend.GetComponentInChildren<Camera>().cullingMask,Is.Zero);
                Assert.That(Time.timeScale,Is.Zero);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                if(frontend!=null)Object.DestroyImmediate(frontend.gameObject);
                Object.DestroyImmediate(profile);
                Time.timeScale=scale;AudioListener.pause=audio;
            }
            if(menu.IsValid()&&menu.isLoaded)yield return SceneManager.UnloadSceneAsync(menu);
            if(source.IsValid()&&source.isLoaded)yield return SceneManager.UnloadSceneAsync(source);
        }

        [Test]
        public void EveryFrontendSectionHasAnImportedIndependentBackground()
        {
            foreach(Poolrooms.Core.FrontendPage page in System.Enum.GetValues(typeof(Poolrooms.Core.FrontendPage)))
            {
                var texture=Resources.Load<Texture2D>("FrontendArt/"+page);
                Assert.That(texture,Is.Not.Null,page.ToString());
                Assert.That(texture.width,Is.GreaterThanOrEqualTo(1600));
                Assert.That(texture.height,Is.GreaterThanOrEqualTo(900));
            }
        }
    }
}
#endif
