using System;

namespace Poolrooms.Core
{
    // One coordinate system for indoor rooms and multi-cell buildings. No traversal-order state.
    public readonly struct ConnectedMallSite
    {
        public readonly int X,Z,RadiusX,RadiusZ,Floors,Bays;
        public readonly float Width,Depth;
        public ConnectedMallSite(LayoutConfig c,int x,int z)
        {
            X=x;Z=z;
            var random=new StableRandom(LiminalRegions.Hash(c.Seed,x,z,0xCA110u));
            int floors,bays;float depth;MallPlan.Dimensions(ref random,c.Seed,x,z,c.ExpandedVariety,c.ExpandedBiomeAreas,out floors,out bays,out depth,LargeRegions.Enabled(c));
            Floors=floors;Bays=bays;Depth=depth;
            Width=Bays*MallPlan.BayWidth+56;
            RadiusX=(int)Math.Ceiling(Width*.5f/c.CellSize);
            RadiusZ=(int)Math.Ceiling(Depth*.5f/c.CellSize);
        }
        public long Key { get { return ContinuousTopology.CellKey(X,Z); } }
        public bool Contains(int x,int z){return Math.Abs((long)x-X)<=RadiusX&&Math.Abs((long)z-Z)<=RadiusZ;}
        public bool Gateway(int x,int z){return z==Z&&Math.Abs((long)x-X)==RadiusX+1;}
        public int Distance(int x,int z)
        {return (int)Math.Min(int.MaxValue,Math.Max(Math.Max(0,Math.Abs((long)x-X)-RadiusX),Math.Max(0,Math.Abs((long)z-Z)-RadiusZ)));}
    }
    public static class ConnectedMallTopology
    {
        public const int BlockSize=24;
        public static bool Enabled(LayoutConfig c){return c.ConnectedMalls&&c.Continuous&&c.ConnectedMallShare>0;}
        public static bool Site(LayoutConfig c,int bx,int bz,out ConnectedMallSite site)
        {
            site=default(ConnectedMallSite);if(!Enabled(c))return false;
            bool spacious=LargeRegions.Enabled(c);if(spacious&&bx==1&&bz==0)return false;
            uint h=StableRandom.Hash(c.Seed,bx,bz,0x28001u);
            if((bx!=0||bz!=0)&&(h>>8)*(1f/16777216f)>=c.ConnectedMallShare)return false;
            // A first connected district near spawn; subsequent locations vary by seed.
            long x=(long)bx*BlockSize+(bx==0&&bz==0?(spacious?13:9):spacious?0:(int)(h%5)-2);
            long z=(long)bz*BlockSize+(bx==0&&bz==0?0:spacious?0:(int)((h>>4)%5)-2);
            long margin=spacious?12:8;
            if(x<int.MinValue+margin||x>int.MaxValue-margin||z<int.MinValue+margin||z>int.MaxValue-margin)return false;
            site=new ConnectedMallSite(c,(int)x,(int)z);
            return !ConnectedTerrainTopology.Intersects(c,site.X,site.Z,site.RadiusX+1,site.RadiusZ+1);
        }
        public static int Block(int cell){return (int)Math.Floor(((long)cell+BlockSize/2)/(double)BlockSize);}
        public static bool At(LayoutConfig c,int x,int z,out ConnectedMallSite site)
        {
            site=default(ConnectedMallSite);if(!Enabled(c))return false;
            int bx=Block(x),bz=Block(z);
            for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
            {ConnectedMallSite candidate;if(Site(c,ix,iz,out candidate)&&candidate.Contains(x,z)){site=candidate;return true;}}
            return false;
        }
        public static bool Gateway(LayoutConfig c,int x,int z,out ConnectedMallSite site)
        {
            site=default(ConnectedMallSite);if(!Enabled(c))return false;
            int bx=Block(x),bz=Block(z);
            for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
            {ConnectedMallSite candidate;if(Site(c,ix,iz,out candidate)&&candidate.Gateway(x,z)){site=candidate;return true;}}
            return false;
        }
        private static bool Near(LayoutConfig c,int x,int z)
        {
            int bx=Block(x),bz=Block(z);
            for(int iz=bz-1;iz<=bz+1;iz++)for(int ix=bx-1;ix<=bx+1;ix++)
            {ConnectedMallSite site;if(Site(c,ix,iz,out site)&&site.Distance(x,z)<=1)return true;}
            return false;
        }
        public static int Exits(LayoutConfig c,int x,int z,int original)
        {
            if(!Enabled(c))return original;
            ConnectedMallSite self;bool reserved=At(c,x,z,out self),rim=!reserved&&Near(c,x,z);
            int exits=reserved?0:original;
            for(int d=0;d<4;d++)
            {
                int nx=x+Directions.DX[d],nz=z+Directions.DZ[d];ConnectedMallSite neighbour;
                bool occupied=At(c,nx,nz,out neighbour);
                if(reserved)
                {
                    if(occupied&&neighbour.Key==self.Key||!occupied&&self.Gateway(nx,nz))exits|=1<<d;
                }
                else if(occupied)
                {if(neighbour.Gateway(x,z))exits|=1<<d;else exits&=~(1<<d);}
                else
                {
                    // Preserve the old maze away from districts. A connected perimeter prevents
                    // a reserved footprint from cutting a formerly connected room tree in two.
                    bool otherRim=Near(c,nx,nz);
                    if(rim||otherRim)exits|=1<<d;
                }
            }
            return exits;
        }
    }
}
