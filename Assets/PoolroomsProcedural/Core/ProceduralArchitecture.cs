using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Procedural grammar: protected circulation + independently dimensioned architectural bays.
    // New environments implement their modules here; topology, streaming and water stay shared.
    public static class ProceduralArchitecture
    {
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            if(room.Plan!=null&&room.Plan.Rain!=null)return RainHouseArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Aquatic!=null)return AquaticParkArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Baths!=null)return ThermalBathArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Garden!=null)return OrangerieArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Quiet!=null)return QuietLevelArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.City!=null)return City11Architecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.SoftPlay!=null)return SoftPlayArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Solar!=null)return SolarArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Interior!=null)return InteriorArchitecture.Build(room,c);
            if(room.Plan!=null&&room.Plan.Store!=null)return RetailArchitecture.Build(room,c);
            var o=new RoomGeometry();ProceduralPlan plan=room.Plan;
            if(plan==null||!plan.Valid(room,c))throw new ArgumentException("Invalid procedural room plan.");
            o.Structure.PlayPalette=room.Environment==LiminalEnvironment.IndoorPlay?1:0;
            o.Decoration.PlayPalette=o.Structure.PlayPalette;o.Structure.PaletteVariant=plan.Palette;o.Decoration.PaletteVariant=plan.Palette;
            o.Regions.AddRange(plan.Pools);if(plan.Pools.Length>0)o.Region=plan.Pools[0];
            Floor(o,room,c);IndependentShell(o,room,c);Ceiling(o,room,c);
            int pits=0;foreach(PlannedModule module in plan.Modules)if(module.Kind==LiminalModule.BallPit)pits++;
            int perPit=(c.CurveSegments<=12?48:c.CurveSegments<=32?96:160)/Math.Max(1,pits);
            foreach(PlannedModule module in plan.Modules)Module(o,room,module,c,perPit);
            // Rounded boundaries can coincide after float translations. Remove those zero-area
            // faces before collider cooking; leave legacy geometry/fingerprints untouched.
            Prune(o.Structure);Prune(o.Water);Prune(o.Decoration);
            return o;
        }
        internal static void DryEnvelope(RoomGeometry o,Room r,LayoutConfig c){Floor(o,r,c);IndependentShell(o,r,c);}
        private static void IndependentShell(RoomGeometry o,Room r,LayoutConfig c)
        {
            if(!c.IndependentBiomeMaterials||r.Environment==LiminalEnvironment.Poolrooms){Shell(o,r,c);return;}
            var envelope=new RoomGeometry();Shell(envelope,r,c);ReassignWallSurfaces(envelope.Structure);o.Structure.Append(envelope.Structure);
        }
        private static void ReassignWallSurfaces(MeshData m)
        {
            var old=m.Indices[(int)Surface.Tile];var floor=new System.Collections.Generic.List<int>();
            for(int i=0;i<old.Count;i+=3)
            {int v=old[i];var destination=Math.Abs(m.Normals[v].Y)<.5f?m.Indices[(int)Surface.Plaster]:floor;destination.Add(old[i]);destination.Add(old[i+1]);destination.Add(old[i+2]);}
            old.Clear();old.AddRange(floor);
        }
        internal static void Prune(MeshData mesh)
        {
            foreach(var indices in mesh.Indices)
            {
                int kept=0;
                for(int i=0;i<indices.Count;i+=3)
                {
                    int a=indices[i],b=indices[i+1],c=indices[i+2];
                    if(V3.Cross(mesh.Positions[b]-mesh.Positions[a],mesh.Positions[c]-mesh.Positions[a]).Length<=1e-6f)continue;
                    indices[kept++]=a;indices[kept++]=b;indices[kept++]=c;
                }
                if(kept<indices.Count)indices.RemoveRange(kept,indices.Count-kept);
            }
        }
        private static void Floor(RoomGeometry o,Room r,LayoutConfig c)
        {
            if(r.Plan.Circulation!=null){VariedFloor(o,r,c);return;}
            var xs=new List<float>{-r.Width*.5f,r.Width*.5f};var zs=new List<float>{-r.Depth*.5f,r.Depth*.5f};
            foreach(WaterRegion p in r.Plan.Pools){xs.Add(p.CenterX-p.Width*.5f);xs.Add(p.CenterX+p.Width*.5f);zs.Add(p.CenterZ-p.Depth*.5f);zs.Add(p.CenterZ+p.Depth*.5f);}
            xs.Sort();zs.Sort();
            for(int x=0;x<xs.Count-1;x++)for(int z=0;z<zs.Count-1;z++)
            {
                float mx=(xs[x]+xs[x+1])*.5f,mz=(zs[z]+zs[z+1])*.5f;bool hole=false;
                foreach(WaterRegion p in r.Plan.Pools)if(Math.Abs(mx-p.CenterX)<p.Width*.5f-1e-5f&&Math.Abs(mz-p.CenterZ)<p.Depth*.5f-1e-5f){hole=true;break;}
                if(!hole)ZoneShapes.Box(o.Structure,mx,mz,xs[x+1]-xs[x],zs[z+1]-zs[z],-1.65f,0,Surface.Tile,c.TextureWorldSize);
            }
            // Colour blocks are geometry on the same level; no thousands of renderers or physics balls.
            if(r.Environment==LiminalEnvironment.IndoorPlay)
            {
                int nx=(int)Math.Ceiling(r.Width/1.4f),nz=(int)Math.Ceiling(r.Depth/1.4f);
                float w=r.Width/nx,d=r.Depth/nz;
                for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
                {
                    Surface mat=(x+z)%3==0?Surface.Accent:(x+z)%3==1?Surface.Metal:Surface.Tile;
                    float ax=-r.Width*.5f+x*w,az=-r.Depth*.5f+z*d;
                    o.Decoration.Quad(new V3(ax,.004f,az),new V3(ax+w,.004f,az),new V3(ax+w,.004f,az+d),new V3(ax,.004f,az+d),V3.Up,mat,c.TextureWorldSize);
                }
            }
            foreach(WaterRegion p in r.Plan.Pools)Basin(o,p,c);
        }
        private static bool Inside(Room r,PlanRect bounds)
        {
            foreach(PlanRect cut in r.Plan.Circulation.Cuts)if(cut.Overlaps(bounds))return false;
            return true;
        }
        private static List<PlanRect> Footprint(Room r,PlanRect bounds)
        {
            var free=new List<PlanRect>{bounds};foreach(PlanRect cut in r.Plan.Circulation.Cuts)PlanRect.Subtract(free,cut);return free;
        }
        private static void Slab(MeshData mesh,Room r,PlanRect bounds,float bottom,float top,Surface material,float uv)
        {
            foreach(PlanRect part in Footprint(r,bounds))ZoneShapes.Box(mesh,part.X,part.Z,part.Width,part.Depth,bottom,top,material,uv);
        }
        private static void VariedFloor(RoomGeometry o,Room r,LayoutConfig c)
        {
            var free=Footprint(r,new PlanRect(-r.Width*.5f,-r.Depth*.5f,r.Width*.5f,r.Depth*.5f));
            foreach(WaterRegion p in r.Plan.Pools)PlanRect.Subtract(free,new PlanRect(p.CenterX-p.Width*.5f,p.CenterZ-p.Depth*.5f,p.CenterX+p.Width*.5f,p.CenterZ+p.Depth*.5f));
            foreach(PlanRect part in free)ZoneShapes.Box(o.Structure,part.X,part.Z,part.Width,part.Depth,-1.65f,0,Surface.Tile,c.TextureWorldSize);
            if(r.Environment==LiminalEnvironment.IndoorPlay)
            {
                int nx=(int)Math.Ceiling(r.Width/1.4f),nz=(int)Math.Ceiling(r.Depth/1.4f);float w=r.Width/nx,d=r.Depth/nz;
                for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
                {
                    float ax=-r.Width*.5f+x*w,az=-r.Depth*.5f+z*d;
                    Surface material=(x+z)%3==0?Surface.Accent:(x+z)%3==1?Surface.Metal:Surface.Tile;
                    foreach(PlanRect part in Footprint(r,new PlanRect(ax,az,ax+w,az+d)))
                        o.Decoration.Quad(new V3(part.MinX,.004f,part.MinZ),new V3(part.MaxX,.004f,part.MinZ),new V3(part.MaxX,.004f,part.MaxZ),new V3(part.MinX,.004f,part.MaxZ),V3.Up,material,c.TextureWorldSize);
                }
            }
            foreach(WaterRegion p in r.Plan.Pools)Basin(o,p,c);
        }
        private static void VariedShell(RoomGeometry o,Room r,LayoutConfig c)
        {
            float hx=r.Width*.5f,hz=r.Depth*.5f;
            for(int side=0;side<4;side++)
            {
                float extent=side%2==0?hx:hz,lo=-extent,hi=extent;
                foreach(PlanRect cut in r.Plan.Circulation.Cuts)
                {
                    bool touches=side==0?cut.MaxZ==hz:side==1?cut.MaxX==hx:side==2?cut.MinZ==-hz:cut.MinX==-hx;
                    if(!touches)continue;
                    float a=side==0?cut.MinX:side==1?-cut.MaxZ:side==2?-cut.MaxX:cut.MinZ;
                    float b=side==0?cut.MaxX:side==1?-cut.MinZ:side==2?-cut.MinX:cut.MaxZ;
                    if(a<=-extent+.001f)lo=Math.Max(lo,b);else hi=Math.Min(hi,a);
                }
                var wall=new MeshData();
                if(r.HasExit(side))
                {
                    float half=c.DoorWidth*.5f+.7f;
                    // Keep the portal centred at the shared connector, even for an asymmetric outline.
                    ZoneShapes.Portal(wall,0,0,0,half*2,r.Height,0,c.DoorWidth,c.DoorHeight,c.WallThickness,r.Environment!=LiminalEnvironment.Poolrooms?3:1,c.CurveSegments/2,c.TextureWorldSize);
                    WallPart(wall,lo,-half,r.Height,c);WallPart(wall,half,hi,r.Height,c);
                }
                else WallPart(wall,lo,hi,r.Height,c);
                wall.TranslateRotate(new V3(Directions.DX[side]*hx,0,Directions.DZ[side]*hz),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
            foreach(PlanRect cut in r.Plan.Circulation.Cuts)
            {
                float x=cut.MinX<0?cut.MaxX:cut.MinX,z=cut.MinZ<0?cut.MaxZ:cut.MinZ;
                ZoneShapes.Box(o.Structure,x,cut.Z,c.WallThickness,cut.Depth,0,r.Height,Surface.Tile,c.TextureWorldSize);
                ZoneShapes.Box(o.Structure,cut.X,z,cut.Width,c.WallThickness,0,r.Height,Surface.Tile,c.TextureWorldSize);
            }
            Slab(o.Structure,r,new PlanRect(-hx,-hz,hx,hz),r.Height,r.Height+.25f,Surface.Plaster,c.TextureWorldSize);
        }
        private static void WallPart(MeshData mesh,float lo,float hi,float height,LayoutConfig c)
        {if(hi-lo>.001f)ZoneShapes.Box(mesh,(lo+hi)*.5f,0,hi-lo,c.WallThickness,0,height,Surface.Tile,c.TextureWorldSize);}
        internal static void Basin(RoomGeometry o,WaterRegion p,LayoutConfig c)
        {
            var m=new MeshData();var water=new MeshData();int n=Math.Max(3,c.CurveSegments/4);float uv=c.TextureWorldSize;
            V2[] outer=Shapes.RoundedRect(p.Width,p.Depth,0,n),contour=Shapes.RoundedRect(p.Width,p.Depth,p.Radius,n);
            Shapes.Ring(m,outer,contour,0,Surface.Tile,uv);
            Shapes.Skirt(m,outer,-1.65f,0,Surface.Tile,uv);Shapes.Fill(m,outer,-1.65f,Surface.Tile,uv,false);
            int steps=(int)Math.Round(-p.Bottom/.21f);float previous=0;
            for(int i=1;i<=steps;i++)
            {
                float y=p.Bottom*i/steps;
                Shapes.Skirt(m,contour,y,previous,Surface.Accent,uv,true);
                V2[] next=Shapes.RoundedRect(p.Width-.6f*i,p.Depth-.6f*i,Math.Max(0,p.Radius-.3f*i),n);
                Shapes.Ring(m,contour,next,y,Surface.Tile,uv);contour=next;previous=y;
            }
            Shapes.Fill(m,contour,p.Bottom,Surface.Accent,uv);
            Shapes.Fill(water,Shapes.RoundedRect(p.Width,p.Depth,p.Radius,n),p.Level,Surface.Tile,uv);
            V3 offset=new V3(p.CenterX,0,p.CenterZ);m.TranslateRotate(offset,0);water.TranslateRotate(offset,0);
            o.Structure.Append(m);o.Water.Append(water);
        }
        private static void Shell(RoomGeometry o,Room r,LayoutConfig c)
        {
            if(r.Plan.Circulation!=null){VariedShell(o,r,c);return;}
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();float width=side%2==0?r.Width:r.Depth;
                if(r.HasExit(side))ZoneShapes.Portal(wall,0,0,0,width,r.Height,0,c.DoorWidth,c.DoorHeight,c.WallThickness,
                    r.Environment!=LiminalEnvironment.Poolrooms?3:1,c.CurveSegments/2,c.TextureWorldSize);
                else ZoneShapes.Box(wall,0,0,width,c.WallThickness,0,r.Height,Surface.Tile,c.TextureWorldSize);
                wall.TranslateRotate(new V3(Directions.DX[side]*r.Width*.5f,0,Directions.DZ[side]*r.Depth*.5f),side*Shapes.Pi*.5f);
                o.Structure.Append(wall);
            }
            ZoneShapes.Box(o.Structure,0,0,r.Width,r.Depth,r.Height,r.Height+.25f,Surface.Plaster,c.TextureWorldSize);
        }
        private static void Ceiling(RoomGeometry o,Room r,LayoutConfig c)
        {
            float h=r.Height,uv=c.TextureWorldSize;
            int rows=r.Plan.CeilingPattern==0?2:3,columns=r.Plan.CeilingPattern==2?3:2;
            for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)
            {
                float px=(x+.5f)*r.Width/columns-r.Width*.5f,pz=(z+.5f)*r.Depth/rows-r.Depth*.5f;
                bool sky=r.Environment==LiminalEnvironment.Poolrooms&&r.Plan.CeilingPattern==2&&x==1;
                float width=sky?2.6f:1.5f,depth=sky?2.6f:.45f;
                if(r.Plan.Circulation!=null&&!Inside(r,new PlanRect(px-width*.5f,pz-depth*.5f,px+width*.5f,pz+depth*.5f)))continue;
                ZoneShapes.Box(o.Structure,px,pz,width,depth,h-.12f,h-.08f,Surface.Glow,uv);
                o.Fixtures.Add(new Fixture{Position=new V3(px,h-.2f,pz),Width=width,Depth=depth,Power=sky?4500:1900,Temperature=sky?5600:4600,Skylight=sky});
            }
            // Beams/grid vary independently of modules; every route retains adult headroom.
            if(r.Plan.CeilingPattern!=0)
            for(int x=-1;x<=1;x++)
            {
                if(r.Plan.Circulation==null)ZoneShapes.Box(o.Structure,x*r.Width*.27f,0,.18f,r.Depth,h-.25f,h,Surface.Plaster,uv);
                else Slab(o.Structure,r,new PlanRect(x*r.Width*.27f-.09f,-r.Depth*.5f,x*r.Width*.27f+.09f,r.Depth*.5f),h-.25f,h,Surface.Plaster,uv);
            }
        }
        private static void Module(RoomGeometry o,Room room,PlannedModule p,LayoutConfig c,int ballAllowance)
        {
            if(p.Kind==LiminalModule.Pool)return;
            var m=new MeshData();var decoration=new MeshData();float w=p.LocalWidth,d=p.LocalDepth,uv=c.TextureWorldSize;
            var random=new StableRandom(p.Seed);int n=Math.Max(8,Math.Min(24,c.CurveSegments));
            switch(p.Kind)
            {
                case LiminalModule.Columns:
                    int count=1+random.Range(3);
                    for(int i=0;i<count;i++)Shapes.Column(m,(i-(count-1)*.5f)*(w-.8f)/Math.Max(1,count),0,random.Range(.2f,.4f),0,room.Height,n,Surface.Tile,uv);
                    break;
                case LiminalModule.Arch:case LiminalModule.Screen:
                    int frames=p.Kind==LiminalModule.Arch?1+random.Range(3):1;
                    for(int i=0;i<frames;i++)ZoneShapes.Portal(m,0,(i-(frames-1)*.5f)*d/frames,0,w,room.Height-.3f,0,
                        Math.Max(1.8f,w-.65f),Math.Min(room.Height-.6f,4.5f),.22f,p.Kind==LiminalModule.Screen?3:random.Range(3),n,uv);
                    break;
                case LiminalModule.Bench:
                    ZoneShapes.Box(m,0,0,w*.7f,.65f,0,.43f,Surface.Accent,uv);
                    ZoneShapes.Box(m,0,.25f,w*.7f,.15f,.43f,.85f,Surface.Tile,uv);break;
                case LiminalModule.Platform:case LiminalModule.PlayFrame:
                    Platform(m,decoration,p,uv,p.Kind==LiminalModule.PlayFrame);break;
                case LiminalModule.Tunnel:
                    ZoneShapes.Vault(m,0,0,1.2f,d-.6f,1.25f,0,n,uv);
                    for(int side=-1;side<=1;side+=2)ZoneShapes.Box(m,side*1.31f,0,.22f,d-.6f,0,1.25f,Surface.Metal,uv);
                    Reassign(m,Surface.Tile,Surface.Metal);break;
                case LiminalModule.BallPit:BallPit(m,decoration,w,d,ref random,c,ballAllowance);break;
                case LiminalModule.Slide:Slide(m,p,c);break;
            }
            V3 offset=new V3(p.X,0,p.Z);float yaw=p.QuarterTurns*Shapes.Pi*.5f;
            m.TranslateRotate(offset,yaw);decoration.TranslateRotate(offset,yaw);o.Structure.Append(m);o.Decoration.Append(decoration);
        }
        private static void Platform(MeshData m,MeshData decoration,PlannedModule p,float uv,bool play)
        {
            float w=p.LocalWidth,d=p.LocalDepth,rise=p.Rise,flight=d-1.75f;
            Surface mat=play?Surface.Metal:Surface.Tile;
            // Full-width landing joined to stairs or a gentle ramp, with no slab over the flight.
            ZoneShapes.Box(m,0,d*.5f-.75f,w-.4f,1.5f,rise-.18f,rise,mat,uv);
            if((p.Seed&1)==0)
            {
                int steps=(int)Math.Round(rise/.2f);float tread=flight/steps;
                for(int i=0;i<steps;i++)ZoneShapes.Box(m,0,-d*.5f+.25f+(i+.5f)*tread,1.8f,tread,0,rise*(i+1)/steps,mat,uv);
            }
            else ZoneShapes.Ramp(m,0,-.625f,1.8f,flight,rise,0,uv);
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*(w*.5f-.35f);
                Shapes.Column(m,x,d*.5f-.8f,.12f,0,rise,8,mat,uv);
                ZoneShapes.Beam(m,new V3(x,rise+.9f,d*.5f-1.5f),new V3(x,rise+.9f,d*.5f-.08f),.07f,Surface.Metal,uv);
            }
            // Back guard does not obstruct access from the flight.
            ZoneShapes.Beam(m,new V3(-w*.5f+.2f,rise+.9f,d*.5f-.1f),new V3(w*.5f-.2f,rise+.9f,d*.5f-.1f),.07f,Surface.Metal,uv);
            if(!play)return;
            float top=rise+2.5f;
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*(w*.5f-.2f);
                for(int end=-1;end<=1;end+=2)Shapes.Column(m,x,end*(d*.5f-.2f),.12f,0,top,8,Surface.Slide,uv);
                ZoneShapes.Beam(m,new V3(x,top,-d*.5f+.2f),new V3(x,top,d*.5f-.2f),.12f,Surface.Accent,uv);
                // Nets are a single non-colliding mesh; posts and guards carry the physical boundaries.
                for(float z=-d*.5f+.3f;z<d*.5f-.2f;z+=.42f)ZoneShapes.Beam(decoration,new V3(x,.15f,z),new V3(x,top,z),.012f,Surface.Plaster,uv,4);
                for(float y=.3f;y<top;y+=.42f)ZoneShapes.Beam(decoration,new V3(x,y,-d*.5f+.2f),new V3(x,y,d*.5f-.2f),.012f,Surface.Plaster,uv,4);
            }
        }
        private static void Slide(MeshData m,PlannedModule p,LayoutConfig c)
        {
            float w=p.LocalWidth,d=p.LocalDepth,rise=p.Rise,uv=c.TextureWorldSize;
            // Adult-width trough: platform aligns with its bottom, not its centreline.
            float x=-w*.18f,z0=d*.5f-1.2f,z1=-d*.5f+.7f,radius=.65f;
            Shapes.Slide(m,new V3(x,rise,z0),new V3(x,rise,z0-(z0-z1)*.35f),
                new V3(x,0,z1+(z0-z1)*.3f),new V3(x,0,z1),radius,Math.Min(32,c.CurveSegments));
            ZoneShapes.Box(m,0,d*.5f-.6f,w-.4f,1.2f,rise-.18f,rise,Surface.Metal,uv);
            int steps=(int)Math.Round(rise/.2f);float flight=d-1.8f,tread=flight/steps;
            for(int i=0;i<steps;i++)ZoneShapes.Box(m,w*.5f-.85f,-d*.5f+.6f+(i+.5f)*tread,1.3f,tread,0,rise*(i+1)/steps,Surface.Accent,uv);
            for(int side=-1;side<=1;side+=2)Shapes.Column(m,side*(w*.5f-.3f),d*.5f-.6f,.12f,0,rise,8,Surface.Metal,uv);
        }
        private static void BallPit(MeshData m,MeshData decoration,float w,float d,ref StableRandom random,LayoutConfig c,int ballAllowance)
        {
            float uv=c.TextureWorldSize;
            ZoneShapes.Box(m,0,0,w-.4f,d-.4f,0,.28f,Surface.Accent,uv);
            for(int side=-1;side<=1;side+=2)ZoneShapes.Box(m,side*(w*.5f-.15f),0,.3f,d,0,.48f,Surface.Slide,uv);
            // Static spheres have no individual GameObjects, colliders or rigidbodies.
            int budget=c.CurveSegments<=12?24:c.CurveSegments<=32?48:80;
            int count=Math.Min(ballAllowance,Math.Min(budget,(int)(w*d*3)));
            for(int i=0;i<count;i++)
            {
                float x=random.Range(-w*.5f+.45f,w*.5f-.45f),z=random.Range(-d*.5f+.35f,d*.5f-.35f);
                Sphere(decoration,new V3(x,.37f,z),random.Range(.1f,.15f),random.Range(3)==0?Surface.Accent:random.Range(2)==0?Surface.Metal:Surface.Slide,c.CurveSegments);
            }
        }
        private static void Sphere(MeshData m,V3 centre,float radius,Surface mat,int quality)
        {
            int rings=quality<=12?3:quality<=32?4:6,sides=quality<=12?6:quality<=32?8:10;
            for(int y=0;y<rings;y++)for(int x=0;x<sides;x++)
            {
                float a=y*Shapes.Pi/rings,b=(y+1)*Shapes.Pi/rings,u=x*Shapes.Pi*2/sides,v=(x+1)*Shapes.Pi*2/sides;
                V3 n0=Point(a,u),n1=Point(a,v),n2=Point(b,v),n3=Point(b,u);
                m.QuadUV(centre+n0*radius,centre+n1*radius,centre+n2*radius,centre+n3*radius,n0,n1,n2,n3,new V2(),new V2(1,0),new V2(1,1),new V2(0,1),mat);
            }
        }
        private static V3 Point(float a,float b)
        {if(a<1e-6f)return V3.Up;if(a>Shapes.Pi-1e-6f)return V3.Up*-1;return new V3(Shapes.Sin(a)*Shapes.Cos(b),Shapes.Cos(a),Shapes.Sin(a)*Shapes.Sin(b));}
        private static void Reassign(MeshData m,Surface from,Surface to)
        {m.Indices[(int)to].AddRange(m.Indices[(int)from]);m.Indices[(int)from].Clear();}
    }
}
