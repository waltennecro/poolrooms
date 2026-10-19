using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum TerrainDaylight { Clear, SoftClouds, WarmAfternoon }
    public sealed class TerrainHousePlan
    {
        public int CellX,CellZ,Variant,Floors,Seed;
        public float X,Z,Base,Width,Depth;
        public int ArchitectureId=-1; // -1 retains hand-authored / legacy rectangular homes.
        private TerrainHouseBlueprint generatedBlueprint;
        public TerrainHouseBlueprint Blueprint {get{return ArchitectureId<0?null:generatedBlueprint??TerrainHouseBlueprint.Get(ArchitectureId);}}
        public float EntranceX {get{return X+(Blueprint==null?0:Blueprint.StairX);}}
        public const float FloorHeight=3.2f;
        public void SetArchitecture(int id)
        {var b=TerrainHouseBlueprint.Get(id);generatedBlueprint=null;ArchitectureId=id;Width=b.Width;Depth=b.Depth;Floors=b.Floors;}
        public void GenerateArchitecture(bool expanded=false)
        {var b=TerrainHouseBlueprint.Generate(Seed,CellX,CellZ,expanded);generatedBlueprint=b;ArchitectureId=b.Id;Width=b.Width;Depth=b.Depth;Floors=b.Floors;}
        public bool Contains(float x,float y,float z)
        {return Blueprint!=null?Blueprint.Contains(x-X,y-Base,z-Z):Math.Abs(x-X)<Width*.5f-.2f&&Math.Abs(z-Z)<Depth*.5f-.2f&&y>=Base-.2f&&y<Base+Floors*FloorHeight;}
    }
    public readonly struct TerrainPath
    {
        public readonly V2 A,B;
        public TerrainPath(V2 a,V2 b){A=a;B=b;}
        public float Distance(float x,float z)
        {
            float dx=B.X-A.X,dz=B.Y-A.Y,l=dx*dx+dz*dz;
            float t=l>0?CameraPresentation.Unit(((x-A.X)*dx+(z-A.Y)*dz)/l):0;
            float ax=x-A.X-t*dx,az=z-A.Y-t*dz;return (float)Math.Sqrt(ax*ax+az*az);
        }
    }
    // An immutable district description, shared by pure worker geometry and main-thread queries.
    // Terrain continues beyond the nominal district; populated lots occupy its seed-sized envelope.
    public sealed class TerrainRegionPlan
    {
        public const float TileSize=64;
        public const float HalfTile=TileSize*.5f;
        public readonly int WorldSeed,Seed,AddressX,AddressZ;
        public readonly float Width,Depth,SunYaw,SunPitch,HalfWidth,HalfDepth;
        public readonly bool Connected;
        public readonly TerrainDaylight Daylight;
        public readonly V3 SunDirection;
        private readonly Dictionary<long,TerrainHousePlan> houses=new Dictionary<long,TerrainHousePlan>();
        private readonly Dictionary<long,List<TerrainPath>> paths=new Dictionary<long,List<TerrainPath>>();
        private readonly float phase;
        public int HouseCount { get { return houses.Count; } }
        public IEnumerable<TerrainHousePlan> Houses { get { return houses.Values; } }
        public static long Key(int x,int z){return ContinuousTopology.CellKey(x,z);}
        public static int Cell(float value){return (int)Math.Floor((value+TileSize*.5f)/TileSize);}
        private TerrainRegionPlan(int seed,int x,int z,float halfWidth=0,float halfDepth=0,bool variedHouses=true,bool expanded=false,bool larger=false)
        {
            WorldSeed=seed;AddressX=x;AddressZ=z;Seed=unchecked((int)StableRandom.Hash(seed,x,z,0x940024u));
            var random=new StableRandom(unchecked((uint)Seed));
            Width=(12+random.Range(9))*128;Depth=(12+random.Range(9))*128;
            Connected=halfWidth>0&&halfDepth>0;
            if(larger)
            {
                // Fill the enlarged reservation with populated lots, instead of a bare outer strip.
                double linear=Math.Sqrt(BiomeExtent.AreaMultiplier);
                Width=Connected?halfWidth*2:(float)(Width*linear);Depth=Connected?halfDepth*2:(float)(Depth*linear);
            }
            HalfWidth=Connected?halfWidth:Width*.5f;HalfDepth=Connected?halfDepth:Depth*.5f;
            phase=random.Range(0,6.28f);Daylight=(TerrainDaylight)random.Range(3);SunYaw=random.Range(25,150);SunPitch=random.Range(32,56);
            float pitch=SunPitch*(float)Math.PI/180,yaw=SunYaw*(float)Math.PI/180;
            SunDirection=new V3(-(float)Math.Sin(yaw)*(float)Math.Cos(pitch),(float)Math.Sin(pitch),-(float)Math.Cos(yaw)*(float)Math.Cos(pitch));
            int nx=(int)Math.Floor(Width/TileSize*.5f-.5f),nz=(int)Math.Floor(Depth/TileSize*.5f-.5f);
            for(int cz=-nz;cz<=nz;cz++)for(int cx=-nx;cx<=nx;cx++)
            {
                var r=new StableRandom(StableRandom.Hash(Seed,cx,cz,0x9400Au));
                if((cx!=0||cz!=0)&&r.Range(0f,1f)>.96f)continue;
                // Groups of four homes: narrower shared yards, larger breaks between clusters.
                // Keep one indexed lot per 64 m terrain cell so streaming, basement cuts and
                // collision queries continue to find exactly the same owning house.
                float px=cx*TileSize+((cx&1)==0?11:-11)+r.Range(-1.5f,1.5f);
                float pz=cz*TileSize+((cz&1)==0?11:-11)+r.Range(-1.5f,1.5f);
                if(cx==0&&cz==0){px=24;pz=24;}
                if(RoadDistance(px,pz)<19||Connected&&BoundaryDistance(px,pz)<192)continue;
                var h=new TerrainHousePlan{CellX=cx,CellZ=cz,X=px,Z=pz,Width=10+r.Range(3),Depth=12+r.Range(3),
                    Floors=r.Range(0f,1f)<.7f?2:1,Variant=r.Range(6),Seed=unchecked((int)StableRandom.Hash(Seed,cx,cz,0x9400Bu))};
                if(variedHouses)h.GenerateArchitecture(expanded);
                if(variedHouses&&cx==0&&cz==0){h.X=30;h.Z=30;}
                TerrainHousePlan first;
                if(variedHouses&&(cx!=0||cz!=0)&&houses.TryGetValue(Key(0,0),out first))
                {
                    float marginX=(h.Width+first.Width)*.5f+6,marginZ=(h.Depth+first.Depth)*.5f+6;
                    if(Math.Abs(h.X-first.X)<marginX&&Math.Abs(h.Z-first.Z)<marginZ)
                    {
                        // Wider entrance homes need their portal clearing and six metres between neighbouring lots.
                        if(cx>0&&(cz<=0||marginX-Math.Abs(h.X-first.X)<=marginZ-Math.Abs(h.Z-first.Z)))h.X=first.X+marginX;
                        else if(cz>0)h.Z=first.Z+marginZ;
                    }
                }
                h.Base=BaseHeight(h.X,h.Z)+.18f;houses.Add(Key(cx,cz),h);
            }
            foreach(var h in houses.Values)
            {
                float front=h.Z-h.Depth*.5f-1;
                float entry=h.EntranceX,rx=RoadX(entry,front),rz=RoadZ(entry,front);
                V2 end=Math.Abs(rx-entry)<Math.Abs(rz-front)?new V2(rx,front):new V2(entry,rz);
                var line=new TerrainPath(new V2(entry,front),end);
                int ax=Cell(Math.Min(line.A.X,line.B.X)-2),bx=Cell(Math.Max(line.A.X,line.B.X)+2);
                int az=Cell(Math.Min(line.A.Y,line.B.Y)-2),bz=Cell(Math.Max(line.A.Y,line.B.Y)+2);
                for(int iz=az;iz<=bz;iz++)for(int ix=ax;ix<=bx;ix++)
                {long key=Key(ix,iz);List<TerrainPath> list;if(!paths.TryGetValue(key,out list)){list=new List<TerrainPath>();paths.Add(key,list);}list.Add(line);}
            }
        }
        public static TerrainRegionPlan Create(int seed,int x,int z,bool variedHouses=true,bool expanded=false,bool larger=false){return new TerrainRegionPlan(seed,x,z,0,0,variedHouses,expanded,larger);}
        public static TerrainRegionPlan CreateConnected(int seed,ConnectedTerrainSite site,bool expanded=false,bool larger=false){return new TerrainRegionPlan(seed,site.X,site.Z,site.HalfWidth,site.HalfDepth,true,expanded,larger);}
        public bool Contains(float x,float z){return Math.Abs(x)<=HalfWidth&&Math.Abs(z)<=HalfDepth;}
        public bool IntersectsTile(int x,int z){return !Connected||Math.Abs(x*TileSize)-HalfTile<HalfWidth&&Math.Abs(z*TileSize)-HalfTile<HalfDepth;}
        public float BoundaryDistance(float x,float z){return Math.Min(HalfWidth-Math.Abs(x),HalfDepth-Math.Abs(z));}
        public float BaseHeight(float x,float z){return RawHeight(x,z)*(Connected?Smooth(BoundaryDistance(x,z),16,384):1);}
        public string ItemKey(TerrainHousePlan house,int slot)
        {return "terrain94:"+WorldSeed+":"+AddressX+":"+AddressZ+":"+house.CellX+":"+house.CellZ+":"+slot;}
        public TerrainHousePlan House(int x,int z){TerrainHousePlan h;return houses.TryGetValue(Key(x,z),out h)?h:null;}
        public TerrainHousePlan HouseAt(float x,float y,float z)
        {
            int cx=Cell(x),cz=Cell(z);
            // Wider homes can straddle a streaming seam, including the guaranteed entrance lot.
            for(int iz=cz-1;iz<=cz+1;iz++)for(int ix=cx-1;ix<=cx+1;ix++)
            {var h=House(ix,iz);if(h!=null&&h.Contains(x,y,z))return h;}
            return null;
        }
        public float RoadX(float x,float z)
        {return (float)Math.Round(x/384)*384+32*((float)Math.Sin(z/230+phase)-(float)Math.Sin(phase));}
        public float RoadZ(float x,float z)
        {return (float)Math.Round(z/384)*384+25*((float)Math.Sin(x/270+phase*.7f)-(float)Math.Sin(phase*.7f));}
        public float RoadDistance(float x,float z){return Math.Min(Math.Abs(x-RoadX(x,z)),Math.Abs(z-RoadZ(x,z)));}
        public IEnumerable<TerrainPath> Paths(int x,int z)
        {List<TerrainPath> lines;return paths.TryGetValue(Key(x,z),out lines)?lines:Array.Empty<TerrainPath>();}
        public float PathDistance(float x,float z,bool neighbours=false)
        {
            float d=RoadDistance(x,z);int cx=Cell(x),cz=Cell(z),radius=neighbours?1:0;List<TerrainPath> lines;
            for(int iz=cz-radius;iz<=cz+radius;iz++)for(int ix=cx-radius;ix<=cx+radius;ix++)
                if(paths.TryGetValue(Key(ix,iz),out lines))foreach(var p in lines)d=Math.Min(d,p.Distance(x,z)+1.3f);
            return d;
        }
        public float RawHeight(float x,float z)
        {return 32+26*Noise(x/360,z/360,0)+12*Noise(x/160,z/160,1)+3*Noise(x/68,z/68,2);}
        public float Height(float x,float z)
        {
            float y=BaseHeight(x,z);
            // Flatten the little doorway clearing, with terrain present on every side.
            float clearing=Smooth((float)Math.Sqrt(x*x+z*z),10,21);
            y=Lerp(BaseHeight(0,0),y,clearing);
            int cx=Cell(x),cz=Cell(z);
            // A clearing may cross a tile edge: use the same neighbouring lots on both sides.
            for(int iz=cz-1;iz<=cz+1;iz++)for(int ix=cx-1;ix<=cx+1;ix++)
            {var house=House(ix,iz);if(house==null)continue;
                float dx=Math.Max(0,Math.Abs(x-house.X)-house.Width*.5f-2),dz=Math.Max(0,Math.Abs(z-house.Z)-house.Depth*.5f-2);
                float distance=(float)Math.Sqrt(dx*dx+dz*dz);
                y=Lerp(house.Base-.18f,y,Smooth(distance,0,9));
            }
            return y;
        }
        public V3 Normal(float x,float z)
        {const float e=.35f;return new V3(Height(x-e,z)-Height(x+e,z),2*e,Height(x,z-e)-Height(x,z+e)).Unit;}
        public float Noise(float x,float z,int salt)
        {
            int ix=(int)Math.Floor(x),iz=(int)Math.Floor(z);float u=x-ix,v=z-iz;u=u*u*(3-2*u);v=v*v*(3-2*v);
            return Lerp(Lerp(Value(ix,iz,salt),Value(ix+1,iz,salt),u),Lerp(Value(ix,iz+1,salt),Value(ix+1,iz+1,salt),u),v);
        }
        private float Value(int x,int z,int salt){return (StableRandom.Hash(Seed,x,z,unchecked((uint)(0x94A000+salt)))>>8)*(1f/16777216f);}
        public static float Smooth(float x,float a,float b){float t=CameraPresentation.Unit((x-a)/(b-a));return t*t*(3-2*t);}
        private static float Lerp(float a,float b,float t){return a+(b-a)*t;}
        public V3 Sky(V3 direction){return TerrainAppearance.Sky(this,direction);}

    }
    public static class TerrainPortalCrossing
    {
        public static bool Crossed(V3 previous,V3 current,bool enterFromNegative,float width=2.6f,float height=3.5f)
        {
            bool crossing=enterFromNegative?previous.Z<0&&current.Z>=0:previous.Z>0&&current.Z<=0;
            if(!crossing||(current-previous).Length>12)return false;
            float t=-previous.Z/(current.Z-previous.Z);V3 p=previous+(current-previous)*t;
            return Math.Abs(p.X)<width*.5f-.08f&&p.Y>.1f&&p.Y<height-.1f;
        }
    }
}
