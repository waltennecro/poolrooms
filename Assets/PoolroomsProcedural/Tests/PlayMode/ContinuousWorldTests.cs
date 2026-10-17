#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ContinuousWorldTests
    {
        [UnityTest]
        public IEnumerator RebaseKeepsLogicalCellWaterAndPhysXFloor()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float oldScale=Time.timeScale;bool oldAudio=AudioListener.pause;
            var oldLock=Cursor.lockState;bool oldVisible=Cursor.visible;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                profile.continuousWorld=true;profile.zoneMode=ZoneMode.Single;profile.singleZone=0;
                profile.composition=SpatialComposition.Exploration;profile.curveSegments=12;
                profile.loadRadius=1;profile.unloadRadius=2;
                root=new GameObject("Continuous physics test");root.SetActive(false);
                var world=root.AddComponent<PoolroomsWorld>();world.unifiedExploration=false;world.profile=profile;
                actor=new GameObject("Continuous test actor");actor.transform.position=Vector3.up*.1f;
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;
                body.stepOffset=.34f;body.skinWidth=.025f;body.slopeLimit=48;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.world=world;explorer.externalInput=true;
                world.viewer=actor.transform;root.SetActive(true);
                for(int frame=0;frame<240&&!world.ViewerReady;frame++)yield return null;
                Assert.That(world.ViewerReady,Is.True,"Origin failed to load.");
                int shifts=0;explorer.OriginShifted+=shift=>shifts++;
                Room requested=world.CurrentLayout.At(13,-14);
                explorer.Teleport(world.RoomWorldPosition(requested)+Vector3.up*.1f);
                for(int frame=0;frame<240&&(!world.ViewerReady||shifts==0);frame++)yield return null;
                Assert.That(world.ViewerReady,Is.True,"Negative destination failed to load.");
                Assert.That(shifts,Is.GreaterThan(0));
                Room current=world.RoomAtViewer();Assert.That(current.X,Is.EqualTo(13));Assert.That(current.Z,Is.EqualTo(-14));
                Assert.That((actor.transform.position-world.transform.position).sqrMagnitude,Is.LessThan(4));
                Vector3 centre=world.RoomWorldPosition(current);
                float level;Assert.That(world.TryGetWater(centre,out level),Is.True);
                Assert.That(level,Is.EqualTo(world.transform.position.y+profile.waterLevel).Within(.001f));
                Physics.SyncTransforms();RaycastHit hit;
                // Keep the ray outside the actor capsule so it measures the generated floor.
                Assert.That(Physics.Raycast(centre+new Vector3(.8f,1.5f,0),Vector3.down,out hit,3),Is.True);
                Assert.That(hit.collider.transform.IsChildOf(world.GeneratedRoot),Is.True);
                Assert.That(hit.point.y,Is.EqualTo(world.transform.position.y+current.Floor).Within(.04f));
                Assert.That(world.CurrentLayout.CachedRoomCount,Is.LessThan(150));
                // Return to the same global coordinates after unloading a distant area.
                Room original=world.CurrentLayout.At(0,0);uint seed=original.DetailSeed;
                explorer.Teleport(world.SpawnWorldPosition);
                for(int frame=0;frame<240&&!world.ViewerReady;frame++)yield return null;
                Assert.That(world.ViewerReady,Is.True);
                Assert.That(world.RoomAtViewer().X,Is.Zero);Assert.That(world.RoomAtViewer().Z,Is.Zero);
                Assert.That(world.RoomAtViewer().DetailSeed,Is.EqualTo(seed));
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);
                if(root!=null)Object.DestroyImmediate(root);
                if(profile!=null)Object.DestroyImmediate(profile);
                Time.timeScale=oldScale;AudioListener.pause=oldAudio;Cursor.lockState=oldLock;Cursor.visible=oldVisible;
            }
        }
    }
}
#endif
