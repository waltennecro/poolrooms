using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class Shapes
    {
        public const float Pi = (float)Math.PI;
        public static float Sin(float x) { return (float)Math.Sin(x); }
        public static float Cos(float x) { return (float)Math.Cos(x); }
        public static V3 At(V2 p,float y) { return new V3(p.X,y,p.Y); }
        private static V2 Tex(V3 p,float uv) { return new V2(p.X/uv,p.Z/uv); }
        public static void Horizontal(MeshData m,V3 a,V3 b,V3 c,V3 d,Surface mat,float uv,bool up=true)
        {
            V3 n=V3.Up*(up?1:-1);
            m.QuadUV(a,b,c,d,n,n,n,n,Tex(a,uv),Tex(b,uv),Tex(c,uv),Tex(d,uv),mat);
        }
        // Identical point counts allow clean rings between a rounded basin and a square room.
        public static V2[] RoundedRect(float width,float depth,float radius,int cornerSegments)
        {
            radius=Math.Max(0,Math.Min(radius,Math.Min(width,depth)*.5f));
            var p=new V2[4*(cornerSegments+1)]; int index=0;
            for(int corner=0;corner<4;corner++)
            {
                float cx=(corner==0||corner==3?1:-1)*(width*.5f-radius);
                float cz=(corner<2?1:-1)*(depth*.5f-radius);
                for(int j=0;j<=cornerSegments;j++)
                {
                    float a=(corner+j/(float)cornerSegments)*Pi*.5f;
                    // Exact cardinal endpoints prevent tiny seam triangles in circular basins.
                    // Those triangles can collapse after a float translation to an offset pool.
                    if(j==0||j==cornerSegments)
                    {
                        int quadrant=(corner+(j==cornerSegments?1:0))&3;
                        p[index++]=new V2(cx+(quadrant==0?radius:quadrant==2?-radius:0),
                            cz+(quadrant==1?radius:quadrant==3?-radius:0));
                    }
                    else p[index++]=new V2(cx+Cos(a)*radius,cz+Sin(a)*radius);
                }
            }
            return p;
        }
        public static void Ring(MeshData m,V2[] outer,V2[] inner,float y,Surface mat,float uv,bool up=true)
        {
            for(int i=0;i<outer.Length;i++)
            {
                int j=(i+1)%outer.Length;
                Horizontal(m,At(outer[i],y),At(outer[j],y),At(inner[j],y),At(inner[i],y),mat,uv,up);
            }
        }
        public static void Fill(MeshData m,V2[] p,float y,Surface mat,float uv,bool up=true)
        {
            V3 n=V3.Up*(up?1:-1);
            for(int i=0;i<p.Length;i++)
            {
                V3 a=At(p[i],y),b=At(p[(i+1)%p.Length],y),c=new V3(0,y,0);
                m.Triangle(a,b,c,n,n,n,Tex(a,uv),Tex(b,uv),Tex(c,uv),mat);
            }
        }
        public static void Skirt(MeshData m,V2[] p,float bottom,float top,Surface mat,float uv,bool inward=false,bool smooth=false)
        {
            float u=0;
            for(int i=0;i<p.Length;i++)
            {
                int j=(i+1)%p.Length;
                V3 a=At(p[i],bottom),b=At(p[j],bottom),c=At(p[j],top),d=At(p[i],top);
                V3 edge=b-a;float len=edge.Length;if(len<1e-6f)continue;
                V3 n=new V3(edge.Z,0,-edge.X).Unit*(inward?-1:1),na=n,nb=n;
                if(smooth)
                {
                    V3 ea=At(p[j],0)-At(p[(i+p.Length-1)%p.Length],0);
                    V3 eb=At(p[(j+1)%p.Length],0)-At(p[i],0);
                    na=new V3(ea.Z,0,-ea.X).Unit*(inward?-1:1);
                    nb=new V3(eb.Z,0,-eb.X).Unit*(inward?-1:1);
                }
                m.QuadUV(a,b,c,d,na,nb,nb,na,new V2(u/uv,bottom/uv),new V2((u+len)/uv,bottom/uv),
                    new V2((u+len)/uv,top/uv),new V2(u/uv,top/uv),mat);u+=len;
            }
        }
        public static void Column(MeshData m,float x,float z,float radius,float bottom,float top,int segments,Surface mat,float uv)
        {
            var p=new V2[segments];
            for(int i=0;i<segments;i++){float a=i*2*Pi/segments;p[i]=new V2(Cos(a)*radius,Sin(a)*radius);}
            var part=new MeshData();Skirt(part,p,bottom,top,mat,uv,false,true);Fill(part,p,top,mat,uv);Fill(part,p,bottom,mat,uv,false);
            part.TranslateRotate(new V3(x,0,z),0);m.Append(part);
        }
        // A real solid wall with an empty semicircular arch, not a decal/overlaid arch.
        public static void Portal(MeshData m,float width,float height,float bottom,float opening,float openingHeight,float thickness,int segments,float uv)
        {
            float r=opening*.5f,spring=openingHeight-r,t=thickness*.5f;
            m.Box(new V3(-width*.5f,bottom,-t),new V3(-r,height,t),Surface.Tile,uv);
            m.Box(new V3(r,bottom,-t),new V3(width*.5f,height,t),Surface.Tile,uv);
            for(int i=0;i<segments;i++)
            {
                float a=Pi-i*Pi/segments,b=Pi-(i+1)*Pi/segments;
                float x0=Cos(a)*r,x1=Cos(b)*r,y0=spring+Sin(a)*r,y1=spring+Sin(b)*r;
                for(int side=-1;side<=1;side+=2)
                {
                    V3 p=new V3(x0,y0,t*side),q=new V3(x1,y1,t*side),s=new V3(x1,height,t*side),v=new V3(x0,height,t*side),n=new V3(0,0,side);
                    m.QuadUV(p,q,s,v,n,n,n,n,new V2(x0/uv,y0/uv),new V2(x1/uv,y1/uv),new V2(x1/uv,height/uv),new V2(x0/uv,height/uv),Surface.Tile);
                }
                V3 na=new V3(-Cos(a),-Sin(a),0),nb=new V3(-Cos(b),-Sin(b),0);
                m.QuadUV(new V3(x0,y0,-t),new V3(x1,y1,-t),new V3(x1,y1,t),new V3(x0,y0,t),na,nb,nb,na,
                    new V2(i*Pi*r/segments/uv,0),new V2((i+1)*Pi*r/segments/uv,0),new V2((i+1)*Pi*r/segments/uv,thickness/uv),new V2(i*Pi*r/segments/uv,thickness/uv),Surface.Tile);
                m.Quad(new V3(x0,height,-t),new V3(x1,height,-t),new V3(x1,height,t),new V3(x0,height,t),V3.Up,Surface.Tile,uv);
            }
        }
        public static void Partition(MeshData m,V2[] path,float thickness,float bottom,float height,Surface mat,float uv)
        {
            var left=new V3[path.Length];var right=new V3[path.Length];var normals=new V3[path.Length];var length=new float[path.Length];
            for(int i=0;i<path.Length;i++)
            {
                V3 p=At(path[i],bottom),e=At(path[Math.Min(i+1,path.Length-1)],0)-At(path[Math.Max(i-1,0)],0);
                V3 n=new V3(e.Z,0,-e.X).Unit;normals[i]=n;left[i]=p-n*(thickness*.5f);right[i]=p+n*(thickness*.5f);
                if(i>0)length[i]=length[i-1]+(At(path[i],0)-At(path[i-1],0)).Length;
            }
            V3 rise=V3.Up*(height-bottom);
            for(int i=0;i<path.Length-1;i++)
            {
                int j=i+1;V2 a=new V2(length[i]/uv,bottom/uv),b=new V2(length[j]/uv,bottom/uv),c=new V2(length[j]/uv,height/uv),d=new V2(length[i]/uv,height/uv);
                m.QuadUV(right[i],right[j],right[j]+rise,right[i]+rise,normals[i],normals[j],normals[j],normals[i],a,b,c,d,mat);
                m.QuadUV(left[i],left[j],left[j]+rise,left[i]+rise,normals[i]*-1,normals[j]*-1,normals[j]*-1,normals[i]*-1,a,b,c,d,mat);
                Horizontal(m,left[i]+rise,right[i]+rise,right[j]+rise,left[j]+rise,mat,uv);
                Horizontal(m,left[i],right[i],right[j],left[j],mat,uv,false);
            }
            V3 first=(At(path[0],0)-At(path[1],0)).Unit,last=(At(path[path.Length-1],0)-At(path[path.Length-2],0)).Unit;
            m.Quad(left[0],right[0],right[0]+rise,left[0]+rise,first,mat,uv);
            int k=path.Length-1;m.Quad(left[k],right[k],right[k]+rise,left[k]+rise,last,mat,uv);
        }
        public static V3 Bezier(V3 a,V3 b,V3 c,V3 d,float t)
        {float s=1-t;return a*(s*s*s)+b*(3*s*s*t)+c*(3*s*t*t)+d*(t*t*t);}
        private static V3 SlideRight(V3 a,V3 b,V3 c,V3 d,float t)
        {
            float s=1-t;V3 tangent=(b-a)*(3*s*s)+(c-b)*(6*s*t)+(d-c)*(3*t*t);
            return new V3(tangent.Z,0,-tangent.X).Unit;
        }

        public static void Slide(MeshData m,V3 a,V3 b,V3 c,V3 d,float radius,int segments)
        {
            const int cross=16;const float shell=.09f;float length=0;
            for(int i=0;i<segments;i++)
            {
                float t0=i/(float)segments,t1=(i+1)/(float)segments;
                V3 p=Bezier(a,b,c,d,t0),q=Bezier(a,b,c,d,t1),tangent=(q-p).Unit;
                V3 right=SlideRight(a,b,c,d,t0),nextRight=SlideRight(a,b,c,d,t1);float len=(q-p).Length;
                for(int j=0;j<cross;j++)
                {
                    float angle0=-Pi*.5f+j*Pi/cross,angle1=-Pi*.5f+(j+1)*Pi/cross;
                    for(int outer=0;outer<2;outer++)
                    {
                        float r=radius+outer*shell;
                        V3 o0=right*(Sin(angle0)*r)+V3.Up*(radius-Cos(angle0)*r),o1=right*(Sin(angle1)*r)+V3.Up*(radius-Cos(angle1)*r);
                        V3 e0=nextRight*(Sin(angle0)*r)+V3.Up*(radius-Cos(angle0)*r),e1=nextRight*(Sin(angle1)*r)+V3.Up*(radius-Cos(angle1)*r);
                        V3 n0=(right*(-Sin(angle0))+V3.Up*Cos(angle0))*(outer==0?1:-1),n1=(right*(-Sin(angle1))+V3.Up*Cos(angle1))*(outer==0?1:-1);
                        V3 en0=(nextRight*(-Sin(angle0))+V3.Up*Cos(angle0))*(outer==0?1:-1),en1=(nextRight*(-Sin(angle1))+V3.Up*Cos(angle1))*(outer==0?1:-1);
                        m.QuadUV(p+o0,p+o1,q+e1,q+e0,n0,n1,en1,en0,new V2(j/(float)cross,length),new V2((j+1)/(float)cross,length),new V2((j+1)/(float)cross,length+len),new V2(j/(float)cross,length+len),Surface.Slide);
                        if((i==0||i==segments-1)&&outer==0)
                        {
                            V3 end=i==0?p:q,frame=i==0?right:nextRight;float sign=i==0?-1:1;
                            V3 outer0=frame*(Sin(angle0)*(radius+shell))+V3.Up*(radius-Cos(angle0)*(radius+shell));
                            V3 outer1=frame*(Sin(angle1)*(radius+shell))+V3.Up*(radius-Cos(angle1)*(radius+shell));
                            m.Quad(end+(i==0?o0:e0),end+(i==0?o1:e1),end+outer1,end+outer0,tangent*sign,Surface.Slide);
                        }
                    }
                }
                for(int side=-1;side<=1;side+=2)
                {
                    V3 inner=right*(radius*side)+V3.Up*radius,outer=right*((radius+shell)*side)+V3.Up*radius;
                    V3 endInner=nextRight*(radius*side)+V3.Up*radius,endOuter=nextRight*((radius+shell)*side)+V3.Up*radius;
                    m.Quad(p+inner,p+outer,q+endOuter,q+endInner,V3.Up,Surface.Slide);
                }
                length+=len;
            }
        }
    }
}
