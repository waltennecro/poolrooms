using System;
using System.IO;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void TravelMovement()
    {
        var prefs=new TravelPreferences{pace=(TravelPace)99};prefs.Sanitize();
        Check(prefs.pace==TravelPace.Normal&&prefs.allowAutoAdvance&&prefs.allowFastSwim&&prefs.allowGrapple,"Invalid/old travel preferences must retain safe defaults");
        foreach(float dt in new[]{1f/10,1f/20,1f/30,1f/60,1f/144,1f/240})
        {
            var normal=new ExplorerTravel();
            Check(normal.StepSpeed(dt,TravelPace.Normal,true,true,false,false,false,3.2f,5.5f)==3.2f,"Normal walking changed");
            Check(normal.StepSpeed(dt,TravelPace.Normal,true,true,true,false,false,3.2f,5.5f)==5.5f,"Original sprint changed");
            Check(normal.StepSpeed(dt,TravelPace.Express,true,false,false,false,false,3.2f,5.5f)==3.2f,"Express must not accelerate backwards/sideways");
            foreach(var pace in new[]{TravelPace.Agile,TravelPace.Express})
            {
                var travel=new ExplorerTravel();float distance=0,time=0,target=pace==TravelPace.Agile?8:12;
                while(time<4-.000001f){float step=Math.Min(dt,4-time);float speed=travel.StepSpeed(step,pace,true,true,false,false,false,3.2f,5.5f);Check(speed>=0&&speed<=target,"Travel exceeds its selected speed");distance+=speed*step;time+=step;}
                float ramp=target/18,expected=target*(4-ramp)+9*ramp*ramp;
                Check(Math.Abs(distance-expected)<.003f,"Travel acceleration changes with frame rate");
                int count=ExplorerTravel.MovementSteps(dt);
                Check(28*dt/count<=ExplorerTravel.MaxMoveStep+.00001f,"Fast movement substep exceeds physical margin");
                Check(travel.StepSpeed(dt,pace,false,true,false,false,false,3.2f,5.5f)==0,"Released direction keeps running");
            }
            var swimmer=new ExplorerTravel();float swim=0;
            for(int i=0;i<Math.Ceiling(2/dt);i++)swim=swimmer.StepSpeed(dt,TravelPace.Express,true,true,true,true,true,3.2f,5.5f);
            Check(Math.Abs(swim-5.2f)<.0001f,"Swimming boost did not reach its bounded speed");
            Check(swimmer.StepSpeed(dt,TravelPace.Express,true,true,false,true,false,3.2f,5.5f)==2.1f,"Normal swimming changed");
            var auto=new ExplorerTravel();V2 axis=auto.ResolveInput(new V2(),true,true);
            Check(auto.AutoAdvance&&axis.Y==1,"Automatic advance did not start");
            axis=auto.ResolveInput(new V2(1,0),false,true);
            Check(axis.X>0&&axis.Y>0&&Math.Abs(axis.X*axis.X+axis.Y*axis.Y-1)<.00001f,"Automatic steering gains diagonal speed");
            for(int i=0;i<Math.Ceiling(.18f/dt);i++)auto.ReportMovement(1,0,dt);
            Check(!auto.AutoAdvance,"Automatic advance persists against an obstruction");
            auto.ResolveInput(new V2(),true,true);auto.ResolveInput(new V2(0,-1),false,true);
            Check(!auto.AutoAdvance,"Backward input does not stop automatic advance");
            auto.ResolveInput(new V2(),true,true);auto.Stop();
            Check(!auto.AutoAdvance,"Pause/loading stop retained automatic movement");
            var hook=new GrapplePull();Check(hook.Begin(28),"Valid architectural anchor rejected");
            V3 offset=new V3(0,0,28);float traveled=0;
            for(int i=0;i<Math.Ceiling(4/dt);i++)
            {
                V3 delta=hook.Step(offset,dt);Check(delta.Length<=15*dt+.0001f,"Grapple exceeds maximum speed");
                int count=ExplorerTravel.StepsFor(delta);Check(delta.Length/count<=.18001f,"Grapple bypasses bounded movement");
                traveled+=delta.Length;offset=offset-delta;
                Check(offset.Length>=GrapplePull.StopDistance-.0001f,"Grapple overshot its attachment");
            }
            Check(Math.Abs(traveled-(28-GrapplePull.StopDistance))<.001f&&!hook.Active,"Grapple does not stop at its target");
            Check(hook.Begin(12),"Cannot reacquire after completed pull");
            for(int i=0;i<Math.Ceiling(.14f/dt);i++){hook.Step(new V3(0,0,12),dt);hook.ReportMovement(1,0,dt);}
            Check(!hook.Active,"Blocked grapple persists indefinitely");
            hook.Begin(12);hook.Step(new V3(float.NaN,0,0),dt);Check(!hook.Active,"Invalid anchor propagates non-finite movement");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1,0,1,28.01f})
        {var hook=new GrapplePull();Check(!hook.Begin(bad),"Unsafe grapple anchor accepted");}
        var safe=new ExplorerTravel();V2 bounded=safe.ResolveInput(new V2(float.NaN,float.PositiveInfinity),false,false);
        Check(bounded.X==0&&bounded.Y==0,"Non-finite input propagates into movement");
        foreach(float dt in new[]{0f,-1,float.NaN,float.PositiveInfinity})
        {var hook=new GrapplePull();hook.Begin(12);Check(hook.Step(new V3(0,0,12),dt).Length==0,"Invalid time advances grapple");}
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
        string runtime=File.ReadAllText(Path.Combine(root,"Assets/PoolroomsProcedural/Runtime/PoolroomsTravel.cs"));
        Check(!runtime.Contains("transform.position=")&&!runtime.Contains(".Teleport("),"Travel replaced collision movement with teleportation");
        Check(runtime.Contains("QueryTriggerInteraction.Ignore")&&runtime.Contains("count>=hits.Length")&&runtime.Contains("Streaming gate"),"Grapple lost conservative target/obstruction selection");
        Console.WriteLine("PASS: travel pacing, auto-stop/steering, swimming and grapple at six frame rates; bounded steps, stop margins, cancellation and invalid-input checks. Unity/PhysX unrun.");
    }
}
