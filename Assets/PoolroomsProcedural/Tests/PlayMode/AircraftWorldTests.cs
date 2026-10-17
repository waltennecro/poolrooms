#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class AircraftWorldTests
    {
        [Test]
        public void ApacheAircraftImportsMappedMaterialsAndTwoNativePropellers()
        {
            var root=new GameObject("Aircraft source check");root.transform.position=new Vector3(24000,1000,24000);
            try
            {
                var plane=PoolroomsAircraft.Create(null,root.transform,Vector3.zero,"test-aircraft");
                Assert.That(plane,Is.Not.Null,"OBJ part names/material mappings must survive native import");
                int pivots=0,discs=0;
                foreach(var t in plane.GetComponentsInChildren<Transform>())if(t.name=="Engine propeller pivot")
                {pivots++;Assert.That(t.childCount,Is.EqualTo(2),"Each rotor needs its opaque hub and alpha disc");}
                foreach(var renderer in plane.GetComponentsInChildren<MeshRenderer>())foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);
                    Assert.That(material.shader.name,Is.Not.EqualTo("Hidden/InternalErrorShader"));
                    if(renderer.name.Contains("_m1"))
                    {discs++;Assert.That(material.renderQueue,Is.GreaterThanOrEqualTo(3000));if(material.HasProperty("_BaseColor"))Assert.That(material.GetColor("_BaseColor").a,Is.EqualTo(1).Within(.001),"Texture alpha must not be multiplied by an arbitrary opacity");}
                }
                Assert.That(pivots,Is.EqualTo(2));Assert.That(discs,Is.EqualTo(2));
                Assert.That(City11Sources.TryLocalBounds(plane.transform,out var bounds),Is.True);
                Assert.That(bounds.size.x,Is.InRange(11.9f,12.1f));Assert.That(bounds.center.magnitude,Is.LessThan(.01f),"Flight volumes must match the recentered model");
                Assert.That(plane.GetComponent<Rigidbody>().isKinematic,Is.True,"Unpiloted aircraft should not simulate far away");
                Assert.That(plane.GetComponents<SphereCollider>().Length,Is.EqualTo(3));
                foreach(var contact in plane.GetComponents<SphereCollider>())Assert.That(contact.sharedMaterial.dynamicFriction,Is.LessThan(.03f),"Ground friction prevents takeoff");
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator FullWidthRunwaySeamsSupportNativeWalking()
        {
            var c=new LayoutConfig{Seed=18,CellSize=28,Continuous=true,Procedural=LiminalGeneration.City11,EnableAircraft=true};
            Assert.That(CityAirfields.Find(c,0,0,out var site),Is.True);var layout=new Layout(c);
            var root=new GameObject("Airfield seam native check");root.transform.position=new Vector3(22000,1000,22000);
            var walker=new GameObject("Runway walker");
            try
            {
                for(int row=-1;row<=1;row++)
                {
                    var tile=new GameObject("Runway tile");tile.transform.SetParent(root.transform,false);tile.transform.localPosition=new Vector3(0,0,row*c.CellSize);
                    var owner=tile.AddComponent<PoolroomsChunk>();var room=layout.At(site.CenterX,site.CenterZ+row);
                    MeshFactory.CreateCollider("Runway support",Architecture.Build(room,c).Structure,tile.transform,owner);
                }
                var body=walker.AddComponent<CharacterController>();body.height=1.8f;body.radius=.32f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.34f;body.minMoveDistance=0;
                // Away from the graph's normal narrow doorway: the entire runway must connect.
                walker.transform.position=root.transform.position+new Vector3(6,.05f,-17);Physics.SyncTransforms();
                for(int frame=0;frame<340;frame++)
                {body.Move(new Vector3(0,-.025f,.1f));Assert.That(walker.transform.position.y-root.transform.position.y,Is.GreaterThan(-.03f),"Walker fell through a loaded runway seam");yield return null;}
                Assert.That(walker.transform.position.z-root.transform.position.z,Is.GreaterThan(16),"A wall blocks the full-width internal runway");
            }
            finally{Object.DestroyImmediate(walker);Object.DestroyImmediate(root);}
        }
    }
}
#endif
