using System;
namespace Poolrooms.Core
{
    // Original tools with physical handles, seams and spout rather than placeholder cubes.
    public static class EncounterTools
    {
        public static bool Available(int id){return id==2||id==3||id==4;}
        public static V3 Size(int id){return id==2?new V3(.45f,.32f,.21f):id==3?new V3(.02f,.115f,.02f):new V3(.25f,.018f,.204f);}
        public static MeshData Build(int id)
        {
            if(!Available(id))throw new ArgumentOutOfRangeException(nameof(id));
            var m=new MeshData{Theme=SurfaceTheme.EncounterFurniture};
            if(id==2)
            {
                // Twenty-four sided body, open rim, long tapered spout, real arched handle.
                for(int i=0;i<24;i++)
                {
                    float a=i*Shapes.Pi/12,b=(i+1)*Shapes.Pi/12;
                    V3 A(float angle,float radius,float y){return new V3((float)Math.Cos(angle)*radius,y,(float)Math.Sin(angle)*radius);}
                    var p=A(a,.095f,.015f);var q=A(b,.095f,.015f);var r=A(b,.104f,.2f);var s=A(a,.104f,.2f);
                    var normal=new V3((float)Math.Cos((a+b)*.5f),-.049f,(float)Math.Sin((a+b)*.5f)).Unit;m.Quad(p,q,r,s,normal,Surface.Metal,.4f);
                    m.Quad(A(a,.104f,.2f),A(b,.104f,.2f),A(b,.092f,.2f),A(a,.092f,.2f),V3.Up,Surface.Metal,.4f);
                    m.Quad(A(a,.092f,.2f),A(b,.092f,.2f),A(b,.086f,.02f),A(a,.086f,.02f),normal*-1,Surface.Metal,.4f);
                    m.Triangle(new V3(0,.015f,0),q,p,V3.Up*-1,V3.Up*-1,V3.Up*-1,new V2(),new V2(),new V2(),Surface.Metal);
                }
                ZoneShapes.Beam(m,new V3(.07f,.05f,0),new V3(.225f,.21f,0),.018f,Surface.Metal,.4f,12);
                ZoneShapes.Beam(m,new V3(.225f,.21f,0),new V3(.235f,.23f,0),.038f,Surface.Metal,.4f,16);
                for(int i=0;i<12;i++)
                {float a=Shapes.Pi*(.5f+i/12f),b=Shapes.Pi*(.5f+(i+1)/12f);ZoneShapes.Beam(m,new V3(-.04f+(float)Math.Cos(a)*.12f,.16f+(float)Math.Sin(a)*.14f,0),new V3(-.04f+(float)Math.Cos(b)*.12f,.16f+(float)Math.Sin(b)*.14f,0),.012f,Surface.Accent,.4f,10);}
            }
            else if(id==3)ZoneShapes.Beam(m,new V3(0,0,0),new V3(0,.115f,0),.01f,Surface.Slide,.2f,12);
            else
            {
                EncounterFurniture.Chamfer(m,0,0,.25f,.19f,0,.018f,.003f,Surface.Tile);
                for(int side=0;side<2;side++)for(int i=0;i<20;i++)ZoneShapes.Beam(m,new V3(-.12f+i*.012f,.009f,(side==0?-1:1)*.095f),new V3(-.12f+i*.012f,.009f,(side==0?-1:1)*.101f),.0012f,Surface.Tile,.2f,6);
            }
            return m;
        }
    }
}
