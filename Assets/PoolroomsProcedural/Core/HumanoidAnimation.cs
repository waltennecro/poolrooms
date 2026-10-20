using System;

namespace Poolrooms.Core
{
    public enum HumanoidGesture { None,Reach,Carry,Read,Drink,Eat,Water,Adjust,PlayKeys,Place,Clean,Photograph,OpenDoor }
    public struct HumanoidMotion
    {
        public V3 Velocity;
        public bool Grounded,Sprint,Dash,Slide,Swimming,Seated,Flying,Carrying;
        public HumanoidGesture Gesture;
        public float GestureProgress;
        public float LookPitch,LeftGround,RightGround;
    }
    // One shared target-pose generator for fitted meshes and existing Humanoid rigs.
    // It never changes the physical player, allocates per frame or advances while paused.
    public sealed class HumanoidAnimation
    {
        public readonly V3[] Joints=new V3[HumanoidRig.Count];
        public HumanoidEmote Emote {get;private set;}
        public float EmoteTime {get;private set;}
        public string State {get;private set;}="Descanso";
        float time,phase,speed,crouch,lean,air,slidePose,seatedPose,swimPose,flyPose,dashPose;
        public static float Duration(HumanoidEmote emote){switch(emote){case HumanoidEmote.Wave:return 3;case HumanoidEmote.Stretch:return 4;case HumanoidEmote.Celebrate:return 3.5f;case HumanoidEmote.Dance:return 6;case HumanoidEmote.Bow:return 3;case HumanoidEmote.Sit:return 8;default:return 0;}}
        public bool Play(HumanoidEmote value,bool grounded)
        {if(!grounded||value==HumanoidEmote.None||!Enum.IsDefined(typeof(HumanoidEmote),value))return false;Emote=value;EmoteTime=0;return true;}
        public void Stop(){Emote=HumanoidEmote.None;EmoteTime=0;}
        public void Step(HumanoidRig rig,HumanoidMotion input,float delta,float intensity=1,float rate=1)
        {
            delta=GamePreferences.Clamp(delta,0,.1f,0);if(delta<=0)return;
            intensity=GamePreferences.Clamp(intensity,0,1.25f,1);rate=GamePreferences.Clamp(rate,.5f,1.5f,1);
            float horizontal=(float)Math.Sqrt(input.Velocity.X*input.Velocity.X+input.Velocity.Z*input.Velocity.Z);if(!float.IsFinite(horizontal))horizontal=0;
            if(Emote!=HumanoidEmote.None)
            {if(horizontal>.35f||!input.Grounded||input.Dash||input.Slide||input.Seated){Stop();}else{EmoteTime+=delta*rate;if(EmoteTime>=Duration(Emote))Stop();}}
            float emoteBlend=Emote==HumanoidEmote.None?0:Math.Min(1,Math.Min(EmoteTime/.35f,(Duration(Emote)-EmoteTime)/.35f));
            time+=delta*rate;float targetCrouch=input.Slide?.48f:input.Seated?.42f:Emote==HumanoidEmote.Sit?.42f*emoteBlend:0;
            float blend=1-(float)Math.Exp(-delta*12);speed+=(horizontal-speed)*blend;crouch+=(targetCrouch-crouch)*blend;
            slidePose+=((input.Slide?1:0)-slidePose)*blend;seatedPose+=((input.Seated?1:Emote==HumanoidEmote.Sit?emoteBlend:0)-seatedPose)*blend;
            swimPose+=((input.Swimming?1:0)-swimPose)*blend;flyPose+=((input.Flying?1:0)-flyPose)*blend;dashPose+=((input.Dash?1:0)-dashPose)*blend;
            float targetLean=input.Slide?.65f:input.Dash?.40f:input.Sprint?.16f:Math.Min(horizontal*.018f,.09f);
            if(Emote==HumanoidEmote.Bow)targetLean+=(float)Math.Sin(Math.PI*EmoteTime/Duration(Emote))*.65f;
            lean+=(targetLean-lean)*blend;air+=((input.Grounded?0:1)-air)*blend;
            float h=rig.Height,stride=(input.Sprint?.17f:.12f)*h*intensity;
            if(input.Grounded&&!input.Slide&&!input.Seated&&horizontal>.1f)phase=(phase+delta*horizontal/Math.Max(.12f,4*stride)*rate)%1;
            V3 travel=horizontal>.001f?new V3(input.Velocity.X/horizontal,0,input.Velocity.Z/horizontal):new V3(0,0,1);
            float activity=Math.Min(1,speed/.8f),breath=(float)Math.Sin(time*1.8f)*.0017f*h*intensity;
            var hip=rig.Rest[0];hip.Y-=h*(crouch+Math.Min(speed*.006f,.035f));hip.Y+=breath;
            if(Emote==HumanoidEmote.Dance)hip.X+=(float)Math.Sin(time*4)*.035f*h*emoteBlend;
            Joints[0]=hip;Joints[1]=hip+(rig.Rest[1]-rig.Rest[0])+new V3(0,0,lean*.12f*h);
            Joints[2]=Joints[1]+(rig.Rest[2]-rig.Rest[1])+new V3(0,-(rig.Rest[2].Y-rig.Rest[1].Y)*lean*.15f,lean*.13f*h);
            Joints[3]=Joints[2]+(rig.Rest[3]-rig.Rest[2])+new V3(0,-(rig.Rest[3].Y-rig.Rest[2].Y)*lean*.15f,lean*.05f*h);
            Joints[4]=Joints[3]+(rig.Rest[4]-rig.Rest[3])+new V3(0,0,(float)Math.Sin(GamePreferences.Clamp(input.LookPitch,-70,70,0)*Math.PI/180)*.025f*h);
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;int leg=side==0?13:17,arm=side==0?5:9;
                Joints[leg]=hip+(rig.Rest[leg]-rig.Rest[0]);
                float p=(phase+side*.5f)%1,z,y;
                if(p<.5f){z=stride*(1-p*4);y=0;}else{float t=(p-.5f)*2;z=stride*(-1+2*t*t*(3-2*t));y=(float)Math.Sin(t*Math.PI)*(input.Sprint?.10f:.065f)*h*intensity;}
                z*=activity;y*=activity;
                float ground=GamePreferences.Clamp(side==0?input.LeftGround:input.RightGround,-.22f*h,.22f*h,0);
                V3 ankle=rig.Rest[leg+2]+new V3(travel.X*z,y+ground,travel.Z*z);
                ankle+=(rig.Rest[leg+2]+new V3(sign*.015f*h,.01f*h,.38f*h)-ankle)*slidePose;
                ankle+=(rig.Rest[leg+2]+new V3(sign*.015f*h,.01f*h,.28f*h)-ankle)*seatedPose;
                var airFoot=rig.Rest[leg+2]+new V3(0,.09f*h+Math.Min(.08f,Math.Max(0,input.Velocity.Y*.014f))*h,sign*.045f*h);
                ankle+=(airFoot-ankle)*air*(1-seatedPose)*(1-slidePose);
                var swimFoot=Joints[leg]+new V3(sign*.015f*h,-.34f*h,.08f*h+(float)Math.Sin(time*4+side*Math.PI)*.075f*h);
                ankle+=(swimFoot-ankle)*swimPose;
                ankle+=(Joints[leg]+new V3(sign*.015f*h,-.42f*h,-.035f*h)-ankle)*flyPose;
                Solve(Joints[leg],ankle,(rig.Rest[leg+1]-rig.Rest[leg]).Length,(rig.Rest[leg+2]-rig.Rest[leg+1]).Length,new V3(0,0,1),out Joints[leg+1],out Joints[leg+2]);
                Joints[leg+3]=Joints[leg+2]+(rig.Rest[leg+3]-rig.Rest[leg+2]);
                Joints[arm]=Joints[2]+(rig.Rest[arm]-rig.Rest[2]);Joints[arm+1]=Joints[2]+(rig.Rest[arm+1]-rig.Rest[2]);
                float armLength=(rig.Rest[arm+2]-rig.Rest[arm+1]).Length+(rig.Rest[arm+3]-rig.Rest[arm+2]).Length;
                V3 hand=Joints[arm+1]+new V3(sign*.045f*h,-armLength*.94f,-z*.65f);
                if(input.Sprint)hand.Z+=.06f*h;
                hand+=new V3(sign*.02f*h,.09f*h,-.14f*h)*dashPose;
                hand+=(Joints[arm+1]+new V3(sign*.045f*h,-.15f*h,.18f*h)-hand)*Math.Max(slidePose,seatedPose);
                if(!input.Grounded&&!input.Seated)hand.Y+=.07f*h*air;
                hand+=(Joints[arm+1]+new V3(sign*.12f*h,(float)Math.Cos(time*2.5f+side*Math.PI)*.09f*h,.16f*h+(float)Math.Sin(time*2.5f+side*Math.PI)*.10f*h)-hand)*swimPose;
                hand+=(Joints[arm+1]+new V3(sign*.10f*h,-armLength*.80f,-.08f*h)-hand)*flyPose;
                if(input.Carrying&&!input.Swimming&&!input.Seated)
                    hand=Joints[arm+1]+new V3(sign*.06f*h,-.15f*h,.24f*h);
                if(input.Gesture!=HumanoidGesture.None&&!input.Dash&&!input.Slide)
                {
                    float progress=GamePreferences.Clamp(input.GestureProgress,0,1,0);
                    float action=(float)Math.Sin(Math.PI*progress);action*=action;
                    V3 target=Joints[arm+1]+new V3(sign*.035f*h,-.1f*h,.26f*h);
                    switch(input.Gesture)
                    {
                        case HumanoidGesture.Read:target=Joints[arm+1]+new V3(sign*.045f*h,-.12f*h,.21f*h);break;
                        case HumanoidGesture.Drink:case HumanoidGesture.Eat:if(side==1)target=Joints[4]+new V3(.035f*h,-.075f*h,.045f*h);break;
                        case HumanoidGesture.Water:target=Joints[arm+1]+new V3(sign*.045f*h,-.16f*h,.33f*h);break;
                        case HumanoidGesture.Clean:target=Joints[arm+1]+new V3(sign*.08f*h+(float)Math.Sin(progress*Math.PI*6)*.09f*h,-.1f*h,.25f*h);break;
                        case HumanoidGesture.PlayKeys:target=Joints[arm+1]+new V3(sign*.09f*h,-.2f*h,.29f*h+(float)Math.Sin(progress*Math.PI*8+side)*.012f*h);break;
                        case HumanoidGesture.Photograph:target=Joints[4]+new V3(sign*.06f*h,-.035f*h,.09f*h);break;
                        case HumanoidGesture.OpenDoor:case HumanoidGesture.Adjust:target=Joints[arm+1]+new V3(sign*.08f*h,-.06f*h,.28f*h);break;
                        case HumanoidGesture.Place:target=Joints[arm+1]+new V3(sign*.04f*h,-.32f*h,.21f*h);break;
                    }
                    hand+=(target-hand)*action;
                }
                V3 emoteHand=hand;
                switch(Emote)
                {
                    case HumanoidEmote.Wave:if(side==1)emoteHand=Joints[arm+1]+new V3(.16f*h,.18f*h,(float)Math.Sin(time*9)*.055f*h);break;
                    case HumanoidEmote.Stretch:emoteHand=Joints[arm+1]+new V3(sign*.10f*h,armLength*.86f,.02f*h);break;
                    case HumanoidEmote.Celebrate:emoteHand=Joints[arm+1]+new V3(sign*.20f*h,.18f*h+(float)Math.Sin(time*8)*.025f*h,.04f*h);break;
                    case HumanoidEmote.Dance:emoteHand=Joints[arm+1]+new V3(sign*.15f*h,(float)Math.Sin(time*4+side*Math.PI)*.12f*h,.18f*h);break;
                }
                hand+=(emoteHand-hand)*emoteBlend;
                Solve(Joints[arm+1],hand,(rig.Rest[arm+2]-rig.Rest[arm+1]).Length,(rig.Rest[arm+3]-rig.Rest[arm+2]).Length,new V3(sign*.3f,0,-1),out Joints[arm+2],out Joints[arm+3]);
            }
            State=Emote!=HumanoidEmote.None?EmoteLabel(Emote):input.Seated?"Sentado":input.Swimming?"Nadar":input.Flying?"Vuelo":input.Slide?"Deslizamiento":input.Dash?"Dash":!input.Grounded?(input.Velocity.Y>0?"Salto":"Caída"):horizontal>.1f?(input.Sprint?"Correr":"Caminar"):"Descanso";
        }
        static string EmoteLabel(HumanoidEmote emote)
        {switch(emote){case HumanoidEmote.Wave:return "Saludar";case HumanoidEmote.Stretch:return "Estirarse";case HumanoidEmote.Celebrate:return "Celebrar";case HumanoidEmote.Dance:return "Bailar";case HumanoidEmote.Bow:return "Reverencia";case HumanoidEmote.Sit:return "Sentarse";default:return "Descanso";}}
        public static void Solve(V3 origin,V3 target,float upper,float lower,V3 pole,out V3 joint,out V3 end)
        {
            upper=Math.Max(.0001f,upper);lower=Math.Max(.0001f,lower);V3 delta=target-origin;float raw=delta.Length;
            V3 direction=raw>.00001f?delta*(1/raw):new V3(0,-1,0);
            float distance=Math.Max(Math.Abs(upper-lower)+.00001f,Math.Min(upper+lower-.00001f,raw));
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance),height=(float)Math.Sqrt(Math.Max(0,upper*upper-along*along));
            V3 bend=pole-direction*V3.Dot(pole,direction);if(bend.Length<.00001f)bend=V3.Cross(direction,new V3(1,0,0));if(bend.Length<.00001f)bend=V3.Cross(direction,new V3(0,0,1));
            joint=origin+direction*along+bend.Unit*height;end=origin+direction*distance;
        }
    }
}
