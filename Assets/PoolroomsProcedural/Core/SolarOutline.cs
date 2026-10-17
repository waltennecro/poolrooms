using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Convex clipping keeps floor slabs, roofs and colliders on the same curved/chamfered outline.
    // Pools and circulation are reserved against a conservative corner envelope before geometry is built.
    internal static class SolarOutline
    {
        internal static float Radius(Room r,SolarRecipe recipe,LayoutConfig c)
        {
            if(recipe.SpatialVariant<2)return 0;
            float radius=Math.Min(recipe.SpatialVariant==2?3.2f:4f,Math.Min(r.Width,r.Depth)*.15f);
            for(int side=0;side<4;side++)if(r.HasExit(side))
                radius=Math.Min(radius,(side%2==0?r.Width:r.Depth)*.5f-SolarConnections.DoorWidth(r,side,c)*.5f-.65f);
            return Math.Max(.25f,radius);
        }
        internal static V2[] Contour(Room r,SolarRoomPlan p,int quality)
        {return Shapes.RoundedRect(r.Width,r.Depth,p.OutlineRadius,p.Chamfered?1:Math.Max(3,Math.Min(16,quality/4)));}
        internal static bool Contains(Room r,SolarRoomPlan p,float x,float z)
        {
            float hx=r.Width*.5f,hz=r.Depth*.5f;if(Math.Abs(x)>hx||Math.Abs(z)>hz)return false;
            float a=Math.Max(0,Math.Abs(x)-(hx-p.OutlineRadius)),b=Math.Max(0,Math.Abs(z)-(hz-p.OutlineRadius));
            return p.Chamfered?a+b<=p.OutlineRadius+.0001f:a*a+b*b<=p.OutlineRadius*p.OutlineRadius+.0001f;
        }
        internal static void Slab(MeshData m,PlanRect q,V2[] contour,float bottom,float top,Surface material,float uv)
        {
            var free=new List<V2>{new V2(q.MinX,q.MinZ),new V2(q.MaxX,q.MinZ),new V2(q.MaxX,q.MaxZ),new V2(q.MinX,q.MaxZ)};
            for(int i=0;i<contour.Length&&free.Count>0;i++)
            {
                V2 a=contour[i],b=contour[(i+1)%contour.Length];if(Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y)<.00001f)continue;
                var next=new List<V2>();V2 previous=free[free.Count-1];float dp=Distance(a,b,previous);
                foreach(V2 point in free)
                {
                    float dc=Distance(a,b,point);bool inside=dc>=-.00001f,oldInside=dp>=-.00001f;
                    if(inside!=oldInside){float t=dp/(dp-dc);next.Add(new V2(previous.X+(point.X-previous.X)*t,previous.Y+(point.Y-previous.Y)*t));}
                    if(inside)next.Add(point);previous=point;dp=dc;
                }
                free=next;
            }
            if(free.Count<3)return;var clean=new List<V2>();foreach(V2 v in free)if(clean.Count==0||Math.Abs(v.X-clean[clean.Count-1].X)+Math.Abs(v.Y-clean[clean.Count-1].Y)>.0001f)clean.Add(v);
            if(clean.Count>1&&Math.Abs(clean[0].X-clean[clean.Count-1].X)+Math.Abs(clean[0].Y-clean[clean.Count-1].Y)<.0001f)clean.RemoveAt(clean.Count-1);
            if(clean.Count<3)return;float x=0,z=0;foreach(V2 v in clean){x+=v.X;z+=v.Y;}x/=clean.Count;z/=clean.Count;
            for(int i=0;i<clean.Count;i++)
            {
                V2 a=clean[i],b=clean[(i+1)%clean.Count];V3 up=V3.Up;
                m.Triangle(Shapes.At(a,top),Shapes.At(b,top),new V3(x,top,z),up,up,up,new V2(a.X/uv,a.Y/uv),new V2(b.X/uv,b.Y/uv),new V2(x/uv,z/uv),material);
                m.Triangle(Shapes.At(a,bottom),Shapes.At(b,bottom),new V3(x,bottom,z),up*-1,up*-1,up*-1,new V2(a.X/uv,a.Y/uv),new V2(b.X/uv,b.Y/uv),new V2(x/uv,z/uv),material);
                V3 normal=new V3(b.Y-a.Y,0,a.X-b.X).Unit;
                m.Quad(Shapes.At(a,bottom),Shapes.At(b,bottom),Shapes.At(b,top),Shapes.At(a,top),normal,material,uv);
            }
        }
        private static float Distance(V2 a,V2 b,V2 p){return (b.X-a.X)*(p.Y-a.Y)-(b.Y-a.Y)*(p.X-a.X);}
        internal static void CornerWalls(MeshData m,Room r,SolarRoomPlan p,LayoutConfig c)
        {
            if(p.OutlineRadius<=0)return;V2[] contour=Contour(r,p,c.CurveSegments);
            for(int i=0;i<contour.Length;i++)
            {
                V2 a=contour[i],b=contour[(i+1)%contour.Length];
                if(Math.Abs(a.X-b.X)<.0001f||Math.Abs(a.Y-b.Y)<.0001f)continue;
                V3 normal=new V3(b.Y-a.Y,0,a.X-b.X).Unit,offset=normal*c.WallThickness;
                V3 lowA=Shapes.At(a,0),lowB=Shapes.At(b,0),highA=Shapes.At(a,r.Height),highB=Shapes.At(b,r.Height);
                m.Quad(lowA,lowB,highB,highA,normal*-1,Surface.Tile,c.TextureWorldSize);
                m.Quad(lowA+offset,lowB+offset,highB+offset,highA+offset,normal,Surface.Tile,c.TextureWorldSize);
                m.Quad(highA,highB,highB+offset,highA+offset,V3.Up,Surface.Tile,c.TextureWorldSize);
            }
        }
    }
}
