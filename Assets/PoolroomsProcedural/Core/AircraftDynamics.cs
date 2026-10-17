using System;

namespace Poolrooms.Core
{
    public readonly struct AircraftForces
    {
        public readonly float Forward,Vertical,Pitch,Roll,YawRate,Authority;
        public AircraftForces(float forward,float vertical,float pitch,float roll,float yawRate,float authority)
        {Forward=forward;Vertical=vertical;Pitch=pitch;Roll=roll;YawRate=yawRate;Authority=authority;}
    }
    // Assisted civil flight: persistent throttle, bounded forces, gradual lift and
    // level/climb assistance. Unity owns collision and integrates its Rigidbody.
    public sealed class AircraftDynamics
    {
        public float Throttle {get;private set;}
        public const float TakeoffSpeed=18,MaximumSpeed=42,MaximumAltitude=250;
        public AircraftForces Step(float dt,float forwardSpeed,float verticalSpeed,float altitude,float throttleInput,float yawInput,float climbInput,bool airbrake)
        {
            if(!Finite(dt)||dt<=0||!Finite(forwardSpeed)||!Finite(verticalSpeed)||!Finite(altitude))return new AircraftForces();
            dt=Math.Min(.05f,dt);throttleInput=UnitAxis(throttleInput);yawInput=UnitAxis(yawInput);climbInput=UnitAxis(climbInput);
            Throttle=Clamp(Throttle+throttleInput*dt*(throttleInput<0?.4f:.28f),0,1);
            float speed=Math.Max(0,forwardSpeed),authority=Clamp(speed/TakeoffSpeed,0,1);authority*=authority;
            float forward=Throttle*5.8f-.0033f*forwardSpeed*Math.Abs(forwardSpeed)-(airbrake?Math.Min(6,speed*.3f):0);
            float ceiling=Clamp((MaximumAltitude-altitude)/25,0,1);if(climbInput>0)climbInput*=ceiling;
            float desiredVertical=climbInput*6;
            float vertical=9.81f*authority+Clamp((desiredVertical-verticalSpeed)*1.6f,-7,7)*authority;
            if(altitude>MaximumAltitude)vertical-=Math.Min(4,(altitude-MaximumAltitude)*.25f);
            return new AircraftForces(Clamp(forward,-10,5.8f),vertical,climbInput*9*authority,-yawInput*12*authority,yawInput*(speed>10?18:7)*Clamp(speed/4,0,1),authority);
        }
        public void Reset(){Throttle=0;}
        static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
        static float UnitAxis(float x){return Finite(x)?Clamp(x,-1,1):0;}
        static float Clamp(float x,float low,float high){return Math.Max(low,Math.Min(high,x));}
    }
}
