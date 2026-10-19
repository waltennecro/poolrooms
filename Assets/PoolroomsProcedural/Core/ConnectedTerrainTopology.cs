using System;

namespace Poolrooms.Core
{
    public readonly struct ConnectedTerrainSite
    {
        public readonly int X,Z,RadiusX,RadiusZ;
        public readonly float HalfWidth,HalfDepth;
        public ConnectedTerrainSite(LayoutConfig c,int x,int z)
        {
            X=x;Z=z;var rng=new StableRandom(StableRandom.Hash(c.Seed,x,z,0x940024u));
            int originalX=(int)Math.Ceiling((12+rng.Range(9))*64/c.CellSize),originalZ=(int)Math.Ceiling((12+rng.Range(9))*64/c.CellSize);
            int rx,rz;BiomeExtent.Radii(c,originalX,originalZ,out rx,out rz);RadiusX=rx;RadiusZ=rz;
            HalfWidth=(RadiusX+.5f)*c.CellSize;HalfDepth=(RadiusZ+.5f)*c.CellSize;
        }
        public long Key { get { return ContinuousTopology.CellKey(X,Z); } }
        public bool Contains(int x,int z){return Math.Abs((long)x-X)<=RadiusX&&Math.Abs((long)z-Z)<=RadiusZ;}
        public int GateX(int d){return X+Directions.DX[d]*(RadiusX+1);}
        public int GateZ(int d){return Z+Directions.DZ[d]*(RadiusZ+1);}
        public bool Gateway(int x,int z){for(int d=0;d<4;d++)if(x==GateX(d)&&z==GateZ(d))return true;return false;}
        public int GateDirection(int x,int z){for(int d=0;d<4;d++)if(x==GateX(d)&&z==GateZ(d))return d;return -1;}
        public int Distance(int x,int z){return (int)Math.Min(int.MaxValue,Math.Max(Math.Max(0,Math.Abs((long)x-X)-RadiusX),Math.Max(0,Math.Abs((long)z-Z)-RadiusZ)));}
    }
    public static class ConnectedTerrainTopology
    {
        public const int BlockSize=192;
        public static bool Enabled(LayoutConfig c){return c.ConnectedTerrain&&c.Continuous&&c.ConnectedTerrainShare>0;}
        public static int Block(int cell){return (int)Math.Floor(((long)cell+BlockSize/2)/(double)BlockSize);}
        public static bool Site(LayoutConfig c,int bx,int bz,out ConnectedTerrainSite site)
        {
            site=default(ConnectedTerrainSite);if(!Enabled(c))return false;
            // The first meadow is south of spawn. Leave its neighbouring block empty so even
            // the largest district with the smallest supported room cells cannot intersect it.
            if(bx==0&&bz==-1)return false;
            uint h=StableRandom.Hash(c.Seed,bx,bz,0x290094u);
            if((bx!=0||bz!=0)&&(h>>8)*(1f/16777216f)>=c.ConnectedTerrainShare)return false;
            long x=(long)bx*BlockSize+(bx==0&&bz==0?0:(int)(h%9)-4);
            long z=(long)bz*BlockSize+(bx==0&&bz==0?-(int)Math.Ceiling(2048/c.CellSize)-12:(int)((h>>4)%9)-4);
            if(x<int.MinValue+96L||x>int.MaxValue-96L||z<int.MinValue+96L||z>int.MaxValue-96L)return false;
            site=new ConnectedTerrainSite(c,(int)x,(int)z);return true;
        }
        public static bool At(LayoutConfig c,int x,int z,out ConnectedTerrainSite site){return Find(c,x,z,0,out site);}
        public static bool Gateway(LayoutConfig c,int x,int z,out ConnectedTerrainSite site)
        {
            site=default(ConnectedTerrainSite);if(!Enabled(c))return false;
            int bx=Block(x),bz=Block(z);
            for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
            {ConnectedTerrainSite s;if(Site(c,ix,iz,out s)&&s.Gateway(x,z)){site=s;return true;}}
            return false;
        }
        public static bool Find(LayoutConfig c,int x,int z,int distance,out ConnectedTerrainSite site)
        {
            site=default(ConnectedTerrainSite);if(!Enabled(c))return false;int bx=Block(x),bz=Block(z),best=int.MaxValue;
            for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
            {ConnectedTerrainSite s;if(!Site(c,ix,iz,out s))continue;int d=s.Distance(x,z);if(d<=distance&&d<best){site=s;best=d;}}
            return best!=int.MaxValue;
        }
        public static bool Nearest(LayoutConfig c,int x,int z,out ConnectedTerrainSite site)
        {
            site=default(ConnectedTerrainSite);if(!Enabled(c))return false;
            int bx=Block(x),bz=Block(z),best=int.MaxValue;
            // Inspect every candidate in the bounded search, including neighbouring blocks
            // with a closer border than the district in the viewer's own block.
            for(int iz=bz-8;iz<=bz+8;iz++)for(int ix=bx-8;ix<=bx+8;ix++)
            {ConnectedTerrainSite candidate;if(!Site(c,ix,iz,out candidate))continue;int d=candidate.Distance(x,z);if(d<best){best=d;site=candidate;}}
            return best!=int.MaxValue;
        }
        public static bool Intersects(LayoutConfig c,int x,int z,int rx,int rz)
        {
            if(!Enabled(c))return false;int bx=Block(x),bz=Block(z);
            for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
            {ConnectedTerrainSite s;if(Site(c,ix,iz,out s)&&Math.Abs((long)x-s.X)<=rx+s.RadiusX+2&&Math.Abs((long)z-s.Z)<=rz+s.RadiusZ+2)return true;}
            return false;
        }
        public static bool Reserved(LayoutConfig c,int x,int z)
        {ConnectedTerrainSite terrain;ConnectedMallSite mall;return At(c,x,z,out terrain)||ConnectedMallTopology.At(c,x,z,out mall);}
        public static int Exits(LayoutConfig c,int x,int z,int original)
        {
            if(!Enabled(c))return original;
            ConnectedTerrainSite self;bool inside=At(c,x,z,out self),rim=!inside&&Find(c,x,z,1,out self);int exits=inside?0:original;
            for(int d=0;d<4;d++)
            {
                int nx=x+Directions.DX[d],nz=z+Directions.DZ[d];ConnectedTerrainSite other;bool occupied=At(c,nx,nz,out other);
                if(inside){if(occupied&&other.Key==self.Key||!occupied&&self.Gateway(nx,nz))exits|=1<<d;}
                else if(occupied){if(other.Gateway(x,z))exits|=1<<d;else exits&=~(1<<d);}
                else {ConnectedTerrainSite near;if(rim||Find(c,nx,nz,1,out near))exits|=1<<d;}
            }
            return exits;
        }
    }
}
