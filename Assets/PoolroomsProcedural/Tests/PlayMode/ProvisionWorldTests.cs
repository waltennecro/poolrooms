#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ProvisionWorldTests
    {
        [Test]
        public void EveryFoodImportsWithItsMeasuredCarrySizeAndMappedMaterials()
        {
            var root=new GameObject("Native licensed food import audit");var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
            try
            {
                for(int i=0;i<ProvisionCatalog.Count;i++)
                {
                    var d=ProvisionCatalog.Get(i);var pickup=PoolroomsPickup.CreateFood("test:food:"+i,i,Vector3.zero,root.transform,profile);
                    Assert.That(pickup,Is.Not.Null,d.Name);Assert.That(pickup.InventoryKey,Is.EqualTo(d.Key));Assert.That(pickup.IsProvision,Is.True);
                    var bounds=pickup.GetComponent<BoxCollider>();Assert.That(bounds,Is.Not.Null);
                    Assert.That((bounds.size-new Vector3(d.Size.X,d.Size.Y,d.Size.Z)).magnitude,Is.LessThan(.0001f));
                    foreach(var renderer in pickup.GetComponentsInChildren<MeshRenderer>())foreach(var material in renderer.sharedMaterials)
                    {Assert.That(material,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);Assert.That(material.shader.name,Is.Not.EqualTo("Hidden/InternalErrorShader"));}
                    Assert.That(pickup.GetComponent<LODGroup>().GetLODs().Length,Is.EqualTo(2));Object.DestroyImmediate(pickup.gameObject);
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(profile);}
        }
        [UnityTest]
        public IEnumerator SharingTablePopulatesOnlyOnActualFloorAndNeverInAnEmptyVoid()
        {
            var root=new GameObject("Native provision placement");root.transform.position=new Vector3(22000,1000,22000);
            var profile=ScriptableObject.CreateInstance<PoolroomsProfile>();GameObject grounded=null,empty=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.profile=profile;world.generateOnStart=false;world.streaming=false;world.unifiedExploration=false;profile.enableProvisions=true;profile.colliders=true;
                var config=new LayoutConfig{EnableProvisions=true};typeof(PoolroomsWorld).GetField("layout",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,new Layout(config));
                grounded=new GameObject("Grounded room");grounded.transform.SetParent(root.transform,false);var owner=grounded.AddComponent<PoolroomsChunk>();
                var floor=new MeshData();floor.Box(new V3(-18,-.2f,-18),new V3(18,0,18),Surface.Tile);MeshFactory.CreateCollider("Actual floor",floor,grounded.transform,owner);
                empty=new GameObject("Empty void");empty.transform.SetParent(root.transform,false);empty.transform.localPosition=Vector3.right*100;empty.AddComponent<PoolroomsChunk>();
                PoolroomsProvisions.Attach(world,grounded.transform,"test-ground",7,0,0,36,36);
                PoolroomsProvisions.Attach(world,empty.transform,"test-void",7,1,0,36,36);
                for(int i=0;i<120;i++)yield return null;
                Assert.That(grounded.GetComponent<PoolroomsProvisions>().CreatedItems,Is.EqualTo(4));
                Assert.That(empty.GetComponent<PoolroomsProvisions>().CreatedItems,Is.Zero);
                foreach(var item in grounded.GetComponentsInChildren<PoolroomsPickup>())if(item.IsProvision)
                {Assert.That(item.GetComponent<Rigidbody>(),Is.Null,"Unmoved food must not consume awake physics bodies");Assert.That(item.transform.localPosition.y,Is.GreaterThan(.75f));}
                LogAssert.NoUnexpectedReceived();
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(profile);}
        }
    }
}
#endif
