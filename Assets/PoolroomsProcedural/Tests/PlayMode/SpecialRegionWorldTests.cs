#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class SpecialRegionWorldTests
    {
        [UnityTest]
        public IEnumerator OrdinaryPlayerCrossesAllTenSpecialRegionThresholds()
        {
            for(int level=0;level<SpecialRegionPlanner.Count;level++)
            {
                var config=new LayoutConfig{CellSize=28,DoorWidth=6,DoorHeight=5.1f,CurveSegments=12,ObjectAbundance=3};
                var a=new Room{Environment=(LiminalEnvironment)(28+level),Exits=15};
                var b=new Room{Environment=(LiminalEnvironment)(28+(level+1)%10),Exits=15};
                RestfulLevelPlanner.DescribeRecipe(a,config,131071);RestfulLevelPlanner.DescribeRecipe(b,config,0);
                var root=new GameObject("Special region walking regression");root.transform.position=new Vector3(4000,1800,4000);
                var actor=new GameObject("Ordinary walker");
                try
                {
                    var owner=root.AddComponent<PoolroomsChunk>();
                    MeshFactory.CreateCollider("First region",Architecture.Build(a,config).Structure,root.transform,owner);
                    MeshFactory.CreateCollider("Shared threshold",Corridors.Build(a,b,0,config).Structure,root.transform,owner);
                    var next=new GameObject("Next region");next.transform.SetParent(root.transform,false);next.transform.localPosition=Vector3.forward*config.CellSize;
                    MeshFactory.CreateCollider("Second region",Architecture.Build(b,config).Structure,next.transform,owner);
                    actor.transform.position=root.transform.position+Vector3.forward*(config.CellSize*.5f-1)+Vector3.up*.04f;
                    var body=actor.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.stepOffset=.3f;body.skinWidth=.025f;
                    Physics.SyncTransforms();
                    for(int step=0;step<60;step++){body.Move(Vector3.forward*.05f-Vector3.up*.02f);if(step%15==0)yield return null;}
                    Assert.That(actor.transform.position.z-root.transform.position.z,Is.GreaterThan(config.CellSize*.5f+1.7f),SpecialRegionPlanner.Name(a.Environment));
                    Assert.That(actor.transform.position.y-root.transform.position.y,Is.InRange(-.1f,.15f));
                }
                finally{Object.DestroyImmediate(actor);Object.DestroyImmediate(root);}
            }
        }
        [Test]
        public void ReflectivePanelsUseOpaqueLitMaterials()
        {
            Material material=null;
            try
            {
                material=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.SpecialMirror,Surface.Metal);
                Assert.That(material,Is.Not.Null);
                if(material.HasProperty("_Metallic"))Assert.That(material.GetFloat("_Metallic"),Is.EqualTo(1));
                if(material.HasProperty("_Smoothness"))Assert.That(material.GetFloat("_Smoothness"),Is.EqualTo(.985f).Within(.001f));
                if(material.HasProperty("_SurfaceType"))Assert.That(material.GetFloat("_SurfaceType"),Is.Zero);
                Assert.That(material.renderQueue,Is.LessThan(3000));
            }
            finally{if(material!=null)Object.DestroyImmediate(material);}
        }
    }
}
#endif
