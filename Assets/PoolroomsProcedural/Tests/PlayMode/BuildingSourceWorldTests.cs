#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class BuildingSourceWorldTests
    {
        [Test]
        public void ImportedBuildingSkinsKeepNativeOrientationAndPrimaryCollisionOwnership()
        {
            foreach(int source in new[]{1,2})foreach(bool turn in new[]{false,true})
            {
                var root=new GameObject("Licensed building native check");
                try
                {
                    var building=new CityBuildingPlan(9.8f,9.8f,10,10,4,0,1,17,turn,source);
                    var plan=new City11Plan(new[]{building},new CityPropPlan[0]);var config=new LayoutConfig{EnableImportedBuildings=true};
                    PoolroomsCityBuildings.Attach(plan,root.transform,config);PoolroomsCityBuildings.Attach(plan,root.transform,config);
                    Assert.That(root.transform.childCount,Is.EqualTo(1),"Duplicate source root on a baked chunk");
                    var group=root.transform.GetChild(0);Assert.That(group.childCount,Is.EqualTo(1));var model=group.GetChild(0);
                    Assert.That(model.localPosition,Is.EqualTo(new Vector3(9.8f,0,9.8f)),"Importer re-centred the adapter's ground pivot");
                    Assert.That(model.GetComponentsInChildren<Collider>(true).Length,Is.Zero,"Imported exteriors replaced primary collision");
                    var lod=model.GetComponent<LODGroup>();Assert.That(lod,Is.Not.Null);Assert.That(lod.GetLODs().Length,Is.EqualTo(3));
                    int markers=0;
                    foreach(var part in model.GetComponentsInChildren<Transform>(true))
                    {
                        if(!part.name.Contains("EntryOrientationMarker"))continue;markers++;
                        Assert.That(City11Sources.TryLocalBounds(part,out var bounds),Is.True);
                        Vector3 point=model.localRotation*City11Sources.ModelPoint(part,model,bounds.center);
                        Assert.That(Mathf.Abs(point.x),Is.LessThan(.002f));Assert.That(point.y,Is.EqualTo(12.8f).Within(.003f));
                        Assert.That(point.z,Is.EqualTo(turn?5.15f:-5.15f).Within(.003f),"Imported opening faces away from the physical entrance");
                        foreach(var renderer in part.GetComponentsInChildren<Renderer>(true))Assert.That(renderer.enabled,Is.False,"Orientation marker is visible");
                    }
                    Assert.That(markers,Is.EqualTo(3),"Missing orientation data on a LOD");
                    foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))foreach(var material in renderer.sharedMaterials)
                        Assert.That(material,Is.Not.Null,"Unassigned source material");
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
    }
}
#endif
