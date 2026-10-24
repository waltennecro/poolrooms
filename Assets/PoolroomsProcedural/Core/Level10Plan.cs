using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum Level10Place
    {
        OpenWheat, BarnLoft, Stable, Granary, PumpCourt, Lakeside,
        ToolShed, HarvestCourt, HayShelter, SeedHouse, OrchardRest, LoadingPorch
    }
    public readonly struct Level10Stalk
    {
        public readonly V3 Foot;
        public readonly float Height,Yaw,Lean;
        public readonly uint Seed;
        public Level10Stalk(V3 foot,float height,float yaw,float lean,uint seed)
        {Foot=foot;Height=height;Yaw=yaw;Lean=lean;Seed=seed;}
    }
    // The cell is a streaming address inside open countryside, not a room footprint.
    public sealed class Level10Plan
    {
        public const int Programmes=12,MaximumStalks=1200;
        public readonly int X,Z;
        public readonly uint Seed;
        public readonly float Cell,Half,TrackHalf;
        public int RoofStyle {get{return (int)((Seed>>12)%3);}}
        public readonly Level10Place Place;
        public readonly PlanRect Farm,Rest,Notes;
        public readonly WaterRegion Lake;
        public readonly WalkingRoute[] Routes;
        public readonly Level10Stalk[] Stalks;
        public readonly Forest39Tree[] Trees;
        public readonly bool[] OpenEdges;
        public readonly float[] Elevation;
        public bool Building {get{return Place!=Level10Place.OpenWheat&&Place!=Level10Place.Lakeside&&Place!=Level10Place.HarvestCourt&&Place!=Level10Place.OrchardRest;}}
        public bool Loft {get{return Place==Level10Place.BarnLoft||Place==Level10Place.Granary||Place==Level10Place.HayShelter;}}
        public float LoftY {get{return 2.8f;}}
        public Level10Plan(LayoutConfig config,int x,int z)
        {
            if(config==null)throw new ArgumentNullException(nameof(config));
            if(float.IsNaN(config.CellSize)||float.IsInfinity(config.CellSize)||config.CellSize<24||config.CellSize>256)throw new ArgumentOutOfRangeException(nameof(config.CellSize));
            X=x;Z=z;Cell=config.CellSize;Half=Cell*.5f;
            Seed=LiminalRegions.Hash(config.Seed,x,z,0x100101u);Place=(Level10Place)(Seed%Programmes);
            TrackHalf=Math.Max(1.7f,config.DoorWidth*.5f+.15f);
            float q=Half*.58f,w=Math.Min(10.4f,Cell*.27f)*(.90f+((Seed>>18)%5)*.025f),d=Math.Min(10.0f,Cell*.27f)*(.90f+((Seed>>22)%5)*.025f);
            Farm=new PlanRect(q-w*.5f,q-d*.5f,q+w*.5f,q+d*.5f);
            Notes=new PlanRect(q+1.5f,Farm.MinZ-1.8f,q+2.5f,Farm.MinZ-.8f);
            Rest=new PlanRect(-q-2.6f,-q-2,-q+2.6f,-q+2);
            float r=Math.Min(3.6f,Half*.22f);
            Lake=Place==Level10Place.Lakeside||Place==Level10Place.PumpCourt||Seed%5==0?
                new WaterRegion{CenterX=-q,CenterZ=q,Width=r*2,Depth=r*2,Radius=r,Bottom=-.9f,Level=-.12f}:new WaterRegion();
            Routes=BuildRoutes();Elevation=Level10Terrain.Sample(this);OpenEdges=new bool[4];
            for(int side=0;side<4;side++)OpenEdges[side]=Level10Planner.Same(config,x,z,side);
            Trees=TreeRow();Stalks=Plant();
        }
        WalkingRoute[] BuildRoutes()
        {
            var list=new List<WalkingRoute>();
            for(int side=0;side<4;side++)
            {
                var points=new V2[25];float bend=((int)((Seed>>(side*5))%17)-8)*.12f;
                for(int i=0;i<points.Length;i++)
                {
                    float t=i/24f,shift=bend*(float)Math.Sin(Math.PI*t)*(float)Math.Sin(Math.PI*t);
                    points[i]=new V2(Directions.DX[side]*Half*t+Directions.DZ[side]*shift,
                        Directions.DZ[side]*Half*t-Directions.DX[side]*shift);
                }
                list.Add(new WalkingRoute(side,points,TrackHalf));
            }
            list.Add(new WalkingRoute(-1,new[]{new V2(0,0),new V2(Farm.X,Farm.MinZ-1.1f),new V2(Farm.X,Farm.Z)},1.1f));
            list.Add(new WalkingRoute(-1,new[]{new V2(0,0),new V2(Rest.X,Rest.Z)},1.1f));
            if(Lake.Width>0)list.Add(new WalkingRoute(-1,new[]{new V2(0,0),new V2(Lake.CenterX+Lake.Radius+1,Lake.CenterZ)},1.1f));
            return list.ToArray();
        }
        public float TrackDistance(float x,float z)
        {
            float distance=float.MaxValue;
            foreach(var route in Routes)for(int i=1;i<route.Points.Length;i++)
                distance=Math.Min(distance,Forest39Trail.SegmentDistance(x,z,route.Points[i-1].X,route.Points[i-1].Y,route.Points[i].X,route.Points[i].Y)-route.HalfWidth);
            return distance;
        }
        public Surface GroundSurface(float x,float z)
        {
            if(Lake.Width>0&&Height(x,z)<-.02f)return Surface.Accent;
            if(Farm.Expanded(.3f).Contains(x,z)||Rest.Contains(x,z))return Surface.Accent;
            foreach(var route in Routes)
            {
                float near=float.MaxValue;
                for(int i=1;i<route.Points.Length;i++)near=Math.Min(near,Forest39Trail.SegmentDistance(x,z,route.Points[i-1].X,route.Points[i-1].Y,route.Points[i].X,route.Points[i].Y));
                if(route.Exit<0&&near<.75f||route.Exit>=0&&Math.Abs(near-.85f)<.32f)return Surface.Accent;
            }
            return Surface.Tile;
        }
        public float Height(float x,float z)
        {
            float ex=(float)Math.Sin(Math.PI*(x+Half)/Cell),ez=(float)Math.Sin(Math.PI*(z+Half)/Cell);
            float clear=Smooth(TrackDistance(x,z)/1.1f);
            clear*=Smooth(Forest39Plan.DistanceToRect(Farm.Expanded(.9f),x,z));
            clear*=Smooth(Forest39Plan.DistanceToRect(Rest.Expanded(.7f),x,z));
            clear*=Smooth(Forest39Plan.DistanceToRect(Notes.Expanded(.45f),x,z));
            float height=(.35f+.18f*(float)Math.Sin(x*.17f+Seed%11)+.12f*(float)Math.Cos(z*.19f))*ex*ex*ez*ez*clear;
            if(Lake.Width>0)
            {
                float dx=x-Lake.CenterX,dz=z-Lake.CenterZ,d=(float)Math.Sqrt(dx*dx+dz*dz);
                if(d<Lake.Radius+1.1f)
                {
                    float bowl=Lake.Bottom*(1-Smooth((d-Lake.Radius*.5f)/(Lake.Radius*.5f)));
                    height=d<=Lake.Radius?bowl:height*Smooth((d-Lake.Radius)/1.1f);
                }
            }
            return height;
        }
        public bool CropClear(float x,float z,float margin=.24f)
        {
            if(Math.Abs(x)+margin>Half-1.25f||Math.Abs(z)+margin>Half-1.25f||TrackDistance(x,z)<margin+.18f)return false;
            if(Farm.Expanded(.7f+margin).Contains(x,z)||Rest.Expanded(.7f+margin).Contains(x,z)||Notes.Expanded(.5f+margin).Contains(x,z))return false;
            if(Lake.Width>0)
            {float dx=x-Lake.CenterX,dz=z-Lake.CenterZ;if(dx*dx+dz*dz<(Lake.Radius+1+margin)*(Lake.Radius+1+margin))return false;}
            if(Trees!=null)foreach(var tree in Trees)
            {float dx=x-tree.Foot.X,dz=z-tree.Foot.Z;if(dx*dx+dz*dz<(tree.Radius+margin+.4f)*(tree.Radius+margin+.4f))return false;}
            return true;
        }
        Forest39Tree[] TreeRow()
        {
            var list=new List<Forest39Tree>(2);float q=Half*.58f;
            for(int index=0;index<2;index++)
            {
                float x=q+index*2.7f,z=-q;float distance=Math.Min(Half-Math.Abs(x),Half-Math.Abs(z));
                float crown=Math.Min(2.1f,(distance-1.3f)/1.6f);if(crown<1.05f||!CropClear(x,z,.55f))continue;
                uint h=LiminalRegions.Hash(unchecked((int)Seed),index,0,0x101100u);
                list.Add(new Forest39Tree(new V3(x,Level10Terrain.MeshHeight(this,x,z)-.02f,z),7.5f+Unit(h)*2,.24f,crown,Unit(h*17)*360,h));
            }
            return list.ToArray();
        }
        Level10Stalk[] Plant()
        {
            var list=new List<Level10Stalk>(MaximumStalks);int n=Math.Min(96,Math.Max(24,(int)(Cell/.63f)));
            // A coprime permutation spreads the bounded budget through every field,
            // including large custom cells; it never fills only the first quadrant.
            int total=n*n,stride=(int)(total*.61803398875);
            while(Gcd(stride,total)!=1)stride++;
            for(int k=0;k<total&&list.Count<Math.Min(MaximumStalks,(int)(Cell*Cell*.55f));k++)
            {
                int i=(int)(((long)k*stride+(Seed%(uint)total))%total),ix=i%n,iz=i/n;
                uint h=LiminalRegions.Hash(unchecked((int)Seed),ix,iz,0x100200u);
                float x=-Half+(ix+.2f+Unit(h)*.6f)*Cell/n,z=-Half+(iz+.2f+Unit(h*1664525u+1013904223u)*.6f)*Cell/n;
                if(!CropClear(x,z))continue;
                list.Add(new Level10Stalk(new V3(x,Level10Terrain.MeshHeight(this,x,z),z),1.05f+Unit(h*17u)*.50f,Unit(h*31u)*360,.06f+Unit(h*47u)*.1f,h));
            }
            return list.ToArray();
        }
        static int Gcd(int a,int b){while(b!=0){int n=a%b;a=b;b=n;}return a;}
        public bool Valid()
        {
            if(Routes.Length<6||Stalks.Length>MaximumStalks||Farm.MinX<TrackHalf+.5f||Farm.MinZ<TrackHalf+.5f||Farm.MaxX>Half-.5f||Farm.MaxZ>Half-.5f)return false;
            if(Math.Abs(Height(0,0))>.0001f)return false;
            foreach(var stalk in Stalks)if(!CropClear(stalk.Foot.X,stalk.Foot.Z)||stalk.Height<1||stalk.Height>1.6f)return false;
            return true;
        }
        public static float Unit(uint v){return (v>>8)*(1f/16777216);}
        public static float Smooth(float v){v=Math.Max(0,Math.Min(1,v));return v*v*(3-2*v);}
    }
    public static class Level10Planner
    {
        public static bool Outskirts(LayoutConfig c,int x,int z)
        {
            if(!c.EnableWheatFields10||c.Procedural!=LiminalGeneration.Expanded)return false;
            BiomeExtent.NearestRegion(c,x,z,out int rx,out int rz);double size=BiomeExtent.RegionSpacing(c);
            double centre=rx*size+(LiminalRegions.Hash(c.Seed,rx,rz,0xE01u)>>8)*(size/16777216.0);
            return x>=centre+Math.Max(1,size*.08);
        }
        public static bool Find(LayoutConfig c,int x,int z,out int foundX,out int foundZ)
        {
            foundX=x;foundZ=z;
            if(c.Procedural==LiminalGeneration.WheatFields10)return Available(c,x,z);
            if(!c.EnableWheatFields10||c.Procedural!=LiminalGeneration.Expanded)return false;
            if(Available(c,x,z))return true;
            bool cityFound=LiminalRegions.Find(c,LiminalEnvironment.City11,x,z,out int cityX,out int cityZ);
            int limit=Math.Min(512,(int)Math.Ceiling(BiomeExtent.RegionSpacing(c)*3)+4);
            for(int radius=1;cityFound&&radius<=limit;radius++)for(int side=0;side<8;side++)
            {
                long xx=(long)cityX+radius*(side<4?Directions.DX[side]:side%2==0?-1:1);
                long zz=(long)cityZ+radius*(side<4?Directions.DZ[side]:side<6?-1:1);
                if(xx<int.MinValue||xx>int.MaxValue||zz<int.MinValue||zz>int.MaxValue||!Available(c,(int)xx,(int)zz))continue;
                foundX=(int)xx;foundZ=(int)zz;return true;
            }
            // In compact worlds an entire first city's rural edge may be
            // reserved for a mall/Level94. Search other logical city centres,
            // rather than returning a missing destination or changing the seed.
            BiomeExtent.NearestRegion(c,x,z,out int regionX,out int regionZ);
            int reach=Math.Min(96,(int)Math.Ceiling(BiomeExtent.RegionSpacing(c))+2);
            for(int radius=0;radius<=24;radius++)for(int offset=-radius;offset<=radius;offset++)for(int side=0;side<4;side++)
            {
                long rx=(long)regionX+(side%2==0?offset:side==1?radius:-radius);
                long rz=(long)regionZ+(side%2==1?offset:side==0?radius:-radius);
                if(rx<int.MinValue||rx>int.MaxValue||rz<int.MinValue||rz>int.MaxValue||!LargeRegions.Center(c,(int)rx,(int)rz,out int cx,out int cz))continue;
                if(LiminalRegions.BaseAt(c,cx,cz)!=LiminalEnvironment.City11)continue;
                for(int dz=-reach;dz<=reach;dz++)for(int dx=0;dx<=reach;dx++)
                {
                    long px=(long)cx+dx,pz=(long)cz+dz;
                    if(px<int.MinValue||px>int.MaxValue||pz<int.MinValue||pz>int.MaxValue||!Available(c,(int)px,(int)pz))continue;
                    foundX=(int)px;foundZ=(int)pz;return true;
                }
            }
            return false;
        }
        public static bool Available(LayoutConfig c,int x,int z)
        {
            if(!c.Continuous&&(x<0||z<0||x>=c.Columns||z>=c.Rows))return false;
            return LiminalRegions.At(c,x,z)==LiminalEnvironment.WheatFields10&&!ConnectedTerrainTopology.Reserved(c,x,z)&&!ConnectedTerrainTopology.Gateway(c,x,z,out _)&&!ConnectedMallTopology.At(c,x,z,out _)&&!ConnectedMallTopology.Gateway(c,x,z,out _);
        }
        public static bool Same(LayoutConfig c,int x,int z,int side)
        {
            long xx=(long)x+Directions.DX[side],zz=(long)z+Directions.DZ[side];
            return Available(c,x,z)&&xx>=int.MinValue&&xx<=int.MaxValue&&zz>=int.MinValue&&zz<=int.MaxValue&&Available(c,(int)xx,(int)zz);
        }
        public static void Describe(Room r,LayoutConfig c)
        {
            var plan=new Level10Plan(c,r.X,r.Z);r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=20;
            r.Plan=new ProceduralPlan(r.Environment,Array.Empty<PlannedModule>(),plan.Lake.Width>0?new[]{plan.Lake}:Array.Empty<WaterRegion>(),1.2f,0,0,level10:plan);
            if(!plan.Valid())throw new InvalidOperationException("Invalid Level10 countryside plan.");
        }
    }
}
