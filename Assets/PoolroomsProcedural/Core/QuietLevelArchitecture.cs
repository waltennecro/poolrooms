using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class QuietLevelArchitecture
    {
        static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,s,1.2f);}
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            var plan=room.Plan.Quiet;if(!plan.Valid(room,c))throw new ArgumentException("Invalid quiet level.");
            var output=new RoomGeometry();
            output.Structure.Theme=output.Decoration.Theme=Theme(plan.Environment);output.Glass.Theme=SurfaceTheme.Glass;
            output.Decoration.EmissionTint=new V3(1,.88f,.69f);output.Decoration.EmissionStrength=.5f;
            if(plan.Environment==LiminalEnvironment.Coast48)Coast(output,room,c);
            else
            {
                Box(output.Structure,0,0,room.Width,room.Depth,-.3f,0,Surface.Tile);
                Envelope(output,room,c);
                if(QuietLevelPlanner.IsCivic(plan.Environment))Civic(output,room,c);
                else if(plan.Environment==LiminalEnvironment.Archive906)Archive(output,room,c);else Courtyard(output,room,c);
            }
            Entrances(output,room,c);
            ProceduralArchitecture.Prune(output.Structure);ProceduralArchitecture.Prune(output.Decoration);return output;
        }
        public static SurfaceTheme Theme(LiminalEnvironment e)
        {return e==LiminalEnvironment.RainHouses?SurfaceTheme.RainLodge:e==LiminalEnvironment.AquaticPark?SurfaceTheme.AquaticPark:e==LiminalEnvironment.ThermalBaths?SurfaceTheme.ThermalStone:e==LiminalEnvironment.Orangerie?SurfaceTheme.Orangerie:e==LiminalEnvironment.Archive906?SurfaceTheme.ArchiveWood:e==LiminalEnvironment.Conservatory?SurfaceTheme.Conservatory:e==LiminalEnvironment.TransitGallery?SurfaceTheme.TransitGallery:e==LiminalEnvironment.Courtyard188?SurfaceTheme.CourtyardStone:SurfaceTheme.CoastSand;}
        static void Envelope(RoomGeometry o,Room r,LayoutConfig c)
        {
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();
                if(r.HasExit(side))Shapes.Portal(wall,r.Width,r.Height,0,c.DoorWidth,c.DoorHeight,.28f,12,1.2f);
                else Box(wall,0,0,r.Width,.28f,0,r.Height,Surface.Plaster);
                wall.Indices[(int)Surface.Plaster].AddRange(wall.Indices[(int)Surface.Tile]);wall.Indices[(int)Surface.Tile].Clear();
                wall.TranslateRotate(new V3(Directions.DX[side]*r.Width*.5f,0,Directions.DZ[side]*r.Depth*.5f),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
        }
        static void Civic(RoomGeometry o,Room r,LayoutConfig c)
        {
            bool garden=r.Environment==LiminalEnvironment.Conservatory;float half=r.Width*.5f;
            // The roof is physically cut away for a long daylight clerestory. The glass
            // sits above structural ribs, so the floor is never covered by a hidden slab.
            float aperture=Math.Min(half-3, garden?r.Width*(.29f+(r.Plan.Quiet.Variant%3)*.015f):r.Width*(.21f+(r.Plan.Quiet.Variant%3)*.015f));
            var roof=new List<PlanRect>{new PlanRect(-half,-half,half,half)};
            PlanRect.Subtract(roof,new PlanRect(-aperture,-half+2,aperture,half-2));
            foreach(var p in roof)Box(o.Structure,p.X,p.Z,p.Width,p.Depth,r.Height,r.Height+.2f,Surface.Plaster);
            Box(o.Glass,0,0,aperture*2,r.Depth-4,r.Height+.06f,r.Height+.085f,Surface.Tile);
            for(float z=-half+2;z<=half-2;z+=3.5f)
            {
                Box(o.Decoration,0,z,r.Width,.12f,r.Height-.2f,r.Height,Surface.Metal);
                foreach(int sign in new[]{-1,1})
                {
                    float x=sign*(half-1.1f);
                    Shapes.Column(o.Structure,x,z,.16f,0,r.Height,12,Surface.Metal,1.2f);
                    Box(o.Decoration,x,z,.5f,.36f,r.Height-.55f,r.Height-.5f,Surface.Glow);
                }
            }
            // Broad inlaid routes remain clear even with the largest doors in the profile.
            float aisle=Math.Max(c.DoorWidth+1,4.5f);
            Box(o.Decoration,0,0,aisle,r.Depth,.003f,.009f,Surface.Accent);
            foreach(int sign in new[]{-1,1})Box(o.Decoration,sign*(half+aisle*.5f)*.5f,0,half-aisle*.5f,aisle,.003f,.009f,Surface.Accent);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                float bx=x*half*.79f,bz=z*half*.65f;
                // Recessed planters and built-in seats do not block the central cross.
                Box(o.Structure,bx,bz,1.2f,3.2f,0,.5f,Surface.Plaster);
                if(garden)
                {
                    Box(o.Decoration,bx,bz,1.05f,3.05f,.5f,.52f,Surface.Accent);
                    // Individual curved fronds, shared batched geometry, no collider/Update per leaf.
                    for(int stem=0;stem<7;stem++)
                    {
                        float sz=bz-1.2f+stem*.4f,height=.9f+(r.Plan.Quiet.Seed+(uint)stem*31)%13*.07f;
                        Shapes.Column(o.Decoration,bx,sz,.022f,.52f,height,5,Surface.Accent,1.2f);
                        for(int leaf=0;leaf<6;leaf++)
                        {
                            float a=leaf*Shapes.Pi/3,cs=Shapes.Cos(a),sn=Shapes.Sin(a),y=height-.12f;
                            var start=new V3(bx,y,sz);var tip=new V3(bx+cs*.52f,y+.14f,sz+sn*.52f);
                            var left=new V3(bx+cs*.26f-sn*.12f,y+.23f,sz+sn*.26f+cs*.12f);
                            var right=new V3(bx+cs*.26f+sn*.12f,y+.23f,sz+sn*.26f-cs*.12f);
                            var n=V3.Cross(left-start,tip-start).Unit;if(n.Y<0)n=n*-1;
                            var n2=V3.Cross(tip-start,right-start).Unit;if(n2.Y<0)n2=n2*-1;
                            o.Decoration.Triangle(start,left,tip,n,n,n,new V2(0,0),new V2(.5f,1),new V2(1,0),Surface.Slide);
                            o.Decoration.Triangle(start,tip,right,n2,n2,n2,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Slide);
                            o.Decoration.Triangle(start,left,tip,n*-1,n*-1,n*-1,new V2(0,0),new V2(.5f,1),new V2(1,0),Surface.Slide);
                            o.Decoration.Triangle(start,tip,right,n2*-1,n2*-1,n2*-1,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Slide);
                        }
                    }
                }
                else
                {
                    Box(o.Structure,bx-x*.8f,bz,.6f,3.2f,.05f,.48f,Surface.Accent);
                    Box(o.Structure,bx-x*.55f,bz,.12f,3.2f,.48f,1.1f,Surface.Accent);
                    // Tall timetable case, clock and ticket counter; no fake inaccessible doorway.
                    Box(o.Structure,bx,bz,.18f,2.7f,.5f,2.55f,Surface.Metal);
                    for(int row=0;row<9;row++)Box(o.Decoration,bx-x*.1f,bz,.018f,2.3f,1.15f+row*.13f,1.18f+row*.13f,Surface.Glow);
                }
            }
            o.Decoration.EmissionTint=new V3(1,.94f,.84f);o.Decoration.EmissionStrength=.25f;
            foreach(int sign in new[]{-1,1})
            {
                float z=sign*r.Depth*.24f;
                o.Fixtures.Add(new Fixture{Position=new V3(0,r.Height-.4f,z),Width=aperture*1.8f,Depth=5,Power=9000,Range=r.Width*.75f,CustomColor=true,LightColor=new V3(1,.96f,.9f),SolarFill=true});
                foreach(int side in new[]{-1,1})o.Fixtures.Add(new Fixture{Position=new V3(side*r.Width*.32f,4.5f,z),Width=1.2f,Depth=.4f,Power=1800,Range=12,CustomColor=true,LightColor=o.Decoration.EmissionTint});
            }
        }
        static void Archive(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f,gallery=half-6;
            // Full-width central reading hall; upper galleries have actual stair access.
            foreach(int side in new[]{-1,1})
            {
                Rail(o,side*(gallery-.06f),0,.06f,r.Depth-5,3.2f,4.25f);
                // Stair tower at a side bay, clear of all four doorway approaches.
                float sx=side*(half-3),sz=-half+4.2f;
                var shaft=new PlanRect(sx-2.1f,sz-3.6f,sx+2.1f,sz+3.6f);
                var galleryParts=new List<PlanRect>{new PlanRect(side<0?-half:gallery,-half,side<0?-gallery:half,half)};PlanRect.Subtract(galleryParts,shaft);
                foreach(var part in galleryParts)Box(o.Structure,part.X,part.Z,part.Width,part.Depth,3.05f,3.2f,Surface.Accent);
                Stair(o.Structure,sx,sz,0);
                for(int level=0;level<2;level++)
                {
                    float y=level*3.2f;
                    for(float z=-half+10;z<half-2;z+=3.2f)
                    {
                        var bounds=new PlanRect(side*(half-1.1f)-.6f,z-.4f,side*(half-1.1f)+.6f,z+.4f);
                        var a=new GeneratedAssembly(AssemblyKind.Bookcase,bounds,side<0?1:3,r.Plan.Quiet.Seed+(uint)(level*100+(int)(z+half)));
                        var solid=new MeshData();var detail=new MeshData();AutonomousArchitecture.Build(solid,detail,a,c.CurveSegments);
                        solid.TranslateRotate(new V3(0,y,0),0);detail.TranslateRotate(new V3(0,y,0),0);o.Structure.Append(solid);o.Decoration.Append(detail);
                    }
                    foreach(float z in new[]{-2f,0,2f})
                    {
                        float x=side*(half-3.1f);Box(o.Structure,x,z,1.4f,1.1f,y+.72f,y+.88f,Surface.Accent);
                        foreach(int sign in new[]{-1,1})Box(o.Structure,x+sign*.5f,z,.07f,.75f,y,y+.72f,Surface.Metal);
                    }
                }
            }
            // Bridges preserve a view down the atrium; no continuous low ceiling over the hall.
            foreach(int sign in new[]{-1,1})
            {
                float z=sign*(half-1.2f);Box(o.Structure,0,z,gallery*2,2.2f,3.05f,3.2f,Surface.Accent);
                Rail(o,0,z-sign*1.05f,gallery*2,.06f,3.2f,4.25f);
            }
            // Actual glass roof apertures, with the opaque roof cut around them.
            var roof=new List<PlanRect>{new PlanRect(-half,-half,half,half)};
            foreach(int sign in new[]{-1,1})
            {
                var hole=new PlanRect(-2.4f,sign*half*.38f-2,2.4f,sign*half*.38f+2);PlanRect.Subtract(roof,hole);
                Box(o.Glass,hole.X,hole.Z,hole.Width,hole.Depth,r.Height,r.Height+.025f,Surface.Tile);
                o.Fixtures.Add(new Fixture{Position=new V3(0,r.Height-.15f,hole.Z),Width=4.8f,Depth=4,Power=10000,Range=20,CustomColor=true,LightColor=new V3(1,.96f,.87f)});
            }
            foreach(var part in roof)Box(o.Structure,part.X,part.Z,part.Width,part.Depth,r.Height,r.Height+.22f,Surface.Plaster);
            for(float z=-half+2;z<half;z+=4)
            {Box(o.Decoration,0,z,r.Width,.18f,r.Height-.26f,r.Height-.12f,Surface.Accent);o.Fixtures.Add(new Fixture{Position=new V3(0,6.9f,z),Width=2,Depth=.5f,Power=4200,Range=16,CustomColor=true,LightColor=o.Decoration.EmissionTint});}
            if(r.Plan.Quiet.Variant%2==1)foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {Shapes.Column(o.Structure,x*gallery*.62f,z*half*.62f,.18f,0,r.Height,12,Surface.Accent,1.2f);Shapes.Column(o.Decoration,x*gallery*.62f,z*half*.62f,.25f,0,.2f,12,Surface.Metal,1.2f);}
            if(r.Plan.Quiet.Variant%3==2)foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {Box(o.Structure,x*gallery*.65f,z*half*.3f,2.4f,.8f,0,.48f,Surface.Slide);Box(o.Structure,x*gallery*.65f,z*half*.3f+.32f,2.4f,.12f,.48f,1.05f,Surface.Accent);}
        }
        static void Courtyard(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f,inner=r.Width*.25f,wing=half-inner;
            // Perimeter galleries and separate apartments around an open-air court.
            for(int level=0;level<r.Plan.Quiet.Floors;level++)
            {
                float y=level*3.2f;
                foreach(int side in new[]{-1,1})
                {
                    float x=side*(inner+wing*.5f);
                    if(level>0)
                    {
                        var parts=new List<PlanRect>{new PlanRect(x-wing*.5f,-half,x+wing*.5f,half)};
                        PlanRect.Subtract(parts,new PlanRect(x-2.1f,-half+.4f,x+2.1f,-half+7.6f));
                        foreach(var part in parts)Box(o.Structure,part.X,part.Z,part.Width,part.Depth,y-.16f,y,Surface.Tile);
                    }
                    // At ground level this is continuous flat floor. A railing here
                    // sealed every gallery entrance; only upper voids need guarding.
                    if(level>0)Rail(o,side*(inner+.08f),0,.06f,r.Depth,y,y+1.1f);
                    for(float z=-half+4.3f;z<half-2;z+=7)
                    {
                        if(z< -half+8)continue; // Reserve the first bay for the stair tower and landing.
                        // Side room with a real door facing the gallery, not a facade-only window.
                        var wall=new MeshData();Shapes.Portal(wall,6.6f,3.2f,0,2.1f,2.7f,.18f,10,1.2f);
                        wall.TranslateRotate(new V3(side*(inner+2.2f),y,z),side*Shapes.Pi*.5f);o.Structure.Append(wall);
                        foreach(int sign in new[]{-1,1})Box(o.Structure,side*(inner+(wing+2.2f)*.5f),z+sign*3.3f,Math.Max(.2f,wing-2.2f),.15f,y,y+3.2f,Surface.Plaster);
                        float tableX=side*(half-2.5f);Box(o.Structure,tableX,z,1.6f,.7f,y+.76f,y+.9f,Surface.Accent);
                        Box(o.Structure,tableX,z+.22f,1.6f,.12f,y,y+.76f,Surface.Accent);
                        var bedBounds=new PlanRect(tableX-.825f,z-2.95f,tableX+.825f,z-.75f);
                        var bed=new GeneratedAssembly(AssemblyKind.Bed,bedBounds,0,r.Plan.Quiet.Seed+(uint)(level*100+(int)(z+half)));
                        var mattress=new MeshData();var bedding=new MeshData();AutonomousArchitecture.Build(mattress,bedding,bed,c.CurveSegments);
                        mattress.TranslateRotate(new V3(0,y,0),0);bedding.TranslateRotate(new V3(0,y,0),0);o.Structure.Append(mattress);o.Decoration.Append(bedding);
                        Box(o.Decoration,tableX,z,.5f,.25f,y+2.78f,y+2.82f,Surface.Glow);
                        o.Fixtures.Add(new Fixture{Position=new V3(tableX,y+2.8f,z),Width=.5f,Depth=.25f,Power=1600,Range=6,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                    }
                    if(level<r.Plan.Quiet.Floors-1)Stair(o.Structure,side*(inner+wing*.5f),-half+4, y);
                }
                // A sheltered rear gallery joins the two stair towers on every level.
                if(level>0)Box(o.Structure,0,half-1.1f,inner*2,2.2f,y-.16f,y,Surface.Tile);
                if(level>0)Rail(o,0,half-2.15f,inner*2,.06f,y,y+1.1f);
                o.Fixtures.Add(new Fixture{Position=new V3(0,y+2.7f,half-1.1f),Width=2,Depth=.5f,Power=2300,Range=18,CustomColor=true,LightColor=o.Decoration.EmissionTint});
            }
            // Roof only above the inhabited wings; the central sky remains open.
            foreach(int side in new[]{-1,1})Box(o.Structure,side*(inner+wing*.5f),0,wing,r.Depth,r.Height-.24f,r.Height,Surface.Plaster);
            foreach(int sign in new[]{-1,1})
            {
                Box(o.Structure,sign*inner*.62f,inner*.45f,2.6f,.7f,0,.45f,Surface.Accent);
                Shapes.Column(o.Structure,sign*inner*.7f,-inner*.55f,.55f,0,.65f,12,Surface.Plaster,1.2f);
                Shapes.Column(o.Decoration,sign*inner*.7f,-inner*.55f,.47f,.65f,.7f,12,Surface.Slide,1.2f);
            }
        }
        static void Coast(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f,shore=r.Plan.Quiet.Shore;var sea=r.Plan.Quiet.Sea;
            if(shore> -half+.001f)Box(o.Structure,(-half+shore)*.5f,0,half+shore,r.Depth,-.35f,0,Surface.Tile);
            if(shore<half-.001f)Box(o.Structure,(shore+half)*.5f,0,half-shore,r.Depth,-2.3f,-2,Surface.Tile);
            // A real shallow shore slope joins the sand and deep water.
            if(shore> -half+.001f&&shore<half-.001f)o.Structure.Quad(new V3(shore,0,-half),new V3(shore,0,half),new V3(Math.Min(half,shore+5),-2,half),new V3(Math.Min(half,shore+5),-2,-half),new V3(2,5,0).Unit,Surface.Tile,1.2f);
            if(sea.Width>0){Shapes.Horizontal(o.Water,new V3(shore,sea.Level,-half),new V3(half,sea.Level,-half),new V3(half,sea.Level,half),new V3(shore,sea.Level,half),Surface.Tile,1.2f);o.Region=sea;o.Regions.Add(sea);}
            // A boardwalk keeps all actual door approaches dry and traversable.
            Box(o.Structure,0,0,r.Width,Math.Max(4.5f,c.DoorWidth+.2f),-.12f,.06f,Surface.Accent);
            var boardParts=new List<PlanRect>{new PlanRect(-Math.Max(4.5f,c.DoorWidth+.2f)*.5f,-half,Math.Max(4.5f,c.DoorWidth+.2f)*.5f,half)};
            PlanRect.Subtract(boardParts,new PlanRect(-half,-Math.Max(4.5f,c.DoorWidth+.2f)*.5f,half,Math.Max(4.5f,c.DoorWidth+.2f)*.5f));
            foreach(var part in boardParts)Box(o.Structure,part.X,part.Z,part.Width,part.Depth,-.12f,.06f,Surface.Accent);
            foreach(int side in new[]{-1,1})for(float x=shore+1;x<half-.5f;x+=2.5f)
            {Shapes.Column(o.Decoration,x,side*2.5f,.065f,0,1.05f,6,Surface.Accent,1.2f);Box(o.Decoration,x,side*2.5f,2.5f,.06f,.96f,1.03f,Surface.Accent);}
            // A furnished shaded pavilion, on land and outside the protected cross.
            for(int side=0;side<4;side++)
            {
                long nx=(long)r.X+Directions.DX[side],nz=(long)r.Z+Directions.DZ[side];
                ConnectedTerrainSite terrain;ConnectedMallSite mall;
                bool same=nx>=int.MinValue&&nx<=int.MaxValue&&nz>=int.MinValue&&nz<=int.MaxValue&&
                    !ConnectedTerrainTopology.Reserved(c,(int)nx,(int)nz)&&!ConnectedTerrainTopology.Gateway(c,(int)nx,(int)nz,out terrain)&&!ConnectedMallTopology.Gateway(c,(int)nx,(int)nz,out mall)&&LiminalRegions.At(c,(int)nx,(int)nz)==LiminalEnvironment.Coast48;
                if(same)continue;var wall=new MeshData();
                if(r.HasExit(side))Shapes.Portal(wall,r.Width,Math.Max(5.2f,c.DoorHeight+.4f),0,c.DoorWidth,c.DoorHeight,.24f,12,1.2f);else Box(wall,0,0,r.Width,.24f,-2,2.4f,Surface.Plaster);
                wall.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
            if(!r.Plan.Quiet.Pavilion)return;
            float px=-r.Width*.3f,pz=-r.Depth*.3f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Shapes.Column(o.Structure,px+sx*2,pz+sz*2,.12f,0,3.1f,10,Surface.Accent,1.2f);
            Box(o.Structure,px,pz,4.6f,4.6f,3.1f,3.3f,Surface.Plaster);
            var seat=new SourceFurniturePlacement(8400,5,new V3(px-1.3f,.02f,pz+.3f),90);o.SourceSeats.Add(seat);
            Box(o.Structure,px+.7f,pz+.75f,1.5f,3,.74f,.88f,Surface.Accent);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Box(o.Structure,px+.7f+x*.55f,pz+.75f+z*1.2f,.09f,.09f,0,.74f,Surface.Accent);
        }
        static void Stair(MeshData m,float x,float z,float y)
        {
            for(int half=0;half<2;half++)for(int step=0;step<9;step++)
            {
                float sz=z+(half==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f),top=y+half*1.6f+(step+1)*1.6f/9;
                Box(m,x+(half==0?-.95f:.95f),sz,1.5f,.305f,top-.13f,top,Surface.Accent);
            }
            Box(m,x,z+1.8f,4.2f,.9f,y+1.44f,y+1.6f,Surface.Accent);Box(m,x,z-2,4.2f,1.3f,y+3.04f,y+3.2f,Surface.Accent);
        }
        static void Rail(RoomGeometry o,float x,float z,float w,float d,float bottom,float top)
        {
            Box(o.CollisionOnly,x,z,w,d,bottom,top,Surface.Metal);Box(o.Decoration,x,z,w,d,top-.07f,top,Surface.Metal);
            float length=Math.Max(w,d);int count=Math.Max(1,(int)Math.Ceiling(length/.6f));
            for(int i=0;i<=count;i++)Box(o.Decoration,w>d?x-length*.5f+length*i/count:x,d>=w?z-length*.5f+length*i/count:z,.035f,.035f,bottom,top,Surface.Metal);
        }
        static void Entrances(RoomGeometry o,Room r,LayoutConfig c)
        {
            // Dress the new level's side of an actual graph opening. Existing-zone
            // future sockets remain inactive; there is no teleport or portal plane.
            for(int d=0;d<4;d++)if(r.HasExit(d))
            {
                var frame=new MeshData();float half=c.DoorWidth*.5f,depth=.9f,height=Math.Min(c.DoorHeight+.2f,r.Height-.1f);
                float floor=r.Environment==LiminalEnvironment.Coast48?.068f:.008f;
                Box(frame,0,-depth,c.DoorWidth+.3f,1.6f,floor,floor+.006f,Surface.Accent);
                foreach(int side in new[]{-1,1})
                {
                    if(r.Environment==LiminalEnvironment.Courtyard188)Shapes.Column(frame,side*(half+.24f),-.55f,.1f,0,height,12,Surface.Plaster,1.2f);
                    else Box(frame,side*(half+.16f),-.3f,.12f,.14f,0,height,Surface.Accent);
                }
                Box(frame,0,-.3f,c.DoorWidth+.5f,.16f,height,height+.08f,Surface.Accent);
                if(r.Environment==LiminalEnvironment.Archive906)
                    for(int line=0;line<3;line++)Box(frame,0,-.3f,c.DoorWidth+.75f+line*.12f,.22f,height+.1f+line*.1f,height+.15f+line*.1f,Surface.Accent);
                frame.TranslateRotate(new V3(Directions.DX[d]*r.Width*.5f,0,Directions.DZ[d]*r.Depth*.5f),d*Shapes.Pi*.5f);o.Decoration.Append(frame);
                if(r.Environment!=LiminalEnvironment.Coast48)
                {
                    var local=new V3(0,Math.Min(height-.3f,3.5f),-1.1f);float co=Shapes.Cos(d*Shapes.Pi*.5f),si=Shapes.Sin(d*Shapes.Pi*.5f);
                    o.Fixtures.Add(new Fixture{Position=new V3(Directions.DX[d]*r.Width*.5f+local.X*co+local.Z*si,local.Y,Directions.DZ[d]*r.Depth*.5f-local.X*si+local.Z*co),Width=.65f,Depth=.25f,Power=650,Range=5,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                }
            }
        }
        public static CorridorGeometry Join(Room r,int direction,LayoutConfig c)
        {
            var result=new CorridorGeometry{Style=CorridorStyle.WideGallery};result.Structure.Theme=Theme(r.Environment);
            float z=c.CellSize*.5f,half=c.DoorWidth*.5f;result.Structure.Box(new V3(-half,-.2f,z-.2f),new V3(half,r.Environment==LiminalEnvironment.RainHouses||r.Environment==LiminalEnvironment.Orangerie||r.Environment==LiminalEnvironment.ThermalBaths||r.Environment==LiminalEnvironment.AquaticPark?-.015f:0,z+.2f),Surface.Tile);
            if(direction==1)result.Structure.TranslateRotate(new V3(),Shapes.Pi*.5f);return result;
        }
    }
}
