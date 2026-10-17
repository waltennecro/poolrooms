#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class TerrainRegionTests
    {
        [UnityTest]
        public IEnumerator DistrictStreamsAllSidedGroundAndHouseStairsAndRestoresInterior()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float oldTime=Time.timeScale;bool oldAudio=AudioListener.pause;var oldCursor=Cursor.lockState;bool oldVisible=Cursor.visible;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;profile.enableTerrainRegions=true;profile.terrainEntranceChance=1;profile.terrainViewRadius=2;
                profile.zoneMode=ZoneMode.Single;for(int i=0;i<ZoneCatalog.Count;i++)if(ZoneCatalog.Get(i).Pools==PoolPlan.Dry){profile.singleZone=i;break;}
                root=new GameObject("Terrain integration world");var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;world.generateOnStart=false;world.streaming=false;
                actor=new GameObject("Terrain integration explorer");var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;
                var body=actor.GetComponent<CharacterController>();body.height=1.8f;body.center=Vector3.up*.9f;body.radius=.32f;body.skinWidth=.025f;body.stepOffset=.25f;
                var eyes=new GameObject("Terrain test eyes");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.65f;
                var camera=eyes.AddComponent<Camera>();camera.farClipPlane=180;explorer.eyes=eyes.transform;explorer.world=world;world.viewer=actor.transform;
                world.BuildAllImmediate();explorer.Teleport(world.SpawnWorldPosition);yield return null;yield return null;
                var terrain=world.Terrain;Assert.That(terrain,Is.Not.Null);Assert.That(terrain.Enter(world.CurrentLayout.Rooms[0],Vector3.zero),Is.True);
                float deadline=Time.realtimeSinceStartup+40;while(!terrain.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(terrain.ViewerReady,Is.True,"Local terrain collision support did not complete.");
                while(terrain.LoadedCount<25&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(terrain.LoadedCount,Is.EqualTo(25),"The far window must still load after releasing local movement.");
                Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.False);Assert.That(camera.farClipPlane,Is.GreaterThanOrEqualTo(1800));
                foreach(var chunk in world.Chunks)Assert.That(chunk.transform.IsChildOf(terrain.RegionRoot),Is.True);
                Assert.That(world.BelowSafeFloor(actor.transform.position),Is.False);float water;Assert.That(world.TryGetWater(actor.transform.position,out water),Is.False);
                Physics.SyncTransforms();Vector3 offset=terrain.RegionRoot.position;
                foreach(float x in new[]{-5f,0,5})foreach(float z in new[]{-5f,0,5})
                {
                    RaycastHit hit;Vector3 position=offset+new Vector3(x,terrain.Plan.Height(x,z)+2,z);
                    Assert.That(Physics.Raycast(position,Vector3.down,out hit,3,~0,QueryTriggerInteraction.Ignore),Is.True,"No land on one side of portal.");
                    Assert.That(Mathf.Abs(hit.point.y-position.y+2),Is.LessThan(.05f));
                }
                explorer.Teleport(offset+new Vector3(62,terrain.Plan.Height(62,12)+.12f,12));explorer.SetPaused(true);
                for(int i=0;i<30;i++){body.Move(new Vector3(.15f,-.04f,0));yield return null;}
                Assert.That(actor.transform.position.x-offset.x,Is.GreaterThan(65),"A visual terrain skirt blocked the sector seam.");
                Vector3 local=actor.transform.position-offset;Assert.That(Mathf.Abs(local.y-terrain.Plan.Height(local.x,local.z)),Is.LessThan(.25f),"Lost ground support at sector seam.");
                var house=terrain.Plan.House(0,0);Assert.That(house,Is.Not.Null);
                Assert.That(house.Blueprint,Is.Not.Null,"Default exploration must use the varied home generator.");
                explorer.Teleport(offset+new Vector3(house.EntranceX,house.Base+.1f,house.Z-house.Depth*.5f-1.8f));explorer.SetPaused(true);
                for(int i=0;i<28;i++){body.Move(new Vector3(0,-.025f,.15f));yield return null;}
                Assert.That(actor.transform.position.z,Is.GreaterThan(offset.z+house.Z-house.Depth*.5f+1),"Front door cannot be walked through.");
                if(house.Blueprint!=null&&house.Blueprint.Highest>0)
                {
                    var b=house.Blueprint;
                    explorer.Teleport(offset+new Vector3(house.X+b.StairX-.95f,house.Base+.12f,house.Z+b.StairZ-1.9f));
                    for(int i=0;i<28;i++){body.Move(new Vector3(0,-.04f,.13f));yield return null;}
                    for(int i=0;i<15;i++){body.Move(new Vector3(1.9f/15,-.04f,0));yield return null;}
                    for(int i=0;i<28;i++){body.Move(new Vector3(0,-.04f,-.13f));yield return null;}
                    Assert.That(actor.transform.position.y-offset.y-house.Base,Is.GreaterThan(3),"Controller failed to climb new U-flight.");
                }
                else if(house.Floors==2)
                {
                    float start=-house.Depth*.5f+1.2f;
                    explorer.Teleport(offset+new Vector3(house.X+house.Width*.5f-1.3f,house.Base+.12f,house.Z+start-.5f));
                    for(int i=0;i<95;i++){body.Move(new Vector3(0,-.025f,.13f));yield return null;}
                    Assert.That(actor.transform.position.y-offset.y-house.Base,Is.GreaterThan(3),"Controller failed to climb house stairs.");
                }
                explorer.SetPaused(false);
                // Catalog pickups own resources; a held object survives destroying the district.
                var pickup=terrain.RegionRoot.GetComponentInChildren<PoolroomsPickup>();Assert.That(pickup,Is.Not.Null);
                var carry=actor.GetComponent<PoolroomsCarry>();Assert.That(carry.Grab(pickup),Is.True);string key=pickup.SpawnKey;
                var mesh=pickup.GetComponentInChildren<MeshFilter>().sharedMesh;terrain.Exit();yield return null;
                Assert.That(terrain.IsInside,Is.False);Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True);Assert.That(camera.farClipPlane,Is.EqualTo(180));
                Assert.That(carry.Held,Is.EqualTo(pickup));Assert.That(mesh,Is.Not.Null);Assert.That(carry.WasTaken(key),Is.True);
                Assert.That(terrain.Enter(world.CurrentLayout.Rooms[0],Vector3.zero),Is.True);deadline=Time.realtimeSinceStartup+40;
                while(!terrain.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;Assert.That(terrain.ViewerReady,Is.True);
                foreach(var item in terrain.RegionRoot.GetComponentsInChildren<PoolroomsPickup>())Assert.That(item.SpawnKey,Is.Not.EqualTo(key),"Taken item respawned on revisit.");
                terrain.Exit();
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=oldTime;AudioListener.pause=oldAudio;Cursor.lockState=oldCursor;Cursor.visible=oldVisible;
            }
        }
    }
}
#endif
