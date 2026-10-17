#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class SolarStreamingTests
    {
        [UnityTest]
        public IEnumerator BroadDoorsKeepCorrectCapsAndSunlightUsesExistingBudgets()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float scale=Time.timeScale;bool paused=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.continuousWorld=true;profile.proceduralGrammar=ProceduralGrammar.SolarAtlas;
                profile.liminalGeneration=LiminalGeneration.Poolrooms;profile.authoredRoomChance=0;profile.sunlitRegionShare=1;profile.directRoomJoinChance=1;
                profile.curveSegments=12;profile.maxActiveLights=8;profile.maxShadowLights=2;
                root=new GameObject("Solar atlas streaming test");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;
                actor=new GameObject("Solar atlas explorer");actor.transform.position=Vector3.up*.1f;
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.34f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.world=world;explorer.externalInput=true;world.viewer=actor.transform;root.SetActive(true);
                int chunks=0;
                for(int frame=0;frame<600;frame++)
                {chunks=0;foreach(var chunk in world.Chunks)chunks++;if(world.ViewerReady&&chunks>=9)break;yield return null;}
                Assert.That(world.ViewerReady,Is.True);Assert.That(chunks,Is.GreaterThanOrEqualTo(9));
                yield return new WaitForSecondsRealtime(.25f);
                var loaded=new HashSet<long>();foreach(var chunk in world.Chunks)loaded.Add(ContinuousTopology.CellKey(chunk.cellX,chunk.cellZ));
                int caps=0,beams=0,active=0,shadows=0;
                foreach(var chunk in world.Chunks)
                {
                    Room r=world.CurrentLayout.At(chunk.cellX,chunk.cellZ);Assert.That(r.Plan.Solar,Is.Not.Null);
                    for(int d=0;d<4;d++)if(r.HasExit(d))
                    {
                        caps++;GameObject cap=chunk.caps[d];Assert.That(cap,Is.Not.Null);Assert.That(cap.GetComponent<MeshCollider>(),Is.Not.Null);
                        Bounds bounds=cap.GetComponent<MeshFilter>().sharedMesh.bounds;
                        Assert.That(d%2==0?bounds.size.x:bounds.size.z,Is.EqualTo(SolarConnections.DoorWidth(r,d,world.CurrentLayout.Config)).Within(.002f));
                        Assert.That(bounds.size.y,Is.EqualTo(SolarConnections.DoorHeight(r,d,world.CurrentLayout.Config)-r.Floor).Within(.002f));
                        bool neighbour=loaded.Contains(ContinuousTopology.CellKey(r.X+Directions.DX[d],r.Z+Directions.DZ[d]));Assert.That(cap.activeSelf,Is.EqualTo(!neighbour));
                    }
                }
                foreach(PoolroomsLuminaire lamp in world.Luminaires)
                {
                    if(lamp.solarBeam){beams++;Assert.That(lamp.source.type,Is.EqualTo(LightType.Spot));Assert.That(lamp.transform.forward.y,Is.LessThan(0));}
                    if(lamp.source.enabled&&lamp.gameObject.activeInHierarchy){active++;if(lamp.source.shadows!=LightShadows.None)shadows++;}
                }
                Assert.That(caps,Is.GreaterThan(0));Assert.That(beams,Is.GreaterThan(0));Assert.That(active,Is.LessThanOrEqualTo(profile.maxActiveLights));Assert.That(shadows,Is.LessThanOrEqualTo(profile.maxShadowLights));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=scale;AudioListener.pause=paused;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
