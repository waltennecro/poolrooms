#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class SourceFurnitureWorldTests
    {
        [Test]
        public void MallSeatsKeepTheirActualUpperFloorWhenSectorMeshesAreRebased()
        {
            var plan=MallPlan.Create(94,-5,9,true,true,true);int floor=plan.Floors-1;
            var data=MallArchitecture.Sector(plan,floor,1,12).Geometry;Assert.That(data.SourceSeats.Count,Is.GreaterThan(0));
            var root=new GameObject("Upper mall furniture check");root.transform.position=new Vector3(18000,1000,18000);
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();var shell=MallArchitecture.Shell(plan);
                var shape=MeshFactory.CreateCollider("Mall actual floors and rails",shell.Structure,root.transform,owner).GetComponent<MeshCollider>();
                var sector=new GameObject("Rebased upper sector");sector.transform.SetParent(root.transform,false);sector.transform.localPosition=Vector3.up*(floor*MallPlan.FloorHeight);
                PoolroomsPickup.CreateMallSeats(plan,data,floor*MallPlan.FloorHeight,sector.transform,null);Physics.SyncTransforms();
                var items=sector.GetComponentsInChildren<PoolroomsPickup>();Assert.That(items.Length,Is.EqualTo(data.SourceSeats.Count));
                foreach(var item in items)
                {
                    var box=item.GetComponent<BoxCollider>();
                    Assert.That(item.transform.localPosition.y-box.size.y*.5f,Is.EqualTo(.02f).Within(.001f));
                    Assert.That(item.SpawnKey,Does.Contain(":source-seat:"));
                    bool overlap=Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,shape,shape.transform.position,shape.transform.rotation,out _,out float depth);
                    Assert.That(overlap&&depth>.003f,Is.False,"Rebased source chair intersects the mall floor or railing");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void HouseSourceSeatsReplaceOldSeatingWithoutIntersectingRooms()
        {
            var region=TerrainRegionPlan.Create(94,0,0);var house=region.House(0,0);house.SetArchitecture(0);
            var data=TerrainHouseArchitecture.Build(house,true,region);Assert.That(data.Geometry.SourceSeats.Count,Is.GreaterThan(0));
            var root=new GameObject("Level94 native source seats");root.transform.position=new Vector3(18000,1000,18000);
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();var structure=MeshFactory.CreateCollider("House final architecture",data.Geometry.Structure,root.transform,owner).GetComponent<MeshCollider>();
                foreach(var seat in data.Geometry.SourceSeats)
                {
                    var item=PoolroomsPickup.CreateSourceSeat(region.ItemKey(house,seat.Slot),seat,root.transform);Assert.That(item,Is.Not.Null);Physics.SyncTransforms();
                    var box=item.GetComponent<BoxCollider>();
                    bool overlap=Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,structure,structure.transform.position,structure.transform.rotation,out _,out float depth);
                    Assert.That(overlap&&depth>.003f,Is.False,"Source furniture intersects a wall or original furnishing");
                    Assert.That(item.GetComponent<Rigidbody>(),Is.Null,"Unused furniture should not simulate rigid bodies");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void BakedFurnitureImportsAtItsPlannedScaleAndUsesSourceColour()
        {
            var root=new GameObject("Attributed furniture source import");
            try
            {
                foreach(int id in new[]{3,4})
                {
                    string source=SourceFurniture.Id(id);var pickup=PoolroomsPickup.CreateSource("native-test-"+id,source,Vector3.up*.02f,root.transform,SourceFurniture.Longest(id));
                    Assert.That(pickup,Is.Not.Null);var planned=SourceFurniture.Dimensions(id);var box=pickup.GetComponent<BoxCollider>();
                    Assert.That(box.size.x,Is.EqualTo(planned.X).Within(.003f));Assert.That(box.size.y,Is.EqualTo(planned.Y).Within(.003f));Assert.That(box.size.z,Is.EqualTo(planned.Z).Within(.003f));
                    Assert.That(pickup.GetComponent<LODGroup>(),Is.Not.Null,"Distant furniture needs bounded renderer visibility");
                    var definition=City11Sources.Get(source);
                    foreach(var renderer in pickup.GetComponentsInChildren<MeshRenderer>())foreach(var material in renderer.sharedMaterials)
                    {
                        Assert.That(material.shader.isSupported,Is.True);Assert.That(material.shader.name,Does.Not.Contain("Unlit"));
                        foreach(var d in definition.materials)if(material.name.EndsWith(d.name))
                        {
                            var expected=new Color(d.colour[0],d.colour[1],d.colour[2],d.colour[3]).gamma;
                            if(material.HasProperty("_BaseColor"))Assert.That(material.GetColor("_BaseColor").r,Is.EqualTo(expected.r).Within(.001f));
                        }
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void SourceFurnitureBoxesDoNotPenetrateShopArchitecture()
        {
            var c=new LayoutConfig{Seed=3,CellSize=36,Continuous=true,Procedural=LiminalGeneration.City11};var room=new Layout(c).At(-6,7);var geometry=Architecture.Build(room,c);
            var root=new GameObject("Furniture native collision check");root.transform.position=new Vector3(18000,1000,18000);
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();var wall=MeshFactory.CreateCollider("City static supports",geometry.Structure,root.transform,owner).GetComponent<MeshCollider>();
                foreach(var prop in room.Plan.City.Props)if(prop.Source>=2)
                {
                    var pickup=PoolroomsPickup.CreateSource("native-placement-"+prop.Source,SourceFurniture.Id(prop.Source),new Vector3(prop.Position.X,prop.Position.Y,prop.Position.Z),root.transform,SourceFurniture.Longest(prop.Source));
                    Assert.That(pickup,Is.Not.Null);pickup.transform.localRotation=Quaternion.Euler(0,prop.Yaw,0);Physics.SyncTransforms();
                    var box=pickup.GetComponent<BoxCollider>();
                    bool overlap=Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,wall,wall.transform.position,wall.transform.rotation,out _,out float depth);
                    Assert.That(overlap&&depth>.003f,Is.False,"Furniture intersects a static wall, stair or counter");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
