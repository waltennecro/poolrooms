#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ConnectedMallWorldTests
    {
        [UnityTest]
        public IEnumerator OldDemoAutomaticallyStreamsPhysicalMallAndKeepsExteriorVisible()
        {
            GameObject root=null,actor=null;PoolroomsProfile profile=null;
            float time=Time.timeScale;bool audio=AudioListener.pause,visible=Cursor.visible;var cursor=Cursor.lockState;
            try
            {
                Time.timeScale=1;AudioListener.pause=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.continuousWorld=false;
                profile.liminalGeneration=LiminalGeneration.Authored;profile.proceduralGrammar=ProceduralGrammar.CrossV12;
                profile.autonomousGeneration=false;profile.enableGiantMalls=false;profile.curveSegments=12;profile.loadRadius=1;
                string saved=JsonUtility.ToJson(profile);
                root=new GameObject("v28 connected world");root.transform.position=new Vector3(21,6,-17);root.SetActive(false);
                var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;
                actor=new GameObject("v28 connected explorer");actor.SetActive(false);actor.transform.position=root.transform.position+Vector3.up*.1f;
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.externalInput=true;explorer.world=world;
                var eyes=new GameObject("v28 eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;world.viewer=actor.transform;
                root.SetActive(true);actor.SetActive(true);yield return null;yield return null;
                float deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);Assert.That(world.CurrentLayout.Config.Continuous,Is.True);
                Assert.That(world.CurrentLayout.Config.Grammar,Is.EqualTo(ProceduralGrammar.SolarAtlas));Assert.That(world.CurrentLayout.Config.AutonomousGeneration,Is.True);
                Assert.That(world.CurrentLayout.Config.Procedural,Is.EqualTo(LiminalGeneration.Expanded));
                Assert.That(JsonUtility.ToJson(profile),Is.EqualTo(saved),"Automatic upgrade edited the saved profile asset.");
                ConnectedMallSite site;Assert.That(ConnectedMallTopology.Site(world.CurrentLayout.Config,0,0,out site),Is.True);
                int gateX=site.X-site.RadiusX-1;var gateway=world.CurrentLayout.At(gateX,site.Z);
                explorer.Teleport(world.CellWorldPosition(gateX,site.Z)+Vector3.up*.1f);
                deadline=Time.realtimeSinceStartup+90;
                while((!world.ViewerReady||!world.ConnectedMalls.GatewayReady(site,-1))&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady&&world.ConnectedMalls.GatewayReady(site,-1),Is.True);
                Vector3 center=world.CellWorldPosition(site.X,site.Z);RaycastHit hit;
                for(int i=0;i<=24;i++)
                {
                    V3 point=ConnectedMallArchitecture.Point(site,gateway,world.CurrentLayout.Config,i/24f);
                    Vector3 position=center+new Vector3(point.X,.1f,point.Z);
                    Assert.That(Physics.Raycast(position,Vector3.down,out hit,.2f,~0,QueryTriggerInteraction.Ignore),Is.True,"Missing physical approach floor");
                    Assert.That(hit.point.y,Is.EqualTo(world.transform.position.y).Within(.02f));
                }
                Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True);
                bool exterior=false,commercial=false;
                foreach(var chunk in world.Chunks){if(chunk.connectedMall)commercial=true;else exterior=true;}
                Assert.That(commercial&&exterior,Is.True,"HDRP/streaming cannot see both types of chunks together.");
                Assert.That(world.ConnectedMalls.VisitNearest(),Is.True);
                deadline=Time.realtimeSinceStartup+90;while(!world.ViewerReady&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(world.ViewerReady,Is.True);Assert.That(world.InsideMall,Is.True);
                Assert.That(world.ActiveMallRoot.position.y,Is.EqualTo(world.transform.position.y));
                Assert.That(world.GeneratedRoot.gameObject.activeSelf,Is.True,"Physical mall hid the shared world.");
                Assert.That(world.ActiveMallPlan.Floors,Is.InRange(2,6));
                // Floating-origin movement must keep the district at its logical address.
                long key=site.Key;explorer.Teleport(world.CellWorldPosition(site.X+5,site.Z)+Vector3.up*30);yield return null;yield return null;
                Assert.That(world.ConnectedMalls.DistrictCount,Is.GreaterThan(0));
                foreach(var chunk in world.ConnectedMalls.Chunks)
                {if(ContinuousTopology.CellKey(chunk.cellX,chunk.cellZ)!=key)continue;Assert.That(chunk.transform.parent.position.y,Is.EqualTo(world.transform.position.y));}
                Assert.That(JsonUtility.ToJson(profile),Is.EqualTo(saved));
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
