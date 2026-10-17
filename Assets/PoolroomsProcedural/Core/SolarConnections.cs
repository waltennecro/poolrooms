using System;

namespace Poolrooms.Core
{
    public enum SolarConnectionKind { Existing,SharedArch,OpenHall,DoubleArch,SunGallery,LowVault,OffsetGallery,Colonnade,PoolsideGallery,ServicePassage }
    public static class SolarConnections
    {
        public static bool IsDirect(SolarConnectionKind kind){return kind==SolarConnectionKind.SharedArch||kind==SolarConnectionKind.OpenHall||kind==SolarConnectionKind.DoubleArch;}
        private static bool Eligible(LayoutConfig c,int x,int z)
        {return (PoolroomArchitectureMix.Enabled(c)?PoolroomArchitectureMix.UsesSolar(c,x,z):c.Grammar==ProceduralGrammar.SolarAtlas&&c.Procedural!=LiminalGeneration.Authored&&LiminalRegions.At(c,x,z)==LiminalEnvironment.Poolrooms&&!LiminalRegions.UsesAuthored(c,x,z))&&(c.Continuous||x>=0&&z>=0&&x<c.Columns&&z<c.Rows);}
        private static bool Edge(LayoutConfig c,int x,int z,int d,out int a,out int b,out int axis)
        {
            if(d<0||d>3)throw new ArgumentOutOfRangeException(nameof(d));
            ConnectedTerrainSite terrain;
            if(ConnectedTerrainTopology.Gateway(c,x,z,out terrain)||ConnectedTerrainTopology.Gateway(c,x+Directions.DX[d],z+Directions.DZ[d],out terrain)){a=x;b=z;axis=d%2;return false;}
            ConnectedMallSite gate;
            if(c.ConnectedMalls&&(ConnectedMallTopology.Gateway(c,x,z,out gate)||ConnectedMallTopology.Gateway(c,x+Directions.DX[d],z+Directions.DZ[d],out gate)))
            {a=x;b=z;axis=d%2;return false;}
            if(d<0||d>3)throw new ArgumentOutOfRangeException(nameof(d));
            long nx=(long)x+Directions.DX[d],nz=(long)z+Directions.DZ[d];a=x;b=z;axis=d%2;
            if(nx<int.MinValue||nx>int.MaxValue||nz<int.MinValue||nz>int.MaxValue||!Eligible(c,x,z)||!Eligible(c,(int)nx,(int)nz))return false;
            if(d==2)b=(int)nz;if(d==3)a=(int)nx;return true;
        }
        public static SolarConnectionKind At(LayoutConfig c,int x,int z,int d)
        {
            int a,b,axis;if(!Edge(c,x,z,d,out a,out b,out axis))return SolarConnectionKind.Existing;
            uint hash=LiminalRegions.Hash(c.Seed,a,b,0x1940u+(uint)axis);
            if((hash>>8)*(1f/16777216f)<c.DirectRoomJoinChance)
                return (SolarConnectionKind)(1+LiminalRegions.Hash(c.Seed,a,b,0x1942u+(uint)axis)%3);
            return (SolarConnectionKind)(4+LiminalRegions.Hash(c.Seed,a,b,0x1944u+(uint)axis)%6);
        }
        public static float Width(LayoutConfig c,int x,int z,int d)
        {
            SolarConnectionKind kind=At(c,x,z,d);if(!IsDirect(kind))return c.DoorWidth;
            int a,b,axis;Edge(c,x,z,d,out a,out b,out axis);
            float n=(LiminalRegions.Hash(c.Seed,a,b,0x1946u+(uint)axis)>>8)*(1f/16777216f);
            return Math.Min(c.CellSize*.44f,8+n*5);
        }
        public static float DoorWidth(Room r,int d,LayoutConfig c){if(r.Plan!=null&&r.Plan.SoftPlay!=null)return SoftPlayPlanner.DoorWidth(r,d,c);return r.Plan!=null&&r.Plan.Solar!=null?Width(c,r.X,r.Z,d):c.DoorWidth;}
        public static float DoorHeight(Room r,int d,LayoutConfig c)
        {if(r.Plan!=null&&r.Plan.SoftPlay!=null&&SoftPlayPlanner.Adjacent(c,r.X,r.Z,d))return Math.Max(c.DoorHeight,7);return r.Plan!=null&&r.Plan.Solar!=null&&IsDirect(At(c,r.X,r.Z,d))?Math.Max(c.DoorHeight,5.7f):c.DoorHeight;}
        public static bool Uses(Room a,Room b,LayoutConfig c)
        {return a!=null&&b!=null&&a.Plan!=null&&b.Plan!=null&&a.Plan.Solar!=null&&b.Plan.Solar!=null;}
        public static CorridorGeometry Build(Room a,Room b,int direction,LayoutConfig c)
        {
            if(direction!=0&&direction!=1)throw new ArgumentOutOfRangeException(nameof(direction));
            SolarConnectionKind kind=At(c,a.X,a.Z,direction);
            float start=(direction==0?a.Depth:a.Width)*.5f,end=c.CellSize-(direction==0?b.Depth:b.Width)*.5f,length=end-start;
            var o=new CorridorGeometry{Style=CorridorStyle.Mixed,SolarConnection=kind};var m=o.Structure;
            float width=Width(c,a.X,a.Z,direction),height=DoorHeight(a,direction,c),uv=c.TextureWorldSize,t=c.WallThickness;
            // When either room expands to a shared boundary, a threshold replaces the tunnel.
            if(length<.4f)
            {
                ZoneShapes.Box(m,0,(start+end)*.5f,width,Math.Max(.4f,length+.3f),-.3f,0,Surface.Tile,uv);
            }
            else
            {
                bool daylight=SolarClimateRegions.HasSky(a.Plan.Solar.Climate)||SolarClimateRegions.HasSky(b.Plan.Solar.Climate);
                int steps=Math.Max(8,Math.Min(48,(int)Math.Ceiling(length/.7f)));
                for(int i=0;i<steps;i++)
                {
                    float u0=(float)i/steps,u1=(float)(i+1)/steps,z0=start+length*u0,z1=start+length*u1;
                    float offset0=Offset(kind,u0,length),offset1=Offset(kind,u1,length);
                    float half0=HalfWidth(kind,u0,width*.5f),half1=HalfWidth(kind,u1,width*.5f);
                    FloorStrip(m,offset0,offset1,half0,half1,z0,z1,uv);
                    // Side strips vary in width/plan rather than repeating the same straight tube.
                    SideStrip(m,offset0-half0,offset1-half1,z0,z1,t,height,uv);
                    SideStrip(m,offset0+half0,offset1+half1,z0,z1,t,height,uv);
                    if(kind!=SolarConnectionKind.SunGallery||!daylight||i%4<2)
                        m.Quad(new V3(offset0-half0,height,z0),new V3(offset0+half0,height,z0),new V3(offset1+half1,height,z1),new V3(offset1-half1,height,z1),V3.Up*-1,Surface.Plaster,uv);
                }
                int count=Math.Max(1,(int)(length/3.8f));
                for(int i=0;i<count;i++)
                {
                    float u=(i+1f)/(count+1),z=start+length*u,x=Offset(kind,u,length),half=HalfWidth(kind,u,width*.5f);
                    if(kind==SolarConnectionKind.Colonnade||kind==SolarConnectionKind.PoolsideGallery)
                        for(int sign=-1;sign<=1;sign+=2)Shapes.Column(m,x+sign*(half-.24f),z,.16f,0,height,8,Surface.Tile,uv);
                    if(kind==SolarConnectionKind.LowVault)
                        ZoneShapes.Portal(m,x,z,0,half*2,height,0,Math.Max(3,half*2-.32f),height-.15f,.18f,1,Math.Max(8,c.CurveSegments/2),uv);
                }
                int lights=Math.Max(1,(int)Math.Ceiling(length/7));
                LightingLook la=PoolroomLighting.Look(a,c),lb=PoolroomLighting.Look(b,c);
                for(int i=0;i<lights;i++)
                {
                    float u=(i+.5f)/lights,z=start+length*u,x=Offset(kind,u,length);V3 color=la.Color*(1-u)+lb.Color*u;
                    o.Fixtures.Add(new Fixture{Position=new V3(x,height-.2f,z),Width=1.2f,Depth=.4f,Power=1800*(la.Power*(1-u)+lb.Power*u),Temperature=5200,LightColor=color,CustomColor=true,Range=15});
                    ZoneShapes.Box(m,x,z,1.2f,.35f,height-.12f,height-.08f,Surface.Glow,uv);
                }
                if(kind==SolarConnectionKind.SunGallery&&daylight)
                {
                    float u=2.5f/steps;V3 color=SolarClimateRegions.HasSky(a.Plan.Solar.Climate)?a.Plan.Solar.ClimateDescription.SunColor:b.Plan.Solar.ClimateDescription.SunColor;
                    o.Fixtures.Add(new Fixture{Position=new V3(0,height+.25f,start+length*u),Aim=new V3(0,-1,0),Width=width,Depth=length/steps,Power=42000,Temperature=5600,LightColor=color,CustomColor=true,Range=22,Skylight=true,SolarBeam=true,PoolroomSunlight=true,ConeAngle=65});
                }
                m.EmissionTint=(la.Color+lb.Color)*.5f;m.EmissionStrength=(la.Emission+lb.Emission)*.5f;
            }
            m.TranslateRotate(new V3(),direction*Shapes.Pi*.5f);
            for(int i=0;i<o.Fixtures.Count;i++){Fixture f=o.Fixtures[i];if(direction==1)f.Position=new V3(f.Position.Z,f.Position.Y,-f.Position.X);o.Fixtures[i]=f;}
            return o;
        }
        public static float Offset(SolarConnectionKind kind,float u,float length)
        {return kind==SolarConnectionKind.OffsetGallery?Math.Min(.9f,length*.08f)*Shapes.Sin(Shapes.Pi*u)*Shapes.Sin(Shapes.Pi*u):0;}
        public static float HalfWidth(SolarConnectionKind kind,float u,float half)
        {float blend=Shapes.Sin(Shapes.Pi*u);return half+(kind==SolarConnectionKind.PoolsideGallery?1.4f:kind==SolarConnectionKind.Colonnade?.8f:kind==SolarConnectionKind.SunGallery?1f:0)*blend*blend;}
        private static void FloorStrip(MeshData m,float x0,float x1,float h0,float h1,float z0,float z1,float uv)
        {
            V3 a=new V3(x0-h0,0,z0),b=new V3(x0+h0,0,z0),c=new V3(x1+h1,0,z1),d=new V3(x1-h1,0,z1);
            m.Quad(a,b,c,d,V3.Up,Surface.Tile,uv);m.Quad(a-V3.Up*.3f,b-V3.Up*.3f,c-V3.Up*.3f,d-V3.Up*.3f,V3.Up*-1,Surface.Tile,uv);
        }
        private static void SideStrip(MeshData m,float x0,float x1,float z0,float z1,float t,float h,float uv)
        {
            V3 n=new V3(z1-z0,0,x0-x1).Unit;
            m.Quad(new V3(x0,0,z0),new V3(x1,0,z1),new V3(x1,h,z1),new V3(x0,h,z0),n,Surface.Tile,uv);
            m.Quad(new V3(x0+t,0,z0),new V3(x1+t,0,z1),new V3(x1+t,h,z1),new V3(x0+t,h,z0),n*-1,Surface.Tile,uv);
        }
    }
}
