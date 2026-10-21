using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Validation rejects an optional addition, never repairs a corrupt base mesh
    // by deleting structural triangles or moving the player's floor.
    public static class ScenerySafety
    {
        public static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool Finite(V3 point)
        {
            return Finite(point.X) && Finite(point.Y) && Finite(point.Z);
        }

        public static bool ValidBox(SceneryBox box)
        {
            return Finite(box.Min) && Finite(box.Max) &&
                box.Min.X <= box.Max.X && box.Min.Y <= box.Max.Y && box.Min.Z <= box.Max.Z;
        }

        public static bool Source(int source)
        {
            return source >= 0 && source < SourceFurniture.Count;
        }

        public static bool TrySource(string name, out int source)
        {
            source = -1;
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < SourceFurniture.Count; i++)
            {
                if (!string.Equals(SourceFurniture.Id(i), name, StringComparison.Ordinal)) continue;
                source = i;
                return true;
            }
            return false;
        }

        public static bool TryBounds(int source, V3 bottom, float yaw, out SceneryBox bounds)
        {
            bounds = default;
            if (!Source(source) || !Finite(bottom) || !Finite(yaw)) return false;
            bounds = SceneryBox.Source(source, bottom, yaw);
            return ValidBox(bounds) && bounds.Size.X > 0 && bounds.Size.Y > 0 && bounds.Size.Z > 0;
        }

        public static bool Triangle(MeshData mesh, int a, int b, int c)
        {
            if (mesh == null || a < 0 || b < 0 || c < 0) return false;
            int count = mesh.Positions.Count;
            if (a >= count || b >= count || c >= count) return false;
            return Finite(mesh.Positions[a]) && Finite(mesh.Positions[b]) && Finite(mesh.Positions[c]);
        }

        public static bool InsideRoom(Room room, SceneryBox box, float inset = .3f)
        {
            if (room == null || !ValidBox(box)) return false;
            return box.Min.X >= -room.Width * .5f + inset &&
                   box.Max.X <= room.Width * .5f - inset &&
                   box.Min.Z >= -room.Depth * .5f + inset &&
                   box.Max.Z <= room.Depth * .5f - inset &&
                   box.Min.Y >= room.Floor - .01f && box.Max.Y <= room.Height - .15f;
        }

        public static bool ClearMainRoutes(Room room, LayoutConfig config, SceneryBox box)
        {
            if (room == null || config == null || !ValidBox(box)) return false;
            // Keep both crossing axes, including closed sides: future connection
            // activation cannot strand a player behind a newly placed sofa.
            float width = Math.Max(config.DoorWidth * .5f + .55f,
                (room.Plan?.ClearHalfWidth ?? 0) + .25f);
            return (box.Min.X >= width || box.Max.X <= -width) &&
                   (box.Min.Z >= width || box.Max.Z <= -width);
        }

        public static bool Dry(IReadOnlyList<WaterRegion> waters, SceneryBox box)
        {
            if (!ValidBox(box)) return false;
            if (waters == null) return true;
            for (int i = 0; i < waters.Count; i++)
            {
                var water = waters[i];
                if (!Finite(water.Level) || !Finite(water.Bottom) ||
                    !Finite(water.CenterX) || !Finite(water.CenterZ) ||
                    !Finite(water.Width) || !Finite(water.Depth) || !Finite(water.Radius)) return false;
                if (water.Level <= box.Min.Y + .002f) continue;
                // Deliberately conservative rectangle/rectangle test. Testing
                // only corners misses a narrow pool passing beneath a long seat.
                if (box.Max.X >= water.CenterX - water.Width * .5f &&
                    box.Min.X <= water.CenterX + water.Width * .5f &&
                    box.Max.Z >= water.CenterZ - water.Depth * .5f &&
                    box.Min.Z <= water.CenterZ + water.Depth * .5f) return false;
            }
            return true;
        }

        public static bool SupportedBy(SceneryPlacement child, SceneryPlacement table)
        {
            if (child.ParentSlot != table.Slot || table.Role != SceneryRole.Table) return false;
            if (child.Cluster != table.Cluster || child.Recipe != table.Recipe) return false;
            float tableTop = table.Bottom.Y + SourceFurniture.Dimensions(table.Source).Y;
            if (Math.Abs(child.Bottom.Y - tableTop - RegionalSourcePlanner.ContactGap) > .002f) return false;
            // Rotate each footprint corner back into the real table rectangle.
            // A world AABB alone incorrectly accepts objects beyond a rotated edge.
            var half = SourceFurniture.HalfExtents(child.Source, child.Yaw);
            var size = SourceFurniture.Dimensions(table.Source);
            float a = -table.Yaw * Shapes.Pi / 180;
            float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
            for (int dx = -1; dx <= 1; dx += 2)
            for (int dz = -1; dz <= 1; dz += 2)
            {
                float x = child.Bottom.X + dx * half.X - table.Bottom.X;
                float z = child.Bottom.Z + dz * half.Z - table.Bottom.Z;
                float localX = c * x + s * z;
                float localZ = -s * x + c * z;
                if (Math.Abs(localX) > size.X * .5f - .018f ||
                    Math.Abs(localZ) > size.Z * .5f - .018f) return false;
            }
            // Small cafe tables are round; their rectangular bounds aren't a
            // valid support at corners even though the broad-phase passed.
            if (table.Source == 7)
            {
                float radius = Math.Min(size.X, size.Z) * .5f - .018f;
                for (int dx = -1; dx <= 1; dx += 2)
                for (int dz = -1; dz <= 1; dz += 2)
                {
                    float x = child.Bottom.X + dx * half.X - table.Bottom.X;
                    float z = child.Bottom.Z + dz * half.Z - table.Bottom.Z;
                    if (x * x + z * z > radius * radius) return false;
                }
            }
            return true;
        }

        public static bool DistinctSlots(IReadOnlyList<SceneryPlacement> placements)
        {
            if (placements == null) return false;
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (p.Slot < 0 || !Source(p.Source) || !Finite(p.Bottom) || !Finite(p.Yaw)) return false;
                for (int j = 0; j < i; j++) if (placements[j].Slot == p.Slot) return false;
            }
            return true;
        }

        public static bool ParentPresent(SceneryPlacement child, IReadOnlyList<SceneryPlacement> placements)
        {
            if (child.ParentSlot < 0) return child.Role != SceneryRole.Tabletop;
            for (int i = 0; i < placements.Count; i++)
                if (placements[i].Slot == child.ParentSlot) return SupportedBy(child, placements[i]);
            return false;
        }

        public static bool ValidPlacement(SceneryPlacement p, Room room, LayoutConfig config)
        {
            return TryBounds(p.Source, p.Bottom, p.Yaw, out var bounds) &&
                InsideRoom(room, bounds) && ClearMainRoutes(room, config, bounds);
        }

        public static bool KnownRecipe(int recipe)
        {
            return recipe >= 0 && recipe < SceneryRecipeCatalog.Count;
        }

        public static bool ValidWater(WaterRegion water)
        {
            return Finite(water.Width) && Finite(water.Depth) && Finite(water.Radius) &&
                Finite(water.CenterX) && Finite(water.CenterZ) && Finite(water.Level) &&
                Finite(water.Bottom) && water.Width >= 0 && water.Depth >= 0 &&
                water.Radius >= 0 && water.Radius <= Math.Min(water.Width, water.Depth) * .5f;
        }

        public static bool AllWaterValid(IReadOnlyList<WaterRegion> waters)
        {
            if (waters == null) return true;
            for (int i = 0; i < waters.Count; i++) if (!ValidWater(waters[i])) return false;
            return true;
        }

        public static bool ValidConfiguration(LayoutConfig config)
        {
            return config != null && Finite(config.DoorWidth) && config.DoorWidth > 0 &&
                Finite(config.DoorHeight) && config.DoorHeight > 0 &&
                Finite(config.WallThickness) && config.WallThickness >= 0;
        }

        public static float NormalizedYaw(float yaw)
        {
            if (!Finite(yaw)) return 0;
            float result = yaw % 360;
            return result < 0 ? result + 360 : result;
        }
    }
}
