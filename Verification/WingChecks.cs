using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Poolrooms.Core;

static partial class Checks
{
    static void WingsAndNormalFlight()
    {
        var corrupted=new WingPreferences{design=999999,speed=(WingSpeed)99,size=float.NaN,attachmentHeight=-1,backOffset=float.PositiveInfinity};corrupted.Sanitize();
        Check(corrupted.design==143&&corrupted.speed==WingSpeed.Medium&&corrupted.size==1&&corrupted.attachmentHeight==.55f&&corrupted.backOffset==.17f,"Wing preference recovery failed");
        Check(WingCatalog.Count>100&&WingCatalog.Families==12,"Wing catalogue too small");
        var fingerprints=new HashSet<string>();int triangles=0,maxTriangles=0;
        var names=new HashSet<string>();
        for(int id=0;id<WingCatalog.Count;id++)
        {
            Check(names.Add(WingCatalog.Name(id)),"Duplicate wing name");
            var mesh=WingCatalog.Build(id);var left=WingCatalog.Build(id,true);
            Check(mesh.TriangleCount>100&&mesh.TriangleCount<10000,"Wing mesh exceeds single-player budget");
            Check(left.Positions.Count==mesh.Positions.Count&&left.TriangleCount==mesh.TriangleCount,"Wing mirror loses geometry");
            Check(mesh.Positions.Count==mesh.Normals.Count&&mesh.Positions.Count==mesh.UV.Count,"Wing attributes mismatch");
            var text=new StringBuilder();
            for(int i=0;i<mesh.Positions.Count;i++)
            {
                var p=mesh.Positions[i];var l=left.Positions[i];var n=mesh.Normals[i];
                Check(HumanoidRig.Finite(p)&&HumanoidRig.Finite(n)&&p.Length<4,"Invalid wing coordinates");
                Check(p.X>=-.05f&&p.X<=2.5f,"Wing escaped attachment envelope");
                Check(Math.Abs(n.Length-1)<.001f,"Wing normal is not unit length");
                Check(l.X==-p.X&&l.Y==p.Y&&l.Z==p.Z,"Mirrored wing not symmetric");
                text.Append(FormattableString.Invariant($"{p.X:R},{p.Y:R},{p.Z:R};"));
            }
            foreach(var m in new[]{mesh,left})foreach(var slot in m.Indices)
            {
                Check(slot.Count%3==0,"Wing index list incomplete");
                for(int t=0;t<slot.Count;t+=3)
                {
                    int a=slot[t],b=slot[t+1],c=slot[t+2];Check(a>=0&&b>=0&&c>=0&&a<m.Positions.Count&&b<m.Positions.Count&&c<m.Positions.Count,"Wing index escaped bounds");
                    V3 normal=V3.Cross(m.Positions[b]-m.Positions[a],m.Positions[c]-m.Positions[a]);
                    Check(normal.Length>1e-7f,"Degenerate wing triangle");
                    Check(V3.Dot(normal,m.Normals[a]+m.Normals[b]+m.Normals[c])>=0,"Wing normal or mirror winding inverted");
                }
            }
            string hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
            Check(fingerprints.Add(hash),"Duplicate wing geometry: "+id);
            triangles+=mesh.TriangleCount;maxTriangles=Math.Max(maxTriangles,mesh.TriangleCount);
        }
        var flight=new WingFlight();
        Check(!flight.Toggle(false)&&!flight.Active,"Unavailable normal flight activated");
        Check(flight.Step(V3.Up,WingSpeed.Fast,.1f).Length==0,"Inactive wing flight moved");
        foreach(var speed in Enum.GetValues<WingSpeed>())
        {
            float reference=-1;
            foreach(int fps in new[]{10,20,30,60,144,240})
            {
                flight.Stop();Check(flight.Toggle(true),"Normal wing flight needs a master session");V3 distance=new V3();
                for(int i=0;i<fps*3;i++)
                {
                    V3 d=flight.Step(new V3(1,1,1),speed,1f/fps);distance+=d;
                    Check(flight.Velocity.Length<=WingFlight.Speed(speed)+.0001f,"Diagonal wing input increases speed");
                    Check(d.Length/WingFlight.Steps(d)<=WingFlight.MaxMoveStep+.00001f,"Flight displacement skips capsule collision steps");
                }
                if(reference<0)reference=distance.Length;else Check(Math.Abs(reference-distance.Length)<.002f,"Wing travel depends on FPS");
                for(int i=0;i<fps*2;i++)flight.Step(new V3(),speed,1f/fps);
                Check(flight.Velocity.Length<.00001f,"Releasing inputs does not brake into hover");
                V3 before=flight.Velocity;
                foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})Check(flight.Step(V3.Up,speed,dt).Length==0&&flight.Velocity.Equals(before),"Invalid time moved flight");
                flight.Step(new V3(float.NaN,0,0),speed,.1f);Check(HumanoidRig.Finite(flight.Velocity),"Invalid wing input poisoned velocity");
                flight.Suspend();Check(flight.Active&&flight.Velocity.Length==0,"Pause loses flight mode or retains drift");
                Check(!flight.Toggle(true)&&!flight.Active,"G cannot return to gravity");
                Check(flight.Step(V3.Up,speed,.1f).Length==0,"Stopped flight moved");
            }
        }
        flight.Stop();flight.Toggle(true);V3 stall=flight.Step(new V3(100,100,100),WingSpeed.VeryFast,1000);
        Check(stall.Length<=WingFlight.Speed(WingSpeed.VeryFast)*ExplorerLocomotion.MaxFrameTime,"A stalled frame teleports the wing player");
        Check(stall.Length/WingFlight.Steps(stall)<=WingFlight.MaxMoveStep+.00001f,"Stalled frame exceeds collision subdivision budget");
        // Older persisted skin preferences still enable the shared rig; static mode is opt-in.
        Check(!new SkinPreferences().objStatic&&new SkinPreferences().animate,"Old skins lost automatic animation");
        Console.WriteLine($"PASS: {names.Count} named wings, {fingerprints.Count} distinct geometry fingerprints, {triangles} right-wing triangles traversed; max {maxTriangles} per wing; four flight speeds at six FPS rates, hover, pause, corrupt input, stalls and bounded collision steps. Native Unity collision/rendering tests remain separate.");
    }
}
