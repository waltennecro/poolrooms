using System;
namespace Poolrooms.Core
{
    // Original metre-scale botanical meshes. Fronds and petals are curved solids
    // with explicit back faces; no billboard per plant, leaf GameObject or leaf collider.
    public static class ConservatoryPlants
    {
        public static void Plant(RoomGeometry o,MeshData foliage,float x,float z,float y,int species,uint seed,float scale=1)
        {
            var random=new StableRandom(seed);
            if(species==0)Palm(o,foliage,x,z,y,seed,scale);
            else if(species==1)
            {
                for(int fern=0;fern<(scale<.5f?1:3);fern++)Fern(foliage,x+(fern-1)*.65f*scale,z+(fern%2)*.45f*scale,y,random.Next(),scale);
            }
            else if(species==2)
            {
                for(int flower=0;flower<7;flower++)
                {
                    float angle=flower*Shapes.Pi*2/7,px=x+Shapes.Cos(angle)*.7f*scale,pz=z+Shapes.Sin(angle)*.7f*scale;
                    Flower(o,foliage,px,pz,y,random.Next(),scale);
                }
            }
            else
            {
                for(int stalk=0;stalk<3;stalk++)
                {
                    float px=x+(stalk-1)*.52f*scale,pz=z+(stalk%2)*.5f*scale,height=(1.1f+random.Value()*.8f)*scale;
                    Shapes.Column(foliage,px,pz,.19f*scale,y,y+height,12,Surface.Tile,1.2f);
                    Shapes.Column(foliage,px+.3f*scale,pz,.095f*scale,y+height*.5f,y+height*.82f,10,Surface.Tile,1.2f);
                    ZoneShapes.Beam(foliage,new V3(px,y+height*.5f,pz),new V3(px+.3f*scale,y+height*.5f,pz),.095f*scale,Surface.Tile,1.2f,10);
                    for(int rib=0;rib<8;rib++)
                    {
                        float a=rib*Shapes.Pi*.25f;
                        ZoneShapes.Beam(o.Decoration,new V3(px+Shapes.Cos(a)*.194f*scale,y+.08f,pz+Shapes.Sin(a)*.194f*scale),new V3(px+Shapes.Cos(a)*.194f*scale,y+height-.03f,pz+Shapes.Sin(a)*.194f*scale),.006f*scale,Surface.Plaster,1.2f,4);
                    }
                }
            }
        }
        static void Palm(RoomGeometry o,MeshData f,float x,float z,float y,uint seed,float scale)
        {
            float height=(5.2f+(seed%5)*.28f)*scale;
            V3 Point(float t){return new V3(x+.22f*scale*t*t,y+height*t,z+.14f*scale*t*t);}
            for(int ring=0;ring<12;ring++)
            {
                var a=Point(ring/12f);var b=Point((ring+1)/12f);
                ZoneShapes.Beam(o.Structure,a,b,(.13f-ring*.0045f)*scale,Surface.Accent,1.2f,10);
                Shapes.Column(o.Decoration,a.X,a.Z,.139f*scale-ring*.0045f*scale,a.Y,a.Y+.035f*scale,10,Surface.Accent,1.2f);
            }
            var top=Point(1);
            for(int frond=0;frond<10;frond++)
            {
                float angle=(frond+(seed%17)*.013f)*Shapes.Pi/5,length=(1.9f+((seed+(uint)frond)%4)*.14f)*scale;
                var direction=new V3(Shapes.Cos(angle),0,Shapes.Sin(angle));var side=new V3(-direction.Z,0,direction.X);
                V3 Curve(float t){return top+direction*(length*t)+V3.Up*(scale*(.9f*(float)Math.Sin(t*Shapes.Pi)-.7f*t*t));}
                for(int segment=0;segment<9;segment++)
                {
                    float a=segment/9f,b=(segment+1)/9f;var start=Curve(a);var end=Curve(b);
                    ZoneShapes.Beam(f,start,end,.012f*scale,Surface.Tile,1.2f,5);
                    foreach(int sign in new[]{-1,1})
                    {
                        float spread=(.4f*(float)Math.Sin((a+.08f)*Shapes.Pi)+.06f)*scale;
                        var tip=end+side*(sign*spread)-direction*(.24f*scale)-V3.Up*(.12f*scale);
                        Blade(f,start,end,tip,spread*.28f,Surface.Tile);
                    }
                }
            }
        }
        static void Fern(MeshData f,float x,float z,float y,uint seed,float scale)
        {
            var origin=new V3(x,y,z);
            int fronds=scale<.5f?6:9,segments=scale<.5f?4:8;
            for(int frond=0;frond<fronds;frond++)
            {
                float angle=frond*Shapes.Pi*2/fronds+(seed%7)*.03f,length=(.95f+(seed%4)*.1f)*scale;
                var dir=new V3(Shapes.Cos(angle),0,Shapes.Sin(angle));var side=new V3(-dir.Z,0,dir.X);
                V3 Curve(float t){return origin+dir*(length*t)+V3.Up*(scale*(.15f+.85f*(float)Math.Sin(t*Shapes.Pi*.82f)));}
                for(int i=0;i<segments;i++)
                {
                    var a=Curve((float)i/segments);var b=Curve((float)(i+1)/segments);ZoneShapes.Beam(f,a,b,.009f*scale,Surface.Tile,1.2f,4);
                    float width=(.29f*(float)Math.Sin((i+.7f)*Shapes.Pi/(segments+1))+.015f)*scale;
                    foreach(int sign in new[]{-1,1})Blade(f,a,b,b+side*(sign*width)-dir*(.12f*scale),width*.32f,Surface.Tile);
                }
            }
        }
        static void Flower(RoomGeometry o,MeshData f,float x,float z,float y,uint seed,float scale)
        {
            float h=(.62f+(seed%8)*.04f)*scale;var top=new V3(x,y+h,z);
            ZoneShapes.Beam(f,new V3(x,y,z),top,.013f*scale,Surface.Tile,1.2f,6);
            foreach(int side in new[]{-1,1})Blade(f,new V3(x,y+.15f*scale,z),new V3(x,y+.5f*scale,z),new V3(x+side*.34f*scale,y+.4f*scale,z+.15f*scale),.12f*scale,Surface.Tile);
            for(int petal=0;petal<6;petal++)
            {
                float a=petal*Shapes.Pi/3+(seed%7)*.03f;var dir=new V3(Shapes.Cos(a),0,Shapes.Sin(a));
                Blade(o.Decoration,top,top+dir*(.21f*scale)+V3.Up*(.08f*scale),top+dir*(.4f*scale)+V3.Up*(.035f*scale),.15f*scale,(seed&1)==0?Surface.Plaster:Surface.Slide);
            }
            Shapes.Column(o.Decoration,x,z,.065f*scale,y+h,y+h+.045f*scale,10,Surface.Slide,1.2f);
        }
        static void Blade(MeshData m,V3 root,V3 middle,V3 tip,float width,Surface surface)
        {
            var direction=(tip-root).Unit;var side=V3.Cross(direction,V3.Up).Unit*width;
            var left=middle-side;var right=middle+side;var raised=middle+V3.Up*(width*.24f);
            Face(root,left,raised);Face(root,raised,right);Face(left,tip,raised);Face(raised,tip,right);
            void Face(V3 a,V3 b,V3 c)
            {
                var n=V3.Cross(b-a,c-a).Unit;
                m.Triangle(a,b,c,n,n,n,new V2(0,0),new V2(.5f,1),new V2(1,0),surface);
                m.Triangle(c,b,a,n*-1,n*-1,n*-1,new V2(1,0),new V2(.5f,1),new V2(0,0),surface);
            }
        }
    }
}
