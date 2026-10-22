using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // A separate appended bank keeps all twenty Journey programmes and their
    // source finish IDs intact. The connected world opts this bank in explicitly.
    public static class HorizonLevelPlanner
    {
        public const int FirstEnvironment = 58;
        public const int Count = 5;
        private static readonly string[] names =
        {
            "Patios de magnolias", "Terminal de funiculares", "Palacio de vitrales",
            "Patios de campanas", "Patios del faro"
        };
        private static readonly string[] notes =
        {
            "El grupo volverá después de regar las magnolias. Dejamos dos tazas y una cesta para recoger las flores caídas; guarda un asiento a la sombra.",
            "La estación descansa entre viajes. Las cabinas de exposición están abiertas para entrar y mirar los planos. Nos encontraremos aquí antes del próximo paseo.",
            "Los vitrales cambian con la tarde. Hemos preparado las mesas para dibujar sus formas; puedes comenzar mientras regresan los demás.",
            "El taller dejó preparada una secuencia de campanas. Escucha con calma y vuelve a tocar los mismos pasos; nos reuniremos al terminar el ensayo.",
            "El faro interior señala el lugar de reunión. Hemos abierto la plataforma y colocado un banco para esperar juntos a quienes llegan por la galería."
        };
        private static readonly StationKind[] activities =
        {
            StationKind.SeedNursery, StationKind.WindRose, StationKind.Mosaic,
            StationKind.BellSequence, StationKind.LanternCircuit
        };
        // These reference actual PBR finishes in the existing attributed bank.
        // Each level has a separate combination, not duplicated image files.
        private static readonly int[] floors = { 1, 6, 3, 11, 14 };
        private static readonly int[] walls = { 21, 26, 23, 31, 34 };

        public static bool IsLevel(LiminalEnvironment e)
        { return (int)e >= FirstEnvironment && (int)e < FirstEnvironment + Count; }
        public static bool IsMode(LiminalGeneration mode)
        { return mode >= LiminalGeneration.MagnoliaCourts && mode <= LiminalGeneration.LighthouseCourts; }
        public static bool IsTheme(SurfaceTheme theme)
        { return theme >= SurfaceTheme.MagnoliaCourts && theme <= SurfaceTheme.LighthouseCourts; }
        public static int Index(LiminalEnvironment e)
        {
            if (!IsLevel(e)) throw new ArgumentOutOfRangeException(nameof(e));
            return (int)e - FirstEnvironment;
        }
        public static LiminalEnvironment ForMode(LiminalGeneration mode)
        {
            if (!IsMode(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            return (LiminalEnvironment)(FirstEnvironment + (int)mode - (int)LiminalGeneration.MagnoliaCourts);
        }
        public static SurfaceTheme Theme(LiminalEnvironment e)
        { return (SurfaceTheme)((int)SurfaceTheme.MagnoliaCourts + Index(e)); }
        public static string Name(LiminalEnvironment e) { return names[Index(e)]; }
        public static RoomVignette Memory(LiminalEnvironment e)
        { int i = Index(e); return new RoomVignette(names[i], notes[i], RoomActivity.Rest, i); }
        public static bool WarmLight(LiminalEnvironment e)
        { return e == LiminalEnvironment.MagnoliaCourts || e == LiminalEnvironment.BellCourts || e == LiminalEnvironment.LighthouseCourts; }
        public static AcousticLook Acoustics(Room room, float delay)
        {
            switch (room.Environment)
            {
                case LiminalEnvironment.MagnoliaCourts: return new AcousticLook(.025f, 0, 1.15f, .38f, delay, delay * 1.3f);
                case LiminalEnvironment.FunicularTerminal: return new AcousticLook(.07f, 0, 1.65f, .5f, delay, delay * 1.5f);
                case LiminalEnvironment.StainedGlassHalls: return new AcousticLook(.025f, 0, 2.15f, .57f, delay, delay * 1.7f);
                case LiminalEnvironment.BellCourts: return new AcousticLook(.02f, 0, 1.4f, .45f, delay, delay * 1.4f);
                default: return new AcousticLook(.055f, 0, 1.6f, .48f, delay, delay * 1.5f);
            }
        }
        public static int Finish(SurfaceTheme theme, Surface slot)
        {
            if (!IsTheme(theme) || slot == Surface.Glow) return -1;
            int i = (int)theme - (int)SurfaceTheme.MagnoliaCourts;
            return slot == Surface.Tile ? floors[i] : slot == Surface.Plaster ? walls[i] :
                slot == Surface.Accent ? 40 : slot == Surface.Metal ? 41 : 42;
        }
        public static string TextureSet(SurfaceTheme theme, Surface slot)
        {
            if (slot == Surface.Glow) return null;
            if (slot == Surface.Metal) return "EncounterBrass";
            if (slot == Surface.Plaster) return "GardenLimewash";
            if (slot == Surface.Accent) return "EncounterOak";
            return slot == Surface.Slide ? "EncounterLinen" : "GardenLimestone";
        }
        public static void Furnish(LiminalEnvironment e, List<QuietPropPlan> props)
        {
            for (int i = 0; i < props.Count; i++)
            {
                var p = props[i]; string source = p.Source;
                if (source == "B_CafeChair") source = e == LiminalEnvironment.FunicularTerminal ? "M_IndoorChair" :
                    e == LiminalEnvironment.StainedGlassHalls ? "I_ChairDamaskPurplegold" : "B_CafeChair";
                // The substitutions keep the original support footprint and all
                // slot orderings; taller floor furnishings never go on tables.
                if (source == "R_BarnLamp" && WarmLight(e)) source = "M_StainedLamp";
                if (source != p.Source) props[i] = new QuietPropPlan(p.Bottom, p.CatalogId, source, p.Yaw);
            }
        }
        public static DressingAnchor ActivityAnchor(PlanRect bay, int index, int variant)
        {
            int sx = index % 2 == 0 ? -1 : 1;
            float x = sx < 0 ? bay.MaxX + .2f : bay.MinX - .2f;
            float z = bay.Z;
            var normal = new V3(-sx, 0, 0);
            var center = new V3(x, 1.2f, z);
            var right = new V3(normal.Z, 0, -normal.X);
            var a = center - right * .75f - V3.Up * .45f;
            var b = center + right * .75f - V3.Up * .45f;
            var c = center + V3.Up * .9f;
            return new DressingAnchor(center, normal, a, b, c, 0, variant);
        }
        public static List<StationPlacement> Stations(Room room, LayoutConfig config)
        {
            var result = new List<StationPlacement>(4);
            if (!config.EncounterExploration || room.Plan?.Restful == null) return result;
            var plan = room.Plan.Restful;
            for (int i = 0; i < 4; i++)
            {
                var kind = i == 0 ? activities[Index(room.Environment)] :
                    i == 1 ? StationKind.Tea : i == 2 ? StationKind.Postcards : StationKind.RestBell;
                result.Add(new StationPlacement(ActivityAnchor(plan.Bays[i], i, plan.Layout), kind,
                    10000 + i, plan.Layout % 3));
            }
            return result;
        }
    }
}
