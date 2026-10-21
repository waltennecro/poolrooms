using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class SceneryAnchors
    {
        // Counter-clockwise local sectors keep their IDs when an earlier
        // candidate fails. Slot identity never depends on acceptance order.
        public static List<SceneryAnchor> Build(Room room, LayoutConfig config)
        {
            var result = new List<SceneryAnchor>(SceneryWorkBudget.MaximumAnchors);
            if (!SceneryWorkBudget.Eligible(room, config)) return result;
            float y = room.Floor + RegionalSourcePlanner.ContactGap;
            uint h = LiminalRegions.Hash(config.Seed, room.X, room.Z, 0x5CE110u);
            for (int corner = 0; corner < 4; corner++)
            {
                float sx = (corner & 1) == 0 ? -1 : 1;
                float sz = (corner & 2) == 0 ? -1 : 1;
                // Nested bands allow both compact and broad rooms to host a
                // cluster without taking over the reserved cross corridors.
                for (int band = 0; band < 3; band++)
                {
                    float fraction = .23f + band * .065f;
                    float jitterX = Jitter(h, corner * 6 + band * 2) * .45f;
                    float jitterZ = Jitter(h, corner * 6 + band * 2 + 1) * .45f;
                    var point = new V3(sx * room.Width * fraction + jitterX,
                        y, sz * room.Depth * fraction + jitterZ);
                    int slot = corner * 3 + band;
                    uint rank = LiminalRegions.Hash(config.Seed, room.X, room.Z,
                        0x5CE200u + (uint)slot);
                    // Furniture faces toward the central crossing, but the
                    // longitudinal room axis changes which side is preferred.
                    float yaw = room.Width >= room.Depth ? (sz < 0 ? 0 : 180) : (sx < 0 ? 90 : 270);
                    result.Add(new SceneryAnchor(point, yaw, slot, rank));
                }
            }
            // Long galleries also receive side bays between their end corners.
            if (room.Width > room.Depth * 1.3f)
            {
                Side(room, config, result, true, y);
            }
            else if (room.Depth > room.Width * 1.3f)
            {
                Side(room, config, result, false, y);
            }
            result.Sort(Compare);
            return result;
        }

        private static void Side(Room room, LayoutConfig config,
            List<SceneryAnchor> result, bool horizontal, float y)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            for (int offset = -1; offset <= 1; offset += 2)
            {
                int slot = 12 + (sign > 0 ? 2 : 0) + (offset > 0 ? 1 : 0);
                float x = horizontal ? room.Width * .17f * offset : room.Width * .34f * sign;
                float z = horizontal ? room.Depth * .34f * sign : room.Depth * .17f * offset;
                float yaw = horizontal ? (sign < 0 ? 0 : 180) : (sign < 0 ? 90 : 270);
                uint rank = LiminalRegions.Hash(config.Seed, room.X, room.Z, 0x5CE200u + (uint)slot);
                result.Add(new SceneryAnchor(new V3(x, y, z), yaw, slot, rank));
            }
        }

        private static int Compare(SceneryAnchor a, SceneryAnchor b)
        {
            int rank = a.Rank.CompareTo(b.Rank);
            return rank != 0 ? rank : a.Slot.CompareTo(b.Slot);
        }

        private static float Jitter(uint hash, int offset)
        {
            uint v = unchecked(hash + (uint)offset * 0x9E3779B9u);
            v ^= v >> 16;
            v = unchecked(v * 0x7FEB352Du);
            v ^= v >> 15;
            return (v & 65535) / 65535f * 2 - 1;
        }

        public static bool CandidateSeparated(SceneryAnchor anchor, IReadOnlyList<SceneryPlacement> placed)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                float dx = anchor.Bottom.X - placed[i].Bottom.X;
                float dz = anchor.Bottom.Z - placed[i].Bottom.Z;
                if (dx * dx + dz * dz < 16) return false;
            }
            return true;
        }

        public static int RecipeStart(Room room, LayoutConfig config, SceneryAnchor anchor)
        {
            uint h = LiminalRegions.Hash(config.Seed, room.X, room.Z, 0x5CE400u + (uint)anchor.Slot);
            return (int)(h % (uint)SceneryRecipeCatalog.Count);
        }

        public static int StableSlot(SceneryAnchor anchor, int recipe, int piece)
        {
            // Bounded, collision-free tuple encoding: anchor < 40, recipe < 64,
            // piece < 16. It does not use culture-dependent text hashing.
            return anchor.Slot * 1024 + recipe * 16 + piece;
        }
    }
}
