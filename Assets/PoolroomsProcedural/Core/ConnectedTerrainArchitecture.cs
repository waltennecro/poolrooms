using System;
namespace Poolrooms.Core
{
    public static class ConnectedTerrainArchitecture
    {
        public static RoomGeometry Gateway(Room r,LayoutConfig c)
        {
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.WaitingHall;
            ProceduralArchitecture.DryEnvelope(o,r,c);
            o.Structure.Box(new V3(-r.Width*.5f,r.Height,-r.Depth*.5f),new V3(r.Width*.5f,r.Height+.2f,r.Depth*.5f),Surface.Plaster,c.TextureWorldSize);
            foreach(int side in new[]{-1,1})MallFurnishing.Bench(o.Structure,side*(r.Width*.5f-2.6f),r.Depth*.28f,0,0);
            o.Fixtures.Add(new Fixture{Position=new V3(0,r.Height-.3f,0),Width=2,Depth=1,Power=6000,Temperature=5100,Range=24});
            return o;
        }
        private static void Quad(MeshData m,V3 a,V3 b,V3 c,V3 d,V3 n,float uv)
        {if(V3.Dot(V3.Cross(b-a,c-a),n)<0)m.Quad(a,d,c,b,n,Surface.Plaster,uv);else m.Quad(a,b,c,d,n,Surface.Plaster,uv);}
        public static V3 Point(ConnectedTerrainSite site,Room gateway,LayoutConfig c,float u)
        {
            int d=site.GateDirection(gateway.X,gateway.Z);if(d<0)throw new ArgumentException("Not a meadow gateway.");
            float start=(d%2==0?(site.RadiusZ+1)*c.CellSize-gateway.Depth*.5f:(site.RadiusX+1)*c.CellSize-gateway.Width*.5f)-.25f;
            float end=(d%2==0?site.HalfDepth:site.HalfWidth)-.25f;
            return new V3(Directions.DX[d]*(start+(end-start)*u),0,Directions.DZ[d]*(start+(end-start)*u));
        }
        public static RoomGeometry Bridge(ConnectedTerrainSite site,Room gateway,LayoutConfig c)
        {
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.WaitingHall;
            int d=site.GateDirection(gateway.X,gateway.Z);V3 lateral=new V3(Directions.DZ[d],0,-Directions.DX[d])*c.DoorWidth*.5f;
            V3 a=Point(site,gateway,c,0),b=Point(site,gateway,c,1);float h=c.DoorHeight;
            o.Structure.Box(new V3(Math.Min(a.X,b.X)-Math.Abs(lateral.X),-.25f,Math.Min(a.Z,b.Z)-Math.Abs(lateral.Z)),new V3(Math.Max(a.X,b.X)+Math.Abs(lateral.X),0,Math.Max(a.Z,b.Z)+Math.Abs(lateral.Z)),Surface.Plaster,c.TextureWorldSize);
            foreach(int side in new[]{-1,1})
            {
                V3 p=a+lateral*side,q=b+lateral*side,n=V3.Cross(q-p,V3.Up).Unit;
                o.Structure.Quad(p,q,q+V3.Up*h,p+V3.Up*h,n,Surface.Plaster,c.TextureWorldSize);
                o.Structure.Quad(q,p,p+V3.Up*h,q+V3.Up*h,n*-1,Surface.Plaster,c.TextureWorldSize);
            }
            // The roof stops before the grass: the landscape surrounds the threshold in all directions.
            V3 end=Point(site,gateway,c,.55f);
            Quad(o.Structure,a+lateral+V3.Up*h,end+lateral+V3.Up*h,end-lateral+V3.Up*h,a-lateral+V3.Up*h,V3.Up*-1,c.TextureWorldSize);
            o.Fixtures.Add(new Fixture{Position=(a+end)*.5f+V3.Up*(h-.2f),Width=1.2f,Depth=.7f,Power=5000,Temperature=5400,Range=18});
            return o;
        }
    }
}
