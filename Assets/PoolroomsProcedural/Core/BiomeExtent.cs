using System;

namespace Poolrooms.Core
{
    // An area multiplier is a square-root change in each axis, not +50% per axis.
    // Logical region IDs and their shuffled biome labels remain unchanged.
    public static class BiomeExtent
    {
        public const double AreaMultiplier=1.5;
        public static double Linear(LayoutConfig c){return c.ExpandedBiomeAreas?Math.Sqrt(AreaMultiplier):1;}
        public static double Spacing(LayoutConfig c,int original){return original*Linear(c);}
        public static double RegionSpacing(LayoutConfig c){return Spacing(c,c.EnvironmentRegionSize)*(LargeRegions.Enabled(c)?c.LargeRegionScale:1);}
        public static int Cell(int coordinate,double spacing){return (int)Math.Floor(coordinate/spacing);}
        public static void Radii(LayoutConfig c,int originalX,int originalZ,out int radiusX,out int radiusZ)
        {
            radiusX=originalX;radiusZ=originalZ;if(!c.ExpandedBiomeAreas)return;
            double x=(originalX+.5)*Linear(c)-.5,z=(originalZ+.5)*Linear(c)-.5;
            double originalArea=(2.0*originalX+1)*(2.0*originalZ+1),best=double.MaxValue;
            // Choose the closest whole-cell rectangle jointly. Rounding both axes up
            // can overshoot +50% by nearly twenty points for small terrain districts.
            for(int rx=(int)Math.Floor(x);rx<=Math.Ceiling(x);rx++)for(int rz=(int)Math.Floor(z);rz<=Math.Ceiling(z);rz++)
            {
                double error=Math.Abs((2.0*rx+1)*(2.0*rz+1)/originalArea-AreaMultiplier);
                if(error<best){best=error;radiusX=rx;radiusZ=rz;}
            }
        }
        public static void NearestRegion(LayoutConfig c,int x,int z,out int selectedX,out int selectedZ)
        {
            double size=RegionSpacing(c);
            int rx=Cell(x,size),rz=Cell(z,size);selectedX=selectedZ=0;double best=double.MaxValue;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                int sx=rx+dx,sz=rz+dz;
                uint a=LiminalRegions.Hash(c.Seed,sx,sz,0xE01u),b=LiminalRegions.Hash(c.Seed,sx,sz,0xE02u);
                double px=sx*size+(a>>8)*(size/16777216.0),pz=sz*size+(b>>8)*(size/16777216.0);
                if(sx==0&&sz==0){px=0;pz=0;}
                double xx=x-px,zz=z-pz,distance=xx*xx+zz*zz;
                if(distance<best){best=distance;selectedX=sx;selectedZ=sz;}
            }
        }
    }
}
