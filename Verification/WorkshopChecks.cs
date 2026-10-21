using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static int[] WorkshopSnapshot(WorkshopBoard board)
    {
        return Enumerable.Range(0, board.CellCount).Select(board.Value).ToArray();
    }

    static void WorkshopSolve(WorkshopBoard board)
    {
        int attempts = 0;
        while (!board.Complete && attempts++ < 256)
        {
            Check(board.Hint(out var move), "Incomplete workshop has no constructive hint");
            Check(board.Apply(move), "Workshop hint proposes an illegal move");
        }
        Check(board.Complete, "Workshop solver did not terminate");
        Check(!board.Hint(out _), "Completed workshop still proposes a solution step");
    }

    static void WorkshopWorld()
    {
        int games = 0, models = 0;
        var distinctModels = new HashSet<string>();
        for (int id = 0; id < WorkshopCatalog.Count; id++)
        {
            var kind = (StationKind)(WorkshopCatalog.First + id);
            Check(WorkshopCatalog.IsWorkshop(kind), "Appended workshop ID missing");
            Check((int)StationKind.RestBell == 11 && (int)StationKind.LanternCircuit == 12, "Legacy station IDs changed");
            Check(!string.IsNullOrWhiteSpace(WorkshopCatalog.Name(kind)) &&
                  !string.IsNullOrWhiteSpace(WorkshopCatalog.Story(kind)) &&
                  !string.IsNullOrWhiteSpace(WorkshopCatalog.Instructions(kind)), "Workshop content incomplete");
            for (uint seed = 0; seed < 256; seed++)
            {
                var board = WorkshopCatalog.Create(kind, seed);
                var same = WorkshopCatalog.Create(kind, seed);
                var initial = WorkshopSnapshot(board);
                Check(!board.Complete, "Workshop starts already completed");
                Check(initial.SequenceEqual(WorkshopSnapshot(same)), "Workshop generation not deterministic");
                for (int cell = 0; cell < board.CellCount; cell++) Check(board.Target(cell) == same.Target(cell), "Workshop target changes with identical seed");
                int version = board.Revision;
                Check(!board.Apply(new WorkshopMove(-1)) && !board.Apply(new WorkshopMove(board.ActionCount)) &&
                      !board.Apply(new WorkshopMove(int.MaxValue)) && !board.Apply(new WorkshopMove(0, 0)) &&
                      !board.Apply(new WorkshopMove(0, 2)), "Invalid input mutated a workshop");
                Check(board.Revision == version && board.Moves == 0 && !board.CanUndo, "Rejected input changes bookkeeping");
                Check(board.Hint(out var first) && board.Apply(first), "First workshop step unavailable");
                Check(board.CanUndo && board.Undo(), "Workshop undo unavailable");
                Check(initial.SequenceEqual(WorkshopSnapshot(board)) && board.Moves == 0, "Workshop undo loses state");
                WorkshopSolve(board);
                board.Restart();
                Check(initial.SequenceEqual(WorkshopSnapshot(board)) && !board.CanUndo && board.Moves == 0,
                    "Workshop restart does not restore its exact initial layout");
                // Challenge solvability after player input, not only after
                // following the advertised solution from the initial state.
                var random = new StableRandom(seed ^ 0xFEEDu);
                int? conserved = board is TideBoard tides ? tides.Total : (int?)null;
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    board.Apply(new WorkshopMove(random.Range(board.ActionCount), random.Range(2) == 0 ? -1 : 1));
                    Check(board.HistoryCount <= 32, "Workshop undo grows without bound");
                    if (conserved.HasValue) Check(((TideBoard)board).Total == conserved.Value, "Tide workshop creates or loses water");
                    if (board is NurseryBoard) for (int cell = 0; cell < board.CellCount; cell++)
                        Check(board.Value(cell) >= 0 && board.Value(cell) <= NurseryBoard.Maximum, "Seed moisture overflow");
                }
                WorkshopSolve(board);
                games++;
            }
            for (int variant = 0; variant < 3; variant++)
            {
                var body = WorkshopModels.Build(kind, variant);
                Validate(body);
                distinctModels.Add(Hash(body));
                Check(body.TriangleCount > 20 && body.TriangleCount < 2000, "Workshop model exceeds station budget: " + kind + " " + body.TriangleCount);
                var board = WorkshopCatalog.Create(kind, (uint)variant);
                for (int state = 0; state < 2; state++)
                {
                    var overlay = WorkshopStateGeometry.Build(kind, board, variant);
                    Validate(overlay);
                    Check(overlay.TriangleCount > 0 && overlay.TriangleCount < 2000, "Dynamic workshop mesh budget");
                    foreach (var mesh in new[] { body, overlay }) foreach (var point in mesh.Positions)
                        Check(Math.Abs(point.X) <= ArchitecturalDressing.HalfWidth &&
                              Math.Abs(point.Y) <= ArchitecturalDressing.HalfHeight &&
                              point.Z >= -.001f && point.Z <= .19f, "Workshop escapes its existing wall footprint");
                    WorkshopSolve(board);
                    models++;
                }
            }
        }
        Check(distinctModels.Count == WorkshopCatalog.Count * 3, "Workshop mounting variants or models collapsed");
        WorkshopCacheChecks();
        int environments = 0;
        foreach (LiminalEnvironment environment in Enum.GetValues(typeof(LiminalEnvironment)))
        {
            var selected = new HashSet<StationKind>();
            for (uint hash = 0; hash < 4096; hash++) selected.Add(ImmersionStations.Choose(environment, hash));
            for (int id = 0; id < WorkshopCatalog.Count; id++)
                Check(selected.Contains((StationKind)(WorkshopCatalog.First + id)), "Workshop excluded from a biome");
            Check(selected.Any(kind => !WorkshopCatalog.IsWorkshop(kind)), "Old stations removed by new workshop selection");
            environments++;
        }
        string root = Directory.GetCurrentDirectory();
        string inventory = File.ReadAllText(Path.Combine(root, "Assets/PoolroomsProcedural/Runtime/PoolroomsInventory.Workshops.cs"));
        string display = File.ReadAllText(Path.Combine(root, "Assets/PoolroomsProcedural/Runtime/PoolroomsWorkshopDisplay.cs"));
        string adapter = File.ReadAllText(Path.Combine(root, "Assets/PoolroomsProcedural/Runtime/PoolroomsStation.cs"));
        Check(inventory.Contains("WorkshopCatalog.Create") == false && inventory.Contains("workshops.Get"), "UI bypasses bounded session cache");
        Check(inventory.Contains("new ScrollView") && inventory.Contains("workshopPlayback") && inventory.Contains("board.Revision != revision"), "Workshop UI lacks scrolling or stale-audio cancellation");
        Check(adapter.Contains("PoolroomsWorkshopDisplay.Show") && adapter.Contains("WorkshopSessions.Seed"), "Unvisited station displays not connected");
        Check(display.Contains("mesh.Clear()") && display.Contains("if (Application.isPlaying) Destroy(mesh)") &&
              display.Contains("WeakReference<WorkshopBoard>") && display.Contains("RayTracingMode.DynamicGeometry") && !display.Contains("Update()") &&
              !display.Contains("new Material("), "Dynamic display retains evicted boards, leaks or updates every frame");
        Console.WriteLine($"Workshops: {games} seeded games solved before/after arbitrary input; {models} state meshes, 36 distinct body/mount combinations; {environments} biome banks include all twelve games; bounded LRU/undo verified. Native Unity UI/audio/rendering unrun.");
    }

    static void WorkshopCacheChecks()
    {
        var cache = new WorkshopSessions(3);
        var first = cache.Get("a", 1, StationKind.Weaving);
        first.Apply(new WorkshopMove(0));
        cache.Get("b", 1, StationKind.Weaving);
        cache.Get("c", 1, StationKind.Weaving);
        Check(cache.TryPeek("a", 1, StationKind.Weaving, out var peek) && ReferenceEquals(first, peek), "Workshop peek loses identity");
        Check(ReferenceEquals(first, cache.Get("a", 1, StationKind.Weaving)), "Revisiting resets workshop state");
        cache.Get("d", 1, StationKind.Weaving);
        Check(cache.Count == 3 && cache.Evictions == 1 && !cache.TryPeek("b", 1, StationKind.Weaving, out _), "Workshop LRU evicts wrong entry");
        Check(!ReferenceEquals(first, cache.Get("a", 2, StationKind.Weaving)), "World seeds share mutable workshop state");
        cache.Get("a", 2, StationKind.WindRose);
        Check(cache.Count == 3, "Station kinds alias in workshop cache");
        cache.ForgetWorld(2);
        Check(cache.Count == 1, "World-specific workshop cleanup removes wrong entries");
        cache.Clear();
        Check(cache.Count == 0 && !cache.TryPeek("absent", 1, StationKind.Weaving, out _), "Empty workshop cache contains state");
        for (int i = 0; i < 10000; i++) cache.Get("station-" + i, i % 3, StationKind.LanternCircuit);
        Check(cache.Count == 3, "Infinite exploration leaks workshop boards");
        var board = new WeavingBoard(1);
        for (int i = 0; i < 1000; i++) board.Apply(new WorkshopMove(i % 16));
        Check(board.HistoryCount == 32, "Undo cache did not cap snapshots");
        for (int i = 0; i < 32; i++) Check(board.Undo(), "Bounded undo loses available history");
        Check(!board.Undo(), "Undo crosses its bounded history");
    }
}
