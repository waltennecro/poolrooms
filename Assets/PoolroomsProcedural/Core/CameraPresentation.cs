using System;

namespace Poolrooms.Core
{
    public enum CameraViewMode { Normal, SoftVHS, Nostalgia, Film35mm, SoftDream, Architectural }

    [Serializable]
    public sealed class CameraPreferences
    {
        public int version=4;
        public bool landingMotion=true;
        public CameraViewMode viewMode=CameraViewMode.Normal;
        public float styleIntensity=1,flareIntensity=.55f,splashIntensity=.65f;
        public bool waterEffects=true,cameraMotion=false;
        public float bloomScale=1,grainScale=1,chromaticScale=1,vignetteScale=1;
        public bool reduceMotion;
        public bool locomotionMotion=true;
        public float motionIntensity=.55f,flareSize=1,ghostIntensity=.5f;
        public void Sanitize()
        {
            if(version<4)landingMotion=true;
            if(version<3){locomotionMotion=true;motionIntensity=.55f;flareSize=1;ghostIntensity=.5f;}
            if(version<2){bloomScale=grainScale=chromaticScale=vignetteScale=1;reduceMotion=false;}
            if(!Enum.IsDefined(typeof(CameraViewMode),viewMode))viewMode=CameraViewMode.Normal;
            styleIntensity=CameraPresentation.Unit(styleIntensity,1);
            flareIntensity=CameraPresentation.Unit(flareIntensity,.55f);
            splashIntensity=CameraPresentation.Unit(splashIntensity,.65f);version=4;
            motionIntensity=CameraPresentation.Unit(motionIntensity,.55f);
            flareSize=GamePreferences.Clamp(flareSize,.5f,1.5f,1);ghostIntensity=CameraPresentation.Unit(ghostIntensity,.5f);
            bloomScale=GamePreferences.Clamp(bloomScale,0,2,1);grainScale=CameraPresentation.Unit(grainScale,1);
            chromaticScale=CameraPresentation.Unit(chromaticScale,1);vignetteScale=CameraPresentation.Unit(vignetteScale,1);
        }
    }

    public readonly struct CameraLook
    {
        public readonly string Name,Description;
        public readonly float Temperature,Contrast,Saturation,Bloom,Grain,Chromatic,Vignette;
        public readonly float Scanlines,Bleed,Jitter,Matte,Diffusion;
        public CameraLook(string name,string description,float temperature,float contrast,float saturation,
            float bloom,float grain,float chromatic,float vignette,float scanlines=0,float bleed=0,
            float jitter=0,float matte=0,float diffusion=0)
        {
            Name=name;Description=description;Temperature=temperature;Contrast=contrast;Saturation=saturation;
            Bloom=bloom;Grain=grain;Chromatic=chromatic;Vignette=vignette;Scanlines=scanlines;
            Bleed=bleed;Jitter=jitter;Matte=matte;Diffusion=diffusion;
        }
    }

    public static class CameraPresentation
    {
        public const int MaxFlares=4;
        public static float Unit(float value,float fallback=0)
        {return float.IsNaN(value)||float.IsInfinity(value)?fallback:Math.Max(0,Math.Min(1,value));}
        public static CameraLook Look(CameraViewMode mode)
        {
            switch(mode)
            {
                case CameraViewMode.Architectural:return new CameraLook("Arquitectura", "Color neutro, detalle limpio y resplandor contenido para leer materiales y espacios.",
                    0,2,-1,.055f,0,0,.025f);
                case CameraViewMode.SoftVHS:return new CameraLook("VHS suave","Cinta analógica: líneas tenues, color ligeramente corrido y grano. Sin parpadeos bruscos.",
                    4,-10,-18,.14f,.18f,.11f,.14f,.12f,1.15f,.35f,.035f,.12f);
                case CameraViewMode.Nostalgia:return new CameraLook("Nostalgia","Recuerdo cálido: sombras suaves, luces cremosas y un grano casi imperceptible.",
                    16,-8,-10,.22f,.045f,.012f,.12f,matte:.075f,diffusion:.16f);
                case CameraViewMode.Film35mm:return new CameraLook("Fotografía 35 mm","Textura de película fina, contraste contenido y separación natural de luces y sombras.",
                    3,10,-6,.15f,.075f,.018f,.15f,matte:.025f,diffusion:.04f);
                case CameraViewMode.SoftDream:return new CameraLook("Sueño suave","Brillo difuso, colores tranquilos y contornos suaves, conservando la lectura del espacio.",
                    -1,-5,-7,.26f,.015f,0,.045f,matte:.035f,diffusion:.3f);
                default:return new CameraLook("Normal","Visión limpia: conserva el ambiente de cada sala, sin textura de cinta ni filtros de color.",
                    -2,4,-3,.1f,0,0,.08f);
            }
        }

        // Optical source strength, deliberately bounded: bright rooms never white out the view.
        public static float FlareStrength(float lumens,float distance,float facing,bool reflected)
        {
            if(float.IsNaN(lumens)||float.IsInfinity(lumens)||lumens<=0||distance<0||float.IsNaN(distance))return 0;
            float light=lumens/(lumens+4000);
            float attenuation=1/(1+distance*distance/500);
            return Unit(light*attenuation*Unit(facing)*(reflected?.55f:1));
        }

        // Specular point on a horizontal plane, using a mirrored light. Runtime still checks the
        // real water footprint and both occlusion rays; this alone does not establish visibility.
        public static bool ReflectOnWater(V3 eye,V3 light,float level,out V3 point,out V3 image)
        {
            image=new V3(light.X,2*level-light.Y,light.Z);point=new V3();
            if(!Finite(eye.X)||!Finite(eye.Y)||!Finite(eye.Z)||!Finite(light.X)||!Finite(light.Y)||!Finite(light.Z)||!Finite(level))return false;
            if(eye.Y<=level+.02f||light.Y<=level+.02f)return false;
            float t=(level-eye.Y)/(image.Y-eye.Y);
            if(t<=0||t>=1)return false;
            point=eye+(image-eye)*t;point.Y=level;return true;
        }
        private static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
    }

    // Tracks surface contacts, not health, oxygen or objectives. Reset on teleport/unloaded rooms.
    public sealed class WaterContactTracker
    {
        private bool initialized,wet;
        private float peakDownward,cooldown;
        public void Reset(){initialized=false;wet=false;peakDownward=0;cooldown=0;}
        public bool SurfaceKick(out float strength)
        {
            strength=0;
            if(!initialized||!wet||cooldown>0)return false;
            strength=.28f;cooldown=.35f;return true;
        }
        public bool Sample(bool hasWater,float feetRelativeToSurface,float downwardSpeed,float deltaTime,out float strength,bool grounded=false)
        {
            strength=0;
            if(deltaTime<=0)return false;
            bool contact=hasWater&&feetRelativeToSurface<=-.015f;
            if(!initialized){initialized=true;wet=contact;return false;}
            cooldown=Math.Max(0,cooldown-deltaTime);
            if(grounded&&!hasWater)peakDownward=0;
            if(!wet&&!(grounded&&!hasWater))peakDownward=Math.Max(peakDownward,Math.Max(0,downwardSpeed));
            bool entered=contact&&!wet;
            // Hysteresis prevents the same contact firing repeatedly near the waterline.
            if(!hasWater||feetRelativeToSurface>.035f)wet=false;
            else if(contact)wet=true;
            if(!entered||cooldown>0)return false;
            strength=.06f+.94f*CameraPresentation.Unit((peakDownward-1.2f)/7);
            peakDownward=0;cooldown=.22f;return true;
        }
    }
}
