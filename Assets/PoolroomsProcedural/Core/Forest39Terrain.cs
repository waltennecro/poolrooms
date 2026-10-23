using System;

namespace Poolrooms.Core
{
    public static class Forest39Terrain
    {
        public const float MaximumGridSpacing = .7f;

        public static MeshData Build(Forest39Plan plan)
        {
            var mesh = new MeshData { Theme = SurfaceTheme.Forest39Ground };
            int count = Math.Max(40, (int)Math.Ceiling(plan.Cell / MaximumGridSpacing));
            count = Math.Min(128, count);
            float spacing = plan.Cell / count;
            for (int z = 0; z < count; z++)
            {
                for (int x = 0; x < count; x++)
                {
                    float ax = -plan.Half + x * spacing;
                    float az = -plan.Half + z * spacing;
                    float bx = ax + spacing, bz = az + spacing;
                    Vertex(plan, ax, az, out var a, out var na, out var ua);
                    Vertex(plan, bx, az, out var b, out var nb, out var ub);
                    Vertex(plan, bx, bz, out var c, out var nc, out var uc);
                    Vertex(plan, ax, bz, out var d, out var nd, out var ud);
                    // A single continuous physical and rendered surface: no
                    // floating trail decals or coplanar second ground layer.
                    Surface material = plan.PathDistance((ax + bx) * .5f, (az + bz) * .5f) <
                        Forest39Plan.PathWidth * .5f ? Surface.Accent : Surface.Tile;
                    mesh.QuadUV(a, b, c, d, na, nb, nc, nd, ua, ub, uc, ud, material);
                }
            }
            return mesh;
        }

        static void Vertex(Forest39Plan plan, float x, float z, out V3 point, out V3 normal, out V2 uv)
        {
            point = new V3(x, plan.Height(x, z), z);
            normal = plan.Normal(x, z);
            // Reduce world-coordinate phase before converting to float; very
            // distant rooms do not lose their entire texture-coordinate fraction.
            // Wrap the cell origin only: wrapping each vertex would create a
            // discontinuity inside triangles that cross a 512-metre phase.
            double worldX = ((double)plan.X * plan.Cell) % 512 + x;
            double worldZ = ((double)plan.Z * plan.Cell) % 512 + z;
            uv = new V2((float)(worldX / 4), (float)(worldZ / 4));
        }

        public static float MeshHeight(Forest39Plan plan, float x, float z)
        {
            // Sample the actual piecewise-linear collider, not the analytical
            // height function, when grounding furniture or tiny collectables.
            int count = Math.Min(128, Math.Max(40, (int)Math.Ceiling(plan.Cell / MaximumGridSpacing)));
            float spacing = plan.Cell / count;
            float xx = Math.Max(0, Math.Min(count - .00001f, (x + plan.Half) / spacing));
            float zz = Math.Max(0, Math.Min(count - .00001f, (z + plan.Half) / spacing));
            int ix = (int)xx, iz = (int)zz;
            float u = xx - ix, v = zz - iz;
            float ax = -plan.Half + ix * spacing, az = -plan.Half + iz * spacing;
            float a = plan.Height(ax, az), b = plan.Height(ax + spacing, az);
            float c = plan.Height(ax + spacing, az + spacing), d = plan.Height(ax, az + spacing);
            return u >= v ? a * (1 - u) + b * (u - v) + c * v :
                a * (1 - v) + c * u + d * (v - u);
        }

        public static void Water(RoomGeometry output, Forest39Plan plan)
        {
            if (plan.Pond.Width <= 0) return;
            var pond = plan.Pond;
            output.Region = pond;
            output.Regions.Add(pond);
            output.Water.Append(Forest39Water.Build(plan));
        }

        public static bool Walkable(Forest39Plan plan, float x, float z, float radius = .35f)
        {
            float center = MeshHeight(plan, x, z);
            if (center < -.08f) return false;
            foreach (var tree in plan.Trees)
            {
                float dx = x - tree.Foot.X, dz = z - tree.Foot.Z;
                if (dx * dx + dz * dz < (tree.Radius + radius) * (tree.Radius + radius)) return false;
            }
            foreach (var stone in plan.Stones)
                if (Math.Abs(x - stone.Foot.X) < stone.Width * .5f + radius &&
                    Math.Abs(z - stone.Foot.Z) < stone.Depth * .5f + radius) return false;
            float maximum = 0;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Shapes.Pi / 4;
                float y = MeshHeight(plan, x + (float)Math.Cos(angle) * radius,
                    z + (float)Math.Sin(angle) * radius);
                maximum = Math.Max(maximum, Math.Abs(y - center));
            }
            return maximum <= radius * .6f;
        }
    }
}
