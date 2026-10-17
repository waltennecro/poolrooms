using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct DressingAnchor
    {
        public readonly V3 Center,Normal,A,B,C;
        public readonly int Motif,Variant;
        public DressingAnchor(V3 center,V3 normal,V3 a,V3 b,V3 c,int motif,int variant)
        {Center=center;Normal=normal;A=a;B=b;C=c;Motif=motif;Variant=variant;}
    }
    public sealed class DressingGeometry
    {
        public readonly MeshData First=new MeshData{Theme=SurfaceTheme.CityFacade},Second=new MeshData{Theme=SurfaceTheme.CityFacade};
        public readonly List<DressingAnchor> Anchors=new List<DressingAnchor>();
    }
    // Thin architectural fittings only: no support, collision, lamps, probes or rigidbodies.
    // Each footprint must fit inside one actual wall triangle, including its four corners.
    public static class ArchitecturalDressing
    {
        public const int MaximumAnchors=24,MaximumTriangles=5184;
        public const float HalfWidth=.28f,HalfHeight=.21f;
        public static DressingGeometry Build(MeshData walls,uint seed)
        {
            var output=new DressingGeometry();var eligible=new List<(int Surface,int Offset)>();
            for(int surface=0;surface<walls.Indices.Length;surface++)
            {
                if(surface==(int)Surface.Glow||surface==(int)Surface.Slide)continue;
                var indices=walls.Indices[surface];
                for(int i=0;i<indices.Count;i+=3)
                {
                    V3 a=walls.Positions[indices[i]],b=walls.Positions[indices[i+1]],c=walls.Positions[indices[i+2]];
                    V3 cross=V3.Cross(b-a,c-a);if(cross.Length<2||Math.Abs(cross.Unit.Y)>.005f)continue;
                    if(Math.Max(a.Y,Math.Max(b.Y,c.Y))-Math.Min(a.Y,Math.Min(b.Y,c.Y))<2)continue;
                    eligible.Add((surface,i));
                }
            }
            if(eligible.Count==0)return output;var random=new StableRandom(seed);
            // Fixed attempt budget; scan eligible triangles once, never a wall search per fitting.
            for(int attempt=0;attempt<768&&output.Anchors.Count<MaximumAnchors;attempt++)
            {
                var candidate=eligible[random.Range(eligible.Count)];var list=walls.Indices[candidate.Surface];int t=candidate.Offset;
                V3 a=walls.Positions[list[t]],b=walls.Positions[list[t+1]],c=walls.Positions[list[t+2]],normal=V3.Cross(b-a,c-a).Unit;
                float u=random.Value(),v=random.Value();if(u+v>1){u=1-u;v=1-v;}
                V3 center=a+(b-a)*u+(c-a)*v;
                float low=Math.Min(a.Y,Math.Min(b.Y,c.Y));if(center.Y<low+.65f||center.Y>low+2.7f)continue;
                if(!FootprintFits(center,normal,a,b,c))continue;
                bool overlaps=false;foreach(var prior in output.Anchors)if((prior.Center-center).Length<1.1f){overlaps=true;break;}
                if(overlaps)continue;
                int motif=random.Range(8),variant=random.Range(2);var anchor=new DressingAnchor(center,normal,a,b,c,motif,variant);output.Anchors.Add(anchor);
                MeshData target=output.Anchors.Count<=12?output.First:output.Second;var local=Fitting(motif,variant);
                V3 right=new V3(normal.Z,0,-normal.X);
                for(int i=0;i<local.Positions.Count;i++)
                {
                    V3 p=local.Positions[i];local.Positions[i]=center+normal*.008f+right*p.X+V3.Up*p.Y+normal*p.Z;
                    p=local.Normals[i];local.Normals[i]=right*p.X+V3.Up*p.Y+normal*p.Z;
                }
                target.Append(local);
            }
            if(output.First.TriangleCount+output.Second.TriangleCount>MaximumTriangles)throw new InvalidOperationException("Dressing budget exceeded.");
            return output;
        }
        public static bool FootprintFits(V3 center,V3 normal,V3 a,V3 b,V3 c)
        {
            V3 right=new V3(normal.Z,0,-normal.X);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
                if(!Inside(center+right*(x*HalfWidth)+V3.Up*(y*HalfHeight),a,b,c))return false;
            return true;
        }
        static bool Inside(V3 p,V3 a,V3 b,V3 c)
        {
            V3 v0=b-a,v1=c-a,v2=p-a;float d00=V3.Dot(v0,v0),d01=V3.Dot(v0,v1),d11=V3.Dot(v1,v1),d20=V3.Dot(v2,v0),d21=V3.Dot(v2,v1),den=d00*d11-d01*d01;
            if(den<1e-6f)return false;float u=(d11*d20-d01*d21)/den,v=(d00*d21-d01*d20)/den;
            return u>=.0001f&&v>=.0001f&&u+v<=.9999f;
        }
        static void Box(MeshData m,float x,float y,float z,float w,float h,float d,Surface surface=Surface.Metal)
        {m.Box(new V3(x-w*.5f,y-h*.5f,z-d*.5f),new V3(x+w*.5f,y+h*.5f,z+d*.5f),surface,.5f);}
        public static MeshData Fitting(int motif,int variant)
        {
            if(motif<0||motif>=8||variant<0||variant>1)throw new ArgumentOutOfRangeException();
            var m=new MeshData();Box(m,0,0,.012f,.55f,.41f,.024f,Surface.Plaster);
            // Inset frame, with visible depth rather than a flat texture decal.
            foreach(int sign in new[]{-1,1}){Box(m,sign*.26f,0,.035f,.025f,.4f,.032f);Box(m,0,sign*.19f,.035f,.5f,.025f,.032f);}
            switch(motif)
            {
                case 0: // Air supply with horizontal or vertical louvers.
                    for(int i=0;i<6;i++)if(variant==0)Box(m,0,-.14f+i*.055f,.043f,.47f,.022f,.025f);else Box(m,-.2f+i*.08f,0,.043f,.025f,.32f,.025f);break;
                case 1: // Return grille with cross braces.
                    for(int i=0;i<5;i++)Box(m,-.19f+i*.095f,0,.038f,.012f,.32f,.018f);
                    for(int i=0;i<3+variant;i++)Box(m,0,-.13f+i*(.26f/(2+variant)),.052f,.47f,.012f,.018f);break;
                case 2: // Inspection hatch, offset hinges and recessed latch.
                    Box(m,0,0,.036f,.44f,.31f,.025f,Surface.Plaster);
                    foreach(int sign in new[]{-1,1})Box(m,(variant==0?-1:1)*.22f,sign*.1f,.057f,.035f,.065f,.028f);
                    Box(m,(variant==0?1:-1)*.15f,0,.057f,.035f,.08f,.025f);break;
                case 3: // Electrical junction enclosure, two fixing straps.
                    Box(m,0,0,.06f,.35f,.25f,.08f,Surface.Plaster);
                    foreach(int sign in new[]{-1,1})Box(m,sign*.2f,0,.05f,.035f,.25f,.025f);
                    Box(m,0,variant==0?.05f:-.05f,.108f,.22f,.018f,.016f);break;
                case 4: // Small access manifold with protective bars.
                    for(int i=0;i<3;i++){Box(m,-.15f+i*.15f,0,.05f,.045f,.31f,.045f);Box(m,-.15f+i*.15f,variant==0?.08f:-.08f,.083f,.09f,.025f,.025f);}break;
                case 5: // Cable service plate with two or three conduit runs.
                    for(int i=0;i<2+variant;i++)Box(m,-.15f+i*.15f,0,.05f,.035f,.32f,.035f);
                    foreach(int sign in new[]{-1,1})Box(m,0,sign*.1f,.075f,.44f,.025f,.02f);break;
                case 6: // Louvered service grille and dividing rail.
                    for(int i=0;i<5;i++)Box(m,0,-.12f+i*.06f,.045f,.45f,.02f,.026f);
                    Box(m,variant==0?-.12f:.12f,0,.063f,.022f,.32f,.018f);break;
                case 7: // Wall protection panel with spaced ribbing.
                    for(int i=0;i<4;i++)if(variant==0)Box(m,-.18f+i*.12f,0,.045f,.065f,.31f,.025f,Surface.Accent);else Box(m,0,-.12f+i*.08f,.045f,.45f,.035f,.025f);break;
            }
            // Four modeled screw heads remain within the mounting footprint.
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})Box(m,x*.23f,y*.16f,.06f,.018f,.018f,.012f);
            return m;
        }
    }
}
