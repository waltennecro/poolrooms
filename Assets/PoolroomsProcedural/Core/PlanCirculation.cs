using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum ProceduralGrammar { CrossV12, Varied, SolarAtlas }
    public enum InternalLayout { CrossV12, OffsetCourt, SplitGallery, PerimeterWalk, RetailParallel, RetailCross, RetailWarehouse }

    public readonly struct PlanRect
    {
        public readonly float MinX,MinZ,MaxX,MaxZ;
        public float Width { get { return MaxX-MinX; } }
        public float Depth { get { return MaxZ-MinZ; } }
        public float X { get { return (MinX+MaxX)*.5f; } }
        public float Z { get { return (MinZ+MaxZ)*.5f; } }
        public PlanRect(float minX,float minZ,float maxX,float maxZ)
        {MinX=minX;MinZ=minZ;MaxX=maxX;MaxZ=maxZ;}
        public bool Contains(float x,float z,float margin=0)
        {return x>=MinX+margin&&x<=MaxX-margin&&z>=MinZ+margin&&z<=MaxZ-margin;}
        public bool Overlaps(PlanRect other,float padding=0)
        {return MaxX+padding>other.MinX&&MinX-padding<other.MaxX&&MaxZ+padding>other.MinZ&&MinZ-padding<other.MaxZ;}
        public PlanRect Expanded(float value){return new PlanRect(MinX-value,MinZ-value,MaxX+value,MaxZ+value);}
        public static void Subtract(List<PlanRect> free,PlanRect cut)
        {
            // Visit only rectangles that existed on entry, so newly split pieces are never processed twice.
            int count=free.Count;
            for(int i=count-1;i>=0;i--)
            {
                PlanRect r=free[i];if(!r.Overlaps(cut))continue;free.RemoveAt(i);
                float x0=Math.Max(r.MinX,cut.MinX),x1=Math.Min(r.MaxX,cut.MaxX),z0=Math.Max(r.MinZ,cut.MinZ),z1=Math.Min(r.MaxZ,cut.MaxZ);
                Add(free,new PlanRect(r.MinX,r.MinZ,r.MaxX,z0));Add(free,new PlanRect(r.MinX,z1,r.MaxX,r.MaxZ));
                Add(free,new PlanRect(r.MinX,z0,x0,z1));Add(free,new PlanRect(x1,z0,r.MaxX,z1));
            }
        }
        private static void Add(List<PlanRect> result,PlanRect r){if(r.Width>.001f&&r.Depth>.001f)result.Add(r);}
    }
    public sealed class WalkingRoute
    {
        public readonly int Exit; // -1 denotes the optional complete perimeter loop.
        public readonly V2[] Points;
        public readonly float HalfWidth;
        public WalkingRoute(int exit,V2[] points,float halfWidth){Exit=exit;Points=points;HalfWidth=halfWidth;}
    }
    public sealed class PlanCirculation
    {
        public readonly InternalLayout Layout;
        public readonly WalkingRoute[] Routes;
        public readonly PlanRect[] Reserved,Cuts;
        public PlanCirculation(InternalLayout layout,WalkingRoute[] routes,PlanRect[] reserved,PlanRect[] cuts)
        {Layout=layout;Routes=routes;Reserved=reserved;Cuts=cuts;}
        public bool Contains(Room room,float x,float z,float margin=0)
        {
            if(Math.Abs(x)>room.Width*.5f-margin||Math.Abs(z)>room.Depth*.5f-margin)return false;
            foreach(PlanRect cut in Cuts)if(cut.Expanded(margin).Contains(x,z))return false;
            return true;
        }
        public bool Fits(PlanRect bounds,float margin=0)
        {
            foreach(PlanRect r in Reserved)if(r.Overlaps(bounds,margin))return false;
            foreach(PlanRect r in Cuts)if(r.Overlaps(bounds,margin))return false;
            return true;
        }
        public bool Valid(Room room,LayoutConfig c)
        {
            if(Layout==InternalLayout.CrossV12||Reserved.Length==0||!Contains(room,0,0,1))return false;
            int exits=0;
            foreach(WalkingRoute route in Routes)
            {
                if(route.HalfWidth<1.1f||route.Points.Length<2)return false;
                if(route.Exit>=0)
                {
                    if(route.Exit>3||(exits&(1<<route.Exit))!=0)return false;exits|=1<<route.Exit;
                    V2 start=route.Points[0],end=route.Points[route.Points.Length-1];
                    if(Math.Abs(start.X)+Math.Abs(start.Y)>.001f||Math.Abs(end.X-Directions.DX[route.Exit]*room.Width*.5f)+Math.Abs(end.Y-Directions.DZ[route.Exit]*room.Depth*.5f)>.001f)return false;
                }
                for(int i=0;i<route.Points.Length;i++)
                {
                    V2 p=route.Points[i];if(float.IsNaN(p.X)||float.IsNaN(p.Y)||!Contains(room,p.X,p.Y))return false;
                    if(i==0)continue;V2 a=route.Points[i-1];
                    if(Math.Abs(a.X-p.X)>.001f&&Math.Abs(a.Y-p.Y)>.001f)return false;
                    PlanRect segment=new PlanRect(Math.Min(a.X,p.X),Math.Min(a.Y,p.Y),Math.Max(a.X,p.X),Math.Max(a.Y,p.Y)).Expanded(route.HalfWidth);
                    foreach(PlanRect cut in Cuts)if(cut.Overlaps(segment,.1f))return false;
                }
            }
            return exits==room.Exits;
        }
        public static PlanCirculation Create(Room room,LayoutConfig c,ref StableRandom random,int attempt=0,int layoutOverride=-1)
        {
            float hx=room.Width*.5f,hz=room.Depth*.5f,half=random.Range(1.15f,1.55f);
            InternalLayout style=(InternalLayout)(1+random.Range(3));
            if(layoutOverride>=1&&layoutOverride<=3)style=(InternalLayout)layoutOverride;
            if(attempt==3){style=InternalLayout.OffsetCourt;half=1.15f;}
            var routes=new List<WalkingRoute>();var reserved=new List<PlanRect>{new PlanRect(-1.25f,-1.25f,1.25f,1.25f)};
            float hubX=random.Range(-2.8f,2.8f),hubZ=random.Range(-2.8f,2.8f);
            if(attempt==3){hubX*=.25f;hubZ*=.25f;}
            float rx=hx-3.3f,rz=hz-3.3f;
            if(style==InternalLayout.PerimeterWalk)
            {
                Add(routes,reserved,-1,new[]{new V2(-rx,-rz),new V2(rx,-rz),new V2(rx,rz),new V2(-rx,rz),new V2(-rx,-rz)},half,hx,hz);
            }
            for(int exit=0;exit<4;exit++)if(room.HasExit(exit))
            {
                var points=new List<V2>{new V2(0,0)};
                if(style==InternalLayout.PerimeterWalk)
                {
                    // Shared access stem, then a branch around the perimeter. The full loop gives both directions.
                    points.Add(new V2(0,-rz));
                    if(exit==0){points.Add(new V2(-rx,-rz));points.Add(new V2(-rx,rz));points.Add(new V2(0,rz));}
                    if(exit==1){points.Add(new V2(rx,-rz));points.Add(new V2(rx,0));}
                    if(exit==3){points.Add(new V2(-rx,-rz));points.Add(new V2(-rx,0));}
                }
                else
                {
                    points.Add(new V2(hubX,0));points.Add(new V2(hubX,hubZ));
                    if(exit%2==0)
                    {
                        float turnZ=Directions.DZ[exit]*(hz-2.8f);
                        if(style==InternalLayout.SplitGallery)
                        {float elbow=Directions.DX[(exit+1)&3]*Math.Min(hx-4,random.Range(2.8f,4.4f));points.Add(new V2(elbow,hubZ));points.Add(new V2(elbow,turnZ));}
                        else points.Add(new V2(hubX,turnZ));
                        points.Add(new V2(0,turnZ));
                    }
                    else
                    {
                        float turnX=Directions.DX[exit]*(hx-2.8f);
                        if(style==InternalLayout.SplitGallery)
                        {float elbow=Directions.DZ[(exit+1)&3]*Math.Min(hz-4,random.Range(2.8f,4.4f));points.Add(new V2(hubX,elbow));points.Add(new V2(turnX,elbow));}
                        else points.Add(new V2(turnX,hubZ));
                        points.Add(new V2(turnX,0));
                    }
                }
                points.Add(new V2(Directions.DX[exit]*hx,Directions.DZ[exit]*hz));
                Add(routes,reserved,exit,points.ToArray(),half,hx,hz);
                float mouth=c.DoorWidth*.5f+.45f;
                reserved.Add(exit%2==0?new PlanRect(-mouth,exit==0?hz-2:-hz,mouth,exit==0?hz:-hz+2):
                    new PlanRect(exit==1?hx-2:-hx,-mouth,exit==1?hx:-hx+2,mouth));
            }
            // An isolated finite preview still connects its spawn to the loop instead of leaving a disconnected island.
            if(room.Exits==0&&style==InternalLayout.PerimeterWalk)Add(routes,reserved,-2,new[]{new V2(0,0),new V2(0,-rz)},half,hx,hz);
            var cuts=new List<PlanRect>();int wanted=1+random.Range(3);
            int first=random.Range(4);
            for(int i=0;i<4&&cuts.Count<wanted;i++)
            {
                int corner=(first+i)&3,sx=corner<2?-1:1,sz=(corner&1)==0?-1:1;
                float w=random.Range(1.5f,Math.Min(4.5f,hx-c.DoorWidth*.5f-1)),d=random.Range(1.5f,Math.Min(4.5f,hz-c.DoorWidth*.5f-1));
                PlanRect cut=new PlanRect(sx<0?-hx:hx-w,sz<0?-hz:hz-d,sx<0?-hx+w:hx,sz<0?-hz+d:hz);
                bool clear=true;foreach(PlanRect path in reserved)if(path.Overlaps(cut,.25f)){clear=false;break;}
                if(clear)cuts.Add(cut);
            }
            return new PlanCirculation(style,routes.ToArray(),reserved.ToArray(),cuts.ToArray());
        }
        private static void Add(List<WalkingRoute> routes,List<PlanRect> reserved,int exit,V2[] points,float half,float hx,float hz)
        {
            var clean=new List<V2>();foreach(V2 p in points)
                if(clean.Count==0||Math.Abs(p.X-clean[clean.Count-1].X)+Math.Abs(p.Y-clean[clean.Count-1].Y)>.001f)clean.Add(p);
            routes.Add(new WalkingRoute(exit,clean.ToArray(),half));
            for(int i=1;i<clean.Count;i++)
            {
                V2 a=clean[i-1],b=clean[i];
                reserved.Add(new PlanRect(Math.Max(-hx,Math.Min(a.X,b.X)-half),Math.Max(-hz,Math.Min(a.Y,b.Y)-half),
                    Math.Min(hx,Math.Max(a.X,b.X)+half),Math.Min(hz,Math.Max(a.Y,b.Y)+half)));
            }
        }
    }
}
