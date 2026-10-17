namespace Poolrooms.HDRP
{
    // Pure capability decision, shared by editor diagnostics and runtime fallback.
    public static class PoolroomsRayTracingPolicy
    {
        public static bool CanUse(bool hdrp,bool dx12,bool hardware,bool assetRT,bool ssr,bool gi,out string reason)
        {
            if(!hdrp){reason="no hay HDRP Asset activo";return false;}
            if(!dx12){reason="la sesión no usa Direct3D12";return false;}
            if(!hardware){reason="la GPU/controlador no admite ray tracing en esta sesión";return false;}
            if(!assetRT){reason="Realtime Ray Tracing está desactivado en el HDRP Asset";return false;}
            if(!ssr||!gi){reason="activa SSR y SSGI en el HDRP Asset";return false;}
            reason="compatible según GPU/API y HDRP Asset";return true;
        }
    }
}
