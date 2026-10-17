using System;

namespace Poolrooms.Core
{
    // Logical coordinates stay stable while Unity positions remain near the scene anchor.
    public sealed class ContinuousOrigin
    {
        public int X { get; private set; }
        public int Z { get; private set; }
        public void Reset(){X=Z=0;}
        public int CellX(float local,float cell){return checked(X+(int)Math.Floor(local/cell+.5f));}
        public int CellZ(float local,float cell){return checked(Z+(int)Math.Floor(local/cell+.5f));}
        public V3 Offset(int x,int z,float cell)
        {return new V3((float)((long)x-X)*cell,0,(float)((long)z-Z)*cell);}
        public bool NeedsShift(int x,int z)
        {return Math.Abs((long)x-X)>12||Math.Abs((long)z-Z)>12;}
        public V3 Recenter(int x,int z,float cell)
        {V3 shift=Offset(x,z,cell)*-1;X=x;Z=z;return shift;}
    }
}
