using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Poolrooms.Core
{
    // Bounded static OBJ reader. Filesystem, textures and Unity objects belong to the runtime adapter.
    public sealed class AvatarObj
    {
        public const int MaxBytes=64*1024*1024,MaxVertices=150000,MaxTriangles=200000,MaxMaterials=16;
        public readonly List<V3> Positions=new List<V3>(),Normals=new List<V3>();
        public readonly List<V2> UV=new List<V2>();
        public readonly List<List<int>> Submeshes=new List<List<int>>();
        public readonly List<string> Materials=new List<string>(),Libraries=new List<string>();
        public bool MissingNormals;
        static float Number(string value)
        {
            if(!float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out float n)||float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>1000000)throw new FormatException("Coordenada OBJ no válida.");
            return n;
        }
        static int Index(string s,int count)
        {
            if(!int.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out int n)||n==0)throw new FormatException("Índice OBJ no válido.");
            long result=n>0?(long)n-1:(long)count+n;
            if(result<0||result>=count)throw new FormatException("Índice OBJ fuera del modelo.");return (int)result;
        }
        public static AvatarObj Parse(string source)
        {
            if(source==null||source.Length>MaxBytes)throw new FormatException("El OBJ supera el límite de 64 MB.");
            var result=new AvatarObj();var vertices=new List<V3>();var normals=new List<V3>();var uv=new List<V2>();
            var corners=new Dictionary<(int,int,int),int>();int material=-1,triangles=0;
            using var reader=new StringReader(source);string line;
            while((line=reader.ReadLine())!=null)
            {
                if(line.Length>16384)throw new FormatException("Línea OBJ demasiado larga.");
                int comment=line.IndexOf('#');if(comment>=0)line=line.Substring(0,comment);
                var words=line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries);if(words.Length==0)continue;
                switch(words[0])
                {
                    case "v":case "vn":
                        if(words.Length<4)throw new FormatException("Vértice OBJ incompleto.");
                        var list=words[0]=="v"?vertices:normals;
                        if(list.Count>=MaxVertices)throw new FormatException("Demasiados vértices OBJ.");
                        list.Add(new V3(Number(words[1]),Number(words[2]),Number(words[3])));break;
                    case "vt":
                        if(words.Length<3||uv.Count>=MaxVertices)throw new FormatException("UV OBJ no válido.");
                        uv.Add(new V2(Number(words[1]),Number(words[2])));break;
                    case "mtllib":
                        if(result.Libraries.Count<4&&words.Length>1)result.Libraries.Add(line.Substring(line.IndexOf(words[1],StringComparison.Ordinal)).Trim());break;
                    case "usemtl":
                        string name=words.Length>1?line.Substring(line.IndexOf(words[1],StringComparison.Ordinal)).Trim():"Default";
                        material=result.Materials.IndexOf(name);
                        if(material<0){if(result.Materials.Count>=MaxMaterials)throw new FormatException("Máximo 16 materiales por OBJ.");result.Materials.Add(name);result.Submeshes.Add(new List<int>());material=result.Materials.Count-1;}break;
                    case "f":
                        int count=words.Length-1;if(count<3||count>64)throw new FormatException("Las caras OBJ deben tener entre 3 y 64 vértices.");
                        if(material<0){result.Materials.Add("Default");result.Submeshes.Add(new List<int>());material=0;}
                        var face=new List<int>(count);
                        for(int i=1;i<words.Length;i++)
                        {
                            var p=words[i].Split('/');if(p.Length>3)throw new FormatException("Cara OBJ no válida.");
                            int a=Index(p[0],vertices.Count),b=p.Length>1&&p[1].Length>0?Index(p[1],uv.Count):-1,c=p.Length>2&&p[2].Length>0?Index(p[2],normals.Count):-1;
                            if(!corners.TryGetValue((a,b,c),out int index))
                            {
                                index=result.Positions.Count;if(index>=MaxVertices)throw new FormatException("Demasiadas esquinas OBJ.");
                                corners.Add((a,b,c),index);result.Positions.Add(vertices[a]);result.UV.Add(b>=0?uv[b]:new V2());
                                result.Normals.Add(c>=0?normals[c].Unit:new V3());result.MissingNormals|=c<0;
                            }
                            face.Add(index);
                        }
                        Triangulate(face,result.Positions,result.Submeshes[material]);triangles+=count-2;
                        if(triangles>MaxTriangles)throw new FormatException("El OBJ supera 200.000 triángulos.");break;
                }
            }
            if(triangles==0)throw new FormatException("El OBJ no contiene caras.");return result;
        }
        static void Triangulate(List<int> face,List<V3> points,List<int> output)
        {
            V3 normal=new V3();for(int i=0;i<face.Count;i++)
            {var a=points[face[i]];var b=points[face[(i+1)%face.Count]];normal.X+=(a.Y-b.Y)*(a.Z+b.Z);normal.Y+=(a.Z-b.Z)*(a.X+b.X);normal.Z+=(a.X-b.X)*(a.Y+b.Y);}
            if(normal.Length<1e-8f)throw new FormatException("Cara OBJ degenerada.");
            int axis=Math.Abs(normal.X)>Math.Abs(normal.Y)?0:1;if(Math.Abs(normal.Z)>(axis==0?Math.Abs(normal.X):Math.Abs(normal.Y)))axis=2;
            Func<int,V2> project=id=>{var v=points[id];return axis==0?new V2(v.Y,v.Z):axis==1?new V2(v.X,v.Z):new V2(v.X,v.Y);};
            float area=0;for(int i=0;i<face.Count;i++){var a=project(face[i]);var b=project(face[(i+1)%face.Count]);area+=a.X*b.Y-a.Y*b.X;}
            float sign=area>0?1:-1;
            while(face.Count>3)
            {
                bool clipped=false;
                for(int i=0;i<face.Count;i++)
                {
                    int prev=(i+face.Count-1)%face.Count,next=(i+1)%face.Count;var a=project(face[prev]);var b=project(face[i]);var c=project(face[next]);
                    if(Cross(a,b,c)*sign<=1e-9f)continue;bool inside=false;
                    for(int j=0;j<face.Count;j++)if(j!=prev&&j!=i&&j!=next)
                    {var p=project(face[j]);if(Cross(a,b,p)*sign>=-1e-9f&&Cross(b,c,p)*sign>=-1e-9f&&Cross(c,a,p)*sign>=-1e-9f){inside=true;break;}}
                    if(inside)continue;output.Add(face[prev]);output.Add(face[i]);output.Add(face[next]);face.RemoveAt(i);clipped=true;break;
                }
                if(!clipped)throw new FormatException("Cara OBJ cruzada o no triangulable. Exporta el modelo triangulado.");
            }
            if(V3.Cross(points[face[1]]-points[face[0]],points[face[2]]-points[face[0]]).Length<1e-9f)throw new FormatException("Triángulo OBJ degenerado.");
            output.AddRange(face);
        }
        static float Cross(V2 a,V2 b,V2 c){return (b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);}
    }
}
