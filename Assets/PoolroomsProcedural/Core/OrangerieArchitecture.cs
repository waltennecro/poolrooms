using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // One structural batch, one detail batch, one glass batch and one foliage batch.
    // Leaves never become individual GameObjects, colliders or physics bodies.
    public static class OrangerieArchitecture
    {
        static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,s,1.2f);}
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            var p=room.Plan.Garden;if(p==null||!p.Valid(room,c))throw new ArgumentException("Invalid Orangerie plan.");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.Orangerie;o.Glass.Theme=SurfaceTheme.Glass;
            o.Decoration.EmissionTint=p.Hall==GardenHall.FernHall?new V3(.88f,.96f,1):p.Hall==GardenHall.ReadingGarden?new V3(1,.87f,.67f):p.Hall==GardenHall.DryGarden?new V3(1,.94f,.83f):new V3(1,.91f,.76f);o.Decoration.EmissionStrength=.35f;
            var leaves=new MeshData{Theme=SurfaceTheme.OrangerieFoliage};
            Box(o.Structure,0,0,room.Width,room.Depth,-.3f,0,Surface.Tile);
            Envelope(o,room,c);Roof(o,room,c);Columns(o,room);FurnishBays(o,room,c);
            // Thin inlay strips frame the walking routes; no coplanar duplicate floor.
            foreach(int sign in new[]{-1,1})
            {
                Box(o.Decoration,sign*p.Clear,0,.055f,room.Depth,.003f,.007f,Surface.Metal);
                Box(o.Decoration,0,sign*p.Clear,room.Width,.055f,.008f,.012f,Surface.Metal);
            }
            for(int i=0;i<p.Beds.Length;i++)Court(o,leaves,p.Beds[i],p,i);
            Thresholds(o,room,c);
            // Diffuse daylight bounce and low-energy bay lamps. Apertures admit the actual sun.
            // The regional sun supplies directional shadows; light budget owners stay unchanged.
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                float px=x*room.Width*.23f,pz=z*room.Depth*.23f;
                o.Fixtures.Add(new Fixture{Position=new V3(px,room.Height-.35f,pz),Width=3.2f,Depth=3.2f,Power=3800,Range=room.Width*.58f,CustomColor=true,LightColor=new V3(1,.96f,.89f),SolarFill=true,AmbientSupport=true});
                float lx=x*(room.Width*.5f-.7f),lz=z*room.Depth*.25f;
                Box(o.Decoration,lx,lz,.14f,.6f,3.35f,3.7f,Surface.Metal);
                Box(o.Decoration,lx-x*.08f,lz,.025f,.4f,3.42f,3.63f,Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(lx-x*.16f,3.5f,lz),Width=.2f,Depth=.4f,Power=450+(int)p.Hall*60,Range=6,CustomColor=true,LightColor=o.Decoration.EmissionTint});
            }
            o.VisualMeshes.Add(leaves);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.Glass);ProceduralArchitecture.Prune(leaves);
            return o;
        }
        static void Envelope(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f;
            for(int side=0;side<4;side++)
            {
                var m=new MeshData();float width=Opening(r,side,c);
                if(r.HasExit(side))Shapes.Portal(m,r.Width,r.Height,0,width,DoorHeight(r,side,c),.28f,16,1.2f);
                else Box(m,0,0,r.Width,.28f,0,r.Height,Surface.Plaster);
                // The old portal helper uses the pool tile slot; walls belong to limewash here.
                m.Indices[(int)Surface.Plaster].AddRange(m.Indices[(int)Surface.Tile]);m.Indices[(int)Surface.Tile].Clear();
                // Lower skirting stops at the opening instead of sealing the threshold.
                float gap=r.HasExit(side)?width*.5f+.1f:0;
                foreach(int sign in new[]{-1,1})Box(m,sign*(half+gap)*.5f,-.16f,half-gap,.09f,0,.14f,Surface.Tile);
                m.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Structure.Append(m);
            }
        }
        public static float DoorHeight(Room r,int side,LayoutConfig c)
        {return Math.Max(c.DoorHeight,Opening(r,side,c)*.5f+2);}
        public static float MaxOpening(LayoutConfig c){return Math.Min(9,c.DoorWidth+3);}
        public static float Opening(Room r,int side,LayoutConfig c)
        {
            if(r.Environment!=LiminalEnvironment.Orangerie||r.MallGateway||r.TerrainGateway)return c.DoorWidth;
            long x=(long)r.X+Directions.DX[side],z=(long)r.Z+Directions.DZ[side];
            if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue)return c.DoorWidth;
            ConnectedMallSite mall;ConnectedTerrainSite terrain;
            if(ConnectedTerrainTopology.Reserved(c,(int)x,(int)z)||ConnectedTerrainTopology.Gateway(c,(int)x,(int)z,out terrain)||ConnectedMallTopology.Gateway(c,(int)x,(int)z,out mall))return c.DoorWidth;
            return LiminalRegions.At(c,(int)x,(int)z)==LiminalEnvironment.Orangerie?MaxOpening(c):c.DoorWidth;
        }
        public static List<PlanRect> Apertures(Room r)
        {
            float half=r.Width*.5f,inner=half-2;int roof=r.Plan.Garden.Roof;
            var a=new List<PlanRect>();
            if(roof==0)a.Add(new PlanRect(-inner*.55f,-inner,inner*.55f,inner));
            else if(roof==1)foreach(int x in new[]{-1,1})a.Add(new PlanRect(x*inner*.5f-inner*.19f,-inner,x*inner*.5f+inner*.19f,inner));
            else if(roof==2)foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})a.Add(new PlanRect(x*inner*.52f-inner*.3f,z*inner*.52f-inner*.3f,x*inner*.52f+inner*.3f,z*inner*.52f+inner*.3f));
            else if(roof==3)
            {
                a.Add(new PlanRect(-inner*.22f,-inner,inner*.22f,inner));
                foreach(int x in new[]{-1,1})a.Add(new PlanRect(x<0?-inner:inner*.22f,-inner*.22f,x<0?-inner*.22f:inner,inner*.22f));
            }
            else for(int i=0;i<5;i++)
            {float z=-inner+inner*.2f+i*inner*.4f;a.Add(new PlanRect(-inner,z-inner*.105f,inner,z+inner*.105f));}
            return a;
        }
        static void Roof(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f,height=r.Height;var holes=Apertures(r);
            var solid=new List<PlanRect>{new PlanRect(-half,-half,half,half)};
            foreach(var a in holes)
            {
                PlanRect.Subtract(solid,a);
                float ridge=height+.35f+r.Plan.Garden.Canopy*.15f;
                // A shallow two-slope glazed lantern has a real opening below it.
                var l0=new V3(a.MinX,height+.04f,a.MinZ);var l1=new V3(a.MinX,height+.04f,a.MaxZ);
                var r0=new V3(a.MaxX,height+.04f,a.MinZ);var r1=new V3(a.MaxX,height+.04f,a.MaxZ);
                var c0=new V3(a.X,ridge,a.MinZ);var c1=new V3(a.X,ridge,a.MaxZ);
                o.Glass.Quad(l0,c0,c1,l1,new V3(-(ridge-height-.04f),a.Width*.5f,0).Unit,Surface.Tile,1.2f);
                o.Glass.Quad(c0,r0,r1,c1,new V3(ridge-height-.04f,a.Width*.5f,0).Unit,Surface.Tile,1.2f);
                o.Glass.Triangle(l0,r0,c0,new V3(0,0,-1),new V3(0,0,-1),new V3(0,0,-1),new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Tile);
                o.Glass.Triangle(l1,c1,r1,new V3(0,0,1),new V3(0,0,1),new V3(0,0,1),new V2(0,0),new V2(.5f,1),new V2(1,0),Surface.Tile);
                foreach(int sign in new[]{-1,1})
                {
                    Box(o.Decoration,a.X,a.Z+sign*a.Depth*.5f,a.Width+.1f,.07f,height-.08f,height+.06f,Surface.Metal);
                    Box(o.Decoration,a.X+sign*a.Width*.5f,a.Z,.07f,a.Depth,height-.08f,height+.06f,Surface.Metal);
                }
                Box(o.Decoration,a.X,a.Z,.065f,a.Depth,ridge-.05f,ridge+.025f,Surface.Metal);
            }
            foreach(var p in solid)Box(o.Structure,p.X,p.Z,p.Width,p.Depth,height,height+.2f,Surface.Plaster);
            // Large openings receive roof framing, never a hidden opaque ceiling.
            for(int i=0;i<3+r.Plan.Garden.Colonnade;i++)
            {float z=-half+1.5f+(r.Depth-3)*(i+.5f)/(3+r.Plan.Garden.Colonnade);Box(o.Decoration,0,z,r.Width,.12f,height-.28f,height-.1f,Surface.Metal);}
        }
        static void Columns(RoomGeometry o,Room r)
        {
            var p=r.Plan.Garden;float half=r.Width*.5f;int count=2+p.Colonnade;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})for(int i=0;i<count;i++)
            {
                float x=sx*(half-.4f),z=sz*(p.Clear+1+(half-p.Clear-2)*(i+.5f)/count);
                Shapes.Column(o.Structure,x,z,.13f,0,r.Height,12,Surface.Metal,1.2f);
                Shapes.Column(o.Decoration,x,z,.2f,0,.18f,12,Surface.Tile,1.2f);
                Shapes.Column(o.Decoration,x,z,.21f,r.Height-.2f,r.Height,12,Surface.Tile,1.2f);
            }
        }
        static void FurnishBays(RoomGeometry o,Room r,LayoutConfig c)
        {
            var p=r.Plan.Garden;float half=r.Width*.5f,center=(p.Clear+half-2.4f)*.5f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*center,z=sz*(half-.8f);
                if(p.Hall==GardenHall.ReadingGarden)
                {
                    var a=new GeneratedAssembly(AssemblyKind.Bookcase,new PlanRect(x-1.1f,z-.3f,x+1.1f,z+.3f),sz<0?0:2,p.Seed+(uint)((sx+1)*5+sz+1));
                    AutonomousArchitecture.Build(o.Structure,o.Decoration,a,12);
                }
                else if(p.Hall==GardenHall.TeaGallery)
                {
                    Box(o.Structure,x,z,2.4f,.65f,0,.85f,Surface.Accent);
                    Box(o.Structure,x,z,2.55f,.75f,.85f,.93f,Surface.Tile);
                    for(int tin=0;tin<4;tin++)
                    {
                        Shapes.Column(o.Decoration,x-.75f+tin*.5f,z,.11f,.93f,1.17f,12,Surface.Metal,1.2f);
                        Shapes.Column(o.Decoration,x-.75f+tin*.5f,z,.115f,1.17f,1.2f,12,Surface.Accent,1.2f);
                    }
                    Box(o.Decoration,x,z+sz*.24f,2.3f,.12f,1.7f,1.78f,Surface.Accent);
                }
                else if(p.Hall==GardenHall.PalmCourt)
                {
                    // Freestanding timber trellis on the rear wall, outside every doorway.
                    for(int slat=0;slat<7;slat++)Box(o.Decoration,x-.9f+slat*.3f,z,.045f,.12f,.2f,3.5f,Surface.Accent);
                    for(int slat=0;slat<5;slat++)Box(o.Decoration,x,z,2.1f,.13f,.55f+slat*.65f,.595f+slat*.65f,Surface.Accent);
                    Box(o.Structure,x,z,2.2f,.45f,0,.44f,Surface.Slide);
                }
                else if(p.Hall==GardenHall.FernHall)
                {
                    Box(o.Structure,x,z,2,.5f,0,.55f,Surface.Slide);
                    for(int rung=0;rung<5;rung++)Box(o.Decoration,x,z,2.1f,.12f,1+rung*.38f,1.05f+rung*.38f,Surface.Accent);
                }
                else
                {
                    Box(o.Structure,x,z,1.5f,.75f,0,.55f,Surface.Tile);
                    for(int stone=0;stone<3;stone++)Shapes.Column(o.Structure,x-.42f+stone*.42f,z,.16f,.55f,.85f+stone*.28f,12,Surface.Plaster,1.2f);
                }
            }
        }
        static void Court(RoomGeometry o,MeshData leaves,PlanRect b,OrangeriePlan p,int index)
        {
            // Five distinct planter footprints: rectangular, elliptical, split, L and island.
            float top=.42f+(int)p.Hall*.035f,w=b.Width,d=b.Depth;
            if(p.Bay==1)
            {
                var pot=new MeshData();Shapes.Column(pot,0,0,1,0,top,24,Surface.Slide,1.2f);
                for(int v=0;v<pot.Positions.Count;v++){var q=pot.Positions[v];pot.Positions[v]=new V3(b.X+q.X*w*.5f,q.Y,b.Z+q.Z*d*.5f);var n=pot.Normals[v];pot.Normals[v]=new V3(n.X/(w*.5f),n.Y,n.Z/(d*.5f)).Unit;}
                o.Structure.Append(pot);
            }
            else if(p.Bay==2)
            {foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*w*.29f,b.Z,w*.4f,d,0,top,Surface.Slide);}
            else if(p.Bay==3)
            {Box(o.Structure,b.X-w*.32f,b.Z,w*.36f,d,0,top,Surface.Slide);Box(o.Structure,b.X+w*.18f,b.Z+d*.32f,w*.64f,d*.36f,0,top,Surface.Slide);}
            else if(p.Bay==4)
            {
                Box(o.Structure,b.X,b.Z,w*.78f,d*.78f,0,top,Surface.Tile);
                foreach(int side in new[]{-1,1})Box(o.Structure,b.X+side*w*.42f,b.Z,w*.12f,d,0,top*.55f,Surface.Slide);
            }
            else Box(o.Structure,b.X,b.Z,w,d,0,top,Surface.Slide);
            // Soil/stone surfaces stay above the solid top; bounded by the same court.
            if(p.Bay==0)Box(o.Decoration,b.X,b.Z,w-.16f,d-.16f,top,top+.012f,Surface.Plaster);
            if(p.Hall==GardenHall.DryGarden)
            {
                for(int stone=0;stone<5;stone++)
                {float x=b.X+(stone%2==0?-1:1)*w*.17f,z=b.Z-d*.32f+stone*d*.16f;Shapes.Column(o.Structure,x,z,.16f+stone*.035f,top,top+.22f+stone*.06f,9,Surface.Tile,1.2f);}
            }
            else if(!((p.Hall==GardenHall.ReadingGarden||p.Hall==GardenHall.TeaGallery)&&index%2==0))
            {
                int stems=p.Hall==GardenHall.PalmCourt?2:p.Hall==GardenHall.FernHall?6:4;
                for(int stem=0;stem<stems;stem++)
                {
                    // Keep stems rooted on solid soil for split/L/island shapes too.
                    float x=b.X+(p.Bay==2?-w*.29f:p.Bay==3?-w*.32f:0);
                    float z=b.Z+(stem-(stems-1)*.5f)*Math.Min(.52f,d/(stems+1));
                    float height=top+(p.Hall==GardenHall.PalmCourt?2.7f:1.15f)+(p.Seed+(uint)(index*17+stem*31))%7*.065f;
                    Shapes.Column(o.Decoration,x,z,p.Hall==GardenHall.PalmCourt?.055f:.018f,top,height,6,Surface.Accent,1.2f);
                    for(int leaf=0;leaf<7;leaf++)Frond(leaves,new V3(x,height,z),leaf*Shapes.Pi*2/7+index*.4f,p.Hall==GardenHall.PalmCourt?1.15f:.62f,.13f+(int)p.Hall*.018f);
                }
            }
            // Built-in wood seats are geometry, independent of carried source furniture.
            if(p.Hall==GardenHall.ReadingGarden||p.Hall==GardenHall.TeaGallery)
            {
                float z=b.MinZ-.42f;
                Box(o.Structure,b.X,z,w,.6f,.38f,.47f,Surface.Accent);
                foreach(int x in new[]{-1,1})Box(o.Structure,b.X+x*w*.35f,z,.1f,.45f,0,.38f,Surface.Metal);
            }
        }
        static void Frond(MeshData mesh,V3 start,float angle,float length,float width)
        {
            var direction=new V3(Shapes.Cos(angle),0,Shapes.Sin(angle));var right=new V3(-direction.Z,0,direction.X);
            const int steps=5;
            for(int i=0;i<steps;i++)
            {
                float t0=(float)i/steps,t1=(float)(i+1)/steps;
                var c0=start+direction*(length*t0)+V3.Up*(.3f*Shapes.Sin(t0*Shapes.Pi)-.22f*t0*t0);
                var c1=start+direction*(length*t1)+V3.Up*(.3f*Shapes.Sin(t1*Shapes.Pi)-.22f*t1*t1);
                // Non-zero tip width avoids degenerate leaf triangles after export.
                float w0=width*(.08f+.92f*Shapes.Sin(t0*Shapes.Pi)),w1=width*(.08f+.92f*Shapes.Sin(t1*Shapes.Pi));
                var a=c0-right*w0;var b=c1-right*w1;var c=c1+right*w1;var d=c0+right*w0;
                var n=V3.Cross(b-a,c-a).Unit;if(n.Y<0)n=n*-1;
                mesh.Triangle(a,b,c,n,n,n,new V2(0,t0),new V2(0,t1),new V2(1,t1),Surface.Slide);
                mesh.Triangle(a,c,d,n,n,n,new V2(0,t0),new V2(1,t1),new V2(1,t0),Surface.Slide);
                mesh.Triangle(a,c,b,n*-1,n*-1,n*-1,new V2(0,t0),new V2(1,t1),new V2(0,t1),Surface.Slide);
                mesh.Triangle(a,d,c,n*-1,n*-1,n*-1,new V2(0,t0),new V2(1,t0),new V2(1,t1),Surface.Slide);
            }
        }
        static void Thresholds(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f;
            for(int side=0;side<4;side++)if(r.HasExit(side))
            {
                float width=Opening(r,side,c),height=DoorHeight(r,side,c);var m=new MeshData();
                foreach(int sign in new[]{-1,1})
                {
                    Shapes.Column(m,sign*(width*.5f+.22f),-.3f,.12f,0,height+.22f,12,Surface.Tile,1.2f);
                    Box(m,sign*(width*.5f+.22f),-.3f,.42f,.42f,height+.15f,height+.29f,Surface.Plaster);
                }
                Box(m,0,-.25f,width+.65f,.2f,height+.3f,height+.44f,Surface.Accent);
                m.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Decoration.Append(m);
            }
        }
    }
}
