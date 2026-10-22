#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
namespace Poolrooms.Tests
{
    public sealed class ConservatoryDistrictTests
    {
        [UnityTest]
        public IEnumerator RotundaStairsStayDryAndReachTheUpperGallery()
        {
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                // This shared pond quarter used to collide with a fixed south-west stair.
                var c=new LayoutConfig{Continuous=true,RebuildConservatory=true,Procedural=LiminalGeneration.Conservatory,CellSize=28,DoorWidth=6,DoorHeight=5.5f};
                var room=new Layout(c).At(2,2);var p=room.Plan.Quiet.Conservatory;var g=Architecture.Build(room,c);
                Assert.That(p.StairEast&&p.StairNorth,Is.True);
                root=new GameObject("Conservatory native regression");root.transform.position=Vector3.up*14000;
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();var owner=root.AddComponent<PoolroomsChunk>();
                var body=MeshFactory.Create("Garden structure",g.Structure,root.transform,profile,owner,true).GetComponent<MeshCollider>();
                MeshFactory.CreateCollider("Garden rails",g.CollisionOnly,root.transform,owner);
                Physics.SyncTransforms();yield return null;
                float start=p.Stair.MinZ+ConservatoryDistrictPlan.Landing,tread=ConservatoryDistrictPlan.Flight/p.Steps;
                for(int flight=0;flight<2;flight++)for(int step=0;step<p.Steps;step++)
                {
                    float expected=flight*p.Upper*.5f+(step+1)*p.Upper/(p.Steps*2);
                    var at=new Vector3(p.Stair.X+(flight==0?-.9f:.9f),expected+.15f,flight==0?start+(step+.5f)*tread:start+ConservatoryDistrictPlan.Flight-(step+.5f)*tread);
                    Assert.That(body.Raycast(new Ray(root.transform.TransformPoint(at),Vector3.down),out var hit,.3f),Is.True);
                    Assert.That(hit.point.y-root.transform.position.y,Is.EqualTo(expected).Within(.02f));
                    Assert.That(body.Raycast(new Ray(root.transform.TransformPoint(at+Vector3.up*.1f),Vector3.up),out _,1.8f),Is.False);
                }
                var pond=new Ray(root.transform.TransformPoint(new Vector3(p.Pond.CenterX,.1f,p.Pond.CenterZ)),Vector3.down);
                Assert.That(body.Raycast(pond,out var bottom,1),Is.True);
                Assert.That(bottom.point.y-root.transform.position.y,Is.EqualTo(p.Pond.Bottom).Within(.02f),"A floor or staircase fills the water basin.");
                var edge=root.transform.TransformPoint(new Vector3(ConservatoryDistrictPlan.Bridge-.5f,p.Upper+.7f,4));
                Assert.That(Physics.Raycast(edge,Vector3.right,1,~0,QueryTriggerInteraction.Ignore),Is.True,"Upper garden void has no safety barrier.");
                Assert.That(Physics.Raycast(root.transform.TransformPoint(new Vector3(0,p.Upper+2,0)),Vector3.up,p.Roof,~0,QueryTriggerInteraction.Ignore),Is.True,"Wing flight can pass through the glazed roof.");
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void SoilMaterialUsesAttributedGroundPbr()
        {
            Material material=null;
            try
            {
                material=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.ConservatorySoil,Surface.Tile);
                var texture=Resources.Load<Texture2D>("PoolroomsBiomeTextures/ConservatoryGround_Albedo");
                string slot=material.HasProperty("_BaseColorMap")?"_BaseColorMap":material.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
                Assert.That(texture,Is.Not.Null);Assert.That(texture.width,Is.EqualTo(2048));Assert.That(material.GetTexture(slot),Is.SameAs(texture));
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(material!=null)Object.DestroyImmediate(material);}
        }
    }
}
#endif
