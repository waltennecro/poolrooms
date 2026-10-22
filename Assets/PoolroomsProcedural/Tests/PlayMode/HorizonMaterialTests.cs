#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Poolrooms.Core;

namespace Poolrooms.Tests
{
    public sealed class HorizonMaterialTests
    {
        [UnityTest]
        public IEnumerator ColoredGlazingReusesOwnedMaterialAndReleasesItWithTheChunk()
        {
            PoolroomsProfile profile=null;GameObject root=null;Material amber=null;
            try
            {
                profile=ScriptableObject.CreateInstance<PoolroomsProfile>();
                root=new GameObject("Horizon glass ownership test");
                var owner=root.AddComponent<PoolroomsChunk>();
                amber=PoolroomsHorizonGlass.Resolve(0,(int)Surface.Tile,profile,owner);
                var same=PoolroomsHorizonGlass.Resolve(0,(int)Surface.Tile,profile,owner);
                var teal=PoolroomsHorizonGlass.Resolve(1,(int)Surface.Tile,profile,owner);
                Assert.That(same,Is.SameAs(amber));
                Assert.That(teal,Is.Not.SameAs(amber));
                Assert.That(owner.ownedMaterials,Does.Contain(amber));
                Assert.That(amber.enableInstancing,Is.True);
                string property=amber.HasProperty("_BaseColor")?"_BaseColor":"_Color";
                var color=amber.GetColor(property);
                Assert.That(color.r,Is.GreaterThan(color.b));
                Assert.That(color.a,Is.LessThan(.5f));
                Object.Destroy(root);root=null;
                yield return null;yield return null;
                Assert.That(amber==null,Is.True,"Unloaded stained glass retained an owned material.");
            }
            finally
            {
                if(root!=null)Object.DestroyImmediate(root);
                if(profile!=null)Object.DestroyImmediate(profile);
            }
        }
    }
}
#endif
