using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct WorkshopMove
    {
        public readonly int Cell;
        public readonly int Direction;

        public WorkshopMove(int cell, int direction = 1)
        {
            Cell = cell;
            Direction = direction;
        }
    }

    public abstract class WorkshopBoard
    {
        private const int HistoryLimit = 32;
        protected readonly int[] values;
        protected readonly int[] target;
        protected StableRandom random;
        private int[] initial;
        private readonly List<int[]> history = new List<int[]>();
        public int Moves { get; private set; }
        public int Revision { get; private set; }
        public int CellCount { get { return values.Length; } }
        public bool CanUndo { get { return history.Count > 0; } }
        public int HistoryCount { get { return history.Count; } }
        public abstract bool Complete { get; }
        public abstract int ActionCount { get; }
        public abstract string Status { get; }

        protected WorkshopBoard(int cells, uint seed)
        {
            if (cells < 1 || cells > 64) throw new ArgumentOutOfRangeException(nameof(cells));
            values = new int[cells];
            target = new int[cells];
            random = new StableRandom(seed);
        }

        public int Value(int index)
        {
            if (index < 0 || index >= CellCount) throw new ArgumentOutOfRangeException(nameof(index));
            return values[index];
        }

        public int Target(int index)
        {
            if (index < 0 || index >= CellCount) throw new ArgumentOutOfRangeException(nameof(index));
            return target[index];
        }

        protected void SealInitial()
        {
            initial = (int[])values.Clone();
        }

        protected static int Mod(int value, int modulus)
        {
            return (value % modulus + modulus) % modulus;
        }

        protected bool Matches()
        {
            for (int i = 0; i < CellCount; i++) if (values[i] != target[i]) return false;
            return true;
        }

        protected int MatchedCount()
        {
            int count = 0;
            for (int i = 0; i < CellCount; i++) if (values[i] == target[i]) count++;
            return count;
        }

        public bool Apply(WorkshopMove move)
        {
            if (move.Cell < 0 || move.Cell >= ActionCount) return false;
            if (move.Direction != -1 && move.Direction != 1) return false;
            if (!Allowed(move)) return false;
            if (history.Count == HistoryLimit) history.RemoveAt(0);
            history.Add((int[])values.Clone());
            Change(move);
            Moves++;
            Revision++;
            return true;
        }

        protected virtual bool Allowed(WorkshopMove move) { return true; }
        protected abstract void Change(WorkshopMove move);
        public abstract bool Hint(out WorkshopMove move);

        public bool Undo()
        {
            if (!CanUndo) return false;
            int last = history.Count - 1;
            Array.Copy(history[last], values, CellCount);
            history.RemoveAt(last);
            Moves = Math.Max(0, Moves - 1);
            Revision++;
            return true;
        }

        public void Restart()
        {
            if (initial == null) throw new InvalidOperationException("Board initial state was not sealed.");
            Array.Copy(initial, values, CellCount);
            history.Clear();
            Moves = 0;
            Revision++;
        }

    }

    // Each player owns a bounded LRU. Recreated chunks reconnect through their
    // stable station key; different world seeds never share a board accidentally.
    public sealed class WorkshopSessions
    {
        private sealed class Entry
        {
            public string Key;
            public int WorldSeed;
            public WorkshopBoard Board;
        }

        private readonly Dictionary<string, LinkedListNode<Entry>> entries;
        private readonly LinkedList<Entry> recent = new LinkedList<Entry>();
        public int Capacity { get; }
        public int Count { get { return entries.Count; } }
        public long Evictions { get; private set; }

        public WorkshopSessions(int capacity = 128)
        {
            if (capacity < 1 || capacity > 1024) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            entries = new Dictionary<string, LinkedListNode<Entry>>(StringComparer.Ordinal);
        }

        public static uint Seed(string key, int worldSeed, StationKind kind)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            uint hash = unchecked(2166136261u ^ (uint)worldSeed);
            foreach (char value in key) hash = unchecked((hash ^ value) * 16777619u);
            return unchecked((hash ^ (uint)kind) * 16777619u);
        }

        private static string Identity(string key, int seed, StationKind kind)
        {
            if (string.IsNullOrEmpty(key) || key.Length > 512)
                throw new ArgumentException("A bounded nonempty station key is required.", nameof(key));
            return seed.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + (int)kind + ":" + key;
        }

        public WorkshopBoard Get(string key, int worldSeed, StationKind kind)
        {
            if (!WorkshopCatalog.IsWorkshop(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            string identity = Identity(key, worldSeed, kind);
            if (entries.TryGetValue(identity, out var node))
            {
                recent.Remove(node);
                recent.AddLast(node);
                return node.Value.Board;
            }
            var entry = new Entry
            {
                Key = identity,
                WorldSeed = worldSeed,
                Board = WorkshopCatalog.Create(kind, Seed(key, worldSeed, kind))
            };
            if (entries.Count == Capacity)
            {
                var oldest = recent.First;
                entries.Remove(oldest.Value.Key);
                recent.RemoveFirst();
                Evictions++;
            }
            var added = recent.AddLast(entry);
            entries.Add(identity, added);
            return entry.Board;
        }

        public bool TryPeek(string key, int seed, StationKind kind, out WorkshopBoard board)
        {
            board = null;
            if (!WorkshopCatalog.IsWorkshop(kind)) return false;
            if (!entries.TryGetValue(Identity(key, seed, kind), out var node)) return false;
            board = node.Value.Board;
            return true;
        }

        public void ForgetWorld(int seed)
        {
            var node = recent.First;
            while (node != null)
            {
                var next = node.Next;
                if (node.Value.WorldSeed == seed)
                {
                    entries.Remove(node.Value.Key);
                    recent.Remove(node);
                }
                node = next;
            }
        }

        public void Clear()
        {
            entries.Clear();
            recent.Clear();
        }
    }
}
