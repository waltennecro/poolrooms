using System;
using System.Collections.Generic;
namespace Poolrooms.Core
{
    // Perimeter seam meshes allow neighbouring streamed rooms to share face ownership
    // without retaining or rebuilding their complete architecture on each load.
    public static class RenderBoundary
    {
        public static MeshData[] Split(MeshData[] source,float cellSize,out MeshData[] boundary)
        {
            var interior=(MeshData[])source.Clone();boundary=new MeshData[source.Length];
            for(int m=0;m<source.Length;m++)
            {
                var input=source[m];boundary[m]=Empty(input);
                if(!RenderSurfaceOwnership.IsOpaque(input))continue;
                MeshData inside=null;var edge=boundary[m];float half=cellSize*.5f;
                for(int s=0;s<input.Indices.Length;s++)
                {
                    var t=input.Indices[s];
                    // Copy a whole authored quad together to retain rectangle recognition.
                    for(int start=0;start<t.Count;)
                    {
                        int count=Quad(input,t,start)?6:3;
                        bool seam=false;
                        for(int axis=0;axis<=2;axis+=2)for(int sign=-1;sign<=1;sign+=2)
                        {
                            for(int j=0;j<count;j++)
                            {var p=input.Positions[t[start+j]];float a=axis==0?p.X:p.Z;if(a*sign>=half-.6f){seam=true;break;}}
                        }
                        if(seam)
                        {
                            if(inside==null)
                            {
                                inside=Empty(input);inside.Positions.AddRange(input.Positions);inside.Normals.AddRange(input.Normals);inside.UV.AddRange(input.UV);inside.BlendWeights.AddRange(input.BlendWeights);
                                for(int old=0;old<s;old++)inside.Indices[old].AddRange(input.Indices[old]);
                                for(int old=0;old<start;old++)inside.Indices[s].Add(t[old]);
                            }
                            for(int j=0;j<count;j++)CopyVertex(edge,input,t[start+j],s,new V3());
                        }
                        else if(inside!=null)for(int j=0;j<count;j++)inside.Indices[s].Add(t[start+j]);
                        start+=count;
                    }
                }
                if(inside!=null)interior[m]=inside;
            }
            return interior;
        }
        static bool Quad(MeshData m,List<int> t,int start)
        {
            if(start+5>=t.Count)return false;
            // MeshData.Quad emits two consecutive triangles sharing its diagonal.
            var a=m.Positions[t[start]];var c=m.Positions[t[start+2]];
            return Equal(a,m.Positions[t[start+3]])&&(Equal(c,m.Positions[t[start+4]])||Equal(m.Positions[t[start+1]],m.Positions[t[start+5]]));
        }
        static bool Equal(V3 a,V3 b){return a.X==b.X&&a.Y==b.Y&&a.Z==b.Z;}
        public static MeshData Translate(MeshData input,V3 offset)
        {
            var m=Empty(input);for(int s=0;s<input.Indices.Length;s++)foreach(int index in input.Indices[s])CopyVertex(m,input,index,s,offset);return m;
        }
        public static MeshData Cover(MeshData source,V3 offset,bool winsTie)
        {
            var m=Empty(source);
            for(int s=0;s<source.Indices.Length;s++)
            {
                var t=source.Indices[s];for(int i=0;i<t.Count;i+=3)
                {
                    V3 a=source.Positions[t[i]],b=source.Positions[t[i+1]],c=source.Positions[t[i+2]];
                    // Prefer the room on the visible side of a shared face. Its
                    // interior paint must beat the other room's exterior/back face.
                    float score=V3.Dot(V3.Cross(b-a,c-a).Unit,offset);
                    if(score>.0001f||Math.Abs(score)<=.0001f&&winsTie)
                        for(int j=0;j<3;j++)CopyVertex(m,source,t[i+j],s,offset);
                }
            }
            return m;
        }
        static void CopyVertex(MeshData target,MeshData source,int index,int surface,V3 offset)
        {
            target.Indices[surface].Add(target.Positions.Count);target.Positions.Add(source.Positions[index]+offset);
            target.Normals.Add(source.Normals[index]);target.UV.Add(source.UV[index]);
            if(source.BlendWeights.Count==source.Positions.Count)target.BlendWeights.Add(source.BlendWeights[index]);
        }
        static MeshData Empty(MeshData s)
        {return new MeshData{Theme=s.Theme,AquaticColor=s.AquaticColor,FlowProfile=s.FlowProfile,Weathering=s.Weathering,PlayPalette=s.PlayPalette,RetailPalette=s.RetailPalette,PaletteVariant=s.PaletteVariant,EmissionTint=s.EmissionTint,EmissionStrength=s.EmissionStrength};}
        public static bool HigherPriority(int x,int z,int otherX,int otherZ){return otherX>x||(otherX==x&&otherZ>z);}
        public static MeshData[] Resolve(MeshData[] own,IEnumerable<MeshData> covers)
        {
            var all=new List<MeshData>(own);all.AddRange(covers);var resolved=RenderSurfaceOwnership.Resolve(all.ToArray());
            var result=new MeshData[own.Length];Array.Copy(resolved,result,own.Length);return result;
        }
    }
}
