using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class ZoneArchitecture
    {
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            ZoneRecipe recipe=ZoneCatalog.Get(r.ZoneId);
            var o=new RoomGeometry();
            WaterRegion[] pools=DescribeWater(r,c);o.Regions.AddRange(pools);
            if(pools.Length>0)o.Region=pools[0];
            Floor(o,r,c,pools);Shell(o,r,c);Ceiling(o,r,c,recipe);
            new Builder(o,r,c,recipe).Build();
            return o;
        }

        // The same analytic regions drive geometry and swimming; no runtime mesh reconstruction.
        public static WaterRegion[] DescribeWater(Room r,LayoutConfig c)
        {
            ZoneRecipe z=ZoneCatalog.Get(r.ZoneId);
            var list=new List<WaterRegion>();
            float depth=z.Family==ZoneFamily.Reservoirs?1.5f:z.Family==ZoneFamily.ThermalCells?.9f:1.2f;
            switch(z.Pools)
            {
                case PoolPlan.Shallow:
                    list.Add(new WaterRegion{Width=r.Width,Depth=r.Depth,Bottom=-.26f,Level=c.WaterLevel});break;
                case PoolPlan.TwinLong:
                    Add(-5.25f,0,5.1f,15.6f,.6f);Add(5.25f,0,5.1f,15.6f,.6f);break;
                case PoolPlan.FourCourts:
                case PoolPlan.FourRound:
                    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
                        Add(x*5.1f,y*5.1f,5.4f,5.4f,z.Pools==PoolPlan.FourRound?2.7f:.45f);
                    break;
                case PoolPlan.TwinCross:
                    Add(0,-5.25f,15.6f,5.1f,.85f);Add(0,5.25f,15.6f,5.1f,.85f);break;
                case PoolPlan.FourSlits:
                    for(int x=-1;x<=1;x+=2)for(int j=-1;j<=1;j+=2)Add(x*5.4f,j*5.4f,2.5f,7,.35f);
                    break;
                case PoolPlan.CornerRounds:
                    for(int x=-1;x<=1;x+=2)for(int j=-1;j<=1;j+=2)Add(x*6,j*6,6,6,3);
                    break;
                case PoolPlan.TwinOvals:
                    Add(-5.4f,0,6,13,3);Add(5.4f,0,6,13,3);break;
                case PoolPlan.OffsetBaths:
                    for(int j=-1;j<=1;j+=2){Add(-5.3f,j*5.3f,5,5,.7f);Add(5.3f,j*3.8f,5,4.4f,1.2f);}
                    break;
                case PoolPlan.SixBaths:
                    for(int x=-1;x<=1;x+=2)for(int j=-1;j<=1;j++)Add(x*5.25f,j*5.3f,4.8f,4.3f,.7f);
                    break;
            }
            return list.ToArray();
            void Add(float x,float zz,float w,float d,float radius)
            {list.Add(new WaterRegion{CenterX=x,CenterZ=zz,Width=w,Depth=d,Radius=radius,Bottom=-depth,Level=c.WaterLevel});}
        }

        private static void Floor(RoomGeometry o,Room r,LayoutConfig c,WaterRegion[] pools)
        {
            ZoneRecipe z=ZoneCatalog.Get(r.ZoneId);float uv=c.TextureWorldSize;
            if(z.Pools==PoolPlan.Shallow||z.Pools==PoolPlan.Dry)
            {
                ZoneShapes.Box(o.Structure,0,0,r.Width,r.Depth,r.Floor-.3f,r.Floor,Surface.Tile,uv);
                if(pools.Length>0)Shapes.Fill(o.Water,Shapes.RoundedRect(r.Width,r.Depth,0,3),c.WaterLevel,Surface.Tile,uv);
                return;
            }
            // Decompose the deck at the exact basin boundaries: no hidden solid slab across a pool.
            var xs=new List<float>{-r.Width*.5f,r.Width*.5f};var zs=new List<float>{-r.Depth*.5f,r.Depth*.5f};
            foreach(var p in pools){xs.Add(p.CenterX-p.Width*.5f);xs.Add(p.CenterX+p.Width*.5f);zs.Add(p.CenterZ-p.Depth*.5f);zs.Add(p.CenterZ+p.Depth*.5f);}
            SortUnique(xs);SortUnique(zs);
            for(int i=0;i<xs.Count-1;i++)for(int j=0;j<zs.Count-1;j++)
            {
                float x=(xs[i]+xs[i+1])*.5f,zz=(zs[j]+zs[j+1])*.5f;bool hole=false;
                foreach(var p in pools)if(Math.Abs(x-p.CenterX)<p.Width*.5f&&Math.Abs(zz-p.CenterZ)<p.Depth*.5f){hole=true;break;}
                if(!hole)ZoneShapes.Box(o.Structure,x,zz,xs[i+1]-xs[i],zs[j+1]-zs[j],-1.9f,0,Surface.Tile,uv);
            }
            foreach(var p in pools)
            {
                var local=new MeshData();var water=new MeshData();int n=Math.Max(3,c.CurveSegments/4);
                V2[] outer=Shapes.RoundedRect(p.Width,p.Depth,0,n),rim=Shapes.RoundedRect(p.Width,p.Depth,p.Radius,n);
                Shapes.Ring(local,outer,rim,0,Surface.Tile,uv);
                Shapes.Skirt(local,outer,-1.9f,0,Surface.Tile,uv);
                Shapes.Fill(local,outer,-1.9f,Surface.Tile,uv,false);
                int steps=(int)Math.Ceiling(-p.Bottom/.25f);
                float inset=Math.Min(.3f,Math.Min(p.Width,p.Depth)/(2*(steps+2)));
                float previous=0;
                for(int s=1;s<=steps;s++)
                {
                    float y=p.Bottom*s/steps;
                    Shapes.Skirt(local,rim,y,previous,Surface.Accent,uv,true);
                    V2[] next=Shapes.RoundedRect(p.Width-2*s*inset,p.Depth-2*s*inset,Math.Max(0,p.Radius-s*inset),n);
                    Shapes.Ring(local,rim,next,y,Surface.Tile,uv);rim=next;previous=y;
                }
                Shapes.Fill(local,rim,p.Bottom,Surface.Accent,uv);
                Shapes.Fill(water,Shapes.RoundedRect(p.Width,p.Depth,p.Radius,n),c.WaterLevel,Surface.Tile,uv);
                V3 offset=new V3(p.CenterX,0,p.CenterZ);local.TranslateRotate(offset,0);water.TranslateRotate(offset,0);
                o.Structure.Append(local);o.Water.Append(water);
            }
        }
        private static void SortUnique(List<float> list)
        {list.Sort();for(int i=list.Count-1;i>0;i--)if(Math.Abs(list[i]-list[i-1])<.001f)list.RemoveAt(i);}

        private static void Shell(RoomGeometry o,Room r,LayoutConfig c)
        {
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();float width=side%2==0?r.Width:r.Depth;
                if(r.HasExit(side))Shapes.Portal(wall,width,r.Height,r.Floor,c.DoorWidth,c.DoorHeight,c.WallThickness,c.CurveSegments/2,c.TextureWorldSize);
                else ZoneShapes.Box(wall,0,0,width,c.WallThickness,r.Floor,r.Height,Surface.Tile,c.TextureWorldSize);
                wall.TranslateRotate(new V3(Directions.DX[side]*r.Width*.5f,0,Directions.DZ[side]*r.Depth*.5f),side*Shapes.Pi*.5f);
                o.Structure.Append(wall);
            }
        }

        private static void Ceiling(RoomGeometry o,Room r,LayoutConfig c,ZoneRecipe recipe)
        {
            // Nine roof sectors, with real shafts and recessed luminous diffusers in selected sectors.
            // Diffusers represent daylight; realtime illumination comes from the associated fixtures.
            float w=r.Width/3,d=r.Depth/3,h=r.Height,uv=c.TextureWorldSize;
            int mode=(recipe.Variant+(int)recipe.Family)%6;
            for(int ix=-1;ix<=1;ix++)for(int iz=-1;iz<=1;iz++)
            {
                float x=ix*w,z=iz*d;
                bool opening=mode==0?ix==0&&iz==0:mode==1?ix==0:mode==2?ix!=0&&iz!=0:
                    mode==3?iz==0:mode==4?ix==iz:false;
                if(!opening){ZoneShapes.Box(o.Structure,x,z,w,d,h,h+.24f,Surface.Plaster,uv);continue;}
                float aw=mode==3?.9f:w*.43f,ad=mode==1?d*.7f:d*.43f;
                float x0=x-aw*.5f,x1=x+aw*.5f,z0=z-ad*.5f,z1=z+ad*.5f;
                o.Structure.Box(new V3(x-w*.5f,h,z-d*.5f),new V3(x0,h+.24f,z+d*.5f),Surface.Plaster,uv);
                o.Structure.Box(new V3(x1,h,z-d*.5f),new V3(x+w*.5f,h+.24f,z+d*.5f),Surface.Plaster,uv);
                o.Structure.Box(new V3(x0,h,z-d*.5f),new V3(x1,h+.24f,z0),Surface.Plaster,uv);
                o.Structure.Box(new V3(x0,h,z1),new V3(x1,h+.24f,z+d*.5f),Surface.Plaster,uv);
                float shaft=recipe.Family==ZoneFamily.LightWells?1.2f:.45f;
                ZoneShapes.Box(o.Structure,x0-.06f,z,.12f,ad,h+.24f,h+shaft,Surface.Tile,uv);
                ZoneShapes.Box(o.Structure,x1+.06f,z,.12f,ad,h+.24f,h+shaft,Surface.Tile,uv);
                ZoneShapes.Box(o.Structure,x,z0-.06f,aw+.24f,.12f,h+.24f,h+shaft,Surface.Tile,uv);
                ZoneShapes.Box(o.Structure,x,z1+.06f,aw+.24f,.12f,h+.24f,h+shaft,Surface.Tile,uv);
                ZoneShapes.Box(o.Structure,x,z,aw,ad,h+shaft,h+shaft+.06f,Surface.Glow,uv);
                o.Fixtures.Add(new Fixture{Position=new V3(x,h-.1f,z),Width=aw,Depth=ad,Power=4200,Temperature=5400,Skylight=true});
            }
            // Four low fixtures illuminate side chambers even under vaults or roof beams.
            for(int sx=-1;sx<=1;sx+=2)for(int sz=-1;sz<=1;sz+=2)
            {
                float x=sx*4,z=sz*4,y=recipe.Family==ZoneFamily.Vaults?3.4f:h-.65f;
                ZoneShapes.Box(o.Structure,x,z,1.5f,.22f,y+.02f,y+.08f,Surface.Glow,uv);
                o.Fixtures.Add(new Fixture{Position=new V3(x,y,z),Width=1.5f,Depth=.22f,Power=mode==5?3500:1800,
                    Temperature=recipe.Family==ZoneFamily.ThermalCells?4100:4900+recipe.Variant*150});
            }
        }

        private sealed class Builder
        {
            private readonly RoomGeometry o;private readonly Room r;private readonly LayoutConfig c;private readonly ZoneRecipe recipe;
            private readonly MeshData m;private readonly int v,n;private readonly float h,uv,floor;
            internal Builder(RoomGeometry output,Room room,LayoutConfig config,ZoneRecipe z)
            {o=output;r=room;c=config;recipe=z;m=o.Structure;v=z.Variant;n=c.CurveSegments;h=r.Height;uv=c.TextureWorldSize;floor=r.Floor;}
            private void Box(float x,float z,float w,float d,float bottom,float top,Surface s=Surface.Tile)
            {ZoneShapes.Box(m,x,z,w,d,bottom,top,s,uv);}
            private void Column(float x,float z,float radius,float top,int segments=0)
            {Shapes.Column(m,x,z,radius,-1.7f,top,segments==0?n:segments,Surface.Tile,uv);}
            private void Bench(float x,float z,float w,float d)
            {Box(x,z,w,d,floor,.45f);Box(x,z,w+.06f,d+.06f,.45f,.51f,Surface.Accent);}
            private void Portal(float x,float z,float yaw,float width,float opening,float aperture,int kind,float thickness=.4f)
            {ZoneShapes.Portal(m,x,z,yaw,width,h,floor,opening,Math.Min(aperture,h-.25f),thickness,kind,n,uv);}
            private void Pipe(V3 a,V3 b,float radius=.06f)
            {ZoneShapes.Beam(m,a,b,radius,Surface.Metal,uv,12);}
            internal void Build()
            {
                switch(recipe.Family)
                {
                    case ZoneFamily.Enfilades:Enfilade();break;
                    case ZoneFamily.Hypostyles:Hypostyle();break;
                    case ZoneFamily.Meanders:Meander();break;
                    case ZoneFamily.Rotundas:Rotunda();break;
                    case ZoneFamily.Natatoriums:Natatorium();break;
                    case ZoneFamily.ThermalCells:Thermal();break;
                    case ZoneFamily.Cloisters:Cloister();break;
                    case ZoneFamily.Terraces:Terraces();break;
                    case ZoneFamily.Bridges:Bridges();break;
                    case ZoneFamily.LightWells:LightWells();break;
                    case ZoneFamily.Vaults:Vaults();break;
                    case ZoneFamily.Niches:Niches();break;
                    case ZoneFamily.ChangingRooms:Changing();break;
                    case ZoneFamily.Showers:Showers();break;
                    case ZoneFamily.Waterparks:Waterpark();break;
                    case ZoneFamily.ServiceGalleries:Service();break;
                    case ZoneFamily.RampHalls:Ramps();break;
                    case ZoneFamily.SplitGalleries:Split();break;
                    case ZoneFamily.IslandBaths:Islands();break;
                    case ZoneFamily.Reservoirs:Reservoir();break;
                    default:ZoneExpansion.Build(o,r,c,recipe);break;
                }
                // Drain channels stay flush and clear of the four main door approaches.
                for(int side=-1;side<=1;side+=2)for(int k=0;k<8;k++)
                    Box(side*(r.Width*.5f-.55f),3+k*.5f,.28f,.08f,floor+.001f,floor+.014f,Surface.Metal);
            }
            private void Enfilade()
            {
                int count=v==1?6:v==2?2:4;
                for(int i=0;i<count;i++)
                {
                    int half=count/2;float z=(i<half?-1:1)*(2.4f+(i%half)*(half>1?4.5f/(half-1):0));
                    bool rotate=v==3;
                    Portal(rotate?z:0,rotate?0:z,rotate?Shapes.Pi*.5f:0,16,5.6f+(v==2?1:0),5.7f,v==5?3:v%3);
                }
            }
            private void Hypostyle()
            {
                int count=v==4?4:3;
                for(int ix=0;ix<count;ix++)for(int iz=0;iz<count;iz++)
                {
                    float x=-6.8f+13.6f*ix/(count-1),z=-6.8f+13.6f*iz/(count-1);
                    if(Math.Abs(x)<1.5f||Math.Abs(z)<1.5f)continue;
                    float radius=v==3?1.1f:v==4?.42f:.7f;
                    if(v==2){Column(x-.6f,z,.32f,h);Column(x+.6f,z,.32f,h);}
                    else Column(x,z,radius,h,v==1?8:0);
                    if(v==5)Box(x,z,2,2,h-.6f,h,Surface.Accent);
                }
            }
            private void Meander()
            {
                int paths=v==3||v==5?4:2;
                for(int i=0;i<paths;i++)
                {
                    float baseX=paths==2?(i==0?-4.7f:4.7f):-7.5f+i*5;
                    // Split at the east-west passage; each section remains a real curved wall.
                    for(int side=-1;side<=1;side+=2)
                    {
                        var p=new V2[n+1];
                        for(int j=0;j<=n;j++)
                        {
                            float z=side*(2.1f+j/(float)n*(4.9f+v*.12f));
                            float amp=paths==4?.5f:.8f+v*.1f;
                            p[j]=new V2(baseX+Shapes.Sin(z*(.3f+v*.08f)+i)*amp,z);
                        }
                        Shapes.Partition(m,p,.32f,-1.7f,h,Surface.Tile,uv);
                    }
                }
            }
            private void Rotunda()
            {
                int rings=v==1||v==5?2:1;
                for(int ring=0;ring<rings;ring++)for(int quadrant=0;quadrant<4;quadrant++)
                {
                    float radius=rings==2?4.6f+ring*3:6.2f+v*.15f;
                    int segments=v==4?2:Math.Max(8,n/2);var path=new V2[segments+1];
                    float start=quadrant*90+24,span=42;
                    for(int j=0;j<=segments;j++){float a=(start+j*span/segments)*Shapes.Pi/180;path[j]=new V2(Shapes.Cos(a)*radius,Shapes.Sin(a)*radius);}
                    Shapes.Partition(m,path,v==2?.85f:.45f,-1.7f,h,Surface.Tile,uv);
                    if(v==3){float a=(quadrant*90+18)*Shapes.Pi/180;Column(Shapes.Cos(a)*7.7f,Shapes.Sin(a)*7.7f,.4f,h);}
                }
            }
            private void Natatorium()
            {
                foreach(var p in o.Regions)
                {
                    bool longZ=p.Depth>p.Width;
                    int lanes=v==4?3:2;
                    for(int i=0;i<lanes;i++)
                    {
                        float offset=(i-(lanes-1)*.5f)*.7f;
                        Box(p.CenterX+(longZ?offset:0),p.CenterZ+(longZ?0:offset),longZ?.08f:p.Width*.5f,
                            longZ?p.Depth*.5f:.08f,p.Bottom+.002f,p.Bottom+.012f,Surface.Metal);
                    }
                }
                if(v==1||v==2||v==5)SideSteps(4+v%2,.22f);
                else for(int side=-1;side<=1;side+=2)
                {
                    Bench(side*8.7f,4,1,4+v*.3f);
                    Box(side*5.2f,-8.5f,1.1f,1,floor,.55f,Surface.Accent);
                }
            }
            private void Thermal()
            {
                foreach(var p in o.Regions)
                {
                    float sign=p.CenterX<0?-1:1,x=p.CenterX;
                    float front=p.CenterZ-p.Depth*.5f-.22f,back=p.CenterZ+p.Depth*.5f+.22f;
                    // The front portal and inner side doorway both connect to the deck network.
                    Portal(x,front,0,p.Width+.5f,2.5f,4.2f,v%4,.25f);
                    Box(x,back,p.Width+.5f,.25f,-1.7f,h);
                    Box(x+sign*(p.Width*.5f+.15f),p.CenterZ,.25f,p.Depth,-1.7f,h);
                    Bench(x+sign*(p.Width*.5f-.45f),p.CenterZ,.7f,Math.Min(2.1f,p.Depth*.5f));
                }
            }
            private void Cloister()
            {
                int count=v==3?4:3;
                for(int axis=0;axis<2;axis++)for(int side=-1;side<=1;side+=2)for(int j=0;j<count;j++)
                {
                    float along=-7+14f*j/(count-1);if(Math.Abs(along)<1.7f)continue;
                    float x=axis==0?side*8.4f:along,z=axis==0?along:side*8.4f;
                    Column(x,z,v==0?.55f:.32f,h);
                    if(v==5)Bench(x,z,axis==0?1:2,axis==0?2:1);
                }
                for(int side=-1;side<=1;side+=2)
                {
                    Box(side*8.4f,0,.7f,17,h-.5f,h,Surface.Accent);Box(0,side*8.4f,17,.7f,h-.5f,h,Surface.Accent);
                    if(v==1||v==4)Portal(0,side*7,0,15,5.5f,v==4?4.4f:5.2f,v==4?1:0,.3f);
                }
            }
            private void SideSteps(int count,float rise)
            {
                for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)for(int i=0;i<count;i++)
                {
                    float x=side*(6.8f+i*.48f),z=end*5.2f;
                    Box(x,z,.48f,4.6f,-1.7f,(i+1)*rise,Surface.Tile);
                }
            }
            private void Terraces()
            {
                SideSteps(3+v%3,.18f+(v/3)*.03f);
                if(v==2||v==5)for(int s=-1;s<=1;s+=2)for(int j=0;j<4;j++)
                    Box(s*4.7f,7+j*.45f,2,.45f,-1.7f,(j+1)*.2f);
            }
            private void Bridges()
            {
                foreach(var p in o.Regions)
                {
                    bool alongX=p.Width<p.Depth;float length=(alongX?p.Width:p.Depth)+.8f;
                    float x=p.CenterX,z=p.CenterZ;
                    Box(x,z,alongX?length:1.6f,alongX?1.6f:length,-.2f,.12f,Surface.Accent);
                    if(v==2||v==4)
                    {
                        for(int side=-1;side<=1;side+=2)Column(x+(alongX?side*1.1f:0),z+(alongX?0:side*1.1f),.2f,-.2f);
                    }
                    if(v==3)
                    {
                        // Side rails, never across the landing.
                        for(int side=-1;side<=1;side+=2)
                        {
                            V3 a=new V3(x+(alongX?-length*.5f:side*.86f),1.1f,z+(alongX?side*.86f:-length*.5f));
                            V3 b=new V3(x+(alongX?length*.5f:side*.86f),1.1f,z+(alongX?side*.86f:length*.5f));
                            Pipe(a,b,.05f);Pipe(a-V3.Up,b-V3.Up,.035f);
                            Pipe(a-V3.Up,a,.035f);Pipe(b-V3.Up,b,.035f);
                        }
                    }
                    if(v==5)Portal(x,z,alongX?Shapes.Pi*.5f:0,length,Math.Min(length-1,4),4.8f,1,.35f);
                }
            }
            private void LightWells()
            {
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                {
                    float px=x*(5.4f+v*.15f),pz=z*5.4f;
                    if(v==3)Column(px,pz,.5f,h);
                    else
                    {
                        Box(px,pz,1+v*.12f,.65f,-1.7f,h);
                        Box(px,pz,3.2f,.6f,h-.5f,h);
                        Box(px,pz,.6f,3.2f,h-.5f,h);
                    }
                }
                if(v==1||v==4)for(int j=-1;j<=1;j++)Box(j*5,0,.5f,18,h-.7f,h);
            }
            private void Vaults()
            {
                bool across=v==1||v==5;int count=v==3?4:2;
                for(int i=0;i<count;i++)
                {
                    float x=count==2?(i==0?-5:5):(i%2==0?-5:5),z=count==2?0:(i<2?-5:5);
                    if(across){float swap=x;x=z;z=swap;}
                    float length=count==2?15:5.4f,radius=2.2f,spring=h-radius-.45f;
                    ZoneShapes.Vault(m,x,z,radius,length,spring,across?Shapes.Pi*.5f:0,n,uv);
                    for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)
                    {
                        float px=x+(across?b*(length*.5f-.2f):a*(radius+.1f));
                        float pz=z+(across?a*(radius+.1f):b*(length*.5f-.2f));
                        Column(px,pz,.28f,spring);
                    }
                    if(v==2)for(int j=-1;j<=1;j++)ZoneShapes.Vault(m,x,z+j*4,radius-.12f,.2f,spring,0,n,uv);
                    if(v==4)for(int s=-1;s<=1;s+=2)Column(x+s*2.2f,0,.4f,spring);
                }
            }
            private void Niches()
            {
                int count=2+v%3;
                for(int side=-1;side<=1;side+=2)for(int half=-1;half<=1;half+=2)for(int i=0;i<count;i++)
                {
                    float z=half*(3+i*5.4f/count),x=side*8.6f,d=5.2f/count;
                    Box(x,z-d*.5f,2,.2f,floor,h*.72f);Box(x,z+d*.5f,2,.2f,floor,h*.72f);
                    Box(x,z,2,d,h*.72f,h*.72f+.2f);Bench(x+side*.3f,z,.8f,d-.3f);
                    if(v==1||v==4)ZoneShapes.Portal(m,x-side*1.1f,z,Shapes.Pi*.5f,d,h*.72f,floor,d-.35f,3.3f,.2f,1,n,uv);
                }
            }
            private void Changing()
            {
                int count=4+v;
                for(int side=-1;side<=1;side+=2)for(int half=-1;half<=1;half+=2)
                {
                    for(int i=0;i<count;i++)
                    {
                        float z=half*(2.5f+i*5.4f/count),x=side*(v==3?6.3f:9);
                        Box(x,z,.72f,.5f,0,2.05f,Surface.Accent);
                        Box(x-side*.37f,z,.035f,.44f,.1f,1.98f);
                        Box(x-side*.4f,z+.14f,.035f,.035f,.95f,1.15f,Surface.Metal);
                    }
                    Bench(side*6.9f,half*5.3f,.8f,4.7f);
                    if(v==1||v==2||v==4)
                        for(int j=0;j<3;j++)Box(side*4.5f,half*(3+j*1.9f),2.2f,.12f,0,2.4f);
                    if(v==5)Box(side*6,half*5,5.5f,.6f,h-.65f,h);
                }
            }
            private void Showers()
            {
                int count=3+v%3;
                for(int side=-1;side<=1;side+=2)for(int half=-1;half<=1;half+=2)for(int i=0;i<count;i++)
                {
                    float x=side*8.7f,z=half*(2.7f+i*5.1f/count);
                    Box(x,z,1.8f,1.3f,floor,floor+.05f,Surface.Accent);
                    Box(x,z,.2f,.2f,floor+.051f,floor+.057f,Surface.Metal);
                    Pipe(new V3(x+side*.6f,.2f,z),new V3(x+side*.6f,2.4f,z));
                    Pipe(new V3(x+side*.6f,2.4f,z),new V3(x,2.4f,z));
                    Box(x,z,.23f,.23f,2.35f,2.42f,Surface.Metal);
                    if(v==1||v==2||v==3)Box(x,z-.7f,1.9f,.12f,floor,2.3f);
                }
                if(v>=4)for(int side=-1;side<=1;side+=2)Bench(side*5.8f,4,.75f,5);
            }
            private void Waterpark()
            {
                int count=v==2?1:2;
                for(int i=0;i<count;i++)
                {
                    int side=i==0?-1:1;float x=side*5.25f,top=2.4f+v*.16f;
                    V3 a=new V3(x,top,5.3f),b=new V3(x+side*(.3f+v*.12f),top,2.7f);
                    V3 cc=new V3(x-side*.6f,.45f,-2.2f),d=new V3(x,.15f,-5.2f);
                    Shapes.Slide(m,a,b,cc,d,.72f,n*2);
                    for(int j=1;j<=3;j++){V3 p=Shapes.Bezier(a,b,cc,d,j/4f);Column(p.X,p.Z,.13f,p.Y-.12f,12);}
                    Box(side*6.7f,6.15f,4.5f,1.7f,top-.22f,top,Surface.Accent);
                    Column(side*8.5f,6.2f,.18f,top-.22f,12);
                    int steps=(int)Math.Ceiling(top/.2f);float tread=.32f;
                    for(int j=0;j<steps;j++)Box(side*8.6f,5.3f-(steps-j-.5f)*tread,1.25f,tread,0,top*(j+1)/steps);
                    // Handrails flank the access flight and the outer edge of the landing.
                    Pipe(new V3(side*9.25f,top+1,5.3f),new V3(side*9.25f,top+1,7));
                    if(v==4)Bench(side*8.2f,-6.5f,1,2);
                }
                if(v==5)for(int z=-1;z<=1;z+=2)Box(0,z*7,18,.7f,h-1,h);
            }
            private void Service()
            {
                int count=2+v%3;
                for(int side=-1;side<=1;side+=2)
                {
                    for(int i=0;i<count;i++)
                    {
                        float y=3+i*.4f,x=side*(8.8f-i*.15f);
                        Pipe(new V3(x,y,-8),new V3(x,y,8),.11f+i*.035f);
                        Pipe(new V3(x,y,-8),new V3(x,1,-8),.11f+i*.035f);
                    }
                    for(int z=-1;z<=1;z+=2)
                    {
                        Box(side*8.2f,z*5,1.7f,2.2f,0,.3f);
                        ZoneShapes.Beam(m,new V3(side*8.2f,.9f,z*5-.7f),new V3(side*8.2f,.9f,z*5+.7f),.5f,Surface.Metal,uv,16);
                        if(v==3)Column(side*6.8f,z*6.8f,.55f,h);
                    }
                }
            }
            private void Ramps()
            {
                int count=v==1||v==4?4:2;
                for(int i=0;i<count;i++)
                {
                    float x=i%2==0?-8.1f:8.1f,z=count==4?(i<2?-5:5):4.5f;
                    float length=count==4?3.2f:4.4f,rise=.65f+v*.1f;
                    ZoneShapes.Ramp(m,x,z,1.8f,length,rise,0,uv);
                    Box(x,z+length*.5f+.55f,1.8f,1.1f,0,rise);
                    for(int s=-1;s<=1;s+=2)
                        Pipe(new V3(x+s*.85f,1,z-length*.5f),new V3(x+s*.85f,rise+1,z+length*.5f+.8f),.04f);
                    if(v==3)ZoneShapes.Portal(m,x,z,0,2.6f,h,0,2.1f,4,.25f,1,n,uv);
                    if(v==5)Box(x+Math.Sign(x)*.9f,z+length*.5f+.55f,.15f,1.1f,rise,rise+.9f);
                }
            }
            private void Split()
            {
                int count=v==3?4:2;
                for(int i=0;i<count;i++)
                {
                    float offset=count==2?(i==0?-4.4f:4.4f):-6.9f+i*4.6f;
                    bool across=v==1||v==4;
                    Portal(across?0:offset,across?offset:0,across?0:Shapes.Pi*.5f,17,4.2f,4.8f,v==2?2:3,.32f);
                    if(v==5)for(int side=-1;side<=1;side+=2)Column(offset,side*6,.65f,h);
                }
            }
            private void Islands()
            {
                foreach(var p in o.Regions)
                {
                    float x=p.CenterX,z=p.CenterZ;
                    if(v%2==0)Box(x,z,1.65f,1.65f,p.Bottom,.12f);
                    else Shapes.Column(m,x,z,.92f,p.Bottom,.12f,n,Surface.Accent,uv);
                    // Low bridge reaches the surrounding deck and remains below stepOffset.
                    Box(x,z-p.Depth*.25f,1.1f,p.Depth*.5f+.4f,-.16f,.12f,Surface.Accent);
                    if(v==2)Bench(x,z,1.2f,.5f);
                    if(v==3||v==5)
                    {
                        for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)Column(x+a*.7f,z+b*.7f,.12f,3.2f,12);
                        Box(x,z,2.1f,2.1f,3.2f,3.4f);
                    }
                    if(v==4)for(int s=0;s<3;s++)Box(x+.8f+s*.25f,z,.25f,1.1f,p.Bottom,.12f-(s+1)*.22f);
                }
            }
            private void Reservoir()
            {
                int count=2+v%3;
                for(int side=-1;side<=1;side+=2)for(int half=-1;half<=1;half+=2)for(int i=0;i<count;i++)
                {
                    float z=half*(3+i*5.2f/count),x=side*8.5f;
                    Box(x,z,1.3f,.75f,-1.7f,h);
                    if(v==2)Column(side*5.3f,z,.6f,h);
                }
                for(int j=0;j<3+v%2;j++)Box(0,-7+14f*j/(2+v%2),18,.7f,h-(v==3?1.2f:.65f),h);
                if(v==4)for(int side=-1;side<=1;side+=2)Portal(0,side*5,0,17,5,5.7f,1,.5f);
            }
        }
    }
}
