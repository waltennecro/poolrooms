using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void Forest39Regressions()
    {
        Forest39DiscoverySlots();
        Forest39FiniteEdges();
        Forest39Ownership();
        Forest39MapProjection();
        Forest39InvalidInput();
    }

    static void Forest39DiscoverySlots()
    {
        var previous = new LayoutConfig();
        WalkableExploration.Configure(previous);
        previous.EnableForest39 = false;
        var expanded = new LayoutConfig();
        WalkableExploration.Configure(expanded);
        for (int bx = -2; bx <= 2; bx++)
        {
            for (int bz = -2; bz <= 2; bz++)
            {
                int forest = 0;
                var seen = new HashSet<LiminalEnvironment>();
                for (int z = 0; z < 8; z++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        Check(WalkableExploration.Region(previous, bx * 8 + x, bz * 8 + z, out var before),
                            "Previous coverage disappeared");
                        Check(WalkableExploration.Region(expanded, bx * 8 + x, bz * 8 + z, out var after),
                            "Expanded coverage disappeared");
                        if (after == LiminalEnvironment.EnchantedForest39) forest++;
                        else Check(after == before, "A preserved 63-biome discovery assignment moved");
                        seen.Add(after);
                    }
                }
                Check(forest == 1 && seen.Count == 64, "Forest did not consume exactly the one former duplicate slot");
            }
        }
        var disabled = new LayoutConfig { Procedural = LiminalGeneration.Expanded };
        Check(!LiminalRegions.Find(disabled, LiminalEnvironment.EnchantedForest39, 0, 0, out _, out _),
            "Disabled custom-profile forest leaked into generation");
        foreach (var mode in new[] { LiminalGeneration.Authored, LiminalGeneration.Poolrooms, LiminalGeneration.City11 })
        {
            var config = new LayoutConfig { EnableForest39 = true, Procedural = mode };
            for (int x = -3; x <= 3; x++)
                Check(LiminalRegions.At(config, x, x + 2) != LiminalEnvironment.EnchantedForest39,
                    "Opt-in flag replaced a dedicated existing level");
        }
    }

    static void Forest39FiniteEdges()
    {
        foreach (int count in new[] { 1, 2, 3 })
        {
            var config = new LayoutConfig
            {
                Continuous = false, Columns = count, Rows = count,
                CellSize = 28, Procedural = LiminalGeneration.EnchantedForest39
            };
            var world = new Layout(config);
            foreach (var room in world.Rooms)
            {
                var data = Architecture.Build(room, world);
                var plan = room.Plan.Forest39;
                for(int tier=0;tier<3;tier++)foreach(var canopy in new[]{data.Forest39Canopy.Wood[tier],data.Forest39Canopy.Leaves[tier]})
                    foreach(var vertex in canopy.Positions)for(int edge=0;edge<4;edge++)if(!plan.OpenEdges[edge])
                        Check(Directions.DX[edge]*vertex.X+Directions.DZ[edge]*vertex.Z<=plan.Half+.0001f,
                            "Forest canopy crosses a closed biome boundary at a visual LOD");
                for (int side = 0; side < 4; side++)
                {
                    int xx = room.X + Directions.DX[side];
                    int zz = room.Z + Directions.DZ[side];
                    bool outside = xx < 0 || zz < 0 || xx >= count || zz >= count;
                    Check(Forest39Planner.Same(config, room.X, room.Z, side) == !outside,
                        "Finite forest edge availability is inconsistent");
                    if (!outside) continue;
                    Check(!room.HasExit(side), "Finite forest path exits the world");
                    var at = new V3(Directions.DX[side] * (plan.Half - .1f), .9f,
                        Directions.DZ[side] * (plan.Half - .1f));
                    Check(!SphereClear(data.CollisionOnly, at, .3f),
                        "Closed finite boundary has no physical berm");
                }
            }
        }
    }

    static void Forest39Ownership()
    {
        var config = new LayoutConfig
        {
            Continuous = true, CellSize = 28,
            Procedural = LiminalGeneration.EnchantedForest39, EncounterExploration = true
        };
        var world = new Layout(config);
        var seen = new HashSet<Forest39Clearing>();
        for (int x = 0; x < 160 && seen.Count < 12; x++)
        {
            var room = world.At(x, 2);
            var plan = room.Plan.Forest39;
            if (!seen.Add(plan.Clearing)) continue;
            var data = Architecture.Build(room, world);
            string physicalBefore = Hash(data.Structure, data.CollisionOnly);
            var input = new[] { data.Structure, data.Decoration }.Concat(data.VisualMeshes).ToArray();
            var visual = RenderSurfaceOwnership.Resolve(input);
            Check(Hash(data.Structure, data.CollisionOnly) == physicalBefore,
                "Visual material ownership modified forest physics");
            Check(visual.Length == input.Length, "Forest finish upload changed material ownership ordering");
            for (int index = 0; index < visual.Length; index++)
            {
                Validate(visual[index]);
                Check(visual[index].Theme == input[index].Theme, "Forest lost its native material theme");
                Check(visual[index].TriangleCount <= input[index].TriangleCount * 8 + 512,
                    "Forest visual ownership exploded geometry");
            }
            var translucent = data.Forest39Canopy.Leaves[0];
            var preserved = RenderSurfaceOwnership.Resolve(new[] { translucent, translucent });
            Check(preserved[0].TriangleCount == translucent.TriangleCount &&
                preserved[1].TriangleCount == translucent.TriangleCount,
                "Opaque trim discarded alpha-cut canopy coverage");
        }
        Check(seen.Count == 12, "Ownership check did not exercise every clearing structure");
    }

    static void Forest39MapProjection()
    {
        var plan = new Forest39Plan(new LayoutConfig { CellSize = 36 }, -7, 9);
        foreach (var path in plan.Trails)
        {
            for (int index = 3; index < path.Points.Length; index++)
            {
                var point = path.Points[index];
                var nearest = Forest39RouteMap.Nearest(plan, point.X, point.Y);
                Check(nearest.DistanceFromPath < .0001f, "Physical route map projects off its own path");
                Check(nearest.MetresToExit >= 0 && nearest.MetresToExit <= path.Length,
                    "Physical route map reports invalid remaining distance");
            }
        }
        var config = new LayoutConfig { Continuous = true, Procedural = LiminalGeneration.EnchantedForest39 };
        var world = new Layout(config);
        var room = world.At(-7, 9);
        string text = Forest39RouteMap.Describe(world, room, 0, 0);
        foreach (int side in new[] { 0, 1, 2, 3 })
            Check(text.Contains(Forest39RouteMap.Direction(side)), "Notebook route map omits an actual open exit");
        Check(text.Contains("m desde el centro"), "Notebook path distance is not explained");
    }

    static void Forest39InvalidInput()
    {
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, 0, 23, 257 })
        {
            bool rejected = false;
            try { new Forest39Plan(new LayoutConfig { CellSize = invalid }, 0, 0); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Forest accepted nonfinite or unsupported sector dimensions");
        }
        foreach (Forest39Activity kind in Enum.GetValues(typeof(Forest39Activity)))
        {
            var state = new Forest39ActivityState(kind, 39);
            bool rejected = false;
            try { state.Restore(state.Limit, true, false); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Forest accepted an invalid persisted puzzle position");
        }
    }
}
