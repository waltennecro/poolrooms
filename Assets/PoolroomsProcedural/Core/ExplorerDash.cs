using System;

namespace Poolrooms.Core
{
    // Planar burst only: never bypasses the controller, gravity or unloaded-world gates.
    public sealed class ExplorerDash
    {
        public const float Speed=16,Duration=.17f,Recharge=.8f;
        public const int Capacity=3;
        public int Charges { get; private set; }=Capacity;
        public bool Active { get { return remaining>0; } }
        public bool Started { get; private set; }
        private float remaining,recharge;
        private V3 heading;
        public void Cancel(){remaining=0;Started=false;heading=new V3();}
        public void Reset(){Cancel();Charges=Capacity;recharge=0;}
        public V3 Step(float dt,bool pressed,bool allowed,V3 desired,V3 facing)
        {
            Started=false;dt=ExplorerLocomotion.FrameTime(dt);
            if(!allowed){Cancel();return desired;}
            if(dt<=0)return desired;
            if(Charges<Capacity)
            {
                recharge+=dt;
                while(recharge>=Recharge&&Charges<Capacity){recharge-=Recharge;Charges++;}
                if(Charges==Capacity)recharge=0;
            }
            if(pressed&&!Active&&Charges>0)
            {
                heading=Planar(desired);if(heading.Length<.01f)heading=Planar(facing);
                if(heading.Length>.01f){heading=heading.Unit;remaining=Duration;Charges--;Started=true;}
            }
            if(!Active)return desired;
            // Integrate the last fractional step exactly at 10 FPS as well as at 144 FPS.
            float span=Math.Min(remaining,dt);remaining=Math.Max(0,remaining-dt);
            return heading*(Speed*span/dt)+desired*(1-span/dt);
        }
        private static V3 Planar(V3 p)
        {
            if(float.IsNaN(p.X)||float.IsInfinity(p.X)||float.IsNaN(p.Z)||float.IsInfinity(p.Z))return new V3();
            return new V3(Math.Max(-100,Math.Min(100,p.X)),0,Math.Max(-100,Math.Min(100,p.Z)));
        }
    }
}
