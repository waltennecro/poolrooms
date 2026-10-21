using System;

namespace Poolrooms.Core
{
    // Fuel is deterministic, unlike a stopwatch: two CPUs produce the same plan.
    // The base architecture is never dropped when optional furnishing runs out.
    public sealed class SceneryWorkBudget
    {
        public const int MaximumTriangles = 90000;
        public const int MaximumQueries = 900;
        public const int MaximumTriangleVisits = 180000;
        public const int MaximumAnchors = 40;
        public const int MaximumRecipesPerAnchor = 8;
        public const int MaximumObjects = 16;
        public const int MaximumClusters = 2;
        public const int MaximumReservations = 4096;
        public const int MaximumGridLinks = 160000;
        private int triangles;
        private int queries;
        private int visits;
        private int links;
        public int TriangleCount { get { return triangles; } }
        public int QueryCount { get { return queries; } }
        public int VisitCount { get { return visits; } }
        public int GridLinkCount { get { return links; } }
        public bool Exhausted { get; private set; }

        public bool Triangle()
        {
            if (triangles >= MaximumTriangles)
            {
                Exhausted = true;
                return false;
            }
            triangles++;
            return true;
        }

        public bool Query()
        {
            if (Exhausted || queries >= MaximumQueries)
            {
                Exhausted = true;
                return false;
            }
            queries++;
            return true;
        }

        public bool Visit()
        {
            if (Exhausted || visits >= MaximumTriangleVisits)
            {
                Exhausted = true;
                return false;
            }
            visits++;
            return true;
        }

        public bool Link()
        {
            if (links >= MaximumGridLinks)
            {
                Exhausted = true;
                return false;
            }
            links++;
            return true;
        }

        public static int ObjectLimit(int abundance)
        {
            return Math.Min(MaximumObjects, 8 + Math.Max(0, Math.Min(3, abundance)) * 2);
        }

        public static bool Eligible(Room room, LayoutConfig config)
        {
            if (room == null || !ScenerySafety.ValidConfiguration(config)) return false;
            if (!config.EncounterExploration || room.MallGateway || room.TerrainGateway) return false;
            if (!ScenerySafety.Finite(room.Width) || !ScenerySafety.Finite(room.Depth)) return false;
            if (!ScenerySafety.Finite(room.Height) || !ScenerySafety.Finite(room.Floor)) return false;
            if (room.Width < 7 || room.Depth < 7 || room.Width > 2048 || room.Depth > 2048) return false;
            return room.Height - room.Floor > 2.2f;
        }

        public static int ClusterLimit(Room room)
        {
            if (room == null) return 0;
            return room.Width * room.Depth >= 550 ? MaximumClusters : 1;
        }

        public static bool Continue(SceneryPlan plan, SceneryWorkBudget budget, int limit)
        {
            if (plan == null || budget == null || budget.Exhausted) return false;
            if (plan.Placements.Count >= limit) return false;
            return plan.Clusters < MaximumClusters;
        }
    }
}
