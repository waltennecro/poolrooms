#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Poolrooms.Tests
{
    public sealed class WorldStreamingRegressionTests
    {
        static object Call(object owner,string method,params object[] args)
        {return owner.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(owner,args);}
        [Test]
        public void RegionalSunExcludesTemplateShadowAndRestoresItWhenLeaving()
        {
            var root=new GameObject("Sun arbitration test");var external=new GameObject("Template sun");PoolroomsProfile profile=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.enabled=false;profile=ScriptableObject.CreateInstance<PoolroomsProfile>();profile.maxShadowLights=2;world.profile=profile;
                var sunObject=new GameObject("Regional sun");sunObject.transform.SetParent(root.transform);var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;
                var template=external.AddComponent<Light>();template.type=LightType.Directional;template.shadows=LightShadows.Soft;
                var policy=root.AddComponent<PoolroomsDirectionalLighting>();Call(policy,"LateUpdate");
                Assert.That(sun.shadows,Is.EqualTo(LightShadows.Soft));Assert.That(template.enabled,Is.False);Assert.That(template.shadows,Is.EqualTo(LightShadows.None));
                sun.enabled=false;Call(policy,"LateUpdate");Assert.That(template.enabled,Is.True);Assert.That(template.shadows,Is.EqualTo(LightShadows.Soft));
                policy.enabled=false;Assert.That(template.enabled,Is.True);Assert.That(template.shadows,Is.EqualTo(LightShadows.Soft));
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(external);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void UnifiedWorldUpgradesLegacyCatalogueOnlyInItsRuntimeProfile()
        {
            var root=new GameObject("Legacy exploration migration");PoolroomsProfile source=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.enabled=false;source=ScriptableObject.CreateInstance<PoolroomsProfile>();source.zoneMode=ZoneMode.Catalog;source.continuousWorld=false;world.profile=source;world.unifiedExploration=true;
                Assert.That((bool)Call(world,"Initialize"),Is.True);
                Assert.That(world.CurrentLayout.Config.Continuous,Is.True);Assert.That(world.CurrentLayout.Config.DiscoveryCoverage,Is.True);
                Assert.That(world.CurrentLayout.Config.ConnectedMalls&&world.CurrentLayout.Config.ConnectedTerrain,Is.True);
                Assert.That(source.zoneMode,Is.EqualTo(ZoneMode.Catalog));Assert.That(source.continuousWorld,Is.False);Assert.That(world.profile,Is.Not.SameAs(source));
            }
            finally{Object.DestroyImmediate(root);if(source!=null)Object.DestroyImmediate(source);}
        }
        [Test]
        public void AquaticRoomUploadRegistersItsChunkAndCreatesAllFourDeckItems()
        {
            var root=new GameObject("Aquatic upload regression");PoolroomsProfile profile=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.enabled=false;world.unifiedExploration=false;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();world.profile=profile;
                var config=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.AquaticPark,CurveSegments=12};var layout=new Layout(config);
                typeof(PoolroomsWorld).GetField("layout",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,layout);
                typeof(PoolroomsWorld).GetField("generatedRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,root.transform);
                var room=layout.At(0,0);var data=Architecture.Build(room,layout);
                Assert.DoesNotThrow(()=>Call(world,"Attach",room,data),"The original invalid catalogue item stopped this exact upload path.");
                Assert.That(world.IsRoomLoaded(0,0),Is.True);int catalogItems=0;
                foreach(var pickup in root.GetComponentsInChildren<PoolroomsPickup>())if(pickup.CatalogId>=0)
                {var item=MallItemCatalog.Get(pickup.CatalogId);Assert.That(item.Shape==MallItemShape.Towel||item.Shape==MallItemShape.Soap,Is.True);catalogItems++;}
                Assert.That(catalogItems,Is.EqualTo(4));
            }
            finally{Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void MallCentreStillStreamsRealGatewayRoomsOutsideItsReservedFootprint()
        {
            var root=new GameObject("Mall anchor regression");var viewer=new GameObject("Viewer");PoolroomsProfile profile=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.enabled=false;profile=ScriptableObject.CreateInstance<PoolroomsProfile>();world.profile=profile;world.viewer=viewer.transform;
                var config=new LayoutConfig();WalkableExploration.Configure(config);var layout=new Layout(config);
                typeof(PoolroomsWorld).GetField("layout",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,layout);
                Assert.That(ConnectedMallTopology.Site(config,0,0,out var site),Is.True);viewer.transform.position=world.CellWorldPosition(site.X,site.Z);
                var malls=root.AddComponent<PoolroomsConnectedMalls>();typeof(PoolroomsWorld).GetField("connectedMalls",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,malls);
                var type=typeof(PoolroomsConnectedMalls).GetNestedType("District",BindingFlags.NonPublic);var district=Activator.CreateInstance(type,true);
                var shell=new GameObject("Reserved mall");shell.transform.SetParent(root.transform);type.GetField("Site").SetValue(district,site);type.GetField("Root").SetValue(district,shell.transform);
                var districts=(IDictionary)typeof(PoolroomsConnectedMalls).GetField("districts",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(malls);districts.Add(site.Key,district);
                Call(world,"RefreshStreamAnchors");var next=(Room)Call(world,"FindNext",world.ViewerCell);
                Assert.That(next,Is.Not.Null,"Being inside a reserved building must not leave every exterior connection unloaded.");
                Assert.That(ConnectedTerrainTopology.Reserved(config,next.X,next.Z),Is.False);
                Assert.That((bool)Call(world,"NearConnection",next.X,next.Z),Is.True);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(viewer);if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
