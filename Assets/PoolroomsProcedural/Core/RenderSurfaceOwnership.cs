using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Upload-time surface ownership, deliberately separate from generation/physics.
    // Later opaque finishes own their covered area. Only coincident, equally facing
    // cardinal rectangles and exact duplicate triangles qualify; no depth bias.
    public static class RenderSurfaceOwnership
    {
        const float Epsilon=.00001f,Grid=4;
        const int MaxCells=256;
        sealed class Face
        {
            public int Mesh,Surface,Start,Count,Axis,Sign;
            public float Plane;
            public PlanRect Rect;
            public List<PlanRect> Parts;
        }
        sealed class PlaneBucket
        {
            public readonly List<int> All=new List<int>(),Large=new List<int>();
            public readonly Dictionary<(int,int),List<int>> Cells=new Dictionary<(int,int),List<int>>();
        }
        public static bool IsOpaque(MeshData m)
        {
            var t=BiomeSurfaces.Resolve(m);
            return t!=SurfaceTheme.Forest39CanopyLeaves&&t!=SurfaceTheme.Glass&&t!=SurfaceTheme.CygnusGlass&&t!=SurfaceTheme.SlideGlass&&
                t!=SurfaceTheme.FlumeFlow&&t!=SurfaceTheme.SoftPlayNet;
        }
        public static MeshData[] Resolve(params MeshData[] sources)
        {
            var result=(MeshData[])sources.Clone();var faces=new List<Face>();
            for(int m=0;m<sources.Length;m++)
            {
                var source=sources[m];if(source==null||!IsOpaque(source))continue;
                for(int s=0;s<source.Indices.Length;s++)
                {
                    var list=source.Indices[s];
                    for(int i=0;i<list.Count;)
                    {
                        var f=new Face{Mesh=m,Surface=s,Start=i,Count=3,Axis=-1};
                        if(i+5<list.Count&&Rectangle(source,list,i,f))f.Count=6;
                        faces.Add(f);i+=f.Count;
                    }
                }
            }
            var planes=new Dictionary<(int,int,int),PlaneBucket>();
            var candidates=new HashSet<int>();var ordered=new List<int>();
            // Last finish wins, independently of material/draw order on the GPU.
            for(int i=faces.Count-1;i>=0;i--)
            {
                var f=faces[i];if(f.Axis<0)continue;
                int plane=PlaneId(f.Plane);candidates.Clear();
                for(int delta=-1;delta<=1;delta++)
                    if(planes.TryGetValue((f.Axis,f.Sign,plane+delta),out var bucket))Query(bucket,f.Rect,candidates);
                ordered.Clear();ordered.AddRange(candidates);ordered.Sort();
                List<PlanRect> pieces=null;
                foreach(int j in ordered)
                {
                    var cover=faces[j];if(Math.Abs(cover.Plane-f.Plane)>Epsilon||!Overlaps(f.Rect,cover.Rect))continue;
                    if(pieces==null)pieces=new List<PlanRect>{f.Rect};
                    Subtract(pieces,cover.Rect);
                    if(pieces.Count==0)break;
                }
                f.Parts=pieces;
                var key=(f.Axis,f.Sign,plane);
                if(!planes.TryGetValue(key,out var own)){own=new PlaneBucket();planes.Add(key,own);}
                Insert(own,f.Rect,i);
            }
            // Exact non-rectangular duplicate triangles also compete, including in
            // rotated arches. Opposite winding is retained for two-sided geometry.
            var seen=new HashSet<((float,float,float),(float,float,float),(float,float,float))>();var dropped=new HashSet<(int,int,int)>();
            for(int i=faces.Count-1;i>=0;i--)
            {
                var f=faces[i];var source=sources[f.Mesh];var indices=source.Indices[f.Surface];
                if(f.Parts!=null)continue;
                for(int t=f.Start+f.Count-3;t>=f.Start;t-=3)
                {
                    V3 a=source.Positions[indices[t]],b=source.Positions[indices[t+1]],c=source.Positions[indices[t+2]];
                    if(Less(b,a)&&Less(b,c)){var first=a;a=b;b=c;c=first;}
                    else if(Less(c,a)&&Less(c,b)){var first=a;a=c;c=b;b=first;}
                    if(!seen.Add(((a.X,a.Y,a.Z),(b.X,b.Y,b.Z),(c.X,c.Y,c.Z))))dropped.Add((f.Mesh,f.Surface,t));
                }
            }
            var changed=new bool[sources.Length];
            foreach(var f in faces)if(f.Parts!=null)changed[f.Mesh]=true;
            foreach(var t in dropped)changed[t.Item1]=true;
            for(int m=0;m<sources.Length;m++)if(changed[m])result[m]=CopyVertices(sources[m]);
            foreach(var f in faces)
            {
                if(!changed[f.Mesh])continue;
                var source=sources[f.Mesh];var output=result[f.Mesh];var indices=source.Indices[f.Surface];
                if(f.Parts==null)
                {
                    for(int t=f.Start;t<f.Start+f.Count;t+=3)if(!dropped.Contains((f.Mesh,f.Surface,t)))
                    {output.Indices[f.Surface].Add(indices[t]);output.Indices[f.Surface].Add(indices[t+1]);output.Indices[f.Surface].Add(indices[t+2]);}
                }
                else foreach(var part in f.Parts)Emit(output,source,indices,f,part);
            }
            return result;
        }
        static int PlaneId(float plane){return (int)Math.Round(plane/(Epsilon*4));}
        static bool Less(V3 a,V3 b){return a.X<b.X||(a.X==b.X&&(a.Y<b.Y||(a.Y==b.Y&&a.Z<b.Z)));}
        static float Component(V3 v,int axis){return axis==0?v.X:axis==1?v.Y:v.Z;}
        static bool Rectangle(MeshData m,List<int> t,int start,Face f)
        {
            V3 origin=m.Positions[t[start]];int axis=-1;
            for(int a=0;a<3;a++)
            {
                bool constant=true;
                for(int i=0;i<6;i++)if(Math.Abs(Component(m.Positions[t[start+i]],a)-Component(origin,a))>Epsilon||Math.Abs(Component(m.Normals[t[start+i]],a))<.99999f){constant=false;break;}
                if(constant){axis=a;break;}
            }
            if(axis<0)return false;
            int u=(axis+1)%3,v=(axis+2)%3;float loU=float.PositiveInfinity,loV=loU,hiU=float.NegativeInfinity,hiV=hiU;
            for(int i=0;i<6;i++){var p=m.Positions[t[start+i]];loU=Math.Min(loU,Component(p,u));hiU=Math.Max(hiU,Component(p,u));loV=Math.Min(loV,Component(p,v));hiV=Math.Max(hiV,Component(p,v));}
            if(hiU-loU<=Epsilon||hiV-loV<=Epsilon)return false;
            int corners=0,firstMask=0,secondMask=0,sign=Math.Sign(Component(m.Normals[t[start]],axis));
            for(int i=0;i<6;i++)
            {
                var p=m.Positions[t[start+i]];float x=Component(p,u),y=Component(p,v);
                if(Math.Sign(Component(m.Normals[t[start+i]],axis))!=sign)return false;
                bool right=Math.Abs(x-hiU)<=Epsilon,top=Math.Abs(y-hiV)<=Epsilon;
                if(!right&&Math.Abs(x-loU)>Epsilon||!top&&Math.Abs(y-loV)>Epsilon)return false;
                int bit=1<<((right?1:0)+(top?2:0));corners|=bit;if(i<3)firstMask|=bit;else secondMask|=bit;
            }
            int shared=firstMask&secondMask;
            if(corners!=15||(shared!=9&&shared!=6)||!AffineAttributes(m,t,start,axis,loU,loV,hiU,hiV))return false;
            float area=0;
            for(int j=0;j<6;j+=3)
            {
                var a=m.Positions[t[start+j]];var b=m.Positions[t[start+j+1]];var c=m.Positions[t[start+j+2]];
                var cross=V3.Cross(b-a,c-a);if(Component(cross,axis)*sign<=0)return false;area+=cross.Length*.5f;
            }
            float expected=(hiU-loU)*(hiV-loV);
            if(Math.Abs(area-expected)>Epsilon*(hiU-loU+hiV-loV+1))return false;
            f.Axis=axis;f.Sign=sign;f.Plane=Component(origin,axis);f.Rect=new PlanRect(loU,loV,hiU,hiV);return true;
        }
        static bool AffineAttributes(MeshData m,List<int> t,int start,int axis,float loU,float loV,float hiU,float hiV)
        {
            Span<int> ids=stackalloc int[4];for(int i=0;i<4;i++)ids[i]=-1;
            for(int i=0;i<6;i++)
            {
                int id=t[start+i];var p=m.Positions[id];int corner=(Math.Abs(Component(p,(axis+1)%3)-hiU)<=Epsilon?1:0)+(Math.Abs(Component(p,(axis+2)%3)-hiV)<=Epsilon?2:0);
                if(ids[corner]>=0)
                {
                    int old=ids[corner];if(Math.Abs(m.UV[id].X-m.UV[old].X)>Epsilon||Math.Abs(m.UV[id].Y-m.UV[old].Y)>Epsilon)return false;
                    if(m.BlendWeights.Count==m.Positions.Count&&(m.BlendWeights[id]-m.BlendWeights[old]).Length>Epsilon)return false;
                }
                ids[corner]=id;
            }
            V2 a=m.UV[ids[0]],b=m.UV[ids[1]],c=m.UV[ids[2]],d=m.UV[ids[3]];
            if(Math.Abs(a.X+d.X-b.X-c.X)>Epsilon||Math.Abs(a.Y+d.Y-b.Y-c.Y)>Epsilon)return false;
            if(m.BlendWeights.Count==m.Positions.Count&&(m.BlendWeights[ids[0]]+m.BlendWeights[ids[3]]-m.BlendWeights[ids[1]]-m.BlendWeights[ids[2]]).Length>Epsilon)return false;
            return true;
        }
        static bool Overlaps(PlanRect a,PlanRect b)
        {return Math.Min(a.MaxX,b.MaxX)-Math.Max(a.MinX,b.MinX)>Epsilon&&Math.Min(a.MaxZ,b.MaxZ)-Math.Max(a.MinZ,b.MinZ)>Epsilon;}
        static void Subtract(List<PlanRect> pieces,PlanRect cut)
        {
            for(int i=pieces.Count-1;i>=0;i--)
            {
                var p=pieces[i];if(!Overlaps(p,cut))continue;pieces.RemoveAt(i);
                float x0=Math.Max(p.MinX,cut.MinX),x1=Math.Min(p.MaxX,cut.MaxX),y0=Math.Max(p.MinZ,cut.MinZ),y1=Math.Min(p.MaxZ,cut.MaxZ);
                void Add(float a,float b,float c,float d){if(c-a>Epsilon&&d-b>Epsilon)pieces.Add(new PlanRect(a,b,c,d));}
                Add(p.MinX,p.MinZ,x0,p.MaxZ);Add(x1,p.MinZ,p.MaxX,p.MaxZ);Add(x0,p.MinZ,x1,y0);Add(x0,y1,x1,p.MaxZ);
            }
        }
        static void Bounds(PlanRect r,out int x0,out int x1,out int y0,out int y1)
        {x0=(int)Math.Floor(r.MinX/Grid);x1=(int)Math.Floor(r.MaxX/Grid);y0=(int)Math.Floor(r.MinZ/Grid);y1=(int)Math.Floor(r.MaxZ/Grid);}
        static bool Large(int x0,int x1,int y0,int y1){return ((long)x1-x0+1)*((long)y1-y0+1)>MaxCells;}
        static void Query(PlaneBucket b,PlanRect r,HashSet<int> output)
        {
            Bounds(r,out int x0,out int x1,out int y0,out int y1);
            if(Large(x0,x1,y0,y1)){foreach(int id in b.All)output.Add(id);return;}
            foreach(int id in b.Large)output.Add(id);
            for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)if(b.Cells.TryGetValue((x,y),out var ids))foreach(int id in ids)output.Add(id);
        }
        static void Insert(PlaneBucket b,PlanRect r,int id)
        {
            b.All.Add(id);Bounds(r,out int x0,out int x1,out int y0,out int y1);
            if(Large(x0,x1,y0,y1)){b.Large.Add(id);return;}
            for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)
            {if(!b.Cells.TryGetValue((x,y),out var ids)){ids=new List<int>();b.Cells.Add((x,y),ids);}ids.Add(id);}
        }
        static MeshData CopyVertices(MeshData s)
        {
            var m=new MeshData{Theme=s.Theme,AquaticColor=s.AquaticColor,FlowProfile=s.FlowProfile,Weathering=s.Weathering,PlayPalette=s.PlayPalette,RetailPalette=s.RetailPalette,PaletteVariant=s.PaletteVariant,EmissionTint=s.EmissionTint,EmissionStrength=s.EmissionStrength};
            m.Positions.AddRange(s.Positions);m.Normals.AddRange(s.Normals);m.UV.AddRange(s.UV);m.BlendWeights.AddRange(s.BlendWeights);return m;
        }
        static void Emit(MeshData output,MeshData source,List<int> indices,Face f,PlanRect part)
        {
            int u=(f.Axis+1)%3,v=(f.Axis+2)%3;
            int Vertex(float x,float y)
            {
                // Interpolate on the original triangle, preserving UV seams and blend
                // weights, including mirrored/rotated UVs on the clipped rectangle.
                for(int k=0;k<6;k+=3)
                {
                    int ia=indices[f.Start+k],ib=indices[f.Start+k+1],ic=indices[f.Start+k+2];
                    V3 a=source.Positions[ia],b=source.Positions[ib],c=source.Positions[ic];
                    float ax=Component(a,u),ay=Component(a,v),bx=Component(b,u),by=Component(b,v),cx=Component(c,u),cy=Component(c,v);
                    float determinant=(by-cy)*(ax-cx)+(cx-bx)*(ay-cy);
                    float wa=((by-cy)*(x-cx)+(cx-bx)*(y-cy))/determinant,wb=((cy-ay)*(x-cx)+(ax-cx)*(y-cy))/determinant,wc=1-wa-wb;
                    if(k==0&&(wa<-.0001f||wb<-.0001f||wc<-.0001f))continue;
                    int id=output.Positions.Count;V3 p=f.Axis==0?new V3(f.Plane,x,y):f.Axis==1?new V3(y,f.Plane,x):new V3(x,y,f.Plane);
                    output.Positions.Add(p);output.Normals.Add((source.Normals[ia]*wa+source.Normals[ib]*wb+source.Normals[ic]*wc).Unit);
                    V2 ta=source.UV[ia],tb=source.UV[ib],tc=source.UV[ic];output.UV.Add(new V2(ta.X*wa+tb.X*wb+tc.X*wc,ta.Y*wa+tb.Y*wb+tc.Y*wc));
                    if(source.BlendWeights.Count==source.Positions.Count)output.BlendWeights.Add(source.BlendWeights[ia]*wa+source.BlendWeights[ib]*wb+source.BlendWeights[ic]*wc);
                    return id;
                }
                throw new InvalidOperationException("Unreachable rectangle interpolation");
            }
            int a0=Vertex(part.MinX,part.MinZ),b0=Vertex(part.MaxX,part.MinZ),c0=Vertex(part.MaxX,part.MaxZ),d0=Vertex(part.MinX,part.MaxZ);
            var t=output.Indices[f.Surface];t.Add(a0);t.Add(f.Sign>0?b0:c0);t.Add(f.Sign>0?c0:b0);t.Add(a0);t.Add(f.Sign>0?c0:d0);t.Add(f.Sign>0?d0:c0);
        }
    }
}
