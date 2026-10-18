#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class WalkableExplorationWorldTests
    {
        [UnityTest]
        public IEnumerator UnifiedWorldUpgradesOldDisabledProfileWithoutEditingSource()
        {
            var source=ScriptableObject.CreateInstance<PoolroomsProfile>();source.zoneMode=ZoneMode.Random;source.continuousWorld=false;source.liminalGeneration=LiminalGeneration.Authored;
            source.enableRainHouses=source.enableAquaticPark=source.enableThermalBaths=source.enableOrangerie=false;
            source.enableQuietLevels=source.enableCivicLevels=source.enableCity11=source.enableSoftPlay=false;
            source.connectedTerrainGeneration=source.connectedMallGeneration=source.enableTerrainRegions=false;source.curveSegments=16;
            source.rainHouseShare=source.aquaticParkShare=source.thermalBathShare=source.orangerieShare=source.quietLevelShare=source.civicLevelShare=source.city11Share=source.softPlayShare=0;
            var root=new GameObject("Unified migration without administrator");root.SetActive(false);var actor=new GameObject("Ordinary viewer");
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.profile=source;world.unifiedExploration=true;world.viewer=actor.transform;root.SetActive(true);yield return null;
                Assert.That(world.CurrentLayout,Is.Not.Null);var config=world.CurrentLayout.Config;
                Assert.That(config.DiscoveryCoverage,Is.True);Assert.That(config.Procedural,Is.EqualTo(LiminalGeneration.Expanded));
                foreach(LiminalEnvironment e in System.Enum.GetValues(typeof(LiminalEnvironment)))Assert.That(WalkableExploration.Enabled(config,e),Is.True,"Environment disabled: "+e);
                Assert.That(ConnectedMallTopology.Enabled(config)&&ConnectedTerrainTopology.Enabled(config),Is.True);
                Assert.That(world.profile,Is.Not.SameAs(source));Assert.That(source.continuousWorld||source.enableRainHouses||source.connectedMallGeneration,Is.False,"Migration changed the source profile asset");
                Assert.That(actor.GetComponent<PoolroomsMasterMode>(),Is.Null,"Administrator mode was required to include levels");
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(actor);Object.DestroyImmediate(source);}
        }
        [UnityTest]
        public IEnumerator CharacterWalksAcrossDirectRainThresholdWithoutTeleport()
        {
            foreach(int side in new[]{0,1})
            {
                var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.RainHouses,CurveSegments=16};var a=new Room{Exits=15};var b=new Room{Exits=15};
                RainHousePlanner.DescribeRecipe(a,c,0);RainHousePlanner.DescribeRecipe(b,c,2499);var ag=RainHouseArchitecture.Build(a,c);var bg=RainHouseArchitecture.Build(b,c);var link=Corridors.Build(a,b,side,c);
                var root=new GameObject("Native shared rain threshold");root.transform.position=new Vector3(31000,1400,31000);var actor=new GameObject("Walking without admin");
                var direction=side==0?Vector3.forward:Vector3.right;
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("First walls",ag.Structure,root.transform,owner);MeshFactory.CreateCollider("First roofs",ag.CollisionOnly,root.transform,owner);MeshFactory.CreateCollider("Shared threshold",link.Structure,root.transform,owner);
                    var next=new GameObject("Adjacent sector");next.transform.SetParent(root.transform,false);next.transform.localPosition=direction*c.CellSize;
                    MeshFactory.CreateCollider("Next walls",bg.Structure,next.transform,owner);MeshFactory.CreateCollider("Next roofs",bg.CollisionOnly,next.transform,owner);
                    actor.transform.position=root.transform.position+direction*(c.CellSize*.5f-1)+Vector3.up*.05f;
                    var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.3f;body.minMoveDistance=0;Physics.SyncTransforms();
                    for(int i=0;i<60;i++){body.Move(direction*.05f-Vector3.up*.02f);if(i%15==0)yield return null;}
                    Assert.That(Vector3.Dot(actor.transform.position-root.transform.position,direction),Is.GreaterThan(c.CellSize*.5f+1.7f));
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.InRange(-.1f,.15f));
                }
                finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
            }
        }
    }
}
#endif
