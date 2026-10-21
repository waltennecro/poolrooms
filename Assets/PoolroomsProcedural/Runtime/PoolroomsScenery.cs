using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // One bounded, finite coroutine per furnished chunk; no idle Update loop.
    // The chunk owns presentation lifetime. Held pickups keep their normal owner.
    [DisallowMultipleComponent]
    public sealed class PoolroomsScenery : MonoBehaviour
    {
        private PoolroomsWorld world;
        private Room room;
        private SceneryPlan plan;
        private PoolroomsCarry carry;
        private readonly Dictionary<int, PoolroomsPickup> spawned = new Dictionary<int, PoolroomsPickup>();
        private readonly HashSet<int> completed = new HashSet<int>();
        private Coroutine worker;
        public int CreatedObjects { get; private set; }
        public int RejectedClusters { get; private set; }

        public static void Attach(PoolroomsWorld world, Room room, Transform parent, SceneryPlan plan)
        {
            if (world == null || room == null || parent == null || plan == null) return;
            if (plan.Placements.Count == 0 || parent.GetComponent<PoolroomsScenery>() != null) return;
            if (world.CurrentLayout == null || !SceneryPlanner.ValidPlan(plan, room, world.CurrentLayout.Config)) return;
            var scenery = parent.gameObject.AddComponent<PoolroomsScenery>();
            scenery.world = world;
            scenery.room = room;
            scenery.plan = plan;
            var actor = world.viewer != null ? world.viewer.GetComponentInParent<PoolroomsExplorer>() : null;
            scenery.carry = actor != null ? actor.GetComponent<PoolroomsCarry>() : null;
            scenery.worker = scenery.StartCoroutine(scenery.Populate());
        }

        private IEnumerator Populate()
        {
            // Base floor and its colliders are already attached. Decorations
            // wait a frame and never hold up the streaming readiness contract.
            yield return null;
            for (int i = 0; i < plan.Placements.Count; i++)
            {
                if (world == null || world.CurrentLayout == null) yield break;
                while (world != null && world.FrontendSuspended) yield return null;
                if (world == null) yield break;
                int cluster = plan.Placements[i].Cluster;
                if (completed.Contains(cluster)) continue;
                completed.Add(cluster);
                var node = new GameObject("Scenery · " + cluster);
                node.transform.SetParent(transform, false);
                node.SetActive(false);
                bool failed = false;
                int frameObjects = 0;
                double frameStart = Time.realtimeSinceStartupAsDouble;
                for (int j = i; j < plan.Placements.Count; j++)
                {
                    var piece = plan.Placements[j];
                    if (piece.Cluster != cluster) continue;
                    string key = piece.Key(world.CurrentLayout.Config.Seed, room.X, room.Z);
                    if (carry != null && carry.WasTaken(key)) continue;
                    if (piece.ParentSlot >= 0 && !spawned.ContainsKey(piece.ParentSlot)) continue;
                    Exception failure = null;
                    PoolroomsPickup pickup = null;
                    try { pickup = Create(piece, key, node.transform); }
                    catch (Exception error) { failure = error; }
                    if (pickup == null || failure != null)
                    {
                        // Optional content failures are isolated to this cluster;
                        // they cannot terminate the world's structural streaming.
                        Debug.LogWarning("Optional scenery cluster could not load source " + piece.Source +
                            (failure != null ? ": " + failure.GetBaseException().Message : "."), this);
                        failed = true;
                        break;
                    }
                    spawned.Add(piece.Slot, pickup);
                    frameObjects++;
                    if (frameObjects >= 2 || Time.realtimeSinceStartupAsDouble - frameStart > .0015)
                    {
                        yield return null;
                        while (world != null && world.FrontendSuspended) yield return null;
                        if (world == null) yield break;
                        frameObjects = 0;
                        frameStart = Time.realtimeSinceStartupAsDouble;
                    }
                }
                if (failed)
                {
                    RejectedClusters++;
                    ForgetCluster(cluster);
                    Destroy(node);
                    continue;
                }
                BindSupports(node.transform, cluster);
                CreatedObjects += node.GetComponentsInChildren<PoolroomsPickup>(true).Length;
                node.SetActive(true);
                Physics.SyncTransforms();
                yield return null;
            }
            // Dictionaries are construction scratch, not a hidden session cache.
            spawned.Clear();
            completed.Clear();
            worker = null;
        }

        private PoolroomsPickup Create(SceneryPlacement piece, string key, Transform parent)
        {
            var point = new Vector3(piece.Bottom.X, piece.Bottom.Y, piece.Bottom.Z);
            var pickup = PoolroomsPickup.CreateSource(key, SourceFurniture.Id(piece.Source),
                point, parent, SourceFurniture.Longest(piece.Source));
            if (pickup == null) return null;
            pickup.transform.localRotation = Quaternion.Euler(0, piece.Yaw, 0);
            pickup.name = SceneryRecipeCatalog.Get(piece.Recipe).Name + " · " + pickup.name;
            if (SceneryStories.CanBind(piece.Role) && SceneryStories.TryRead(piece.Recipe,
                room.Environment, world.CurrentLayout.Config.Seed, room.X, room.Z,
                out string title, out string note))
            {
                var use = pickup.GetComponent<PoolroomsInteractable>();
                if (use == null) use = pickup.gameObject.AddComponent<PoolroomsInteractable>();
                // Keep genuine specialised interactions: no plant/music/seat
                // behavior is replaced just to display a story on that object.
                if (use.activity == RoomActivity.Notice || use.activity == RoomActivity.Reading)
                {
                    use.activity = RoomActivity.Reading;
                    use.key = key;
                    use.title = title;
                    use.note = note;
                    use.world = world;
                }
            }
            return pickup;
        }

        private void BindSupports(Transform group, int cluster)
        {
            PoolroomsScenerySupport monitor = null;
            foreach (var piece in plan.Placements)
            {
                if (piece.Cluster != cluster || piece.ParentSlot < 0) continue;
                if (!spawned.TryGetValue(piece.Slot, out var child) || child == null) continue;
                if (!spawned.TryGetValue(piece.ParentSlot, out var table) || table == null) continue;
                if (monitor == null)
                {
                    monitor = group.gameObject.AddComponent<PoolroomsScenerySupport>();
                    monitor.Configure(world, carry);
                }
                monitor.Bind(table, child);
            }
        }

        private void ForgetCluster(int cluster)
        {
            foreach (var piece in plan.Placements)
                if (piece.Cluster == cluster) spawned.Remove(piece.Slot);
        }

        private void OnDestroy()
        {
            if (worker != null) StopCoroutine(worker);
            spawned.Clear();
            completed.Clear();
        }
    }
}
