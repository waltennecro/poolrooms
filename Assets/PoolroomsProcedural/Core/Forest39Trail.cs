using System;

namespace Poolrooms.Core
{
    public sealed class Forest39Trail
    {
        public const int Samples = 24;
        public readonly V2[] Points;
        public readonly int Side;
        public readonly float Length;

        Forest39Trail(V2[] points, int side)
        {
            Points = points;
            Side = side;
            float length = 0;
            for (int i = 1; i < points.Length; i++)
            {
                float dx = points[i].X - points[i - 1].X;
                float dz = points[i].Y - points[i - 1].Y;
                length += (float)Math.Sqrt(dx * dx + dz * dz);
            }
            Length = length;
        }

        public V2 At(float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            float index = t * (Points.Length - 1);
            int a = Math.Min(Points.Length - 2, (int)index);
            float u = index - a;
            return new V2(Points[a].X * (1 - u) + Points[a + 1].X * u,
                Points[a].Y * (1 - u) + Points[a + 1].Y * u);
        }

        public float Distance(float x, float z)
        {
            float nearest = float.MaxValue;
            for (int i = 1; i < Points.Length; i++)
                nearest = Math.Min(nearest, SegmentDistance(x, z, Points[i - 1].X,
                    Points[i - 1].Y, Points[i].X, Points[i].Y));
            return nearest;
        }

        public static float SegmentDistance(float x, float z, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az;
            float length = dx * dx + dz * dz;
            float t = length < 1e-8f ? 0 : Math.Max(0, Math.Min(1, ((x - ax) * dx + (z - az) * dz) / length));
            float xx = x - ax - dx * t, zz = z - az - dz * t;
            return (float)Math.Sqrt(xx * xx + zz * zz);
        }

        public static Forest39Trail ClosedWalk(float cell, uint seed)
        {
            const int count = 48;
            float q = cell * .29f;
            float radius = System.Math.Min(3, cell * .095f);
            var points = new V2[count + 3];
            points[0] = new V2(0, 0);
            for (int index = 0; index <= count; index++)
            {
                float angle = index * Shapes.Pi * 2 / count;
                float r = radius * (1 + .08f * (float)System.Math.Sin(angle * 3 + seed % 13));
                points[index + 1] = new V2(-q + (float)System.Math.Cos(angle) * r,
                    q + (float)System.Math.Sin(angle) * r * .8f);
            }
            points[points.Length - 1] = points[0];
            return new Forest39Trail(points, -1);
        }

        public static Forest39Trail[] Build(int seed, int x, int z, float cell)
        {
            var result = new Forest39Trail[4];
            for (int side = 0; side < 4; side++)
            {
                uint h = LiminalRegions.Hash(seed, x, z, 0x390700u + (uint)side);
                float bend = (Forest39Plan.Unit(h) * 2 - 1) * Math.Min(3.8f, cell * .1f);
                float endX = Directions.DX[side] * cell * .5f;
                float endZ = Directions.DZ[side] * cell * .5f;
                var points = new V2[Samples + 1];
                for (int i = 0; i <= Samples; i++)
                {
                    float t = (float)i / Samples;
                    // Zero displacement and derivative at both ends: adjacent
                    // sectors share a centred, straight 2.4 metre threshold.
                    float wave = 16 * t * t * (1 - t) * (1 - t) * bend;
                    points[i] = new V2(endX * t + Directions.DZ[side] * wave,
                        endZ * t - Directions.DX[side] * wave);
                }
                result[side] = new Forest39Trail(points, side);
            }
            return result;
        }
    }
}
