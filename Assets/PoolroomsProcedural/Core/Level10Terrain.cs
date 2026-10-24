using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class Level10Terrain
    {
        public const float Spacing=.85f;
        public static int Grid(Level10Plan p){return Math.Min(128,Math.Max(36,(int)Math.Ceiling(p.Cell/Spacing)));}
        public static float[] Sample(Level10Plan p)
        {
            int n=Grid(p);float step=p.Cell/n;var values=new float[(n+1)*(n+1)];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)values[z*(n+1)+x]=p.Height(-p.Half+x*step,-p.Half+z*step);
            return values;
        }
        public static MeshData Build(Level10Plan p)
        {
            var mesh=new MeshData{Theme=SurfaceTheme.Level10Ground};int n=Grid(p);float step=p.Cell/n;
            var points=new V3[(n+1)*(n+1)];var normals=new V3[points.Length];var uv=new V2[points.Length];
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {
                int index=z*(n+1)+x;points[index]=new V3(-p.Half+x*step,p.Elevation[index],-p.Half+z*step);uv[index]=UV(p,points[index]);
                normals[index]=x==0||z==0||x==n||z==n?V3.Up:new V3(p.Elevation[index-1]-p.Elevation[index+1],2*step,p.Elevation[index-n-1]-p.Elevation[index+n+1]).Unit;
            }
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                int a=z*(n+1)+x,b=a+1,d=a+n+1,c=d+1;
                Surface material=p.GroundSurface(-p.Half+(x+.5f)*step,-p.Half+(z+.5f)*step);
                mesh.QuadUV(points[a],points[b],points[c],points[d],normals[a],normals[b],normals[c],normals[d],uv[a],uv[b],uv[c],uv[d],material);
            }
            return mesh;
        }
        static V2 UV(Level10Plan p,V3 v){return new V2((float)((((double)p.X*p.Cell)%512+v.X)/3),(float)((((double)p.Z*p.Cell)%512+v.Z)/3));}
        public static float MeshHeight(Level10Plan p,float x,float z)
        {
            int n=Grid(p);float step=p.Cell/n;
            float xx=Math.Max(0,Math.Min(n-.00001f,(x+p.Half)/step)),zz=Math.Max(0,Math.Min(n-.00001f,(z+p.Half)/step));
            int ix=(int)xx,iz=(int)zz;float u=xx-ix,v=zz-iz,ax=-p.Half+ix*step,az=-p.Half+iz*step;
            int index=iz*(n+1)+ix;float a=p.Elevation[index],b=p.Elevation[index+1],c=p.Elevation[index+n+2],d=p.Elevation[index+n+1];
            return u>=v?a*(1-u)+b*(u-v)+c*v:a*(1-v)+c*u+d*(v-u);
        }
        public static void Water(RoomGeometry result,Level10Plan plan)
        {
            if(plan.Lake.Width<=0)return;
            result.Region=plan.Lake;result.Regions.Add(plan.Lake);
            var floor=result.Structure;float level=plan.Lake.Level;
            var clipped=new List<V3>(4);
            foreach(var indices in floor.Indices)for(int i=0;i<indices.Count;i+=3)
            {
                var a=floor.Positions[indices[i]];var b=floor.Positions[indices[i+1]];var c=floor.Positions[indices[i+2]];
                if(a.Y>=level&&b.Y>=level&&c.Y>=level)continue;
                clipped.Clear();Clip(clipped,a,b,level);Clip(clipped,b,c,level);Clip(clipped,c,a,level);
                if(clipped.Count<3)continue;
                for(int j=1;j<clipped.Count-1;j++)
                {
                    V3 aa=clipped[0],bb=clipped[j],cc=clipped[j+1];aa.Y=bb.Y=cc.Y=level;
                    result.Water.Triangle(aa,bb,cc,V3.Up,V3.Up,V3.Up,new V2(aa.X/3,aa.Z/3),new V2(bb.X/3,bb.Z/3),new V2(cc.X/3,cc.Z/3),Surface.Slide);
                }
            }
        }
        static void Clip(List<V3> output,V3 a,V3 b,float y)
        {
            bool insideA=a.Y<=y,insideB=b.Y<=y;
            if(insideA)output.Add(a);
            if(insideA!=insideB)
            {float t=(y-a.Y)/(b.Y-a.Y);output.Add(a+(b-a)*t);}
        }
    }
}
