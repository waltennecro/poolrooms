using System;

namespace Poolrooms.Core
{
    public static partial class RestfulLevelArchitecture
    {
        // A cut corner and a sloping top edge produce real highlight transitions.
        // One shared room mesh; no additional Unity objects, lights or materials.
        static void ChamferedTop(MeshData m,float x,float z,float width,float depth,float bottom,float top,float bevel,Surface surface)
        {
            float w=width*.5f,d=depth*.5f;
            var ring=new[]{new V2(-w+bevel,-d),new V2(w-bevel,-d),new V2(w,-d+bevel),new V2(w,d-bevel),new V2(w-bevel,d),new V2(-w+bevel,d),new V2(-w,d-bevel),new V2(-w,-d+bevel)};
            for(int i=0;i<ring.Length;i++)
            {
                var a=ring[i];var b=ring[(i+1)%ring.Length];
                var loA=new V3(x+a.X,bottom,z+a.Y);var loB=new V3(x+b.X,bottom,z+b.Y);
                var midA=new V3(x+a.X,top-bevel,z+a.Y);var midB=new V3(x+b.X,top-bevel,z+b.Y);
                var hiA=new V3(x+a.X*(w-bevel)/w,top,z+a.Y*(d-bevel)/d);var hiB=new V3(x+b.X*(w-bevel)/w,top,z+b.Y*(d-bevel)/d);
                var normal=new V3(b.Y-a.Y,0,a.X-b.X).Unit;
                m.Quad(loA,loB,midB,midA,normal,surface,1.2f);
                m.Quad(midA,midB,hiB,hiA,(normal+V3.Up).Unit,surface,1.2f);
                m.Triangle(new V3(x,top,z),hiA,hiB,V3.Up,V3.Up,V3.Up,new V2(.5f,.5f),new V2((a.X+w)/width,(a.Y+d)/depth),new V2((b.X+w)/width,(b.Y+d)/depth),surface);
                m.Triangle(new V3(x,bottom,z),loB,loA,V3.Up*-1,V3.Up*-1,V3.Up*-1,new V2(.5f,.5f),new V2((b.X+w)/width,(b.Y+d)/depth),new V2((a.X+w)/width,(a.Y+d)/depth),surface);
            }
        }
        static void PerimeterJoinery(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f;var p=r.Plan.Restful;
            for(int side=0;side<4;side++)
            {
                if(LargeRegions.OpenEdge(c,r.X,r.Z,side,r.Environment))continue;
                var trim=new MeshData();
                // Side-bay cornices leave every portal and the walking cross intact.
                foreach(int sign in new[]{-1,1})
                {
                    float length=half-p.Clear-.45f,center=sign*(p.Clear+.3f+length*.5f);
                    Box(trim,center,-half+.18f,length,.14f,2.85f,2.94f,Surface.Plaster);
                    Box(trim,center,-half+.2f,length,.18f,r.Height-.35f,r.Height-.23f,Surface.Accent);
                }
                trim.TranslateRotate(new V3(),side*Shapes.Pi*.5f);o.Decoration.Append(trim);
            }
        }
        static void Washer(RoomGeometry o,float x,float z,float width)
        {
            // Split the housing around an actual recess instead of placing a glass
            // disc over an opaque front. Collision retains a sealed, usable body.
            float radius=width*.3f,centerY=.51f;
            Box(o.Structure,x,z+.17f,width,.46f,0,1.05f,Surface.Plaster);
            foreach(int side in new[]{-1,1})Box(o.Structure,x+side*(width*.25f+radius*.5f),z-.23f,width*.5f-radius,.34f,0,1.05f,Surface.Plaster);
            Box(o.Structure,x,z-.23f,radius*2,.34f,0,centerY-radius,Surface.Plaster);
            Box(o.Structure,x,z-.23f,radius*2,.34f,centerY+radius,1.05f,Surface.Plaster);
            // Square aperture corners are covered by the annular steel bezel.
            const int sides=24;float outer=width*.43f,inner=width*.285f,front=z-.45f;
            for(int a=0;a<sides;a++)
            {
                float t=a*2*Shapes.Pi/sides,u=(a+1)*2*Shapes.Pi/sides;
                var v0=new V3(x+outer*Shapes.Cos(t),centerY+outer*Shapes.Sin(t),front);
                var v1=new V3(x+outer*Shapes.Cos(u),centerY+outer*Shapes.Sin(u),front);
                var v2=new V3(x+inner*Shapes.Cos(u),centerY+inner*Shapes.Sin(u),front);
                var v3=new V3(x+inner*Shapes.Cos(t),centerY+inner*Shapes.Sin(t),front);
                o.Decoration.Quad(v0,v1,v2,v3,new V3(0,0,-1),Surface.Metal,1.2f);
                var outer0=new V3(v0.X,v0.Y,z-.4f);var outer1=new V3(v1.X,v1.Y,z-.4f);
                o.Decoration.Quad(v0,v1,outer1,outer0,new V3(Shapes.Cos((t+u)*.5f),Shapes.Sin((t+u)*.5f),0),Surface.Metal,1.2f);
                // Recessed drum wall, visible through the separate glass pane.
                var back0=new V3(v3.X,v3.Y,z-.08f);var back1=new V3(v2.X,v2.Y,z-.08f);
                var inward=new V3(-Shapes.Cos((t+u)*.5f),-Shapes.Sin((t+u)*.5f),0);
                o.Decoration.Quad(v3,v2,back1,back0,inward,Surface.Metal,1.2f);
                var paneCenter=new V3(x,centerY,front+.008f);
                var pane0=new V3(v3.X,v3.Y,paneCenter.Z);var pane1=new V3(v2.X,v2.Y,paneCenter.Z);var n=new V3(0,0,-1);
                o.Glass.Triangle(paneCenter,pane0,pane1,n,n,n,new V2(.5f,.5f),new V2(.5f+.5f*Shapes.Cos(t),.5f+.5f*Shapes.Sin(t)),new V2(.5f+.5f*Shapes.Cos(u),.5f+.5f*Shapes.Sin(u)),Surface.Tile);
            }
            // The rear wall blocks looking straight through the washer; inset ribs
            // provide depth without dozens of extra renderers or rigid bodies.
            Box(o.Decoration,x,z-.07f,inner*2,.025f,centerY-inner,centerY+inner,Surface.Metal);
            Box(o.Decoration,x+inner+.035f,front-.018f,.06f,.055f,centerY-.09f,centerY+.09f,Surface.Metal);
        }
    }
}
