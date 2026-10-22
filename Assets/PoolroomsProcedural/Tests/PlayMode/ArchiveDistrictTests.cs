#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class ArchiveDistrictTests
    {
        [UnityTest]
        public IEnumerator NativeLibraryStepsAndGalleryVoidRemainPhysical()
        {
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                var c=new LayoutConfig{Continuous=true,Procedural=LiminalGeneration.Archive906,RebuildArchive906=true,CellSize=28,DoorWidth=6,DoorHeight=5.5f};
                var room=new Layout(c).At(1,0);var p=room.Plan.Quiet.Archive;var data=Architecture.Build(room,c);
                root=new GameObject("Cygnus native geometry regression");root.transform.position=Vector3.up*12000;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var owner=root.AddComponent<PoolroomsChunk>();
                var collider=MeshFactory.Create("Library architecture",data.Structure,root.transform,profile,owner,true).GetComponent<MeshCollider>();
                var glazing=MeshFactory.Create("Cygnus glazing",data.Glass,root.transform,profile,owner,false).GetComponent<MeshRenderer>();
                Assert.That(glazing.shadowCastingMode,Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),"Transparent roof must not cast an opaque shadow.");
                MeshFactory.CreateCollider("Library safety rails",data.CollisionOnly,root.transform,owner);
                Physics.SyncTransforms();yield return null;
                for(int f=0;f<2;f++)for(int flight=0;flight<2;flight++)for(int step=0;step<p.Steps(f);step++)
                {
                    int count=p.Steps(f);float tread=ArchiveDistrictPlan.Flight/count;
                    float expected=p.FloorY(f)+flight*p.Rise(f)*.5f+(step+1)*p.Rise(f)/(2*count);
                    float start=p.Stair.MinZ+ArchiveDistrictPlan.Landing;
                    var at=new Vector3(p.Stair.X+(flight==0?-.9f:.9f),expected+.15f,flight==0?start+(step+.5f)*tread:start+ArchiveDistrictPlan.Flight-(step+.5f)*tread);
                    Assert.That(collider.Raycast(new Ray(root.transform.TransformPoint(at),Vector3.down),out var hit,.3f),Is.True);
                    Assert.That(hit.point.y-root.transform.position.y,Is.EqualTo(expected).Within(.02f));
                    Assert.That(collider.Raycast(new Ray(root.transform.TransformPoint(at+Vector3.up*.1f),Vector3.up),out _,1.8f),Is.False,"A slab blocks the staircase.");
                }
                var voidRay=new Ray(root.transform.TransformPoint(new Vector3(0,p.FloorY(2)+.1f,c.CellSize*.25f)),Vector3.down);
                Assert.That(collider.Raycast(voidRay,out var floorHit,20),Is.True);
                Assert.That(floorHit.point.y-root.transform.position.y,Is.EqualTo(0).Within(.02f),"The nave is filled by an upper floor.");
                float inner=c.CellSize*.5f-ArchiveDistrictPlan.Gallery;
                Assert.That(Physics.Raycast(root.transform.TransformPoint(new Vector3(inner-1,p.FloorY(1)+.7f,4)),Vector3.right,2,~0,QueryTriggerInteraction.Ignore),Is.True,"Gallery edge has no collision barrier.");
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void LibraryVelvetBindsFabricPbrAndKeepsBlueTint()
        {
            Material material=null;
            try
            {
                material=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.CygnusVelvet,Surface.Tile);
                string albedo=material.HasProperty("_BaseColorMap")?"_BaseColorMap":material.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
                Assert.That(material.GetTexture(albedo),Is.SameAs(Resources.Load<Texture2D>("PoolroomsBiomeTextures/Fabric_Albedo")));
                Assert.That(material.GetTexture(albedo),Is.Not.Null);
                var colour=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material.color;
                Assert.That(colour.b,Is.GreaterThan(colour.r*3));
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(material!=null)Object.DestroyImmediate(material);}
        }
    }
}
#endif
