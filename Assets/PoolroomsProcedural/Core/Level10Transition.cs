using System;

namespace Poolrooms.Core
{
    public static class Level10Transition
    {
        // A road becomes a farm track under the same sky. There is no luminous
        // portal, compulsory interaction, indoor ceiling or fixture-light tunnel.
        public static CorridorGeometry Build(Room a,Room b,int direction,LayoutConfig c)
        {
            float start=(direction==0?a.Depth:a.Width)*.5f,end=c.CellSize-(direction==0?b.Depth:b.Width)*.5f;
            if(end-start<=.02f)return null;
            var result=new CorridorGeometry{Style=CorridorStyle.WideGallery,Natural=true,Passage=NaturalPassage.FarmTrack};
            var first=new MeshData{Theme=BiomeSurfaces.ForEnvironment(a.Environment)};
            var second=new MeshData{Theme=BiomeSurfaces.ForEnvironment(b.Environment)};
            int steps=Math.Max(4,(int)Math.Ceiling((end-start)/.5f));if(steps%2!=0)steps++;
            float from=SolarConnections.DoorWidth(a,direction,c)*.5f,to=SolarConnections.DoorWidth(b,direction+2,c)*.5f;
            for(int i=0;i<steps;i++)
            {
                float p=i/(float)steps,q=(i+1)/(float)steps,z0=start+(end-start)*p,z1=start+(end-start)*q;
                float w0=from+(to-from)*p,w1=from+(to-from)*q,y0=a.Floor+(b.Floor-a.Floor)*p,y1=a.Floor+(b.Floor-a.Floor)*q;
                var m=i<steps/2?first:second;
                m.Quad(new V3(-w0,y0,z0),new V3(w0,y0,z0),new V3(w1,y1,z1),new V3(-w1,y1,z1),V3.Up,Surface.Accent,c.TextureWorldSize);
                m.Quad(new V3(-w0,y0-.18f,z0),new V3(-w1,y1-.18f,z1),new V3(w1,y1-.18f,z1),new V3(w0,y0-.18f,z0),V3.Up*-1,Surface.Accent,c.TextureWorldSize);
                foreach(int sign in new[]{-1,1})
                {
                    m.Quad(new V3(sign*w0,y0-.18f,z0),new V3(sign*w1,y1-.18f,z1),new V3(sign*w1,y1,z1),new V3(sign*w0,y0,z0),new V3(sign,0,0),Surface.Accent,c.TextureWorldSize);
                    // Low roadside edging; its inside face is outside the route.
                    Level10Architecture.Box(m,sign*((w0+w1)*.5f+.10f),(z0+z1)*.5f,.12f,z1-z0,Math.Min(y0,y1)-.08f,Math.Max(y0,y1)+.14f);
                }
            }
            for(int sign=-1;sign<=1;sign+=2)
            {
                float z=(start+end)*.5f,width=(from+to)*.5f;
                Level10Architecture.Box(first,sign*(width+.20f),z,.11f,.11f,0,1.15f,Surface.Metal);
            }
            if(direction==1){first.TranslateRotate(new V3(),Shapes.Pi*.5f);second.TranslateRotate(new V3(),Shapes.Pi*.5f);}
            result.Structure.Append(first);result.Structure.Append(second);result.Finishes.Add(first);result.Finishes.Add(second);return result;
        }
    }
}
