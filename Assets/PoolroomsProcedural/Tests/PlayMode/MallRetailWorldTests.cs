#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms.Tests
{
    public sealed class MallRetailWorldTests
    {
        [Test]
        public void LicensedRetailSourcesLoadWithMetricBoundsAndRealMaterials()
        {
            var root=new GameObject("Mall native source audit");
            try
            {
                for(int source=14;source<SourceFurniture.Count;source++)
                {
                    string id=SourceFurniture.Id(source);
                    var item=City11Sources.Create(id,root.transform,Vector3.zero,SourceFurniture.Longest(source),out var size);
                    Assert.That(item,Is.Not.Null,id);
                    var actual=SourceFurniture.Dimensions(source);
                    Assert.That(size.x,Is.EqualTo(actual.X).Within(.003f),id);Assert.That(size.y,Is.EqualTo(actual.Y).Within(.003f),id);Assert.That(size.z,Is.EqualTo(actual.Z).Within(.003f),id);
                    Assert.That(item.GetComponent<LODGroup>(),Is.Not.Null,id);
                    foreach(var renderer in item.GetComponentsInChildren<MeshRenderer>())foreach(var material in renderer.sharedMaterials)
                    {
                        Assert.That(material,Is.Not.Null,id);Assert.That(material.shader,Is.Not.Null,id);
                        Assert.That(material.shader.name,Does.Not.Contain("InternalError"),id);
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void RetailMaterialsHaveColourNormalAndAuthoredGlossTextures()
        {
            var root=new GameObject("Mall PBR audit");
            try
            {
                var owner=root.AddComponent<PoolroomsChunk>();
                foreach(MallShopKind kind in Enum.GetValues(typeof(MallShopKind)))
                foreach(var surface in new[]{Surface.Tile,Surface.Plaster,Surface.Accent,Surface.Metal})
                {
                    var material=PoolroomsMallMaterials.Resolve(MallRealism.Theme(kind),(int)surface,0,owner);
                    Assert.That(material,Is.Not.Null,kind+" / "+surface);
                    if(material.shader.name=="HDRP/Lit")
                    {
                        Assert.That(material.GetTexture("_BaseColorMap"),Is.Not.Null);
                        int finish=MallRealism.Finish(MallRealism.Theme(kind),surface,0);
                        if(finish!=10&&finish!=17)Assert.That(material.GetTexture("_NormalMap"),Is.Not.Null);
                        Assert.That(material.GetTexture("_MaskMap"),Is.Not.Null);
                        Assert.That(material.GetTexture("_SpecularColorMap"),Is.Not.Null);
                    }
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [Test]
        public void UpperStoreyFinishAndColliderUseTheSameLocalCoordinates()
        {
            var root=new GameObject("Upper mall floor");root.transform.position=Vector3.up*MallPlan.FloorHeight;
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
            try
            {
                var p=MallPlan.Create(31,13,0,true,true,true,true,true,true,true);var sector=MallArchitecture.Sector(p,1,2,16);
                var owner=root.AddComponent<PoolroomsChunk>();
                PoolroomsMallMaterials.Attach(sector.Geometry,root.transform,profile,owner,true,MallPlan.FloorHeight);
                var collision=root.GetComponentInChildren<MeshCollider>();Assert.That(collision,Is.Not.Null);
                Assert.That(collision.sharedMesh.bounds.min.y,Is.GreaterThanOrEqualTo(-.01f));
                Assert.That(collision.sharedMesh.bounds.max.y,Is.LessThan(MallPlan.FloorHeight));
                int rendered=0;foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    Assert.That(renderer.bounds.min.y,Is.GreaterThan(root.transform.position.y-.03f));
                    Assert.That(renderer.bounds.max.y,Is.LessThan(root.transform.position.y+MallPlan.FloorHeight));rendered++;
                }
                Assert.That(rendered,Is.GreaterThan(0));
                // No visual mesh receives a second collider; the furniture batch owns it once.
                Assert.That(root.GetComponentsInChildren<MeshCollider>().Length,Is.EqualTo(1));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(profile);}
        }
    }
}
#endif
