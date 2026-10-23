namespace Poolrooms.Core
{
    public static class Forest39Materials
    {
        public static bool IsTheme(SurfaceTheme theme)
        {
            return theme >= SurfaceTheme.Forest39Ground && theme <= SurfaceTheme.Forest39CanopyLeaves;
        }

        public static string Bank(SurfaceTheme theme, Surface surface)
        {
            if (theme == SurfaceTheme.Forest39CanopyLeaves) return "Canopy";
            if (theme == SurfaceTheme.Forest39Leaves) return "Leaf";
            if (theme == SurfaceTheme.Forest39Stone) return "Stone";
            if (theme == SurfaceTheme.Forest39Bark) return "Bark";
            return "Ground";
        }

        public static V3 Tint(SurfaceTheme theme, Surface surface)
        {
            if (theme == SurfaceTheme.Forest39Leaves) return new V3(.88f, .96f, .73f);
            if (theme == SurfaceTheme.Forest39Stone) return new V3(1, 1, 1);
            if (theme == SurfaceTheme.Forest39Ground && surface == Surface.Accent)
                return new V3(.7f, .59f, .42f);
            return new V3(1, 1, 1);
        }

        public static float Roughness(SurfaceTheme theme)
        {
            return theme == SurfaceTheme.Forest39Leaves ? .66f : .88f;
        }
    }
}
