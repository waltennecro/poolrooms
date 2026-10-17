using System;

namespace Poolrooms.Core
{
    public enum PoolWaterAppearance { ClearCeramic, SoftTurquoise, BluePorcelain, Custom }
    public enum PoolWaterDetail { Performance, High, Cinematic }
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
        public static PoolWaterLook Look(PoolWaterAppearance appearance)
        {
            switch(appearance)
            {
                case PoolWaterAppearance.SoftTurquoise:return new PoolWaterLook(new V3(.79f,.97f,.94f),new V3(.025f,.18f,.16f),5.5f,.8f);
                case PoolWaterAppearance.BluePorcelain:return new PoolWaterLook(new V3(.78f,.93f,.99f),new V3(.02f,.095f,.18f),5,.7f);
                default:return new PoolWaterLook(new V3(.9f,.985f,.975f),new V3(.025f,.13f,.115f),7,.65f);
            }
        }
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
