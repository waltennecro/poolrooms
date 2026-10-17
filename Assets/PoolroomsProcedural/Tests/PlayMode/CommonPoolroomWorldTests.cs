#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class CommonPoolroomWorldTests
    {
        [Test]
        public void OriginalProfileIncludesBothArchitecturesWithoutAtlasActivation()
        {
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
            try
            {
                Assert.That(profile.liminalGeneration,Is.EqualTo(LiminalGeneration.Authored));
                Assert.That(profile.proceduralGrammar,Is.EqualTo(ProceduralGrammar.CrossV12));
                var config=profile.Snapshot();config.Continuous=true;var layout=new Layout(config);int solar=0,original=0;
                for(int z=-8;z<=8;z++)for(int x=-8;x<=8;x++)
                {
                    Room room=layout.At(x,z);
                    if(room.Plan!=null){solar++;Assert.That(room.Plan.Solar,Is.Not.Null);Assert.That(room.Plan.Pools.Length,Is.GreaterThan(0));}
                    else{original++;Assert.That(room.ZoneId,Is.InRange(0,169));}
                }
                Assert.That(solar,Is.GreaterThan(50));Assert.That(original,Is.GreaterThan(50));
            }
            finally{Object.DestroyImmediate(profile);}
        }

        [UnityTest]
        public IEnumerator NormalSceneStreamsSolarAndOriginalRoomsWithCollision()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.loadRadius=1;profile.curveSegments=12;
                string saved=JsonUtility.ToJson(profile);
                root=new GameObject("Common Poolrooms v30");root.SetActive(false);
                var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;
                actor=new GameObject("Common explorer");actor.SetActive(false);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;world.viewer=actor.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                Assert.That(PoolroomArchitectureMix.Enabled(world.CurrentLayout.Config),Is.True);
                foreach(bool sunlight in new[]{true,false})
                {
                    Assert.That(world.VisitPoolClimate(sunlight),Is.True);
                    float deadline=Time.realtimeSinceStartup+90;
                    while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                    Assert.That(world.ViewerReady,Is.True,"Streamed room collider did not finish.");
                    Room room=world.RoomAtViewer();Assert.That(room,Is.Not.Null);
                    Assert.That(room.Environment,Is.EqualTo(LiminalEnvironment.Poolrooms));
                    Assert.That(room.Plan!=null&&room.Plan.Solar!=null,Is.EqualTo(sunlight));
                    PoolroomsChunk current=null;
                    foreach(var chunk in world.Chunks)if(chunk.cellX==room.X&&chunk.cellZ==room.Z&&!chunk.connectedMall){current=chunk;break;}
                    Assert.That(current,Is.Not.Null);Assert.That(current.GetComponentsInChildren<MeshCollider>().Length,Is.GreaterThan(0));
                    Physics.SyncTransforms();RaycastHit hit;
                    Assert.That(Physics.Raycast(actor.transform.position+Vector3.up,Vector3.down,out hit,4,~0,QueryTriggerInteraction.Ignore),Is.True,"Arrival has no supporting floor.");
                    if(sunlight)
                    {
                        int beams=0;foreach(var lamp in current.GetComponentsInChildren<PoolroomsLuminaire>(true))if(lamp.solarBeam)beams++;
                        Assert.That(beams,Is.GreaterThan(0),"Solar architecture did not create runtime daylight sources.");
                        Assert.That(room.Plan.Solar.Apertures.Length,Is.GreaterThan(0));
                    }
                    else Assert.That(room.ZoneId,Is.InRange(0,169));
                    Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True);
                    Assert.That(JsonUtility.ToJson(profile),Is.EqualTo(saved),"Mix edited the saved profile.");
                }
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
