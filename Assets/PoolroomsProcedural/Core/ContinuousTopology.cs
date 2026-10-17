using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Connected local trees + guaranteed randomly placed links between adjoining tiles.
    // Rooms, details and light use global coordinates, not tile traversal or generation order.
    public sealed class ContinuousTopology
    {
        private readonly LayoutConfig config;
        private readonly Dictionary<long,Layout> tiles=new Dictionary<long,Layout>();
        private readonly List<long> retired=new List<long>();
        public int Count { get { return tiles.Count; } }
        public ContinuousTopology(LayoutConfig c){config=c;}
        public static long CellKey(int x,int z){return unchecked(((long)x<<32)|(uint)z);}
        public static int FloorDivide(int value,int size)
        {int result=value/size;return value%size<0?result-1:result;}

        public static bool FindZone(LayoutConfig c,int zone,int x,int z,out int foundX,out int foundZ)
        {
            foundX=x;foundZ=z;
            if(zone<0||zone>=ZoneCatalog.Count||c.Zones==ZoneMode.Legacy)return false;
            if(c.Zones==ZoneMode.Single&&c.Procedural==LiminalGeneration.Authored)return c.SingleZone==zone;
            if(c.Zones==ZoneMode.Single&&c.SingleZone!=zone)return false;
            if(c.Zones!=ZoneMode.Single&&c.ZoneFamilyFilter>=0&&(int)ZoneCatalog.Get(zone).Family!=c.ZoneFamilyFilter)return false;
            for(int radius=0;radius<=128;radius++)
            for(int offset=-radius;offset<=radius;offset++)
            {
                // Visit the ring, not its whole area at each successive radius.
                for(int side=0;side<4;side++)
                {
                    int xx=x+(side==0||side==2?offset:side==1?radius:-radius);
                    int zz=z+(side==1||side==3?offset:side==0?radius:-radius);
                    if(ConnectedTerrainTopology.Reserved(c,xx,zz))continue;
                    if(c.Zones==ZoneMode.Mixed&&StableRandom.Hash(c.Seed,xx,zz,0x81u)%8==0)continue;
                    if((c.Procedural!=LiminalGeneration.Authored||PoolroomArchitectureMix.Enabled(c))&&!LiminalRegions.UsesAuthored(c,xx,zz))continue;
                    if((c.Zones==ZoneMode.Single?c.SingleZone:ZoneCatalog.RandomAt(c,xx,zz))!=zone)continue;
                    foundX=xx;foundZ=zz;return true;
                }
            }
            return false;
        }

        private Layout Tile(int x,int z)
        {
            long key=CellKey(x,z);Layout tile;if(tiles.TryGetValue(key,out tile))return tile;
            int size=config.DistrictSize;
            var c=new LayoutConfig{Seed=unchecked((int)StableRandom.Hash(config.Seed,x,z,0x71E1u)),Columns=size,Rows=size,
                CellSize=config.CellSize,Distribution=config.Distribution,LoopChance=config.LoopChance,
                DistrictSize=size,CurveSegments=12};
            tile=new Layout(c);tiles.Add(key,tile);return tile;
        }

        public int Exits(int x,int z)
        {
            int size=config.DistrictSize,tx=FloorDivide(x,size),tz=FloorDivide(z,size);
            int localX=(int)(x-(long)tx*size),localZ=(int)(z-(long)tz*size),exits=Tile(tx,tz).At(localX,localZ).Exits;
            if(localX==size-1&&Gate(tx,tz,localZ,1))exits|=1<<1;
            if(localX==0&&Gate(tx-1,tz,localZ,1))exits|=1<<3;
            if(localZ==size-1&&Gate(tx,tz,localX,0))exits|=1;
            if(localZ==0&&Gate(tx,tz-1,localX,0))exits|=1<<2;
            return exits;
        }

        private bool Gate(int tx,int tz,int offset,int direction)
        {
            uint salt=0xB01Du+(uint)direction;
            if(offset==(int)(StableRandom.Hash(config.Seed,tx,tz,salt)%(uint)config.DistrictSize))return true;
            float chance=config.LoopChance;
            if(config.Distribution==LayoutStyle.Maze||config.Distribution==LayoutStyle.Galleries)chance*=chance;
            uint hash=StableRandom.Hash(config.Seed,tx,tz,salt+19u+(uint)offset*7u);
            return (hash>>8)*(1f/16777216f)<chance;
        }

        public void Trim(int x,int z,int radius)
        {
            int size=config.DistrictSize;
            int x0=FloorDivide(x-radius-1,size),x1=FloorDivide(x+radius+1,size);
            int z0=FloorDivide(z-radius-1,size),z1=FloorDivide(z+radius+1,size);
            retired.Clear();
            foreach(var pair in tiles)
            {
                int tx=(int)(pair.Key>>32),tz=unchecked((int)pair.Key);
                if(tx<x0||tx>x1||tz<z0||tz>z1)retired.Add(pair.Key);
            }
            foreach(long key in retired)tiles.Remove(key);retired.Clear();
        }
    }
}
