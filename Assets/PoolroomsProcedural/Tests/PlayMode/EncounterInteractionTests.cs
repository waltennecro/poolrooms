#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    // These exercise actual component/input/physics behavior; cloud syntax checks do not execute them.
    public sealed class EncounterInteractionTests
    {
        GameObject root,actor,obstacle;
        PoolroomsProfile profile;
        PoolroomsExplorer explorer;
        PoolroomsInventory inventory;
        CursorLockMode cursor;
        bool visible;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            cursor=Cursor.lockState;visible=Cursor.visible;
            profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.columns=profile.rows=1;profile.curveSegments=8;
            root=new GameObject("Encounter world test");var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;world.generateOnStart=false;world.streaming=false;
            actor=new GameObject("Encounter explorer test");explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
            var eyes=new GameObject("Eyes");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.65f;eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;explorer.world=world;world.viewer=actor.transform;
            world.BuildAllImmediate();explorer.Teleport(world.SpawnWorldPosition);yield return null;yield return null;
            Cursor.lockState=CursorLockMode.Locked;inventory=PoolroomsInventory.For(explorer);Assert.That(explorer.IsReady,Is.True);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if(obstacle!=null)Object.DestroyImmediate(obstacle);if(actor!=null)Object.DestroyImmediate(actor);if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);
            Cursor.lockState=cursor;Cursor.visible=visible;yield return null;
        }
        [UnityTest]
        public IEnumerator FullInventoryKeepsHeldObjectAndSuccessfulStoreTransfersExactlyOnce()
        {
            for(int i=0;i<256;i++)inventory.Items.Add("catalog:"+i,1);
            // 10 starter types + first 240 catalogue types fill all 250 slots.
            Assert.That(inventory.Items.Count("catalog:255"),Is.EqualTo(System.Numerics.BigInteger.Zero));
            var item=PoolroomsPickup.CreateCatalog("test:stored",255,explorer.Aim.position,actor.transform,profile);var carry=actor.GetComponent<PoolroomsCarry>();
            Assert.That(carry.Grab(item),Is.True);Assert.That(inventory.StoreHeld(),Is.False);Assert.That(carry.Held,Is.EqualTo(item));Assert.That(item,Is.Not.Null);
            inventory.Items.Remove("tool:8",inventory.Items.Count("tool:8"));
            Assert.That(inventory.StoreHeld(),Is.True);Assert.That(carry.Held,Is.Null);Assert.That(inventory.Items.Count("catalog:255"),Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(inventory.StoreHeld(),Is.False);yield return null;Assert.That(item==null,Is.True);
        }
        [Test]
        public void FiveOnlyTogglesHotbarAndInventoryOwnsInputUntilClosed()
        {
            Assert.That(inventory.HotbarVisible,Is.False);
            inventory.HandleInput(false,true,false,false,false,-1,0,false);Assert.That(inventory.HotbarVisible,Is.True);
            inventory.HandleInput(false,true,false,false,false,-1,0,false);Assert.That(inventory.HotbarVisible,Is.False);
            inventory.HandleInput(true,false,false,false,false,-1,0,false);Assert.That(inventory.InterfaceOpen,Is.True);Assert.That(explorer.InputCaptured,Is.True);
            inventory.HandleInput(false,true,false,false,false,-1,0,false);Assert.That(inventory.HotbarVisible,Is.False,"Modal leaked hotbar key");
            inventory.HandleInput(false,false,false,false,false,-1,0,true);Assert.That(inventory.InterfaceOpen,Is.False);Assert.That(explorer.InputCaptured,Is.False);
        }
        [UnityTest]
        public IEnumerator FixtureUseRespectsWallOcclusionAndPickupKeyStillMovesIt()
        {
            // Isolate the ray from generated walls without rebuilding the test world.
            explorer.Teleport(Vector3.up*100);yield return null;var point=explorer.Aim.position+explorer.Aim.forward*1.5f;
            var item=PoolroomsPickup.CreateCatalog("test:read",0,point,root.transform,profile);
            var use=item.gameObject.AddComponent<PoolroomsInteractable>();use.activity=RoomActivity.Notice;use.key="test:notice";use.title="La próxima visita";use.note="Dejamos una taza para ti.";
            obstacle=new GameObject("Solid wall before notice");obstacle.transform.position=explorer.Aim.position+explorer.Aim.forward*.7f;obstacle.AddComponent<BoxCollider>().size=new Vector3(3,3,.1f);Physics.SyncTransforms();
            var carry=actor.GetComponent<PoolroomsCarry>();carry.Interact();Assert.That(inventory.InterfaceOpen,Is.False);Assert.That(inventory.Journal.Memories,Is.Zero);
            Object.DestroyImmediate(obstacle);obstacle=null;Physics.SyncTransforms();carry.Interact();Assert.That(inventory.InterfaceOpen,Is.True);Assert.That(inventory.Journal.Memories,Is.EqualTo(1));Assert.That(carry.Held,Is.Null);
            inventory.CloseInterface();carry.Interact(true);Assert.That(carry.Held,Is.EqualTo(item));yield return null;
        }
        [UnityTest]
        public IEnumerator SeatingRestoresControllerAndChairCollisionAfterRelocation()
        {
            explorer.Teleport(Vector3.up*100);var chair=new GameObject("Test chair");chair.transform.SetParent(root.transform,false);chair.transform.position=actor.transform.position+Vector3.forward*1.2f+Vector3.up*.4f;
            var shape=chair.AddComponent<BoxCollider>();shape.size=new Vector3(.6f,.8f,.6f);var use=chair.AddComponent<PoolroomsInteractable>();use.activity=RoomActivity.Rest;use.key="test:seat";
            Physics.SyncTransforms();var seat=actor.GetComponent<PoolroomsSeating>();Assert.That(seat.Sit(use),Is.True);Assert.That(actor.GetComponent<CharacterController>().enabled,Is.False);Assert.That(shape.enabled,Is.False);
            explorer.Teleport(Vector3.up*102);Assert.That(seat.IsSeated,Is.False);Assert.That(actor.GetComponent<CharacterController>().enabled,Is.True);Assert.That(shape.enabled,Is.True);yield return null;
        }
    }
}
#endif
