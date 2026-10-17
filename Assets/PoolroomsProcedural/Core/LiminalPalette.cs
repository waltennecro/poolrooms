namespace Poolrooms.Core
{
    public static class LiminalPalette
    {
        public static V3 Color(Surface surface,int variant)
        {
            bool faded=variant==1,night=variant==2;
            switch(surface)
            {
                case Surface.Tile:return faded?new V3(.53f,.65f,.63f):night?new V3(.32f,.46f,.4f):new V3(.27f,.61f,.48f);
                case Surface.Accent:return faded?new V3(.82f,.72f,.4f):night?new V3(.62f,.55f,.25f):new V3(.94f,.72f,.16f);
                case Surface.Metal:return faded?new V3(.39f,.55f,.7f):night?new V3(.22f,.38f,.6f):new V3(.15f,.43f,.76f);
                case Surface.Slide:return faded?new V3(.72f,.4f,.39f):night?new V3(.54f,.23f,.3f):new V3(.82f,.24f,.25f);
                default:return new V3(.9f,.87f,.78f);
            }
        }
        public static float Smoothness(Surface surface)
        {return surface==Surface.Tile?.22f:surface==Surface.Plaster?.18f:surface==Surface.Slide?.48f:.35f;}
    }
}
