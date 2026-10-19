using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum HumanoidRestPose { Auto,T,A,ArmsDown }
    public enum HumanoidEmote { None,Wave,Stretch,Celebrate,Dance,Bow,Sit }
    public enum HumanoidBone { Hips,Spine,Chest,Neck,Head,LeftShoulder,LeftArm,LeftForearm,LeftHand,RightShoulder,RightArm,RightForearm,RightHand,LeftThigh,LeftCalf,LeftFoot,LeftToe,RightThigh,RightCalf,RightFoot,RightToe }
    [Serializable]
    public sealed class SkinPreferences
    {
        public HumanoidRestPose restPose;
        public bool zUp,animate=true,rigidPieces,castShadows=true,materialOverride,previewBindPose,objStatic;
        public float shoulderScale=1,hipHeight=.53f,animationSpeed=1,intensity=1,smoothness=.35f,metallic;
        public HumanoidEmote emote=HumanoidEmote.Wave;
        public void Sanitize()
        {
            if(!Enum.IsDefined(typeof(HumanoidRestPose),restPose))restPose=HumanoidRestPose.Auto;
            if(!Enum.IsDefined(typeof(HumanoidEmote),emote))emote=HumanoidEmote.Wave;
            shoulderScale=GamePreferences.Clamp(shoulderScale,.65f,1.4f,1);hipHeight=GamePreferences.Clamp(hipHeight,.43f,.60f,.53f);
            animationSpeed=GamePreferences.Clamp(animationSpeed,.5f,1.5f,1);intensity=GamePreferences.Clamp(intensity,0,1.25f,1);
            smoothness=GamePreferences.Clamp(smoothness,0,1,.35f);metallic=GamePreferences.Clamp(metallic,0,1,0);
        }
    }
    public readonly struct HumanoidWeight
    {
        public readonly int A,B,C,D;
        public readonly float WA,WB,WC,WD;
        public HumanoidWeight(int a,int b,int c,int d,float x,float y,float z,float w){A=a;B=b;C=c;D=d;WA=x;WB=y;WC=z;WD=w;}
    }
    public sealed class HumanoidRig
    {
        public const int Count=21;
        public static readonly int[] Parents={-1,0,1,2,3,2,5,6,7,2,9,10,11,0,13,14,15,0,17,18,19};
        public static readonly int[] Children={1,2,3,4,-1,6,7,8,-1,10,11,12,-1,14,15,16,-1,18,19,20,-1};
        public readonly V3[] Rest=new V3[Count];
        public readonly float Height;
        public readonly HumanoidRestPose Pose;
        public readonly string Assessment;
        public HumanoidRig(float height,HumanoidRestPose pose,float shoulderScale=1,float hip=.53f,float halfWidth=.48f)
        {
            Height=GamePreferences.Clamp(height,.5f,2.5f,1.75f);Pose=pose==HumanoidRestPose.Auto?HumanoidRestPose.A:pose;
            float h=Height;hip=GamePreferences.Clamp(hip,.43f,.60f,.53f);float shoulder=.13f*GamePreferences.Clamp(shoulderScale,.65f,1.4f,1);
            Rest[0]=new V3(0,hip*h,0);Rest[1]=new V3(0,.65f*h,0);Rest[2]=new V3(0,.76f*h,0);Rest[3]=new V3(0,.85f*h,0);Rest[4]=new V3(0,.93f*h,0);
            float reach=GamePreferences.Clamp(halfWidth,shoulder+.12f,.65f,.48f);
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1:1;int arm=side==0?5:9,leg=side==0?13:17;
                Rest[arm]=new V3(sign*.08f*h,.77f*h,0);Rest[arm+1]=new V3(sign*shoulder*h,.78f*h,0);
                var hand=Pose==HumanoidRestPose.T?new V3(sign*(reach-.035f)*h,.78f*h,0):Pose==HumanoidRestPose.A?new V3(sign*(reach-.02f)*h,.43f*h,0):new V3(sign*(shoulder+.035f)*h,.40f*h,0);
                Rest[arm+3]=hand;Rest[arm+2]=Rest[arm+1]+(hand-Rest[arm+1])*.52f;
                Rest[leg]=new V3(sign*.075f*h,hip*h,0);Rest[leg+1]=new V3(sign*.075f*h,(hip+.055f)*.5f*h,.015f*h);
                Rest[leg+2]=new V3(sign*.075f*h,.055f*h,0);Rest[leg+3]=new V3(sign*.075f*h,.025f*h,.09f*h);
            }
            Assessment="Esqueleto anatómico estimado; revisa hombros, codos y rodillas en tercera persona.";
        }
        public static HumanoidRig Fit(IReadOnlyList<V3> vertices,SkinPreferences preferences)
        {
            preferences.Sanitize();if(vertices==null||vertices.Count<12)throw new FormatException("El modelo necesita geometría humanoide suficiente.");
            float minY=float.MaxValue,maxY=float.MinValue,maxX=0;int leftLeg=0,rightLeg=0,head=0;
            foreach(var p in vertices){if(!Finite(p))throw new FormatException("La skin contiene coordenadas no válidas.");minY=Math.Min(minY,p.Y);maxY=Math.Max(maxY,p.Y);}
            float h=maxY-minY;if(h<.3f||h>3)throw new FormatException("Normaliza la skin verticalmente antes de adaptar el esqueleto.");
            // Quantiles avoid letting one accessory vertex determine the entire arm span.
            var lateral=new List<float>();
            foreach(var p in vertices)
            {
                float y=(p.Y-minY)/h;
                if(y>.38f&&y<.84f)lateral.Add(Math.Abs(p.X)/h);
                if(y>.12f&&y<.4f){if(p.X<-.025f*h)leftLeg++;if(p.X>.025f*h)rightLeg++;}
                if(y>.87f&&Math.Abs(p.X)<.18f*h)head++;
            }
            if(leftLeg<2||rightLeg<2||head<2||lateral.Count<4)throw new FormatException("No se distingue una cabeza y dos piernas. Usa un humanoide de pie, centrado y con eje Y vertical.");
            lateral.Sort();maxX=lateral[Math.Min(lateral.Count-1,(int)(lateral.Count*.98f))];
            float outerHeight=0;int outerCount=0;
            foreach(var p in vertices)if(Math.Abs(p.X)/h>=maxX*.78f&&p.Y/h>.32f&&p.Y/h<.85f){outerHeight+=p.Y/h;outerCount++;}
            outerHeight/=Math.Max(1,outerCount);
            var pose=preferences.restPose==HumanoidRestPose.Auto?(maxX>.24f?(outerHeight>.67f?HumanoidRestPose.T:HumanoidRestPose.A):HumanoidRestPose.ArmsDown):preferences.restPose;
            float width=pose==HumanoidRestPose.ArmsDown?.18f:Math.Max(maxX,.29f);
            return new HumanoidRig(h,pose,preferences.shoulderScale,preferences.hipHeight,width);
        }
        public HumanoidWeight Weight(V3 vertex,bool rigid=false)
        {
            float y=vertex.Y/Height,x=vertex.X/Height;bool left=x<0;int leg=left?13:17,arm=left?5:9;
            // Select anatomical regions before distances. This avoids weighting a hand to an adjacent hip.
            bool armRegion=false;
            if(y>.32f&&y<.86f)
            {
                var shoulder=Rest[arm+1];var hand=Rest[arm+3];
                float t=Math.Max(0,Math.Min(1,(.78f-y)/Math.Max(.01f,.78f-hand.Y/Height)));
                float inner=(Math.Abs(shoulder.X)+(Math.Abs(hand.X)-Math.Abs(shoulder.X))*t)/Height;
                armRegion=Math.Abs(x)>Math.Max(.105f,inner-.045f);
            }
            int first=0,last=4;
            if(y<.105f){first=leg+1;last=leg+3;}
            else if(y<Rest[0].Y/Height-.035f&&!armRegion){first=leg;last=leg+2;}
            else if(armRegion){first=arm;last=arm+3;}
            else if(y>.84f){first=3;last=4;}
            else{first=0;last=3;}
            int a=first,b=first,c=first,d=first;float da=float.MaxValue,db=da,dc=da,dd=da;
            for(int bone=first;bone<=last;bone++)
            {
                int child=Children[bone];V3 end=child>=0?Rest[child]:Rest[bone]+(Rest[bone]-Rest[Parents[bone]])*.3f;
                float distance=SegmentSquared(vertex,Rest[bone],end)+Height*Height*.0001f;
                if(distance<da){d=c;dd=dc;c=b;dc=db;b=a;db=da;a=bone;da=distance;}
                else if(distance<db){d=c;dd=dc;c=b;dc=db;b=bone;db=distance;}
                else if(distance<dc){d=c;dd=dc;c=bone;dc=distance;}
                else if(distance<dd){d=bone;dd=distance;}
            }
            if(rigid)return new HumanoidWeight(a,a,a,a,1,0,0,0);
            float wa=1/(da*da),wb=db==float.MaxValue?0:1/(db*db),wc=dc==float.MaxValue?0:1/(dc*dc),wd=dd==float.MaxValue?0:1/(dd*dd),sum=wa+wb+wc+wd;
            return new HumanoidWeight(a,b,c,d,wa/sum,wb/sum,wc/sum,wd/sum);
        }
        static float SegmentSquared(V3 p,V3 a,V3 b){V3 d=b-a;float length=V3.Dot(d,d),t=length>1e-10f?Math.Max(0,Math.Min(1,V3.Dot(p-a,d)/length)):0;d=p-(a+d*t);return V3.Dot(d,d);}
        public static bool Finite(V3 v){return float.IsFinite(v.X)&&float.IsFinite(v.Y)&&float.IsFinite(v.Z);}
    }
}
