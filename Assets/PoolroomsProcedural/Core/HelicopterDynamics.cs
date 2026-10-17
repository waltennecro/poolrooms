using System;

namespace Poolrooms.Core
{
    // Exploration assistance, not a certified rotorcraft simulation. Acceleration is
    // integrated by the native Rigidbody; no transform movement or damage logic.
    public sealed class HelicopterDynamics
    {
        public float Rotor {get;private set;}
        public const float MaximumSpeed=18,MaximumAltitude=250;
        public AircraftForces Step(float dt,float forwardSpeed,float verticalSpeed,float altitude,float forwardInput,float yawInput,float climbInput,bool brake)
        {
            if(!Finite(dt)||dt<=0||!Finite(forwardSpeed)||!Finite(verticalSpeed)||!Finite(altitude))return new AircraftForces();
            dt=Math.Min(dt,.05f);forwardInput=Axis(forwardInput);yawInput=Axis(yawInput);climbInput=Axis(climbInput);
            Rotor+=(1-Rotor)*(1-(float)Math.Exp(-dt/.8f));float authority=Rotor*Rotor;
            if(climbInput>0)climbInput*=Clamp((MaximumAltitude-altitude)/25,0,1);
            float vertical=9.81f*authority+Clamp((climbInput*(climbInput>0?5:3)-verticalSpeed)*2,-6,6)*authority;
            if(altitude>MaximumAltitude)vertical-=Math.Min(4,(altitude-MaximumAltitude)*.25f);
            float acceleration=Clamp(((brake?0:forwardInput*MaximumSpeed)-forwardSpeed)*1.4f,-5,5)*authority;
            return new AircraftForces(acceleration,vertical,(brake?0:forwardInput)*11*authority,-yawInput*5*authority,yawInput*35*authority,authority);
        }
        public void Reset(){Rotor=0;}
        static float Clamp(float x,float lo,float hi){return Math.Max(lo,Math.Min(hi,x));}
        static bool Finite(float x){return !float.IsNaN(x)&&!float.IsInfinity(x);}
        static float Axis(float x){return Finite(x)?Clamp(x,-1,1):0;}
    }
}
