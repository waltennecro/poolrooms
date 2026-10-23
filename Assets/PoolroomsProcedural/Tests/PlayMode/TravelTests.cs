#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using Poolrooms.Core;

namespace Poolrooms.Tests
{
    public sealed class TravelTests
    {
        static GameObject Actor(out PoolroomsExplorer explorer,out PoolroomsTravel travel)
        {
            var actor=new GameObject("Travel native test actor");actor.SetActive(false);
            var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.3f;
            explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
            var eyes=new GameObject("Aim");eyes.transform.SetParent(actor.transform,false);eyes.transform.localPosition=Vector3.up*1.6f;explorer.eyes=eyes.transform;
            travel=actor.AddComponent<PoolroomsTravel>();actor.SetActive(true);return actor;
        }
        static GameObject Wall(string name,Vector3 position,Vector3 scale,bool mesh=true)
        {
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name=name;
            wall.transform.position=position;wall.transform.localScale=scale;
            if(mesh)
            {
                var data=wall.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(wall.GetComponent<BoxCollider>());
                wall.AddComponent<MeshCollider>().sharedMesh=data;wall.AddComponent<PoolroomsChunk>();
            }
            return wall;
        }
        [Test]
        public void GrappleStopsBeforeWallAndReleasingOrPausingCancelsIt()
        {
            GameObject actor=null,target=null;string old=PlayerPrefs.GetString(PoolroomsTravel.PreferenceKey,"");bool existed=PlayerPrefs.HasKey(PoolroomsTravel.PreferenceKey);
            try
            {
                actor=Actor(out var explorer,out var travel);travel.Preferences.allowGrapple=true;
                target=Wall("Architecture",new Vector3(0,4,12),new Vector3(20,10,.15f));Physics.SyncTransforms();
                travel.FeedInput(false,false,true);bool moved=false;
                for(int i=0;i<60;i++)moved|=travel.TickGrapple(.1f);
                Assert.That(moved,Is.True);Assert.That(actor.transform.position.z,Is.GreaterThan(4));
                Assert.That(actor.GetComponent<CharacterController>().bounds.max.z,Is.LessThan(target.GetComponent<Collider>().bounds.min.z));
                travel.FeedInput(false,false,false);travel.TickGrapple(.1f);Assert.That(travel.Grappling,Is.False);
                actor.transform.position=Vector3.zero;Physics.SyncTransforms();travel.FeedInput(false,false,true);Assert.That(travel.TickGrapple(.1f),Is.True);
                Vector3 before=actor.transform.position;explorer.SetPaused(true);travel.TickGrapple(.1f);
                Assert.That(travel.Grappling,Is.False);Assert.That(actor.transform.position,Is.EqualTo(before));
            }
            finally{if(actor!=null)Object.DestroyImmediate(actor);if(target!=null)Object.DestroyImmediate(target);if(existed)PlayerPrefs.SetString(PoolroomsTravel.PreferenceKey,old);else PlayerPrefs.DeleteKey(PoolroomsTravel.PreferenceKey);}
        }
        [Test]
        public void GrappleCannotSelectThroughObstacleOrInvisibleStreamingGate()
        {
            GameObject actor=null,target=null,blocker=null;
            try
            {
                actor=Actor(out var explorer,out var travel);travel.Preferences.allowGrapple=true;
                target=Wall("Architecture",new Vector3(0,4,12),new Vector3(20,10,.15f));
                blocker=Wall("Unhookable obstacle",new Vector3(0,2,3),new Vector3(3,4,.2f),false);Physics.SyncTransforms();
                travel.FeedInput(false,false,true);Assert.That(travel.TickGrapple(.1f),Is.False);Assert.That(travel.Grappling,Is.False);
                Object.DestroyImmediate(blocker);travel.FeedInput(false,false,false);travel.TickGrapple(.1f);
                target.name="Streaming gate 0";Physics.SyncTransforms();travel.FeedInput(false,false,true);
                Assert.That(travel.TickGrapple(.1f),Is.False);Assert.That(travel.Grappling,Is.False);
            }
            finally{if(actor!=null)Object.DestroyImmediate(actor);if(target!=null)Object.DestroyImmediate(target);if(blocker!=null)Object.DestroyImmediate(blocker);}
        }
        [Test]
        public void AutomaticAdvanceRefusesUnsupportedGroundAndStopsOnSuspend()
        {
            GameObject actor=null,floor=null;
            try
            {
                actor=Actor(out var explorer,out var travel);travel.Preferences.allowAutoAdvance=true;
                floor=Wall("Architecture",new Vector3(0,-.1f,0),new Vector3(4,.2f,2));Physics.SyncTransforms();
                travel.FeedInput(false,true,false);travel.PrepareInput(Vector2.zero);
                Assert.That(travel.AutoAdvance,Is.True);Assert.That(travel.CanAutoAdvance(Vector3.forward*5,.02f),Is.True);
                actor.transform.position=new Vector3(0,0,.8f);Physics.SyncTransforms();
                Assert.That(travel.CanAutoAdvance(Vector3.forward*5,.02f),Is.False,"Cruise ran across an unsupported drop.");
                travel.Suspend();Assert.That(travel.AutoAdvance,Is.False);Assert.That(travel.PrepareInput(Vector2.zero),Is.EqualTo(Vector2.zero));
            }
            finally{if(actor!=null)Object.DestroyImmediate(actor);if(floor!=null)Object.DestroyImmediate(floor);}
        }
    }
}
#endif
