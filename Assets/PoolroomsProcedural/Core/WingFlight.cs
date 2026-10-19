using System;

namespace Poolrooms.Core
{
    public enum WingSpeed { Slow,Medium,Fast,VeryFast }
    [Serializable]
    public sealed class WingPreferences
    {
        public bool equipped=true,foldOnGround=true,castShadows=true;
        public int design;
        public WingSpeed speed=WingSpeed.Medium;
        public float size=1,attachmentHeight=.74f,backOffset=.17f;
        public void Sanitize()
        {
            design=Math.Max(0,Math.Min(WingCatalog.Count-1,design));
            if(!Enum.IsDefined(typeof(WingSpeed),speed))speed=WingSpeed.Medium;
            size=GamePreferences.Clamp(size,.6f,1.5f,1);
            attachmentHeight=GamePreferences.Clamp(attachmentHeight,.55f,.85f,.74f);
            backOffset=GamePreferences.Clamp(backOffset,.08f,.4f,.17f);
        }
    }
    // The controller adapter alone moves/collides. No master session or engine dependency.
    public sealed class WingFlight
    {
        public const float MaxMoveStep=.18f;
        public bool Active {get;private set;}
        public V3 Velocity {get;private set;}
        public static float Speed(WingSpeed speed)
        {switch(speed){case WingSpeed.Slow:return 3;case WingSpeed.Fast:return 18;case WingSpeed.VeryFast:return 36;default:return 8;}}
        public bool Toggle(bool allowed)
        {if(Active){Stop();return false;}if(!allowed)return false;Active=true;Velocity=new V3();return true;}
        public void Stop(){Active=false;Suspend();}
        public void Suspend(){Velocity=new V3();}
        public V3 Step(V3 direction,WingSpeed speed,float dt)
        {
            dt=ExplorerLocomotion.FrameTime(dt);
            if(!Active||dt<=0)return new V3();
            if(!HumanoidRig.Finite(direction))direction=new V3();
            if(direction.Length>1)direction=direction.Unit;
            V3 goal=direction*Speed(speed),difference=Velocity-goal;
            const float response=9;
            float decay=(float)Math.Exp(-response*dt);
            V3 displacement=goal*dt+difference*((1-decay)/response);
            Velocity=goal+difference*decay;return displacement;
        }
        public static int Steps(V3 displacement)
        {return HumanoidRig.Finite(displacement)?Math.Max(1,Math.Min(64,(int)Math.Ceiling(displacement.Length/MaxMoveStep))):1;}
    }
}
