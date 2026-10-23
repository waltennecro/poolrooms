#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class CityBuildingPlayModeTests
    {
        [UnityTest]
        public IEnumerator RealColliderKeepsStairReturnAndUpperGallerySupported()
        {
            GameObject root=null;
            try
            {
                root=new GameObject("City building native support regression");root.transform.position=Vector3.up*17000;
                var owner=root.AddComponent<PoolroomsChunk>();var b=new CityBuildingPlan(0,0,10,10,4,0,0,25,false,design:191);
                var g=new RoomGeometry();CityBuildingArchitecture.Build(g,b,new LayoutConfig{DetailedCityBuildings=true});
                var collider=MeshFactory.CreateCollider("City physical shell",g.Structure,root.transform,owner);Physics.SyncTransforms();yield return null;
                for(int floor=0;floor<4;floor++)
                {
                    float y=.12f+floor*3.2f;
                    foreach(float x in new[]{.65f,2f,3.5f})
                    {
                        var at=root.transform.position+new Vector3(x,y,-2.05f);
                        Assert.That(collider.Raycast(new Ray(at+Vector3.up*.3f,Vector3.down),out var hit,.5f),Is.True);
                        Assert.That(Mathf.Abs(hit.point.y-at.y),Is.LessThan(.012f));
                        Assert.That(collider.Raycast(new Ray(at+Vector3.up*.05f,Vector3.up),out _,1.8f),Is.False,"Gallery headroom was obstructed");
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)Object.DestroyImmediate(root);}
        }
        [UnityTest]
        public IEnumerator ThirdLicensedExteriorResolvesItsMaterialsAndOrientationMarker()
        {
            GameObject root=null;
            try
            {
                root=new GameObject("City imported facade regression");
                var plan=new City11Plan(new[]{new CityBuildingPlan(0,0,10,10,4,0,0,1,false,3)},new CityPropPlan[0]);
                PoolroomsCityBuildings.Attach(plan,root.transform,new LayoutConfig{EnableImportedBuildings=true,DetailedCityBuildings=true});yield return null;
                var model=root.transform.Find("Licensed building exteriors");Assert.That(model,Is.Not.Null);
                var lod=model.GetComponentInChildren<LODGroup>();Assert.That(lod,Is.Not.Null);Assert.That(lod.lodCount,Is.EqualTo(3));
                bool marker=false;
                foreach(var child in model.GetComponentsInChildren<Transform>(true))
                {
                    if(!child.name.Contains("EntryOrientationMarker"))continue;marker=true;
                    foreach(var r in child.GetComponentsInChildren<Renderer>())Assert.That(r.enabled,Is.False);
                }
                Assert.That(marker,Is.True);Assert.That(model.GetComponentsInChildren<Collider>(),Is.Empty,"Imported skin must not duplicate generated collision");
                LogAssert.NoUnexpectedReceived();
            }
            finally{if(root!=null)Object.DestroyImmediate(root);}
        }
    }
}
#endif
