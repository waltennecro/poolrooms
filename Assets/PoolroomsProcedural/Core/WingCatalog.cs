using System;

namespace Poolrooms.Core
{
    // 144 original volumetric constructions, not 144 downloaded models or palette swaps.
    public static class WingCatalog
    {
        public const int Families=12,Variants=12,Count=Families*Variants;
        public static readonly string[] FamilyNames={"Ángel","Serafín","Cuervo","Demonio","Dragón","Mariposa","Libélula","Robot","Neón","Luz","Cristal","Mecánico"};
        public static readonly string[] Icons={"angel-wings","fluffy-wing","feathered-wing","bat-wing","batwing-emblem","butterfly","dragonfly","steelwing-emblem","lightning-helix","winged-emblem","curly-wing","cog"};
        private static readonly string[] Shapes={"Aurora","Horizonte","Eclipse","Ascenso","Vértice","Cascada","Corona","Cometa","Catedral","Vendaval","Órbita","Infinito"};
        public static int Clamp(int id){return Math.Max(0,Math.Min(Count-1,id));}
        public static string Name(int id){id=Clamp(id);return FamilyNames[id/Variants]+" · "+Shapes[id%Variants];}
        public static V3 Color(int id,bool trim=false)
        {
            int family=Clamp(id)/Variants;
            V3[] palette={new V3(.91f,.87f,.72f),new V3(.92f,.76f,.43f),new V3(.095f,.12f,.20f),new V3(.38f,.045f,.09f),new V3(.09f,.33f,.25f),new V3(.34f,.12f,.64f),new V3(.25f,.64f,.7f),new V3(.3f,.36f,.42f),new V3(.04f,.64f,.8f),new V3(.92f,.78f,.5f),new V3(.4f,.6f,.86f),new V3(.46f,.25f,.1f)};
            return trim?new V3(.12f,.15f,.2f):palette[family];
        }
        public static MeshData Build(int id,bool left=false)
        {
            id=Clamp(id);int family=id/Variants,variant=id%Variants;
            var mesh=new MeshData();
            float span=1.05f+.075f*variant,crest=.18f+.12f*(variant%4),sweep=.06f+.05f*(variant/4);
            if(family==2){span*=1.12f;crest*=.42f;sweep+=.25f;}
            V3 shoulder=new V3(0,0,0),elbow=new V3(span*.43f,crest+.2f,-sweep),tip=new V3(span,crest-.12f,-sweep*.6f);
            Tube(mesh,shoulder,elbow,.045f,.032f,Surface.Metal);Tube(mesh,elbow,tip,.032f,.012f,Surface.Metal);
            if(family<=2||family==8||family==9)
            {
                int layers=family==1?3:family==9?2:1;
                int feathers=9+variant;
                for(int layer=0;layer<layers;layer++)for(int i=0;i<feathers;i++)
                {
                    float t=i/(float)(feathers-1),x=.12f+span*.88f*t;
                    V3 root=new V3(x,crest*(float)Math.Sin(t*Math.PI),-.025f*layer-sweep*t);
                    float length=(.38f+.085f*(variant%3)+.45f*(float)Math.Sin(t*Math.PI*.72f))*(1-.2f*layer);
                    V3 end=root+new V3(.12f+.15f*t,-length,-.08f-.1f*(variant/4));
                    Leaf(mesh,root,end,(span/feathers)*.75f,.026f,family==8||family==9?Surface.Glow:Surface.Tile);
                    // Overlapping coverts make feathered wings have actual depth near the spar.
                    if(layer==0&&family<=2)Leaf(mesh,root+new V3(0,.05f,.032f),root+new V3(.04f,-length*.38f,.035f),span/feathers*.55f,.023f,Surface.Tile);
                }
            }
            else if(family==3||family==4||family==5||family==6)
            {
                int ribs=family==6?4:5+variant%4;
                V3 previous=new V3(.05f,-.3f,0);
                for(int i=0;i<ribs;i++)
                {
                    float t=i/(float)(ribs-1);
                    V3 root=new V3(span*t*.5f,crest*t,-sweep*t);
                    float lobe=family==5?.62f+.25f*(float)Math.Sin(t*Math.PI*3):.52f+.38f*t;
                    V3 end=new V3(.2f+span*t,-lobe+.25f*t,-.12f-.1f*(variant/4));
                    if(family==6)end=new V3(span*(.7f+.3f*(i%2)),i<2?.4f:-.55f,-.1f*(i/2));
                    Tube(mesh,root,end,.023f,.008f,Surface.Metal);
                    // Convex front/back membrane with a recessed scallop between reinforced ribs.
                    V3 notch=(previous+end)*.5f+new V3(-.045f,.12f+.025f*variant,-.045f);
                    Membrane(mesh,root,previous,notch,end,family==6?Surface.Accent:Surface.Tile);
                    previous=end;
                    if(family==4)Leaf(mesh,root,root+new V3(.1f,.18f+.014f*variant,0),.035f,.02f,Surface.Accent);
                }
            }
            else
            {
                int blades=5+variant%6;
                for(int i=0;i<blades;i++)
                {
                    float t=i/(float)(blades-1);V3 root=new V3(.08f+span*.72f*t,crest*t,-sweep*t);
                    V3 end=root+new V3(.2f+.14f*(variant/4),-.38f-.46f*t,-.03f-.02f*(i%3));
                    Leaf(mesh,root,end,span/blades*.36f,family==10?.065f:.04f,Surface.Tile);
                    Tube(mesh,root,root+(end-root)*.8f,.012f,.006f,family==7?Surface.Glow:Surface.Metal);
                    if(family==11)Tube(mesh,new V3(.05f,-.16f,0),root,.018f,.014f,Surface.Metal);
                }
            }
            if(left)
            {
                for(int i=0;i<mesh.Positions.Count;i++){V3 p=mesh.Positions[i],n=mesh.Normals[i];mesh.Positions[i]=new V3(-p.X,p.Y,p.Z);mesh.Normals[i]=new V3(-n.X,n.Y,n.Z);}
                foreach(var indices in mesh.Indices)for(int i=0;i<indices.Count;i+=3){int b=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=b;}
            }
            return mesh;
        }
        private static void Leaf(MeshData m,V3 a,V3 b,float width,float depth,Surface slot)
        {
            const int rings=7,sides=8;
            V3 axis=(b-a).Unit,u=V3.Cross(axis,new V3(0,0,1)).Unit,v=V3.Cross(axis,u).Unit;
            for(int r=0;r<rings;r++)for(int s=0;s<sides;s++)
            {
                float t0=r/(float)rings,t1=(r+1f)/rings,q0=s*(float)Math.PI*2/sides,q1=(s+1f)*(float)Math.PI*2/sides;
                V3 p0=LeafPoint(a,b,u,v,width,depth,t0,q0),p1=LeafPoint(a,b,u,v,width,depth,t1,q0),p2=LeafPoint(a,b,u,v,width,depth,t1,q1),p3=LeafPoint(a,b,u,v,width,depth,t0,q1);
                V3 normal=(u*(float)Math.Cos((q0+q1)*.5f)+v*(float)Math.Sin((q0+q1)*.5f)).Unit;
                m.Quad(p0,p1,p2,p3,normal,slot,.15f);
            }
        }
        private static V3 LeafPoint(V3 a,V3 b,V3 u,V3 v,float w,float d,float t,float angle)
        {
            float shape=(float)Math.Sin(t*Math.PI);shape=(float)Math.Pow(Math.Max(0,shape),.7);
            return a+(b-a)*t+new V3(0,0,.07f*(float)Math.Sin(t*Math.PI))+u*(w*shape*(float)Math.Cos(angle))+v*(d*shape*(float)Math.Sin(angle));
        }
        private static void Tube(MeshData m,V3 a,V3 b,float r0,float r1,Surface slot)
        {
            V3 axis=(b-a).Unit,u=V3.Cross(axis,V3.Up);if(u.Length<.001f)u=V3.Cross(axis,new V3(0,0,1));u=u.Unit;V3 v=V3.Cross(axis,u).Unit;
            for(int s=0;s<8;s++)
            {
                float q=s*(float)Math.PI/4,next=(s+1)*(float)Math.PI/4;
                V3 n=u*(float)Math.Cos(q)+v*(float)Math.Sin(q),nn=u*(float)Math.Cos(next)+v*(float)Math.Sin(next);
                m.QuadUV(a+n*r0,b+n*r1,b+nn*r1,a+nn*r0,n,n,nn,nn,new V2(0,0),new V2(1,0),new V2(1,1),new V2(0,1),slot);
                m.Triangle(a,a+nn*r0,a+n*r0,axis*-1,axis*-1,axis*-1,new V2(),new V2(),new V2(),slot);
                m.Triangle(b,b+n*r1,b+nn*r1,axis,axis,axis,new V2(),new V2(),new V2(),slot);
            }
        }
        private static void Membrane(MeshData m,V3 root,V3 a,V3 notch,V3 b,Surface slot)
        {
            V3 n=V3.Cross(a-root,b-root).Unit;
            foreach(int side in new[]{-1,1})
            {
                V3 shift=n*(side*.008f),normal=n*side;
                m.Triangle(root+shift,a+shift,notch+shift,normal,normal,normal,new V2(.5f,1),new V2(0,0),new V2(.5f,.2f),slot);
                m.Triangle(root+shift,notch+shift,b+shift,normal,normal,normal,new V2(.5f,1),new V2(.5f,.2f),new V2(1,0),slot);
            }
            m.Quad(a+n*.008f,a-n*.008f,notch-n*.008f,notch+n*.008f,(notch-a).Unit,slot);
            m.Quad(notch+n*.008f,notch-n*.008f,b-n*.008f,b+n*.008f,(b-notch).Unit,slot);
        }
    }
}
