#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;

namespace Poolrooms.Tests
{
    public sealed class HelicopterWorldTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LicensedHelicopterImportsMovingPartsAndLitMaterials(bool economy)
        {
            var root=new GameObject("Helicopter source integration");root.transform.position=new Vector3(26000,1000,26000);
            try
            {
                var model=PoolroomsHelicopter.Create(null,root.transform,Vector3.zero,"native-test-heli");
                Assert.That(model,Is.Not.Null,"Native OBJ node/material import must preserve rotor markers");
                var main=model.transform.Find("Main rotor pivot");var tail=model.transform.Find("Tail rotor pivot");
                Assert.That(main,Is.Not.Null);Assert.That(tail,Is.Not.Null);
                Assert.That(main.childCount,Is.EqualTo(3),"Every detail tier needs to animate the main rotor");Assert.That(tail.childCount,Is.EqualTo(3));
                Assert.That(tail.localPosition.z,Is.LessThan(main.localPosition.z-3),"Tail or nose import orientation is reversed");
                var tiers=model.GetComponent<LODGroup>().GetLODs();Assert.That(tiers.Length,Is.EqualTo(3));
                foreach(var tier in tiers)foreach(var renderer in tier.renderers)
                {
                    Assert.That(renderer.name.Contains("_pivot")||renderer.name.Contains("nose_axis"),Is.False,"Pivot markers must not render in any detail tier");
                    foreach(var material in renderer.sharedMaterials){Assert.That(material,Is.Not.Null);Assert.That(material.shader.isSupported,Is.True);Assert.That(material.shader.name,Does.Not.Contain("Unlit"));}
                }
                Assert.That(model.GetComponent<Rigidbody>().isKinematic,Is.True);Assert.That(model.GetComponents<CapsuleCollider>().Length,Is.EqualTo(3));
                if(economy)
                {
                    var lower=City11Sources.Create("H_MH6",root.transform,Vector3.zero,9.4f,out _,1);
                    Assert.That(lower,Is.Not.Null);Assert.That(lower.GetComponent<LODGroup>().GetLODs().Length,Is.EqualTo(2),"Economic import must start at reduced detail");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
#endif
