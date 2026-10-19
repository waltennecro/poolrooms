using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct RoomCell
    {
        public readonly int X,Z;
        public RoomCell(int x,int z){X=x;Z=z;}
    }
    public static class RoomStreamingSearch
    {
        // Select coordinates before describing a room. A wide view must not build hundreds
        // of unused plans on the main thread just to select one background geometry job.
        public static Room Next<T>(Layout layout,IReadOnlyDictionary<long,T> loaded,int focusX,int focusZ,
            bool local,int radius,IReadOnlyList<RoomCell> anchors)
        {
            var c=layout.Config;long best=long.MaxValue;int nextX=0,nextZ=0;bool found=false;
            long x0=c.Continuous?(long)focusX-radius:local?Math.Max(0L,(long)focusX-radius):0;
            long x1=c.Continuous?(long)focusX+radius:local?Math.Min(c.Columns-1L,(long)focusX+radius):c.Columns-1;
            long z0=c.Continuous?(long)focusZ-radius:local?Math.Max(0L,(long)focusZ-radius):0;
            long z1=c.Continuous?(long)focusZ+radius:local?Math.Min(c.Rows-1L,(long)focusZ+radius):c.Rows-1;
            x0=Math.Max(int.MinValue,x0);x1=Math.Min(int.MaxValue,x1);z0=Math.Max(int.MinValue,z0);z1=Math.Min(int.MaxValue,z1);
            for(long z=z0;z<=z1;z++)for(long x=x0;x<=x1;x++)
            {
                long dx=x-focusX,dz=z-focusZ,score=dx*dx+dz*dz;
                if(score>=best||loaded.ContainsKey(ContinuousTopology.CellKey((int)x,(int)z))||ConnectedTerrainTopology.Reserved(c,(int)x,(int)z))continue;
                nextX=(int)x;nextZ=(int)z;best=score;found=true;
            }
            if(!found&&anchors!=null)foreach(var p in anchors)
            for(long z=(long)p.Z-1;z<=(long)p.Z+1;z++)for(long x=(long)p.X-1;x<=(long)p.X+1;x++)
            {
                if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue||!c.Continuous&&(x<0||x>=c.Columns||z<0||z>=c.Rows))continue;
                long dx=x-p.X,dz=z-p.Z,score=dx*dx+dz*dz;
                if(score>=best||loaded.ContainsKey(ContinuousTopology.CellKey((int)x,(int)z))||ConnectedTerrainTopology.Reserved(c,(int)x,(int)z))continue;
                nextX=(int)x;nextZ=(int)z;best=score;found=true;
            }
            return found?layout.At(nextX,nextZ):null;
        }
    }
}
