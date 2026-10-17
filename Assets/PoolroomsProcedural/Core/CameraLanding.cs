using System;

namespace Poolrooms.Core
{
    // Camera-only contact response: exact critically damped integration remains stable
    // at low FPS. Never feeds the flashlight, locomotion, collision or health systems.
    public sealed class CameraLanding
    {
        private bool initialized,grounded;
        private float peakFall,offset,velocity;
        public float Offset { get { return offset; } }
        public void Reset(){initialized=false;grounded=false;peakFall=offset=velocity=0;}
        public float Step(bool onGround,float verticalSpeed,bool enabled,float intensity,float deltaTime)
        {
            if(!enabled){Reset();return 0;}
            if(float.IsNaN(verticalSpeed)||float.IsInfinity(verticalSpeed)||float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)||deltaTime<0){Reset();return 0;}
            if(deltaTime==0)return offset;
            if(!initialized){initialized=true;grounded=onGround;}
            if(!onGround)peakFall=Math.Max(peakFall,Math.Max(0,-verticalSpeed));
            if(onGround&&!grounded)
            {
                offset-=Math.Min(.055f,Math.Max(0,peakFall-2)*.005f)*CameraPresentation.Unit(intensity);
                offset=Math.Max(-.075f,offset);peakFall=0;
            }
            grounded=onGround;
            if(onGround)peakFall=0;
            const float frequency=13;
            float t=Math.Min(deltaTime,.5f),decay=(float)Math.Exp(-frequency*t),term=velocity+frequency*offset;
            velocity=(velocity-frequency*term*t)*decay;offset=(offset+term*t)*decay;
            return offset;
        }
    }
}
