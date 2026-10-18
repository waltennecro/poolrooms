#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class OrangerieWorldTests
    {
        [UnityTest]
        public IEnumerator GardenAislesSupportControllerAndFoliageHasNoCollider()
        {
            foreach(int recipe in new[]{0,626,1252,1878,2499})
            {
                var c=new LayoutConfig{CellSize=28,DoorWidth=6,DoorHeight=5.1f,Procedural=LiminalGeneration.Orangerie};
                var r=new Room{X=0,Z=0,Exits=15};OrangeriePlanner.DescribeRecipe(r,c,recipe);var g=OrangerieArchitecture.Build(r,c);
                var root=new GameObject("Orangerie native walking");root.transform.position=new Vector3(20000,1000,20000);
                var actor=new GameObject("Garden walker");
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();MeshFactory.CreateCollider("Garden support",g.Structure,root.transform,owner);
                    var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.minMoveDistance=0;
                    foreach(var direction in new[]{Vector3.right,Vector3.forward})
                    {
                        body.enabled=false;actor.transform.position=root.transform.position-direction*13+Vector3.up*.05f;body.enabled=true;Physics.SyncTransforms();
                        for(int step=0;step<260;step++)
                        {
                            body.Move(direction*.1f-Vector3.up*.02f);
                            Assert.That(actor.transform.position.y-root.transform.position.y,Is.GreaterThan(-.1f));
                            if(step%30==0)yield return null;
                        }
                        Assert.That(Vector3.Dot(actor.transform.position-root.transform.position,direction),Is.GreaterThan(12));
                    }
                    Assert.That(g.VisualMeshes.Count,Is.EqualTo(1));
                    var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                    try
                    {
                        var foliage=MeshFactory.Create("Garden leaf batch",g.VisualMeshes[0],root.transform,profile,owner,false);
                        if(g.VisualMeshes[0].TriangleCount==0)Assert.That(foliage,Is.Null);
                        else Assert.That(foliage.GetComponentsInChildren<Collider>().Length,Is.Zero);
                    }
                    finally{Object.DestroyImmediate(profile);}
                    foreach(var aperture in OrangerieArchitecture.Apertures(r))
                        Assert.That(Physics.Raycast(root.transform.position+new Vector3(aperture.X,r.Height+.01f,aperture.Z),Vector3.up,1,~0,QueryTriggerInteraction.Ignore),Is.False,"An opaque collider seals the skylight");
                }
                finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
            }
        }
        [Test]
        public void LicensedPottedPlantLoadsWithCutoutLeavesAndPickupVolume()
        {
            var parent=new GameObject("Licensed garden source test");
            try
            {
                var plant=PoolroomsPickup.CreateSource("test:garden", "G_PottedPlant",Vector3.zero,parent.transform,1.15f);
                Assert.That(plant,Is.Not.Null);Assert.That(plant.SpawnKey,Is.EqualTo("test:garden"));
                Assert.That(plant.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
                Assert.That(plant.GetComponentsInChildren<LODGroup>().Length,Is.EqualTo(1));
                bool leaves=false;
                foreach(var renderer in plant.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material,Is.Not.Null);
                    if(!material.name.EndsWith("G_PottedPlant_m1"))continue;
                    leaves=true;Assert.That(material.IsKeywordEnabled("_ALPHATEST_ON"),Is.True);
                    if(material.HasProperty("_AlphaCutoff"))Assert.That(material.GetFloat("_AlphaCutoff"),Is.EqualTo(.5f));
                }
                Assert.That(leaves,Is.True);
            }
            finally{Object.DestroyImmediate(parent);}
        }
        [Test]
        public void GardenPbrMapsExistInAllFourFinishSets()
        {
            foreach(string set in new[]{"GardenLimestone","GardenLimewash","GardenTerracotta","GardenLeaves"})
            foreach(string suffix in new[]{"Albedo","Normal","Mask"})
            {
                var map=Resources.Load<Texture2D>("PoolroomsBiomeTextures/"+set+"_"+suffix);
                Assert.That(map,Is.Not.Null,set+"/"+suffix);Assert.That(map.width,Is.EqualTo(1024));Assert.That(map.height,Is.EqualTo(1024));
            }
        }
    }
}
#endif
