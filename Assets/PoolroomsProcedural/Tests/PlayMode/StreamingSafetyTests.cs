#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class StreamingSafetyTests
    {
        [Test]
        public void PhysicsUploadsKeepIndependentMeshesAndHonorSupportPrefix()
        {
            var root=new GameObject("v36 independent collision uploads");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();var data=new MeshData();
                data.Box(new V3(-2,-.2f,-2),new V3(2,0,2),Surface.Tile,1);
                int support=data.Indices[(int)Surface.Tile].Count;
                // A tall skirt represents rendered geometry that must never block walking.
                data.Box(new V3(1.9f,-7,-2),new V3(2,3,2),Surface.Tile,1);
                var first=MeshFactory.CreateCollider("first",data,root.transform,owner,support);
                Vector3[] snapshot=first.sharedMesh.vertices;int[] triangles=first.sharedMesh.triangles;
                var other=new MeshData();other.Box(new V3(20,0,20),new V3(22,5,22),Surface.Tile,1);
                var second=MeshFactory.CreateCollider("second",other,root.transform,owner);
                CollectionAssert.AreEqual(snapshot,first.sharedMesh.vertices);
                CollectionAssert.AreEqual(triangles,first.sharedMesh.triangles);
                Assert.That(first.sharedMesh,Is.Not.SameAs(second.sharedMesh));
                Assert.That(first.sharedMesh.triangles.Length,Is.EqualTo(support));
                Assert.That(first.sharedMesh.vertexCount,Is.LessThan(data.Positions.Count),"Unused skirt vertices inflated physics buffers.");
                Assert.That(first.sharedMesh.bounds.size.y,Is.EqualTo(.2f).Within(.001f));
                Physics.SyncTransforms();RaycastHit hit;
                Assert.That(first.Raycast(new Ray(new Vector3(0,2,0),Vector3.down),out hit,3),Is.True);
                Assert.That(hit.point.y,Is.EqualTo(0).Within(.001f));
                Assert.That(first.Raycast(new Ray(new Vector3(0,1,0),Vector3.right),out hit,3),Is.False,"Visual skirt entered collider prefix.");
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator ReplacementKeepsOldPhysicsUntilCommitAndCancelsCleanly()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;IEnumerator upload=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            int rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;
                profile.enableTerrainRegions=true;profile.terrainViewRadius=2;profile.zoneMode=ZoneMode.Single;
                for(int i=0;i<ZoneCatalog.Count;i++)if(ZoneCatalog.Get(i).Pools==PoolPlan.Dry){profile.singleZone=i;break;}
                root=new GameObject("v36 publication test");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();
                world.unifiedExploration=false;world.profile=profile;world.generateOnStart=false;world.streaming=false;
                actor=new GameObject("v36 publication actor");actor.SetActive(false);var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;
                explorer.externalInput=true;explorer.world=world;world.viewer=actor.transform;
                var body=actor.GetComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.skinWidth=.025f;body.center=Vector3.up*.9f;
                root.SetActive(true);world.BuildAllImmediate();explorer.Teleport(world.SpawnWorldPosition);actor.SetActive(true);
                yield return null;yield return null;
                var terrain=world.Terrain;Assert.That(terrain.Enter(world.CurrentLayout.Rooms[0],Vector3.zero),Is.True);
                float deadline=Time.realtimeSinceStartup+60;
                while((terrain.LoadedCount<25||terrain.HasStagedTile||!terrain.ViewerReady)&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(terrain.LoadedCount,Is.EqualTo(25));Assert.That(terrain.ViewerReady,Is.True);
                explorer.SetPaused(true);
                var flags=BindingFlags.NonPublic|BindingFlags.Instance;
                var coroutine=(Coroutine)typeof(PoolroomsTerrainSystem).GetField("worker",flags).GetValue(terrain);
                if(coroutine!=null)terrain.StopCoroutine(coroutine);
                typeof(PoolroomsTerrainSystem).GetField("worker",flags).SetValue(terrain,null);
                int epoch=(int)typeof(PoolroomsTerrainSystem).GetField("epoch",flags).GetValue(terrain);
                PoolroomsChunk previous=null;
                foreach(var chunk in world.Chunks)if(chunk.gameObject.name=="Meadow 0,0 · near")previous=chunk;
                Assert.That(previous,Is.Not.Null);
                var plan=terrain.Plan;var house=plan.House(0,0);int indices;
                var data=new RoomGeometry();data.Structure.Append(TerrainArchitecture.Ground(plan,0,0,32,true,out indices));
                data.Decoration.Append(TerrainArchitecture.Roads(plan,0,0));var geometry=TerrainHouseArchitecture.Build(house,true,plan);
                var method=typeof(PoolroomsTerrainSystem).GetMethod("UploadTile",flags);
                object[] arguments={epoch,0,0,32,true,indices,plan,house,geometry,data};
                upload=(IEnumerator)method.Invoke(terrain,arguments);
                Assert.That(upload.MoveNext(),Is.True);Assert.That(terrain.HasStagedTile,Is.True);
                Assert.That(previous.gameObject.activeInHierarchy,Is.True);
                ((System.IDisposable)upload).Dispose();upload=null;
                Assert.That(terrain.HasStagedTile,Is.False);Assert.That(previous.gameObject.activeInHierarchy,Is.True);
                yield return null;
                upload=(IEnumerator)method.Invoke(terrain,arguments);int phases=0;
                while(upload.MoveNext())
                {
                    phases++;Assert.That(previous.gameObject.activeInHierarchy,Is.True,"Old support removed during upload.");
                    foreach(var chunk in world.Chunks)Assert.That(chunk.gameObject.activeInHierarchy,Is.True,"Inactive staging entered world registry.");
                    Physics.SyncTransforms();RaycastHit hit;
                    Vector3 point=terrain.RegionRoot.TransformPoint(new Vector3(-8,plan.Height(-8,0)+1,0));
                    Assert.That(Physics.Raycast(point,Vector3.down,out hit,2,~0,QueryTriggerInteraction.Ignore),Is.True);
                    Assert.That(hit.collider.transform.IsChildOf(previous.transform),Is.True,"Uncommitted collider replaced live support.");
                    yield return null;
                }
                ((System.IDisposable)upload).Dispose();upload=null;
                Assert.That(phases,Is.GreaterThanOrEqualTo(8));Assert.That(terrain.HasStagedTile,Is.False);
                Assert.That(previous==null||!previous.gameObject.activeSelf,Is.True);
                Assert.That(terrain.ViewerReady,Is.True);
                // Exercise the real controller/audio integration on the committed terrain.
                explorer.SetPaused(false);explorer.allowSlide=true;explorer.slideSpeed=11;
                for(int frame=0;frame<10;frame++)
                {Cursor.lockState=CursorLockMode.Locked;explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);explorer.FeedSlideInput(false);yield return null;}
                for(int frame=0;frame<30;frame++)
                {Cursor.lockState=CursorLockMode.Locked;explorer.FeedInput(Vector2.up,Vector2.zero,false,false,false,false);explorer.FeedSlideInput(true);yield return null;}
                var ambient=actor.GetComponent<PoolroomsAmbientAudio>();
                Assert.That(explorer.IsSliding,Is.True);Assert.That(ambient.SlideGain,Is.GreaterThan(.02f));
                var audioRoot=world.transform.Find("Poolrooms sound (runtime)");Assert.That(audioRoot,Is.Not.Null);
                Assert.That(audioRoot.GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(ambient.SourceBudget));
                for(int voice=0;voice<4;voice++)Assert.That(audioRoot.Find("Contact voice "+voice).GetComponent<AudioSource>().isPlaying,Is.False,"Walking taps emitted during sliding.");
                Assert.That(audioRoot.Find("Continuous slide friction 0").GetComponent<AudioSource>().loop,Is.True);
                Assert.That(audioRoot.Find("Continuous slide friction 1").GetComponent<AudioSource>().loop,Is.True);
                Assert.That(Resources.Load<AudioClip>("PoolroomsAudio/SlideSoftLoop"),Is.Not.Null);
                explorer.SetPaused(true);
                terrain.Exit();
            }
            finally
            {
                if(upload is System.IDisposable disposable)disposable.Dispose();
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
                Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;
            }
        }

        [UnityTest]
        public IEnumerator SettledRoomWindowStopsRepeatingTopologySearches()
        {
            GameObject root=null,viewer=null;PoolroomsProfile profile=null;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;
                profile.zoneMode=ZoneMode.Single;profile.singleZone=0;
                root=new GameObject("v36 idle search test");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();
                world.unifiedExploration=false;world.profile=profile;world.streaming=false;
                viewer=new GameObject("idle search viewer");world.viewer=viewer.transform;root.SetActive(true);
                float deadline=Time.realtimeSinceStartup+10;
                while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);yield return null;yield return null;
                int count=world.WorkSearchCount;
                for(int i=0;i<30;i++)yield return null;
                Assert.That(world.WorkSearchCount,Is.EqualTo(count));
                world.Rebuild();while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.WorkSearchCount,Is.GreaterThan(count),"Rebuild did not invalidate settled search.");
            }
            finally
            {if(viewer!=null)Object.DestroyImmediate(viewer);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
