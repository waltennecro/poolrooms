using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class Forest39Water
    {
        // Intersect the *actual sampled terrain triangles* with the water plane.
        // A decorative circle can leave a dry trench or cover dry ground; this
        // polygon clipping keeps the transparent surface against its real shore.
        public static MeshData Build(Forest39Plan plan)
        {
            var mesh = new MeshData();
            if (plan.Pond.Width <= 0) return mesh;
            int count = Math.Min(128, Math.Max(40,
                (int)Math.Ceiling(plan.Cell / Forest39Terrain.MaximumGridSpacing)));
            float spacing = plan.Cell / count;
            float radius = plan.Pond.Width * .5f;
            int minX = Math.Max(0, (int)Math.Floor((plan.Pond.CenterX - radius + plan.Half) / spacing));
            int maxX = Math.Min(count - 1, (int)Math.Ceiling((plan.Pond.CenterX + radius + plan.Half) / spacing));
            int minZ = Math.Max(0, (int)Math.Floor((plan.Pond.CenterZ - radius + plan.Half) / spacing));
            int maxZ = Math.Min(count - 1, (int)Math.Ceiling((plan.Pond.CenterZ + radius + plan.Half) / spacing));
            var polygon = new List<V3>(4);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float ax = -plan.Half + x * spacing;
                    float az = -plan.Half + z * spacing;
                    var a = new V3(ax, plan.Height(ax, az), az);
                    var b = new V3(ax + spacing, plan.Height(ax + spacing, az), az);
                    var c = new V3(ax + spacing, plan.Height(ax + spacing, az + spacing), az + spacing);
                    var d = new V3(ax, plan.Height(ax, az + spacing), az + spacing);
                    ClipTriangle(mesh, polygon, a, b, c, plan.Pond.Level);
                    ClipTriangle(mesh, polygon, a, c, d, plan.Pond.Level);
                }
            }
            return mesh;
        }

        static void ClipTriangle(MeshData mesh, List<V3> polygon, V3 a, V3 b, V3 c, float level)
        {
            polygon.Clear();
            Edge(polygon, a, b, level);
            Edge(polygon, b, c, level);
            Edge(polygon, c, a, level);
            if (polygon.Count < 3) return;
            V3 first = Project(polygon[0], level);
            for (int index = 1; index + 1 < polygon.Count; index++)
            {
                V3 p = Project(polygon[index], level);
                V3 q = Project(polygon[index + 1], level);
                mesh.Triangle(first, p, q, V3.Up, V3.Up, V3.Up,
                    new V2(first.X, first.Z), new V2(p.X, p.Z), new V2(q.X, q.Z), Surface.Tile);
            }
        }

        static void Edge(List<V3> polygon, V3 a, V3 b, float level)
        {
            bool firstWet = a.Y <= level;
            bool secondWet = b.Y <= level;
            if (firstWet) polygon.Add(a);
            if (firstWet == secondWet) return;
            float fraction = (level - a.Y) / (b.Y - a.Y);
            polygon.Add(a + (b - a) * fraction);
        }

        static V3 Project(V3 value, float level)
        {
            return new V3(value.X, level, value.Z);
        }

        public static float SurfaceArea(MeshData mesh)
        {
            double area = 0;
            foreach (var indices in mesh.Indices)
            {
                for (int index = 0; index < indices.Count; index += 3)
                {
                    V3 a = mesh.Positions[indices[index]];
                    V3 b = mesh.Positions[indices[index + 1]];
                    V3 c = mesh.Positions[indices[index + 2]];
                    area += V3.Cross(b - a, c - a).Length * .5;
                }
            }
            return (float)area;
        }
    }
}
