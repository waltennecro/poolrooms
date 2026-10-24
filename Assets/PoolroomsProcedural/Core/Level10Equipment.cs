using System;

namespace Poolrooms.Core
{
    public static class Level10Equipment
    {
        static void B(MeshData m,float x,float z,float w,float d,float y,float top,Surface s=Surface.Accent)
        {Level10Architecture.Box(m,x,z,w,d,y,top,s);}
        public static V3 Pivot(Level10Action kind)
        {return kind==Level10Action.HandPump?new V3(0,.85f,0):kind==Level10Action.WindVane?new V3(0,1.50f,0):kind==Level10Action.SeedCabinet?new V3(-.39f,.45f,-.34f):new V3(0,.88f,0);}
        public static MeshData Body(Level10Action kind)
        {
            var m=new MeshData{Theme=SurfaceTheme.Level10Equipment};
            switch(kind)
            {
                case Level10Action.GrainMill:
                    Table(m);Shapes.Column(m,0,0,.29f,.78f,.88f,24,Surface.Plaster,1);
                    for(int i=0;i<4;i++)B(m,0,.06f-i*.04f,.15f,.025f,.96f+i*.03f,1.02f+i*.03f,Surface.Metal);
                    B(m,0,-.27f,.16f,.15f,.7f,.85f,Surface.Metal);break;
                case Level10Action.HandPump:
                    B(m,0,0,.55f,.6f,0,.10f,Surface.Plaster);
                    Shapes.Column(m,0,0,.085f,.1f,.85f,16,Surface.Metal,1);
                    Level10Crops.Stem(m,new V3(0,.65f,0),new V3(0,.68f,-.27f),.045f,12,Surface.Metal);
                    B(m,0,-.25f,.5f,.16f,.12f,.2f,Surface.Plaster);
                    foreach(int side in new[]{-1,1})B(m,side*.23f,-.25f,.04f,.16f,.20f,.31f,Surface.Plaster);
                    break;
                case Level10Action.WindVane:
                    Shapes.Column(m,0,0,.28f,0,.12f,16,Surface.Plaster,1);
                    Shapes.Column(m,0,0,.035f,.12f,1.52f,12,Surface.Metal,1);
                    foreach(int side in new[]{-1,1})
                    {B(m,side*.23f,0,.15f,.025f,1.34f,1.38f,Surface.Metal);B(m,0,side*.23f,.025f,.15f,1.34f,1.38f,Surface.Metal);}break;
                case Level10Action.SeedCabinet:
                    foreach(int side in new[]{-1,1})B(m,side*.39f,0,.08f,.65f,0,1.1f);
                    B(m,0,.28f,.74f,.07f,0,1.1f);
                    foreach(float y in new[]{.1f,.6f,1.06f})B(m,0,0,.75f,.65f,y,y+.045f);
                    for(int row=0;row<2;row++)for(int i=0;i<3;i++)
                    {var packet=Keepsake(1);packet.TranslateRotate(new V3(-.22f+i*.22f,.15f+row*.5f,0),0);m.Append(packet);}break;
                case Level10Action.FieldNotes:
                    Table(m);var book=Keepsake(2);book.TranslateRotate(new V3(0,.80f,0),0);m.Append(book);break;
                case Level10Action.WheatBundle:m.Append(Keepsake(0));break;
                case Level10Action.SeedPacket:m.Append(Keepsake(1));break;
                case Level10Action.Rest:break;
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
            return m;
        }
        public static MeshData Moving(Level10Action kind)
        {
            var m=new MeshData{Theme=SurfaceTheme.Level10Equipment};
            if(kind==Level10Action.GrainMill)
            {Shapes.Column(m,0,0,.25f,-.015f,.035f,24,Surface.Plaster,1);B(m,.25f,0,.055f,.045f,0,.17f);B(m,.35f,0,.25f,.035f,.13f,.18f);}
            else if(kind==Level10Action.HandPump)
            {Level10Crops.Stem(m,new V3(0,0,0),new V3(0,.11f,.32f),.026f,10,Surface.Metal);Shapes.Column(m,0,.32f,.038f,.08f,.17f,12,Surface.Accent,1);}
            else if(kind==Level10Action.WindVane)
            {
                B(m,0,0,.72f,.025f,-.01f,.03f,Surface.Metal);
                m.Triangle(new V3(.35f,0,-.08f),new V3(.35f,0,.08f),new V3(.46f,0,0),V3.Up,V3.Up,V3.Up,new V2(),new V2(1,0),new V2(.5f,1),Surface.Metal);
                B(m,-.25f,0,.10f,.16f,.01f,.10f,Surface.Accent);
            }
            else if(kind==Level10Action.SeedCabinet)
            {B(m,.39f,0,.77f,.055f,-.33f,.6f);Shapes.Column(m,.65f,-.06f,.02f,.03f,.13f,10,Surface.Metal,1);}
            return m;
        }
        public static MeshData WaterFlow()
        {
            var m=new MeshData{Theme=SurfaceTheme.Level10Equipment};
            for(int i=0;i<8;i++)
            {
                float a=i/8f,b=(i+1)/8f;
                V3 p=new V3(0,.65f-.43f*a,-.27f-.06f*(float)Math.Sin(a*Math.PI));
                V3 q=new V3(0,.65f-.43f*b,-.27f-.06f*(float)Math.Sin(b*Math.PI));
                Level10Crops.Stem(m,p,q,.014f,6,Surface.Slide);
            }
            return m;
        }
        static void Table(MeshData m)
        {
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})B(m,x*.29f,z*.23f,.065f,.065f,0,.78f);
            B(m,0,0,.74f,.58f,.75f,.8f);B(m,0,0,.6f,.05f,.2f,.27f);
        }
        public static MeshData Keepsake(int id)
        {
            if(id<0||id>=3)throw new ArgumentOutOfRangeException(nameof(id));
            var m=new MeshData{Theme=id==0?SurfaceTheme.Level10Grain:SurfaceTheme.Level10Equipment};
            if(id==0)
            {
                for(int i=0;i<7;i++)
                {float a=i*2.399963f;Level10Crops.Stalk(m,new V3((float)Math.Cos(a)*.035f,0,(float)Math.Sin(a)*.035f),.3f+i*.01f,a,.02f,i<3?0:1);}
                for(int j=0;j<2;j++)Shapes.Column(m,0,0,.06f,.10f+j*.018f,.11f+j*.018f,16,Surface.Plaster,1);
            }
            else if(id==1)
            {
                B(m,0,0,.20f,.26f,0,.028f,Surface.Glow);
                B(m,0,.10f,.18f,.035f,.028f,.033f,Surface.Accent);
                var grain=new MeshData{Theme=SurfaceTheme.Level10Grain};
                Level10Crops.Grain(grain,new V3(0,.034f,0),.025f,.006f,.04f,Surface.Tile);m.Append(grain);
            }
            else
            {
                B(m,0,0,.30f,.22f,0,.025f,Surface.Accent);
                B(m,0,0,.27f,.205f,.025f,.031f,Surface.Glow);
                for(int row=0;row<4;row++)B(m,0,-.06f+row*.035f,.18f,.004f,.0315f,.0325f,Surface.Metal);
                B(m,-.12f,0,.014f,.22f,.028f,.036f);
            }
            return m;
        }
    }
}
