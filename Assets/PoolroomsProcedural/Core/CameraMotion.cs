using System;

namespace Poolrooms.Core
{
    public readonly struct CameraMotionFrame
    {
        public readonly V3 Position,Angles,FlashlightPosition,FlashlightAngles;
        public readonly float FieldOfView;
        public CameraMotionFrame(V3 position,V3 angles,V3 flashPosition,V3 flashAngles,float fov)
        {Position=position;Angles=angles;FlashlightPosition=flashPosition;FlashlightAngles=flashAngles;FieldOfView=fov;}
    }
    // Distance-driven gait, not a new random shake every frame. No Unity state or global RNG.
    public sealed class CameraMotionTracker
    {
        private double phase;
        private float envelope,sprintMix,yawLag,pitchLag,strafe;
        public CameraMotionFrame Frame { get; private set; }
        public void Reset(){phase=0;envelope=sprintMix=yawLag=pitchLag=strafe=0;Frame=new CameraMotionFrame();}
        public CameraMotionFrame Step(float speed,float sideSpeed,bool running,bool grounded,bool active,float yawRate,float pitchRate,float intensity,float dt)
        {
            intensity=CameraPresentation.Unit(intensity,.55f);
            if(!active||intensity<=0){Reset();return Frame;}
            if(!Finite(dt)||dt<=0)return Frame; // Pausing freezes the existing pose and phase.
            dt=Math.Min(dt,.1f);speed=Clamp(speed,0,10);sideSpeed=Clamp(sideSpeed,-10,10);
            bool moving=grounded&&speed>.15f;
            float target=moving?Math.Min(1,speed/3.2f):0;
            envelope=Approach(envelope,target,moving?9:12,dt);
            sprintMix=Approach(sprintMix,running&&moving?1:0,7,dt);
            if(moving)phase=(phase+speed*dt*(2*Math.PI)/(3+ .4*sprintMix))%(2*Math.PI);
            yawLag=Approach(yawLag,Clamp(-yawRate*.012f,-2.6f,2.6f),12,dt);
            pitchLag=Approach(pitchLag,Clamp(-pitchRate*.01f,-2,2),12,dt);
            strafe=Approach(strafe,moving?Clamp(sideSpeed/5.5f,-1,1):0,10,dt);
            float wave=(float)Math.Sin(phase),step=(float)Math.Cos(phase*2),amount=intensity*envelope*(.22f+ .78f*sprintMix);
            V3 position=new V3(wave*.018f*amount,step*.027f*amount,0);
            V3 angles=new V3(step*.45f*amount,wave*.12f*amount,-wave*.65f*amount-strafe*.18f*intensity);
            V3 flashPosition=new V3(-position.X*.35f,step*.008f*amount,0);
            V3 flashAngles=new V3(-angles.X*.4f+step*.65f*amount+pitchLag*intensity,
                -angles.Y*.4f+wave*.55f*amount+yawLag*intensity,-angles.Z*.35f);
            Frame=new CameraMotionFrame(position,angles,flashPosition,flashAngles,sprintMix*envelope*1.4f*intensity);
            return Frame;
        }
        private static float Approach(float value,float goal,float rate,float dt){return goal+(value-goal)*(float)Math.Exp(-rate*dt);}
        private static bool Finite(float v){return !float.IsNaN(v)&&!float.IsInfinity(v);}
        private static float Clamp(float v,float min,float max){return Finite(v)?Math.Max(min,Math.Min(max,v)):0;}
    }
    public static class CameraOptics
    {
        public static float SourceScale(float width,float depth,float distance,float fov)
        {
            width=GamePreferences.Clamp(width,0,20,1);depth=GamePreferences.Clamp(depth,0,20,1);
            distance=GamePreferences.Clamp(distance,.1f,200,200);fov=GamePreferences.Clamp(fov,50,105,70);
            float angular=(float)Math.Atan(Math.Sqrt(width*depth)/distance);
            return GamePreferences.Clamp(.7f+angular*4*(70/fov),.65f,1.6f,1);
        }
        public static float WaterGlint(V3 eye,V3 point)
        {
            V3 delta=eye-point;float length=delta.Length;
            if(float.IsNaN(length)||float.IsInfinity(length)||length<=.02f||delta.Y<=0)return 0;
            float cosine=CameraPresentation.Unit(delta.Y/length),grazing=(float)Math.Pow(1-cosine,5);
            return .32f+.68f*(.02f+.98f*grazing);
        }
        public static float Fade(float current,float target,float dt)
        {
            current=CameraPresentation.Unit(current);target=CameraPresentation.Unit(target);
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return current;
            return target+(current-target)*(float)Math.Exp(-(target>current?8:14)*Math.Min(dt,.2f));
        }
    }
}
