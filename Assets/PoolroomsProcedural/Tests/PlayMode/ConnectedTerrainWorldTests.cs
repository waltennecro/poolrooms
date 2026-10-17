#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ConnectedTerrainWorldTests
    {
        [UnityTest]
        public IEnumerator DisabledConnectedDistrictsDoNotFallBackToHiddenPortalMaps()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.connectedTerrainShare=0;profile.connectedMallShare=0;profile.curveSegments=12;
                root=new GameObject("disabled connected districts");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;
                actor=new GameObject("disabled district explorer");actor.SetActive(false);var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;world.viewer=actor.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                Assert.That(world.Terrain.ConnectedMode,Is.False);Assert.That(world.Terrain.VisitNearest(),Is.False);
                Assert.That(world.Terrain.Enter(world.CurrentLayout.Rooms[0],Vector3.zero),Is.False);
                Assert.That(world.Malls.VisitNearest(),Is.False);Assert.That(world.Malls.Enter(world.CurrentLayout.Rooms[0]),Is.False);
                Assert.That(world.Terrain.HasEntrance(world.CurrentLayout.Rooms[0]),Is.False);
                Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True);Assert.That(world.Terrain.RegionRoot,Is.Null);
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=time;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }

        [UnityTest]
        public IEnumerator TerrainSharesMapAndCanBeEnteredAndLeftByWalking()
        {
            GameObject root=null,actor=null;PoolroomsProfile savedProfile=null;
            float oldTime=Time.timeScale;bool oldAudio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                savedProfile=ScriptableObject.CreateInstance<PoolroomsProfile>();savedProfile.continuousWorld=false;savedProfile.enableTerrainRegions=false;
                savedProfile.curveSegments=12;savedProfile.loadRadius=1;savedProfile.terrainViewRadius=2;string saved=JsonUtility.ToJson(savedProfile);
                root=new GameObject("Connected v29 world");root.SetActive(false);root.transform.position=new Vector3(18,5,-7);
                var world=root.AddComponent<PoolroomsWorld>();world.profile=savedProfile;
                actor=new GameObject("Connected v29 explorer");actor.SetActive(false);actor.transform.position=root.transform.position+Vector3.up*.1f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("eyes");eyes.transform.SetParent(actor.transform,false);var camera=eyes.AddComponent<Camera>();camera.farClipPlane=180;explorer.eyes=eyes.transform;world.viewer=actor.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                Assert.That(world.CurrentLayout.Config.ConnectedTerrain,Is.True);Assert.That(world.Terrain,Is.Not.Null);
                Assert.That(world.Terrain.VisitNearest(),Is.True);
                float deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True,"Terrain collision did not finish near player.");
                var terrain=world.Terrain;Assert.That(terrain.IsConnected&&terrain.IsInside,Is.True);
                Assert.That(terrain.RegionRoot.position.y,Is.EqualTo(root.transform.position.y));
                Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True,"Terrain hid ordinary rooms.");
                Assert.That(camera.farClipPlane,Is.GreaterThanOrEqualTo(1800));
                Assert.That(terrain.RegionRoot.GetComponentInChildren<PoolroomsTerrainPortal>(),Is.Null,"Walking must not use a teleport plane.");
                Assert.That(terrain.RegionRoot.GetComponentInChildren<PoolroomsPickup>(),Is.Not.Null,"Furnished houses lost interactive items.");
                Assert.That(JsonUtility.ToJson(savedProfile),Is.EqualTo(saved));
                var site=terrain.ConnectedSite;long key=site.Key;int d=0;
                Room gate=world.CurrentLayout.At(site.GateX(d),site.GateZ(d));
                explorer.Teleport(world.CellWorldPosition(gate.X,gate.Z)+Vector3.up*.1f);explorer.SetPaused(true);
                deadline=Time.realtimeSinceStartup+90;
                while((!world.ViewerReady||!terrain.GatewayReady(site,gate.X,gate.Z))&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady&&terrain.GatewayReady(site,gate.X,gate.Z),Is.True,"Threshold released without landscape support.");
                yield return null;Physics.SyncTransforms();
                for(int i=0;i<=16;i++)
                {
                    V3 p=ConnectedTerrainArchitecture.Point(site,gate,world.CurrentLayout.Config,i/16f);
                    RaycastHit hit;Vector3 origin=terrain.RegionRoot.TransformPoint(new Vector3(p.X,.1f,p.Z));
                    Assert.That(Physics.Raycast(origin,Vector3.down,out hit,.2f,~0,QueryTriggerInteraction.Ignore),Is.True,"Threshold floor missing.");
                }
                var body=actor.GetComponent<CharacterController>();
                for(int i=0;i<140;i++){body.Move(new Vector3(0,-.025f,-.2f));yield return null;}
                Assert.That(terrain.IsInside,Is.True,"Controller cannot walk out into the meadow.");
                Assert.That(terrain.ConnectedSite.Key,Is.EqualTo(key));Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True);
                Vector3 local=terrain.RegionRoot.InverseTransformPoint(actor.transform.position);
                Assert.That(local.z,Is.GreaterThan(site.HalfDepth-24),"Walking unexpectedly relocated the player.");
                bool indoor=false,outdoor=false;
                foreach(var chunk in world.Chunks){if(chunk.GetComponent<PoolroomsTerrainChunk>()!=null)outdoor=true;else if(!chunk.connectedMall)indoor=true;}
                Assert.That(indoor&&outdoor,Is.True,"Scene systems cannot see both generators.");
                for(int i=0;i<140;i++){body.Move(new Vector3(0,-.025f,.2f));yield return null;}
                Assert.That(terrain.IsInside,Is.False);Assert.That(camera.farClipPlane,Is.EqualTo(180));
                Assert.That(Vector3.Distance(actor.transform.position,world.CellWorldPosition(gate.X,gate.Z)),Is.LessThan(1),"Exit returned to spawn instead of crossing the same doorway.");
                Assert.That(JsonUtility.ToJson(savedProfile),Is.EqualTo(saved));
                // The same logical destination is revisited, independent of generation order.
                explorer.SetPaused(false);Assert.That(terrain.VisitNearest(),Is.True);
                deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);Assert.That(terrain.ConnectedSite.Key,Is.EqualTo(key));
                Assert.That(Vector3.Distance(terrain.RegionRoot.position,world.CellWorldPosition(site.X,site.Z)),Is.LessThan(.01f),"Floating origin desynchronized land and rooms.");
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(savedProfile!=null)Object.DestroyImmediate(savedProfile);
                Time.timeScale=oldTime;AudioListener.pause=oldAudio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
