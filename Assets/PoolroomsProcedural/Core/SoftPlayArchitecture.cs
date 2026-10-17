using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class SoftPlayArchitecture
    {
        private const float UV=1.2f;
        private static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,s,UV);}
        private static Surface Colour(int n){return n%4==0?Surface.Accent:n%4==1?Surface.Slide:n%4==2?Surface.Metal:Surface.Tile;}
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            var p=room.Plan.SoftPlay;if(!p.Valid(room,c))throw new ArgumentException("Invalid soft-play hall.");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.SoftPlayFinish;
            o.Decoration.EmissionTint=p.Lighting==1?new V3(1,.87f,.68f):p.Lighting==2?new V3(.85f,.91f,1):new V3(1,.97f,.9f);
            o.Decoration.EmissionStrength=.65f;
            o.Structure.EmissionTint=o.Decoration.EmissionTint;o.Structure.EmissionStrength=.65f;
            Box(o.Structure,0,0,room.Width,room.Depth,-.35f,0,Surface.Tile);
            var floor=new MeshData{Theme=SurfaceTheme.SoftPlayFloor};
            Shapes.Horizontal(floor,new V3(-room.Width*.5f,.004f,-room.Depth*.5f),new V3(room.Width*.5f,.004f,-room.Depth*.5f),new V3(room.Width*.5f,.004f,room.Depth*.5f),new V3(-room.Width*.5f,.004f,room.Depth*.5f),Surface.Tile,2.4f);
            // Continue the floor pattern across neighbouring cells without huge float UVs.
            double ox=(double)room.X*c.CellSize/2.4,oz=(double)room.Z*c.CellSize/2.4;
            for(int i=0;i<floor.UV.Count;i++){var uv=floor.UV[i];floor.UV[i]=new V2(uv.X+(float)(ox-Math.Floor(ox)),uv.Y+(float)(oz-Math.Floor(oz)));}
            o.VisualMeshes.Add(floor);
            var net=new MeshData{Theme=SurfaceTheme.SoftPlayNet};o.VisualMeshes.Add(net);
            Shell(o,room,c);
            for(int i=0;i<p.Frames.Length;i++)Frame(o,p.Frames[i],p,i,c,net);
            Bridge(o,p);Slide(o,p.Slide);BallPit(o,p,c.CurveSegments);
            Lounge(o,room,p);Lights(o,room,p);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.CollisionOnly);
            return o;
        }
        private static void Shell(RoomGeometry o,Room room,LayoutConfig c)
        {
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();float span=side%2==0?room.Width:room.Depth;
                if(room.HasExit(side))Shapes.Portal(wall,span,room.Height,0,SoftPlayPlanner.DoorWidth(room,side,c),SolarConnections.DoorHeight(room,side,c),c.WallThickness,8,UV);
                else Box(wall,0,0,span,c.WallThickness,0,room.Height,Surface.Tile);
                // Painted drywall, never the old swimming-pool tile walls.
                wall.Indices[(int)Surface.Plaster].AddRange(wall.Indices[(int)Surface.Tile]);wall.Indices[(int)Surface.Tile].Clear();
                wall.TranslateRotate(new V3(Directions.DX[side]*room.Width*.5f,0,Directions.DZ[side]*room.Depth*.5f),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
            Box(o.Structure,0,0,room.Width,room.Depth,room.Height,room.Height+.2f,Surface.Plaster);
            for(float z=-room.Depth*.5f+3;z<room.Depth*.5f;z+=6)
            {
                Box(o.Decoration,0,z,room.Width,.11f,room.Height-.22f,room.Height-.1f,Surface.Metal);
                if(room.Plan.SoftPlay.RoofStyle==1)for(int sx=-1;sx<=1;sx+=2)
                    ZoneShapes.Beam(o.Decoration,new V3(sx*room.Width*.45f,room.Height-.65f,z),new V3(0,room.Height-.15f,z),.06f,Surface.Metal,UV,4);
            }
            if(room.Plan.SoftPlay.RoofStyle==2)for(float x=-room.Width*.5f+3;x<room.Width*.5f;x+=6)
                Box(o.Decoration,x,0,.1f,room.Depth,room.Height-.33f,room.Height-.23f,Surface.Metal);
        }
        private static void Frame(RoomGeometry o,PlayFramePlan f,SoftPlayPlan p,int index,LayoutConfig c,MeshData net)
        {
            float hx=f.Width*.5f,hz=f.Depth*.5f,top=(f.Levels-1)*3.2f+2.7f;
            var solid=new MeshData();var detail=new MeshData();var safety=new MeshData();var localNet=new MeshData();
            for(int ix=0;ix<=f.Columns;ix++)for(int iz=0;iz<=f.Rows;iz++)
            {
                float x=-hx+ix*4.2f,z=-hz+iz*4.2f;
                Shapes.Column(solid,x,z,.115f,0,top,8,Colour(ix+iz+index),UV);
                // Fabric sleeves and fastening collars sit outside the unchanged structural pole.
                foreach(float y in new[]{.12f,top-.16f})Shapes.Column(detail,x,z,.127f,y,y+.065f,8,Colour(ix+iz+index+1),UV);
            }
            var shaft=new PlanRect(-hx,-hz+4.2f-3.6f,-hx+4.2f,-hz+4.2f+3.6f);
            for(int level=0;level<f.Levels;level++)
            {
                float y=level*3.2f;
                if(level>0)for(int ix=0;ix<f.Columns;ix++)for(int iz=0;iz<f.Rows;iz++)
                {
                    var rect=new PlanRect(-hx+ix*4.2f,-hz+iz*4.2f,-hx+(ix+1)*4.2f,-hz+(iz+1)*4.2f);
                    var parts=new List<PlanRect>{rect};PlanRect.Subtract(parts,shaft);
                    foreach(var q in parts)Box(solid,q.X,q.Z,q.Width,q.Depth,y-.16f,y,Colour(ix+iz+level));
                }
                for(int side=0;side<4;side++)
                {
                    int n=side%2==0?f.Columns:f.Rows;
                    for(int k=0;k<n;k++)
                    {
                        bool north=side==0,south=side==2;
                        bool port=level==0&&(north||south)&&k==f.Columns-1||level==0&&south;
                        port|=level==2&&k==f.Columns-1&&(index==0&&south||index==1&&north);
                        port|=level==1&&index==1&&north&&k==f.Columns-1;
                        float centre=-n*2.1f+(k+.5f)*4.2f;
                        if(side==1||side==2)centre=-centre; // rotation mirrors cell order on east/south faces
                        var panel=new MeshData();var barrier=new MeshData();var threads=new MeshData();
                        if(!port)
                        {
                            Box(panel,centre,0,4.2f,.08f,y+.92f,y+1.04f,Colour(level+k));
                            Box(barrier,centre,0,4.2f,.045f,y+.03f,y+2.65f,Surface.Metal);
                            Net(threads,centre-2.1f,centre+2.1f,y+.12f,y+2.6f,c.CurveSegments<=12?.32f:.18f);
                        }
                        else if(level>0)
                        {
                            // An actual open mouth; rails only on its two side posts.
                            foreach(int sign in new[]{-1,1})Box(panel,centre+sign*2.0f,0,.12f,.12f,y,y+2.6f,Surface.Accent);
                        }
                        // local canonical north face -> outward side
                        panel.TranslateRotate(new V3(Directions.DX[side]*hx,0,Directions.DZ[side]*hz),side*Shapes.Pi*.5f);
                        barrier.TranslateRotate(new V3(Directions.DX[side]*hx,0,Directions.DZ[side]*hz),side*Shapes.Pi*.5f);
                        threads.TranslateRotate(new V3(Directions.DX[side]*hx,0,Directions.DZ[side]*hz),side*Shapes.Pi*.5f);
                        detail.Append(panel);safety.Append(barrier);localNet.Append(threads);
                    }
                }
            }
            Stairs(solid,detail,safety,-hx+2.1f,-hz+4.2f,f.Levels);
            // Foam obstacles stay in side bays; the stair landing and central upper route remain clear.
            for(int level=0;level<f.Levels;level++)
            {
                float y=level*3.2f;
                Box(detail,hx-2.1f,hz-1.0f,1.0f,.32f,y+.02f,y+.3f,Surface.Slide);
                o.Fixtures.Add(new Fixture{Position=new V3(f.X+hx-2.1f,y+2.5f,f.Z),Width=.7f,Depth=.5f,Power=950,Range=7,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                Box(detail,hx-2.1f,0,.7f,.5f,y+2.48f,y+2.52f,Surface.Glow);
            }
            solid.TranslateRotate(new V3(f.X,0,f.Z),0);detail.TranslateRotate(new V3(f.X,0,f.Z),0);safety.TranslateRotate(new V3(f.X,0,f.Z),0);
            o.Structure.Append(solid);o.Decoration.Append(detail);o.CollisionOnly.Append(safety);
            localNet.TranslateRotate(new V3(f.X,0,f.Z),0);net.Append(localNet);
        }
        private static void Stairs(MeshData m,MeshData details,MeshData safety,float x,float z,int levels)
        {
            for(int level=0;level<levels-1;level++)
            {
                float baseY=level*3.2f;
                for(int half=0;half<2;half++)for(int step=0;step<9;step++)
                {
                    float sx=x+(half==0?-.95f:.95f),sz=z+(half==0?-1.35f+(step+.5f)*.3f:1.35f-(step+.5f)*.3f);
                    float y=baseY+half*1.6f+(step+1)*1.6f/9;
                    // Thin steps preserve >3 m vertical clearance in the flights above.
                    Box(m,sx,sz,1.5f,.305f,y-.13f,y,Colour(level+half));
                }
                Box(m,x,z+1.8f,4.2f,.9f,baseY+1.6f-.16f,baseY+1.6f,Surface.Accent);
                Box(m,x,z-2.0f,4.2f,1.3f,baseY+3.2f-.16f,baseY+3.2f,Surface.Accent);
                // Protect the shaft edge, leaving the upper landing's sideways exit open.
                float floor=baseY+3.2f;
                foreach(var edge in new[]{new PlanRect(x+2.07f,z-3.6f,x+2.13f,z-2.65f),new PlanRect(x+2.07f,z-1.35f,x+2.13f,z+3.6f),new PlanRect(x-2.1f,z-2.68f,x+2.1f,z-2.62f)})
                {
                    Box(safety,edge.X,edge.Z,edge.Width,edge.Depth,floor,floor+1.1f,Surface.Metal);
                    Box(details,edge.X,edge.Z,edge.Width,edge.Depth,floor+.97f,floor+1.05f,Surface.Metal);
                    for(float t=0;t<Math.Max(edge.Width,edge.Depth);t+=.45f)
                        Box(details,edge.Width>edge.Depth?edge.MinX+t:edge.X,edge.Depth>edge.Width?edge.MinZ+t:edge.Z,.035f,.035f,floor,floor+1.05f,Surface.Metal);
                }
                foreach(int sign in new[]{-1,1})
                {
                    float sx=x+sign*1.78f;
                    var a=new V3(sx,baseY+1.0f+(sign<0?0:3.2f),z-1.5f);
                    var b=new V3(sx,baseY+2.6f,z+1.5f);
                    ZoneShapes.Beam(m,a,b,.045f,Surface.Metal,UV,4);
                }
            }
        }
        private static void Net(MeshData m,float x0,float x1,float bottom,float top,float spacing)
        {
            for(float x=x0;x<=x1;x+=spacing)Ribbon(m,new V3(x,bottom,0),new V3(x,top,0),.013f);
            for(float y=bottom;y<=top;y+=spacing)Ribbon(m,new V3(x0,y,0),new V3(x1,y,0),.013f);
        }
        private static void Ribbon(MeshData m,V3 a,V3 b,float width)
        {
            V3 side=V3.Cross(b-a,new V3(0,0,1)).Unit*(width*.5f);
            m.Quad(a-side,b-side,b+side,a+side,new V3(0,0,1),Surface.Metal,UV);
            m.Quad(a+side,b+side,b-side,a-side,new V3(0,0,-1),Surface.Metal,UV);
        }
        private static void Bridge(RoomGeometry o,SoftPlayPlan p)
        {
            V3 direction=(p.BridgeEnd-p.BridgeStart).Unit,right=V3.Cross(V3.Up,direction).Unit;
            V3 a=p.BridgeStart-direction*.35f,b=p.BridgeEnd+direction*.35f;
            o.Structure.Quad(a-right,b-right,b+right,a+right,V3.Up,Surface.Accent,UV);
            o.Structure.Quad(a+right-V3.Up*.15f,b+right-V3.Up*.15f,b-right-V3.Up*.15f,a-right-V3.Up*.15f,V3.Up*-1,Surface.Accent,UV);
            int parts=Math.Max(2,(int)Math.Ceiling((b-a).Length/2));
            for(int i=0;i<parts;i++)
            {
                V3 start=a+(b-a)*(i/(float)parts),end=a+(b-a)*((i+1f)/parts);
                if(p.EnclosedBridge)
                {
                    int sides=12;
                    for(int j=0;j<sides;j++)
                    {
                        float aa=j*Shapes.Pi*2/sides,bb=(j+1)*Shapes.Pi*2/sides;
                        V3 ra=right*(Shapes.Cos(aa)*1.44f)+V3.Up*(1.33f+Shapes.Sin(aa)*1.44f);
                        V3 rb=right*(Shapes.Cos(bb)*1.44f)+V3.Up*(1.33f+Shapes.Sin(bb)*1.44f);
                        // Double-sided hollow plastic shell; never a capped cylinder.
                        V3 normal=(right*Shapes.Cos((aa+bb)*.5f)+V3.Up*Shapes.Sin((aa+bb)*.5f)).Unit;
                        o.Structure.Quad(start+ra,end+ra,end+rb,start+rb,normal,Colour(i));
                        o.Structure.Quad(start+rb,end+rb,end+ra,start+ra,normal*-1,Colour(i));
                    }
                }
                else foreach(int sign in new[]{-1,1})
                {
                    V3 ra=right*(sign*1.02f);
                    o.CollisionOnly.Quad(start+ra,end+ra,end+ra+V3.Up*1.1f,start+ra+V3.Up*1.1f,right*sign,Surface.Metal);
                    o.CollisionOnly.Quad(start+ra+V3.Up*1.1f,end+ra+V3.Up*1.1f,end+ra,start+ra,right*-sign,Surface.Metal);
                    ZoneShapes.Beam(o.Decoration,start+ra+V3.Up,end+ra+V3.Up,.045f,Surface.Metal,UV,4);
                    ZoneShapes.Beam(o.Decoration,start+ra,end+ra+V3.Up,.016f,Surface.Metal,UV,4);
                }
            }
            o.Fixtures.Add(new Fixture{Position=(a+b)*.5f+V3.Up*2.35f,Width=.5f,Depth=.5f,Power=1000,Range=12,CustomColor=true,LightColor=o.Decoration.EmissionTint});
            var centre=(a+b)*.5f;Box(o.Decoration,centre.X,centre.Z,.5f,.5f,centre.Y+2.34f,centre.Y+2.37f,Surface.Glow);
        }
        private static void Slide(RoomGeometry o,PlaySlidePath path)
        {
            V3 right=V3.Cross(V3.Up,path.End-path.Start).Unit;float half=path.Width*.5f;
            const int n=24;
            for(int i=0;i<n;i++)
            {
                V3 a=path.Point(i/(float)n),b=path.Point((i+1f)/n);
                V3 normal=V3.Cross(b-a,right).Unit;
                o.Structure.Quad(a-right*half,b-right*half,b+right*half,a+right*half,normal,Surface.Slide,UV);
                o.Structure.Quad(a+right*half-V3.Up*.12f,b+right*half-V3.Up*.12f,b-right*half-V3.Up*.12f,a-right*half-V3.Up*.12f,normal*-1,Surface.Slide,UV);
                foreach(int sign in new[]{-1,1})
                {
                    V3 ra=right*(sign*half);
                    o.Structure.Quad(a+ra,b+ra,b+ra+V3.Up*.7f,a+ra+V3.Up*.7f,right*-sign,Surface.Accent,UV);
                    o.Structure.Quad(a+ra+V3.Up*.7f,b+ra+V3.Up*.7f,b+ra,a+ra,right*sign,Surface.Accent,UV);
                }
            }
        }
        private static void BallPit(RoomGeometry o,SoftPlayPlan p,int quality)
        {
            var q=p.BallPit;
            foreach(int sign in new[]{-1,1})
            {Box(o.Structure,q.X+sign*q.Width*.5f,q.Z,.16f,q.Depth,.02f,.26f,Surface.Accent);Box(o.Structure,q.X,q.Z+sign*q.Depth*.5f,q.Width,.16f,.02f,.26f,Surface.Slide);}
            var random=new StableRandom(p.Frames[0].Seed^0x39002u);
            int count=quality<=12?48:96;
            for(int i=0;i<count;i++)Sphere(o.Decoration,new V3(random.Range(q.MinX+.25f,q.MaxX-.25f),.16f,random.Range(q.MinZ+.25f,q.MaxZ-.25f)),.14f,Colour(i),6);
        }
        public static MeshData Prop(int kind,int colour)
        {
            var m=new MeshData{Theme=SurfaceTheme.SoftPlayFinish};var finish=Colour(colour);
            if(kind==0)Sphere(m,new V3(),.22f,finish,10);
            else if(kind==1)Box(m,0,0,.38f,.38f,-.19f,.19f,finish);
            else for(int i=0;i<16;i++)for(int j=0;j<5;j++)
            {
                float a=i*Shapes.Pi*2/16,b=(i+1)*Shapes.Pi*2/16,u=j*Shapes.Pi*2/5,v=(j+1)*Shapes.Pi*2/5;
                m.Quad(RingPoint(a,u),RingPoint(b,u),RingPoint(b,v),RingPoint(a,v),
                    new V3(Shapes.Cos((a+b)*.5f)*Shapes.Cos((u+v)*.5f),Shapes.Sin((a+b)*.5f)*Shapes.Cos((u+v)*.5f),Shapes.Sin((u+v)*.5f)),finish,UV);
            }
            return m;
        }
        private static V3 RingPoint(float a,float b)
        {float r=.18f+.045f*Shapes.Cos(b);return new V3(r*Shapes.Cos(a),r*Shapes.Sin(a),.045f*Shapes.Sin(b));}
        private static void Sphere(MeshData m,V3 centre,float radius,Surface surface,int sides)
        {
            int rings=sides/2;
            for(int y=0;y<rings;y++)for(int x=0;x<sides;x++)
            {
                V3 a=SpherePoint(x,y,sides,rings),b=SpherePoint(x+1,y,sides,rings),c=SpherePoint(x+1,y+1,sides,rings),d=SpherePoint(x,y+1,sides,rings);
                m.QuadUV(centre+a*radius,centre+b*radius,centre+c*radius,centre+d*radius,a,b,c,d,
                    new V2(x/(float)sides,y/(float)rings),new V2((x+1f)/sides,y/(float)rings),new V2((x+1f)/sides,(y+1f)/rings),new V2(x/(float)sides,(y+1f)/rings),surface);
            }
        }
        private static V3 SpherePoint(int x,int y,int sides,int rings)
        {float phi=y*Shapes.Pi/rings,theta=x*Shapes.Pi*2/sides;return new V3(Shapes.Sin(phi)*Shapes.Cos(theta),Shapes.Cos(phi),Shapes.Sin(phi)*Shapes.Sin(theta));}
        private static void Lounge(RoomGeometry o,Room r,SoftPlayPlan p)
        {
            // Small furniture and cubbies in the SE corner, away from the slide and the four exits.
            float x=r.Width*.5f-4,z=-r.Depth*.5f+3.5f;
            if(p.Frames.Length>2)return;
            Box(o.Structure,x,z,3.2f,.65f,0,.46f,Surface.Accent);
            Box(o.Structure,x,z-.3f,3.2f,.08f,.46f,.98f,Surface.Slide);
            Box(o.Structure,x+1.7f,z+1.5f,1.1f,.5f,0,1.5f,Surface.Plaster);
            for(int i=0;i<3;i++)Box(o.Decoration,x+1.7f,z+1.24f,.85f,.03f,.15f+i*.43f,.42f+i*.43f,Colour(i));
        }
        private static void Lights(RoomGeometry o,Room r,SoftPlayPlan p)
        {
            V3 colour=p.Lighting==1?new V3(1,.87f,.68f):p.Lighting==2?new V3(.85f,.91f,1):new V3(1,.97f,.9f);
            foreach(int sx in new[]{-1,0,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*r.Width*.31f,z=sz*r.Depth*.3f;
                Box(o.Decoration,x,z,2,.65f,r.Height-.23f,r.Height-.18f,Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(x,r.Height-.25f,z),Width=2,Depth=.65f,Power=p.Lighting==1?3500:5200,Range=r.Height+18,CustomColor=true,LightColor=colour});
            }
            // Modest bounced fill keeps a large hall readable in Performance without SSGI.
            o.Fixtures.Add(new Fixture{Position=new V3(0,3.4f,0),Width=2,Depth=2,Power=3500,Range=Math.Min(18,r.Width*.5f-1.2f),SolarFill=true,AmbientSupport=true,CustomColor=true,LightColor=colour});
        }
        public static CorridorGeometry Join(Room from,Room to,int direction,LayoutConfig c)
        {
            var o=new CorridorGeometry{Style=CorridorStyle.WideGallery};o.Structure.Theme=SurfaceTheme.SoftPlay;
            float half=SoftPlayPlanner.DoorWidth(from,direction,c)*.5f,z=c.CellSize*.5f;
            o.Structure.Box(new V3(-half,-.2f,z-.2f),new V3(half,0,z+.2f),Surface.Tile,UV);
            if(direction==1)o.Structure.TranslateRotate(new V3(),Shapes.Pi*.5f);
            return o;
        }
    }
}
