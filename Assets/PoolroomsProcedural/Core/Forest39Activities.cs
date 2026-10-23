using System;

namespace Poolrooms.Core
{
    public enum Forest39Activity
    {
        LeafPress, StoneBalance, TrailCompass, WaterStudy,
        Listening, FallenLeaf, Acorn, FieldNotebook
    }

    public readonly struct Forest39Action
    {
        public readonly Forest39Activity Kind;
        public readonly V3 Bottom;
        public readonly int Slot;
        public readonly float Yaw;

        public Forest39Action(Forest39Activity kind, V3 bottom, int slot, float yaw = 0)
        {
            Kind = kind;
            Bottom = bottom;
            Slot = slot;
            Yaw = yaw;
        }
    }

    // Compact session state; one optional activity never gates an exit.
    // Answers derive from the level seed and are exposed through physical marks.
    public sealed class Forest39ActivityState
    {
        public readonly Forest39Activity Kind;
        public readonly int Target, Limit;
        public int Position { get; private set; }
        public int Actions { get; private set; }
        public bool Complete { get; private set; }
        public bool Claimed { get; private set; }

        public Forest39ActivityState(Forest39Activity kind, uint seed)
        {
            Kind = kind;
            Limit = kind == Forest39Activity.TrailCompass ? 4 : kind == Forest39Activity.StoneBalance ? 5 : 6;
            Target = (int)(seed % (uint)Limit);
            Position = (Target + 1) % Limit;
        }

        public bool Advance()
        {
            if (Complete) return false;
            Position = (Position + 1) % Limit;
            Actions++;
            Complete = Position == Target;
            return true;
        }

        public bool Claim()
        {
            if (Claimed) return false;
            Claimed = true;
            return true;
        }

        public void Restore(int position, bool complete, bool claimed)
        {
            if (position < 0 || position >= Limit) throw new ArgumentOutOfRangeException(nameof(position));
            Position = position;
            Complete = complete && position == Target;
            Claimed = claimed;
        }

        public string Hint()
        {
            switch (Kind)
            {
                case Forest39Activity.LeafPress:
                    return "Alinea la hoja con la marca " + (Target + 1) + " de la prensa.";
                case Forest39Activity.StoneBalance:
                    return "La piedra de referencia tiene " + (Target + 1) + " muescas; busca la misma altura.";
                case Forest39Activity.TrailCompass:
                    return "Gira el indicador hasta la marca " + (Target + 1) + " del mapa; las salidas siguen abiertas.";
                case Forest39Activity.WaterStudy:
                    return "Compara la altura del agua con la marca " + (Target + 1) + " de la ficha.";
                default:
                    return "No hay prisa. Puedes continuar el paseo cuando quieras.";
            }
        }

        public string Feedback()
        {
            if (Complete) return "El estudio está completo. Queda preparado para quien llegue después.";
            return "Marca actual: " + (Position + 1) + " / " + Limit + ". " + Hint();
        }
    }

    public static class Forest39Activities
    {
        public const int Count = 8;
        public const int MaximumPerSector = 6;

        public static Forest39Action[] Plan(Forest39Plan plan)
        {
            float x = plan.Rest.X, z = plan.Rest.Z;
            var primary = (Forest39Activity)((int)plan.Clearing % 4);
            var result = new Forest39Action[MaximumPerSector];
            result[0] = new Forest39Action(primary, new V3(x - 1.45f, 0, z + 1.1f), 0);
            result[1] = new Forest39Action(Forest39Activity.FieldNotebook, new V3(x, SourceFurniture.Dimensions(7).Y + .009f, z + .3f), 1);
            result[2] = new Forest39Action(Forest39Activity.Listening, new V3(x, .01f, z - 1.1f), 2);
            // Collectable locations live in the flat clear region. They remain
            // reachable even when foliage or source furniture quality is disabled.
            result[3] = new Forest39Action(Forest39Activity.FallenLeaf, new V3(2.1f, 0, -.7f), 3);
            result[4] = new Forest39Action(Forest39Activity.Acorn, new V3(-.7f, 0, 2.1f), 4);
            result[5] = new Forest39Action(Forest39Activity.FallenLeaf, new V3(.9f, 0, -1.8f), 5);
            return result;
        }

        public static string Name(Forest39Activity kind)
        {
            switch (kind)
            {
                case Forest39Activity.LeafPress: return "Prensa del herbario";
                case Forest39Activity.StoneBalance: return "Estudio de equilibrio";
                case Forest39Activity.TrailCompass: return "Mapa de los senderos";
                case Forest39Activity.WaterStudy: return "Registro del estanque";
                case Forest39Activity.Listening: return "El banco que escucha";
                case Forest39Activity.FallenLeaf: return "Hoja caída de roble";
                case Forest39Activity.Acorn: return "Bellota del camino";
                default: return "Cuaderno del bosque";
            }
        }

        public static StationSound Sound(Forest39Activity kind)
        {
            switch (kind)
            {
                case Forest39Activity.WaterStudy: return StationSound.WaterDrop;
                case Forest39Activity.StoneBalance: return StationSound.Ceramic;
                case Forest39Activity.TrailCompass: return StationSound.Latch;
                case Forest39Activity.FieldNotebook: return StationSound.Page;
                default: return StationSound.Leaves;
            }
        }

        public static string ItemKey(Forest39Activity kind)
        {
            return kind == Forest39Activity.FallenLeaf ? "forest39:0" :
                kind == Forest39Activity.Acorn ? "forest39:1" : "forest39:2";
        }

        public static ExplorationItem Item(int id)
        {
            switch (id)
            {
                case 0:
                    return new ExplorationItem("forest39:0", "Hoja de roble",
                        "Una hoja caída recogida en el Nivel 39. Sus nervaduras conservan el dibujo del bosque.", ItemUse.Read);
                case 1:
                    return new ExplorationItem("forest39:1", "Bellota del camino",
                        "Una pequeña bellota. Puedes guardarla como recuerdo de un paseo sin prisa.");
                case 2:
                    return new ExplorationItem("forest39:2", "Ficha del herbario",
                        "Un estudio terminado de las hojas, las piedras y la luz del bosque.", ItemUse.Read);
                default: return null;
            }
        }

        public static string Key(int seed, int x, int z, int slot)
        {
            return "forest39:" + seed + ":" + x + ":" + z + ":" + slot;
        }
    }
}
