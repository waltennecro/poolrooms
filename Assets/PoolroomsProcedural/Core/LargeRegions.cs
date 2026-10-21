using System;

namespace Poolrooms.Core
{
    public enum RegionSection { Galleries,Forum,Workshops,Exhibition,Rest }
    // Physical extent is independent from chunk size and view distance. This field
    // is queried by coordinate; it never creates or caches all the rooms in a region.
    public static class LargeRegions
    {
        public const int HallSpan=4;
        public static bool Enabled(LayoutConfig c){return c.LargeProceduralRegions&&c.Continuous&&c.Procedural!=LiminalGeneration.Authored;}
        public static RegionSection Section(LayoutConfig c,int x,int z,LiminalEnvironment environment)
        {
            if(!Enabled(c))return RegionSection.Galleries;
            int sx=ContinuousTopology.FloorDivide(x,HallSpan),sz=ContinuousTopology.FloorDivide(z,HallSpan);
            return (RegionSection)(LiminalRegions.Hash(c.Seed,sx,sz,0x420000u+(uint)environment)%5);
        }
        public static string SectionName(RegionSection s)
        {switch(s){case RegionSection.Forum:return "salón principal";case RegionSection.Workshops:return "talleres";case RegionSection.Exhibition:return "exposición";case RegionSection.Rest:return "descanso";default:return "galerías";}}
        public static bool OpenEdge(LayoutConfig c,int x,int z,int side,LiminalEnvironment environment)
        {
            if(!Enabled(c)||!RestfulLevelPlanner.IsLevel(environment)||Section(c,x,z,environment)!=RegionSection.Forum)return false;
            long nx=(long)x+Directions.DX[side],nz=(long)z+Directions.DZ[side];
            if(nx<int.MinValue||nx>int.MaxValue||nz<int.MinValue||nz>int.MaxValue)return false;
            int xx=(int)nx,zz=(int)nz;
            if(ContinuousTopology.FloorDivide(x,HallSpan)!=ContinuousTopology.FloorDivide(xx,HallSpan)||ContinuousTopology.FloorDivide(z,HallSpan)!=ContinuousTopology.FloorDivide(zz,HallSpan))return false;
            if(LiminalRegions.At(c,x,z)!=environment||LiminalRegions.At(c,xx,zz)!=environment)return false;
            ConnectedMallSite mall;ConnectedTerrainSite terrain;
            return !ConnectedTerrainTopology.Reserved(c,x,z)&&!ConnectedTerrainTopology.Reserved(c,xx,zz)&&
                !ConnectedMallTopology.Gateway(c,x,z,out mall)&&!ConnectedMallTopology.Gateway(c,xx,zz,out mall)&&
                !ConnectedTerrainTopology.Gateway(c,x,z,out terrain)&&!ConnectedTerrainTopology.Gateway(c,xx,zz,out terrain);
        }
        public static int Exits(LayoutConfig c,int x,int z,int original)
        {
            if(!Enabled(c))return original;
            // Reciprocal through-routes every six cells shorten traversals of extensive
            // districts. Reservation wrappers still own mall and terrain boundaries.
            int result=original;if(x%6==0)result|=5;if(z%6==0)result|=10;
            var e=LiminalRegions.At(c,x,z);
            if(RestfulLevelPlanner.IsLevel(e))for(int side=0;side<4;side++)if(OpenEdge(c,x,z,side,e))result|=1<<side;
            return result;
        }
        public static bool Center(LayoutConfig c,int regionX,int regionZ,out int x,out int z)
        {
            double size=BiomeExtent.RegionSpacing(c);
            double px=regionX*size+(LiminalRegions.Hash(c.Seed,regionX,regionZ,0xE01u)>>8)*(size/16777216.0);
            double pz=regionZ*size+(LiminalRegions.Hash(c.Seed,regionX,regionZ,0xE02u)>>8)*(size/16777216.0);
            if(regionX==0&&regionZ==0)px=pz=0;
            x=z=0;if(px<int.MinValue+.5||px>int.MaxValue-.5||pz<int.MinValue+.5||pz>int.MaxValue-.5)return false;
            x=(int)Math.Round(px);z=(int)Math.Round(pz);return true;
        }
        public static bool Find(LayoutConfig c,LiminalEnvironment environment,int x,int z,out int foundX,out int foundZ)
        {
            foundX=x;foundZ=z;
            if(LiminalRegions.At(c,x,z)==environment&&Usable(c,x,z))return true;
            BiomeExtent.NearestRegion(c,x,z,out int rx,out int rz);
            bool found=false;double best=double.MaxValue;int first=-1;
            // Search logical region centres, not millions of physical room addresses.
            for(int radius=0;radius<=16&&(first<0||radius<=first+2);radius++)
                for(int offset=-radius;offset<=radius;offset++)for(int side=0;side<4;side++)
                {
                    int xx=rx+(side%2==0?offset:side==1?radius:-radius),zz=rz+(side%2==1?offset:side==0?radius:-radius);
                    if(!Center(c,xx,zz,out int cx,out int cz))continue;
                    for(int probe=0;probe<5;probe++)
                    {
                        long ax=(long)cx+(probe==0?0:Directions.DX[probe-1]*3),az=(long)cz+(probe==0?0:Directions.DZ[probe-1]*3);
                        if(ax<int.MinValue||ax>int.MaxValue||az<int.MinValue||az>int.MaxValue)continue;
                        int px=(int)ax,pz=(int)az;
                        double dx=ax-x,dz=az-z,score=dx*dx+dz*dz;if(score>=best||LiminalRegions.At(c,px,pz)!=environment||!Usable(c,px,pz))continue;
                        best=score;foundX=px;foundZ=pz;found=true;if(first<0)first=radius;
                    }
                }
            return found;
        }
        static bool Usable(LayoutConfig c,int x,int z)
        {return !ConnectedTerrainTopology.Reserved(c,x,z)&&!ConnectedMallTopology.Gateway(c,x,z,out _)&&!ConnectedTerrainTopology.Gateway(c,x,z,out _);}
        public static bool Different(LayoutConfig c,int x,int z,out int tx,out int tz)
        {
            tx=x;tz=z;var initial=LiminalRegions.At(c,x,z);
            int limit=(int)Math.Min(2048,Math.Ceiling(BiomeExtent.RegionSpacing(c)*4));
            for(int radius=1;radius<=limit;radius++)for(int side=0;side<8;side++)
            {
                long xx=(long)x+(side<4?Directions.DX[side]:(side%2==0?-1:1))*radius;
                long zz=(long)z+(side<4?Directions.DZ[side]:(side<6?-1:1))*radius;
                if(xx<int.MinValue||xx>int.MaxValue||zz<int.MinValue||zz>int.MaxValue)continue;
                if(LiminalRegions.At(c,(int)xx,(int)zz)==initial||!Usable(c,(int)xx,(int)zz))continue;
                tx=(int)xx;tz=(int)zz;return true;
            }
            return false;
        }
    }
}
