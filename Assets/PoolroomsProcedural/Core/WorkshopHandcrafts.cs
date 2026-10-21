using System;

namespace Poolrooms.Core
{
    public sealed class WeavingBoard : WorkshopBoard
    {
        public const int Colours = 4;
        public override int ActionCount { get { return 16; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 16 hilos en su lugar"; } }

        public WeavingBoard(uint seed) : base(16, seed)
        {
            int warp = random.Range(4), weft = random.Range(4);
            int pattern = random.Range(3);
            for (int i = 0; i < CellCount; i++)
            {
                int x = i % 4, y = i / 4;
                switch (pattern)
                {
                    case 0: target[i] = (x + y + warp) % Colours; break;
                    case 1: target[i] = ((x / 2 + y / 2) % 2 == 0 ? warp : weft); break;
                    default: target[i] = (Math.Abs(x - y) + warp) % Colours; break;
                }
                values[i] = random.Range(Colours);
            }
            if (Complete) values[0] = (values[0] + 1) % Colours;
            SealInitial();
        }

        protected override void Change(WorkshopMove move)
        {
            values[move.Cell] = Mod(values[move.Cell] + move.Direction, Colours);
        }

        public override bool Hint(out WorkshopMove move)
        {
            move = default;
            for (int i = 0; i < CellCount; i++)
            {
                if (values[i] == target[i]) continue;
                int forward = Mod(target[i] - values[i], Colours);
                move = new WorkshopMove(i, forward <= 2 ? 1 : -1);
                return true;
            }
            return false;
        }
    }

    public sealed class HerbariumBoard : WorkshopBoard
    {
        private static readonly string[] species =
        {
            "Albahaca", "Bambú", "Caléndula", "Dalia", "Eucalipto", "Helecho"
        };
        public override int ActionCount { get { return 6; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 6 etiquetas ordenadas"; } }

        public HerbariumBoard(uint seed) : base(6, seed)
        {
            for (int i = 0; i < 6; i++) target[i] = values[i] = i;
            for (int i = 5; i > 0; i--)
            {
                int other = random.Range(i + 1);
                int value = values[i];
                values[i] = values[other];
                values[other] = value;
            }
            if (Complete) Change(new WorkshopMove(0));
            SealInitial();
        }

        public static string Species(int index)
        {
            if (index < 0 || index >= species.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return species[index];
        }

        protected override void Change(WorkshopMove move)
        {
            int next = Mod(move.Cell + move.Direction, 6);
            int old = values[move.Cell];
            values[move.Cell] = values[next];
            values[next] = old;
        }

        public override bool Hint(out WorkshopMove move)
        {
            // Adjacent inversion removal is a terminating sorting strategy;
            // unlike random swaps it never undoes a correctly sorted prefix.
            for (int i = 0; i < 5; i++)
            {
                if (values[i] <= values[i + 1]) continue;
                move = new WorkshopMove(i);
                return true;
            }
            move = default;
            return false;
        }
    }

    public sealed class StarBoard : WorkshopBoard
    {
        public override int ActionCount { get { return 9; } }
        public override bool Complete { get { return Connected == CellCount; } }
        public override string Status { get { return Connected + " / 9 estrellas unidas"; } }
        public int Connected
        {
            get
            {
                int count = 0;
                for (int i = 0; i < CellCount; i++) count += values[i];
                return count;
            }
        }

        public StarBoard(uint seed) : base(9, seed)
        {
            for (int i = 0; i < CellCount; i++) target[i] = i + 1;
            for (int i = CellCount - 1; i > 0; i--)
            {
                int other = random.Range(i + 1);
                int old = target[i];
                target[i] = target[other];
                target[other] = old;
            }
            SealInitial();
        }

        protected override bool Allowed(WorkshopMove move)
        {
            return values[move.Cell] == 0 && target[move.Cell] == Connected + 1;
        }

        protected override void Change(WorkshopMove move) { values[move.Cell] = 1; }

        public override bool Hint(out WorkshopMove move)
        {
            int next = Connected + 1;
            for (int i = 0; i < CellCount; i++)
            {
                if (target[i] != next) continue;
                move = new WorkshopMove(i);
                return true;
            }
            move = default;
            return false;
        }
    }

    public sealed class BellBoard : WorkshopBoard
    {
        public const int Notes = 8;
        public override int ActionCount { get { return Notes; } }
        public override bool Complete { get { return Entered == CellCount; } }
        public override string Status { get { return Entered + " / 6 notas de la melodía"; } }
        public int Entered
        {
            get
            {
                for (int i = 0; i < CellCount; i++) if (values[i] < 0) return i;
                return CellCount;
            }
        }

        public BellBoard(uint seed) : base(6, seed)
        {
            // A pentatonic phrase avoids dissonant random clusters. The full
            // eight-key keyboard remains playable without losing progress.
            int[] pentatonic = { 0, 1, 2, 4, 5, 7 };
            int previous = random.Range(pentatonic.Length);
            for (int i = 0; i < CellCount; i++)
            {
                previous = Mod(previous + random.Range(3) - 1, pentatonic.Length);
                target[i] = pentatonic[previous];
                values[i] = -1;
            }
            SealInitial();
        }

        protected override bool Allowed(WorkshopMove move)
        {
            int next = Entered;
            return next < CellCount && move.Cell == target[next];
        }

        protected override void Change(WorkshopMove move) { values[Entered] = move.Cell; }

        public override bool Hint(out WorkshopMove move)
        {
            int next = Entered;
            move = next < CellCount ? new WorkshopMove(target[next]) : default;
            return next < CellCount;
        }
    }

    public sealed class BindingBoard : WorkshopBoard
    {
        public override int ActionCount { get { return 3; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 9 páginas encuadernadas"; } }

        public BindingBoard(uint seed) : base(9, seed)
        {
            for (int row = 0; row < 3; row++)
            {
                int colour = random.Range(4);
                for (int col = 0; col < 3; col++) target[row * 3 + col] = values[row * 3 + col] = (colour + col) % 4;
                int rotations = 1 + random.Range(2);
                for (int i = 0; i < rotations; i++) Change(new WorkshopMove(row));
            }
            SealInitial();
        }

        protected override void Change(WorkshopMove move)
        {
            int first = move.Cell * 3;
            if (move.Direction > 0)
            {
                int old = values[first + 2];
                values[first + 2] = values[first + 1];
                values[first + 1] = values[first];
                values[first] = old;
            }
            else
            {
                int old = values[first];
                values[first] = values[first + 1];
                values[first + 1] = values[first + 2];
                values[first + 2] = old;
            }
        }

        public override bool Hint(out WorkshopMove move)
        {
            for (int row = 0; row < 3; row++)
            {
                int first = row * 3;
                if (values[first] == target[first]) continue;
                int direction = values[first + 2] == target[first] ? 1 : -1;
                move = new WorkshopMove(row, direction);
                return true;
            }
            move = default;
            return false;
        }
    }

    public sealed class NurseryBoard : WorkshopBoard
    {
        public const int Maximum = 5;
        public override int ActionCount { get { return 6; } }
        public override bool Complete { get { return Matches(); } }
        public override string Status { get { return MatchedCount() + " / 6 semilleros con humedad adecuada"; } }

        public NurseryBoard(uint seed) : base(6, seed)
        {
            for (int i = 0; i < CellCount; i++)
            {
                target[i] = 2 + random.Range(3);
                values[i] = random.Range(2);
            }
            SealInitial();
        }

        protected override bool Allowed(WorkshopMove move)
        {
            int next = values[move.Cell] + move.Direction;
            return next >= 0 && next <= Maximum;
        }

        protected override void Change(WorkshopMove move) { values[move.Cell] += move.Direction; }

        public override bool Hint(out WorkshopMove move)
        {
            for (int i = 0; i < CellCount; i++)
            {
                if (values[i] == target[i]) continue;
                move = new WorkshopMove(i, values[i] < target[i] ? 1 : -1);
                return true;
            }
            move = default;
            return false;
        }
    }
}
