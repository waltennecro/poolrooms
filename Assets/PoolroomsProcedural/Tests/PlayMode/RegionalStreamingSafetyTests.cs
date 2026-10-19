#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Poolrooms.Tests
{
    public sealed class RegionalStreamingSafetyTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Field(object owner,string name,object value){owner.GetType().GetField(name,Private).SetValue(owner,value);}
        static void Property(object owner,string name,object value){owner.GetType().GetProperty(name).SetValue(owner,value);}
        static object Call(object owner,string name,params object[] args){return owner.GetType().GetMethod(name,Private).Invoke(owner,args);}
        [Test]
        public void ReadyViewerDoesNotMakeAnUnloadedMallReservationSafe()
        {
            var root=new GameObject("Position query regression");var viewer=new GameObject("Viewer");PoolroomsProfile profile=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.enabled=false;profile=ScriptableObject.CreateInstance<PoolroomsProfile>();world.profile=profile;world.viewer=viewer.transform;
                var c=new LayoutConfig();WalkableExploration.Configure(c);Field(world,"layout",new Layout(c));
                var loaded=(IDictionary)typeof(PoolroomsWorld).GetField("loaded",Private).GetValue(world);loaded.Add(ContinuousTopology.CellKey(0,0),root.AddComponent<PoolroomsChunk>());
                Assert.That(world.ViewerReady,Is.True);Assert.That(ConnectedMallTopology.Site(c,0,0,out var site),Is.True);
                Assert.That(world.IsPositionStreamed(world.CellWorldPosition(site.X,site.Z)),Is.False,"The target has no mall or collision sectors, regardless of the ready viewer.");
                Assert.That(world.IsPositionStreamed(new Vector3(float.NaN,0,0)),Is.False);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(viewer);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void TerrainQueriesRequireDetailedSupportAtTheRequestedPoint()
        {
            var root=new GameObject("Terrain point query regression");var viewer=new GameObject("Viewer");PoolroomsProfile profile=null;
            try
            {
                var world=root.AddComponent<PoolroomsWorld>();world.enabled=false;profile=ScriptableObject.CreateInstance<PoolroomsProfile>();world.profile=profile;world.viewer=viewer.transform;Field(world,"layout",new Layout(new LayoutConfig{Continuous=true}));
                var terrain=root.AddComponent<PoolroomsTerrainSystem>();Field(world,"terrain",terrain);terrain.enabled=false;
                var plan=TerrainRegionPlan.Create(31,0,0);var region=new GameObject("Terrain region");region.transform.SetParent(root.transform);
                Property(terrain,"Plan",plan);Property(terrain,"RegionRoot",region.transform);Property(terrain,"IsInside",true);Field(terrain,"bootstrapped",true);
                var type=typeof(PoolroomsTerrainSystem).GetNestedType("Tile",BindingFlags.NonPublic);var tiles=(IDictionary)typeof(PoolroomsTerrainSystem).GetField("loaded",Private).GetValue(terrain);
                object Tile(int x,bool detailed)
                {
                    var value=Activator.CreateInstance(type);type.GetField("X").SetValue(value,x);type.GetField("Z").SetValue(value,0);type.GetField("Resolution").SetValue(value,detailed?32:8);type.GetField("Detailed").SetValue(value,detailed);
                    var tileObject=new GameObject("Tile "+x);tileObject.transform.SetParent(region.transform);type.GetField("Chunk").SetValue(value,tileObject.AddComponent<PoolroomsChunk>());return value;
                }
                tiles.Add(TerrainRegionPlan.Key(0,0),Tile(0,true));viewer.transform.position=new Vector3(0,plan.Height(0,0)+1,0);
                Assert.That(world.ViewerReady,Is.True);Vector3 target=new Vector3(192,plan.Height(192,0)+1,0);
                Assert.That(world.IsPositionStreamed(target),Is.False);tiles.Add(TerrainRegionPlan.Key(3,0),Tile(3,false));
                Assert.That(world.IsPositionStreamed(target),Is.False,"A distant visual mesh is not detailed collision support.");
                tiles[TerrainRegionPlan.Key(3,0)]=Tile(3,true);Assert.That(world.IsPositionStreamed(target),Is.True);
            }
            finally{Object.DestroyImmediate(root);Object.DestroyImmediate(viewer);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void DroppedObjectPreservesMotionAcrossMissingAndRestoredSupport()
        {
            var root=new GameObject("Loose support regression");PoolroomsProfile profile=null;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var item=PoolroomsPickup.CreateCatalog("test:streaming",0,Vector3.up*2,root.transform,profile);
                item.Release(root.transform,Vector3.up*2,new Vector3(2,0,0));var body=item.GetComponent<Rigidbody>();Vector3 velocity=body.linearVelocity;body.angularVelocity=new Vector3(0,.3f,0);
                Vector3 position=item.transform.position;Call(item,"SetStreamingSupport",false);Assert.That(body.isKinematic,Is.True);Assert.That(item.transform.position,Is.EqualTo(position));
                Call(item,"SetStreamingSupport",false);Call(item,"SetStreamingSupport",true);Assert.That(body.isKinematic,Is.False);
                Assert.That((body.linearVelocity-velocity).magnitude,Is.LessThan(.0001f));Assert.That((body.angularVelocity-new Vector3(0,.3f,0)).magnitude,Is.LessThan(.0001f));
                Call(item,"SetStreamingSupport",false);item.Attach(root.transform);item.Release(root.transform,Vector3.up*2,Vector3.zero);Call(item,"SetStreamingSupport",true);
                Assert.That(body.linearVelocity.magnitude,Is.LessThan(.0001f),"Picking up/releasing must discard the old frozen momentum.");
            }
            finally{Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
    }
}
#endif
