using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class RainHouseArchitecture
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
            return LiminalRegions.At(c,(int)x,(int)z)==LiminalEnvironment.RainHouses?MaxOpening(c):c.DoorWidth;
        }
        public static float DoorHeight(Room r,int side,LayoutConfig c){return Math.Max(c.DoorHeight,Opening(r,side,c)*.5f+2);}
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Rain;if(p==null||!p.Valid(r,c))throw new ArgumentException("Invalid rain houses geometry");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.RainLodge;o.Glass.Theme=SurfaceTheme.Glass;
            o.Decoration.EmissionTint=new V3(1,.82f,.6f);o.Decoration.EmissionStrength=.3f;
            var roof=new MeshData{Theme=SurfaceTheme.RainRoof};var wood=new MeshData{Theme=SurfaceTheme.RainInterior};
            Box(o.Structure,0,0,r.Width,r.Depth,-.28f,0,Surface.Tile);
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();float door=Opening(r,side,c),height=DoorHeight(r,side,c),top=Math.Max(4.8f,height+.3f);
                if(r.HasExit(side))Shapes.Portal(wall,r.Width,top,0,door,height,.24f,16,1);
                else Box(wall,0,0,r.Width,.24f,0,top,Surface.Plaster);
                wall.Indices[(int)Surface.Plaster].AddRange(wall.Indices[(int)Surface.Tile]);wall.Indices[(int)Surface.Tile].Clear();
                wall.TranslateRotate(new V3(Directions.DX[side]*r.Width*.5f,0,Directions.DZ[side]*r.Depth*.5f),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
            // Raised only one centimetre; no invisible elevated floor or doorway step.
            foreach(var h in p.Houses)
            {
                var structure=new MeshData();var detail=new MeshData();var glazing=new MeshData();var cap=new MeshData();var floor=new MeshData();
                Lodge(structure,detail,glazing,cap,floor,h,p);
                V3 offset=new V3(h.X,0,h.Z);float yaw=h.Turns*Shapes.Pi*.5f;
                foreach(var mesh in new[]{structure,detail,glazing,cap,floor})mesh.TranslateRotate(offset,yaw);
                o.Structure.Append(structure);o.Decoration.Append(detail);o.Glass.Append(glazing);roof.Append(cap);wood.Append(floor);
                o.Fixtures.Add(new Fixture{Position=h.ToRoom(new V3(0,2.85f,.25f)),Width=1,Depth=1,Power=800,Range=6.5f,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                o.Fixtures.Add(new Fixture{Position=h.ToRoom(new V3(1.05f,2.25f,-h.Depth*.5f-.15f)),Width=.2f,Depth=.25f,Power=220,Range=3.5f,CustomColor=true,LightColor=o.Decoration.EmissionTint});
            }
            Galleries(o,r,p);
            o.VisualMeshes.Add(roof);o.VisualMeshes.Add(wood);o.CollisionOnly.Append(roof);o.CollisionOnly.Append(wood);o.CollisionOnly.Append(o.Glass);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.Glass);
            return o;
        }
        static void Wall(MeshData structure,MeshData detail,MeshData glass,float width,float height,float z,int turn,bool entrance,int rhythm)
        {
            var s=new MeshData();var d=new MeshData();var g=new MeshData();var panels=new List<PlanRect>{new PlanRect(-width*.5f,0,width*.5f,height)};
            if(entrance)PlanRect.Subtract(panels,new PlanRect(-.8f,0,.8f,2.5f));
            var windows=entrance?new[]{-width*.32f,width*.32f}:new[]{0f};float ww=entrance?1.05f:Math.Min(width-1,2.2f+rhythm*.1f);
            foreach(float x in windows)
            {
                var hole=new PlanRect(x-ww*.5f,1.02f,x+ww*.5f,2.42f);PlanRect.Subtract(panels,hole);
                Box(g,x,0,ww,.025f,1.02f,2.42f,Surface.Tile);
                foreach(int sign in new[]{-1,1})Box(d,x+sign*ww*.5f,0,.065f,.22f,.98f,2.47f,Surface.Accent);
                foreach(float y in new[]{1.02f,2.42f})Box(d,x,0,ww+.12f,.22f,y-.035f,y+.035f,Surface.Accent);
                Box(d,x,0,.04f,.05f,1.02f,2.42f,Surface.Metal);Box(d,x,-.1f,ww+.2f,.35f,.93f,.99f,Surface.Accent);
            }
            foreach(var a in panels)Box(s,a.X,0,a.Width,.16f,a.MinZ,a.MaxZ,Surface.Plaster);
            if(entrance)
            {
                foreach(int sign in new[]{-1,1})Box(d,sign*.82f,0,.1f,.24f,0,2.57f,Surface.Accent);
                Box(d,0,0,1.74f,.24f,2.5f,2.6f,Surface.Accent);
            }
            // Horizontal timber joints remain geometry at corners, not stripes painted on pool tile.
            for(int i=1;i<5;i++)foreach(int sign in new[]{-1,1})Box(d,sign*(width*.5f-.15f),-.1f,.18f,.04f,i*.58f,i*.58f+.025f,Surface.Metal);
            var offset=new V3(Directions.DX[turn]*z,0,Directions.DZ[turn]*z);
            foreach(var m in new[]{s,d,g})m.TranslateRotate(offset,turn*Shapes.Pi*.5f);
            structure.Append(s);detail.Append(d);glass.Append(g);
        }
        static void Lodge(MeshData s,MeshData d,MeshData glass,MeshData roof,MeshData floor,RainLodge h,RainHousePlan p)
        {
            float w=h.Width,depth=h.Depth,top=h.WallHeight;
            Wall(s,d,glass,w,top,depth*.5f,2,true,p.Rhythm);Wall(s,d,glass,w,top,depth*.5f,0,false,p.Rhythm);
            Wall(s,d,glass,depth,top,w*.5f,1,false,p.Rhythm);Wall(s,d,glass,depth,top,w*.5f,3,false,p.Rhythm);
            Box(floor,0,-.6f,w+.18f,depth+1.2f,.002f,.01f,Surface.Tile);
            // Plinths, corner joinery, skirting, gable fascia, porch columns and gutter downpipes.
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                Box(d,sx*(w*.5f-.08f),sz*(depth*.5f-.08f),.18f,.18f,0,top+.03f,Surface.Accent);
                Shapes.Column(d,sx*(w*.5f-.18f),sz*(depth*.5f+.22f),.035f,.03f,top+.2f,8,Surface.Metal,1);
            }
            foreach(int side in new[]{-1,1})
            {
                Box(d,side*(w*.5f-.15f),-depth*.5f-.95f,.12f,.12f,.01f,2.85f,Surface.Accent);
                Box(s,side*(w*.5f-.11f),-depth*.5f-.93f,.28f,.28f,0,.12f,Surface.Tile);
                Box(d,side*(w*.5f+.18f),0,.14f,depth+.6f,top-.06f,top+.05f,Surface.Metal);
            }
            Box(roof,0,-depth*.5f-.6f,w+.42f,1.5f,2.84f,2.96f,Surface.Tile);
            Box(d,1.05f,-depth*.5f-.13f,.24f,.16f,2.13f,2.38f,Surface.Metal);Box(d,1.05f,-depth*.5f-.23f,.16f,.035f,2.17f,2.32f,Surface.Glow);
            // Rear sideboard supports three objects at y=.93; central walking spine stays open.
            Box(s,0,depth*.5f-.42f,2.35f,.65f,.01f,.86f,Surface.Accent);Box(s,0,depth*.5f-.42f,2.48f,.78f,.86f,.91f,Surface.Tile);
            for(int drawer=0;drawer<3;drawer++)
            {Box(d,-.75f+drawer*.75f,depth*.5f-.76f,.69f,.035f,.2f,.75f,Surface.Accent);Box(d,-.75f+drawer*.75f,depth*.5f-.8f,.2f,.035f,.59f,.62f,Surface.Metal);}
            // Hanging fixture is the physical counterpart of the warm room light.
            Shapes.Column(d,0,.25f,.012f,2.78f,top,6,Surface.Metal,1);Shapes.Column(d,0,.25f,.22f,2.68f,2.8f,16,Surface.Accent,1);
            Shapes.Column(d,0,.25f,.18f,2.66f,2.68f,16,Surface.Glow,1);
            Furnish(s,d,h,p);
            var profile=RoofProfile(w,top,p.Roof,p.Detail);
            for(int plane=0;plane<profile.Length-1;plane++)
            {
                V3 a=profile[plane],b=profile[plane+1];RoofPanel(roof,a,b,depth+.58f);
                for(int rib=0;rib<3+p.Rhythm;rib++)
                {
                    float z=-depth*.5f+(rib+.5f)*depth/(3+p.Rhythm);
                    Beam(d,new V3(a.X,a.Y-.12f,z),new V3(b.X,b.Y-.12f,z),.07f,Surface.Accent);
                }
            }
            foreach(int sign in new[]{-1,1})for(int i=0;i<profile.Length-1;i++)
            {
                var a=profile[i];var b=profile[i+1];float z=sign*depth*.5f;
                s.Quad(new V3(a.X,top,z),new V3(b.X,top,z),new V3(b.X,b.Y,z),new V3(a.X,a.Y,z),new V3(0,0,sign),Surface.Plaster);
                Beam(d,new V3(a.X,a.Y,z),new V3(b.X,b.Y,z),.1f,Surface.Accent);
            }
        }
        static V3[] RoofProfile(float w,float h,int roof,int detail)
        {
            float x=w*.5f+.28f,rise=.85f+detail*.12f;
            if(roof==1)return new[]{new V3(-x,h+rise,0),new V3(x,h+.2f,0)};
            if(roof==2)return new[]{new V3(-x,h+rise,0),new V3(0,h+.22f,0),new V3(x,h+rise,0)};
            if(roof==3)return new[]{new V3(-x,h+.18f,0),new V3(-.55f,h+rise+.55f,0),new V3(.55f,h+rise+.55f,0),new V3(x,h+.18f,0)};
            if(roof==4)return new[]{new V3(-x,h+.2f,0),new V3(-x*.35f,h+rise,0),new V3(x*.25f,h+rise*.55f,0),new V3(x,h+.2f,0)};
            return new[]{new V3(-x,h+.12f,0),new V3(0,h+rise,0),new V3(x,h+.12f,0)};
        }
        static void RoofPanel(MeshData m,V3 a,V3 b,float depth)
        {
            float z=depth*.5f;V3 n=V3.Cross(b-a,new V3(0,0,depth)).Unit;if(n.Y<0)n=n*-1;
            var a0=new V3(a.X,a.Y,-z);var b0=new V3(b.X,b.Y,-z);var b1=new V3(b.X,b.Y,z);var a1=new V3(a.X,a.Y,z);var down=new V3(0,-.1f,0);
            m.Quad(a0,b0,b1,a1,n,Surface.Tile);m.Quad(a0+down,b0+down,b1+down,a1+down,n*-1,Surface.Tile);
            m.Quad(a0,a1,a1+down,a0+down,new V3(-1,0,0),Surface.Tile);m.Quad(b0,b1,b1+down,b0+down,new V3(1,0,0),Surface.Tile);
            m.Quad(a0,b0,b0+down,a0+down,new V3(0,0,-1),Surface.Tile);m.Quad(a1,b1,b1+down,a1+down,new V3(0,0,1),Surface.Tile);
        }
        static void Beam(MeshData m,V3 a,V3 b,float size,Surface slot)
        {
            var axis=(b-a).Unit;var right=new V3(axis.Z,0,-axis.X).Unit*size*.5f;var up=V3.Cross(axis,right).Unit*size*.5f;
            var p=new[]{a-right-up,a+right-up,a+right+up,a-right+up,b-right-up,b+right-up,b+right+up,b-right+up};
            m.Quad(p[0],p[1],p[2],p[3],axis*-1,slot);m.Quad(p[4],p[5],p[6],p[7],axis,slot);
            m.Quad(p[0],p[4],p[7],p[3],right.Unit*-1,slot);m.Quad(p[1],p[5],p[6],p[2],right.Unit,slot);
            m.Quad(p[0],p[1],p[5],p[4],up.Unit*-1,slot);m.Quad(p[3],p[2],p[6],p[7],up.Unit,slot);
        }
        static void Furnish(MeshData s,MeshData d,RainLodge h,RainHousePlan p)
        {
            float x=-h.Width*.5f+1.1f,z=h.Depth*.5f-1.6f;
            if(h.Use==RainHouseUse.Bedroom)
            {
                Box(s,x,z,1.65f,2.1f,.08f,.35f,Surface.Accent);Box(s,x,z,1.57f,2.02f,.35f,.58f,Surface.Slide);
                Box(d,x,z+.96f,1.7f,.12f,.1f,1.06f,Surface.Accent);Box(d,x,z-.22f,1.6f,1.4f,.58f,.61f,Surface.Slide);
                foreach(int sign in new[]{-1,1})Box(d,x+sign*.38f,z+.64f,.63f,.46f,.58f,.74f,Surface.Slide);
            }
            else if(h.Use==RainHouseUse.Reading)
            {
                Box(s,x,z,1.8f,.6f,.01f,2.25f,Surface.Accent);
                for(int row=0;row<5;row++)
                {
                    Box(d,x,z-.32f,1.66f,.03f,.15f+row*.4f,.48f+row*.4f,Surface.Slide);
                    for(int book=0;book<9;book++)Box(d,x-.72f+book*.18f,z-.35f,.12f,.08f,.18f+row*.4f,.36f+row*.4f+(book%3)*.035f,book%2==0?Surface.Accent:Surface.Plaster);
                }
            }
            else if(h.Use==RainHouseUse.Tea)
            {
                Box(s,x,z,1.9f,.75f,.01f,.86f,Surface.Accent);Box(s,x,z,2,.83f,.86f,.92f,Surface.Tile);
                Shapes.Column(d,x-.45f,z,.15f,.92f,1.14f,16,Surface.Metal,1);Beam(d,new V3(x-.58f,1.14f,z),new V3(x-.32f,1.14f,z),.045f,Surface.Metal);
                foreach(int cup in new[]{-1,1})Shapes.Column(d,x+.45f,z+cup*.2f,.065f,.92f,1.02f,12,Surface.Tile,1);
                for(int shelf=0;shelf<2;shelf++)Box(s,x,z+.3f,1.9f,.3f,1.55f+shelf*.4f,1.6f+shelf*.4f,Surface.Accent);
            }
            else if(h.Use==RainHouseUse.Workshop)
            {
                Box(s,x,z,1.9f,.9f,.84f,.94f,Surface.Accent);
                foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})Box(s,x+a*.8f,z+b*.32f,.1f,.1f,.01f,.84f,Surface.Metal);
                for(int spool=0;spool<5;spool++)Shapes.Column(d,x-.65f+spool*.32f,z,.08f,.94f,1.07f,10,Surface.Slide,1);
                Box(d,x,z+.4f,1.8f,.08f,1.25f,1.8f,Surface.Accent);
                for(int peg=0;peg<8;peg++)Box(d,x-.7f+peg*.2f,z+.32f,.025f,.16f,1.38f,1.42f,Surface.Metal);
            }
            else
            {
                Box(s,x,z,1.9f,1.2f,0,.44f,Surface.Accent);Box(d,x,z,1.76f,1.06f,.44f,.46f,Surface.Plaster);
                for(int pot=0;pot<3;pot++)
                {Shapes.Column(d,x-.55f+pot*.55f,z,.16f,.46f,.7f,16,Surface.Tile,1);for(int stem=0;stem<3;stem++)Shapes.Column(d,x-.55f+pot*.55f+(stem-1)*.05f,z,.012f,.7f,.95f+stem*.08f,6,Surface.Accent,1);}
            }
            // Lattice ventilation, visible trim and a coat rack next to, never across, the doorway.
            Box(d,h.Width*.5f-.5f,-h.Depth*.5f+.12f,.65f,.06f,1.3f,1.4f,Surface.Accent);
            for(int hook=0;hook<3;hook++)Box(d,h.Width*.5f-.7f+hook*.2f,-h.Depth*.5f+.19f,.025f,.12f,1.27f,1.35f,Surface.Metal);
        }
        static void Galleries(RoomGeometry o,Room r,RainHousePlan p)
        {
            float width=p.Clear*2-.5f,half=r.Width*.5f;
            // Real roof volumes determine rain shelter. Crossings have two staggered roofs.
            Box(o.Glass,0,0,width,r.Depth,3.75f,3.79f,Surface.Tile);
            foreach(int sign in new[]{-1,1})Box(o.Glass,sign*(half+width*.5f)*.5f,0,half-width*.5f,width,3.85f,3.89f,Surface.Tile);
            for(int i=0;i<4+p.Rhythm;i++)
            {
                float t=-half+.65f+(r.Width-1.3f)*(i+.5f)/(4+p.Rhythm);
                Box(o.Decoration,0,t,width+.14f,.075f,3.64f,3.74f,Surface.Accent);
                if(Math.Abs(t)>width*.5f)Box(o.Decoration,t,0,.075f,width+.14f,3.74f,3.84f,Surface.Accent);
                foreach(int sign in new[]{-1,1})if(Math.Abs(t)>p.Clear+.4f)
                {Box(o.Structure,sign*(p.Clear-.1f),t,.1f,.1f,0,3.75f,Surface.Accent);Box(o.Structure,t,sign*(p.Clear-.1f),.1f,.1f,0,3.85f,Surface.Accent);}
            }
            foreach(int sign in new[]{-1,1})
            {
                Box(o.Decoration,sign*width*.5f,0,.08f,r.Depth,3.67f,3.77f,Surface.Accent);
                Box(o.Decoration,0,sign*width*.5f,r.Width,.08f,3.77f,3.87f,Surface.Accent);
                Box(o.Decoration,sign*(p.Clear-.2f),0,.035f,r.Depth,.004f,.012f,Surface.Metal);
                Box(o.Decoration,0,sign*(p.Clear-.2f),r.Width,.035f,.014f,.022f,Surface.Metal);
            }
        }
    }
}
