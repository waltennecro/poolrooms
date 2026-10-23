using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Poolrooms.Core;

static partial class Checks
{
    static void Forest39()
    {
        long before = assertions;
        int sectors = 0, trees = 0, entrances = 0;
        var clearings = new HashSet<Forest39Clearing>();
        var geometry = new HashSet<string>();
        var sourceModels = new HashSet<int>();
        int[] triangles = new int[3];
        foreach (int size in new[] { 28, 36, 60 })
        {
            foreach (int seed in new[] { 0, 39039, -182 })
            {
                var config = new LayoutConfig
                {
                    CellSize = size, Continuous = true, Seed = seed,
                    Procedural = LiminalGeneration.EnchantedForest39,
                    EncounterExploration = true, CurveSegments = 12
                };
                var world = new Layout(config);
                for (int z = -2; z <= 2; z++)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        var room = world.At(x, z);
                        var plan = room.Plan?.Forest39;
                        Check(plan != null, "Forest was sent to a generic room builder");
                        Check(room.Plan.Valid(room, config), "Invalid forest plan");
                        Check(room.Width == size && room.Depth == size, "Forest no longer shares its full cell");
                        var built = Architecture.Build(room, world);
                        sectors++;
                        trees += plan.Trees.Length;
                        clearings.Add(plan.Clearing);
                        geometry.Add(Hash(built.Structure, built.Decoration, built.CollisionOnly));
                        foreach (var mesh in new[] { built.Structure, built.Decoration, built.Water, built.CollisionOnly }.Concat(built.VisualMeshes))
                            Validate(mesh);
                        Check(built.Glass.TriangleCount == 0 && built.Fixtures.Count == 0,
                            "Outdoor forest acquired an indoor ceiling or artificial lights");
                        Check(built.Structure.TriangleCount < 45000 && built.CollisionOnly.TriangleCount < 5000,
                            "Unbounded forest collider complexity");
                        Check(built.Forest39Canopy != null, "Missing forest canopy");
                        for (int lod = 0; lod < 3; lod++)
                        {
                            Validate(built.Forest39Canopy.Wood[lod]);
                            Validate(built.Forest39Canopy.Leaves[lod]);
                            int count = built.Forest39Canopy.Wood[lod].TriangleCount + built.Forest39Canopy.Leaves[lod].TriangleCount;
                            triangles[lod] = Math.Max(triangles[lod], count);
                            if (lod > 0)
                                Check(count < built.Forest39Canopy.Wood[lod - 1].TriangleCount +
                                    built.Forest39Canopy.Leaves[lod - 1].TriangleCount, "Oak LOD does not reduce geometry");
                        }
                        Check(built.Forest39Canopy.Wood[0].TriangleCount + built.Forest39Canopy.Leaves[0].TriangleCount < 150000,
                            "Forest crown exceeds the sector budget");
                        Check(built.Life != null && built.Life.Scenery!=null && SceneryPlanner.ValidPlan(built.Life.Scenery,room,config) && built.Life.Stations.Count == 1, "Missing supported forest activity");
                        foreach (var station in built.Life.Stations)
                        {
                            var anchor = station.Anchor;
                            Check(ArchitecturalDressing.FootprintFits(anchor.Center,anchor.Normal,anchor.A,anchor.B,anchor.C),
                                "Forest station is larger than its physical support panel");
                            Check(anchor.Center.Y>=.95f&&anchor.Center.Y<=1.75f,"Forest station is out of reach");
                            Check((anchor.Center-built.Life.Notice.Value.Center).Length>=1.05f,"Forest notice overlaps an activity");
                        }
                        Check(built.SourceProps.Count == 3, "Forest furnishings missing");
                        foreach (var source in built.SourceProps)
                        {
                            sourceModels.Add(source.Source);
                            float support = Support(built.Structure, source.Bottom);
                            if (source.Source == 11)
                                support = SourceFurniture.Dimensions(7).Y + .004f;
                            Check(Math.Abs(support - source.Bottom.Y) < .009f,
                                "Unsupported forest furniture: " + SourceFurniture.Id(source.Source));
                            Check(SourceFurniture.Clear(built.Structure, source, .002f), "Forest furniture intersects structure");
                        }
                        foreach (var trail in plan.Loop==null?plan.Trails:plan.Trails.Concat(new[]{plan.Loop}))
                        {
                            for (int i = 0; i <= 40; i++)
                            {
                                var p = trail.At(i / 40f);
                                // Mathematical sampler reads actual triangles for
                                // floor support, rather than assuming height=0.
                                Check(Math.Abs(Forest39Terrain.MeshHeight(plan, p.X, p.Y)) < .06f,
                                    "Forest path rises into its terrain or water");
                                Check(Forest39Terrain.Walkable(plan, p.X, p.Y), "A winding path is obstructed");
                                var sample = new V3(p.X, 1, p.Y);
                                Check(SphereClear(built.CollisionOnly, sample, .31f), "Forest path has a physical obstacle");
                            }
                        }
                        for (int side = 0; side < 4; side++)
                        {
                            var neighbor = world.Neighbour(room, side);
                            Check(room.HasExit(side) && neighbor.HasExit(Directions.Opposite(side)), "Forest edge is not reciprocal");
                            var edge = new V3(Directions.DX[side] * (plan.Half - .2f), .95f,
                                Directions.DZ[side] * (plan.Half - .2f));
                            Check(SphereClear(built.Structure, edge, .31f) && SphereClear(built.CollisionOnly, edge, .31f),
                                "Forest threshold has insufficient headroom");
                            var next = neighbor.Plan.Forest39;
                            for (int sample = 0; sample <= 20; sample++)
                            {
                                float offset = -plan.Half + plan.Cell * sample / 20;
                                float ax = side % 2 == 0 ? offset : Directions.DX[side] * plan.Half;
                                float az = side % 2 == 0 ? Directions.DZ[side] * plan.Half : offset;
                                float bx = ax - Directions.DX[side] * plan.Cell;
                                float bz = az - Directions.DZ[side] * plan.Cell;
                                Check(Math.Abs(plan.Height(ax, az) - next.Height(bx, bz)) < .0001f, "Cracked forest terrain seam");
                            }
                        }
                        if (plan.Pond.Width > 0)
                        {
                            Check(built.Water.TriangleCount > 24 && built.Regions.Count == 1, "Pond water missing");
                            Check(Forest39Water.SurfaceArea(built.Water)>5&&Forest39Water.SurfaceArea(built.Water)<20,"Water clipped to an implausible shore area");
                            foreach (var p in built.Water.Positions)
                                Check(Forest39Terrain.MeshHeight(plan, p.X, p.Z) <= plan.Pond.Level + .0002f,
                                    "Water polygon protrudes over dry ground");
                            Check(plan.Pond.Width <= 5 && plan.Pond.Level - plan.Pond.Bottom <= .5f,
                                "Pond no longer matches the shallow small Wiki description");
                        }
                        foreach (var action in Forest39Activities.Plan(plan))
                        {
                            var mesh = Forest39ActivityGeometry.Body(action.Kind);
                            Validate(mesh);
                            Check(mesh.TriangleCount <= 1000, "Unbounded optional activity");
                        }
                    }
                }
            }
        }
        Check(geometry.Count > 60 && clearings.Count == 12, "Forest lost its distinct clearing distribution");
        var mixed = new LayoutConfig { CellSize = 36 };
        WalkableExploration.Configure(mixed);
        Check(mixed.EnableForest39 && !new LayoutConfig().EnableForest39, "Legacy opt-in or unified generation changed");
        Check((int)LiminalEnvironment.EnchantedForest39 == 63, "Forest must append after the original 63 biomes");
        var available = new HashSet<LiminalEnvironment>();
        for (int z = 0; z < 8; z++)
            for (int x = 0; x < 8; x++)
                if (WalkableExploration.Region(mixed, x + 8, z + 8, out var environment)) available.Add(environment);
        Check(available.Count == 64 && available.Contains(LiminalEnvironment.EnchantedForest39),
            "A new biome consumed an existing destination's discovery slot");
        var connected = new Layout(mixed);
        Check(LiminalRegions.Find(mixed, LiminalEnvironment.EnchantedForest39, 0, 0, out int fx, out int fz),
            "Forest requires master mode");
        for (int z = fz - 4; z <= fz + 4; z++)
        {
            for (int x = fx - 4; x <= fx + 4; x++)
            {
                var room = connected.At(x, z);
                if (room.Plan?.Forest39 == null) continue;
                for (int side = 0; side < 4; side++)
                {
                    if (!room.HasExit(side)) continue;
                    var other = connected.Neighbour(room, side);
                    Check(other.HasExit(Directions.Opposite(side)), "Mixed forest entrance is one way");
                    if (other.Plan?.Forest39 != null) continue;
                    var corridor = side < 2 ? Corridors.Build(room, other, side, mixed) :
                        Corridors.Build(other, room, side - 2, mixed);
                    Validate(corridor.Structure);
                    entrances++;
                }
            }
        }
        Check(entrances > 0, "Forest exists only in a dedicated test scene");
        Forest39SupplyFootprints();
        Forest39Games();
        Forest39Coordinates();
        Forest39Regressions();
        string path = Environment.GetEnvironmentVariable("POOLROOMS_FOREST39_REPORT");
        var report = new
        {
            sectors, trees, clearingProgrammes = clearings.Count, distinctMeshes = geometry.Count,
            existingExternalModels = sourceModels.Count, newExternalModels = 0, peakCanopyTriangles = triangles,
            walkingEntrances = entrances, mathematicalAssertions = assertions - before,
            nativeUnityExecuted = false, nativePhysicsExecuted = false, nativeRenderingExecuted = false
        };
        if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Level39: " + JsonSerializer.Serialize(report));
    }

    static void Forest39SupplyFootprints()
    {
        var plan=new Forest39Plan(new LayoutConfig{CellSize=36},0,0);
        var positions=new List<V3>();var sizes=new List<V3>();
        for(int index=0;index<Forest39Supplies.Count;index++)
        {
            positions.Add(Forest39Supplies.Bottom(plan,index));
            sizes.Add(ProvisionCatalog.Get(Forest39Supplies.Food(index)).Size);
        }
        positions.Add(Forest39Supplies.Cup(plan));sizes.Add(SourceFurniture.Dimensions(11));
        for(int i=0;i<positions.Count;i++)
        {
            var p=positions[i];var size=sizes[i];float x=p.X-plan.Rest.X,z=p.Z-plan.Rest.Z-.5f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                Check((float)Math.Sqrt((x+sx*size.X*.5f)*(x+sx*size.X*.5f)+(z+sz*size.Z*.5f)*(z+sz*size.Z*.5f))<.448f,
                    "Forest food overhangs the circular cafe tabletop");
            for(int j=0;j<i;j++)Check(Math.Abs(p.X-positions[j].X)>(size.X+sizes[j].X)*.5f+.002f||
                Math.Abs(p.Z-positions[j].Z)>(size.Z+sizes[j].Z)*.5f+.002f,"Forest food/cup footprints overlap");
        }
    }

    static void Forest39Games()
    {
        foreach (Forest39Activity kind in Enum.GetValues(typeof(Forest39Activity)))
        {
            for (uint seed = 0; seed < 48; seed++)
            {
                var state = new Forest39ActivityState(kind, seed);
                int turns = 0;
                while (!state.Complete && turns++ <= state.Limit) state.Advance();
                Check(state.Complete && state.Position == state.Target && turns <= state.Limit,
                    "Forest study cannot be completed");
                Check(!state.Advance(), "Completed forest study advances unexpectedly");
                Check(state.Claim() && !state.Claim(), "Forest reward can be claimed twice");
                var restored = new Forest39ActivityState(kind, seed);
                restored.Restore(state.Position, true, true);
                Check(restored.Complete && restored.Claimed, "Forest study loses session state");
            }
        }
        var inventory = new ExplorationInventory();
        foreach (int id in new[] { 0, 1, 2 })
        {
            string key = "forest39:" + id;
            Check(inventory.Add(key, System.Numerics.BigInteger.Pow(10, 30)), "Forest item does not support unbounded stacks");
            Check(ExplorationItems.Resolve(key).Key == key, "Forest item key is not stable");
        }
        Check(ExplorationItems.Resolve("forest39:-1") == null && ExplorationItems.Resolve("forest39:3") == null,
            "Unknown forest item accepted");
    }

    static void Forest39Coordinates()
    {
        foreach (int coordinate in new[] { int.MinValue, -1000000, -1, 0, 1, 1000000, int.MaxValue })
        {
            var config = new LayoutConfig { CellSize = 36 };
            var first = new Forest39Plan(config, coordinate, coordinate);
            var second = new Forest39Plan(config, coordinate, coordinate);
            Check(first.Seed == second.Seed && first.Trees.Length == second.Trees.Length && first.Valid(),
                "Forest depends on load order or overflows at large coordinates");
            foreach (var trail in first.Trails)
            {
                var a = trail.At(0);
                var b = trail.At(1);
                Check(a.X == 0 && a.Y == 0, "Forest origin is not connected");
                Check(Math.Abs(Math.Abs(b.X) + Math.Abs(b.Y) - 18) < .001f, "Forest edge endpoint changed");
            }
        }
    }
}
