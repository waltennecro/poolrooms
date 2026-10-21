using System.Collections.Generic;
using UnityEngine;

namespace Poolrooms
{
    // Only begins physics when a real supporting table is moved or collected.
    // Otherwise every decorative cup remains asleep without a Rigidbody.
    public sealed class PoolroomsScenerySupport : MonoBehaviour
    {
        private sealed class Binding
        {
            public PoolroomsPickup Table;
            public PoolroomsPickup Child;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Transform Parent;
        }

        private readonly List<Binding> bindings = new List<Binding>();
        private PoolroomsWorld world;
        private PoolroomsCarry carry;
        private float nextCheck;

        internal void Configure(PoolroomsWorld world, PoolroomsCarry carry)
        {
            this.world = world;
            this.carry = carry;
        }

        internal void Bind(PoolroomsPickup table, PoolroomsPickup child)
        {
            if (table == null || child == null || bindings.Count >= 16) return;
            bindings.Add(new Binding
            {
                Table = table,
                Child = child,
                Parent = table.transform.parent,
                LocalPosition = table.transform.localPosition,
                LocalRotation = table.transform.localRotation
            });
        }

        private void Update()
        {
            if (Time.time < nextCheck || Time.deltaTime <= 0 || world == null || world.FrontendSuspended) return;
            nextCheck = Time.time + .12f;
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                var binding = bindings[i];
                if (binding.Child == null || binding.Child.transform.parent != transform ||
                    carry != null && carry.Held == binding.Child)
                {
                    bindings.RemoveAt(i);
                    continue;
                }
                bool moved = binding.Table == null || binding.Table.transform.parent != binding.Parent;
                if (!moved)
                    moved = (binding.Table.transform.localPosition - binding.LocalPosition).sqrMagnitude > .0004f ||
                        Quaternion.Angle(binding.Table.transform.localRotation, binding.LocalRotation) > .5f;
                if (!moved) continue;
                if (!world.IsPositionStreamed(binding.Child.transform.position)) continue;
                binding.Child.Release(transform, binding.Child.transform.position, Vector3.zero);
                bindings.RemoveAt(i);
            }
            if (bindings.Count == 0) enabled = false;
        }
    }
}
