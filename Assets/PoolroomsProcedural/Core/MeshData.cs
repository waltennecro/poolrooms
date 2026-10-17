using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public struct V2
    {
        public float X, Y;
        public V2(float x, float y) { X = x; Y = y; }
    }
    public struct V3
    {
        public float X, Y, Z;
        public V3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static V3 operator +(V3 a, V3 b) { return new V3(a.X+b.X,a.Y+b.Y,a.Z+b.Z); }
        public static V3 operator -(V3 a, V3 b) { return new V3(a.X-b.X,a.Y-b.Y,a.Z-b.Z); }
        public static V3 operator *(V3 a, float s) { return new V3(a.X*s,a.Y*s,a.Z*s); }
        public static V3 Cross(V3 a,V3 b) { return new V3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X); }
        public static float Dot(V3 a,V3 b) { return a.X*b.X+a.Y*b.Y+a.Z*b.Z; }
        public float Length { get { return (float)Math.Sqrt(Dot(this,this)); } }
        public V3 Unit { get { float n=Length; return n < 1e-8f ? new V3(0,1,0) : this*(1/n); } }
        public static readonly V3 Up = new V3(0,1,0);
    }
    public enum Surface { Tile, Accent, Plaster, Metal, Glow, Slide }

    // All dimensions and UVs are in metres. The Unity adapter alone owns engine objects.
    public sealed class MeshData
    {
        public SurfaceTheme Theme;
        public float Weathering;
        public float PlayPalette;
        public float RetailPalette;
        public int PaletteVariant;
        public V3 EmissionTint;
        public float EmissionStrength=-1;
        public readonly List<V3> Positions = new List<V3>();
        public readonly List<V3> Normals = new List<V3>();
        public readonly List<V2> UV = new List<V2>();
        // Optional HDRP LayeredLit weights: R=soil, G/B=unused; alpha supplied as 1 by the adapter.
        public readonly List<V3> BlendWeights = new List<V3>();
        public readonly List<int>[] Indices = new List<int>[6];
        public MeshData() { for(int i=0;i<Indices.Length;i++) Indices[i]=new List<int>(); }
        public int TriangleCount { get { int n=0; foreach(var list in Indices)n+=list.Count/3; return n; } }
        public void Triangle(V3 a,V3 b,V3 c,V3 na,V3 nb,V3 nc,V2 ua,V2 ub,V2 uc,Surface mat)
        {
            V3 cross=V3.Cross(b-a,c-a);
            if (cross.Length < 1e-7f) return;
            if(V3.Dot(cross,na+nb+nc)<0) { Swap(ref b,ref c); Swap(ref nb,ref nc); Swap(ref ub,ref uc); }
            int start=Positions.Count;
            if(BlendWeights.Count>0){BlendWeights.Add(new V3());BlendWeights.Add(new V3());BlendWeights.Add(new V3());}
            Positions.Add(a);Positions.Add(b);Positions.Add(c);
            Normals.Add(na);Normals.Add(nb);Normals.Add(nc);
            UV.Add(ua);UV.Add(ub);UV.Add(uc);
            Indices[(int)mat].Add(start);Indices[(int)mat].Add(start+1);Indices[(int)mat].Add(start+2);
        }
        private static void Swap<T>(ref T a,ref T b) { T t=a;a=b;b=t; }
        public void Quad(V3 a,V3 b,V3 c,V3 d,V3 n,Surface mat,float uvScale=1)
        {
            float u=(b-a).Length/uvScale,v=(d-a).Length/uvScale;
            QuadUV(a,b,c,d,n,n,n,n,new V2(0,0),new V2(u,0),new V2(u,v),new V2(0,v),mat);
        }
        public void QuadUV(V3 a,V3 b,V3 c,V3 d,V3 na,V3 nb,V3 nc,V3 nd,V2 ua,V2 ub,V2 uc,V2 ud,Surface mat)
        {
            Triangle(a,b,c,na,nb,nc,ua,ub,uc,mat); Triangle(a,c,d,na,nc,nd,ua,uc,ud,mat);
        }
        public void Box(V3 min,V3 max,Surface mat,float uv=1.2f)
        {
            V3 a=new V3(min.X,min.Y,min.Z),b=new V3(max.X,min.Y,min.Z),c=new V3(max.X,min.Y,max.Z),d=new V3(min.X,min.Y,max.Z);
            V3 e=new V3(min.X,max.Y,min.Z),f=new V3(max.X,max.Y,min.Z),g=new V3(max.X,max.Y,max.Z),h=new V3(min.X,max.Y,max.Z);
            Quad(a,b,c,d,new V3(0,-1,0),mat,uv);Quad(e,f,g,h,V3.Up,mat,uv);
            Quad(a,b,f,e,new V3(0,0,-1),mat,uv);Quad(d,c,g,h,new V3(0,0,1),mat,uv);
            Quad(a,d,h,e,new V3(-1,0,0),mat,uv);Quad(b,c,g,f,new V3(1,0,0),mat,uv);
        }
        public void TranslateRotate(V3 offset, float yaw)
        {
            float c=(float)Math.Cos(yaw),s=(float)Math.Sin(yaw);
            for(int i=0;i<Positions.Count;i++)
            {
                V3 p=Positions[i];Positions[i]=new V3(c*p.X+s*p.Z,p.Y,-s*p.X+c*p.Z)+offset;
                p=Normals[i];Normals[i]=new V3(c*p.X+s*p.Z,p.Y,-s*p.X+c*p.Z);
            }
        }
        public void Append(MeshData other)
        {
            int offset=Positions.Count;
            if(BlendWeights.Count>0||other.BlendWeights.Count>0)
            {
                while(BlendWeights.Count<offset)BlendWeights.Add(new V3());
                if(other.BlendWeights.Count==other.Positions.Count)BlendWeights.AddRange(other.BlendWeights);
                else for(int v=0;v<other.Positions.Count;v++)BlendWeights.Add(new V3());
            }
            Positions.AddRange(other.Positions);Normals.AddRange(other.Normals);UV.AddRange(other.UV);
            for(int m=0;m<Indices.Length;m++) foreach(int i in other.Indices[m])Indices[m].Add(i+offset);
        }
    }
}
