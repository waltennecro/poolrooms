using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public sealed class LanternBoard : WorkshopBoard
    {
        public override int ActionCount { get { return 9; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 9 faroles encendidos"; } }

        public LanternBoard(uint seed) : base(9, seed)
        {
            for (int i = 0; i < CellCount; i++) values[i] = target[i] = 1;
            for (int i = 0; i < 12; i++) Toggle(random.Range(9));
            if (Complete) Toggle(4);
            SealInitial();
        }

        private static int Neighbours(int cell)
        {
            int mask = 1 << cell;
            int x = cell % 3, y = cell / 3;
            if (x > 0) mask |= 1 << (cell - 1);
            if (x < 2) mask |= 1 << (cell + 1);
            if (y > 0) mask |= 1 << (cell - 3);
            if (y < 2) mask |= 1 << (cell + 3);
            return mask;
        }

        private void Toggle(int cell)
        {
            int mask = Neighbours(cell);
            for (int i = 0; i < CellCount; i++) if ((mask & (1 << i)) != 0) values[i] ^= 1;
        }

        protected override void Change(WorkshopMove move) { Toggle(move.Cell); }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            if (Complete) return false;
            int current = 0;
            for (int i = 0; i < CellCount; i++) current |= values[i] << i;
            int best = -1, cost = 10;
            for (int option = 1; option < 512; option++)
            {
                int result = current, count = 0;
                for (int i = 0; i < 9; i++)
                {
                    if ((option & (1 << i)) == 0) continue;
                    result ^= Neighbours(i);
                    count++;
                }
                if (result == 511 && count < cost) { best = option; cost = count; }
            }
            if (best < 0) throw new InvalidOperationException("An unreachable lantern state was generated.");
            for (int i = 0; i < 9; i++)
            {
                if ((best & (1 << i)) == 0) continue;
                move = new WorkshopMove(i);
                return true;
            }
            return false;
        }
    }

    public sealed class ChannelBoard : WorkshopBoard
    {
        // Bit order: north, east, south, west. Closed pipe ends do not leak.
        private readonly int[] pieces = new int[16];
        private static readonly int[] path = { 0, 1, 2, 3, 7, 6, 5, 4, 8, 9, 10, 11, 15, 14, 13, 12 };
        private static readonly int[] neighbours = { -4, 1, 4, -1 };
        public override int ActionCount { get { return 16; } }
        public override bool Complete { get { return ConnectedCount == 16 && (Mask(12) & 8) != 0; } }
        public override string Status { get { return ConnectedCount + " / 16 canales conectados a la entrada"; } }

        public ChannelBoard(uint seed) : base(16, seed)
        {
            for (int i = 0; i < path.Length; i++)
            {
                int cell = path[i];
                int mask = 0;
                if (i == 0 || i == path.Length - 1) mask |= 8;
                if (i > 0) mask |= Direction(cell, path[i - 1]);
                if (i + 1 < path.Length) mask |= Direction(cell, path[i + 1]);
                target[cell] = mask;
                pieces[cell] = mask;
                values[cell] = random.Range(4);
            }
            if (Complete) values[0] = (values[0] + 1) % 4;
            SealInitial();
        }

        private static int Direction(int from, int to)
        {
            if (to == from - 4) return 1;
            if (to == from + 1) return 2;
            if (to == from + 4) return 4;
            if (to == from - 1) return 8;
            throw new ArgumentException("Pipe path must use adjacent cells.");
        }

        public static int Rotate(int mask, int turns)
        {
            for (int i = 0; i < Mod(turns, 4); i++) mask = ((mask << 1) & 15) | (mask >> 3);
            return mask;
        }

        public int Mask(int cell) { return Rotate(pieces[cell], Value(cell)); }

        public int ConnectedCount
        {
            get
            {
                if ((Mask(0) & 8) == 0) return 0;
                int visited = 1, pending = 1;
                while (pending != 0)
                {
                    int cell = 0;
                    while ((pending & (1 << cell)) == 0) cell++;
                    pending &= ~(1 << cell);
                    int mask = Mask(cell);
                    for (int direction = 0; direction < 4; direction++)
                    {
                        if ((mask & (1 << direction)) == 0) continue;
                        int next = cell + neighbours[direction];
                        if (next < 0 || next >= 16) continue;
                        if ((direction == 1 && cell % 4 == 3) || (direction == 3 && cell % 4 == 0)) continue;
                        if ((Mask(next) & (1 << ((direction + 2) % 4))) == 0) continue;
                        if ((visited & (1 << next)) != 0) continue;
                        visited |= 1 << next;
                        pending |= 1 << next;
                    }
                }
                int count = 0;
                for (int i = 0; i < 16; i++) if ((visited & (1 << i)) != 0) count++;
                return count;
            }
        }

        protected override void Change(WorkshopMove move)
        {
            values[move.Cell] = Mod(values[move.Cell] + move.Direction, 4);
        }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            if (Complete) return false;
            for (int i = 0; i < 16; i++)
            {
                if (Mask(i) == target[i]) continue;
                move = new WorkshopMove(i);
                return true;
            }
            return false;
        }
    }

    public sealed class SundialBoard : WorkshopBoard
    {
        public override int ActionCount { get { return 3; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 3 discos en hora"; } }

        public SundialBoard(uint seed) : base(3, seed)
        {
            for (int i = 0; i < 3; i++)
            {
                target[i] = random.Range(12);
                values[i] = (target[i] + 1 + random.Range(11)) % 12;
            }
            SealInitial();
        }

        protected override void Change(WorkshopMove move)
        {
            values[move.Cell] = Mod(values[move.Cell] + move.Direction, 12);
        }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            for (int i = 0; i < 3; i++)
            {
                if (values[i] == target[i]) continue;
                int direction = Mod(target[i] - values[i], 12) <= 6 ? 1 : -1;
                move = new WorkshopMove(i, direction);
                return true;
            }
            return false;
        }
    }

    public sealed class BalanceBoard : WorkshopBoard
    {
        private readonly int[] weights = new int[6];
        public int DesiredWeight { get; }
        public override int ActionCount { get { return 6; } }
        public override bool Complete { get { return CurrentWeight == DesiredWeight; } }
        public override string Status { get { return CurrentWeight + " / " + DesiredWeight + " medidas"; } }

        public int CurrentWeight
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < 6; i++) sum += values[i] * weights[i];
                return sum;
            }
        }

        public BalanceBoard(uint seed) : base(6, seed)
        {
            int chosen = 1 + random.Range(62);
            for (int i = 0; i < 6; i++)
            {
                weights[i] = 1 + i * 2 + random.Range(2);
                target[i] = (chosen >> i) & 1;
                DesiredWeight += target[i] * weights[i];
            }
            SealInitial();
        }

        public int Weight(int cell)
        {
            if (cell < 0 || cell >= 6) throw new ArgumentOutOfRangeException(nameof(cell));
            return weights[cell];
        }

        protected override void Change(WorkshopMove move) { values[move.Cell] ^= 1; }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            if (Complete) return false;
            int best = -1, cost = 7;
            for (int option = 0; option < 64; option++)
            {
                int weight = 0, changes = 0;
                for (int i = 0; i < 6; i++)
                {
                    int bit = (option >> i) & 1;
                    weight += bit * weights[i];
                    if (bit != values[i]) changes++;
                }
                if (weight == DesiredWeight && changes < cost) { best = option; cost = changes; }
            }
            for (int i = 0; i < 6; i++)
            {
                if (((best >> i) & 1) == values[i]) continue;
                move = new WorkshopMove(i);
                return true;
            }
            return false;
        }
    }

    public sealed class TideBoard : WorkshopBoard
    {
        public const int Maximum = 6;
        public override int ActionCount { get { return 16; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 4 depósitos en su marca · agua conservada: " + Total; } }
        public int Total
        {
            get { int sum = 0; for (int i = 0; i < 4; i++) sum += values[i]; return sum; }
        }

        public TideBoard(uint seed) : base(4, seed)
        {
            for (int i = 0; i < 4; i++) target[i] = values[i] = 1 + random.Range(5);
            for (int i = 0; i < 30; i++)
            {
                var move = new WorkshopMove(random.Range(16));
                if (Allowed(move)) Change(move);
            }
            if (Complete)
            {
                int destination = values[1] < Maximum ? 1 : 2;
                Change(new WorkshopMove(destination));
            }
            SealInitial();
        }

        protected override bool Allowed(WorkshopMove move)
        {
            int from = move.Cell / 4, to = move.Cell % 4;
            return CanTransfer(from, to);
        }

        public bool CanTransfer(int from, int to)
        {
            return from >= 0 && from < 4 && to >= 0 && to < 4 && from != to &&
                values[from] > 0 && values[to] < Maximum;
        }

        protected override void Change(WorkshopMove move)
        {
            values[move.Cell / 4]--;
            values[move.Cell % 4]++;
        }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            int from = -1, to = -1;
            for (int i = 0; i < 4; i++)
            {
                if (values[i] > target[i]) from = i;
                if (values[i] < target[i]) to = i;
            }
            if (from < 0 || to < 0) return false;
            move = new WorkshopMove(from * 4 + to);
            return true;
        }
    }

    public sealed class WindBoard : WorkshopBoard
    {
        public override int ActionCount { get { return 4; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 4 agujas orientadas"; } }

        public WindBoard(uint seed) : base(4, seed)
        {
            for (int i = 0; i < 4; i++) target[i] = values[i] = random.Range(4);
            for (int i = 0; i < 9; i++) Change(new WorkshopMove(random.Range(4)));
            if (Complete) Change(new WorkshopMove(0));
            SealInitial();
        }

        protected override void Change(WorkshopMove move)
        {
            int next = (move.Cell + 1) % 4;
            values[move.Cell] = Mod(values[move.Cell] + move.Direction, 4);
            values[next] = Mod(values[next] + move.Direction, 4);
        }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            if (Complete) return false;
            int best = -1, cost = int.MaxValue;
            for (int encoded = 0; encoded < 256; encoded++)
            {
                int[] turns = { encoded & 3, (encoded >> 2) & 3, (encoded >> 4) & 3, (encoded >> 6) & 3 };
                bool solved = true;
                int steps = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (Mod(values[i] + turns[i] + turns[(i + 3) % 4], 4) != target[i]) solved = false;
                    steps += Math.Min(turns[i], 4 - turns[i]);
                }
                if (solved && steps < cost) { best = encoded; cost = steps; }
            }
            if (best < 0) throw new InvalidOperationException("Unreachable compass state.");
            for (int i = 0; i < 4; i++)
            {
                int turns = (best >> (i * 2)) & 3;
                if (turns == 0) continue;
                move = new WorkshopMove(i, turns == 3 ? -1 : 1);
                return true;
            }
            return false;
        }
    }
}
