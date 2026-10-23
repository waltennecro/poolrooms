#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace Poolrooms.Tests
{
    public sealed class Forest39PlayModeTests
    {
        [UnityTest]
        public IEnumerator CurvedPathColliderSupportsARealWalkingCapsule()
        {
            GameObject root = null;
            try
            {
                root = new GameObject("Level 39 native path regression");
                root.transform.position = Vector3.up * 18000;
                var owner = root.AddComponent<PoolroomsChunk>();
                var config = new LayoutConfig
                {
                    Continuous = true, CellSize = 36,
                    Procedural = LiminalGeneration.EnchantedForest39,
                    EncounterExploration = true
                };
                var layout = new Layout(config);
                var room = layout.At(0, 0);
                var data = Architecture.Build(room, layout);
                var collider = MeshFactory.CreateCollider("Forest ground", data.Structure, root.transform, owner);
                var obstacles = MeshFactory.CreateCollider("Forest trunks", data.CollisionOnly, root.transform, owner);
                Physics.SyncTransforms();
                yield return null;
                foreach (var trail in room.Plan.Forest39.Trails)
                {
                    for (int sample = 0; sample <= 40; sample++)
                    {
                        var point = trail.At(sample / 40f);
                        Vector3 world = root.transform.position + new Vector3(point.X, .4f, point.Y);
                        Assert.That(collider.Raycast(new Ray(world, Vector3.down), out var hit, .8f), Is.True,
                            "Real PhysX collider has no trail floor");
                        Assert.That(Mathf.Abs(hit.point.y - root.transform.position.y), Is.LessThan(.08f));
                        Assert.That(obstacles.Raycast(new Ray(hit.point + Vector3.up * .9f, Vector3.up), out _, .9f), Is.False,
                            "Trunk/berm blocks the path headroom");
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        [UnityTest]
        public IEnumerator GeneratedCanopyHasThreeMaterialBoundLodsAndNoLeafCollider()
        {
            GameObject root = null;
            PoolroomsProfile profile = null;
            try
            {
                root = new GameObject("Forest canopy native regression");
                var owner = root.AddComponent<PoolroomsChunk>();
                profile = ScriptableObject.CreateInstance<PoolroomsProfile>();
                var plan = new Forest39Plan(new LayoutConfig { CellSize = 36 }, 4, -6);
                var canopy = Forest39Oak.Build(plan);
                PoolroomsForest39.AttachCanopy(canopy, root.transform, profile, owner);
                yield return null;
                var group = root.GetComponentInChildren<LODGroup>();
                Assert.That(group, Is.Not.Null);
                Assert.That(group.lodCount, Is.EqualTo(3));
                var lods = group.GetLODs();
                for (int index = 0; index < lods.Length; index++)
                {
                    Assert.That(lods[index].renderers.Length, Is.EqualTo(2));
                    foreach (var renderer in lods[index].renderers)
                    {
                        foreach (var material in renderer.sharedMaterials)
                        {
                            Assert.That(material, Is.Not.Null);
                            Assert.That(material.shader, Is.Not.Null);
                            Assert.That(material.shader.isSupported, Is.True);
                        }
                    }
                }
                Assert.That(group.GetComponentsInChildren<Collider>(), Is.Empty,
                    "Changing canopy LOD must never change physical trunk collision");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (profile != null) Object.DestroyImmediate(profile);
            }
        }

        [UnityTest]
        public IEnumerator ForestKeepsakesCanLeaveInventoryAsOwnedPhysicalObjects()
        {
            PoolroomsProfile profile = null;
            try
            {
                profile = ScriptableObject.CreateInstance<PoolroomsProfile>();
                for (int id = 0; id < 3; id++)
                {
                    var pickup = PoolroomsForest39.CreateItem(id, "forest-native-" + id,
                        new Vector3(0, 19000 + id, 0), profile);
                    Assert.That(pickup, Is.Not.Null);
                    try
                    {
                        Assert.That(pickup.InventoryKey, Is.EqualTo("forest39:" + id));
                        Assert.That(pickup.GetComponent<PoolroomsChunk>(), Is.Not.Null);
                        Assert.That(pickup.GetComponent<Collider>(), Is.Not.Null);
                        Assert.That(pickup.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
                        yield return null;
                    }
                    finally
                    {
                        Object.DestroyImmediate(pickup.gameObject);
                    }
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (profile != null) Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void NativeResourcesIncludeAllThreeScansAndSpatialMonoClips()
        {
            foreach (string role in new[] { "Ground", "Bark", "Stone" })
            {
                foreach (string suffix in new[] { "Albedo", "Normal", "Mask", "Height" })
                {
                    var texture = Resources.Load<Texture2D>("Forest39/Textures/" + role + "_" + suffix);
                    Assert.That(texture, Is.Not.Null, role + " " + suffix);
                    Assert.That(Mathf.Max(texture.width, texture.height), Is.EqualTo(4096));
                    Assert.That(texture.mipmapCount, Is.GreaterThan(1));
                }
            }
            foreach (string name in new[] { "OakWind", "OakLeaves0", "OakLeaves1", "OakLeaves2" })
            {
                var clip = Resources.Load<AudioClip>("Forest39/Audio/" + name);
                Assert.That(clip, Is.Not.Null);
                Assert.That(clip.channels, Is.EqualTo(1), "Three-dimensional audio source must be mono");
                Assert.That(clip.length, Is.GreaterThan(1));
            }
        }
    }
}
#endif
