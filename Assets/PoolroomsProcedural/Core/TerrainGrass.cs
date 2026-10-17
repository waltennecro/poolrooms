using System;

namespace Poolrooms.Core
{
    public enum TerrainGrassQuality { Off, Economy, High }
    public static class TerrainGrass
    {
        public const float PatchSize=16;
        public static int Radius(TerrainGrassQuality quality){return quality==TerrainGrassQuality.High?2:quality==TerrainGrassQuality.Economy?1:0;}
        public static int PatchBudget(TerrainGrassQuality quality){int r=Radius(quality);return quality==TerrainGrassQuality.Off?0:(r*2+1)*(r*2+1);}
        public static int Cell(float v){return (int)Math.Floor((v+PatchSize*.5f)/PatchSize);}
        public static float SurfaceHeight(TerrainRegionPlan p,float x,float z)
        {
            // Match the actual near ground triangles, rather than floating over their interpolation.
            float step=TerrainRegionPlan.TileSize/32,ax=(float)Math.Floor(x/step)*step,az=(float)Math.Floor(z/step)*step;
            float u=(x-ax)/step,v=(z-az)/step,a=p.Height(ax,az),b=p.Height(ax+step,az),c=p.Height(ax+step,az+step),d=p.Height(ax,az+step);
            return u>=v?a+(b-a)*u+(c-b)*v:a+(d-a)*v+(c-d)*u;
        }
        public static bool Allowed(TerrainRegionPlan p,float x,float z)
        {
            if(!p.Contains(x,z)||p.Connected&&p.BoundaryDistance(x,z)<16||x*x+z*z<100||p.PathDistance(x,z)<4.4f)return false;
            int cx=TerrainRegionPlan.Cell(x),cz=TerrainRegionPlan.Cell(z);
            for(int iz=cz-1;iz<=cz+1;iz++)for(int ix=cx-1;ix<=cx+1;ix++)
            {var h=p.House(ix,iz);if(h!=null&&Math.Abs(x-h.X)<h.Width*.5f+4&&Math.Abs(z-h.Z)<h.Depth*.5f+5)return false;}
            return true;
        }
        public static MeshData Build(TerrainRegionPlan p,int cx,int cz,TerrainGrassQuality quality,int distanceRing=0)
        {
            var m=new MeshData();if(p==null||quality==TerrainGrassQuality.Off)return m;
            bool high=quality==TerrainGrassQuality.High;int ring=Math.Max(0,Math.Min(2,distanceRing));
            int n=high?(ring==0?24:ring==1?18:12):10,blades=high?(ring<2?ring==0?5:4:3):3;
            var random=new StableRandom(StableRandom.Hash(p.Seed,cx,cz,0x943300u));
            float step=PatchSize/n,ox=cx*PatchSize,oz=cz*PatchSize;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            {
                float px=ox-PatchSize*.5f+(x+.5f+random.Range(-.3f,.3f))*step,pz=oz-PatchSize*.5f+(z+.5f+random.Range(-.3f,.3f))*step;
                if(!Allowed(p,px,pz))continue;V3 normal=p.Normal(px,pz);if(normal.Y<.78f)continue;
                V3 basePoint=new V3(px-ox,SurfaceHeight(p,px,pz)-.025f,pz-oz),up=(V3.Up*.8f+normal*.2f).Unit;
                float height=random.Range(high?.18f:.12f,high?.48f:.29f),angle=random.Range(0,6.2832f);
                for(int b=0;b<blades;b++)
                {
                    float a=angle+b*2.39996f;V3 side=new V3((float)Math.Cos(a),0,(float)Math.Sin(a));
                    V3 bend=new V3(-side.Z,0,side.X)*height*.24f,foot=basePoint+side*random.Range(0,.11f);
                    float width=high?.027f:.034f;V3 left=foot-side*width,right=foot+side*width;
                    V3 tip=foot+up*height+bend,face=(V3.Cross(right-left,tip-left).Unit*.65f+up*.35f).Unit;
                    if(high)
                    {
                        V3 middle=foot+up*(height*.58f)+bend*.32f;
                        m.Quad(left,right,middle+side*width*.55f,middle-side*width*.55f,face,Surface.Tile);
                        m.Triangle(middle-side*width*.55f,middle+side*width*.55f,tip,face,face,face,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Plaster);
                    }
                    else m.Triangle(left,right,tip,face,face,face,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Tile);
                }
            }
            return m;
        }
    }
}
