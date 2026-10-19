using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Unequal frontage subdivision and optional rear wings. Each new wing pays for
    // a real connecting gallery before rooms are placed; there are no retry searches.
    public static class InteriorSubdivision
    {
        public const int MaximumBays=16;
        public const float MinimumRoom=5.25f,GalleryWidth=2.6f;
        public static void Add(InteriorBay root,ref StableRandom random,List<InteriorBay> bays,
            List<PlanRect> galleries,List<WalkingRoute> routes)
        {
            float w=root.LocalWidth,d=root.LocalDepth,front=-d*.5f;
            const float foyerMinimum=1.2f;
            bool rearWing=w>=MinimumRoom+GalleryWidth&&d>=MinimumRoom+GalleryWidth+foyerMinimum&&random.Value()<.62f;
            if(!rearWing)
            {
                Strip(root,-w*.5f,w*.5f,front,d,ref random,bays,routes,false,0,0);
                return;
            }
            int side=random.Range(2)==0?-1:1;
            float spine=side*(w*.5f-GalleryWidth*.5f);
            float x0=-w*.5f+(side<0?GalleryWidth:0),x1=w*.5f-(side>0?GalleryWidth:0);
            // Typical 36 m world cells cannot fit two furnished rows per quadrant.
            // Use an open foyer in that case, keeping the rear room and its gallery
            // rather than silently restricting this architecture to oversized cells.
            bool frontRooms=d>=MinimumRoom*2+GalleryWidth;
            float firstDepth=random.Range(frontRooms?MinimumRoom:foyerMinimum,d-MinimumRoom-GalleryWidth);
            float branch=front+firstDepth+GalleryWidth*.5f;
            galleries.Add(WorldBounds(root,new PlanRect(spine-GalleryWidth*.5f,front,spine+GalleryWidth*.5f,d*.5f)));
            galleries.Add(WorldBounds(root,new PlanRect(x0,branch-GalleryWidth*.5f,x1,branch+GalleryWidth*.5f)));
            if(frontRooms)Strip(root,x0,x1,front,firstDepth,ref random,bays,routes,false,spine,branch);
            Strip(root,x0,x1,branch+GalleryWidth*.5f,d*.5f-branch-GalleryWidth*.5f,ref random,bays,routes,true,spine,branch);
        }
        private static void Strip(InteriorBay root,float x0,float x1,float front,float depth,ref StableRandom random,
            List<InteriorBay> bays,List<WalkingRoute> routes,bool rear,float spine,float branch)
        {
            // Binary subdivision uses a continuous split ratio, rather than two equal halves.
            const float gap=.24f;float span=x1-x0;
            bool split=span>=MinimumRoom*2+gap&&random.Value()<.78f;
            float cut=split?random.Range(x0+MinimumRoom,x1-MinimumRoom-gap):x1;
            int count=split?2:1;
            for(int i=0;i<count;i++)
            {
                float lo=i==0?x0:cut+gap,hi=i==0?cut:x1;
                // Setbacks produce recesses without moving the gallery-facing doorway.
                float roomDepth=random.Range(MinimumRoom,depth);
                V3 p=root.World((lo+hi)*.5f,0,front+roomDepth*.5f);
                int turns=root.QuarterTurns;
                int variant=random.Range(3);if(root.Variant>=3)variant=root.Variant;
                var bay=new InteriorBay(p.X,p.Z,(turns&1)==0?hi-lo:roomDepth,(turns&1)==0?roomDepth:hi-lo,turns,variant,random.Next());
                bays.Add(bay);
                var points=new List<V2>{new V2(0,0)};
                V3 entry=root.World(rear?spine:(lo+hi)*.5f,0,-root.LocalDepth*.5f);
                // First turn stays on the protected world cross, independently of orientation.
                points.Add((turns&1)==0?new V2(entry.X,0):new V2(0,entry.Z));points.Add(new V2(entry.X,entry.Z));
                if(rear)
                {
                    p=root.World(spine,0,branch);points.Add(new V2(p.X,p.Z));
                    p=root.World((lo+hi)*.5f,0,branch);points.Add(new V2(p.X,p.Z));
                }
                p=bay.World(0,0,-bay.LocalDepth*.5f+.9f);points.Add(new V2(p.X,p.Z));
                for(int k=points.Count-1;k>0;k--)if(Math.Abs(points[k].X-points[k-1].X)+Math.Abs(points[k].Y-points[k-1].Y)<.0001f)points.RemoveAt(k);
                routes.Add(new WalkingRoute(-1,points.ToArray(),.6f));
            }
        }
        private static PlanRect WorldBounds(InteriorBay root,PlanRect bounds)
        {
            V3 a=root.World(bounds.MinX,0,bounds.MinZ),b=root.World(bounds.MaxX,0,bounds.MaxZ);
            return new PlanRect(Math.Min(a.X,b.X),Math.Min(a.Z,b.Z),Math.Max(a.X,b.X),Math.Max(a.Z,b.Z));
        }
    }
}
