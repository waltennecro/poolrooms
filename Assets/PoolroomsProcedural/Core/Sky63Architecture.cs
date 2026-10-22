using System;
using System.Linq;
using System.Collections.Generic;
namespace Poolrooms.Core
{
    public static class Sky63Architecture
    {
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Quiet.Sky63;var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=o.CollisionOnly.Theme=SurfaceTheme.TranquilityOak;
            Floors(o,p.Ground,0);Floors(o,p.Above,p.Upper);
            Edges(o,p.Ground,0,r,c,p);Edges(o,p.Above,p.Upper,r,c,p);Stairs(o,p);Suspension(o,p);Programme(o,p);
            var vines=new MeshData{Theme=SurfaceTheme.OrangerieFoliage};Vines(o,vines,r,c,p);o.VisualMeshes.Add(vines);
            Furnish(o,p);if(c.EncounterExploration)Activities(o,p);
            foreach(var m in new[]{o.Structure,o.Decoration,o.CollisionOnly,vines})ProceduralArchitecture.Prune(m);
            return o;
        }
        static void B(MeshData m,float x,float z,float w,float d,float y,float top,Surface s=Surface.Tile)
        {if(w>.0001f&&d>.0001f&&top>y)ZoneShapes.Box(m,x,z,w,d,y,top,s,1.2f);}
        static void Floors(RoomGeometry o,PlanRect[] parts,float y)
        {
            foreach(var a in parts)
            {
                B(o.Structure,a.X,a.Z,a.Width,a.Depth,y-.2f,y);
                // Shallow plank joints live below the walking surface; no height snag.
                if(a.Width>a.Depth)for(float x=a.MinX+.5f;x<a.MaxX;x+=.7f)B(o.Decoration,x,a.Z,.012f,a.Depth,y-.002f,y+.0015f,Surface.Accent);
                else for(float z=a.MinZ+.5f;z<a.MaxZ;z+=.7f)B(o.Decoration,a.X,z,a.Width,.012f,y-.002f,y+.0015f,Surface.Accent);
            }
        }
        static void Edges(RoomGeometry o,PlanRect[] parts,float y,Room r,LayoutConfig c,Sky63Plan p)
        {
            foreach(var a in parts)for(int side=0;side<4;side++)
            {
                bool horizontal=side%2==0;float edge=horizontal?(side==0?a.MaxZ:a.MinZ):(side==1?a.MaxX:a.MinX);
                float lo=horizontal?a.MinX:a.MinZ,hi=horizontal?a.MaxX:a.MaxZ;
                var spans=new List<V2>{new V2(lo,hi)};
                foreach(var b in parts)
                {
                    float other=horizontal?(side==0?b.MinZ:b.MaxZ):(side==1?b.MinX:b.MaxX);
                    if(Math.Abs(other-edge)<.001f)Cut(spans,horizontal?b.MinX:b.MinZ,horizontal?b.MaxX:b.MaxZ);
                }
                bool boundary=Math.Abs(Math.Abs(edge)-p.Half)<.002f;
                if(boundary)
                {
                    if(Sky63Planner.Same(c,r.X,r.Z,side))spans.Clear();
                    else if(y<1&&r.HasExit(side))Cut(spans,-c.DoorWidth*.5f,c.DoorWidth*.5f);
                }
                // The upper stair landing opens onto the south gallery, not a rail.
                if(y==p.Upper&&horizontal&&Math.Abs(edge-(p.Stair.MinZ+Sky63Plan.Landing))<.002f)Cut(spans,p.Stair.X,p.Stair.MaxX);
                foreach(var s in spans)if(s.Y-s.X>.01f)Rail(o,horizontal?(s.X+s.Y)*.5f:edge,horizontal?edge:(s.X+s.Y)*.5f,horizontal?s.Y-s.X:.055f,horizontal?.055f:s.Y-s.X,y);
            }
        }
        static void Cut(List<V2> a,float lo,float hi)
        {
            for(int i=a.Count-1;i>=0;i--)
            {
                var s=a[i];if(lo>=s.Y-.001f||hi<=s.X+.001f)continue;a.RemoveAt(i);
                if(lo>s.X+.001f)a.Add(new V2(s.X,Math.Min(lo,s.Y)));if(hi<s.Y-.001f)a.Add(new V2(Math.Max(hi,s.X),s.Y));
            }
        }
        static void Rail(RoomGeometry o,float x,float z,float w,float d,float y)
        {
            B(o.CollisionOnly,x,z,w,d,y,y+1.12f,Surface.Accent);B(o.Decoration,x,z,w+.018f,d+.018f,y+1.04f,y+1.12f,Surface.Accent);
            B(o.Decoration,x,z,w,d,y+.12f,y+.18f,Surface.Accent);
            int n=Math.Max(1,(int)(Math.Max(w,d)/.65f));
            for(int i=0;i<=n;i++)B(o.Decoration,w>d?x-w*.5f+w*i/n:x,d>w?z-d*.5f+d*i/n:z,.065f,.065f,y,y+1.1f,Surface.Accent);
        }
        public static void Stairs(RoomGeometry o,Sky63Plan p)
        {
            var b=p.Stair;float start=b.MinZ+Sky63Plan.Landing,end=start+Sky63Plan.Flight,tread=Sky63Plan.Flight/p.Steps;
            for(int flight=0;flight<2;flight++)
            {
                float x=b.X+(flight==0?-.9f:.9f);
                for(int i=0;i<p.Steps;i++)
                {
                    float z=flight==0?start+(i+.5f)*tread:end-(i+.5f)*tread,y=flight*p.Upper*.5f+(i+1)*p.Upper/(2*p.Steps);
                    B(o.Structure,x,z,1.62f,tread+.002f,y-.15f,y);
                    B(o.Decoration,x,z+(flight==0?-1:1)*(tread*.5f-.025f),1.6f,.035f,y+.001f,y+.005f,Surface.Accent);
                }
                foreach(int sign in new[]{-1,1})
                {
                    float xx=x+sign*.79f;var a=new V3(xx,flight*p.Upper*.5f,flight==0?start:end);var bb=new V3(xx,(flight+1)*p.Upper*.5f,flight==0?end:start);
                    ZoneShapes.Beam(o.Decoration,a+V3.Up*1.08f,bb+V3.Up*1.08f,.04f,Surface.Accent,1.2f,6);
                    o.CollisionOnly.Quad(a,bb,bb+V3.Up*1.12f,a+V3.Up*1.12f,new V3(sign,0,0),Surface.Accent,1.2f);
                    o.CollisionOnly.Quad(a,a+V3.Up*1.12f,bb+V3.Up*1.12f,bb,new V3(-sign,0,0),Surface.Accent,1.2f);
                    for(int i=0;i<=p.Steps;i+=2)
                    {float z=flight==0?start+i*tread:end-i*tread,y=flight*p.Upper*.5f+i*p.Upper/(2*p.Steps);B(o.Decoration,xx,z,.05f,.05f,y,y+1.1f,Surface.Accent);}
                }
            }
            B(o.Structure,b.X,(end+b.MaxZ)*.5f,b.Width,b.MaxZ-end,p.Upper*.5f-.16f,p.Upper*.5f);
            B(o.Structure,b.X,(b.MinZ+start)*.5f,b.Width,start-b.MinZ,p.Upper-.16f,p.Upper);
            Rail(o,b.X,b.MaxZ,b.Width,.06f,p.Upper*.5f);
            Rail(o,b.MinX,(end+b.MaxZ)*.5f,.06f,b.MaxZ-end,p.Upper*.5f);Rail(o,b.MaxX,(end+b.MaxZ)*.5f,.06f,b.MaxZ-end,p.Upper*.5f);
        }
        static void Suspension(RoomGeometry o,Sky63Plan p)
        {
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*(p.Half-1.5f),z=sz*(p.Half-1.5f);if(p.Stair.Contains(x,z))continue;
                Shapes.Column(o.Structure,x,z,.12f,-2,p.Upper+5.2f,10,Surface.Accent,1.2f);
                Shapes.Column(o.Decoration,x,z,.18f,p.Upper-.2f,p.Upper+.14f,10,Surface.Metal,1.2f);
                var start=new V3(x,p.Upper+4.9f,z);
                var target=new V3(x,1.2f,sz*p.Clear);
                for(int i=0;i<12;i++)
                {
                    V3 Curve(float t){return start*(1-t)+target*t-V3.Up*(1.25f*Shapes.Sin(t*Shapes.Pi));}
                    ZoneShapes.Beam(o.Decoration,Curve(i/12f),Curve((i+1)/12f),.019f,Surface.Metal,1.2f,6);
                }
            }
        }
        static void Programme(RoomGeometry o,Sky63Plan p)
        {
            float x=-p.Q,z=p.Q,rad=p.Terrace-.35f;
            if(p.Use==Sky63Use.Tea||p.Use==Sky63Use.Meditation||p.Use==Sky63Use.Garden)
            {
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})B(o.Structure,x+sx*rad,z+sz*rad,.11f,.11f,0,3.25f,Surface.Accent);
                for(int i=-3;i<=3;i++)B(o.Decoration,x+i*rad/3,z,.075f,rad*2+.3f,3.2f,3.32f,Surface.Accent);
                foreach(int sign in new[]{-1,1})B(o.Structure,x,z+sign*rad,rad*2+.2f,.1f,3.1f,3.2f,Surface.Accent);
            }
            else if(p.Use==Sky63Use.Observatory||p.Use==Sky63Use.Wind)
            {
                Shapes.Column(o.Structure,x,z,.13f,0,1.3f,10,Surface.Metal,1.2f);
                // A freestanding armillary sculpture, not an invisible trigger.
                for(int ring=0;ring<3;ring++)for(int i=0;i<40;i++)
                {
                    V3 Point(float a){float s=Shapes.Sin(a),co=Shapes.Cos(a);return new V3(x+(ring==0?co:co*.65f),1.95f+(ring==0?0:s*.8f),z+(ring==0?s:ring==1?co*.4f:-co*.4f));}
                    ZoneShapes.Beam(o.Decoration,Point(i*Shapes.Pi/20),Point((i+1)*Shapes.Pi/20),.025f,Surface.Metal,1.2f,6);
                }
            }
            else if(p.Use==Sky63Use.Chimes)
            {
                foreach(int s in new[]{-1,1})B(o.Structure,x+s*1.1f,z,.09f,.09f,0,2.8f,Surface.Accent);B(o.Structure,x,z,2.3f,.1f,2.72f,2.83f,Surface.Accent);
                for(int i=0;i<9;i++){float xx=x-1+i*.25f;ZoneShapes.Beam(o.Decoration,new V3(xx,2.72f,z),new V3(xx,2.1f,z),.008f,Surface.Accent,1.2f,4);Shapes.Column(o.Decoration,xx,z,.03f,1.0f+i*.06f,2.1f,10,Surface.Metal,1.2f);}
            }
            else
            {
                // Shelving for shared notebooks / drawing papers.
                foreach(int s in new[]{-1,1})B(o.Structure,x+s*1.2f,z,.08f,.45f,0,1.6f,Surface.Accent);
                for(int shelf=0;shelf<3;shelf++)B(o.Structure,x,z,2.5f,.45f,.35f+shelf*.55f,.41f+shelf*.55f,Surface.Accent);
            }
        }
        static void Vines(RoomGeometry o,MeshData leaves,Room r,LayoutConfig c,Sky63Plan p)
        {
            for(int side=0;side<4;side++)if(!Sky63Planner.Same(c,r.X,r.Z,side))
                for(float along=-p.Half+1;along<p.Half;along+=2.3f)
                {
                    if(r.HasExit(side)&&Math.Abs(along)<c.DoorWidth*.5f+1.2f)continue;
                    float x=Directions.DX[side]*(p.Half-.12f)+(side%2==0?along:0),z=Directions.DZ[side]*(p.Half-.12f)+(side%2!=0?along:0);
                    for(int i=0;i<24;i++)
                    {
                        float y=-12+i*1.05f,xx=x+.16f*Shapes.Sin(y+along),zz=z+.12f*Shapes.Cos(y*.7f+along);
                        ZoneShapes.Beam(leaves,new V3(xx,y,zz),new V3(x+.16f*Shapes.Sin(y+1.05f+along),y+1.05f,z+.12f*Shapes.Cos((y+1.05f)*.7f+along)),.035f,Surface.Tile,1.2f,5);
                        foreach(int s in new[]{-1,1})
                        {
                            var a=new V3(xx,y,zz);var b=a+new V3(s*.45f,.22f,.13f);var cc=a+new V3(s*.2f,.43f,-.16f);
                            var n=V3.Cross(b-a,cc-a).Unit;leaves.Quad(a,b,cc,a+V3.Up*.15f,n,Surface.Tile,1.2f);leaves.Quad(a,a+V3.Up*.15f,cc,b,n*-1,Surface.Tile,1.2f);
                        }
                    }
                }
        }
        static void Furnish(RoomGeometry o,Sky63Plan p)
        {
            Source(o,p,5,-p.Q,-p.Q,0);Source(o,p,7,p.Q,p.Q,0);Source(o,p,8,p.Q-1.08f,p.Q,0,90);Source(o,p,8,p.Q+1.08f,p.Q,0,270);
            float top=SourceFurniture.Dimensions(7).Y+.012f;Source(o,p,11,p.Q-.18f,p.Q,top);Source(o,p,0,p.Q+.18f,p.Q,top);
            Source(o,p,9,-p.Q+1.7f,p.Q-1.8f,0);Source(o,p,2,-p.Half*.4f,p.Half-1.5f,p.Upper);
        }
        static void Source(RoomGeometry o,Sky63Plan p,int id,float x,float z,float y,float yaw=0)
        {
            var a=new SourceFurniturePlacement(630000+o.SourceProps.Count+o.SourceSeats.Count,id,new V3(x,y+.006f,z),yaw);var h=SourceFurniture.HalfExtents(id,yaw,.04f);
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)if(!p.Supports(x+dx*h.X,z+dz*h.Z,y>1))return;
            if(!SourceFurniture.Clear(o.Structure,a,.003f)||!SourceFurniture.Clear(o.CollisionOnly,a,.003f))return;
            if(SourceFurniture.IsSeat(id))o.SourceSeats.Add(a);else o.SourceProps.Add(a);
        }
        static void Activities(RoomGeometry o,Sky63Plan p)
        {
            var names=new[]{"El banco sobre las nubes","La mesa que espera","El jardín del viento","Cuadernos del cielo","El observatorio abierto","Cartas para mañana","Los nueve tubos","La terraza de las enredaderas"};
            var life=new RoomLifePlan{Vignette=new RoomVignette(names[(int)p.Use],"Una nota recoge las visitas de ayer y reserva este lugar para las de mañana. Dejaron agua, una silla y una página libre. No hace falta apresurarse: los caminos siguen unidos más allá de las nubes.",RoomActivity.Rest,(int)p.Use)};
            life.Notice=Panel(o,-p.Q,-p.Q+2.4f,0);
            life.Stations.Add(new StationPlacement(life.Notice.Value,StationKind.RestBell,6300,(int)p.Use));
            var kinds=new[]{StationKind.Sketch,StationKind.Tea,StationKind.WindRose,StationKind.Sketch,StationKind.StarChart,StationKind.Postcards,StationKind.Chimes,StationKind.Herbarium};
            life.Stations.Add(new StationPlacement(Panel(o,p.Q,p.Q+2.4f,0),kinds[(int)p.Use],6301,(int)p.Use));
            life.Stations.Add(new StationPlacement(Panel(o,p.Half*.25f,p.Half-1.4f,p.Upper),StationKind.WindRose,6302,(int)(p.Seed%16)));
            life.Furniture.Add(new LifePlacement(LifeFurnishing.Cabinet,-1,6303,new V3(-p.Half+1.5f,p.Upper+.006f,-p.Half*.35f),90));o.Life=life;
        }
        static DressingAnchor Panel(RoomGeometry o,float x,float z,float y)
        {
            B(o.Structure,x,z,1.4f,.1f,y+.75f,y+1.9f,Surface.Accent);B(o.Structure,x,z,.09f,.09f,y,y+.75f,Surface.Accent);
            var a=new V3(x-.68f,y+.78f,z-.052f);var b=new V3(x+.68f,y+.78f,z-.052f);var c=new V3(x-.68f,y+1.87f,z-.052f);
            return new DressingAnchor(new V3(x-.36f,y+1.15f,z-.052f),new V3(0,0,-1),a,c,b,0,0);
        }
    }
}
