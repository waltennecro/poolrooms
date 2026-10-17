namespace Poolrooms.Core
{
    // Numeric budgets only. The HDRP bridge still decides device/asset capability.
    public readonly struct GraphicsSampling
    {
        public readonly bool Ultra;
        public readonly int ReflectionSamples,ReflectionBounces,IndirectSamples,IndirectBounces,ShadowResolution;
        public readonly float ReflectionDistance,IndirectDistance;
        public GraphicsSampling(int quality,bool rayTracing)
        {
            Ultra=quality==3;
            ReflectionSamples=rayTracing?(Ultra?8:4):2;
            ReflectionBounces=rayTracing&&Ultra?2:1;
            IndirectSamples=rayTracing?(Ultra?8:4):2;
            IndirectBounces=rayTracing?(Ultra?4:3):2;
            ReflectionDistance=Ultra?90:60;IndirectDistance=Ultra?65:45;
            ShadowResolution=quality==0?512:Ultra?2048:1024;
        }
    }
}
