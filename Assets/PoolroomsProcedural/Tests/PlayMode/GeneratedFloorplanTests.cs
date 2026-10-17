#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class GeneratedFloorplanTests
    {
        [Test]
        public void RearWingRoutesHaveNativeCapsuleClearanceAndFloorSupport()
        {
            foreach(var environment in new[]{LiminalEnvironment.Office,LiminalEnvironment.Hotel,LiminalEnvironment.WaitingHall,LiminalEnvironment.DiningHall})
            foreach(float cell in new[]{36f,60f})
            {
                var config=new LayoutConfig{CellSize=cell,DoorWidth=4.4f,DoorHeight=5,AutonomousGeneration=true};
                Room room=null;
                for(int seed=0;seed<128;seed++)
                {
                    config.Seed=seed;room=new Room{X=17,Z=-23,Environment=environment,Exits=15};InteriorPlanner.Describe(room,config);
                    if(room.Plan.Interior.Galleries.Length>0)break;
                }
                Assert.That(room.Plan.Interior.Galleries.Length,Is.GreaterThan(0),"No seeded rear wing found.");
                var root=new GameObject("Generated gallery physics check");root.transform.position=new Vector3(10000,1000,10000);
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
                    MeshFactory.CreateCollider("Interior physics",Architecture.Build(room,config).Structure,root.transform,owner);Physics.SyncTransforms();
                    foreach(var route in room.Plan.Interior.AccessRoutes)for(int i=1;i<route.Points.Length;i++)
                    {
                        V2 a=route.Points[i-1],b=route.Points[i];var start=root.transform.position+new Vector3(a.X,.05f,a.Y);
                        var end=root.transform.position+new Vector3(b.X,.05f,b.Y);var delta=end-start;RaycastHit hit;
                        Assert.That(Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.5f,.32f,delta.normalized,
                            out hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore),Is.False,"Gallery or room door obstructs the capsule.");
                        for(int k=0;k<=4;k++)
                        {
                            var position=Vector3.Lerp(start,end,k/4f);
                            Assert.That(Physics.Raycast(position+Vector3.up*.1f,Vector3.down,out hit,.3f),Is.True,"Route floor is missing.");
                            Assert.That(hit.point.y,Is.EqualTo(root.transform.position.y).Within(.015f));
                        }
                    }
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
        [Test]
        public void GeneratedHouseStairsShareTheNativeTerrainExcavation()
        {
            var region=TerrainRegionPlan.Create(94,0,0);TerrainHousePlan house=null;
            foreach(var candidate in region.Houses)if(candidate.Blueprint.Basements>0&&candidate.CellX!=0&&candidate.CellZ!=0){house=candidate;break;}
            Assert.That(house,Is.Not.Null);Assert.That(house.Blueprint.Generated,Is.True);
            var root=new GameObject("Generated home and land physics check");root.transform.position=new Vector3(10000,1000,10000);
            try
            {
                int cx=house.CellX,cz=house.CellZ;
                var owner=root.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
                var tile=new GameObject("Support terrain");tile.transform.SetParent(root.transform,false);tile.transform.localPosition=new Vector3(cx*64,0,cz*64);
                var tileOwner=tile.AddComponent<PoolroomsChunk>();tileOwner.waterRegions=new WaterRegion[0];
                int support;var ground=TerrainArchitecture.Ground(region,cx,cz,32,true,out support);
                MeshFactory.CreateCollider("Excavated ground",ground,tile.transform,tileOwner,support);
                var home=new GameObject("Home");home.transform.SetParent(root.transform,false);home.transform.localPosition=new Vector3(house.X,house.Base,house.Z);
                MeshFactory.CreateCollider("House physics",TerrainHouseArchitecture.Build(house,true).Geometry.Structure,home.transform,owner);Physics.SyncTransforms();
                var b=house.Blueprint;
                for(int level=b.Lowest;level<b.Highest;level++)for(int half=0;half<2;half++)for(int step=0;step<9;step++)
                {
                    float x=b.StairX+(half==0?-.95f:.95f),z=b.StairZ+(half==0?-1.4f+(step+.5f)*.3f:1.3f-(step+.5f)*.3f);
                    float y=level*3.2f+half*1.6f+(step+1)*1.6f/9;RaycastHit hit;
                    var point=home.transform.position+new Vector3(x,y,z);
                    Assert.That(Physics.Raycast(point+Vector3.up*.1f,Vector3.down,out hit,.2f),Is.True);
                    Assert.That(hit.point.y,Is.EqualTo(point.y).Within(.015f));
                    Assert.That(Physics.Raycast(point+Vector3.up*.08f,Vector3.up,1.9f),Is.False,"Land or upper flight blocks the stairs.");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
