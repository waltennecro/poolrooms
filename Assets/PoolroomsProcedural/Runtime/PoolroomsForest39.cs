using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    public static class PoolroomsForest39
    {
        public static void AttachCanopy(Forest39Canopy canopy, Transform parent,
            PoolroomsProfile profile, PoolroomsChunk owner)
        {
            if (canopy == null) return;
            var root = new GameObject("Level 39 / batched oak canopy");
            root.transform.SetParent(parent, false);
            var levels = new LOD[3];
            var thresholds = new[] { .19f, .075f, .008f };
            for (int lod = 0; lod < 3; lod++)
            {
                var group = new GameObject("Oak geometry LOD " + lod);
                group.transform.SetParent(root.transform, false);
                MeshFactory.Create("Bark", canopy.Wood[lod], group.transform, profile, owner, false);
                MeshFactory.Create("Leaves", canopy.Leaves[lod], group.transform, profile, owner, false);
                var renderers = group.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
                    renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                }
                levels[lod] = new LOD(thresholds[lod], renderers);
            }
            var lodGroup = root.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;
            lodGroup.SetLODs(levels);
            lodGroup.RecalculateBounds();
        }

        public static void Attach(PoolroomsWorld world, Room room, Transform parent)
        {
            if (world == null || room.Plan?.Forest39 == null) return;
            var plan = room.Plan.Forest39;
            var owner = parent.GetComponent<PoolroomsChunk>();
            if (owner == null) return;
            var actor = world.viewer != null ? world.viewer.GetComponentInParent<PoolroomsExplorer>() : null;
            var inventory = actor != null ? PoolroomsInventory.For(actor) : null;
            foreach (var action in Forest39Activities.Plan(plan))
            {
                string key = Forest39Activities.Key(world.CurrentLayout.Config.Seed, room.X, room.Z, action.Slot);
                bool collectable = action.Kind == Forest39Activity.FallenLeaf || action.Kind == Forest39Activity.Acorn;
                if (collectable && inventory != null && inventory.WasCollected(key)) continue;
                var go = new GameObject(Forest39Activities.Name(action.Kind));
                go.transform.SetParent(parent, false);
                var point = action.Bottom;
                if (action.Kind == Forest39Activity.Listening)
                    point = new V3(plan.Rest.X, 0, plan.Rest.Z - 1.1f);
                else if (collectable)
                    point.Y = Forest39Terrain.MeshHeight(plan, point.X, point.Z) + .003f;
                go.transform.localPosition = Vec(point);
                go.transform.localRotation = Quaternion.Euler(0, action.Yaw, 0);
                var state = new Forest39ActivityState(action.Kind, plan.Seed + (uint)action.Slot);
                var body = Forest39ActivityGeometry.Body(action.Kind, state.Position, state.Limit);
                var visual = action.Kind == Forest39Activity.Listening ? null :
                    MeshFactory.Create("Forest study", body, go.transform, world.profile, owner, false);
                var size = Forest39ActivityGeometry.Size(action.Kind);
                var collider = go.AddComponent<BoxCollider>();
                collider.size = action.Kind == Forest39Activity.Listening ? new Vector3(3.5f, .95f, .5f) : Vec(size);
                collider.center = Vector3.up * size.Y * .5f;
                var interactable = go.AddComponent<PoolroomsInteractable>();
                interactable.world = world;
                interactable.key = key;
                interactable.title = Forest39Activities.Name(action.Kind);
                interactable.activity = RoomActivity.Reading;
                var activity = go.AddComponent<PoolroomsForest39Activity>();
                activity.Configure(action.Kind, key, plan.Seed + (uint)action.Slot, visual,
                    Forest39Stories.Note(plan.Clearing), owner);
                if (inventory != null) activity.Restore(inventory);
            }
            AttachSupplies(world, room, parent, inventory);
            var support=parent.gameObject.AddComponent<PoolroomsForest39Support>();support.world=world;
        }

        static void AttachSupplies(PoolroomsWorld world, Room room, Transform parent, PoolroomsInventory inventory)
        {
            var plan = room.Plan.Forest39;
            for (int index = 0; index < 3; index++)
            {
                int food = index == 0 ? ProvisionCatalog.AlmondWater : index == 1 ? 69 : 0;
                string key = Forest39Activities.Key(world.CurrentLayout.Config.Seed, room.X, room.Z, 100 + index);
                if (inventory != null && inventory.WasCollected(key)) continue;
                var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;
                var carry=actor!=null?actor.GetComponent<PoolroomsCarry>():null;
                if(carry!=null&&carry.WasTaken(key))continue;
                float x = plan.Rest.X + (index - 1) * .22f;
                float z = plan.Rest.Z + .7f;
                float y = SourceFurniture.Dimensions(7).Y + .009f;
                // Source food items own their resources: a carried object can
                // survive unloading the forest sector that originally held it.
                PoolroomsPickup.CreateFood(key, food, new Vector3(x, y, z), parent, world.profile);
            }
        }

        public static PoolroomsPickup CreateItem(int id, string key, Vector3 position, PoolroomsProfile profile)
        {
            if (Forest39Activities.Item(id) == null) return null;
            var kind = id == 0 ? Forest39Activity.FallenLeaf : id == 1 ? Forest39Activity.Acorn : Forest39Activity.FieldNotebook;
            var go = new GameObject(Forest39Activities.Item(id).Name);
            go.transform.position = position;
            var owner = go.AddComponent<PoolroomsChunk>();
            MeshFactory.Create("Forest keepsake", Forest39ActivityGeometry.Keepsake(id), go.transform, profile, owner, false);
            var size = id==2?new V3(.32f,.035f,.24f):Forest39ActivityGeometry.Size(kind);
            var shape = go.AddComponent<BoxCollider>();
            shape.size = Vec(size);
            shape.center = Vector3.up * size.Y * .5f;
            var pickup = go.AddComponent<PoolroomsPickup>();
            pickup.SpawnKey = key;
            pickup.InventoryKey = "forest39:" + id;
            pickup.shape = shape;
            pickup.mass = id == 0 ? .005f : .02f;
            pickup.CarryRadius = size.Length * .5f + .015f;
            pickup.surface = PickupSurface.Rigid;
            return pickup;
        }

        static Vector3 Vec(V3 value) { return new Vector3(value.X, value.Y, value.Z); }
    }
}
