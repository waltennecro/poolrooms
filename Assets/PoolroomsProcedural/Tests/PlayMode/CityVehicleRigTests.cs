#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class CityVehicleRigTests
    {
        [TestCase("ToyCar")]
        [TestCase("U_CarConcept")]
        public void ImportedTyresMatchWheelContactsAndAllLodsRemainAnimated(string source)
        {
            var parent=new GameObject("Car wheel rig check");
            try
            {
                var car=PoolroomsCityVehicle.Create(null,parent.transform,Vector3.zero,"native-car-test",source);Assert.That(car,Is.Not.Null);
                var wheels=car.GetComponentsInChildren<WheelCollider>();Assert.That(wheels.Length,Is.EqualTo(4));
                var lod=car.GetComponent<LODGroup>();Assert.That(lod,Is.Not.Null);Assert.That(lod.GetLODs().Length,Is.EqualTo(3));
                foreach(var wheel in wheels)
                {
                    Assert.That(wheel.enabled,Is.False,"Parked car must not simulate four wheels");
                    Assert.That(wheel.radius,source=="ToyCar"?Is.InRange(.28f,.30f):Is.InRange(.2f,.6f));
                    string part=wheel.name.Split(' ')[0];var pivot=car.transform.Find(part+" / suspension visual");Assert.That(pivot,Is.Not.Null);
                    Assert.That(pivot.GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThanOrEqualTo(3),"Every LOD of the tyre must follow its suspension");
                    if(source=="U_CarConcept")Assert.That(car.transform.Find(part+" / fixed caliper").GetComponentsInChildren<MeshFilter>().Length,Is.GreaterThanOrEqualTo(3),"Brake calipers must steer without spinning with the tyre");
                    Assert.That((wheel.transform.localPosition-pivot.localPosition-Vector3.up*.09f).magnitude,Is.LessThan(.001f));
                    Assert.That(wheel.transform.localPosition.z>0,Is.EqualTo(part.Contains("Wheel_F")),"Imported nose direction is reversed");
                }
                Assert.That(car.GetComponent<Rigidbody>().isKinematic,Is.True);
                foreach(var tier in lod.GetLODs())foreach(var renderer in tier.renderers)Assert.That(renderer,Is.Not.Null,"Reparenting wheels lost their LOD renderer");
            }
            finally{Object.DestroyImmediate(parent);}
        }
    }
}
#endif
