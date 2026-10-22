#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class InteractableLifetimeTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void SourceInteractionCanBeAddedWithoutNativeConstructorAllocation(bool active)
        {
            var source=new GameObject("Licensed source constructor regression");
            try
            {
                source.SetActive(active);
                var interaction=source.AddComponent<PoolroomsInteractable>();
                Assert.That(interaction,Is.Not.Null);
                source.SetActive(true);
                LogAssert.NoUnexpectedReceived();
            }
            finally {Object.DestroyImmediate(source);}
        }
    }
}
#endif
