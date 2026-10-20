using System;

namespace Poolrooms.Core
{
    public enum PoolWaterAppearance { ClearCeramic, SoftTurquoise, BluePorcelain, Custom, CrystalClear }
    public enum PoolWaterSetting { Pool,Coast,Thermal,Aquatic }
    public enum PoolWaterDetail { Performance, High, Cinematic }
    // Optical quality is independent of mesh/streaming distance and never changes physical water contacts.
    public readonly struct PoolWaterOptics
    {
        public readonly float FarSmoothness,FadeStart,FadeDistance,RefractionScale;
        public readonly int ProbeResolution;
        public PoolWaterOptics(PoolWaterDetail detail)
        {
            bool low=detail==PoolWaterDetail.Performance,cinematic=detail==PoolWaterDetail.Cinematic;
            FarSmoothness=low?.94f:cinematic?.965f:.955f;
            FadeStart=low?25:cinematic?60:40;
            FadeDistance=low?70:cinematic?160:120;
            RefractionScale=.42f;
            ProbeResolution=low?128:cinematic?512:256;
        }
    }
    public readonly struct PoolWaterLook
    {
        public readonly V3 Refraction,Scattering;
        public readonly float Absorption,RefractionDistance;
        public PoolWaterLook(V3 refraction,V3 scattering,float absorption,float refractionDistance)
        {Refraction=refraction;Scattering=scattering;Absorption=absorption;RefractionDistance=refractionDistance;}
    }
    public readonly struct WaterBudget
    {
        public readonly int Ripples,SurfaceResolution,AtlasResolution;
        public readonly float RegionSize,Tessellation;
        public readonly bool Foam,Caustics;
        public WaterBudget(int ripples,int surface,int atlas,float region,float tessellation,bool foam,bool caustics)
        {Ripples=ripples;SurfaceResolution=surface;AtlasResolution=atlas;RegionSize=region;Tessellation=tessellation;Foam=foam;Caustics=caustics;}
    }
    public static class PoolWaterModel
    {
        public const int MaxRipples=12;
        public const float MaxHeight=.04f;
        public const float RefractiveIndex=1.333f;
        public static float Fresnel(float cosine)
        {
            cosine=CameraPresentation.Unit(cosine);
            float ratio=(1-RefractiveIndex)/(1+RefractiveIndex),f0=ratio*ratio;
            return f0+(1-f0)*(float)Math.Pow(1-cosine,5);
        }
        // Three dispersive capillary/gravity wave bands, matching the native decal.
        // Envelope propagation, attenuation and the rim mask bound energy per event.
        public static float RipplePacket(float distance,float age)
        {
            if(float.IsNaN(distance)||float.IsInfinity(distance)||float.IsNaN(age)||float.IsInfinity(age)||distance<0||age<0)return 0;
            float phase=distance-(.12f+age*1.45f);
            float packet=(float)Math.Exp(-phase*phase/.09f);
            float waves=.62f*(float)Math.Sin(distance*14-age*(float)Math.Sqrt(9.81f*14))+
                .25f*(float)Math.Sin(distance*23-age*(float)Math.Sqrt(9.81f*23))+
                .13f*(float)Math.Sin(distance*37-age*(float)Math.Sqrt(9.81f*37));
            return packet*waves/(1+distance*.65f);
        }
        public static PoolWaterLook Look(PoolWaterAppearance appearance)
        {
            switch(appearance)
            {
                case PoolWaterAppearance.SoftTurquoise:return new PoolWaterLook(new V3(.94f,.987f,.98f),new V3(.008f,.045f,.038f),10,.55f);
                case PoolWaterAppearance.BluePorcelain:return new PoolWaterLook(new V3(.935f,.975f,.994f),new V3(.006f,.027f,.055f),11,.5f);
                case PoolWaterAppearance.CrystalClear:return new PoolWaterLook(new V3(.985f,.998f,.996f),new V3(.003f,.014f,.012f),18,.32f);
                default:return new PoolWaterLook(new V3(.97f,.995f,.991f),new V3(.006f,.03f,.025f),12,.45f);
            }
        }
        // Pool settings stay clear in every biome. Custom values are applied by the adapter unchanged.
        public static PoolWaterLook Look(PoolWaterAppearance appearance,PoolWaterSetting setting)
        {
            if(appearance!=PoolWaterAppearance.ClearCeramic)return Look(appearance);
            switch(setting)
            {
                case PoolWaterSetting.Coast:return new PoolWaterLook(new V3(.95f,.985f,.995f),new V3(.006f,.025f,.04f),12,.5f);
                case PoolWaterSetting.Thermal:return new PoolWaterLook(new V3(.96f,.99f,.98f),new V3(.008f,.035f,.028f),12,.45f);
                case PoolWaterSetting.Aquatic:return new PoolWaterLook(new V3(.98f,.997f,.994f),new V3(.004f,.022f,.018f),16,.45f);
                default:return Look(appearance);
            }
        }
        public const float ClearFallbackOpacity=.07f,ClearNormalStrength=.105f;
        public static WaterBudget Budget(PoolWaterDetail detail)
        {
            switch(detail)
            {
                case PoolWaterDetail.Performance:return new WaterBudget(4,256,128,12,0,false,false);
                case PoolWaterDetail.Cinematic:return new WaterBudget(12,1024,512,24,4,true,true);
                default:return new WaterBudget(8,512,256,18,3,true,true);
            }
        }
        public static WaterRegion Translate(WaterRegion local,V3 offset)
        {local.CenterX+=offset.X;local.CenterZ+=offset.Z;local.Level+=offset.Y;local.Bottom+=offset.Y;return local;}
        // Same attenuation convention as WaterSurface.extinction in HDRP 17.3.
        public static V3 Transmission(PoolWaterLook look,float depth)
        {
            depth=Math.Max(0,depth);float scale=-(float)Math.Log(.02)*depth/look.Absorption;
            return new V3((float)Math.Exp(-scale*Math.Max(1-look.Refraction.X,.01f)),
                (float)Math.Exp(-scale*Math.Max(1-look.Refraction.Y,.01f)),(float)Math.Exp(-scale*Math.Max(1-look.Refraction.Z,.01f)));
        }
    }
    public struct PoolRipple
    {
        public V3 Position;
        public WaterRegion Pool;
        public float Age,Lifetime,Strength,Amplitude;
        public bool Impact;
        public bool Alive { get { return Lifetime>0&&Age<Lifetime; } }
    }
    public sealed class PoolRippleBuffer
    {
        public readonly PoolRipple[] Events=new PoolRipple[PoolWaterModel.MaxRipples];
        private int capacity=8;
        public int Capacity
        {
            get { return capacity; }
            set {int next=Math.Max(1,Math.Min(Events.Length,value));if(next!=capacity){capacity=next;Clear();}}
        }
        public void Clear(){Array.Clear(Events,0,Events.Length);}
        public void ShiftOrigin(V3 shift)
        {
            for(int i=0;i<capacity;i++)if(Events[i].Alive)
            {Events[i].Position+=shift;Events[i].Pool=PoolWaterModel.Translate(Events[i].Pool,shift);}
        }
        public void Advance(float dt)
        {if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;for(int i=0;i<capacity;i++)if(Events[i].Alive)Events[i].Age+=dt;}
        public bool Add(V3 position,WaterRegion pool,float strength,bool impact)
        {
            if(!pool.Contains(position.X,position.Z)||pool.Level<=pool.Bottom)return false;
            strength=CameraPresentation.Unit(strength);if(strength<=0)return false;
            int slot=-1;float oldest=-1;
            for(int i=0;i<capacity;i++)
            {
                if(!Events[i].Alive){slot=i;break;}
                if(!impact&&Events[i].Impact)continue;
                float score=Events[i].Age/Events[i].Lifetime+(Events[i].Impact?0:2);
                if(score>oldest){slot=i;oldest=score;}
            }
            if(slot<0)return false;
            float depth=pool.Level-pool.Bottom;
            Events[slot]=new PoolRipple{Position=position,Pool=pool,Strength=strength,Impact=impact,
                Lifetime=impact?2.8f:1.7f,Amplitude=strength*Math.Min(PoolWaterModel.MaxHeight,depth*.1f)};
            return true;
        }
    }
}
