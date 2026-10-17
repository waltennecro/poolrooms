#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class GeneratedLiminalWorldTests
    {
        [UnityTest]
        public IEnumerator GeneratedEnvironmentsKeepDryRoutesAndTranslatedWaterQueries()
        {
            float scale=Time.timeScale;bool audioPause=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                foreach(ProceduralGrammar grammar in new[]{ProceduralGrammar.CrossV12,ProceduralGrammar.Varied,ProceduralGrammar.SolarAtlas})
                foreach(LiminalGeneration mode in new[]{LiminalGeneration.Poolrooms,LiminalGeneration.IndoorPlay,LiminalGeneration.Supermarkets,
                    LiminalGeneration.Offices,LiminalGeneration.Hotels,LiminalGeneration.WaitingHalls,LiminalGeneration.DiningHalls})
                {
                    GameObject root=null,actor=null;PoolroomsProfile profile=null;
                    try
                    {
                        profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.continuousWorld=true;
                        profile.proceduralGrammar=grammar;profile.liminalGeneration=mode;profile.authoredRoomChance=0;profile.curveSegments=12;
                        root=new GameObject("Generated liminal test");root.transform.position=new Vector3(12,6,-20);root.SetActive(false);
                        var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;
                        actor=new GameObject("Generated explorer test");actor.transform.position=root.transform.position+Vector3.up*.1f;
                        var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.34f;
                        var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.world=world;explorer.externalInput=true;
                        world.viewer=actor.transform;root.SetActive(true);
                        for(int i=0;i<240&&!world.ViewerReady;i++)yield return null;
                        Assert.That(world.ViewerReady,Is.True);
                        Room requested=world.CurrentLayout.At(-15,14);
                        explorer.Teleport(world.RoomWorldPosition(requested)+Vector3.up*.1f);
                        for(int i=0;i<240&&!world.ViewerReady;i++)yield return null;
                        Assert.That(world.ViewerReady,Is.True);
                        Room current=world.RoomAtViewer();Assert.That(current.Plan,Is.Not.Null);Assert.That(current.X,Is.EqualTo(-15));Assert.That(current.Z,Is.EqualTo(14));
                        Vector3 centre=world.RoomWorldPosition(current);float level;
                        Assert.That(world.TryGetWater(centre,out level),Is.False,"Generated circulation must be dry.");
                        Physics.SyncTransforms();RaycastHit hit;
                        Assert.That(Physics.Raycast(centre+new Vector3(.8f,1.5f,0),Vector3.down,out hit,2),Is.True);
                        Assert.That(hit.collider.transform.IsChildOf(world.GeneratedRoot),Is.True);
                        Assert.That(hit.point.y,Is.EqualTo(world.transform.position.y).Within(.02f));
                        if(current.Plan.Circulation!=null)
                        foreach(WalkingRoute route in current.Plan.Circulation.Routes)foreach(V2 point in route.Points)
                        {
                            // Endpoints lie on portals; test inset points through the actual PhysX collider.
                            Vector3 local=new Vector3(point.X,0,point.Y)*.98f;
                            Assert.That(Physics.Raycast(centre+local+Vector3.up*.12f,Vector3.down,out hit,.15f),Is.True,"Missing route floor");
                            Assert.That(hit.point.y,Is.EqualTo(world.transform.position.y).Within(.02f));
                        }
                        foreach(WaterRegion pool in current.Plan.Pools)
                        {
                            Assert.That(world.TryGetWater(centre+new Vector3(pool.CenterX,0,pool.CenterZ),out level),Is.True);
                            Assert.That(level,Is.EqualTo(world.transform.position.y+profile.waterLevel).Within(.001f));
                        }
                        Assert.That(mode==LiminalGeneration.Poolrooms?current.Plan.Pools.Length>0:current.Plan.Pools.Length==0,Is.True);
                        if(current.Plan.Interior!=null)
                        foreach(InteriorBay bay in current.Plan.Interior.Bays)
                        {
                            V3 a=bay.World(0,0,-bay.LocalDepth*.5f-.5f),b=bay.World(0,0,-bay.LocalDepth*.5f+1);
                            Vector3 start=centre+new Vector3(a.X,.05f,a.Z),end=centre+new Vector3(b.X,.05f,b.Z);
                            Assert.That(Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.5f,.32f,
                                (end-start).normalized,out hit,(end-start).magnitude,~0,QueryTriggerInteraction.Ignore),Is.False,"Blocked side room for player capsule");
                            Assert.That(Physics.Raycast(end+Vector3.up*.5f,Vector3.down,out hit,.65f),Is.True,"Missing interior floor");
                        }
                        foreach(var chunk in world.Chunks)
                        {
                            Transform decorations=chunk.transform.Find("Batched decoration");
                            if(decorations!=null)Assert.That(decorations.GetComponentsInChildren<Collider>().Length,Is.Zero);
                        }
                        Assert.That(world.CurrentLayout.CachedRoomCount,Is.LessThan(150));
                    }
                    finally
                    {
                        if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                    }
                }
            }
            finally{Time.timeScale=scale;AudioListener.pause=audioPause;Cursor.lockState=cursor;Cursor.visible=visible;}
        }
    }
}
#endif
