#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class WorkshopDisplayTests
    {
        [UnityTest]
        public IEnumerator EditingAWorkshopReusesOneMeshWithoutAddingLightsOrFloorColliders()
        {
            var root = new GameObject("Workshop native test");
            root.SetActive(false);
            var profile = ScriptableObject.CreateInstance<PoolroomsProfile>();
            Mesh uploaded = null;
            try
            {
                var world = root.AddComponent<PoolroomsWorld>();
                world.profile = profile;
                world.generateOnStart = false;
                world.enabled = false;
                var room = new GameObject("Room");
                room.transform.SetParent(root.transform, false);
                room.AddComponent<PoolroomsChunk>();
                var node = new GameObject("Canals");
                node.transform.SetParent(room.transform, false);
                var station = node.AddComponent<PoolroomsStation>();
                station.world = world;
                station.kind = StationKind.WaterChannels;
                station.key = "native-workshop";
                station.enabled = false;
                root.SetActive(true);
                var board = WorkshopCatalog.Create(station.kind, 19);
                PoolroomsWorkshopDisplay.Show(station, board);
                uploaded = node.GetComponentInChildren<MeshFilter>(true).sharedMesh;
                Assert.That(uploaded, Is.Not.Null);
                Assert.That(uploaded.vertexCount, Is.GreaterThan(0));
                for (int i = 0; i < 100; i++)
                {
                    board.Apply(new WorkshopMove(i % 16));
                    PoolroomsWorkshopDisplay.Show(station, board);
                    Assert.That(node.GetComponentInChildren<MeshFilter>(true).sharedMesh, Is.SameAs(uploaded));
                }
                Assert.That(node.GetComponentsInChildren<MeshFilter>(true).Length, Is.EqualTo(1));
                Assert.That(node.GetComponentsInChildren<Collider>(true).Length, Is.Zero);
                Assert.That(node.GetComponentsInChildren<Light>(true).Length, Is.Zero);
                Assert.That(node.GetComponentsInChildren<ReflectionProbe>(true).Length, Is.Zero);
                var renderer = node.GetComponentInChildren<MeshRenderer>(true);
                Assert.That(renderer.sharedMaterials.Length, Is.EqualTo(6));
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.That(material, Is.Not.Null);
                    Assert.That(material.shader.name, Does.Not.Contain("InternalError"));
                }
                Assert.That(uploaded.normals.Length, Is.EqualTo(uploaded.vertexCount));
                Assert.That(uploaded.tangents.Length, Is.EqualTo(uploaded.vertexCount));
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(profile);
            }
            yield return null;
            Assert.That(uploaded == null, Is.True, "Workshop-owned mesh survived chunk destruction");
        }
    }
}
#endif
