#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class MallMerchandiseTests
    {
        [UnityTest]
        public IEnumerator CombinedDisplayBecomesOneCarriedObjectAndDoesNotRespawn(){return CheckDisplay(false);}
        [UnityTest]
        public IEnumerator AutonomousDisplayBecomesOneCarriedObjectAndDoesNotRespawn(){return CheckDisplay(true);}
        private IEnumerator CheckDisplay(bool autonomous)
        {
            GameObject sector=null,actor=null;PoolroomsProfile profile=null;
            var cursor=Cursor.lockState;bool visible=Cursor.visible;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();actor=new GameObject("Catalogue carry test");
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.showStartMenu=false;explorer.externalInput=true;
                var eyes=new GameObject("Eyes");eyes.transform.SetParent(actor.transform,false);eyes.AddComponent<Camera>();explorer.eyes=eyes.transform;
                yield return null;yield return null;
                var carry=actor.GetComponent<PoolroomsCarry>();var plan=MallPlan.Create(20,0,0,true,true,autonomous);
                var shop=plan.Shops.First(s=>s.Floor==0&&s.Kind!=MallShopKind.Restrooms);
                var description=MallArchitecture.Sector(plan,0,shop.Sector,12);var first=description.Pickups[0];
                sector=new GameObject("Combined store display");var owner=sector.AddComponent<PoolroomsChunk>();
                var display=sector.AddComponent<PoolroomsMallDisplay>();display.Initialize(plan,description,0,profile,owner,carry);
                Assert.That(display.DisplayedCount,Is.GreaterThanOrEqualTo(12));
                Assert.That(sector.GetComponentsInChildren<PoolroomsPickup>().Length,Is.Zero,"Every product should remain batched before interaction.");
                Assert.That(sector.GetComponentsInChildren<Rigidbody>().Length,Is.Zero);
                int count=display.DisplayedCount;int index;float distance;
                Vector3 center=PoolroomsMallSystem.ToUnity(first.Position),rayOrigin=center+Vector3.back*1.5f;
                Assert.That(display.Find(rayOrigin,Vector3.forward,CarryModel.Reach,out index,out distance),Is.True);
                Assert.That(distance,Is.LessThanOrEqualTo(CarryModel.Reach));Assert.That(display.Take(index),Is.True);
                Assert.That(display.DisplayedCount,Is.EqualTo(count-1));Assert.That(carry.Held.CatalogId,Is.GreaterThanOrEqualTo(0));
                Assert.That(display.Take(index),Is.False);Assert.That(carry.Held.GetComponent<BoxCollider>().enabled,Is.False);
                var held=carry.Held;var mesh=held.GetComponentInChildren<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(sector);sector=null;yield return null;
                Assert.That(held,Is.Not.Null);Assert.That(mesh,Is.Not.Null,"Held resource ownership cannot belong to an unloaded sector.");
                sector=new GameObject("Revisited store display");owner=sector.AddComponent<PoolroomsChunk>();display=sector.AddComponent<PoolroomsMallDisplay>();
                display.Initialize(plan,description,0,profile,owner,carry);Assert.That(display.DisplayedCount,Is.EqualTo(count-1));
                explorer.SetPaused(true);Assert.That(carry.Drop(),Is.False);explorer.SetPaused(false);
                actor.transform.position=Vector3.up*100;Physics.SyncTransforms();Assert.That(carry.Drop(),Is.True);
                Assert.That(held.GetComponent<Rigidbody>().mass,Is.EqualTo(MallItemCatalog.Get(held.CatalogId).Mass).Within(.0001f));
                Assert.That(held.GetComponent<BoxCollider>().enabled,Is.True);
            }
            finally
            {if(sector!=null)Object.DestroyImmediate(sector);if(actor!=null)Object.DestroyImmediate(actor);if(profile!=null)Object.DestroyImmediate(profile);Cursor.lockState=cursor;Cursor.visible=visible;}
        }
        [Test]
        public void BiomeFloorMapsDoNotReusePoolCeramicAndGlassUsesTransparentLit()
        {
            var floor=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.Mall,Surface.Tile);
            var wall=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.IndoorPlay,Surface.Plaster);
            var glass=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.Glass,Surface.Plaster);
            try
            {
                if(floor.HasProperty("_BaseColorMap"))Assert.That(floor.GetTexture("_BaseColorMap").name,Is.EqualTo("MallStone_Albedo"));
                if(wall.HasProperty("_BaseColorMap"))Assert.That(wall.GetTexture("_BaseColorMap").name,Is.EqualTo("Paint_Albedo"));
                Assert.That(glass.renderQueue,Is.GreaterThanOrEqualTo(3000));
                if(glass.HasProperty("_SurfaceType"))Assert.That(glass.GetFloat("_SurfaceType"),Is.EqualTo(1));
                if(glass.HasProperty("_DoubleSidedEnable"))Assert.That(glass.GetFloat("_DoubleSidedEnable"),Is.EqualTo(1));
            }
            finally{Object.DestroyImmediate(floor);Object.DestroyImmediate(wall);Object.DestroyImmediate(glass);}
        }
    }
}
#endif
