namespace Poolrooms.Core
{
    public static class RetailPalette
    {
        public static V3 Color(Surface surface,int variant)
        {
            if(InteriorPalette.Uses(variant))return InteriorPalette.Color(surface,variant);
            if(variant==3)
            {
                switch(surface){case Surface.Tile:return new V3(.72f,.66f,.53f);case Surface.Accent:return new V3(.56f,.31f,.12f);
                    case Surface.Metal:return new V3(.56f,.57f,.57f);case Surface.Slide:return new V3(.13f,.24f,.31f);default:return new V3(.83f,.79f,.68f);}
            }
            bool old=variant==1,cold=variant==2;
            switch(surface)
            {
                case Surface.Tile:return old?new V3(.53f,.5f,.39f):cold?new V3(.42f,.48f,.46f):new V3(.65f,.66f,.58f);
                case Surface.Accent:return old?new V3(.42f,.3f,.16f):cold?new V3(.25f,.34f,.3f):new V3(.53f,.35f,.22f);
                case Surface.Metal:return old?new V3(.43f,.42f,.35f):new V3(.42f,.45f,.44f);
                case Surface.Slide:return old?new V3(.13f,.17f,.16f):new V3(.09f,.17f,.2f);
                default:return old?new V3(.7f,.65f,.49f):cold?new V3(.53f,.62f,.59f):new V3(.75f,.76f,.67f);
            }
        }
        public static float Smoothness(Surface surface)
        {return surface==Surface.Slide?.72f:surface==Surface.Tile?.34f:surface==Surface.Metal?.4f:.2f;}
        public static float Metallic(Surface surface){return surface==Surface.Metal?.55f:0;}
    }
}
