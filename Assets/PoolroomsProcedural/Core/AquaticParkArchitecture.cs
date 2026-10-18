using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class AquaticParkArchitecture
    {
        static void Box(MeshData m,float x,float z,float w,float d,float lo,float hi,Surface slot)
        {ZoneShapes.Box(m,x,z,w,d,lo,hi,slot,1);}
        public static float MaxOpening(LayoutConfig c){return Math.Min(8,c.DoorWidth+2);}
        public static float Opening(Room r,int side,LayoutConfig c)
        {
            long x=(long)r.X+Directions.DX[side],z=(long)r.Z+Directions.DZ[side];
            if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue)return c.DoorWidth;
            ConnectedTerrainSite terrain;ConnectedMallSite mall;
            if(ConnectedTerrainTopology.Reserved(c,(int)x,(int)z)||ConnectedTerrainTopology.Gateway(c,(int)x,(int)z,out terrain)||ConnectedMallTopology.Gateway(c,(int)x,(int)z,out mall))return c.DoorWidth;
            return LiminalRegions.At(c,(int)x,(int)z)==LiminalEnvironment.AquaticPark?MaxOpening(c):c.DoorWidth;
        }
        public static float DoorHeight(Room r,int side,LayoutConfig c){return Math.Max(c.DoorHeight,Opening(r,side,c)*.5f+2);}
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Aquatic;if(p==null||!p.Valid(r,c))throw new ArgumentException("Invalid aquatic park geometry.");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=((int)p.Style%2==0?SurfaceTheme.AquaticPark:SurfaceTheme.AquaticCoral);o.Glass.Theme=SurfaceTheme.Glass;
            var floor=new List<PlanRect>{new PlanRect(-p.Half,-p.Half,p.Half,p.Half)};
            foreach(var pool in p.Pools)
            {
                PlanRect.Subtract(floor,new PlanRect(pool.CenterX-pool.Width*.5f,pool.CenterZ-pool.Depth*.5f,pool.CenterX+pool.Width*.5f,pool.CenterZ+pool.Depth*.5f));
                var basin=new RoomGeometry();ProceduralArchitecture.Basin(basin,pool,c);o.Structure.Append(basin.Structure);o.Water.Append(basin.Water);o.Regions.Add(pool);
                // Flush rounded coping is part of the actual basin, never a slab over the water.
            }
            o.Region=p.Pools[0];foreach(var f in floor)Box(o.Structure,f.X,f.Z,f.Width,f.Depth,-1.4f,0,Surface.Tile);
            Envelope(o,r,c);Roof(o,r);Tower(o,r);
            foreach(var slide in p.Slides)
            {
                Flume(o.Structure,slide,Math.Max(16,Math.Min(32,c.CurveSegments)),Surface.Slide);
                // Structural saddles below the shell, not freestanding decorative floating tubes.
                for(int i=12;i<slide.Points.Length-10;i+=24)
                {
                    var at=slide.Points[i];if(Math.Abs(at.Z)<p.Clear+1.0f)continue;
                    // Columns only on dry paving: pool beds and exit steps stay clear.
                    bool wet=false;foreach(var pool in p.Pools)if(pool.Contains(at.X,at.Z))wet=true;
                    bool clear=true;
                    foreach(var other in p.Slides)foreach(var q in other.Points)
                        if((q.X-at.X)*(q.X-at.X)+(q.Z-at.Z)*(q.Z-at.Z)<1.8f&&q.Y<at.Y-.2f)clear=false;
                    if(!wet&&clear&&at.Y>1)Shapes.Column(o.Structure,at.X,at.Z,.1f,0,at.Y-.18f,12,Surface.Metal,1);
                }
            }
            Attractions(o,r);
            // This level intentionally has no emissive geometry, point lights or solar-fill fixtures.
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.Water);ProceduralArchitecture.Prune(o.Glass);return o;
        }
        static void Envelope(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f;
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();float opening=Opening(r,side,c),height=DoorHeight(r,side,c);
                float sill=Math.Max(7.3f,height+.35f);
                if(r.HasExit(side))Shapes.Portal(wall,r.Width,sill,0,opening,height,.26f,20,1);
                else Box(wall,0,0,r.Width,.26f,0,sill,Surface.Plaster);
                Box(wall,0,0,r.Width,.26f,sill+2.4f,r.Height,Surface.Plaster);
                wall.Indices[(int)Surface.Plaster].AddRange(wall.Indices[(int)Surface.Tile]);wall.Indices[(int)Surface.Tile].Clear();
                wall.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Structure.Append(wall);
                var glass=new MeshData();Box(glass,0,0,r.Width-.2f,.035f,sill,sill+2.4f,Surface.Tile);
                var frames=new MeshData();int count=8+r.Plan.Aquatic.Hall;
                for(int i=0;i<=count;i++)Box(frames,-half+r.Width*i/count,0,.1f,.32f,sill-.1f,sill+2.5f,Surface.Metal);
                foreach(float y in new[]{sill,sill+2.4f})Box(frames,0,0,r.Width,.32f,y-.05f,y+.05f,Surface.Metal);
                glass.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);
                frames.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Glass.Append(glass);o.Decoration.Append(frames);
            }
        }
        public static List<PlanRect> RoofApertures(Room r)
        {
            float h=r.Width*.5f-1.1f;var holes=new List<PlanRect>();int type=r.Plan.Aquatic.Roof;
            if(type==0)holes.Add(new PlanRect(-h,-h,h,h));
            else if(type==1)holes.Add(new PlanRect(-h*.6f,-h,h*.6f,h));
            else if(type==2)foreach(int sign in new[]{-1,1})holes.Add(new PlanRect(sign*h*.5f-h*.32f,-h,sign*h*.5f+h*.32f,h));
            else if(type==3)for(int i=0;i<4;i++)holes.Add(new PlanRect(-h,-h+(i+.15f)*h*.5f,h,-h+(i+.82f)*h*.5f));
            else foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})holes.Add(new PlanRect(sx*h*.5f-h*.38f,sz*h*.5f-h*.38f,sx*h*.5f+h*.38f,sz*h*.5f+h*.38f));
            return holes;
        }
        static void Roof(RoomGeometry o,Room r)
        {
            var p=r.Plan.Aquatic;var solid=new List<PlanRect>{new PlanRect(-p.Half,-p.Half,p.Half,p.Half)};
            foreach(var a in RoofApertures(r))
            {
                PlanRect.Subtract(solid,a);Box(o.Glass,a.X,a.Z,a.Width,a.Depth,r.Height+.03f,r.Height+.055f,Surface.Tile);
                foreach(int sign in new[]{-1,1})
                {Box(o.Decoration,a.X+sign*a.Width*.5f,a.Z,.1f,a.Depth,r.Height-.15f,r.Height+.15f,Surface.Metal);Box(o.Decoration,a.X,a.Z+sign*a.Depth*.5f,a.Width,.1f,r.Height-.15f,r.Height+.15f,Surface.Metal);}
            }
            foreach(var a in solid)Box(o.Structure,a.X,a.Z,a.Width,a.Depth,r.Height,r.Height+.22f,Surface.Plaster);
            int ribs=4+p.Hall;
            for(int i=0;i<ribs;i++)
            {
                float z=-p.Half+(i+.5f)*r.Depth/ribs;
                Box(o.Decoration,0,z,r.Width,.09f,r.Height-.18f,r.Height-.08f,Surface.Metal);
                // Truss diagonals sit overhead; all walking headroom is preserved.
                for(int k=0;k<8;k++)Beam(o.Decoration,new V3(-p.Half+k*r.Width/8,r.Height-.2f,z),new V3(-p.Half+(k+.5f)*r.Width/8,r.Height-.75f,z),.035f,Surface.Metal);
            }
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                Shapes.Column(o.Structure,sx*(p.Half-.35f),sz*(p.Half-.4f),.22f,0,r.Height,20,Surface.Tile,1);
        }
        public static void Beam(MeshData m,V3 a,V3 b,float radius,Surface slot)
        {
            V3 tangent=(b-a).Unit,right=V3.Cross(tangent,Math.Abs(tangent.Y)>.95f?new V3(1,0,0):V3.Up).Unit,up=V3.Cross(tangent,right).Unit;
            const int cross=8;
            for(int j=0;j<cross;j++)
            {
                float t=j*2*Shapes.Pi/cross,u=(j+1)*2*Shapes.Pi/cross;
                V3 n=right*Shapes.Cos(t)+up*Shapes.Sin(t),q=right*Shapes.Cos(u)+up*Shapes.Sin(u);
                m.QuadUV(a+n*radius,b+n*radius,b+q*radius,a+q*radius,n,n,q,q,new V2(0,0),new V2((b-a).Length,0),new V2((b-a).Length,.1f),new V2(0,.1f),slot);
                m.Triangle(a,a+n*radius,a+q*radius,tangent*-1,tangent*-1,tangent*-1,new V2(),new V2(),new V2(),slot);
                m.Triangle(b,b+n*radius,b+q*radius,tangent,tangent,tangent,new V2(),new V2(),new V2(),slot);
            }
        }
        static void Tower(RoomGeometry o,Room r)
        {
            var p=r.Plan.Aquatic;float h=p.Half,z0=-h+2.2f,run=p.FlightRun,y=p.TowerHeight,deckMin=p.Slides[p.Slides.Length-1].Points[0].X-1.25f;
            Box(o.Structure,(deckMin+h-.55f)*.5f,-h+1.3f,h-.55f-deckMin,1.8f,y-.16f,y,Surface.Tile);
            // Two 1.25m flights, 18cm risers and 27cm treads, linked by a full-width landing.
            for(int i=0;i<p.FlightSteps;i++)
            {
                Box(o.Structure,h-1.35f,z0+(i+.5f)*.27f,1.25f,.27f,0,(i+1)*.18f,Surface.Tile);
                Box(o.Structure,h-2.8f,z0+run-(i+.5f)*.27f,1.25f,.27f,p.TowerHeight*.5f,(p.FlightSteps+i+1)*.18f,Surface.Tile);
            }
            Box(o.Structure,h-2.07f,z0+run+.65f,2.75f,1.3f,y*.5f-.16f,y*.5f,Surface.Tile);
            for(int flight=0;flight<2;flight++)foreach(int sign in new[]{-1,1})
            {
                float x=h-(flight==0?1.35f:2.8f)+sign*.67f;
                V3 a=new V3(x,(flight==0?0:y)+1.05f,z0),b=new V3(x,y*.5f+1.05f,z0+run);
                Beam(o.Structure,a,b,.035f,Surface.Metal);
                for(int k=0;k<=p.FlightSteps;k+=3)
                {float t=k/(float)p.FlightSteps;float top=a.Y+(b.Y-a.Y)*t;Shapes.Column(o.Structure,x,z0+run*t,.025f,top-1.10f,top,8,Surface.Metal,1);}
            }
            foreach(int sign in new[]{-1,1})Shapes.Column(o.Structure,h-.72f,-h+1+sign*.35f,.095f,0,y-.16f,12,Surface.Metal,1);
            // Guard the landing's far edge while both flight entrances stay open.
            float back=z0+run+1.3f;
            Beam(o.Structure,new V3(h-3.45f,y*.5f+1.05f,back),new V3(h-.7f,y*.5f+1.05f,back),.035f,Surface.Metal);
            foreach(float x in new[]{h-3.45f,h-.7f})
            {
                Beam(o.Structure,new V3(x,y*.5f+1.05f,z0+run),new V3(x,y*.5f+1.05f,back),.035f,Surface.Metal);
                foreach(float z in new[]{z0+run,back})Shapes.Column(o.Structure,x,z,.025f,y*.5f-.02f,y*.5f+1.05f,8,Surface.Metal,1);
            }
            Shapes.Column(o.Structure,deckMin+.12f,-h+.8f,.09f,0,y-.16f,12,Surface.Metal,1);
            Beam(o.Structure,new V3(deckMin,y+1.05f,-h+.45f),new V3(deckMin,y+1.05f,z0),.035f,Surface.Metal);
            foreach(float z in new[]{-h+.45f,z0})Shapes.Column(o.Structure,deckMin,z,.025f,y-.02f,y+1.05f,8,Surface.Metal,1);
            // Platform guard has an open forward edge for the slide mouths and a stair opening.
            Beam(o.Structure,new V3(deckMin,y+1.05f,-h+.45f),new V3(h-.55f,y+1.05f,-h+.45f),.035f,Surface.Metal);
            for(float x=deckMin;x<h-.5f;x+=.75f)Shapes.Column(o.Structure,x,-h+.45f,.025f,y,y+1.05f,8,Surface.Metal,1);
            var guards=new List<PlanRect>{new PlanRect(deckMin,z0-.05f,h-.55f,z0+.05f)};
            foreach(var slide in p.Slides)
            {float x=slide.Points[0].X;PlanRect.Subtract(guards,new PlanRect(x-slide.Radius-.16f,z0-1,x+slide.Radius+.16f,z0+1));}
            PlanRect.Subtract(guards,new PlanRect(h-3.49f,z0-1,h-2.11f,z0+1));
            foreach(var guard in guards)if(guard.Width>.12f)
            {
                Beam(o.Structure,new V3(guard.MinX,y+1.05f,z0),new V3(guard.MaxX,y+1.05f,z0),.035f,Surface.Metal);
                foreach(float x in new[]{guard.MinX,guard.MaxX})Shapes.Column(o.Structure,x,z0,.025f,y-.02f,y+1.05f,8,Surface.Metal,1);
            }
        }
        public static void Flume(MeshData m,AquaticSlidePath path,int cross,Surface slot)
        {
            const float shell=.075f;float range=path.Enclosed?2*Shapes.Pi:Shapes.Pi,begin=path.Enclosed?-Shapes.Pi:-Shapes.Pi*.5f;
            for(int i=0;i<path.Points.Length-1;i++)
            {
                V3 t0=path.Tangent(i),t1=path.Tangent(i+1),right0=new V3(t0.Z,0,-t0.X).Unit,right1=new V3(t1.Z,0,-t1.X).Unit;
                V3 up0=V3.Cross(t0,right0).Unit,up1=V3.Cross(t1,right1).Unit,p=path.Points[i]+up0*path.Radius,q=path.Points[i+1]+up1*path.Radius;
                for(int j=0;j<cross;j++)
                {
                    float a=begin+range*j/cross,b=begin+range*(j+1)/cross;
                    V3 na=right0*Shapes.Sin(a)-up0*Shapes.Cos(a),nb=right0*Shapes.Sin(b)-up0*Shapes.Cos(b),nc=right1*Shapes.Sin(b)-up1*Shapes.Cos(b),nd=right1*Shapes.Sin(a)-up1*Shapes.Cos(a);
                    for(int outer=0;outer<2;outer++)
                    {
                        float radius=path.Radius+outer*shell,sign=outer==0?-1:1;
                        m.QuadUV(p+na*radius,p+nb*radius,q+nc*radius,q+nd*radius,na*sign,nb*sign,nc*sign,nd*sign,new V2(a,path.Distances[i]),new V2(b,path.Distances[i]),new V2(b,path.Distances[i+1]),new V2(a,path.Distances[i+1]),slot);
                    }
                    if(i==0)m.Quad(p+na*path.Radius,p+nb*path.Radius,p+nb*(path.Radius+shell),p+na*(path.Radius+shell),t0*-1,slot);
                    if(i==path.Points.Length-2)m.Quad(q+nd*path.Radius,q+nc*path.Radius,q+nc*(path.Radius+shell),q+nd*(path.Radius+shell),t1,slot);
                }
                if(!path.Enclosed)foreach(int edge in new[]{0,cross})
                {
                    float a=begin+range*edge/cross;V3 n=right0*Shapes.Sin(a)-up0*Shapes.Cos(a),nn=right1*Shapes.Sin(a)-up1*Shapes.Cos(a);
                    m.Quad(p+n*path.Radius,p+n*(path.Radius+shell),q+nn*(path.Radius+shell),q+nn*path.Radius,(up0+up1).Unit,slot);
                }
            }
        }
        static void Attractions(RoomGeometry o,Room r)
        {
            var p=r.Plan.Aquatic;var pool=p.Pools[1];
            // Stepping islands have real supports; 27cm gaps can be crossed with normal movement/jumping.
            int islands=2+p.Hall%4;
            for(int i=0;i<islands;i++)
            {
                float z=pool.CenterZ+(i-(islands-1)*.5f)*.95f,x=pool.CenterX;
                Shapes.Column(o.Structure,x,z,.31f,pool.Bottom,.12f,20,Surface.Accent,1);
                Shapes.Column(o.Decoration,x,z,.34f,.12f,.16f,20,Surface.Slide,1);
            }
            // Low accessible springboard deck. The existing jump/double-jump works on it.
            var calm=p.Pools[2];float bx=calm.CenterX,bz=calm.CenterZ-calm.Depth*.5f-.45f;
            Box(o.Structure,bx,bz,1.15f,1.1f,0,.18f,Surface.Tile);
            Box(o.Structure,bx,bz+.7f,1.1f,2.0f,.18f,.29f,Surface.Slide);
            foreach(int sign in new[]{-1,1})Shapes.Column(o.Structure,bx+sign*.4f,bz,.045f,0,.18f,8,Surface.Metal,1);
            // Current control is on paving, not in an obstructed pool or behind a rail.
            Box(o.Structure,p.Valve.X,p.Valve.Z,.18f,.18f,0,1.1f,Surface.Metal);
            // Benches, drainage grilles and wall-mounted towel rails give the big hall functional detail.
            foreach(int sign in new[]{-1,1})
            {
                float x=-p.Half+3.2f,z=sign*(p.Half-.6f);
                for(int i=0;i<3;i++)Box(o.Structure,x+(i-1)*.72f,z,.68f,.58f,.48f,.54f,Surface.Accent);
                foreach(int end in new[]{-1,1})Box(o.Structure,x+end*.8f,z,.09f,.5f,0,.48f,Surface.Metal);
                Beam(o.Decoration,new V3(-p.Half+.25f,1.45f,sign*(p.Half-2.6f)),new V3(-p.Half+.25f,1.45f,sign*(p.Half-1.1f)),.025f,Surface.Metal);
            }
            foreach(var basin in p.Pools)for(int k=0;k<8;k++)
                Box(o.Decoration,basin.CenterX+(k-3.5f)*.09f,basin.CenterZ-basin.Depth*.5f-.2f,.035f,.2f,.001f,.004f,Surface.Metal);
        }
        public static MeshData ValveWheel()
        {
            var mesh=new MeshData{Theme=SurfaceTheme.AquaticPark};
            const int n=24;
            for(int i=0;i<n;i++)
            {
                float a=i*2*Shapes.Pi/n,b=(i+1)*2*Shapes.Pi/n;
                Beam(mesh,new V3(Shapes.Cos(a)*.24f,0,Shapes.Sin(a)*.24f),new V3(Shapes.Cos(b)*.24f,0,Shapes.Sin(b)*.24f),.027f,Surface.Slide);
            }
            for(int i=0;i<4;i++){float a=i*Shapes.Pi*.5f;Beam(mesh,new V3(),new V3(Shapes.Cos(a)*.24f,0,Shapes.Sin(a)*.24f),.02f,Surface.Metal);}
            return mesh;
        }
    }
}
