using System;

namespace Poolrooms.Core
{
    public enum TravelPace { Normal, Agile, Express }

    [Serializable]
    public sealed class TravelPreferences
    {
        public TravelPace pace;
        public bool allowAutoAdvance=true,allowGrapple=true,allowFastSwim=true;
        public void Sanitize()
        {if((int)pace<0||(int)pace>2)pace=TravelPace.Normal;}
    }

    // Deliberately independent of world generation and of the master-mode state.
    public sealed class ExplorerTravel
    {
        public const float AgileSpeed=8,ExpressSpeed=12,SwimSpeed=5.2f,MaxMoveStep=.18f;
        public bool AutoAdvance {get;private set;}
        private float speed,blockedTime;
        public void Stop(){AutoAdvance=false;speed=blockedTime=0;}
        public void StopAuto(){AutoAdvance=false;blockedTime=0;}
        public V2 ResolveInput(V2 input,bool toggle,bool allowed)
        {
            float x=Finite(input.X)?Math.Max(-1,Math.Min(1,input.X)):0;
            float y=Finite(input.Y)?Math.Max(-1,Math.Min(1,input.Y)):0;
            if(!allowed)StopAuto();else if(toggle){AutoAdvance=!AutoAdvance;blockedTime=0;}
            if(y<-.1f)StopAuto();
            if(AutoAdvance)y=1;
            float length=(float)Math.Sqrt(x*x+y*y);
            return length>1?new V2(x/length,y/length):new V2(x,y);
        }
        public float StepSpeed(float dt,TravelPace pace,bool moving,bool forward,bool sprint,
            bool swimming,bool fastSwim,float walk,float run)
        {
            dt=ExplorerLocomotion.FrameTime(dt);
            walk=Bound(walk,1.5f,5,3.2f);run=Bound(run,walk,8,Math.Max(walk,5.5f));
            bool travel=(pace==TravelPace.Agile||pace==TravelPace.Express)&&forward;
            float target=swimming?(fastSwim?SwimSpeed:2.1f):travel?(pace==TravelPace.Agile?AgileSpeed:ExpressSpeed):(sprint||AutoAdvance?run:walk);
            // Normal pace remains the original locomotion, including its instant response.
            if(!travel&&!(swimming&&fastSwim)&&!AutoAdvance){speed=target;return target;}
            if(!moving){speed=0;return 0;}
            if(dt<=0)return 0;
            float initial=Math.Max(0,Math.Min(target,speed)),ramp=Math.Min(dt,(target-initial)/18);
            float distance=initial*ramp+9*ramp*ramp+target*(dt-ramp);
            speed=Math.Min(target,initial+18*dt);return distance/dt;
        }
        public void ReportMovement(float expected,float actual,float dt)
        {
            if(!AutoAdvance)return;
            if(!Finite(expected)||!Finite(actual)||expected<.001f){blockedTime=0;return;}
            blockedTime=actual<expected*.15f?blockedTime+ExplorerLocomotion.FrameTime(dt):0;
            if(blockedTime>=.16f)StopAuto();
        }
        public static int StepsFor(V3 displacement)
        {
            if(!Finite(displacement.X)||!Finite(displacement.Y)||!Finite(displacement.Z))return 1;
            return Math.Max(1,Math.Min(64,(int)Math.Ceiling(displacement.Length/MaxMoveStep)));
        }
        public static int MovementSteps(float dt)
        {return StepsFor(new V3(28*ExplorerLocomotion.FrameTime(dt),0,0));}
        private static float Bound(float x,float min,float max,float fallback)
        {return !Finite(x)?fallback:Math.Max(min,Math.Min(max,x));}
        private static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
    }

    // Controlled pull, not a teleport, pendulum simulation or source of extra jumps.
    public sealed class GrapplePull
    {
        public const float Range=28,StopDistance=1.3f,Speed=15,MaxSeconds=4;
        public bool Active {get;private set;}
        private float speed,age,blocked;
        public bool Begin(float distance)
        {
            Stop();
            if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<2.5f||distance>Range)return false;
            Active=true;return true;
        }
        public void Stop(){Active=false;speed=age=blocked=0;}
        public V3 Step(V3 offset,float dt)
        {
            dt=ExplorerLocomotion.FrameTime(dt);
            if(!Active||dt<=0)return new V3();
            if(!HumanoidRig.Finite(offset)||offset.Length>Range+3){Stop();return new V3();}
            float distance=offset.Length;
            if(distance<=StopDistance||age+dt>MaxSeconds){Stop();return new V3();}
            age+=dt;float ramp=Math.Min(dt,(Speed-speed)/24);
            float covered=speed*ramp+12*ramp*ramp+Speed*(dt-ramp);
            speed=Math.Min(Speed,speed+24*dt);float travel=Math.Min(distance-StopDistance,covered);
            return offset*(travel/distance);
        }
        public void ReportMovement(float expected,float actual,float dt)
        {
            if(!Active)return;
            blocked=expected>.001f&&actual<expected*.15f?blocked+ExplorerLocomotion.FrameTime(dt):0;
            if(blocked>=.12f)Stop();
        }
    }
}
