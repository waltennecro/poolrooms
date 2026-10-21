#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    // These checks require the actual Unity importer/shader APIs. Cloud geometry
    // verification cannot substitute for executing this suite in the user's project.
    public sealed class JourneyMaterialTests
    {
        [Test]
        public void EveryJourneyProgrammeBindsImportedColourNormalAndGlossMaps()
        {
            for(int level=0;level<JourneyLevelPlanner.Count;level++)
            foreach(var slot in new[]{Surface.Tile,Surface.Plaster,Surface.Accent,Surface.Metal,Surface.Slide})
            {
                var theme=JourneyLevelPlanner.Theme((LiminalEnvironment)(38+level));
                var material=PoolroomsJourneyMaterials.Resolve(theme,(int)slot);
                Assert.That(material,Is.Not.Null,theme+" / "+slot);
                Assert.That(material.shader.name,Does.Not.Contain("InternalError"));
                if(material.shader.name=="HDRP/Lit")
                {
                    Assert.That(material.GetTexture("_BaseColorMap"),Is.Not.Null);
                    Assert.That(material.GetTexture("_NormalMap"),Is.Not.Null);
                    Assert.That(material.GetTexture("_MaskMap"),Is.Not.Null);
                    Assert.That(material.GetTexture("_SpecularColorMap"),Is.Not.Null);
                }
            }
        }
        [Test]
        public void SourcePickupDefinitionsKeepMetricScaleAndInventoryNames()
        {
            var root=new GameObject("Journey source import check");
            try
            {
                for(int source=28;source<36;source++)
                {
                    var id=SourceFurniture.Id(source);
                    var item=City11Sources.Create(id,root.transform,Vector3.zero,SourceFurniture.Longest(source),out var size);
                    Assert.That(item,Is.Not.Null,id);var expected=SourceFurniture.Dimensions(source);
                    Assert.That(size.x,Is.EqualTo(expected.X).Within(.003f),id);
                    Assert.That(size.y,Is.EqualTo(expected.Y).Within(.003f),id);
                    Assert.That(size.z,Is.EqualTo(expected.Z).Within(.003f),id);
                    Assert.That(item.GetComponent<LODGroup>(),Is.Not.Null,id);
                    Assert.That(ExplorationItems.Resolve("source:"+id),Is.Not.Null,id);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
