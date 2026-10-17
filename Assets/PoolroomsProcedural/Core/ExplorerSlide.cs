using System;

namespace Poolrooms.Core
{
    // World-space planar momentum only: jumping, gravity and collisions keep their owners.
    public sealed class ExplorerSlide
    {
        public const float DefaultSpeed=11,MinSpeed=8,MaxSpeed=18,TurnRate=2.4f,AirDrag=1.5f;
        public bool Sliding { get; private set; }
        public bool HasMomentum { get; private set; }
        public bool RequiresRelease { get; private set; }
        public V3 Velocity { get; private set; }
        public float Speed { get { return Velocity.Length; } }
        public void Stop(){Sliding=HasMomentum=false;Velocity=new V3();RequiresRelease=true;}
        public V3 Step(float dt,bool held,bool grounded,bool swimming,bool jump,V3 desired,V3 facing,float speed,bool allowed=true)
        {
            dt=ExplorerLocomotion.FrameTime(dt);desired=Planar(desired);facing=Planar(facing);
            if(!held)RequiresRelease=false;
            if(!allowed||swimming){Stop();return desired;}
            if(dt<=0)return HasMomentum?Velocity:desired;
            speed=GamePreferences.Clamp(speed,MinSpeed,MaxSpeed,DefaultSpeed);
            if(jump&&Sliding){Sliding=false;RequiresRelease=true;}
            if(grounded&&!jump)
            {
                if(!held||RequiresRelease){Sliding=HasMomentum=false;Velocity=new V3();return desired;}
                V3 direction=desired.Length>.05f?Heading(desired):HasMomentum?Heading(Velocity):Heading(facing);
                if(direction.Length<.5f)return desired;
                Velocity=HasMomentum?Turn(Velocity,direction,TurnRate*dt)*speed:direction*speed;
                Sliding=HasMomentum=true;return Velocity;
            }
            Sliding=false;
            if(!HasMomentum)return desired; // Holding Ctrl in the air cannot manufacture speed.
            float remaining=Math.Max(desired.Length,Speed-AirDrag*dt);
            if(remaining<.0001f){HasMomentum=false;Velocity=new V3();return desired;}
            V3 heading=desired.Length>.05f?Turn(Velocity,Heading(desired),TurnRate*.35f*dt):Heading(Velocity);
            Velocity=heading*remaining;return Velocity;
        }
        private static V3 Heading(V3 v){return v.Length>.0001f?v.Unit:new V3();}
        private static V3 Planar(V3 v)
        {
            if(float.IsNaN(v.X)||float.IsInfinity(v.X)||float.IsNaN(v.Z)||float.IsInfinity(v.Z))return new V3();
            var flat=new V3(Math.Max(-100,Math.Min(100,v.X)),0,Math.Max(-100,Math.Min(100,v.Z)));
            return flat.Length>MaxSpeed?flat.Unit*MaxSpeed:flat;
        }
        private static V3 Turn(V3 velocity,V3 target,float maxAngle)
        {
            float a=(float)Math.Atan2(velocity.X,velocity.Z),b=(float)Math.Atan2(target.X,target.Z);
            float delta=(float)Math.Atan2(Math.Sin(b-a),Math.Cos(b-a));
            a+=Math.Max(-maxAngle,Math.Min(maxAngle,delta));return new V3((float)Math.Sin(a),0,(float)Math.Cos(a));
        }
        public static float LowHeight(float standing,float radius,float skin)
        {return Math.Min(standing,Math.Max(.95f,2*radius+2*skin+.02f));}
    }
}
