using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Geometry is authored in metres, not a different furnishing recipe inside the
    // old square-room envelope. Shared courtyards have no inter-chunk partitions.
    public static class CourtyardDistrictArchitecture
    {
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Quiet.Courtyard;
            if(p==null)throw new ArgumentException("A hotel district plan is required.");
            var o=new RoomGeometry();
            o.Structure.Theme=o.Decoration.Theme=o.CollisionOnly.Theme=SurfaceTheme.CourtyardStone;
            o.Glass.Theme=SurfaceTheme.Glass;o.Decoration.EmissionTint=new V3(1,.88f,.72f);o.Decoration.EmissionStrength=.035f;
            Box(o.Structure,0,0,p.Cell,p.Cell,-.3f,0,Surface.Tile);
            if(p.Court)Garden(o,p);
            else
            {
                Floors(o,p);
                Facades(o,r,p,c);
                var edges=new HashSet<string>();var mirrors=new MeshData{Theme=SurfaceTheme.SpecialMirror};
                foreach(var suite in p.Suites)Suite(o,p,suite,edges,mirrors);
                if(mirrors.TriangleCount>0)o.VisualMeshes.Add(mirrors);
                if(p.StairCore)Stairs(o,p);
                Corridors(o,p);
                Roof(o,r,p,c);
            }
            if(c.EncounterExploration)Activities(o,p);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);
            foreach(var mesh in o.VisualMeshes)ProceduralArchitecture.Prune(mesh);
            return o;
        }
        static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {if(w>.0001f&&d>.0001f&&top>bottom)ZoneShapes.Box(m,x,z,w,d,bottom,top,s,1.2f);}
        static void Rounded(MeshData m,float x,float z,float w,float d,float radius,float bottom,float top,Surface s)
        {
            var mesh=new MeshData();var path=Shapes.RoundedRect(w,d,radius,5);
            Shapes.Fill(mesh,path,top,s,.7f);Shapes.Fill(mesh,path,bottom,s,.7f,false);Shapes.Skirt(mesh,path,bottom,top,s,.7f,false,true);
            mesh.TranslateRotate(new V3(x,0,z),0);m.Append(mesh);
        }
        static void Floors(RoomGeometry o,CourtyardDistrictPlan p)
        {
            float h=p.Cell*.5f;
            for(int f=1;f<=CourtyardDistrictPlan.Floors;f++)
            {
                float y=f==CourtyardDistrictPlan.Floors?p.Roof:p.FloorY(f);
                var pieces=new List<PlanRect>{new PlanRect(-h,-h,h,h)};
                if(p.StairCore)PlanRect.Subtract(pieces,p.Stair);
                foreach(var b in pieces)Box(o.Structure,b.X,b.Z,b.Width,b.Depth,y-.2f,y,Surface.Tile);
            }
        }
        static void LocalWall(RoomGeometry o,MeshData wall,float x,float z,float y,float yaw)
        {wall.TranslateRotate(new V3(x,y,z),yaw*Shapes.Pi/180);o.Structure.Append(wall);}
        static void Portal(RoomGeometry o,float x,float z,float y,float yaw,float width,float height,float doorWidth=1.15f,float doorHeight=2.35f)
        {
            var wall=new MeshData();Shapes.Portal(wall,width,height,0,doorWidth,doorHeight,.16f,8,1.2f);LocalWall(o,wall,x,z,y,yaw);
            var trim=new MeshData();
            foreach(int s in new[]{-1,1})Box(trim,s*(doorWidth*.5f+.055f),0,.09f,.21f,0,doorHeight+.09f,Surface.Accent);
            Box(trim,0,0,doorWidth+.2f,.21f,doorHeight,doorHeight+.09f,Surface.Accent);
            trim.TranslateRotate(new V3(x,y,z),yaw*Shapes.Pi/180);o.Decoration.Append(trim);
        }
        public static IEnumerable<CourtyardOpening> Openings(Room room,LayoutConfig c)
        {
            var p=room.Plan?.Quiet?.Courtyard;if(p==null||p.Court)yield break;
            foreach(var s in p.Suites)
            {
                bool ns=s.DoorSide%2==0;float sign=s.DoorSide==0||s.DoorSide==1?1:-1;
                yield return new CourtyardOpening(new V3(ns?s.Bounds.X:sign>0?s.Bounds.MaxX:s.Bounds.MinX,p.FloorY(s.Floor),ns?sign>0?s.Bounds.MaxZ:s.Bounds.MinZ:s.Bounds.Z),1.15f,2.35f,s.DoorSide*90,true,false,s.Number);
            }
            for(int side=0;side<4;side++)if(CourtyardDistrictPlanner.CourtNeighbour(p,side))
            {
                int count=Math.Max(3,(int)(p.Cell/3.6f));float bay=p.Cell/count;
                for(int f=0;f<CourtyardDistrictPlan.Floors;f++)for(int i=0;i<count;i++)
                {
                    float along=-p.Cell*.5f+(i+.5f)*bay;
                    if(f==0&&Math.Abs(along)<c.DoorWidth*.5f+bay*.5f)continue;
                    float angle=side*Shapes.Pi*.5f;
                    yield return new CourtyardOpening(new V3(along*Shapes.Cos(angle)+Directions.DX[side]*(p.Cell*.5f-.13f),p.FloorY(f)+1.05f,-along*Shapes.Sin(angle)+Directions.DZ[side]*(p.Cell*.5f-.13f)),Math.Min(2.05f,bay-.42f),1.75f,side*90,false,true);
                }
            }
        }
        static void Facades(RoomGeometry o,Room r,CourtyardDistrictPlan p,LayoutConfig c)
        {
            float half=p.Cell*.5f;
            for(int side=0;side<4;side++)
            {
                bool court=CourtyardDistrictPlanner.CourtNeighbour(p,side),same=CourtyardDistrictPlanner.Same(c,r.X,r.Z,side);
                for(int f=0;f<CourtyardDistrictPlan.Floors;f++)
                {
                    float y=p.FloorY(f),height=p.Rise(f);var wall=new MeshData();
                    if(court)WindowWall(wall,o,p,c,side,f);
                    else if(f==0&&r.HasExit(side)||same)Shapes.Portal(wall,p.Cell,height,0,f==0?c.DoorWidth:2.6f,f==0?Math.Min(c.DoorHeight,height-.15f):2.5f,.24f,10,1.2f);
                    else Box(wall,0,0,p.Cell,.24f,0,height,Surface.Plaster);
                    wall.TranslateRotate(new V3(Directions.DX[side]*(half-.12f),y,Directions.DZ[side]*(half-.12f)),side*Shapes.Pi*.5f);o.Structure.Append(wall);
                    // Floor-by-floor brick/stone string courses, sill returns and
                    // gutters make the elevation legible without emissive facades.
                    var detail=new MeshData();Box(detail,0,-.145f,p.Cell,.07f,height-.17f,height-.08f,Surface.Accent);
                    detail.TranslateRotate(new V3(Directions.DX[side]*(half-.12f),y,Directions.DZ[side]*(half-.12f)),side*Shapes.Pi*.5f);o.Decoration.Append(detail);
                }
            }
            foreach(var w in Openings(r,c))if(!w.Door)
            {
                var glass=new MeshData();Box(glass,0,0,w.Width-.08f,.018f,0,w.Height,Surface.Tile);
                glass.TranslateRotate(w.Center,w.Yaw*Shapes.Pi/180);o.Glass.Append(glass);
                var stop=new MeshData();Box(stop,0,0,w.Width,.045f,0,w.Height,Surface.Tile);
                stop.TranslateRotate(w.Center,w.Yaw*Shapes.Pi/180);o.CollisionOnly.Append(stop);
            }
        }
        static void WindowWall(MeshData wall,RoomGeometry o,CourtyardDistrictPlan p,LayoutConfig c,int side,int floor)
        {
            int count=Math.Max(3,(int)(p.Cell/3.6f));float bay=p.Cell/count,height=p.Rise(floor);
            for(int i=0;i<count;i++)
            {
                float x=-p.Cell*.5f+(i+.5f)*bay;
                bool entry=floor==0&&Math.Abs(x)<c.DoorWidth*.5f+bay*.5f;
                if(entry)
                {
                    // Subtract one continuous full-width doorway from all bays it crosses.
                    float a=Math.Max(-p.Cell*.5f+i*bay,-c.DoorWidth*.5f),b=Math.Min(-p.Cell*.5f+(i+1)*bay,c.DoorWidth*.5f);
                    float l=-p.Cell*.5f+i*bay,rr=l+bay;
                    if(a>l)Box(wall,(l+a)*.5f,0,a-l,.24f,0,height,Surface.Plaster);
                    if(rr>b)Box(wall,(b+rr)*.5f,0,rr-b,.24f,0,height,Surface.Plaster);
                    if(b>a)Box(wall,(a+b)*.5f,0,b-a,.24f,c.DoorHeight,height,Surface.Plaster);
                    continue;
                }
                float width=Math.Min(2.05f,bay-.42f),sill=1.05f,top=sill+1.75f;
                Box(wall,x,0,bay,.24f,0,sill,Surface.Plaster);Box(wall,x,0,bay,.24f,top,height,Surface.Plaster);
                foreach(int sign in new[]{-1,1})Box(wall,x+sign*(width*.5f+(bay-width)*.25f),0,(bay-width)*.5f,.24f,sill,top,Surface.Plaster);
                var frame=new MeshData();
                foreach(int sign in new[]{-1,1})Box(frame,x+sign*(width*.5f-.035f),-.005f,.07f,.3f,sill,top,Surface.Metal);
                foreach(float sy in new[]{sill,top})Box(frame,x,-.015f,width,.33f,sy-.04f,sy+.04f,Surface.Metal);
                Box(frame,x,0,.045f,.25f,sill,top,Surface.Metal);
                Box(frame,x,-.05f,width+.16f,.4f,sill-.08f,sill-.025f,Surface.Tile);
                frame.TranslateRotate(new V3(Directions.DX[side]*(p.Cell*.5f-.12f),p.FloorY(floor),Directions.DZ[side]*(p.Cell*.5f-.12f)),side*Shapes.Pi*.5f);o.Decoration.Append(frame);
            }
        }
        static void Suite(RoomGeometry o,CourtyardDistrictPlan p,CourtyardSuite suite,HashSet<string> edges,MeshData mirrors)
        {
            var b=suite.Bounds;float y=p.FloorY(suite.Floor),height=p.Rise(suite.Floor);bool horizontal=suite.DoorSide%2==0;
            // The entry lies along the corridor, all suites have real doors. Shared
            // partitions are emitted once, never overlapping coplanar faces.
            float front=horizontal?(suite.DoorSide==0?b.MaxZ:b.MinZ):(suite.DoorSide==1?b.MaxX:b.MinX);
            Portal(o,horizontal?b.X:front,horizontal?front:b.Z,y,horizontal?0:90,horizontal?b.Width:b.Depth,height);
            foreach(float edge in horizontal?new[]{b.MinX,b.MaxX}:new[]{b.MinZ,b.MaxZ})
            {
                string key=suite.Floor+":"+(horizontal?"X":"Z")+":"+edge.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+(horizontal?b.MinZ:b.MinX).ToString("R",System.Globalization.CultureInfo.InvariantCulture);
                if(!edges.Add(key))continue;
                Box(o.Structure,horizontal?edge:b.X,horizontal?b.Z:edge,horizontal?.14f:b.Width,horizontal?b.Depth:.14f,y,y+height,Surface.Plaster);
            }
            // In-room plan rotates independently from district orientation.
            float usableX=b.Width-.45f,usableZ=b.Depth-.45f;
            float x=b.X,z=b.Z;
            if(suite.Use==CourtyardRoomUse.Guest||suite.Use==CourtyardRoomUse.Twin)
            {
                float bedWidth=Math.Min(suite.Use==CourtyardRoomUse.Twin?1.05f:1.65f,usableX*.43f);
                float bz=z+Math.Min(1.3f,usableZ*.18f);
                Bed(o,x-(suite.Use==CourtyardRoomUse.Twin?.7f:0),bz,y,bedWidth,2.15f);
                if(suite.Use==CourtyardRoomUse.Twin)Bed(o,x+.7f,bz,y,bedWidth,2.15f);
                Table(o,x,b.MinZ+.62f,y,Math.Min(1.6f,usableX-.2f),.6f,.76f);
                AddSource(o,22,x,b.MinZ+1.4f,y,180);
                AddSource(o,18,x-.36f,b.MinZ+.62f,y+.782f);
                AddSource(o,30,x+.35f,b.MinZ+.62f,y+.782f);
                // A recessed wash corner, basin, mirror and separate towel rail.
                float wx=b.MaxX-.64f,wz=b.MaxZ-.64f;
                Box(o.Structure,wx,wz,.8f,.7f,y+.05f,y+.75f,Surface.Accent);
                Rounded(o.Decoration,wx,wz,.75f,.55f,.15f,y+.75f,y+.82f,Surface.Tile);
                Box(o.Decoration,wx,wz+.24f,.045f,.045f,y+.8f,y+1.08f,Surface.Metal);
                Box(o.Decoration,wx,wz+.28f,.18f,.04f,y+1.04f,y+1.08f,Surface.Metal);
                Box(mirrors,wx,b.MaxZ-.14f,.7f,.025f,y+1.2f,y+2,Surface.Metal);
                for(int rail=0;rail<3;rail++)Box(o.Decoration,b.MinX+.2f,z+rail*.24f,.09f,.16f,y+.85f,y+1.25f,Surface.Slide);
            }
            else if(suite.Use==CourtyardRoomUse.Laundry)
            {
                for(int washer=0;washer<Math.Min(3,(int)(usableX/.8f));washer++)
                {
                    float px=b.MinX+.6f+washer*.85f;
                    var appliance=new RoomGeometry();RestfulLevelArchitecture.Washer(appliance,px,b.MaxZ-.7f,.7f);
                    foreach(var part in new[]{appliance.Structure,appliance.Decoration,appliance.Glass})part.TranslateRotate(new V3(0,y,0),0);
                    o.Structure.Append(appliance.Structure);o.Decoration.Append(appliance.Decoration);o.Glass.Append(appliance.Glass);
                    Box(o.Decoration,px,b.MaxZ-1.065f,.42f,.025f,y+.8f,y+.84f,Surface.Metal);
                }
                Table(o,x,z,y,Math.Min(2,usableX-.4f),.8f,.85f);AddSource(o,30,x,z,y+.87f);AddSource(o,33,x+.4f,z,y+.87f);
            }
            else if(suite.Use==CourtyardRoomUse.Reception)
            {
                Table(o,x,z,y,Math.Min(2.5f,usableX-.4f),.85f,1.02f);
                AddSource(o,24,x-.3f,z,y+1.04f);AddSource(o,28,b.MinX+.7f,b.MaxZ-.7f,y);
                AddSource(o,22,x,z+1.1f,y);AddSource(o,35,b.MaxX-.65f,b.MinZ+.7f,y);
                // Individual mail/key cubbies, with actual shelf thickness.
                float w=Math.Min(2,usableX-.3f);Box(o.Structure,x,b.MaxZ-.3f,w,.28f,y+.7f,y+2.25f,Surface.Accent);
                for(int col=0;col<5;col++)for(int row=0;row<4;row++)
                    Box(o.Decoration,x-w*.5f+(col+.5f)*w/5,b.MaxZ-.47f,w/5-.07f,.04f,y+.79f+row*.35f,y+1.04f+row*.35f,Surface.Metal);
            }
            else
            {
                Table(o,x,z,y,1.3f,.8f,.75f);
                AddSource(o,suite.Use==CourtyardRoomUse.Reading?3:8,x-1.18f,z,y,90);
                AddSource(o,8,x+1.15f,z,y,270);AddSource(o,11,x-.4f,z,y+.772f);AddSource(o,11,x+.4f,z,y+.772f);
                AddSource(o,12,x,z+.2f,y+.772f);AddSource(o,34,b.MinX+.6f,b.MaxZ-.3f,y+.08f);
                Box(o.Structure,b.MinX+.6f,b.MaxZ-.3f,.78f,.13f,y+.055f,y+.08f,Surface.Metal);
                foreach(int leg in new[]{-1,1})Box(o.Structure,b.MinX+.6f+leg*.3f,b.MaxZ-.3f,.05f,.13f,y,y+.055f,Surface.Metal);
                if(suite.Use==CourtyardRoomUse.Reading)
                {
                    float width=Math.Min(2,usableX-.4f);Box(o.Structure,x,b.MaxZ-.35f,width,.35f,y,y+.16f,Surface.Accent);
                    for(int shelf=1;shelf<=5;shelf++)Box(o.Structure,x,b.MaxZ-.35f,width,.35f,y+shelf*.34f,y+shelf*.34f+.045f,Surface.Accent);
                    foreach(int s in new[]{-1,1})Box(o.Structure,x+s*(width*.5f-.04f),b.MaxZ-.35f,.08f,.35f,y,y+1.8f,Surface.Accent);
                    for(int book=0;book<22;book++)Box(o.Decoration,x-width*.45f+(book%11)*width*.085f,b.MaxZ-.37f,.06f,.24f,y+.39f+(book/11)*.68f,y+.63f+(book/11)*.68f,book%2==0?Surface.Slide:Surface.Plaster);
                }
            }
            if((suite.Seed&3)==0)AddSource(o,9,b.MaxX-.65f,b.MinZ+.65f,y);
            // Lights belong to fixtures on the same storey, not a fill light high
            // above all floors. The existing world budget chooses active lamps.
            int lightCount=b.Depth>10?2:1;
            for(int light=0;light<lightCount;light++)
            {
                float ly=y+height-.25f,lz=z+(lightCount==1?0:(light==0?-1:1)*b.Depth*.22f);
                Box(o.Decoration,x,lz,.75f,.4f,ly,ly+.035f,Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(x,ly-.04f,lz),Power=1600*height*height/(3.4f*3.4f),Range=6,Width=.75f,Depth=.4f,Temperature=3300,MallLocal=true,FloorOffset=height-.29f,StoreyHeight=height,ShadowlessRange=height+.6f});
            }
        }

        static void AddSource(RoomGeometry o,int source,float x,float z,float y,float yaw=0)
        {
            if(o.SourceProps.Count+o.SourceSeats.Count>=48)return;
            var p=new SourceFurniturePlacement(188000+o.SourceProps.Count+o.SourceSeats.Count,source,new V3(x,y+.008f,z),yaw);
            if(SourceFurniture.IsSeat(source))o.SourceSeats.Add(p);else o.SourceProps.Add(p);
        }
        static void Table(RoomGeometry o,float x,float z,float y,float w,float d,float top)
        {
            Rounded(o.Structure,x,z,w,d,.06f,y+top-.06f,y+top,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(o.Structure,x+sx*(w*.5f-.09f),z+sz*(d*.5f-.09f),.07f,.07f,y+.025f,y+top-.06f,Surface.Metal);
        }
        static void Bed(RoomGeometry o,float x,float z,float y,float w,float d)
        {
            Rounded(o.Structure,x,z,w,d,.09f,y+.12f,y+.34f,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(o.Structure,x+sx*w*.38f,z+sz*d*.36f,.08f,.08f,y+.02f,y+.12f,Surface.Metal);
            Rounded(o.Structure,x,z,w-.035f,d-.035f,.13f,y+.34f,y+.57f,Surface.Slide);
            Rounded(o.Decoration,x,z-.16f,w+.018f,d*.68f,.08f,y+.571f,y+.61f,Surface.Slide);
            for(int seam=0;seam<5;seam++)Box(o.Decoration,x,z-.62f+seam*.24f,w-.1f,.008f,y+.611f,y+.616f,Surface.Plaster);
            Rounded(o.Structure,x,z+d*.48f,w+.08f,.13f,.045f,y+.3f,y+1.12f,Surface.Accent);
            foreach(int sign in new[]{-1,1})Rounded(o.Decoration,x+sign*w*.23f,z+d*.31f,w*.42f,.42f,.13f,y+.58f,y+.72f,Surface.Slide);
            for(int button=0;button<3;button++)Box(o.Decoration,x-w*.28f+button*w*.28f,z+d*.405f,.035f,.02f,y+.83f,y+.87f,Surface.Metal);
        }
        static void Corridors(RoomGeometry o,CourtyardDistrictPlan p)
        {
            for(int f=0;f<CourtyardDistrictPlan.Floors;f++)
            {
                float y=p.FloorY(f),top=y+p.Rise(f);
                foreach(int sign in new[]{-1,1})
                {
                    Box(o.Decoration,0,sign*(p.Aisle-.12f),p.Cell,.06f,y+.025f,y+.07f,Surface.Accent);
                    Box(o.Decoration,sign*(p.Aisle-.12f),0,.06f,p.Cell,y+.025f,y+.07f,Surface.Accent);
                    Box(o.Decoration,sign*p.Cell*.24f,0,.9f,.22f,top-.24f,top-.2f,Surface.Glow);
                    o.Fixtures.Add(new Fixture{Position=new V3(sign*p.Cell*.24f,top-.27f,0),Power=1700*p.Rise(f)*p.Rise(f)/(3.4f*3.4f),Range=7,Width=.9f,Depth=.22f,Temperature=3300,MallLocal=true,FloorOffset=p.Rise(f)-.27f,StoreyHeight=p.Rise(f),ShadowlessRange=p.Rise(f)+.6f});
                }
            }
        }
        public static void Stairs(RoomGeometry o,CourtyardDistrictPlan p)
        {
            var b=p.Stair;float start=b.MinZ+CourtyardDistrictPlan.Landing,end=start+CourtyardDistrictPlan.Flight;
            for(int f=0;f<CourtyardDistrictPlan.Floors;f++)
            {
                float y=p.FloorY(f),rise=p.Rise(f);int steps=p.Steps(f);float tread=CourtyardDistrictPlan.Flight/steps;
                for(int flight=0;flight<2;flight++)
                {
                    float x=b.X+(flight==0?-.9f:.9f);
                    for(int step=0;step<steps;step++)
                    {
                        float z=flight==0?start+(step+.5f)*tread:end-(step+.5f)*tread;
                        float top=y+rise*.5f*flight+(step+1)*rise*.5f/steps;
                        Box(o.Structure,x,z,1.62f,tread+.002f,top-.14f,top,Surface.Tile);
                        Box(o.Decoration,x,z+(flight==0?-1:1)*(tread*.5f-.025f),1.6f,.04f,top+.002f,top+.008f,Surface.Metal);
                    }
                    // Sloped continuous balustrade along each flight, with a
                    // collision-only infill; no invisible ramp under the steps.
                    foreach(int s in new[]{-1,1})
                    {
                        float rx=x+s*.79f;
                        var a=new V3(rx,y+flight*rise*.5f+1.04f,flight==0?start:end);
                        var bb=new V3(rx,y+(flight+1)*rise*.5f+1.04f,flight==0?end:start);
                        ZoneShapes.Beam(o.Decoration,a,bb,.035f,Surface.Metal,1.2f,8);
                        for(int post=0;post<=steps;post+=2)
                        {
                            float z=flight==0?start+post*tread:end-post*tread,baseY=y+flight*rise*.5f+post*rise*.5f/steps;
                            Box(o.Structure,rx,z,.035f,.035f,baseY,baseY+1.04f,Surface.Metal);
                        }
                        float zb=flight==0?start:end,ze=flight==0?end:start;
                        V3 v0=new V3(rx-.025f,y+flight*rise*.5f,zb),v1=new V3(rx-.025f,y+(flight+1)*rise*.5f,ze);
                        o.CollisionOnly.Quad(v0,v1,v1+V3.Up*1.04f,v0+V3.Up*1.04f,new V3(-1,0,0),Surface.Metal,1.2f);
                        o.CollisionOnly.Quad(v0+new V3(.05f,0,0),v0+new V3(.05f,1.04f,0),v1+new V3(.05f,1.04f,0),v1+new V3(.05f,0,0),new V3(1,0,0),Surface.Metal,1.2f);
                    }
                }
                Box(o.Structure,b.X,(end+b.MaxZ)*.5f,b.Width,b.MaxZ-end,y+rise*.5f-.16f,y+rise*.5f,Surface.Tile);
                Box(o.Structure,b.X,(b.MinZ+start)*.5f,b.Width,start-b.MinZ,y+rise-.16f,y+rise,Surface.Tile);
                Rail(o,b.MinX,b.Z,.045f,b.Depth,y+rise,y+rise+1.08f);
                Rail(o,b.MaxX,b.Z,.045f,b.Depth,y+rise,y+rise+1.08f);
                Rail(o,b.X,b.MaxZ,b.Width,.045f,y+rise,y+rise+1.08f);
            }
        }
        static void Rail(RoomGeometry o,float x,float z,float w,float d,float y,float top)
        {
            Box(o.CollisionOnly,x,z,w,d,y,top,Surface.Metal);Box(o.Decoration,x,z,w+.015f,d+.015f,top-.055f,top,Surface.Metal);
            int count=Math.Max(1,(int)(Math.Max(w,d)/.35f));
            for(int i=0;i<=count;i++)Box(o.Decoration,w>d?x-w*.5f+w*i/count:x,d>=w?z-d*.5f+d*i/count:z,.025f,.025f,y,top,Surface.Metal);
        }
        static void Roof(RoomGeometry o,Room r,CourtyardDistrictPlan p,LayoutConfig c)
        {
            float half=p.Cell*.5f;
            for(int side=0;side<4;side++)
            {
                bool same=CourtyardDistrictPlanner.Same(c,r.X,r.Z,side),court=CourtyardDistrictPlanner.CourtNeighbour(p,side);
                if(same&&!court)continue;
                var parapet=new MeshData();Box(parapet,0,0,p.Cell,.25f,p.Roof,p.Roof+1.12f,Surface.Plaster);
                Box(parapet,0,0,p.Cell,.34f,p.Roof+1.12f,p.Roof+1.18f,Surface.Metal);
                parapet.TranslateRotate(new V3(Directions.DX[side]*(half-.13f),0,Directions.DZ[side]*(half-.13f)),side*Shapes.Pi*.5f);o.Structure.Append(parapet);
            }
            // Roof services sit away from the stair hatch and crossing routes.
            foreach(int sign in new[]{-1,1})
            {
                float x=sign*half*.6f,z=half*.6f;
                Rounded(o.Structure,x,z,1.6f,1.1f,.07f,p.Roof,p.Roof+.85f,Surface.Metal);
                for(int grille=0;grille<12;grille++)Box(o.Decoration,x-.65f+grille*.12f,z-.56f,.035f,.02f,p.Roof+.15f,p.Roof+.7f,Surface.Accent);
                Shapes.Column(o.Structure,x+1.1f,z,.12f,p.Roof,p.Roof+1.15f,10,Surface.Metal,1.2f);
            }
        }
        static void Garden(RoomGeometry o,CourtyardDistrictPlan p)
        {
            float half=p.Cell*.5f;int sx=p.Column==1?-1:1,sz=p.Row==1?-1:1;
            // Four lawns, one in each court quadrant. A continuous perimeter walk
            // and a central stone cross meet a wooden circle at the district origin.
            float margin=3.4f;var lawn=new MeshData{Theme=SurfaceTheme.CourtyardLawn};
            float minX=-half+margin,maxX=half-margin,minZ=-half+margin,maxZ=half-margin;
            Box(lawn,(minX+maxX)*.5f,(minZ+maxZ)*.5f,maxX-minX,maxZ-minZ,.002f,.035f,Surface.Tile);
            // Low stone edging is a small traversable step, not a fence.
            foreach(int s in new[]{-1,1})
            {
                Box(o.Structure,s*(half-margin),0,.12f,p.Cell-2*margin,0,.08f,Surface.Tile);
                Box(o.Structure,0,s*(half-margin),p.Cell-2*margin,.12f,0,.08f,Surface.Tile);
            }
            var random=new StableRandom(p.Seed+(uint)(p.Column*17+p.Row*43));
            for(int tuft=0;tuft<160;tuft++)
            {
                float x=random.Range(minX,maxX),z=random.Range(minZ,maxZ),height=random.Range(.05f,.105f),angle=random.Range(0,Shapes.Pi);
                var d=new V3(Shapes.Cos(angle)*.025f,0,Shapes.Sin(angle)*.025f);var a=new V3(x,.036f,z)-d;var b=new V3(x,.036f,z)+d;var tip=new V3(x+.02f,.036f+height,z);
                var n=V3.Cross(b-a,tip-a).Unit;
                lawn.Triangle(a,b,tip,n,n,n,new V2(0,0),new V2(1,0),new V2(.5f,1),Surface.Tile);
                lawn.Triangle(b,a,tip,n*-1,n*-1,n*-1,new V2(1,0),new V2(0,0),new V2(.5f,1),Surface.Tile);
            }
            o.VisualMeshes.Add(lawn);
            // One quarter-disc per owning chunk, clipped exactly at the seam.
            float cx=-sx*half,cz=-sz*half,radius=2.7f;
            for(int wedge=0;wedge<20;wedge++)
            {
                float a=wedge*Shapes.Pi/40,b=(wedge+1)*Shapes.Pi/40;
                var center=new V3(cx,.045f,cz);var p1=new V3(cx+sx*radius*Shapes.Cos(a),.045f,cz+sz*radius*Shapes.Sin(a));var p2=new V3(cx+sx*radius*Shapes.Cos(b),.045f,cz+sz*radius*Shapes.Sin(b));
                o.Decoration.Triangle(center,p1,p2,V3.Up,V3.Up,V3.Up,new V2(0,0),new V2(p1.X/.28f,p1.Z/2),new V2(p2.X/.28f,p2.Z/2),Surface.Accent);
            }
            float bx=sx*(half-1.55f),bz=sz*(half*.36f);
            Bench(o,bx,bz,sz>0?180:0);
            AddSource(o,9,bx,bz+2,0);AddSource(o,35,bx,bz-2,0);
            // Surface drains, no collision trenches or opaque walls at court seams.
            for(int i=0;i<20;i++)Box(o.Decoration,-half+.5f+i*(p.Cell-1)/19,sz*(half-.6f),.018f,.22f,.003f,.007f,Surface.Metal);
            Table(o,sx*(half*.45f),sz*(half-1.7f),0,1.3f,.8f,.75f);
            AddSource(o,8,sx*(half*.45f)-1.05f,sz*(half-1.7f),0,90);
            AddSource(o,11,sx*(half*.45f),sz*(half-1.7f),.775f);
        }
        static void Bench(RoomGeometry o,float x,float z,float yaw)
        {
            var bench=new MeshData();for(int slat=0;slat<5;slat++)
            {Box(bench,0,-.23f+slat*.115f,2,.085f,.42f,.48f,Surface.Accent);Box(bench,0,.25f,2,.07f,.56f+slat*.09f,.62f+slat*.09f,Surface.Accent);}
            foreach(int sign in new[]{-1,1}){Box(bench,sign*.75f,0,.08f,.52f,.02f,.42f,Surface.Metal);Box(bench,sign*.84f,0,.055f,.55f,.65f,.7f,Surface.Metal);}
            bench.TranslateRotate(new V3(x,0,z),yaw*Shapes.Pi/180);o.Structure.Append(bench);
        }
        static void Activities(RoomGeometry o,CourtyardDistrictPlan p)
        {
            var life=new RoomLifePlan{Vignette=new RoomVignette(p.Court?"Las cuatro parcelas del patio":p.StairCore?"La escalera de las cinco plantas":"El hotel de las ventanas",p.Court?
                "El sendero de piedra une cuatro céspedes y un círculo de madera. Alguien dejó la mesa del patio preparada para la próxima visita.":
                "Las habitaciones tienen puertas, mesas de trabajo y luz propia. La lavandería conserva las cestas; en recepción preparan las llaves para quienes todavía no han llegado. Las escaleras de las esquinas llegan hasta la azotea.",RoomActivity.Reading,(int)(p.Seed%144))};
            float x=p.Court?p.Cell*.18f:p.Aisle-.2f,z=-p.Cell*.28f;
            life.Notice=DisplayPanel(o,x,z,p.Court?0:90);
            float ax=p.Court?x+2.1f:-p.Aisle+.2f;
            var stationAnchor=DisplayPanel(o,ax,z,p.Court?0:-90);
            life.Stations.Add(new StationPlacement(stationAnchor,p.Court?StationKind.Postcards:p.StairCore?StationKind.BellSequence:StationKind.Books,1880,(int)(p.Seed%6)));
            o.Life=life;
        }
        static DressingAnchor DisplayPanel(RoomGeometry o,float x,float z,float yaw)
        {
            // Rectangular boards face into the hotel corridor. Their width runs
            // along its wall; it never narrows the through-route to one person.
            var solid=new MeshData();var detail=new MeshData();
            Box(solid,0,0,.1f,.1f,0,1.85f,Surface.Metal);
            Box(solid,0,.04f,1.4f,.08f,.72f,1.88f,Surface.Accent);
            V3 a=new V3(-.675f,.75f,-.01f),b=new V3(.675f,.75f,-.01f),cc=new V3(-.675f,1.85f,-.01f),d=new V3(.675f,1.85f,-.01f);
            var n=new V3(0,0,-1);
            solid.Triangle(a,cc,b,n,n,n,new V2(0,0),new V2(0,1),new V2(1,0),Surface.Slide);
            solid.Triangle(b,cc,d,n,n,n,new V2(1,0),new V2(0,1),new V2(1,1),Surface.Slide);
            for(int floor=0;floor<5;floor++)Box(detail,.35f,-.027f,.36f,.008f,1.02f+floor*.14f,1.04f+floor*.14f,Surface.Metal);
            float angle=yaw*Shapes.Pi/180,co=Shapes.Cos(angle),si=Shapes.Sin(angle);
            solid.TranslateRotate(new V3(x,0,z),angle);detail.TranslateRotate(new V3(x,0,z),angle);o.Structure.Append(solid);o.Decoration.Append(detail);
            V3 Point(V3 p){return new V3(x+p.X*co+p.Z*si,p.Y,z-p.X*si+p.Z*co);}
            return new DressingAnchor(Point(new V3(-.36f,1.12f,-.01f)),new V3(-si,0,-co),Point(a),Point(cc),Point(b),0,0);
        }
    }
}
