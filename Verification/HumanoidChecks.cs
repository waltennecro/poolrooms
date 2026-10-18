using System;
using System.Collections.Generic;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static List<V3> HumanoidPoints(HumanoidRig rig)
    {
        var result=new List<V3>();for(int i=0;i<rig.Rest.Length;i++)
        {
            var start=rig.Rest[i];int child=HumanoidRig.Children[i];var end=child<0?start:rig.Rest[child];
            for(int step=0;step<5;step++)for(int corner=0;corner<8;corner++)
            {
                float radius=rig.Height*(i==4?.045f:.018f);
                result.Add(start+(end-start)*(step/4f)+new V3((corner&1)==0?-radius:radius,(corner&2)==0?-radius:radius,(corner&4)==0?-radius:radius));
            }
        }
        result.Add(new V3(-.075f*rig.Height,0,0));result.Add(new V3(.075f*rig.Height,rig.Height,0));return result;
    }
    static void HumanoidSkins()
    {
        var settings=new SkinPreferences{shoulderScale=float.NaN,hipHeight=20,intensity=float.PositiveInfinity,animationSpeed=-3,emote=(HumanoidEmote)900,restPose=(HumanoidRestPose)900};settings.Sanitize();
        Check(settings.shoulderScale==1&&settings.hipHeight==.6f&&settings.intensity==1&&settings.animationSpeed==.5f,"Corrupt skin settings must be repaired");
        Check(settings.emote==HumanoidEmote.Wave&&settings.restPose==HumanoidRestPose.Auto,"Invalid skin enum was not repaired");
        Check((int)SettingsCategory.Comfort==5&&(int)SettingsCategory.Skins==6,"Existing settings category IDs changed");
        var preferences=new GamePreferences{exposureEV=9,walkSpeed=4};preferences.Reset(SettingsCategory.Skins);
        Check(preferences.exposureEV==9&&preferences.walkSpeed==4&&GamePreferences.ChangesFor(SettingsCategory.Skins)==SettingsChange.None,"Skin reset changed unrelated gameplay settings");
        foreach(float height in new[]{.5f,1.2f,1.75f,2.5f})foreach(var pose in new[]{HumanoidRestPose.T,HumanoidRestPose.A,HumanoidRestPose.ArmsDown})foreach(float shoulders in new[]{.65f,1f,1.4f})
        {
            var original=new HumanoidRig(height,pose,shoulders);var points=HumanoidPoints(original);var fitted=HumanoidRig.Fit(points,new SkinPreferences{restPose=pose,shoulderScale=shoulders});
            Check(HumanoidRig.Fit(points,new SkinPreferences()).Pose==pose,"Automatic rest-pose classification failed");
            Check(Math.Abs(fitted.Height-height)<.06f*height,"Humanoid fitting changed normalized height");
            foreach(var point in points)
            {
                var w=fitted.Weight(point);float total=w.WA+w.WB+w.WC+w.WD;
                Check(float.IsFinite(total)&&Math.Abs(total-1)<.000001f,"Skin weights do not sum to one");
                Check(new[]{w.A,w.B,w.C,w.D}.All(i=>i>=0&&i<HumanoidRig.Count),"Skin bone index outside skeleton");
                Check(new[]{w.WA,w.WB,w.WC,w.WD}.All(v=>v>=0&&v<=1),"Negative or invalid skin influence");
                var rigid=fitted.Weight(point,true);Check(rigid.WA==1&&rigid.WB==0&&rigid.WC==0&&rigid.WD==0,"Rigid part must follow exactly one bone");
            }
            foreach(int bone in new[]{14,15,18,19})
            {
                var w=original.Weight(original.Rest[bone]);bool left=bone<17;
                Check((left?w.A>=13&&w.A<=16:w.A>=17&&w.A<=20),"Leg vertex weighted to opposite limb or torso");
            }
            foreach(int bone in new[]{8,12}){var w=original.Weight(original.Rest[bone]);Check(bone==8?w.A>=5&&w.A<=8:w.A>=9&&w.A<=12,"Hand weighted to hip/torso");}
            foreach(int fps in new[]{10,20,60,144})
            {
                var animation=new HumanoidAnimation();var same=new HumanoidAnimation();
                for(int frame=0;frame<fps*6;frame++)
                {
                    int state=frame/fps;var input=new HumanoidMotion{Grounded=state<2||state==4,Velocity=new V3(.6f,state==2?3:state==3?-4:0,state==0?0:state==1?4:7),Sprint=state==1,Dash=state==2,Slide=state==4,Swimming=state==5,LookPitch=20,LeftGround=.08f,RightGround=-.04f};
                    animation.Step(original,input,1f/fps);same.Step(original,input,1f/fps);
                    for(int i=0;i<HumanoidRig.Count;i++){Check(HumanoidRig.Finite(animation.Joints[i]),"Nonfinite animated joint");Check((animation.Joints[i]-same.Joints[i]).Length<1e-7f,"Animation isn't deterministic");}
                    foreach(int limb in new[]{6,10,13,17})
                    {
                        Check(Math.Abs((animation.Joints[limb+1]-animation.Joints[limb]).Length-(original.Rest[limb+1]-original.Rest[limb]).Length)<height*.0001f,"Upper limb length changed");
                        Check(Math.Abs((animation.Joints[limb+2]-animation.Joints[limb+1]).Length-(original.Rest[limb+2]-original.Rest[limb+1]).Length)<height*.0001f,"Lower limb length changed");
                    }
                }
                var before=(V3[])animation.Joints.Clone();animation.Step(original,new HumanoidMotion(),0);Check(before.SequenceEqual(animation.Joints),"Paused pose advanced");
                foreach(var emote in Enum.GetValues<HumanoidEmote>().Where(e=>e!=HumanoidEmote.None))
                {
                    Check(!animation.Play(emote,false)&&animation.Play(emote,true),"Emote grounding contract broken");
                    for(int i=0;i<fps*HumanoidAnimation.Duration(emote)+2;i++){animation.Step(original,new HumanoidMotion{Grounded=true},1f/fps);Check(animation.Joints.All(HumanoidRig.Finite),"Emote emitted invalid joint");}
                    Check(animation.Emote==HumanoidEmote.None,"Emote did not finish");
                    animation.Play(emote,true);animation.Step(original,new HumanoidMotion{Grounded=true,Velocity=new V3(1,0,0)},.02f);Check(animation.Emote==HumanoidEmote.None,"Walking doesn't cancel emote");
                }
            }
        }
        foreach(var target in new[]{new V3(),new V3(0,-1,0),new V3(0,999,0),new V3(1,0,0)})foreach(float a in new[]{.0001f,.2f,.5f})foreach(float b in new[]{.0001f,.25f,.7f})
        {
            HumanoidAnimation.Solve(new V3(),target,a,b,V3.Up,out var knee,out var end);
            Check(HumanoidRig.Finite(knee)&&HumanoidRig.Finite(end),"IK singularity");Check(Math.Abs(knee.Length-a)<.0002f,"IK upper length");Check(Math.Abs((end-knee).Length-b)<.0002f,"IK lower length");
        }
        // Imported Humanoids can have off-centre hips and a nonvertical rest spine.
        var centred=new HumanoidRig(1.75f,HumanoidRestPose.A);var offsetRig=new HumanoidRig(1.75f,HumanoidRestPose.A);var offset=new V3(.21f,0,-.14f);
        for(int i=0;i<HumanoidRig.Count;i++)offsetRig.Rest[i]+=offset;
        var poseA=new HumanoidAnimation();var poseB=new HumanoidAnimation();
        foreach(var state in new[]{new HumanoidMotion{Grounded=true},new HumanoidMotion{Grounded=true,Slide=true},new HumanoidMotion{Grounded=true,Seated=true},new HumanoidMotion{Swimming=true}})
        for(int frame=0;frame<60;frame++)
        {poseA.Step(centred,state,1f/60);poseB.Step(offsetRig,state,1f/60);for(int bone=0;bone<HumanoidRig.Count;bone++)Check((poseB.Joints[bone]-poseA.Joints[bone]-offset).Length<.0001f,"Retargeting changed imported hip/spine offset");}
        var noAllocationRig=new HumanoidRig(1.75f,HumanoidRestPose.T);var noAllocation=new HumanoidAnimation();
        var standing=new HumanoidMotion{Grounded=true};for(int i=0;i<100;i++)noAllocation.Step(noAllocationRig,standing,.001f);noAllocation.Play(HumanoidEmote.Dance,true);
        long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)noAllocation.Step(noAllocationRig,standing,.001f);
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"Shared per-frame pose generator allocates managed memory");
        foreach(var invalid in new[]{Array.Empty<V3>(),Enumerable.Repeat(new V3(float.NaN,0,0),20).ToArray(),Enumerable.Repeat(new V3(0,0,0),20).ToArray()})
        {bool failed=false;try{HumanoidRig.Fit(invalid,new SkinPreferences());}catch(FormatException){failed=true;}Check(failed,"Invalid humanoid accepted");}
        Console.WriteLine("PASS: shared humanoid fitting, four-bone normalized weights, side isolation, IK lengths/singularities, poses/emotes at 10/20/60/144 FPS, cancellation, pause and settings IDs. Native skin deformation/appearance unrun.");
    }
}
