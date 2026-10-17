using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // A connected spanning tree is built first; stylistic loops never remove its links.
    public static class Topology
    {
        private struct Edge
        {
            public Room A, B;
            public int Direction;
            public float Cost;
            public bool CrossesDistrict;
        }

        public static LayoutStyle Resolve(LayoutConfig c)
        {
            return c.Distribution == LayoutStyle.BySeed
                ? (LayoutStyle)(StableRandom.Hash(c.Seed, 17, 43, 98731u) % 4)
                : c.Distribution;
        }

        public static void Generate(Layout layout)
        {
            LayoutConfig c = layout.Config;
            LayoutStyle mode = layout.Distribution;
            var random = new StableRandom(StableRandom.Hash(c.Seed, -3, 19, 0xa341316cu));
            var edges = new List<Edge>();
            int preferredAxis = (int)(StableRandom.Hash(c.Seed, 11, 9, 71u) & 1);
            foreach (Room r in layout.Rooms)
            {
                r.Exits = 0;
                for (int d = 0; d < 2; d++)
                {
                    Room n = layout.Neighbour(r, d);
                    if (n == null) continue;
                    bool boundary = r.X / c.DistrictSize != n.X / c.DistrictSize || r.Z / c.DistrictSize != n.Z / c.DistrictSize;
                    float cost = random.Value();
                    if (mode == LayoutStyle.Galleries && d != preferredAxis) cost += 2;
                    if (mode == LayoutStyle.Districts && boundary) cost += 4;
                    edges.Add(new Edge { A = r, B = n, Direction = d, Cost = cost, CrossesDistrict = boundary });
                }
            }

            if (mode == LayoutStyle.Maze) DepthFirst(layout, ref random);
            else
            {
                // Stable tie-breaks prevent platform sort order from changing the layout.
                edges.Sort((a, b) =>
                {
                    int order = a.Cost.CompareTo(b.Cost);
                    if (order == 0) order = a.A.Id.CompareTo(b.A.Id);
                    return order == 0 ? a.Direction.CompareTo(b.Direction) : order;
                });
                var parent = new int[layout.Rooms.Length];
                for (int i = 0; i < parent.Length; i++) parent[i] = i;
                foreach (Edge e in edges)
                {
                    int a = Find(parent, e.A.Id), b = Find(parent, e.B.Id);
                    if (a == b) continue;
                    parent[a] = b; Open(e.A, e.B, e.Direction);
                }
            }

            foreach (Edge e in edges)
            {
                if (e.A.HasExit(e.Direction)) continue;
                float p = c.LoopChance;
                if (mode == LayoutStyle.Maze || mode == LayoutStyle.Galleries) p *= p;
                else if (mode == LayoutStyle.Districts && e.CrossesDistrict) p = p * p * p;
                else p = 1 - (1 - p) * (1 - p);
                if (random.Value() < p) Open(e.A, e.B, e.Direction);
            }
        }

        private static void DepthFirst(Layout layout, ref StableRandom random)
        {
            var stack = new Stack<Room>();
            var seen = new bool[layout.Rooms.Length];
            var candidates = new int[4];
            seen[0] = true; stack.Push(layout.Rooms[0]);
            while (stack.Count > 0)
            {
                Room r = stack.Peek(); int count = 0;
                for (int d = 0; d < 4; d++)
                {
                    Room n = layout.Neighbour(r, d);
                    if (n != null && !seen[n.Id]) candidates[count++] = d;
                }
                if (count == 0) { stack.Pop(); continue; }
                int chosen = candidates[random.Range(count)]; Room next = layout.Neighbour(r, chosen);
                Open(r, next, chosen); seen[next.Id] = true; stack.Push(next);
            }
        }

        private static int Find(int[] parent, int id)
        {
            while (parent[id] != id) { parent[id] = parent[parent[id]]; id = parent[id]; }
            return id;
        }

        private static void Open(Room a, Room b, int direction)
        {
            a.Exits |= 1 << direction; b.Exits |= 1 << Directions.Opposite(direction);
        }
    }
}
