using System;

namespace Poolrooms.Core
{
    // Width depends on both actual regional addresses, not on load order. The
    // existing graph still decides which edges open; no new disconnected exits.
    public static class CityStreets
    {
        public const float RoadWidth=6;
        public static float OpeningWidth(Room room,int direction,LayoutConfig c)
        {
            if(room.Plan==null||room.Plan.City==null)return c.DoorWidth;
            long x=(long)room.X+Directions.DX[direction],z=(long)room.Z+Directions.DZ[direction];
            if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue||
                !c.Continuous&&(x<0||z<0||x>=c.Columns||z>=c.Rows))return c.DoorWidth;
            int nx=(int)x,nz=(int)z;ConnectedTerrainSite terrain;ConnectedMallSite mall;
            if(ConnectedTerrainTopology.Reserved(c,nx,nz)||ConnectedTerrainTopology.Gateway(c,room.X,room.Z,out terrain)||ConnectedTerrainTopology.Gateway(c,nx,nz,out terrain)||
                ConnectedMallTopology.At(c,nx,nz,out mall)||ConnectedMallTopology.Gateway(c,room.X,room.Z,out mall)||ConnectedMallTopology.Gateway(c,nx,nz,out mall)||
                LiminalRegions.At(c,nx,nz)!=LiminalEnvironment.City11)return c.DoorWidth;
            var field=room.Plan.City.Airfield;
            CityAirfieldCell other;
            if(field!=null&&CityAirfields.At(c,nx,nz,out other)&&field.CenterX==other.CenterX&&field.CenterZ==other.CenterZ)return c.CellSize;
            return Math.Max(c.DoorWidth,RoadWidth);
        }
    }
}
