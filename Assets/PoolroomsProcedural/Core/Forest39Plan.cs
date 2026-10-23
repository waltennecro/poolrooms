using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum Forest39Clearing
    {
        MossGarden, ReflectionPool, ReadingGrove, Herbarium,
        FallenOak, StoneCircle, PicnicGlade, FernHollow,
        SketchingPlace, ListeningGrove, RainShelter, WayfarersRest
    }

    public readonly struct Forest39Tree
    {
        public readonly V3 Foot;
        public readonly float Height, Radius, Crown, Yaw;
        public readonly uint Seed;

        public Forest39Tree(V3 foot, float height, float radius, float crown, float yaw, uint seed)
        {
            Foot = foot;
            Height = height;
            Radius = radius;
            Crown = crown;
            Yaw = yaw;
            Seed = seed;
        }
    }

    public readonly struct Forest39Stone
    {
        public readonly V3 Foot;
        public readonly float Width, Height, Depth;
        public readonly uint Seed;

        public Forest39Stone(V3 foot, float width, float height, float depth, uint seed)
        {
            Foot = foot;
            Width = width;
            Height = height;
            Depth = depth;
            Seed = seed;
        }
    }

    // The square is a streaming address, never an enclosed square room.
    // Paths and terrain meet exactly at the common cell edge. All other details
    // are derived from coordinates, so loading order cannot alter this plan.
    public sealed class Forest39Plan
    {
        public const float PathWidth = 2.4f;
        public const float PlayerClearance = .65f;
        public const int MaximumTrees = 14;
        public const int MaximumStones = 9;
        public const int MaximumCandidates = 112;
        public readonly uint Seed;
        public readonly int X, Z;
        public readonly float Cell, Half;
        public readonly Forest39Clearing Clearing;
        public readonly Forest39Trail[] Trails;
        public readonly Forest39Trail Loop;
        public readonly Forest39Tree[] Trees;
        public readonly Forest39Stone[] Stones;
        public readonly WaterRegion Pond;
        public readonly PlanRect Rest, Feature;
        public readonly float HillHeight;
        public readonly bool[] OpenEdges;

        public Forest39Plan(LayoutConfig config, int x, int z)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (float.IsNaN(config.CellSize) || float.IsInfinity(config.CellSize) || config.CellSize < 24 || config.CellSize > 256)
                throw new ArgumentOutOfRangeException(nameof(config.CellSize));
            X = x;
            Z = z;
            Cell = config.CellSize;
            Half = Cell * .5f;
            Seed = LiminalRegions.Hash(config.Seed, x, z, 0x390001u);
            Clearing = (Forest39Clearing)(Seed % 12);
            HillHeight = .85f + ((Seed >> 8) % 7) * .16f;
            Trails = Forest39Trail.Build(config.Seed, x, z, Cell);
            Loop = Seed % 3 == 0 ? Forest39Trail.ClosedWalk(Cell, Seed) : null;
            // The rest area and pond occupy opposite quadrants. A path spur
            // leads to each; the origin remains a level, clear spawn point.
            float q = Half * .58f;
            Rest = new PlanRect(-q - 2.5f, -q - 2, -q + 2.5f, -q + 2);
            Feature = new PlanRect(q - 2.3f, -q - 1.8f, q + 2.3f, -q + 1.8f);
            bool pond = Clearing == Forest39Clearing.ReflectionPool ||
                Clearing == Forest39Clearing.FernHollow || Seed % 5 == 0;
            float radius = Math.Min(2.35f, Half * .17f);
            Pond = pond ? new WaterRegion
            {
                CenterX = q, CenterZ = q, Width = radius * 2, Depth = radius * 2,
                Radius = radius, Bottom = -.48f, Level = -.065f
            } : new WaterRegion();
            OpenEdges = new bool[4];
            for(int side=0;side<4;side++)OpenEdges[side]=Forest39Planner.Same(config,x,z,side);
            Trees = PlaceTrees(config.Seed);
            Stones = PlaceStones(config.Seed);
        }

        public float PathDistance(float x, float z)
        {
            float distance = (float)Math.Sqrt(x * x + z * z);
            distance = Math.Max(0, distance - 2.5f);
            foreach (var trail in Trails)
                distance = Math.Min(distance, trail.Distance(x, z));
            if (Loop != null) distance = Math.Min(distance, Loop.Distance(x, z));
            // Explicit accessible spurs, not random props buried among trees.
            distance = Math.Min(distance, Forest39Trail.SegmentDistance(x, z, 0, 0, Rest.X, Rest.Z));
            distance = Math.Min(distance, Forest39Trail.SegmentDistance(x, z, 0, 0, Feature.X, Feature.Z));
            if (Pond.Width > 0)
                distance = Math.Min(distance, Forest39Trail.SegmentDistance(x, z, 0, 0,
                    Pond.CenterX - Pond.Width * .5f - 1.1f, Pond.CenterZ));
            return distance;
        }

        public float Height(float x, float z)
        {
            // Smooth zero height AND zero derivative on each cell edge prevents
            // cracked seams even where a forest meets a different biome.
            double ex = Math.Sin(Math.PI * (x + Half) / Cell);
            double ez = Math.Sin(Math.PI * (z + Half) / Cell);
            float envelope = (float)(ex * ex * ez * ez);
            float phase = (Seed % 127) * .049f;
            float hills = HillHeight * (.52f + .24f * (float)Math.Sin(x * .27f + phase) +
                .24f * (float)Math.Cos(z * .23f - phase));
            float clear = Smooth((PathDistance(x, z) - PathWidth * .5f) / 2.1f);
            float rest = Math.Min(DistanceToRect(Rest.Expanded(.7f), x, z), DistanceToRect(Feature.Expanded(.35f), x, z));
            float y = hills * envelope * clear * Smooth(rest / 1.5f);
            if (Pond.Width > 0)
            {
                float dx = x - Pond.CenterX, dz = z - Pond.CenterZ;
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                float radius = Pond.Width * .5f;
                if (d < radius + 1.2f)
                {
                    float bowl = Pond.Bottom * (1 - Smooth((d - radius * .57f) / (radius * .43f)));
                    y = d <= radius ? bowl : y * Smooth((d - radius) / 1.2f);
                }
            }
            return y;
        }

        public V3 Normal(float x, float z)
        {
            const float delta = .025f;
            return new V3(Height(x - delta, z) - Height(x + delta, z), 2 * delta,
                Height(x, z - delta) - Height(x, z + delta)).Unit;
        }

        public bool Clear(float x, float z, float radius)
        {
            if (Math.Abs(x) + radius > Half - .7f || Math.Abs(z) + radius > Half - .7f)
                return false;
            if (PathDistance(x, z) < PathWidth * .5f + radius + PlayerClearance)
                return false;
            if (DistanceToRect(Rest.Expanded(.85f), x, z) < radius || DistanceToRect(Feature.Expanded(.65f), x, z) < radius)
                return false;
            if (Pond.Width > 0)
            {
                float dx = x - Pond.CenterX, dz = z - Pond.CenterZ;
                if (dx * dx + dz * dz < Square(Pond.Width * .5f + radius + .8f))
                    return false;
            }
            return true;
        }

        Forest39Tree[] PlaceTrees(int worldSeed)
        {
            var trees = new List<Forest39Tree>(MaximumTrees);
            // Spacing excludes both trunk overlap and a congested lower canopy.
            // This work bound is independent from coordinate size and abundance.
            for (int candidate = 0; candidate < MaximumCandidates && trees.Count < MaximumTrees; candidate++)
            {
                uint h = LiminalRegions.Hash(worldSeed, X, Z, 0x390100u + (uint)candidate);
                float x = -Half + 1.7f + Unit(h) * (Cell - 3.4f);
                float z = -Half + 1.7f + Unit(Mix(h)) * (Cell - 3.4f);
                float radius = .24f + Unit(Mix(h + 19)) * .25f;
                if (!Clear(x, z, radius + .45f)) continue;
                bool blocked = false;
                foreach (var prior in trees)
                    if (Square(x - prior.Foot.X) + Square(z - prior.Foot.Z) < 11.56f)
                        blocked = true;
                if (blocked) continue;
                float height = 8.5f + Unit(Mix(h + 41)) * 5.5f;
                float crown = Math.Min(3.8f, 2.5f + height * .075f);
                for(int side=0;side<4;side++)if(!OpenEdges[side])
                {
                    float distance=Half-(Directions.DX[side]*x+Directions.DZ[side]*z);
                    crown=Math.Min(crown,(distance-2.1f)/1.6f);
                }
                if(crown<1.35f)continue;
                trees.Add(new Forest39Tree(new V3(x, Height(x, z) - .025f, z),
                    height, radius, crown, Unit(Mix(h + 67)) * 360, h));
            }
            return trees.ToArray();
        }

        Forest39Stone[] PlaceStones(int worldSeed)
        {
            var stones = new List<Forest39Stone>();
            for (int trial = 0; trial < 48 && stones.Count < MaximumStones; trial++)
            {
                uint h = LiminalRegions.Hash(worldSeed, X, Z, 0x390300u + (uint)trial);
                float x = (Unit(h) * 2 - 1) * (Half - 1.8f);
                float z = (Unit(Mix(h)) * 2 - 1) * (Half - 1.8f);
                float width = .4f + Unit(Mix(h + 2)) * 1.4f;
                if (!Clear(x, z, width * .6f)) continue;
                bool blocked = false;
                foreach (var t in Trees)
                    if (Square(t.Foot.X - x) + Square(t.Foot.Z - z) < Square(width + t.Radius + .3f))
                        blocked = true;
                foreach (var s in stones)
                    if (Square(s.Foot.X - x) + Square(s.Foot.Z - z) < Square(width + s.Width))
                        blocked = true;
                if (blocked) continue;
                stones.Add(new Forest39Stone(new V3(x, Height(x, z) - .08f, z),
                    width, width * (.38f + Unit(h + 1) * .3f), width * .8f, h));
            }
            return stones.ToArray();
        }

        public bool Valid()
        {
            if (Trails.Length != 4 || Trees.Length > MaximumTrees || Stones.Length > MaximumStones)
                return false;
            if (Math.Abs(Height(0, 0)) > .0001f || Pond.Width > 5 || Pond.Bottom < -.5f)
                return false;
            foreach (var t in Trees)
                if (!Clear(t.Foot.X, t.Foot.Z, t.Radius + .45f) || t.Height < 8 || t.Height > 15)
                    return false;
            return true;
        }

        public static float Smooth(float value)
        {
            float t = Math.Max(0, Math.Min(1, value));
            return t * t * (3 - 2 * t);
        }

        public static float DistanceToRect(PlanRect rect, float x, float z)
        {
            float dx = Math.Max(Math.Max(rect.MinX - x, 0), x - rect.MaxX);
            float dz = Math.Max(Math.Max(rect.MinZ - z, 0), z - rect.MaxZ);
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public static float Unit(uint value) { return (value >> 8) * (1f / 16777216); }
        static float Square(float value) { return value * value; }
        static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                return value ^ (value >> 16);
            }
        }
    }

    public static class Forest39Planner
    {
        public static bool Available(LayoutConfig config, int x, int z)
        {
            if (!config.Continuous && (x < 0 || z < 0 || x >= config.Columns || z >= config.Rows))
                return false;
            ConnectedMallSite mall;
            ConnectedTerrainSite terrain;
            return LiminalRegions.At(config, x, z) == LiminalEnvironment.EnchantedForest39 &&
                !ConnectedTerrainTopology.Reserved(config, x, z) &&
                !ConnectedTerrainTopology.Gateway(config, x, z, out terrain) &&
                !ConnectedMallTopology.At(config, x, z, out mall) &&
                !ConnectedMallTopology.Gateway(config, x, z, out mall);
        }

        public static bool Same(LayoutConfig config, int x, int z, int side)
        {
            long xx = (long)x + Directions.DX[side], zz = (long)z + Directions.DZ[side];
            return Available(config, x, z) && xx >= int.MinValue && xx <= int.MaxValue &&
                zz >= int.MinValue && zz <= int.MaxValue && Available(config, (int)xx, (int)zz);
        }

        public static void Describe(Room room, LayoutConfig config)
        {
            var forest = new Forest39Plan(config, room.X, room.Z);
            room.ZoneId = -1;
            room.Width = room.Depth = config.CellSize;
            room.Height = 22;
            room.Plan = new ProceduralPlan(room.Environment, Array.Empty<PlannedModule>(),
                forest.Pond.Width > 0 ? new[] { forest.Pond } : Array.Empty<WaterRegion>(),
                1.2f, 0, 0, forest39: forest);
            if (!forest.Valid()) throw new InvalidOperationException("Invalid Level 39 forest plan.");
        }
    }
}
