using System;

namespace Poolrooms.Core
{
    // PR-121..170. Ground-level structures preserve the four cardinal circulation axes.
    public static class ZoneExpansion
    {
        public static void Build(RoomGeometry o,Room r,LayoutConfig c,ZoneRecipe recipe)
        {
            var m=o.Structure;int v=recipe.Variant,n=c.CurveSegments;float h=r.Height,uv=c.TextureWorldSize;
            void Box(float x,float z,float w,float d,float lo,float hi,Surface s=Surface.Tile)
            {ZoneShapes.Box(m,x,z,w,d,lo,hi,s,uv);}
            void Column(float x,float z,float radius,float hi)
            {Shapes.Column(m,x,z,radius,-1.7f,hi,n,Surface.Tile,uv);}
            void Beam(V3 a,V3 b,float radius=.045f)
            {ZoneShapes.Beam(m,a,b,radius,Surface.Metal,uv,12);}
            void Arc(float x,float z,float radius,float a,float span,float thickness,float lo,float hi)
            {
                int steps=Math.Max(12,n);var points=new V2[steps+1];
                for(int i=0;i<=steps;i++){float angle=(a+span*i/steps)*Shapes.Pi/180;points[i]=new V2(x+radius*Shapes.Cos(angle),z+radius*Shapes.Sin(angle));}
                Shapes.Partition(m,points,thickness,lo,hi,Surface.Tile,uv);
            }
            switch(recipe.Family)
            {
                case ZoneFamily.RecessedCourts:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        int steps=3+v;float tread=.26f+v*.025f;
                        for(int k=0;k<steps;k++)Box(sx*(9.2f-k*tread),sz*(5.3f+v*.12f),tread,2.4f+v*.25f,-1.7f,(steps-k)*.18f);
                        if(v%2==0)Column(sx*8.8f,sz*7.5f,.22f,h);
                        Box(sx*8.5f,sz*7.7f,1.6f,.55f,r.Floor,.44f,Surface.Accent);
                    }
                    break;
                case ZoneFamily.LanternHalls:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        float x=sx*5.4f,z=sz*5.4f,w=2.4f+v*.35f,lo=h-1.3f-v*.15f;
                        for(int k=0;k<2+v;k++)
                        {
                            float span=w+k*.13f,y=lo+k*.22f;
                            Box(x-span*.5f,z,.16f,span,y,y+.12f,Surface.Accent);
                            Box(x+span*.5f,z,.16f,span,y,y+.12f,Surface.Accent);
                            Box(x,z-span*.5f,span,.16f,y,y+.12f,Surface.Accent);
                            Box(x,z+span*.5f,span,.16f,y,y+.12f,Surface.Accent);
                        }
                        for(int side=-1;side<=1;side+=2)Beam(new V3(x+side*w*.45f,lo,z),new V3(x+side*w*.45f,h,z),.025f);
                        if(v==1||v==4)Column(x,z,.32f,lo-.15f);
                    }
                    break;
                case ZoneFamily.RibbedChannels:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)for(int k=0;k<3+v;k++)
                    {
                        float z=sz*(2.8f+k*5.6f/(3+v));
                        Box(sx*(8.5f-v*.16f),z,1.5f+v*.13f,.22f+v*.035f,-1.7f,h-.4f);
                        if(v==3)Column(sx*6.6f,z,.2f,h-.4f);
                        if(v==4)Box(sx*6.5f,z,5,.32f,h-.65f,h-.4f);
                    }
                    break;
                case ZoneFamily.ApsidalBaths:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        float x=sx*5.8f,z=sz*5.8f;
                        float start=sx>0?-70:110;
                        Arc(x,z,2.6f+v*.12f,start,140,.28f+v*.03f,-1.7f,h-(v==3?2:1));
                        if(v==1||v==3)Arc(x,z,2.15f,start+12,116,.5f,r.Floor,.46f);
                        if(v==2)Box(x,z,3.3f,3.3f,h-1,h-.8f);
                        if(v==4)for(int k=-1;k<=1;k+=2)Column(x-sx*1.6f,z+k*2,.2f,h-1);
                    }
                    break;
                case ZoneFamily.RaisedPromenades:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        float x=sx*8.5f,z=sz*5.6f,rise=.55f+v*.18f;
                        Box(x,z,1.8f,2.4f,0,rise,Surface.Accent);
                        // Ramp rises toward the landing on either half of the room.
                        ZoneShapes.Ramp(m,x,z-sz*2.7f,1.8f,3,rise,sz>0?0:Shapes.Pi,uv);
                        Beam(new V3(x+sx*.85f,rise+1,z-1.1f),new V3(x+sx*.85f,rise+1,z+1.1f));
                        if(v>=2)Column(x-sx*.7f,z+sz*.8f,.16f,h);
                        if(v==3)Box(x,z,2.2f,2.8f,h-1,h-.8f);
                        if(v==4)Box(x-sx*.75f,z,.18f,2.4f,rise,rise+.45f);
                    }
                    break;
                case ZoneFamily.FilterScreens:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        int count=4+v;float x=sx*7,z=sz*5;
                        for(int k=0;k<count;k++)
                        {
                            float offset=(k-(count-1)*.5f)*.47f;
                            Box(x+offset,z,.16f,1.25f+v*.13f,r.Floor,3.2f+v*.35f);
                            if(v==2)Box(x+offset,z+sz*2,.16f,.9f,r.Floor,3.9f);
                        }
                        Box(x,z,count*.47f,.16f,3.2f+v*.35f,3.38f+v*.35f,Surface.Accent);
                        if(v==4)Box(x,z-sz*1.5f,3.2f,.65f,r.Floor,.5f);
                    }
                    break;
                case ZoneFamily.SteppedOculi:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        int rings=3+v;float x=sx*5.4f,z=sz*5.4f;
                        for(int k=0;k<rings;k++)
                            Arc(x,z,1.25f+k*(.2f+v*.008f),0,360,.22f,h-1.2f+k*.17f,h-.97f+k*.17f);
                        if(v==1||v==4)for(int j=-1;j<=1;j+=2)Column(x+j*2.4f,z,.22f,h-1.2f);
                    }
                    break;
                case ZoneFamily.Aqueducts:
                    for(int side=-1;side<=1;side+=2)
                    {
                        float z=side*(4.3f+v*.38f);
                        ZoneShapes.Portal(m,0,z,0,17,h,r.Floor,5.2f+v*.35f,5.4f+v*.17f,.55f+v*.04f,v%3,n,uv);
                        Box(0,z,17,1.2f,h-1.5f,h-1.25f,Surface.Accent);
                        if(v==2)for(int sx=-1;sx<=1;sx+=2)Box(sx*8.5f,z,1.2f,2,-1.7f,h);
                        if(v==4)Box(side*5.4f,0,1.5f,15,-.18f,.1f,Surface.Accent);
                    }
                    break;
                case ZoneFamily.QuietLounges:
                    for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
                    {
                        float x=sx*8.4f,z=sz*5.1f;int count=1+v%3;
                        for(int k=0;k<count;k++)
                        {
                            float zz=z+(k-(count-1)*.5f)*1.1f;
                            Box(x,zz,2.2f,.7f,r.Floor,.43f);Box(x+sx*.85f,zz,.28f,.7f,.43f,.8f,Surface.Accent);
                        }
                        if(v>=3){Column(x-sx*.9f,z-1.3f,.18f,3.1f);Column(x-sx*.9f,z+1.3f,.18f,3.1f);Box(x,z,2.4f,3,3.1f,3.3f);}
                        if(v==2)ZoneShapes.Ramp(m,x-sx*.8f,z,.6f,1.7f,.32f,0,uv);
                    }
                    break;
                case ZoneFamily.ConcentricPassages:
                    for(int ring=0;ring<(v==2?3:2);ring++)for(int q=0;q<4;q++)
                    {
                        float radius=5.3f+ring*(v==2?1.55f:2.6f);
                        Arc(0,0,radius,q*90+27,36,.22f+v*.035f,-1.7f,h-(v==3?2.3f:.8f));
                        if(v==1)Arc(0,0,radius,q*90+21,48,.35f,h-.8f,h-.5f);
                        if(v==4)
                        {
                            float a=(q*90+20)*Shapes.Pi/180;
                            Column(radius*Shapes.Cos(a),radius*Shapes.Sin(a),.2f,h);
                        }
                    }
                    break;
            }
        }
    }
}
