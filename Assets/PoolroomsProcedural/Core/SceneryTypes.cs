using System;
using System.Collections.Generic;
using System.Globalization;

namespace Poolrooms.Core
{
    // These IDs belong only to this appended furnishing layer. Existing room,
    // station, item and biome IDs are deliberately left untouched.
    [Flags]
    public enum SceneryMood
    {
        None = 0,
        Water = 1,
        Garden = 2,
        Civic = 4,
        Retail = 8,
        Domestic = 16,
        Play = 32,
        Any = Water | Garden | Civic | Retail | Domestic | Play
    }

    public enum SceneryStory
    {
        TeaForTwo,
        PlantExchange,
        ReadingCircle,
        AfternoonMarket,
        PackedLunch,
        BeforeOpening,
        RainShelter,
        PhotographyWalk,
        QuietMusic,
        SharedBreakfast,
        WaitingFriends,
        PottingLesson
    }

    public enum SceneryRole
    {
        Furniture,
        Seat,
        Table,
        Plant,
        Tabletop,
        Story
    }

    public readonly struct SceneryBox
    {
        public readonly V3 Min;
        public readonly V3 Max;

        public SceneryBox(V3 min, V3 max)
        {
            Min = min;
            Max = max;
        }

        public V3 Center { get { return (Min + Max) * .5f; } }
        public V3 Size { get { return Max - Min; } }

        public bool Overlaps(SceneryBox other, float gap = 0)
        {
            return Min.X < other.Max.X + gap && Max.X > other.Min.X - gap &&
                   Min.Y < other.Max.Y + gap && Max.Y > other.Min.Y - gap &&
                   Min.Z < other.Max.Z + gap && Max.Z > other.Min.Z - gap;
        }

        public bool ContainsXZ(float x, float z, float inset = 0)
        {
            return x >= Min.X + inset && x <= Max.X - inset &&
                   z >= Min.Z + inset && z <= Max.Z - inset;
        }

        public static SceneryBox Source(int source, V3 bottom, float yaw, float gap = 0)
        {
            var half = SourceFurniture.HalfExtents(source, yaw, gap);
            return new SceneryBox(
                new V3(bottom.X - half.X, bottom.Y, bottom.Z - half.Z),
                new V3(bottom.X + half.X, bottom.Y + half.Y * 2, bottom.Z + half.Z));
        }
    }

    public readonly struct SceneryPiece
    {
        public readonly int Source;
        public readonly SceneryRole Role;
        public readonly V3 Offset;
        public readonly float Yaw;
        public readonly int Parent;
        public readonly bool Required;

        // Parent indexes a preceding table in this recipe. Offsets remain in
        // the cluster coordinate system; the table supplies only the height.
        public SceneryPiece(int source, SceneryRole role, float x, float z,
            float yaw = 0, int parent = -1, bool required = true)
        {
            Source = source;
            Role = role;
            Offset = new V3(x, 0, z);
            Yaw = yaw;
            Parent = parent;
            Required = required;
        }
    }

    public sealed class SceneryRecipe
    {
        private readonly SceneryPiece[] pieces;
        public readonly int Id;
        public readonly string Name;
        public readonly SceneryMood Moods;
        public readonly SceneryStory Story;
        public readonly float Approach;
        public int Count { get { return pieces.Length; } }
        public SceneryPiece Piece(int index) { return pieces[index]; }

        public SceneryRecipe(int id, string name, SceneryMood moods,
            SceneryStory story, float approach, params SceneryPiece[] pieces)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
            if (pieces == null || pieces.Length == 0 || pieces.Length > 10)
                throw new ArgumentException(nameof(pieces));
            Id = id;
            Name = name;
            Moods = moods;
            Story = story;
            Approach = Math.Max(.65f, approach);
            this.pieces = (SceneryPiece[])pieces.Clone();
            for (int i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece.Source < 0 || piece.Source >= SourceFurniture.Count)
                    throw new ArgumentOutOfRangeException(nameof(pieces));
                if (piece.Parent >= i || piece.Parent < -1)
                    throw new ArgumentException("Table parent must precede its child.");
                if (piece.Parent >= 0 && pieces[piece.Parent].Role != SceneryRole.Table)
                    throw new ArgumentException("Only tables may support tabletop objects.");
            }
        }
    }

    public readonly struct SceneryPlacement
    {
        public readonly int Slot;
        public readonly int Recipe;
        public readonly int Cluster;
        public readonly int Piece;
        public readonly int Source;
        public readonly SceneryRole Role;
        public readonly int ParentSlot;
        public readonly V3 Bottom;
        public readonly float Yaw;

        public SceneryPlacement(int slot, int recipe, int cluster, int piece,
            int source, SceneryRole role, int parentSlot, V3 bottom, float yaw)
        {
            Slot = slot;
            Recipe = recipe;
            Cluster = cluster;
            Piece = piece;
            Source = source;
            Role = role;
            ParentSlot = parentSlot;
            Bottom = bottom;
            Yaw = yaw;
        }

        public SceneryBox Bounds { get { return SceneryBox.Source(Source, Bottom, Yaw); } }

        public string Key(int seed, int x, int z)
        {
            return "scenery:" + seed.ToString(CultureInfo.InvariantCulture) + ":" +
                x.ToString(CultureInfo.InvariantCulture) + ":" +
                z.ToString(CultureInfo.InvariantCulture) + ":" +
                Slot.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class SceneryPlan
    {
        private readonly List<SceneryPlacement> placements = new List<SceneryPlacement>();
        public IReadOnlyList<SceneryPlacement> Placements { get { return placements; } }
        public int Clusters { get; internal set; }
        public int Candidates { get; internal set; }
        public bool BudgetExhausted { get; internal set; }
        public bool InvalidInput { get; internal set; }
        internal void Add(SceneryPlacement placement) { placements.Add(placement); }
    }

    public readonly struct SceneryAnchor
    {
        public readonly V3 Bottom;
        public readonly float Yaw;
        public readonly int Slot;
        public readonly uint Rank;

        public SceneryAnchor(V3 bottom, float yaw, int slot, uint rank)
        {
            Bottom = bottom;
            Yaw = yaw;
            Slot = slot;
            Rank = rank;
        }

        public V3 Transform(V3 point)
        {
            float radians = Yaw * Shapes.Pi / 180;
            float c = (float)Math.Cos(radians);
            float s = (float)Math.Sin(radians);
            return new V3(Bottom.X + c * point.X + s * point.Z,
                Bottom.Y + point.Y, Bottom.Z - s * point.X + c * point.Z);
        }
    }
}
