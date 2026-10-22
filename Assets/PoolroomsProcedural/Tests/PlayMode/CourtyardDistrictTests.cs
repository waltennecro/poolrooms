#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Poolrooms.Core;

namespace Poolrooms.Tests
{
    public sealed class CourtyardDistrictTests
    {
        [UnityTest]
        public IEnumerator FiveStoreyStairHasNativeColliderSupportAndClearHeadroom()
        {
            GameObject root=null;PoolroomsProfile profile=null;
            try
            {
                var config=new LayoutConfig{RebuildCourtyard188=true,Continuous=true,Procedural=LiminalGeneration.Courtyard188};
                var room=new Layout(config).At(0,0);var plan=room.Plan.Quiet.Courtyard;
                var data=Architecture.Build(room,config);
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                root=new GameObject("188 native collider regression");root.transform.position=Vector3.up*10000;
                var owner=root.AddComponent<PoolroomsChunk>();
                var body=MeshFactory.Create("Hotel architecture",data.Structure,root.transform,profile,owner,true).GetComponent<MeshCollider>();
                Physics.SyncTransforms();yield return null;
                for(int floor=0;floor<CourtyardDistrictPlan.Floors;floor++)
                {
                    int steps=plan.Steps(floor);float tread=CourtyardDistrictPlan.Flight/steps;
                    for(int step=0;step<steps;step++)
                    {
                        float expected=plan.FloorY(floor)+(step+1)*plan.Rise(floor)/(steps*2);
                        var local=new Vector3(plan.Stair.X-.9f,expected+.15f,plan.Stair.MinZ+CourtyardDistrictPlan.Landing+(step+.5f)*tread);
                        var ray=new Ray(root.transform.TransformPoint(local),Vector3.down);
                        Assert.That(body.Raycast(ray,out var hit,.3f),Is.True,"Missing native step collider.");
                        Assert.That(hit.point.y-root.transform.position.y,Is.EqualTo(expected).Within(.015f));
                        Assert.That(body.Raycast(new Ray(root.transform.TransformPoint(local+Vector3.up*.2f),Vector3.up),out _,1.75f),Is.False,"Stair ceiling collides with the player.");
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally {if(root!=null)Object.DestroyImmediate(root);if(profile!=null)Object.DestroyImmediate(profile);}
        }
        [Test]
        public void HotelDoorUsesSideHingeAndKeepsItsSettledRotation()
        {
            var door=new GameObject("Hotel hinge regression");
            try
            {
                var i=door.AddComponent<PoolroomsInteractable>();i.activity=RoomActivity.Window;i.sideHinge=true;i.movingPart=door.transform;i.closedRotation=Quaternion.identity;
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(PoolroomsInteractable).GetField("pose",flags).SetValue(i,1f);
                var apply=typeof(PoolroomsInteractable).GetMethod("ApplyPose",flags);apply.Invoke(i,null);apply.Invoke(i,null);
                Assert.That(Quaternion.Angle(door.transform.localRotation,Quaternion.Euler(0,90,0)),Is.LessThan(.01f));
                Assert.That(door.transform.localEulerAngles.x,Is.EqualTo(0).Within(.01f));
            }
            finally {Object.DestroyImmediate(door);}
        }
        [Test]
        public void LawnUsesExistingTerrainPbrInsteadOfPaintedPlaster()
        {
            Material material=null;
            try
            {
                material=PoolroomsBiomeMaterialFactory.Create(SurfaceTheme.CourtyardLawn,Surface.Tile);
                string albedo=material.HasProperty("_BaseColorMap")?"_BaseColorMap":material.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
                Assert.That(material.GetTexture(albedo),Is.SameAs(Resources.Load<Texture2D>("PoolroomsTerrainTextures/Grass_Albedo")));
                Assert.That(material.GetTexture(albedo),Is.Not.Null);
                LogAssert.NoUnexpectedReceived();
            }
            finally {if(material!=null)Object.DestroyImmediate(material);}
        }
    }
}
#endif
