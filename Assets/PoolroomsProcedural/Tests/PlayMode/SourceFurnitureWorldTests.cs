#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class SourceFurnitureWorldTests
    {
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
