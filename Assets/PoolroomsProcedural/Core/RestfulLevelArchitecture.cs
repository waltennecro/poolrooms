using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class RestfulLevelArchitecture
    {
        public const float TableTop=.82f;
        private static readonly MeshData CeramicTemplate=MakeCeramicVessel();
        static void Box(MeshData m,float x,float z,float w,float d,float lo,float hi,Surface s)
        {ZoneShapes.Box(m,x,z,w,d,lo,hi,s,1.2f);}
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Restful;if(p==null||!p.Valid(r,c))throw new ArgumentException("Invalid restful architecture");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=RestfulLevelPlanner.Theme(r.Environment);o.Glass.Theme=SurfaceTheme.Glass;
            float half=r.Width*.5f;var floor=new List<PlanRect>{new PlanRect(-half,-half,half,half)};
            foreach(var pool in p.Pools)PlanRect.Subtract(floor,new PlanRect(pool.CenterX-pool.Width*.5f,pool.CenterZ-pool.Depth*.5f,pool.CenterX+pool.Width*.5f,pool.CenterZ+pool.Depth*.5f));
            foreach(var f in floor)Box(o.Structure,f.X,f.Z,f.Width,f.Depth,-.28f,0,Surface.Tile);
            foreach(var pool in p.Pools)Basin(o,pool);
            Envelope(o,r,c);Roof(o,r,c);
            for(int i=0;i<4;i++)
            {
                var b=p.Bays[i];int sx=i%2==0?-1:1,sz=i<2?-1:1;
                switch(r.Environment)
                {
                    case LiminalEnvironment.SunsetLibrary:Library(o,b,p);break;
                    case LiminalEnvironment.CloudTerminal:Terminal(o,b,p);break;
                    case LiminalEnvironment.TeaPavilions:Tea(o,b,p);break;
                    case LiminalEnvironment.CeramicGalleria:Ceramics(o,b,p);break;
                    case LiminalEnvironment.WinterGarden:Garden(o,b,p);break;
                    case LiminalEnvironment.CanalGalleries:Canal(o,b,p);break;
                    case LiminalEnvironment.MusicStudios:Music(o,b,p);break;
                    case LiminalEnvironment.LanternCourts:Lanterns(o,b,p);break;
                    case LiminalEnvironment.LinenLaundries:Laundry(o,b,p);break;
                    case LiminalEnvironment.MineralSanctuary:Sanctuary(o,b,p);break;
                }
                // Separate reachable furniture alcove, identical support heights in plan/runtime.
                float z=sz*(half-1.25f);
                Box(o.Structure,b.X,z,2.65f,1.22f,TableTop-.09f,TableTop,Surface.Accent);
                foreach(int dx in new[]{-1,1})foreach(int dz in new[]{-1,1})Box(o.Structure,b.X+dx*1.1f,z+dz*.47f,.085f,.085f,0,TableTop-.09f,Surface.Metal);
                // Skirting and inset ceiling joinery, with no decorative coplanar faces.
                Box(o.Decoration,sx*(half-.12f),sz*(p.Clear+half)*.5f,.1f,half-p.Clear-.3f,.03f,.14f,Surface.Accent);
            }
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.Glass);return o;
        }
        static void Envelope(RoomGeometry o,Room r,LayoutConfig c)
        {
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();
                if(r.HasExit(side))Shapes.Portal(wall,r.Width,r.Height,0,c.DoorWidth,c.DoorHeight,.28f,Math.Max(12,c.CurveSegments/2),1.2f);
                else Box(wall,0,0,r.Width,.28f,0,r.Height,Surface.Plaster);
                wall.Indices[(int)Surface.Plaster].AddRange(wall.Indices[(int)Surface.Tile]);wall.Indices[(int)Surface.Tile].Clear();
                wall.TranslateRotate(new V3(Directions.DX[side]*r.Width*.5f,0,Directions.DZ[side]*r.Depth*.5f),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
        }
        static void Roof(RoomGeometry o,Room r,LayoutConfig c)
        {
            var p=r.Plan.Restful;float half=r.Width*.5f,aperture=r.Width*(.14f+p.Roof*.012f);
            var solid=new List<PlanRect>{new PlanRect(-half,-half,half,half)};
            var opening=new PlanRect(-aperture,-half+1.1f,aperture,half-1.1f);PlanRect.Subtract(solid,opening);
            foreach(var t in solid)Box(o.Structure,t.X,t.Z,t.Width,t.Depth,r.Height,r.Height+.2f,Surface.Plaster);
            Box(o.Glass,0,0,opening.Width,opening.Depth,r.Height+.09f,r.Height+.115f,Surface.Tile);
            int ribs=5+p.Rhythm;
            for(int i=0;i<=ribs;i++)
            {
                float z=-half+1.1f+(r.Depth-2.2f)*i/ribs;
                Box(o.Decoration,0,z,opening.Width+.2f,.085f,r.Height-.22f,r.Height+.08f,Surface.Metal);
                // Eight physical clerestory/louvre profiles, independent from colour or seed.
                if((p.Roof&1)!=0)Box(o.Decoration,0,z+.16f,opening.Width,.12f,r.Height+.16f,r.Height+.24f+p.Roof*.035f,Surface.Accent);
            }
            foreach(int side in new[]{-1,1})Box(o.Decoration,side*(aperture+.05f),0,.12f,r.Depth-2.1f,r.Height-.25f,r.Height+.15f,Surface.Metal);
            var tone=r.Environment==LiminalEnvironment.SunsetLibrary||r.Environment==LiminalEnvironment.TeaPavilions?new V3(1,.73f,.46f):new V3(1,.95f,.88f);
            o.Decoration.EmissionTint=tone;o.Decoration.EmissionStrength=.18f;
            // Local aperture fallback shares the actual sun hue and illuminates below a
            // real opening. The existing single-sun arbiter owns directional shadows.
            foreach(int z in new[]{-1,1})o.Fixtures.Add(new Fixture{Position=new V3(0,r.Height-.35f,z*r.Depth*.23f),Width=opening.Width*.8f,Depth=4,Power=4200,Range=r.Width*.8f,CustomColor=true,LightColor=tone,SolarFill=true});
        }
        // All four layout programmes change physical walls/aisles within each side bay.
        static void Shelves(RoomGeometry o,PlanRect b,RestfulLevelPlan p,AssemblyKind kind)
        {
            int count=p.Layout==0?1:p.Layout==3?3:2;
            for(int i=0;i<count;i++)
            {
                float x=b.X,z=b.Z,w=b.Width,d=Math.Min(.7f,b.Depth*.22f);
                if(p.Layout==1){w=(b.Width-1.2f)*.5f;x=b.MinX+w*.5f+i*(w+1.2f);}
                if(p.Layout==2){w=b.Width*.76f;z=b.MinZ+.4f+i*(b.Depth-.8f);}
                if(p.Layout==3){w=b.Width*.4f;x=b.MinX+w*.5f+i*b.Width*.28f;z=b.MinZ+.4f+i*(b.Depth-.8f)*.5f;}
                var rect=new PlanRect(x-w*.5f,z-d*.5f,x+w*.5f,z+d*.5f);
                AutonomousArchitecture.Build(o.Structure,o.Decoration,new GeneratedAssembly(kind,rect,0,(uint)(p.Recipe+i*71)),12);
            }
            // Footprint is visible, and Span/Reach always affect actual geometry.
            Box(o.Decoration,b.X,b.MinZ+.06f,b.Width,.065f,.004f,.012f,Surface.Accent);
            Box(o.Decoration,b.MaxX-.06f,b.Z,.065f,b.Depth,.004f,.012f,Surface.Accent);
        }
        static void Library(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Shelves(o,b,p,AssemblyKind.Bookcase);
            for(int i=0;i<2+p.Layout;i++)
            {
                float z=b.MinZ+.25f+i*(b.Depth-.5f)/(1+p.Layout);
                Box(o.Decoration,b.MinX+.1f,z,.08f,.13f,2.25f,3.1f,Surface.Accent);
            }
            Box(o.Structure,b.X,b.MaxZ-.3f,b.Width,.45f,0,.42f,Surface.Accent);
        }
        static void Terminal(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            Shelves(o,b,p,AssemblyKind.SeatGroup);
            float h=3.1f+p.Layout*.22f;
            foreach(int side in new[]{-1,1})Shapes.Column(o.Structure,b.X+side*b.Width*.42f,b.MaxZ-.25f,.095f,0,h,12,Surface.Metal,1.2f);
            Box(o.Structure,b.X,b.MaxZ-.25f,b.Width,.14f,h,h+.16f,Surface.Metal);
            Box(o.Decoration,b.X,b.MaxZ-.32f,b.Width*.62f,.04f,h-.65f,h-.12f,Surface.Accent);
            for(int row=0;row<4;row++)Box(o.Decoration,b.X,b.MaxZ-.35f,b.Width*.53f,.015f,h-.55f+row*.09f,h-.53f+row*.09f,Surface.Plaster);
        }
        static void Tea(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {float x=b.X+sx*(b.Width*.5f-.14f),z=b.Z+sz*(b.Depth*.5f-.14f);Shapes.Column(o.Structure,x,z,.085f,0,3+p.Layout*.2f,12,Surface.Accent,1.2f);}
            for(int i=0;i<5+p.Layout;i++)Box(o.Decoration,b.MinX+(i+.5f)*b.Width/(5+p.Layout),b.Z,.07f,b.Depth,3+p.Layout*.2f,3.12f+p.Layout*.2f,Surface.Accent);
            Box(o.Structure,b.X,b.Z,Math.Min(1.8f,b.Width*.65f),Math.Min(1.15f,b.Depth*.4f),.32f,.4f,Surface.Accent);
            Box(o.Structure,b.X,b.MaxZ-.35f,b.Width-.2f,.5f,0,.3f,Surface.Accent);
            for(int slat=0;slat<5+p.Layout;slat++)Box(o.Decoration,b.MinX+(slat+.5f)*b.Width/(5+p.Layout),b.MaxZ-.15f,.035f,.045f,.4f,2.6f,Surface.Accent);
        }
        static void Ceramics(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int count=2+p.Layout;
            for(int i=0;i<count;i++)
            {
                float x=b.MinX+b.Width*(i+.5f)/count,z=b.Z+(i%2==0?-1:1)*b.Depth*.2f,h=.65f+i*.13f;
                Shapes.Column(o.Structure,x,z,.27f,0,h,16,Surface.Plaster,1.2f);
                var vessel=new MeshData();vessel.Append(CeramicTemplate);
                vessel.TranslateRotate(new V3(x,h+.02f,z),0);o.Decoration.Append(vessel);
            }
            Box(o.Structure,b.X,b.MaxZ-.18f,b.Width,.18f,0,2.1f,Surface.Plaster);
            Box(o.Decoration,b.MinX+.05f,b.Z,.06f,b.Depth,.004f,.012f,Surface.Accent);
        }
        // Immutable pure-geometry template: generate the revolved profile once,
        // clone before placement, and retain smooth outward/inward normals separately.
        static MeshData MakeCeramicVessel()
        {
            var m=new MeshData();const int rings=8,sides=16;const float height=.68f,thickness=.018f;
            for(int ring=0;ring<rings;ring++)
            {
                float y0=height*ring/rings,y1=height*(ring+1)/rings;
                float r0=.1f+.12f*Shapes.Sin(ring*Shapes.Pi/rings),r1=.1f+.12f*Shapes.Sin((ring+1)*Shapes.Pi/rings);
                float slope0=.12f*Shapes.Pi/height*Shapes.Cos(ring*Shapes.Pi/rings),slope1=.12f*Shapes.Pi/height*Shapes.Cos((ring+1)*Shapes.Pi/rings);
                for(int a=0;a<sides;a++)
                {
                    float a0=a*Shapes.Pi*2/sides,a1=(a+1)*Shapes.Pi*2/sides;
                    foreach(int shell in new[]{0,1})
                    {
                        float sign=shell==0?1:-1,lo=r0-shell*thickness,hi=r1-shell*thickness;
                        var v0=new V3(Shapes.Cos(a0)*lo,y0,Shapes.Sin(a0)*lo);var v1=new V3(Shapes.Cos(a1)*lo,y0,Shapes.Sin(a1)*lo);
                        var v2=new V3(Shapes.Cos(a1)*hi,y1,Shapes.Sin(a1)*hi);var v3=new V3(Shapes.Cos(a0)*hi,y1,Shapes.Sin(a0)*hi);
                        var n0=new V3(Shapes.Cos(a0),-slope0,Shapes.Sin(a0)).Unit*sign;var n1=new V3(Shapes.Cos(a1),-slope0,Shapes.Sin(a1)).Unit*sign;
                        var n2=new V3(Shapes.Cos(a1),-slope1,Shapes.Sin(a1)).Unit*sign;var n3=new V3(Shapes.Cos(a0),-slope1,Shapes.Sin(a0)).Unit*sign;
                        m.QuadUV(v0,v1,v2,v3,n0,n1,n2,n3,new V2((float)a/sides,(float)ring/rings),new V2((float)(a+1)/sides,(float)ring/rings),new V2((float)(a+1)/sides,(float)(ring+1)/rings),new V2((float)a/sides,(float)(ring+1)/rings),Surface.Accent);
                    }
                }
            }
            for(int a=0;a<sides;a++)
            {
                float a0=a*Shapes.Pi*2/sides,a1=(a+1)*Shapes.Pi*2/sides;
                var v0=new V3(Shapes.Cos(a0)*.1f,height,Shapes.Sin(a0)*.1f);var v1=new V3(Shapes.Cos(a1)*.1f,height,Shapes.Sin(a1)*.1f);
                var v2=new V3(Shapes.Cos(a1)*(.1f-thickness),height,Shapes.Sin(a1)*(.1f-thickness));var v3=new V3(Shapes.Cos(a0)*(.1f-thickness),height,Shapes.Sin(a0)*(.1f-thickness));
                m.Quad(v0,v1,v2,v3,V3.Up,Surface.Accent,1.2f);
            }
            Shapes.Column(m,0,0,.1f,0,thickness,sides,Surface.Accent,1.2f);return m;
        }
        static void Garden(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float w=b.Width*(p.Layout==1?.45f:.85f),d=b.Depth*(p.Layout==2?.45f:.85f);
            Box(o.Structure,b.X,b.Z,w,d,0,.38f,Surface.Plaster);Box(o.Decoration,b.X,b.Z,w-.08f,d-.08f,.38f,.395f,Surface.Accent);
            // Licensed foliage comes through the standard interactable source loader.
            Box(o.Structure,b.MinX+.15f,b.Z,.24f,b.Depth,0,2.7f,Surface.Metal);
            for(int rail=0;rail<4+p.Layout;rail++)Box(o.Decoration,b.MinX+.15f,b.Z,.28f,b.Depth,.6f+rail*.35f,.64f+rail*.35f,Surface.Accent);
        }
        static void Canal(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            // The deck does not span a hidden solid slab; water occupies a cut-out basin.
            float bridge=.7f+p.Layout*.16f;
            Box(o.Structure,b.X,b.Z,bridge,b.Depth,-.1f,.055f,Surface.Accent);
            foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*(bridge*.5f+.06f),b.Z,.06f,b.Depth,0,.75f,Surface.Metal);
            foreach(int side in new[]{-1,1})Shapes.Column(o.Structure,b.MinX+.2f,b.Z+side*b.Depth*.35f,.12f,0,2.7f+p.Layout*.15f,12,Surface.Plaster,1.2f);
        }
        static void Music(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float x=b.X,z=b.MaxZ-.45f,w=Math.Min(1.75f,b.Width-.4f);
            Box(o.Structure,x,z,w,.65f,0,1.22f,Surface.Accent);
            Box(o.Structure,x,z-.36f,w,.3f,.68f,.74f,Surface.Accent);
            for(int key=0;key<22;key++)
            {
                float kx=x+(key-10.5f)*w/22;
                Box(o.Decoration,kx,z-.4f,w/22*.92f,.23f,.74f,.765f,Surface.Plaster);
                if(key%7!=2&&key%7!=6)Box(o.Decoration,kx+w/44,z-.33f,w/22*.45f,.1f,.765f,.79f,Surface.Metal);
            }
            Box(o.Structure,x,z-1.3f,w*.7f,.44f,0,.45f,Surface.Accent);
            for(int panel=0;panel<3+p.Layout;panel++)
            {float px=b.MinX+(panel+.5f)*b.Width/(3+p.Layout);Box(o.Structure,px,b.MinZ+.12f,b.Width/(3+p.Layout)*.6f,.12f,.5f,2.6f,Surface.Plaster);}
            Box(o.Decoration,b.MinX+.04f,b.Z,.06f,b.Depth,.004f,.012f,Surface.Accent);
        }
        static void Lanterns(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int n=3+p.Layout;
            for(int i=0;i<n;i++)
            {
                float x=b.MinX+.3f+(b.Width-.6f)*i/(n-1),z=b.MinZ+.3f+(b.Depth-.6f)*i/(n-1),h=2.8f+(i%3)*.25f;
                Shapes.Column(o.Structure,x,z,.065f,0,h,10,Surface.Metal,1.2f);
                Box(o.Decoration,x,z,.4f,.4f,h-.5f,h,Surface.Plaster);
                foreach(int side in new[]{-1,1})Box(o.Decoration,x+side*.22f,z,.035f,.45f,h-.55f,h+.07f,Surface.Accent);
                Box(o.Decoration,x,z,.52f,.52f,h+.03f,h+.1f,Surface.Accent);
            }
            Box(o.Structure,b.X,b.MaxZ-.25f,b.Width,.4f,0,.4f,Surface.Plaster);
        }
        static void Laundry(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            int count=2+p.Layout;float width=Math.Min(.8f,(b.Width-.25f)/count);
            for(int i=0;i<count;i++)
            {
                float x=b.MinX+(i+.5f)*b.Width/count,z=b.MaxZ-.5f;
                Box(o.Structure,x,z,width,.8f,0,1.05f,Surface.Plaster);
                // Cylindrical door faces out into a real approach aisle; no decal windows.
                var ring=new MeshData();Shapes.Column(ring,0,0,width*.34f,0,.055f,24,Surface.Metal,1.2f);
                for(int v=0;v<ring.Positions.Count;v++){var q=ring.Positions[v];ring.Positions[v]=new V3(x+q.X,.51f+q.Z,z-.405f-q.Y);var n=ring.Normals[v];ring.Normals[v]=new V3(n.X,n.Z,-n.Y);}
                o.Decoration.Append(ring);
                Box(o.Decoration,x,z-.414f,width*.72f,.015f,.88f,.99f,Surface.Metal);
                for(int knob=0;knob<3;knob++)Box(o.Decoration,x-width*.22f+knob*width*.22f,z-.425f,.045f,.025f,.91f,.95f,Surface.Plaster);
            }
            Box(o.Structure,b.X,b.MinZ+.3f,b.Width,.55f,.74f,.82f,Surface.Accent);
            foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*b.Width*.4f,b.MinZ+.3f,.08f,.4f,0,.74f,Surface.Metal);
        }
        static void Sanctuary(RoomGeometry o,PlanRect b,RestfulLevelPlan p)
        {
            float h=2.7f+p.Layout*.24f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=b.X+sx*(b.Width*.5f-.12f),z=b.Z+sz*(b.Depth*.5f-.12f);
                Shapes.Column(o.Structure,x,z,.115f,0,h,16,Surface.Plaster,1.2f);
                Shapes.Column(o.Decoration,x,z,.17f,h-.14f,h,16,Surface.Accent,1.2f);
            }
            foreach(int side in new[]{-1,1})Box(o.Structure,b.X,b.Z+side*(b.Depth*.5f-.12f),b.Width,.2f,h,h+.18f,Surface.Plaster);
        }
        static void Basin(RoomGeometry o,WaterRegion p)
        {
            Box(o.Structure,p.CenterX,p.CenterZ,p.Width,p.Depth,p.Bottom-.2f,p.Bottom,Surface.Slide);
            foreach(int side in new[]{-1,1})
            {
                Box(o.Structure,p.CenterX+side*(p.Width*.5f+.1f),p.CenterZ,.2f,p.Depth,p.Bottom,0,Surface.Slide);
                Box(o.Structure,p.CenterX,p.CenterZ+side*(p.Depth*.5f+.1f),p.Width+.4f,.2f,p.Bottom,0,Surface.Slide);
            }
            // Broad four-tread submerged entry at the outer edge; player-sized treads.
            for(int step=0;step<3;step++)Box(o.Structure,p.CenterX,p.CenterZ-p.Depth*.5f+(step+.5f)*.32f,1.3f,.325f,p.Bottom,-.18f*(step+1),Surface.Slide);
            var fill=Shapes.RoundedRect(p.Width,p.Depth,0,3);var water=new MeshData();Shapes.Fill(water,fill,p.Level,Surface.Tile,1.2f);water.TranslateRotate(new V3(p.CenterX,0,p.CenterZ),0);o.Water.Append(water);o.Regions.Add(p);
        }
    }
}
