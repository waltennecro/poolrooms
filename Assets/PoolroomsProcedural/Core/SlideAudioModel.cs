using System;

namespace Poolrooms.Core
{
    public enum SlideContactSound { Tile,Metal,Soft,Wet }

    // Actual travel controls a continuous friction bed. No step cadence or impact pulses.
    public sealed class SlideAudioEnvelope
    {
        public float Gain { get; private set; }
        public float Pitch { get; private set; }=1;
        public void Reset(){Gain=0;Pitch=1;}
        public void Step(float dt,float distance,bool sliding,bool grounded,bool swimming,bool flying)
        {
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
            if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<0||distance>dt*25)
            {Reset();return;}
            float speed=distance/dt;
            float target=sliding&&grounded&&!swimming&&!flying&&speed>.25f?
                .30f*(float)Math.Pow(Math.Min(1,speed/12),.7):0;
            Gain+=(target-Gain)*(1-(float)Math.Exp(-dt*(target>Gain?18:24)));
            Pitch=.85f+.25f*Math.Min(1,speed/18);
            if(target==0&&Gain<.0001f)Gain=0;
        }
    }
}
