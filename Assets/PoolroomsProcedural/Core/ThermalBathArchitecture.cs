using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class ThermalBathArchitecture
    {
        static void Box(MeshData m,float x,float z,float w,float d,float lo,float hi,Surface slot)
        {ZoneShapes.Box(m,x,z,w,d,lo,hi,slot,1.2f);}
        public static float MaxOpening(LayoutConfig c){return Math.Min(8,c.DoorWidth+2);}
        public static float Opening(Room r,int side,LayoutConfig c)
        {
            long x=(long)r.X+Directions.DX[side],z=(long)r.Z+Directions.DZ[side];
            if(x<int.MinValue||x>int.MaxValue||z<int.MinValue||z>int.MaxValue)return c.DoorWidth;
            ConnectedTerrainSite t;ConnectedMallSite m;
            if(ConnectedTerrainTopology.Reserved(c,(int)x,(int)z)||ConnectedTerrainTopology.Gateway(c,(int)x,(int)z,out t)||ConnectedMallTopology.Gateway(c,(int)x,(int)z,out m))return c.DoorWidth;
            return LiminalRegions.At(c,(int)x,(int)z)==LiminalEnvironment.ThermalBaths?MaxOpening(c):c.DoorWidth;
        }
        public static float DoorHeight(Room r,int side,LayoutConfig c){return Math.Max(c.DoorHeight,Opening(r,side,c)*.5f+2);}
        public static RoomGeometry Build(Room r,LayoutConfig c)
        {
            var p=r.Plan.Baths;if(p==null||!p.Valid(r,c))throw new ArgumentException("Invalid thermal baths.");
            var o=new RoomGeometry();o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.ThermalStone;o.Glass.Theme=SurfaceTheme.Glass;
            o.Decoration.EmissionTint=p.Hall==BathHall.WarmCloister?new V3(1,.84f,.65f):new V3(1,.93f,.82f);o.Decoration.EmissionStrength=.25f;
            var wicker=new MeshData{Theme=SurfaceTheme.ThermalWicker};
            var floor=new List<PlanRect>{new PlanRect(-r.Width*.5f,-r.Depth*.5f,r.Width*.5f,r.Depth*.5f)};
            foreach(var pool in p.Pools)
            {
                PlanRect.Subtract(floor,new PlanRect(pool.CenterX-pool.Width*.5f,pool.CenterZ-pool.Depth*.5f,pool.CenterX+pool.Width*.5f,pool.CenterZ+pool.Depth*.5f));
                var basin=new RoomGeometry();ProceduralArchitecture.Basin(basin,pool,c);
                foreach(var list in basin.Structure.Indices)if(list!=basin.Structure.Indices[(int)Surface.Slide]){basin.Structure.Indices[(int)Surface.Slide].AddRange(list);list.Clear();}
                o.Structure.Append(basin.Structure);o.Water.Append(basin.Water);
                o.Regions.Add(pool);
                // Narrow coping stays outside the cutout, without floating over the water.
                foreach(int sign in new[]{-1,1})
                {
                    Box(o.Decoration,pool.CenterX+sign*(pool.Width*.5f+.08f),pool.CenterZ,.12f,pool.Depth+.3f,.002f,.012f,Surface.Slide);
                    Box(o.Decoration,pool.CenterX,pool.CenterZ+sign*(pool.Depth*.5f+.08f),pool.Width,.12f,.002f,.012f,Surface.Slide);
                }
            }
            o.Region=p.Pools[0];foreach(var f in floor)Box(o.Structure,f.X,f.Z,f.Width,f.Depth,-1.65f,0,Surface.Tile);
            Envelope(o,r,c);Roof(o,r);Colonnade(o,r);
            for(int i=0;i<4;i++)if(!ThermalBathPlanner.WetCourt(p.Hall,i))Lounger(o,wicker,p.Courts[i]);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
            {
                float x=sx*(r.Width*.5f-1),z=sz*(r.Depth*.5f-1.1f);
                // Folded towels on built-in shelving; off the wet paths and doorway approaches.
                Box(o.Structure,x-sx*.6f,z,2.5f,.65f,0,.82f,Surface.Accent);
                Box(o.Structure,x-sx*.6f,z,2.55f,.68f,.82f,.9f,Surface.Tile);
                for(int towel=0;towel<3;towel++)Box(o.Decoration,x-sx*.6f-.7f+towel*.7f,z,.5f,.4f,.9f,.99f,Surface.Plaster);
                float lx=sx*(r.Width*.5f-.22f),lz=sz*r.Depth*.2f;
                Box(o.Decoration,lx,lz,.12f,.55f,2.7f,3.15f,Surface.Metal);
                Box(o.Decoration,lx-sx*.07f,lz,.02f,.35f,2.81f,3.04f,Surface.Glow);
                o.Fixtures.Add(new Fixture{Position=new V3(lx-sx*.16f,2.95f,lz),Width=.2f,Depth=.4f,Power=700,Range=7,CustomColor=true,LightColor=o.Decoration.EmissionTint});
                o.Fixtures.Add(new Fixture{Position=new V3(sx*r.Width*.2f,r.Height-.3f,sz*r.Depth*.2f),Width=3,Depth=3,Power=p.Hall==BathHall.WarmCloister?2400:3600,Range=r.Width*.65f,CustomColor=true,LightColor=new V3(1,.96f,.89f),SolarFill=true,AmbientSupport=true});
            }
            if(wicker.TriangleCount>0)o.VisualMeshes.Add(wicker);
            ProceduralArchitecture.Prune(o.Structure);ProceduralArchitecture.Prune(o.Water);ProceduralArchitecture.Prune(o.Decoration);ProceduralArchitecture.Prune(o.Glass);return o;
        }
        static void Envelope(RoomGeometry o,Room r,LayoutConfig c)
        {
            float half=r.Width*.5f;
            for(int side=0;side<4;side++)
            {
                var wall=new MeshData();float opening=Opening(r,side,c),height=DoorHeight(r,side,c);
                if(r.HasExit(side))Shapes.Portal(wall,r.Width,r.Height,0,opening,height,.3f,16,1.2f);
                else Box(wall,0,0,r.Width,.3f,0,r.Height,Surface.Plaster);
                wall.Indices[(int)Surface.Plaster].AddRange(wall.Indices[(int)Surface.Tile]);wall.Indices[(int)Surface.Tile].Clear();
                float gap=r.HasExit(side)?opening*.5f+.1f:0;
                foreach(int sign in new[]{-1,1})Box(wall,sign*(half+gap)*.5f,-.19f,half-gap,.1f,0,.22f,Surface.Tile);
                wall.TranslateRotate(new V3(Directions.DX[side]*half,0,Directions.DZ[side]*half),side*Shapes.Pi*.5f);o.Structure.Append(wall);
            }
        }
        public static List<PlanRect> Apertures(Room r)
        {
            float h=r.Width*.5f-2;var holes=new List<PlanRect>();
            switch(r.Plan.Baths.Roof)
            {
                case 0:holes.Add(new PlanRect(-h*.4f,-h,h*.4f,h));break;
                case 1:holes.Add(new PlanRect(-h*.68f,-h*.68f,h*.68f,h*.68f));break;
                case 2:foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})holes.Add(new PlanRect(x*h*.52f-h*.28f,z*h*.52f-h*.28f,x*h*.52f+h*.28f,z*h*.52f+h*.28f));break;
                case 3:foreach(int x in new[]{-1,1})holes.Add(new PlanRect(x*h*.5f-h*.16f,-h,x*h*.5f+h*.16f,h));break;
                default:holes.Add(new PlanRect(-h*.23f,-h,h*.23f,h));foreach(int x in new[]{-1,1})holes.Add(new PlanRect(x<0?-h:h*.23f,-h*.23f,x<0?-h*.23f:h,h*.23f));break;
            }
            return holes;
        }
        static void Roof(RoomGeometry o,Room r)
        {
            float h=r.Width*.5f,y=r.Height;var solid=new List<PlanRect>{new PlanRect(-h,-h,h,h)};
            foreach(var a in Apertures(r))
            {
                PlanRect.Subtract(solid,a);bool round=r.Plan.Baths.Roof==1||r.Plan.Baths.Roof==2;
                if(round)
                {
                    var rim=new MeshData();var outer=Shapes.RoundedRect(a.Width,a.Depth,0,8);var inner=Shapes.RoundedRect(a.Width,a.Depth,Math.Min(a.Width,a.Depth)*.5f,8);
                    Shapes.Ring(rim,outer,inner,y,Surface.Plaster,1.2f,false);Shapes.Ring(rim,outer,inner,y+.18f,Surface.Plaster,1.2f);Shapes.Skirt(rim,inner,y,y+.18f,Surface.Plaster,1.2f,true);
                    rim.TranslateRotate(new V3(a.X,0,a.Z),0);o.Structure.Append(rim);
                    var glass=new MeshData();Shapes.Fill(glass,inner,y+.08f,Surface.Tile,1.2f);glass.TranslateRotate(new V3(a.X,0,a.Z),0);o.Glass.Append(glass);
                }
                else Box(o.Glass,a.X,a.Z,a.Width,a.Depth,y+.06f,y+.085f,Surface.Tile);
                foreach(int sign in new[]{-1,1})
                {
                    Box(o.Decoration,a.X+sign*a.Width*.5f,a.Z,.09f,a.Depth,y-.1f,y+.2f,Surface.Metal);
                    Box(o.Decoration,a.X,a.Z+sign*a.Depth*.5f,a.Width,.09f,y-.1f,y+.2f,Surface.Metal);
                }
            }
            foreach(var s in solid)Box(o.Structure,s.X,s.Z,s.Width,s.Depth,y,y+.2f,Surface.Plaster);
        }
        static void Colonnade(RoomGeometry o,Room r)
        {
            var p=r.Plan.Baths;float half=r.Width*.5f;int n=2+p.Rhythm;
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})for(int i=0;i<n;i++)
            {
                float x=sx*(half-.42f),z=sz*(p.Clear+1+(half-p.Clear-2)*(i+.5f)/n);
                Shapes.Column(o.Structure,x,z,.17f,0,r.Height,16,Surface.Tile,1.2f);
                Shapes.Column(o.Decoration,x,z,.23f,0,.24f,16,Surface.Slide,1.2f);
                Shapes.Column(o.Decoration,x,z,.27f,r.Height-.24f,r.Height,16,Surface.Tile,1.2f);
            }
            // Shallow segmented stone ribs bridge the colonnades, above all walking headroom.
            foreach(int sign in new[]{-1,1})
            {
                float z=sign*r.Depth*.27f;
                for(int segment=0;segment<16;segment++)
                {
                    float x0=-half+r.Width*segment/16,x1=-half+r.Width*(segment+1)/16;
                    float y0=r.Height-.35f-1.25f*(x0/half)*(x0/half),y1=r.Height-.35f-1.25f*(x1/half)*(x1/half);
                    var a=new V3(x0,y0,z-.09f);var b=new V3(x1,y1,z-.09f);var c=new V3(x1,y1,z+.09f);var d=new V3(x0,y0,z+.09f);
                    var normal=V3.Cross(b-a,d-a).Unit;Prism(o.Decoration,a,b,c,d,normal,.16f,Surface.Plaster);
                }
            }
        }
        static void Prism(MeshData m,V3 a,V3 b,V3 c,V3 d,V3 normal,float thickness,Surface slot)
        {
            // Close both faces and the four sides; no floating one-sided ribbon.
            var up=V3.Up*thickness;
            m.Quad(a,b,c,d,normal.Y<0?normal:normal*-1,slot,1.2f);
            m.Quad(a+up,b+up,c+up,d+up,normal.Y>0?normal:normal*-1,slot,1.2f);
            var center=(a+b+c+d)*.25f;
            foreach(var pair in new[]{(a,b),(b,c),(c,d),(d,a)})
            {
                var n=V3.Cross(pair.Item2-pair.Item1,up).Unit;
                if(V3.Dot(n,(pair.Item1+pair.Item2)*.5f-center)<0)n=n*-1;
                m.Quad(pair.Item1,pair.Item2,pair.Item2+up,pair.Item1+up,n,slot,1.2f);
            }
        }
        static void Lounger(RoomGeometry o,MeshData wicker,PlanRect court)
        {
            float x=court.X,z=court.Z;
            foreach(int sign in new[]{-1,1})Box(o.Structure,x+sign*.42f,z,.09f,2.05f,.08f,.39f,Surface.Accent);
            foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})Box(o.Structure,x+sx*.37f,z+sz*.85f,.1f,.1f,0,.42f,Surface.Metal);
            Box(o.Structure,x,z-.38f,.96f,1.42f,.37f,.43f,Surface.Accent);
            Box(wicker,x,z-.38f,.9f,1.34f,.432f,.45f,Surface.Accent);
            var a=new V3(x-.48f,.43f,z+.3f);var b=new V3(x+.48f,.43f,z+.3f);var c=new V3(x+.48f,1.02f,z+1.12f);var d=new V3(x-.48f,1.02f,z+1.12f);
            var n=V3.Cross(c-a,b-a).Unit;if(n.Y<0)n=n*-1;
            o.Structure.Quad(a,b,c,d,n,Surface.Accent,1.2f);wicker.Quad(a+V3.Up*.012f,b+V3.Up*.012f,c+V3.Up*.012f,d+V3.Up*.012f,n,Surface.Accent,.5f);
            float za=z+.66f,zb=z+1.0f,ya=.43f+(.66f-.3f)*.59f/.82f+.012f,yb=.43f+(1.0f-.3f)*.59f/.82f+.012f;
            Prism(wicker,new V3(x-.3f,ya,za),new V3(x+.3f,ya,za),new V3(x+.3f,yb,zb),new V3(x-.3f,yb,zb),n,.075f,Surface.Plaster);
        }
    }
}
