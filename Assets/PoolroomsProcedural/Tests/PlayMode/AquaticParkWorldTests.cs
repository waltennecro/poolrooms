#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class AquaticParkWorldTests
    {
        [UnityTest]
        public IEnumerator GuidedRidePausesAndNoclipRestoresControllerOwnership()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            string key=PoolroomsGameSettings.PreferencesKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
            float scale=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            int rate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            try
            {
                PlayerPrefs.DeleteKey(key);Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.continuousWorld=true;profile.liminalGeneration=LiminalGeneration.AquaticPark;
                profile.connectedTerrainGeneration=false;profile.connectedMallGeneration=false;profile.enableTerrainRegions=false;profile.curveSegments=16;
                root=new GameObject("Aquatic interaction world");root.SetActive(false);var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;
                actor=new GameObject("Aquatic interaction explorer");actor.SetActive(false);actor.transform.position=Vector3.up*.05f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;explorer.world=world;
                var body=actor.GetComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.34f;
                var eyes=new GameObject("Aquatic test camera");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.65f;eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                world.viewer=actor.transform;root.SetActive(true);actor.SetActive(true);
                for(int frame=0;frame<600&&!world.ViewerReady;frame++)yield return null;
                Assert.That(world.ViewerReady,Is.True);yield return null;yield return null;
                var room=world.RoomAtViewer();var slide=room.Plan.Aquatic.Slides[0];var start=room.Plan.Aquatic.DeckApproach(slide);
                explorer.Teleport(world.RoomWorldPosition(room)+new Vector3(start.X,start.Y,start.Z));
                for(int frame=0;frame<30;frame++){Cursor.lockState=CursorLockMode.Locked;yield return null;}
                var traversal=actor.GetComponent<PoolroomsAquaticTraversal>();Assert.That(traversal.TryInteract(),Is.True,"Supported deck cannot activate ride");
                Assert.That(body.enabled,Is.False);explorer.SetPaused(true);var at=actor.transform.position;yield return null;yield return null;
                Assert.That(actor.transform.position,Is.EqualTo(at),"Paused ride still moves");explorer.SetPaused(false);
                var effects=eyes.GetComponent<PoolroomsCameraEffects>();effects.enabled=false;Cursor.lockState=CursorLockMode.Locked;yield return null;
                Assert.That(eyes.transform.localPosition.y,Is.LessThan(1.1f),"Disabled optics removed seated camera pose");
                var master=explorer.MasterMode;master.Submit("/unlock mastermode");master.SetNoclip(true);master.CloseInterface();
                Assert.That(traversal.Riding,Is.False);Assert.That(body.enabled,Is.False,"Ride overwrote noclip controller ownership");
                master.SetNoclip(false);Assert.That(body.enabled,Is.True,"Leaving noclip retained disabled ride controller");
                Assert.That(actor.transform.position.y,Is.GreaterThan(world.RoomWorldPosition(room).y+room.Plan.Aquatic.TowerHeight-.2f),"Mode change sent rider to origin");
                master.DisableMasterMode();
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
                if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
                Time.timeScale=scale;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;Application.targetFrameRate=rate;QualitySettings.vSyncCount=vsync;
            }
        }
        [UnityTest]
        public IEnumerator TwoFlightTowerCanBeClimbedByStandingPlayer()
        {
            var cfg=new LayoutConfig{CellSize=28,DoorWidth=6,Continuous=true,Procedural=LiminalGeneration.AquaticPark,CurveSegments=16};
            var room=new Room{X=0,Z=0,Exits=15};AquaticParkPlanner.DescribeRecipe(room,cfg,4200,orientationOverride:0);var p=room.Plan.Aquatic;var geometry=AquaticParkArchitecture.Build(room,cfg);
            var root=new GameObject("Aquatic native stairs");root.transform.position=new Vector3(24000,2000,24000);var actor=new GameObject("Aquatic stair walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Park architecture",geometry.Structure,root.transform,owner);MeshFactory.CreateCollider("Flume walls including glass",geometry.CollisionOnly,root.transform,owner);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.34f;body.minMoveDistance=0;
                float z=-p.Half+2.2f;
                body.enabled=false;actor.transform.position=root.transform.position+new Vector3(p.Half-1.35f,.04f,z-.4f);body.enabled=true;Physics.SyncTransforms();
                int forward=Mathf.CeilToInt((p.FlightRun+.95f)/.04f);
                for(int i=0;i<forward;i++){body.Move(new Vector3(0,-.02f,.04f));if(i%12==0)yield return null;}
                Assert.That(actor.transform.position.y-root.transform.position.y,Is.EqualTo(p.TowerHeight*.5f).Within(.12f),"Standing player cannot climb first flight");
                for(int i=0;i<36;i++){body.Move(new Vector3(-.04f,-.02f,0));if(i%12==0)yield return null;}
                for(int i=0;i<forward+8;i++){body.Move(new Vector3(0,-.02f,-.04f));if(i%12==0)yield return null;}
                Assert.That(actor.transform.position.y-root.transform.position.y,Is.EqualTo(p.TowerHeight).Within(.12f),"Switchback landing or second flight blocks player");
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator ExitCrossHasStandingCapsuleClearanceAndWaterHasActualBottom()
        {
            var cfg=new LayoutConfig{CellSize=28,DoorWidth=6,Continuous=true,Procedural=LiminalGeneration.AquaticPark,CurveSegments=16};
            for(int style=0;style<8;style++)
            {
                var room=new Room{X=0,Z=0,Exits=15};AquaticParkPlanner.DescribeRecipe(room,cfg,style*7);var data=AquaticParkArchitecture.Build(room,cfg);
                var root=new GameObject("Aquatic native clearance");root.transform.position=new Vector3(25000,2000,25000);
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Park supports",data.Structure,root.transform,owner);MeshFactory.CreateCollider("Flume walls including glass",data.CollisionOnly,root.transform,owner);Physics.SyncTransforms();
                    foreach(var axis in new[]{Vector3.forward,Vector3.right})for(float t=-13;t<=13;t+=.5f)
                    {
                        var feet=root.transform.position+axis*t+Vector3.up*.06f;
                        Assert.That(Physics.CheckCapsule(feet+Vector3.up*.32f,feet+Vector3.up*1.48f,.3f,~0,QueryTriggerInteraction.Ignore),Is.False,"Standing route blocked by attraction");
                    }
                    var pool=room.Plan.Aquatic.Pools[0];
                    Assert.That(Physics.Raycast(root.transform.position+new Vector3(pool.CenterX,2,pool.CenterZ),Vector3.down,out var hit,4),Is.True);
                    Assert.That(hit.point.y-root.transform.position.y,Is.EqualTo(pool.Bottom).Within(.025f),"Receiving pool has hidden floor");
                    Assert.That(data.Fixtures.Count,Is.Zero,"Solar-only park cannot contain fake fill lamps");
                    yield return null;
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
    }
}
#endif
