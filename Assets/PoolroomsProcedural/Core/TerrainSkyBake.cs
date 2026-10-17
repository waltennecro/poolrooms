namespace Poolrooms.Core
{
    public static class TerrainSkyBake
    {
        // Unity CubemapFace order; samples are generated without accessing engine objects.
        public static V3 Direction(int face,float u,float v)
        {
            switch(face)
            {
                case 0:return new V3(1,-v,-u).Unit;
                case 1:return new V3(-1,-v,u).Unit;
                case 2:return new V3(u,1,v).Unit;
                case 3:return new V3(u,-1,-v).Unit;
                case 4:return new V3(u,-v,1).Unit;
                default:return new V3(-u,-v,-1).Unit;
            }
        }
        public static V3[] Face(TerrainRegionPlan plan,int size,int face)
        {
            var pixels=new V3[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                pixels[y*size+x]=plan.Sky(Direction(face,2*(x+.5f)/size-1,2*(y+.5f)/size-1));
            return pixels;
        }
    }
}
