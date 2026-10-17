#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class MallCarryTests
    {
        [UnityTest]
        public IEnumerator MallStreamsDryFloorsAndCharacterControllerClimbsOneLevel()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float scale=Time.timeScale;bool audio=AudioListener.pause;var cursor=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;
                profile.liminalGeneration=LiminalGeneration.Supermarkets;profile.proceduralGrammar=ProceduralGrammar.Varied;
                profile.enableGiantMalls=true;profile.mallEntranceChance=1;profile.curveSegments=12;
                root=new GameObject("Mall integration test");var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;
                world.profile=profile;world.generateOnStart=false;world.streaming=false;
                actor=new GameObject("Mall explorer");var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var body=actor.GetComponent<CharacterController>();body.height=1.8f;body.center=Vector3.up*.9f;body.radius=.32f;body.skinWidth=.025f;body.stepOffset=.25f;
                var eyes=new GameObject("Test eyes");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.65f;eyes.AddComponent<Camera>();
                explorer.eyes=eyes.transform;explorer.world=world;world.viewer=actor.transform;
                world.BuildAllImmediate();explorer.Teleport(world.SpawnWorldPosition);yield return null;yield return null;
                var malls=world.Malls;Assert.That(malls,Is.Not.Null);Assert.That(malls.Enter(world.CurrentLayout.Rooms[0]),Is.True);
                float deadline=Time.realtimeSinceStartup+25;
                while(!malls.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(malls.ViewerReady,Is.True,"Mall sector did not finish loading.");
                Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.False,"Exterior rooms must not appear over the skylight.");
                foreach(var chunk in world.Chunks)Assert.That(chunk.transform.IsChildOf(malls.InteriorRoot),Is.True,"HDRP must not capture exterior chunks during a mall visit.");
                Assert.That(malls.Plan.Floors,Is.InRange(2,6));Assert.That(world.BelowSafeFloor(actor.transform.position),Is.False);
                float water;Assert.That(world.TryGetWater(actor.transform.position,out water),Is.False);
                Physics.SyncTransforms();
                Vector3 origin=malls.InteriorRoot.position;RaycastHit hit;
                Assert.That(Physics.Raycast(origin+new Vector3(0,1,-18),Vector3.down,out hit,2),Is.True);
                Assert.That(Mathf.Abs(hit.point.y-origin.y),Is.LessThan(.03f));
                // Check the actual PhysX stair surface, then walk both flights with the controller.
                float x1=malls.Plan.ShopHalf+7,x2=malls.Plan.ShopHalf+13;
                Assert.That(Physics.Raycast(origin+new Vector3(x1,2,-6.1f),Vector3.down,out hit,2),Is.True);
                Assert.That(Mathf.Abs(hit.point.y-origin.y-MallPlan.FloorHeight*.25f),Is.LessThan(.05f));
                explorer.Teleport(origin+new Vector3(x1,.1f,-13));explorer.SetPaused(true);
                for(int i=0;i<66;i++){body.Move(new Vector3(0,-.03f,.2f));yield return null;}
                Assert.That(actor.transform.position.y-origin.y,Is.GreaterThan(2.5f));
                for(int i=0;i<30;i++){body.Move(new Vector3(.2f,-.02f,0));yield return null;}
                Assert.That(Mathf.Abs(actor.transform.position.x-origin.x-x2),Is.LessThan(.3f));
                for(int i=0;i<74;i++){body.Move(new Vector3(0,-.03f,-.2f));yield return null;}
                Assert.That(actor.transform.position.y-origin.y,Is.GreaterThan(5.25f));
                explorer.SetPaused(false);malls.Exit();
                Assert.That(malls.IsInside,Is.False);Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True);Assert.That(actor.transform.position.y,Is.GreaterThan(-1));
                Assert.That(world.CurrentLayout.Rooms[0].Environment,Is.EqualTo(LiminalEnvironment.Supermarket));
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=scale;AudioListener.pause=audio;Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }

        [UnityTest]
        public IEnumerator RayGrabIsBlockedByWallsAndHeldResourcesSurviveSectorUnload()
        {
            GameObject actor=null,sector=null,wall=null;PoolroomsProfile profile=null;
            var cursor=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();actor=new GameObject("Carry test");actor.transform.position=Vector3.up*100;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var eyes=new GameObject("Test eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                yield return null;yield return null;
                sector=new GameObject("Disposable sector");sector.transform.position=eyes.transform.position;
                var item=PoolroomsPickup.Create("test:snack",Vector3.forward*1.5f,sector.transform,profile);
                wall=new GameObject("Occluding wall");wall.transform.position=eyes.transform.position+Vector3.forward*.8f;
                wall.AddComponent<BoxCollider>().size=new Vector3(3,3,.1f);Physics.SyncTransforms();
                var carry=actor.GetComponent<PoolroomsCarry>();carry.Interact();Assert.That(carry.Held,Is.Null);
                Object.DestroyImmediate(wall);wall=null;Physics.SyncTransforms();carry.Interact();Assert.That(carry.Held,Is.EqualTo(item));
                Assert.That(carry.WasTaken("test:snack"),Is.True);Assert.That(item.GetComponent<BoxCollider>().enabled,Is.False);
                var mesh=item.GetComponentInChildren<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(sector);sector=null;yield return null;
                Assert.That(item,Is.Not.Null);Assert.That(mesh,Is.Not.Null);Assert.That(item.transform.parent,Is.EqualTo(eyes.transform));
                Assert.That(carry.Grab(PoolroomsPickup.Create("test:second",Vector3.forward,eyes.transform,profile)),Is.False);
                Quaternion rotation=item.transform.localRotation;Assert.That(carry.Inspect(new Vector2(50,25)),Is.True);
                yield return null;Assert.That(Quaternion.Angle(rotation,item.transform.localRotation),Is.GreaterThan(.01f));
                explorer.SetPaused(true);Assert.That(carry.Inspect(new Vector2(50,25)),Is.False);Assert.That(carry.Drop(),Is.False);explorer.SetPaused(false);
                wall=new GameObject("Close release wall");wall.transform.position=eyes.transform.position+Vector3.forward*.2f;
                wall.AddComponent<BoxCollider>().size=new Vector3(3,3,.08f);Physics.SyncTransforms();
                Assert.That(carry.Drop(),Is.False);Assert.That(carry.Held,Is.EqualTo(item));Object.DestroyImmediate(wall);wall=null;Physics.SyncTransforms();
                Assert.That(carry.Drop(),Is.True);Assert.That(carry.Held,Is.Null);Assert.That(item.GetComponent<Rigidbody>().isKinematic,Is.False);
                Assert.That(item.GetComponent<BoxCollider>().enabled,Is.True);Assert.That(carry.WasTaken("test:snack"),Is.True);
            }
            finally
            {
                if(wall!=null)Object.DestroyImmediate(wall);if(sector!=null)Object.DestroyImmediate(sector);if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);
                Cursor.lockState=cursor;Cursor.visible=visible;
            }
        }
    }
}
#endif
