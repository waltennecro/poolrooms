#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class TerrainHouseVarietyTests
    {
        [Test]
        public void NativeGroundExcavationLeavesTheCellarStairOpeningClear()
        {
            var root=new GameObject("v35 cellar terrain check");
            try
            {
                var p=TerrainRegionPlan.Create(94,0,0);var h=p.House(0,0);h.SetArchitecture(5);
                foreach(int cx in new[]{0,1})
                {
                    var tile=new GameObject("Support tile "+cx);tile.transform.SetParent(root.transform,false);tile.transform.localPosition=new Vector3(cx*64,0,0);
                    var owner=tile.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
                    int indices;var m=TerrainArchitecture.Ground(p,cx,0,cx==0?8:32,false,out indices);
                    Assert.That(indices,Is.EqualTo(m.TriangleCount*3));MeshFactory.CreateCollider("Excavated land",m,tile.transform,owner);
                }
                Physics.SyncTransforms();var hole=h.Blueprint.StairHole;
                for(int z=0;z<5;z++)for(int x=0;x<5;x++)
                {
                    var position=new Vector3(h.X+hole.MinX+hole.Width*(x+.5f)/5,h.Base+1,h.Z+hole.MinZ+hole.Depth*(z+.5f)/5);
                    Assert.That(Physics.Raycast(position,Vector3.down,2),Is.False,"Ground blocks cellar opening.");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(5)]
        [TestCase(319)]
        [TestCase(799)]
        [TestCase(839)]
        [TestCase(879)]
        [TestCase(919)]
        [TestCase(959)]
        [TestCase(999)]
        [TestCase(1039)]
        [TestCase(1079)]
        [TestCase(1119)]
        [TestCase(1159)]
        [TestCase(1199)]
        public void PhysXSupportsCellarsUpperFlightsAndAttics(int id)
        {
            var root=new GameObject("v35 house collision check");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
                var h=new TerrainHousePlan{Seed=350094,Variant=2};h.SetArchitecture(id);
                var data=TerrainHouseArchitecture.Build(h,true);var b=h.Blueprint;
                var collision=MeshFactory.CreateCollider("House physics",data.Geometry.Structure,root.transform,owner);
                Assert.That(collision.GetComponent<Renderer>(),Is.Null);Physics.SyncTransforms();
                for(int level=b.Lowest;level<=b.Highest;level++)
                {
                    float y=level*TerrainHousePlan.FloorHeight;RaycastHit hit;
                    Assert.That(Physics.Raycast(new Vector3(b.StairX,y+.3f,b.StairZ-2.35f),Vector3.down,out hit,.5f),Is.True);
                    Assert.That(hit.point.y,Is.EqualTo(y).Within(.01f));
                    if(level==b.Highest)continue;
                    for(int half=0;half<2;half++)for(int step=0;step<9;step++)
                    {
                        float x=b.StairX+(half==0?-.95f:.95f);
                        float z=b.StairZ+(half==0?-1.4f+(step+.5f)*.3f:1.3f-(step+.5f)*.3f);
                        float top=y+half*1.6f+(step+1)*1.6f/9;
                        Assert.That(Physics.Raycast(new Vector3(x,top+.15f,z),Vector3.down,out hit,.3f),Is.True);
                        Assert.That(hit.point.y,Is.EqualTo(top).Within(.01f));
                        Assert.That(Physics.Raycast(new Vector3(x,top+.08f,z),Vector3.up,1.9f),Is.False);
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator ControllerWalksFromCellarToAtticAndBackWithoutTeleporting()
        {
            var root=new GameObject("v35 stacked stairs");var actor=new GameObject("v35 walker");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
                var house=new TerrainHousePlan{Seed=350094,Variant=2};house.SetArchitecture(79);var b=house.Blueprint;
                MeshFactory.CreateCollider("House physics",TerrainHouseArchitecture.Build(house,true).Geometry.Structure,root.transform,owner);
                var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.25f;body.skinWidth=.025f;
                actor.transform.position=new Vector3(b.StairX-.95f,b.Lowest*3.2f+.05f,b.StairZ-1.9f);Physics.SyncTransforms();yield return null;
                for(int level=b.Lowest;level<b.Highest;level++)
                {
                    for(int i=0;i<28;i++){body.Move(new Vector3(0,-.08f,.13f));yield return null;}
                    for(int i=0;i<15;i++){body.Move(new Vector3(1.9f/15,-.08f,0));yield return null;}
                    for(int i=0;i<28;i++){body.Move(new Vector3(0,-.08f,-.13f));yield return null;}
                    Assert.That(actor.transform.position.y,Is.EqualTo((level+1)*3.2f).Within(.12f),"Cannot reach next real floor.");
                    if(level+1<b.Highest)for(int i=0;i<15;i++){body.Move(new Vector3(-1.9f/15,-.08f,0));yield return null;}
                }
                Assert.That(house.Contains(actor.transform.position.x,actor.transform.position.y,actor.transform.position.z),Is.True);
                for(int level=b.Highest;level>b.Lowest;level--)
                {
                    for(int i=0;i<28;i++){body.Move(new Vector3(0,-.08f,.13f));yield return null;}
                    for(int i=0;i<15;i++){body.Move(new Vector3(-1.9f/15,-.08f,0));yield return null;}
                    for(int i=0;i<28;i++){body.Move(new Vector3(0,-.08f,-.13f));yield return null;}
                    Assert.That(actor.transform.position.y,Is.EqualTo((level-1)*3.2f).Within(.12f),"Cannot descend to cellar / lost floor.");
                    if(level-1>b.Lowest)for(int i=0;i<15;i++){body.Move(new Vector3(1.9f/15,-.08f,0));yield return null;}
                }
            }
            finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
        }
    }
}
#endif
