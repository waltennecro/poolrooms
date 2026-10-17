using System;

namespace Poolrooms.Core
{
    // Shared metric geometry. No Unity objects, random globals or third-party assets.
    public static class ZoneShapes
    {
        public static void Box(MeshData m,float x,float z,float width,float depth,float bottom,float top,
            Surface surface,float uv)
        {
            if(width<=0||depth<=0||top<=bottom)return;
            m.Box(new V3(x-width*.5f,bottom,z-depth*.5f),new V3(x+width*.5f,top,z+depth*.5f),surface,uv);
        }

        // kind 0: semicircular, 1: segmental, 2: pointed, 3: rectangular.
        // Both wall faces, reveal, top and jamb solids are present. The aperture stays empty.
        public static void Portal(MeshData m,float x,float z,float yaw,float width,float height,
            float bottom,float opening,float aperture,float thickness,int kind,int segments,float uv)
        {
            var part=new MeshData();float half=opening*.5f,t=thickness*.5f;
            Box(part,-(width+opening)*.25f,0,(width-opening)*.5f,thickness,bottom,height,Surface.Tile,uv);
            Box(part,(width+opening)*.25f,0,(width-opening)*.5f,thickness,bottom,height,Surface.Tile,uv);
            if(kind==3)Box(part,0,0,opening,thickness,aperture,height,Surface.Tile,uv);
            else
            {
                segments=Math.Max(12,segments);
                // Even segmentation puts a vertex at the pointed crown.
                segments+=(segments&1);
                float rise=kind==1?half*.5f:half;
                for(int i=0;i<segments;i++)
                {
                    float u0=i/(float)segments,u1=(i+1f)/segments;
                    float a=(2*u0-1)*half,b=(2*u1-1)*half;
                    float y0=aperture-rise+rise*Profile(2*u0-1,kind);
                    float y1=aperture-rise+rise*Profile(2*u1-1,kind);
                    for(int side=-1;side<=1;side+=2)
                    {
                        V3 n=new V3(0,0,side);
                        part.QuadUV(new V3(a,y0,t*side),new V3(b,y1,t*side),new V3(b,height,t*side),new V3(a,height,t*side),
                            n,n,n,n,new V2(a/uv,y0/uv),new V2(b/uv,y1/uv),new V2(b/uv,height/uv),new V2(a/uv,height/uv),Surface.Tile);
                    }
                    V3 reveal=new V3(y1-y0,a-b,0).Unit;
                    part.Quad(new V3(a,y0,-t),new V3(b,y1,-t),new V3(b,y1,t),new V3(a,y0,t),reveal,Surface.Tile,uv);
                    part.Quad(new V3(a,height,-t),new V3(b,height,-t),new V3(b,height,t),new V3(a,height,t),V3.Up,Surface.Tile,uv);
                }
            }
            part.TranslateRotate(new V3(x,0,z),yaw);m.Append(part);
        }
        private static float Profile(float x,int kind)
        { return kind==2?1-Math.Abs(x): (float)Math.Sqrt(Math.Max(0,1-x*x)); }

        public static void Beam(MeshData m,V3 a,V3 b,float radius,Surface material,float uv,int sides=8)
        {
            V3 axis=(b-a).Unit;
            V3 right=V3.Cross(axis,Math.Abs(axis.Y)>.9f?new V3(1,0,0):V3.Up).Unit;
            V3 up=V3.Cross(axis,right).Unit;
            for(int i=0;i<sides;i++)
            {
                float p=i*2*Shapes.Pi/sides,q=(i+1)*2*Shapes.Pi/sides;
                V3 n0=right*Shapes.Cos(p)+up*Shapes.Sin(p),n1=right*Shapes.Cos(q)+up*Shapes.Sin(q);
                m.QuadUV(a+n0*radius,a+n1*radius,b+n1*radius,b+n0*radius,n0,n1,n1,n0,
                    new V2(p*radius/uv,0),new V2(q*radius/uv,0),new V2(q*radius/uv,(b-a).Length/uv),new V2(p*radius/uv,(b-a).Length/uv),material);
                m.Triangle(a,a+n1*radius,a+n0*radius,axis*-1,axis*-1,axis*-1,new V2(),new V2(1,0),new V2(0,1),material);
                m.Triangle(b,b+n0*radius,b+n1*radius,axis,axis,axis,new V2(),new V2(1,0),new V2(0,1),material);
            }
        }

        public static void Ramp(MeshData m,float x,float z,float width,float length,float rise,float yaw,float uv)
        {
            var part=new MeshData();float w=width*.5f,l=length*.5f;
            V3 a=new V3(-w,0,-l),b=new V3(w,0,-l),c=new V3(w,rise,l),d=new V3(-w,rise,l);
            part.Quad(a,b,c,d,new V3(0,length,-rise).Unit,Surface.Tile,uv);
            part.Quad(a,b,new V3(w,0,l),new V3(-w,0,l),V3.Up*-1,Surface.Tile,uv);
            part.Quad(new V3(-w,0,l),new V3(w,0,l),c,d,new V3(0,0,1),Surface.Accent,uv);
            part.Triangle(a,new V3(-w,0,l),d,new V3(-1,0,0),new V3(-1,0,0),new V3(-1,0,0),new V2(),new V2(length/uv,0),new V2(length/uv,rise/uv),Surface.Tile);
            part.Triangle(b,c,new V3(w,0,l),new V3(1,0,0),new V3(1,0,0),new V3(1,0,0),new V2(),new V2(length/uv,rise/uv),new V2(length/uv,0),Surface.Tile);
            part.TranslateRotate(new V3(x,0,z),yaw);m.Append(part);
        }

        public static void Vault(MeshData m,float x,float z,float radius,float length,float spring,float yaw,int segments,float uv)
        {
            var part=new MeshData();segments=Math.Max(12,segments);const float thickness=.22f;
            for(int i=0;i<segments;i++)
            {
                float a=i*Shapes.Pi/segments,b=(i+1)*Shapes.Pi/segments;
                for(int shell=0;shell<2;shell++)
                {
                    float r=radius+shell*thickness,sign=shell==0?-1:1;
                    V3 na=new V3(Shapes.Cos(a),Shapes.Sin(a),0)*sign,nb=new V3(Shapes.Cos(b),Shapes.Sin(b),0)*sign;
                    part.QuadUV(Arch(a,r,spring,-length*.5f),Arch(b,r,spring,-length*.5f),Arch(b,r,spring,length*.5f),Arch(a,r,spring,length*.5f),
                        na,nb,nb,na,new V2(a*r/uv,0),new V2(b*r/uv,0),new V2(b*r/uv,length/uv),new V2(a*r/uv,length/uv),Surface.Tile);
                }
                for(int end=-1;end<=1;end+=2)
                    part.Quad(Arch(a,radius,spring,end*length*.5f),Arch(b,radius,spring,end*length*.5f),
                        Arch(b,radius+thickness,spring,end*length*.5f),Arch(a,radius+thickness,spring,end*length*.5f),new V3(0,0,end),Surface.Tile,uv);
            }
            for(int side=-1;side<=1;side+=2)
                Box(part,side*(radius+thickness*.5f),0,thickness,length,spring-.12f,spring,Surface.Tile,uv);
            part.TranslateRotate(new V3(x,0,z),yaw);m.Append(part);
        }
        private static V3 Arch(float angle,float radius,float spring,float z)
        { return new V3(Shapes.Cos(angle)*radius,spring+Shapes.Sin(angle)*radius,z); }
    }
}
