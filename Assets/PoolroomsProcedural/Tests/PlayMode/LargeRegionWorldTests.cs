#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class LargeRegionWorldTests
    {
        [Test]
        public void NewProfilesEnableLargeRegionsWhilePreservingStreamingRadii()
        {
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
            try
            {
                profile.continuousWorld=true;profile.liminalGeneration=LiminalGeneration.Expanded;profile.loadRadius=3;profile.unloadRadius=4;
                var config=profile.Snapshot();WalkableExploration.Configure(config);
                Assert.That(LargeRegions.Enabled(config),Is.True);Assert.That(config.LargeRegionScale,Is.EqualTo(8));
                Assert.That(profile.loadRadius,Is.EqualTo(3));Assert.That(profile.unloadRadius,Is.EqualTo(4));
                profile.largeProceduralRegions=false;config=profile.Snapshot();WalkableExploration.Configure(config);
                Assert.That(LargeRegions.Enabled(config),Is.False,"Unified exploration should preserve the size preference");
            }
            finally{Object.DestroyImmediate(profile);}
        }
        [UnityTest]
        public IEnumerator PlayerCrossesABroadHallAwayFromTheNarrowDoorAxis()
        {
            var config=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.IndoorMarina,LargeProceduralRegions=true,CellSize=28,DoorWidth=6,DoorHeight=5.1f,CurveSegments=12,ObjectAbundance=3};
            int x=0,z=0;bool found=false;
            for(int bx=-8;bx<=8&&!found;bx++)for(int bz=-8;bz<=8&&!found;bz++)if(LargeRegions.Section(config,bx*4,bz*4,LiminalEnvironment.IndoorMarina)==RegionSection.Forum){x=bx*4+1;z=bz*4+1;found=true;}
            Assert.That(found,Is.True);var layout=new Layout(config);var a=layout.At(x,z);var b=layout.At(x+1,z);
            var root=new GameObject("Broad hall physical crossing");root.transform.position=new Vector3(4000,1800,4000);var actor=new GameObject("Ordinary walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("First hall chunk",Architecture.Build(a,config).Structure,root.transform,owner);
                var next=new GameObject("Next hall chunk");next.transform.SetParent(root.transform,false);next.transform.localPosition=Vector3.right*config.CellSize;
                MeshFactory.CreateCollider("Second hall chunk",Architecture.Build(b,config).Structure,next.transform,owner);
                actor.transform.position=root.transform.position+new Vector3(config.CellSize*.5f-1,.05f,3.2f);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.3f;body.skinWidth=.025f;
                Physics.SyncTransforms();
                for(int step=0;step<60;step++){body.Move(Vector3.right*.05f-Vector3.up*.02f);if(step%15==0)yield return null;}
                Assert.That(actor.transform.position.x-root.transform.position.x,Is.GreaterThan(config.CellSize*.5f+1.7f));
                Assert.That(actor.transform.position.y-root.transform.position.y,Is.InRange(-.1f,.15f));
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
        [Test]
        public void RuntimeAcceptsMeshlessHallLinksAndOpensTheirStreamingCaps()
        {
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var root=new GameObject("Runtime open hall attachment");root.SetActive(false);var viewer=new GameObject("Ordinary hall viewer");
            try
            {
                profile.continuousWorld=true;profile.liminalGeneration=LiminalGeneration.SolarObservatory;profile.cellSize=28;profile.curveSegments=12;profile.loadRadius=1;
                profile.largeProceduralRegions=true;profile.enableTerrainRegions=false;profile.enableGiantMalls=false;profile.objectAbundance=1;
                var config=profile.Snapshot();int x=0,z=0;bool found=false;
                for(int bx=-8;bx<=8&&!found;bx++)for(int bz=-8;bz<=8&&!found;bz++)if(LargeRegions.Section(config,bx*4,bz*4,LiminalEnvironment.SolarObservatory)==RegionSection.Forum){x=bx*4+1;z=bz*4+1;found=true;}
                Assert.That(found,Is.True);viewer.transform.position=new Vector3(x*profile.cellSize,.1f,z*profile.cellSize);
                var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;world.viewer=viewer.transform;world.unifiedExploration=false;
                Assert.DoesNotThrow(()=>world.BuildAllImmediate());Assert.That(world.ViewerReady,Is.True);
                PoolroomsChunk center=null,edge=null;foreach(var chunk in world.Chunks){if(chunk.cellX==x&&chunk.cellZ==z)center=chunk;if(chunk.cellX==x+1&&chunk.cellZ==z)edge=chunk;}
                Assert.That(center,Is.Not.Null);Assert.That(center.links[1],Is.Null,"Continuous floor should not upload an empty mesh");
                Assert.That(center.caps[1],Is.Not.Null);Assert.That(center.caps[1].activeSelf,Is.False,"Loaded neighbour remained blocked by a streaming cap");
                Assert.That(edge,Is.Not.Null);Assert.That(edge.caps[1].activeSelf,Is.True);
                var cap=edge.caps[1].GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That(cap.size.z,Is.EqualTo(profile.cellSize).Within(.002f),"Unloaded hall side leaves an open gap");
                Assert.That(cap.size.y,Is.EqualTo(world.CurrentLayout.At(x+1,z).Height).Within(.002f));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(viewer);Object.DestroyImmediate(profile);}
        }
        [Test]
        public void LargeMallPlanAndReservationUseIdenticalDimensionsWithoutCoveringSpawn()
        {
            var c=new LayoutConfig{Continuous=true,LargeProceduralRegions=true};WalkableExploration.Configure(c);
            Assert.That(ConnectedMallTopology.Site(c,0,0,out var mall),Is.True);Assert.That(mall.Contains(0,0),Is.False);
            var plan=MallPlan.Create(c.Seed,mall.X,mall.Z,true,true,true,true,true,true);
            Assert.That(plan.Width,Is.EqualTo(mall.Width));Assert.That(plan.Depth,Is.EqualTo(mall.Depth));Assert.That(plan.Valid(),Is.True);
            Assert.That(plan.SectorCount,Is.LessThan(32));
        }
    }
}
#endif
