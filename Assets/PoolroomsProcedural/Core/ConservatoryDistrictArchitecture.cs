using System;
using System.Collections.Generic;
namespace Poolrooms.Core
{
    public static class ConservatoryDistrictArchitecture
    {
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Quiet.Conservatory;if(p==null)throw new ArgumentException("A crystal conservatory plan is required.");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=o.CollisionOnly.Theme=SurfaceTheme.CrystalConservatory;
            o.Glass.Theme=SurfaceTheme.Glass;o.Decoration.EmissionTint=new V3(1,.9f,.72f);o.Decoration.EmissionStrength=.035f;
            var soil=new MeshData{Theme=SurfaceTheme.ConservatorySoil};var foliage=new MeshData{Theme=SurfaceTheme.OrangerieFoliage};
            Foundation(o,p);UpperWalk(o,p);Boundary(o,r,c,p);Roof(o,r,c,p);Stairs(o,p);
            foreach(var bed in p.Beds)Bed(o,soil,foliage,p,bed);
            Programme(o,p);Lighting(o,p);Furnish(o,p);
            if(c.EncounterExploration)Activities(o,p);
            o.VisualMeshes.Add(soil);o.VisualMeshes.Add(foliage);
            foreach(var m in new[]{o.Structure,o.Decoration,o.Glass,o.CollisionOnly,soil,foliage})ProceduralArchitecture.Prune(m);
            return o;
        }
        static void B(MeshData m,float x,float z,float w,float d,float y,float top,Surface slot=Surface.Tile)
        {if(w>.0001f&&d>.0001f&&top>y)ZoneShapes.Box(m,x,z,w,d,y,top,slot,1.2f);}
        static void Foundation(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f;var floor=new List<PlanRect>{new PlanRect(-h,-h,h,h)};
            if(p.Pond.Width>0)PlanRect.Subtract(floor,p.PondBounds);
            foreach(var b in floor)B(o.Structure,b.X,b.Z,b.Width,b.Depth,-.3f,0);
            if(p.Pond.Width<=0)return;
            var w=p.Pond;o.Region=w;o.Regions.Add(w);
            B(o.Structure,w.CenterX,w.CenterZ,w.Width,w.Depth,w.Bottom-.2f,w.Bottom);
            Shapes.Fill(o.Water,Translate(Shapes.RoundedRect(w.Width,w.Depth,0,1),w.CenterX,w.CenterZ),w.Level,Surface.Tile,1.2f);
            // Internal quarter-pond seams have neither lips nor vertical partitions.
            foreach(int side in new[]{0,1,2,3})
            {
                bool alongX=side%2==0;float edge=alongX?w.CenterZ+Directions.DZ[side]*w.Depth*.5f:w.CenterX+Directions.DX[side]*w.Width*.5f;
                if(p.Court&&Math.Abs(Math.Abs(edge)-h)<.01f)continue;
                float x=alongX?w.CenterX:edge,z=alongX?edge:w.CenterZ;
                B(o.Structure,x,z,alongX?w.Width:.14f,alongX?.14f:w.Depth,w.Bottom,.12f,Surface.Tile);
            }
            // Three broad submerged steps allow a walker to leave the shallow basin.
            float sign=p.Court?(p.Column==1?-1:1):-1;
            for(int step=0;step<3;step++)
                B(o.Structure,w.CenterX+sign*(w.Width*.5f-.22f-step*.38f),w.CenterZ, .39f,1.5f,w.Bottom,-step*.215f);
        }
        static V2[] Translate(V2[] path,float x,float z)
        {for(int i=0;i<path.Length;i++)path[i]=new V2(path[i].X+x,path[i].Y+z);return path;}
        static void UpperWalk(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f,inner=h-ConservatoryDistrictPlan.Gallery,bridge=ConservatoryDistrictPlan.Bridge;
            var parts=new List<PlanRect>{new PlanRect(-h,-h,h,h)};
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                var hole=new PlanRect(sx<0?-inner:bridge,sz<0?-inner:bridge,sx<0?-bridge:inner,sz<0?-bridge:inner);
                PlanRect.Subtract(parts,hole);
                Rail(o,hole.MinX,hole.Z,.045f,hole.Depth,p.Upper);Rail(o,hole.MaxX,hole.Z,.045f,hole.Depth,p.Upper);
                Rail(o,hole.X,hole.MinZ,hole.Width,.045f,p.Upper);Rail(o,hole.X,hole.MaxZ,hole.Width,.045f,p.Upper);
            }
            PlanRect.Subtract(parts,p.Stair);
            foreach(var b in parts)B(o.Structure,b.X,b.Z,b.Width,b.Depth,p.Upper-.22f,p.Upper,Surface.Accent);
            Rail(o,p.Stair.MinX,p.Stair.Z,.045f,p.Stair.Depth,p.Upper);Rail(o,p.Stair.MaxX,p.Stair.Z,.045f,p.Stair.Depth,p.Upper);Rail(o,p.Stair.X,p.Stair.MaxZ,p.Stair.Width,.045f,p.Upper);
            // Slim posts meet the floor's underside, not a decorative ring floating beside it.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*(inner+.14f),z=sz*(inner+.14f);
                if(p.Stair.Expanded(.3f).Contains(x,z))continue;
                Shapes.Column(o.Structure,x,z,.11f,0,p.Upper-.22f,10,Surface.Metal,1.2f);
                Shapes.Column(o.Decoration,x,z,.19f,0,.14f,10,Surface.Metal,1.2f);
            }
        }
        static void Rail(RoomGeometry o,float x,float z,float w,float d,float y)
        {
            B(o.CollisionOnly,x,z,w,d,y,y+1.1f,Surface.Metal);
            B(o.Decoration,x,z,w+.025f,d+.025f,y+1.04f,y+1.1f,Surface.Metal);
            int n=Math.Max(1,(int)(Math.Max(w,d)/.32f));
            for(int i=0;i<=n;i++)B(o.Decoration,w>d?x-w*.5f+w*i/n:x,d>=w?z-d*.5f+d*i/n:z,.025f,.025f,y,y+1.04f,Surface.Metal);
        }
        static void Roof(RoomGeometry o,Room room,LayoutConfig cfg,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f;const int n=8;
            for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++)
            {
                float x=-h+ix*p.Cell/n,z=-h+iz*p.Cell/n,xx=x+p.Cell/n,zz=z+p.Cell/n;
                var a=new V3(x,p.RoofY(x,z),z);var b=new V3(xx,p.RoofY(xx,z),z);var c=new V3(xx,p.RoofY(xx,zz),zz);var d=new V3(x,p.RoofY(x,zz),zz);
                var normal=V3.Cross(d-a,b-a).Unit;
                o.Glass.Quad(a,b,c,d,normal,Surface.Tile,1.2f);
                o.CollisionOnly.Quad(a,b,c,d,normal,Surface.Tile,1.2f);
                o.CollisionOnly.Quad(a,d,c,b,normal*-1,Surface.Tile,1.2f);
                // The north/east neighbour owns a shared outer mullion.
                ZoneShapes.Beam(o.Decoration,a,b,.045f,Surface.Metal,1.2f,6);ZoneShapes.Beam(o.Decoration,a,d,.045f,Surface.Metal,1.2f,6);
                if(iz==n-1&&!ConservatoryDistrictPlanner.Same(cfg,room.X,room.Z,0))ZoneShapes.Beam(o.Decoration,d,c,.045f,Surface.Metal,1.2f,6);
                if(ix==n-1&&!ConservatoryDistrictPlanner.Same(cfg,room.X,room.Z,1))ZoneShapes.Beam(o.Decoration,b,c,.045f,Surface.Metal,1.2f,6);
            }
            // Internal columns leave both full-width ground crossing axes clear.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*(h-.42f),z=sz*(h-.42f);
                Shapes.Column(o.Structure,x,z,.095f,0,p.RoofY(x,z),10,Surface.Metal,1.2f);
                Shapes.Column(o.Decoration,x,z,.18f,0,.18f,10,Surface.Tile,1.2f);
            }
        }
        static void Boundary(RoomGeometry o,Room r,LayoutConfig c,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f;
            for(int side=0;side<4;side++)
            {
                if(ConservatoryDistrictPlanner.Same(c,r.X,r.Z,side))continue;
                // A low masonry plinth and real glazing close finite/external edges.
                int panes=Math.Max(8,(int)(p.Cell/2.5f));var cuts=new List<float>();for(int i=0;i<=panes;i++)cuts.Add(-h+i*p.Cell/panes);
                if(r.HasExit(side)){cuts.Add(-c.DoorWidth*.5f);cuts.Add(c.DoorWidth*.5f);}cuts.Sort();
                for(int i=0;i<cuts.Count-1;i++)
                {
                    float a=cuts[i],b=cuts[i+1];if(b-a<.001f)continue;
                    bool entry=r.HasExit(side)&&Math.Abs((a+b)*.5f)<c.DoorWidth*.5f;
                    V3 Point(float along,float y){return new V3(along*Shapes.Cos(side*Shapes.Pi*.5f)+Directions.DX[side]*(h-.05f),y,-along*Shapes.Sin(side*Shapes.Pi*.5f)+Directions.DZ[side]*(h-.05f));}
                    var aa=Point(a,0);var bb=Point(b,0);float ya=p.RoofY(aa.X,aa.Z),yb=p.RoofY(bb.X,bb.Z),bottom=entry?c.DoorHeight:.75f;
                    var n=new V3(-Directions.DX[side],0,-Directions.DZ[side]);
                    o.Glass.Quad(Point(a,bottom),Point(b,bottom),Point(b,yb),Point(a,ya),n,Surface.Tile,1.2f);
                    o.CollisionOnly.Quad(Point(a,bottom),Point(a,ya),Point(b,yb),Point(b,bottom),n,Surface.Tile,1.2f);
                    o.CollisionOnly.Quad(Point(a,bottom),Point(b,bottom),Point(b,yb),Point(a,ya),n*-1,Surface.Tile,1.2f);
                    if(!entry)
                    {
                        var plinth=new MeshData();B(plinth,(a+b)*.5f,0,b-a,.16f,0,.75f,Surface.Plaster);plinth.TranslateRotate(new V3(Directions.DX[side]*(h-.08f),0,Directions.DZ[side]*(h-.08f)),side*Shapes.Pi*.5f);o.Structure.Append(plinth);
                    }
                    ZoneShapes.Beam(o.Decoration,Point(a,bottom),Point(a,ya),.045f,Surface.Metal,1.2f,6);
                    ZoneShapes.Beam(o.Decoration,Point(a,ya),Point(b,yb),.055f,Surface.Metal,1.2f,6);
                    if(entry)ZoneShapes.Beam(o.Decoration,Point(a,bottom),Point(b,bottom),.055f,Surface.Metal,1.2f,6);
                }
            }
        }
        static void Bed(RoomGeometry o,MeshData soil,MeshData foliage,ConservatoryDistrictPlan p,ConservatoryBed b)
        {
            const int n=32;var path=new List<V2>();var inner=new List<V2>();
            for(int i=0;i<n;i++)
            {
                float a=i*Shapes.Pi*2/n;
                path.Add(new V2(b.X+Shapes.Cos(a)*b.Width*.5f,b.Z+Shapes.Sin(a)*b.Depth*.5f));
                inner.Add(new V2(b.X+Shapes.Cos(a)*(b.Width*.5f-.12f),b.Z+Shapes.Sin(a)*(b.Depth*.5f-.12f)));
            }
            Shapes.Skirt(o.Structure,path.ToArray(),0,b.Height,Surface.Tile,1.2f,false,true);
            Shapes.Fill(o.Structure,path.ToArray(),b.Height,Surface.Tile,1.2f);
            Shapes.Fill(soil,inner.ToArray(),b.Height+.006f,Surface.Tile,1.2f);
            for(int i=0;i<n;i++)ZoneShapes.Beam(o.Decoration,new V3(path[i].X,b.Height+.045f,path[i].Y),new V3(path[(i+1)%n].X,b.Height+.045f,path[(i+1)%n].Y),.055f,Surface.Tile,1.2f,6);
            float scale=Math.Min(1.15f,Math.Min(b.Width,b.Depth)/3.4f);
            ConservatoryPlants.Plant(o,foliage,b.X,b.Z,b.Height+.01f,b.Species,p.Seed+(uint)b.Species*37,scale);
            // A low understorey makes the ground read as a garden rather than a solitary prop.
            for(int i=0;i<8;i++)
            {
                float a=i*Shapes.Pi*.25f;
                ConservatoryPlants.Plant(o,foliage,b.X+Shapes.Cos(a)*b.Width*.3f,b.Z+Shapes.Sin(a)*b.Depth*.3f,b.Height+.01f,1,p.Seed+(uint)i*43,.32f);
            }
        }
        static void Programme(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f,x=h-1.35f,z=h*.55f;
            if(p.Use==ConservatoryUse.SeedNursery||p.Use==ConservatoryUse.OrchidHouse||p.Use==ConservatoryUse.Herbarium)
            {
                // Long potting bench, real trays, labelled shelf levels and drying rail.
                Table(o,x,z,0,.95f,p.Cell*.22f,.9f);
                int trays=Math.Max(3,(int)(p.Cell*.22f/.7f));
                for(int i=0;i<trays;i++)
                {
                    float zz=z-p.Cell*.11f+(i+.5f)*p.Cell*.22f/trays;
                    B(o.Decoration,x,zz,.66f,.54f,.901f,.94f,Surface.Slide);
                    foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Shapes.Column(o.Decoration,x+sx*.17f,zz+sz*.14f,.055f,.94f,1.04f,8,Surface.Slide,1.2f);
                }
                foreach(int sign in new[]{-1,1})B(o.Structure,x+.34f,z+sign*p.Cell*.11f,.055f,.055f,0,2.3f,Surface.Metal);
                B(o.Structure,x+.34f,z,.055f,p.Cell*.22f,2.25f,2.3f,Surface.Metal);
                for(int i=0;i<trays;i++)B(o.Decoration,x+.31f,z-p.Cell*.1f+i*p.Cell*.2f/trays,.05f,.04f,1.8f,2.23f,Surface.Accent);
            }
            else if(p.Use==ConservatoryUse.TeaPavilion)
            {
                float cx=(p.PavilionEast?1:-1)*h*.55f,cz=h*.55f,radius=Math.Min(3.0f,p.Cell*.08f);
                for(int i=0;i<6;i++)
                {
                    float a=i*Shapes.Pi/3,xx=cx+Shapes.Cos(a)*radius,zz=cz+Shapes.Sin(a)*radius;
                    Shapes.Column(o.Structure,xx,zz,.075f,0,3.25f,10,Surface.Accent,1.2f);
                    var tip=new V3(cx,3.85f,cz);var aa=new V3(xx,3.25f,zz);var bb=new V3(cx+Shapes.Cos(a+Shapes.Pi/3)*radius,3.25f,cz+Shapes.Sin(a+Shapes.Pi/3)*radius);
                    o.Decoration.Triangle(aa,bb,tip,V3.Up,V3.Up,V3.Up,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Accent);
                    o.Decoration.Triangle(tip,bb,aa,V3.Up*-1,V3.Up*-1,V3.Up*-1,new V2(.5f,1),new V2(1,0),new V2(0,0),Surface.Accent);
                }
            }
            else if(p.Use==ConservatoryUse.RockGarden)
            {
                foreach(var bed in p.Beds)
                {
                    for(int tier=0;tier<3;tier++)B(o.Structure,bed.X,bed.Z,bed.Width*(.65f-tier*.12f),bed.Depth*(.65f-tier*.12f),bed.Height+tier*.19f,bed.Height+(tier+1)*.19f,Surface.Plaster);
                }
            }
            else if(p.Use==ConservatoryUse.PalmGrove||p.Use==ConservatoryUse.FernGarden)
            {
                // A pergola frames the long gallery and has real supporting posts.
                float xx=h-1.4f;
                foreach(int sign in new[]{-1,1})B(o.Structure,xx,sign*h*.67f,.08f,.08f,0,3.2f,Surface.Accent);
                B(o.Structure,xx,0,.1f,h*1.34f,3.1f,3.2f,Surface.Accent);
                for(int i=-3;i<=3;i++)B(o.Decoration,xx,i*h*.2f,1.5f,.07f,3.2f,3.26f,Surface.Accent);
            }
        }
        static void Table(RoomGeometry o,float x,float z,float y,float width,float depth,float height)
        {
            B(o.Structure,x,z,width,depth,y+height-.065f,y+height,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})B(o.Structure,x+sx*(width*.5f-.1f),z+sz*(depth*.5f-.1f),.07f,.07f,y,y+height-.065f,Surface.Metal);
        }
        static void Furnish(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f,x=h-1.6f,z=-h*.4f;
            if(p.Use==ConservatoryUse.TeaPavilion){x=(p.PavilionEast?1:-1)*h*.55f;z=h*.55f;}
            Source(o,7,x,z,.006f);Source(o,8,x,z-1.05f,.006f,0);Source(o,8,x,z+1.05f,.006f,180);
            float top=.006f+SourceFurniture.Dimensions(7).Y+.006f;
            Source(o,11,x-.16f,z,top);Source(o,0,x+.16f,z,top);
            Source(o,3,-h*.35f,h-2,p.Upper+.006f,0);
            Source(o,9,-h+1.4f,(p.StairNorth?-1:1)*h*.62f,.006f);
            Source(o,35,h-1.7f,(p.StairEast&&!p.StairNorth?1:-1)*h*.72f,p.Upper+.006f);
        }
        static void Source(RoomGeometry o,int source,float x,float z,float y,float yaw=0)
        {
            if(o.SourceProps.Count+o.SourceSeats.Count>=28)return;
            var at=new SourceFurniturePlacement(720000+o.SourceProps.Count+o.SourceSeats.Count,source,new V3(x,y,z),yaw);
            if(!SourceFurniture.Clear(o.Structure,at,.005f)||!SourceFurniture.Clear(o.CollisionOnly,at,.005f))return;
            var half=SourceFurniture.HalfExtents(source,yaw);
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
            {
                var foot=new V3(x+dx*half.X,y-.006f,z+dz*half.Z);bool supported=RegionalSourcePlanner.Supported(o.Structure,foot);
                foreach(var previous in o.SourceProps)if(previous.Source==7&&Math.Abs(previous.Bottom.Y+SourceFurniture.Dimensions(7).Y-foot.Y)<.002f&&Math.Abs(foot.X-previous.Bottom.X)<.45f&&Math.Abs(foot.Z-previous.Bottom.Z)<.45f)supported=true;
                if(!supported)return;
            }
            if(SourceFurniture.IsSeat(source))o.SourceSeats.Add(at);else o.SourceProps.Add(at);
        }
        static void Lighting(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            float h=p.Cell*.5f;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*(h-2.1f),z=sz*h*.43f,y=p.Upper+2.4f;
                B(o.Decoration,x,z,.24f,.24f,y,y+.32f,Surface.Metal);B(o.Decoration,x,z,.15f,.15f,y+.04f,y+.27f,Surface.Glow);
                ZoneShapes.Beam(o.Decoration,new V3(x,y+.32f,z),new V3(x,p.RoofY(x,z)-.1f,z),.015f,Surface.Metal,1.2f,6);
                o.Fixtures.Add(new Fixture{Position=new V3(x,y+.12f,z),Width=.15f,Depth=.15f,Power=600,Range=7,Temperature=3100});
            }
        }
        public static void Stairs(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            var b=p.Stair;float start=b.MinZ+ConservatoryDistrictPlan.Landing,end=start+ConservatoryDistrictPlan.Flight;
            int steps=p.Steps;float tread=ConservatoryDistrictPlan.Flight/steps;
            for(int flight=0;flight<2;flight++)
            {
                float x=b.X+(flight==0?-.9f:.9f);
                for(int step=0;step<steps;step++)
                {
                    float z=flight==0?start+(step+.5f)*tread:end-(step+.5f)*tread;
                    float y=flight*p.Upper*.5f+(step+1)*p.Upper/(steps*2);
                    B(o.Structure,x,z,1.62f,tread+.002f,y-.14f,y,Surface.Accent);
                    B(o.Decoration,x,z+(flight==0?-1:1)*(tread*.5f-.025f),1.6f,.035f,y+.001f,y+.006f,Surface.Metal);
                }
                foreach(int sign in new[]{-1,1})
                {
                    float rx=x+sign*.79f;var a=new V3(rx,flight*p.Upper*.5f,flight==0?start:end);var bb=new V3(rx,(flight+1)*p.Upper*.5f,flight==0?end:start);
                    ZoneShapes.Beam(o.Decoration,a+V3.Up*1.04f,bb+V3.Up*1.04f,.035f,Surface.Metal,1.2f,8);
                    o.CollisionOnly.Quad(a,bb,bb+V3.Up*1.04f,a+V3.Up*1.04f,new V3(sign,0,0),Surface.Metal,1.2f);
                    o.CollisionOnly.Quad(a,a+V3.Up*1.04f,bb+V3.Up*1.04f,bb,new V3(-sign,0,0),Surface.Metal,1.2f);
                    for(int i=0;i<=steps;i+=2)
                    {
                        float z=flight==0?start+i*tread:end-i*tread,by=flight*p.Upper*.5f+i*p.Upper/(steps*2);
                        B(o.Decoration,rx,z,.025f,.025f,by,by+1.04f,Surface.Metal);
                    }
                }
            }
            B(o.Structure,b.X,(end+b.MaxZ)*.5f,b.Width,b.MaxZ-end,p.Upper*.5f-.16f,p.Upper*.5f,Surface.Accent);
            B(o.Structure,b.X,(b.MinZ+start)*.5f,b.Width,start-b.MinZ,p.Upper-.16f,p.Upper,Surface.Accent);
        }
        static void Activities(RoomGeometry o,ConservatoryDistrictPlan p)
        {
            var life=new RoomLifePlan{Vignette=new RoomVignette(p.Court?"La rotonda de los esquejes":Name(p.Use),
                Memory(p.Use),RoomActivity.Plant,(int)(p.Seed%144))};
            float h=p.Cell*.5f;
            life.Notice=Panel(o,p.Aisle+.8f,-p.Cell*.12f,0);
            life.Stations.Add(new StationPlacement(Panel(o,-p.Aisle-.8f,-p.Cell*.12f,0),p.Use==ConservatoryUse.Herbarium?StationKind.Herbarium:p.Use==ConservatoryUse.TeaPavilion?StationKind.Tea:StationKind.SeedNursery,7200,0));
            life.Stations.Add(new StationPlacement(Panel(o,h-2.0f,-h*.3f,p.Upper),p.Use==ConservatoryUse.WaterGarden?StationKind.WaterChannels:StationKind.Sundial,7201,1));
            life.Stations.Add(new StationPlacement(Panel(o,h*.35f,h-2,p.Upper),StationKind.Sketch,7202,2));
            life.Furniture.Add(new LifePlacement(LifeFurnishing.Cabinet,-1,7203,new V3(-h+1.4f,.006f,(p.StairNorth&&!p.StairEast?-1:1)*h*.36f),90));
            o.Life=life;
        }
        static string Memory(ConservatoryUse use)
        {
            switch(use)
            {
                case ConservatoryUse.PalmGrove:return "Una libreta conserva las alturas de las palmeras. La última medida es de esta mañana; alguien ha dejado espacio para que tú anotes la siguiente. La pasarela sigue abierta sobre las copas.";
                case ConservatoryUse.FernGarden:return "Los helechos tienen etiquetas escritas a mano. Una nota pide dejar un esqueje para quien llegue después y cuenta que las bandejas pequeñas ya están listas.";
                case ConservatoryUse.RockGarden:return "Entre las piedras hay plantas que tardaron años en florecer. Una tarjeta reserva el banco para una visita que viene cada primavera; las herramientas siguen guardadas en su armario.";
                case ConservatoryUse.OrchidHouse:return "Hay bandejas de flores ordenadas por la fecha de su primera floración. La última etiqueta dice: para la mesa de bienvenida. Alguien ha estado preparando el próximo encuentro.";
                case ConservatoryUse.TeaPavilion:return "La mesa tiene dos tazas. El cuaderno del pabellón recomienda una infusión suave y pide dejar las sillas juntas cuando refresque. La puerta hacia los otros jardines sigue abierta.";
                case ConservatoryUse.SeedNursery:return "Las semillas están contadas en pequeñas bandejas. Una lista reparte las tareas de mañana: regar, trasplantar, preparar otras macetas. Queda una línea en blanco para tu nombre.";
                case ConservatoryUse.Herbarium:return "Los ejemplares llevan fechas, iniciales y recuerdos de distintos caminos. Hay hojas preparadas para prensar la próxima muestra y una invitación a volver cuando esté seca.";
                default:return "La marca junto al estanque señala el nivel que debe mantener el agua. Una nota agradece cuidar el agua para quienes aún no han llegado; arriba dejaron un lugar para dibujar el jardín.";
            }
        }
        public static string Name(ConservatoryUse use)
        {
            switch(use){case ConservatoryUse.PalmGrove:return "La nave de las palmeras";case ConservatoryUse.FernGarden:return "El jardín de helechos";case ConservatoryUse.RockGarden:return "Las terrazas minerales";case ConservatoryUse.OrchidHouse:return "La casa de las flores";case ConservatoryUse.TeaPavilion:return "El pabellón del té";case ConservatoryUse.SeedNursery:return "El vivero de semillas";case ConservatoryUse.Herbarium:return "El herbario compartido";default:return "El jardín del agua";}
        }
        static DressingAnchor Panel(RoomGeometry o,float x,float z,float y)
        {
            B(o.Structure,x,z,1.4f,.1f,y+.75f,y+1.9f,Surface.Accent);B(o.Structure,x,z,.1f,.1f,y,y+.75f,Surface.Metal);
            var a=new V3(x-.68f,y+.78f,z-.052f);var b=new V3(x+.68f,y+.78f,z-.052f);var c=new V3(x-.68f,y+1.87f,z-.052f);
            return new DressingAnchor(new V3(x-.36f,y+1.15f,z-.052f),new V3(0,0,-1),a,c,b,0,0);
        }
    }
}
