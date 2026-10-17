using System;

namespace Poolrooms.Core
{
    public sealed class CityAirfieldCell
    {
        public readonly int X,Z,CenterX,CenterZ;
        public CityAirfieldCell(int x,int z,int centerX,int centerZ){X=x;Z=z;CenterX=centerX;CenterZ=centerZ;}
        public int Column {get{return X-CenterX;}}
        public int Row {get{return Z-CenterZ;}}
        public bool AircraftSpawn {get{return Column==0&&Row==-2;}}
        public bool Terminal {get{return Column==-1&&Row==0;}}
    }
    public static class CityAirfields
    {
        public const int DistrictSize=18;
        public static bool Enabled(LayoutConfig c)
        {return c.EnableAircraft&&c.Continuous&&c.CellSize>=28&&(c.Procedural==LiminalGeneration.City11||c.Procedural==LiminalGeneration.Expanded&&c.EnableCity11);}
        public static bool At(LayoutConfig c,int x,int z,out CityAirfieldCell cell)
        {
            cell=null;if(!Enabled(c))return false;
            int bx=ContinuousTopology.FloorDivide(x,DistrictSize),bz=ContinuousTopology.FloorDivide(z,DistrictSize);
            long cx=(long)bx*DistrictSize+8+(LiminalRegions.Hash(c.Seed,bx,bz,0xA18001u)%3),cz=(long)bz*DistrictSize+8+(LiminalRegions.Hash(c.Seed,bx,bz,0xA18002u)%3);
            if(Math.Abs((long)x-cx)>1||Math.Abs((long)z-cz)>3||cx<int.MinValue+2L||cx>int.MaxValue-2L||cz<int.MinValue+4L||cz>int.MaxValue-4L)return false;
            // The entire airfield must belong to one actual city, with no meadow/mall
            // reserve or gateway under its runway. No forced biome replacement.
            for(int dz=-3;dz<=3;dz++)for(int dx=-1;dx<=1;dx++)
            {
                int px=(int)cx+dx,pz=(int)cz+dz;ConnectedTerrainSite terrain;ConnectedMallSite mall;
                if(LiminalRegions.At(c,px,pz)!=LiminalEnvironment.City11||ConnectedTerrainTopology.Reserved(c,px,pz)||ConnectedTerrainTopology.Gateway(c,px,pz,out terrain)||ConnectedMallTopology.Gateway(c,px,pz,out mall))return false;
            }
            cell=new CityAirfieldCell(x,z,(int)cx,(int)cz);return true;
        }
        public static bool Find(LayoutConfig c,int x,int z,out CityAirfieldCell cell)
        {
            cell=null;if(!Enabled(c))return false;int bx=ContinuousTopology.FloorDivide(x,DistrictSize),bz=ContinuousTopology.FloorDivide(z,DistrictSize);
            for(int ring=0;ring<=20;ring++)for(int dz=-ring;dz<=ring;dz++)for(int dx=-ring;dx<=ring;dx++)
            {
                if(Math.Max(Math.Abs(dx),Math.Abs(dz))!=ring)continue;
                long ax=((long)bx+dx)*DistrictSize+8+(LiminalRegions.Hash(c.Seed,bx+dx,bz+dz,0xA18001u)%3),az=((long)bz+dz)*DistrictSize+8+(LiminalRegions.Hash(c.Seed,bx+dx,bz+dz,0xA18002u)%3)-2;
                if(ax<int.MinValue||ax>int.MaxValue||az<int.MinValue||az>int.MaxValue)continue;
                if(At(c,(int)ax,(int)az,out cell))return true;
            }
            return false;
        }
    }
    public static class CityAirfieldArchitecture
    {
        static void Box(MeshData m,float x,float z,float w,float d,float low,float high,Surface surface)
        {ZoneShapes.Box(m,x,z,w,d,low,high,surface,2);}
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            var o=new RoomGeometry();o.Structure.Theme=SurfaceTheme.Airfield;o.Decoration.Theme=SurfaceTheme.CityFacade;
            float half=c.CellSize*.5f;var field=room.Plan.City.Airfield;
            Box(o.Structure,0,0,c.CellSize,c.CellSize,-.3f,0,Surface.Tile);
            var paint=new MeshData{Theme=SurfaceTheme.Airfield};o.VisualMeshes.Add(paint);
            if(field.Column==0)
            {
                // Shared longitudinal phase; clipped rectangles do not overlap cell seams.
                double start=field.Row*(double)c.CellSize-half,end=start+c.CellSize;
                for(double line=Math.Floor(start/12)*12;line<end;line+=12)
                {double low=Math.Max(start,line),high=Math.Min(end,line+5);if(high>low)Box(paint,0,(float)((low+high)*.5-field.Row*(double)c.CellSize),.25f,(float)(high-low),.005f,.009f,Surface.Accent);}
                foreach(int side in new[]{-1,1})Box(paint,side*8,0,.16f,c.CellSize,.005f,.009f,Surface.Accent);
                if(Math.Abs(field.Row)==3)for(int i=-3;i<=3;i++)Box(paint,i*1.15f,field.Row<0?-half+7:half-7,.45f,6,.005f,.009f,Surface.Accent);
            }
            if(field.Terminal)
            {
                // Small open terminal beside the runway, with an actual sheltered floor.
                foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Shapes.Column(o.Structure,x*5,z*5,.18f,0,4.4f,12,Surface.Metal,2);
                Box(o.Structure,0,0,11,11,4.4f,4.6f,Surface.Plaster);
                foreach(int side in new[]{-1,1})
                {Box(o.Structure,side*3.4f,3.5f,2.6f,.7f,0,.48f,Surface.Metal);Box(o.Structure,side*3.4f,3.8f,2.6f,.12f,.48f,1.1f,Surface.Plaster);}
                Box(o.Decoration,0,0,3,.4f,4.2f,4.25f,Surface.Glow);o.Decoration.EmissionTint=new V3(1,.94f,.86f);o.Decoration.EmissionStrength=.3f;
                o.Fixtures.Add(new Fixture{Position=new V3(0,4.18f,0),Width=3,Depth=.4f,Power=2500,Range=10,CustomColor=true,LightColor=o.Decoration.EmissionTint});
            }
            for(int side=0;side<4;side++)
            {
                long nx=(long)room.X+Directions.DX[side],nz=(long)room.Z+Directions.DZ[side];CityAirfieldCell neighbour;
                if(nx>=int.MinValue&&nx<=int.MaxValue&&nz>=int.MinValue&&nz<=int.MaxValue&&CityAirfields.At(c,(int)nx,(int)nz,out neighbour)&&neighbour.CenterX==field.CenterX&&neighbour.CenterZ==field.CenterZ)continue;
                // Keep existing walkable graph entrances at the outer boundary.
                var edge=new MeshData();float opening=room.HasExit(side)?c.DoorWidth*.5f:0;
                if(opening>0){Box(edge,-(half+opening)*.5f,0,half-opening,.12f,0,1.05f,Surface.Metal);Box(edge,(half+opening)*.5f,0,half-opening,.12f,0,1.05f,Surface.Metal);}
                else Box(edge,0,0,c.CellSize,.12f,0,1.05f,Surface.Metal);
                edge.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Structure.Append(edge);
            }
            return o;
        }
    }
}
