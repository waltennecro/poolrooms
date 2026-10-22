using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void CameraAndAvatars()
    {
        var names=new HashSet<string>();var offsets=new HashSet<string>();var view=CameraPerspective.FirstPerson;
        for(int i=0;i<8;i++)
        {
            var pose=CameraPerspectives.Get(view);Check(names.Add(pose.Name),"Camera perspectives have duplicate names");
            Check(offsets.Add($"{pose.Offset.X}:{pose.Offset.Y}:{pose.Offset.Z}"),"Camera perspectives have duplicate poses");
            Check(pose.Offset.Length<10,"Camera leaves the bounded nearby environment");view=CameraPerspectives.Next(view);
        }
        Check(view==CameraPerspective.FirstPerson,"F5 cycle does not return to first person");
        var prefs=new GamePreferences{perspective=(CameraPerspective)999};prefs.Sanitize();Check(prefs.perspective==CameraPerspective.FirstPerson,"Corrupt saved perspective was not repaired");
        prefs.perspective=CameraPerspective.Front;prefs.Reset(SettingsCategory.Camera);Check(prefs.perspective==CameraPerspective.FirstPerson,"Camera reset leaves stale perspective");
        float expected=5*(1-(float)Math.Exp(-10));
        foreach(int fps in new[]{10,15,20,30,60,120,240})
        {
            float distance=0;for(int i=0;i<fps;i++){distance=CameraPerspectives.Recovery(distance,5,1f/fps);Check(distance>=0&&distance<=5,"Camera recovery overshoots safe distance");}
            Check(Math.Abs(distance-expected)<.0001f,"Camera recovery varies with FPS");
            float safe=CameraPerspectives.SafeDistance(5,1.3f);Check(CameraPerspectives.Recovery(distance,safe,0)==safe,"Camera eases through a new obstruction");
        }
        Check(CameraPerspectives.SafeDistance(5,.01f)==0,"Camera has a negative collision distance");
        var prior=CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture=new CultureInfo("es-MX");
            string vertices="v 0 0 0\nv 3 0 0\nv 3 3 0\nv 2 3 0\nv 2 1 0\nv 1 1 0\nv 1 3 0\nv 0 3 0\n";
            foreach(string face in new[]{"1 2 3 4 5 6 7 8","8 7 6 5 4 3 2 1","-8 -7 -6 -5 -4 -3 -2 -1"})
            {
                var obj=AvatarObj.Parse(vertices+"f "+face);Check(obj.Submeshes[0].Count==18,"Concave OBJ not triangulated");float area=0;var indices=obj.Submeshes[0];
                for(int t=0;t<indices.Count;t+=3)
                {
                    var a=obj.Positions[indices[t]];var b=obj.Positions[indices[t+1]];var c=obj.Positions[indices[t+2]];
                    float triangle=V3.Cross(b-a,c-a).Length*.5f;Check(triangle>0,"OBJ contains degenerate triangles");area+=triangle;
                    var centre=(a+b+c)*(1f/3);Check(!(centre.X>1&&centre.X<2&&centre.Y>1),"OBJ triangulation filled the concave opening");
                }
                Check(Math.Abs(area-7)<.0001f,"OBJ triangulation changes silhouette area");
            }
            var seams=AvatarObj.Parse("mtllib skin.mtl\nv 0 0 0\nv 1.5 0 0\nv 0 2 0\nvt 0 0\nvt 1 0\nvt 0 1\nvn 0 0 1\nvn 0 0 -1\nusemtl Front\nf 1/1/1 2/2/1 3/3/1\nusemtl Back\nf -1/3/2 -2/2/2 -3/1/2");
            Check(seams.Materials.SequenceEqual(new[]{"Front","Back"})&&seams.Submeshes.Count==2,"OBJ lost material slots");
            Check(seams.Positions.Count==6&&seams.UV.Count==6&&!seams.MissingNormals,"OBJ merged authored normal/UV seams");
            Check(seams.Libraries.Single()=="skin.mtl"&&seams.Positions[1].X==1.5f,"OBJ culture or material library changed");
            foreach(string invalid in new[]{"", "v NaN 0 0", "v Infinity 0 0", "v 0 0 0\nf 0 1 1", "v 0 0 0\nf -5 1 1", "v 0 0 0\nf 1 1 1", "v 0 0 0\nf 1 2 3",new string('x',AvatarObj.MaxBytes+1)})
            {bool failed=false;try{AvatarObj.Parse(invalid);}catch(FormatException){failed=true;}Check(failed,"Malformed OBJ accepted");}
            // Text-heavy exports can exceed the former 16 MiB limit while their
            // geometry remains tiny. The 64 MiB file budget must accept these.
            string comments=string.Concat(Enumerable.Repeat("# export metadata "+new string('a',1000)+"\n",17000));
            Check(comments.Length>16*1024*1024&&comments.Length<AvatarObj.MaxBytes,"OBJ large-text fixture misses the former boundary");
            Check(AvatarObj.Parse(comments+"v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n").Positions.Count==3,"Valid OBJ over the former 16 MiB cap was rejected");
            var random=new Random(2701);
            for(int n=3;n<=32;n++)for(int trial=0;trial<8;trial++)
            {
                // Simple star-shaped polygons: compare authored outline area to triangles, both windings.
                var lines=new List<string>();float outline=0;var points=new V3[n];
                for(int i=0;i<n;i++){double angle=i*Math.PI*2/n;float radius=.4f+(float)random.NextDouble();points[i]=new V3((float)Math.Cos(angle)*radius,(float)Math.Sin(angle)*radius,0);lines.Add(FormattableString.Invariant($"v {points[i].X:R} {points[i].Y:R} 0"));}
                for(int i=0;i<n;i++){var a=points[i];var b=points[(i+1)%n];outline+=a.X*b.Y-a.Y*b.X;}
                foreach(bool reverse in new[]{false,true})
                {
                    var order=Enumerable.Range(1,n);if(reverse)order=order.Reverse();
                    var obj=AvatarObj.Parse(string.Join("\n",lines)+"\nf "+string.Join(" ",order));var indices=obj.Submeshes[0];float area=0;
                    Check(indices.Count==(n-2)*3,"OBJ polygon lost faces");
                    for(int i=0;i<indices.Count;i+=3)area+=V3.Cross(obj.Positions[indices[i+1]]-obj.Positions[indices[i]],obj.Positions[indices[i+2]]-obj.Positions[indices[i]]).Length*.5f;
                    Check(Math.Abs(area-Math.Abs(outline)*.5f)<.00002f,"OBJ polygon tessellation changed outline area");
                }
            }
        }
        finally{CultureInfo.CurrentCulture=prior;}
        Console.WriteLine("PASS: eight distinct camera poses, collision recovery across seven FPS rates and OBJ tessellation/material/UV/normal contracts, including 480 polygon/winding cases. Native camera and avatar rendering unrun.");
    }
}
