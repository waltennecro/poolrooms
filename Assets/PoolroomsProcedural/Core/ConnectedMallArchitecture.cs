using System;

namespace Poolrooms.Core
{
    public static class ConnectedMallArchitecture
    {
        public const float DoorZ=18;
        public static bool ContainsSupportFootprint(ConnectedMallSite site,LayoutConfig c,float x,float z)
        {
            if(float.IsNaN(x)||float.IsInfinity(x)||float.IsNaN(z)||float.IsInfinity(z))return false;
            float ax=Math.Abs(x);
            if(ax<=site.Width*.5f+.25f&&Math.Abs(z)<=site.Depth*.5f+.25f)return true;
            float start=(site.RadiusX+1)*c.CellSize-Math.Min(22,c.CellSize-6)*.5f-.25f,end=site.Width*.5f-.25f;
            if(ax<end||ax>start)return false;
            float u=(start-ax)/(start-end),t=Math.Max(0,Math.Min(1,(u-.15f)/.7f));
            float centerZ=DoorZ*t*t*(3-2*t);
            return Math.Abs(z-centerZ)<=c.DoorWidth*.5f;
        }
        // In mall-local coordinates, ends overlap both supporting floors by 0.25 metres.
        public static V3 Point(ConnectedMallSite site,Room gateway,LayoutConfig c,float u)
        {
            int side=gateway.X<site.X?-1:1;
            float start=side*((site.RadiusX+1)*c.CellSize-gateway.Width*.5f-.25f);
            float end=side*(site.Width*.5f-.25f);
            float travel=Math.Max(0,Math.Min(1,(u-.15f)/.7f));
            float smooth=travel*travel*(3-2*travel);
            return new V3(start+(end-start)*u,0,DoorZ*smooth);
        }
        public static RoomGeometry Bridge(ConnectedMallSite site,Room gateway,LayoutConfig c)
        {
            var o=new RoomGeometry();MallArchitecture.SetMallTheme(o);
            float half=c.DoorWidth*.5f,height=c.DoorHeight;
            int steps=Math.Max(24,(int)Math.Ceiling(((site.RadiusX+1)*c.CellSize-gateway.Width*.5f-site.Width*.5f)/1.2f));
            for(int i=0;i<steps;i++)
            {
                V3 a=Point(site,gateway,c,(float)i/steps),b=Point(site,gateway,c,(i+1f)/steps);
                // Constant Z offsets meet both east/west door jambs exactly. The S curve
                // is a broad gallery, with no steps or instantaneous change of heading.
                V3 left=new V3(0,0,-half),right=new V3(0,0,half);
                Quad(o.Structure,a+left,b+left,b+right,a+right,V3.Up,c);
                Quad(o.Structure,a+left-V3.Up*.25f,a+right-V3.Up*.25f,b+right-V3.Up*.25f,b+left-V3.Up*.25f,V3.Up*-1,c);
                Wall(o.Structure,a+left,b+left,height,c);
                Wall(o.Structure,b+right,a+right,height,c);
                // Roof openings every fourth bay give the approach its own daylight rhythm.
                if(i%8<6)Quad(o.Structure,a+left+V3.Up*height,a+right+V3.Up*height,b+right+V3.Up*height,b+left+V3.Up*height,V3.Up*-1,c);
            }
            int lamps=4;
            for(int i=0;i<lamps;i++)
            {
                V3 p=Point(site,gateway,c,(i+.5f)/lamps);
                o.Fixtures.Add(new Fixture{Position=p+V3.Up*(height-.2f),Width=1.4f,Depth=.6f,Power=3600,Temperature=4600,Range=22});
                MallArchitecture.Box(o.Decoration,p.X,p.Z,1.4f,.4f,height-.14f,height-.08f,Surface.Glow);
            }
            int opening=Math.Min(steps-1,(steps/2/8)*8+6);
            V3 sun=Point(site,gateway,c,(opening+.5f)/steps);
            o.Fixtures.Add(new Fixture{Position=sun+V3.Up*(height+.1f),Aim=new V3(0,-1,0),Power=42000,Width=c.DoorWidth,Depth=3,Temperature=5600,Range=20,ConeAngle=58,Skylight=true,SolarBeam=true});
            return o;
        }
        private static void Quad(MeshData mesh,V3 a,V3 b,V3 d,V3 e,V3 normal,LayoutConfig c)
        {
            // MeshData retains the supplied winding. Match the geometric normal to the
            // intended face so PhysX receives upward walkable surfaces on either side.
            if(V3.Dot(V3.Cross(b-a,d-a),normal)<0)mesh.Quad(a,e,d,b,normal,Surface.Tile,c.TextureWorldSize);
            else mesh.Quad(a,b,d,e,normal,Surface.Tile,c.TextureWorldSize);
        }
        private static void Wall(MeshData mesh,V3 a,V3 b,float height,LayoutConfig c)
        {
            V3 n=V3.Cross(b-a,V3.Up).Unit;
            mesh.Quad(a,b,b+V3.Up*height,a+V3.Up*height,n,Surface.Plaster,c.TextureWorldSize);
            mesh.Quad(b,a,a+V3.Up*height,b+V3.Up*height,n*-1,Surface.Plaster,c.TextureWorldSize);
        }
    }
}
