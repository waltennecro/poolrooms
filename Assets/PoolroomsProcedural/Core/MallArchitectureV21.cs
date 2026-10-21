using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public static class MallArchitectureV21
    {
        private static void B(MeshData m,float x,float z,float w,float d,float a,float b,Surface s=Surface.Plaster)
        {MallArchitecture.Box(m,x,z,w,d,a,b,s);}
        public static void Balconies(RoomGeometry o,MallPlan p,int floor)
        {
            float y=floor*MallPlan.FloorHeight;
            foreach(var edge in p.Design.Edges(floor))
            {
                V3 a=edge.A+V3.Up*y,b=edge.B+V3.Up*y,mid=(a+b)*.5f;
                B(o.Structure,mid.X,mid.Z,Math.Max(.13f,Math.Abs(b.X-a.X)),Math.Max(.13f,Math.Abs(b.Z-a.Z)),y,y+.18f,Surface.Accent);
                int count=Math.Max(1,(int)Math.Ceiling((b-a).Length/2));
                for(int i=0;i<count;i++)
                {
                    V3 start=a+(b-a)*(i/(float)count),end=a+(b-a)*((i+1)/(float)count);
                    V3 panel=(start+end)*.5f;
                    // Thin closed panes block PhysX from both directions at each balcony edge.
                    B(o.Glass,panel.X,panel.Z,Math.Max(.035f,Math.Abs(end.X-start.X)),Math.Max(.035f,Math.Abs(end.Z-start.Z)),y+.2f,y+1.02f);
                    ZoneShapes.Beam(o.Decoration,start+V3.Up*.96f,end+V3.Up*.96f,.065f,Surface.Metal,1,6);
                    B(o.Decoration,start.X,start.Z,.055f,.055f,y+.12f,y+1.08f,Surface.Metal);
                }
                ZoneShapes.Beam(o.Structure,a+V3.Up*1.08f,b+V3.Up*1.08f,.07f,Surface.Metal,1,6);
            }
            foreach(float x in p.Design.Bridges(floor))
            {
                // The bridge plate is produced by FloorPlates, so its joins have no collision gap.
                B(o.Decoration,x,0,MallSpatialDesign.BridgeWidth-.15f,24,y-.24f,y-.20f,Surface.Metal);
                foreach(int side in new[]{-1,1})B(o.Decoration,x+side*1.6f,0,.06f,22,y+.002f,y+.009f,Surface.Metal);
                MallGlyphs.Write(o.Decoration,"GALERIA "+(floor+1),new V3(x-1.9f,y+3.55f,-13.8f),.055f);
            }
        }
        public static void Roof(RoomGeometry o,MallPlan p)
        {
            float y=p.Floors*MallPlan.FloorHeight;
            var plates=new List<MallRect>{new MallRect(-p.Width*.5f,-p.Depth*.5f,p.Width*.5f,p.Depth*.5f)};
            foreach(var hole in p.Design.RoofOpenings)
            {var next=new List<MallRect>();foreach(var plate in plates)next.AddRange(plate.Subtract(hole));plates=next;}
            foreach(var r in plates)B(o.Structure,(r.X0+r.X1)*.5f,(r.Z0+r.Z1)*.5f,r.Width,r.Depth,y,y+.25f);
            foreach(var r in p.Design.RoofOpenings)
            {
                int strips=p.Design.Roof==MallRoofShape.BarrelVault?8:p.Design.Roof==MallRoofShape.Lantern?2:1;
                float center=(r.Z0+r.Z1)*.5f,half=r.Depth*.5f;
                for(int i=0;i<strips;i++)
                {
                    float a=-1+2*i/(float)strips,b=-1+2*(i+1)/(float)strips;
                    V3 v0=new V3(r.X0,y+p.Design.RoofRise(a),center+half*a),v1=new V3(r.X1,v0.Y,v0.Z);
                    V3 v3=new V3(r.X0,y+p.Design.RoofRise(b),center+half*b),v2=new V3(r.X1,v3.Y,v3.Z);
                    o.Glass.Quad(v0,v1,v2,v3,V3.Cross(v3-v0,v1-v0).Unit,Surface.Plaster);
                    foreach(float x in new[]{r.X0,r.X1,(r.X0+r.X1)*.5f})
                        ZoneShapes.Beam(o.Decoration,new V3(x,v0.Y,v0.Z),new V3(x,v3.Y,v3.Z),.085f,Surface.Metal,1,4);
                }
                foreach(float z in new[]{r.Z0,r.Z1})
                {
                    B(o.Structure,(r.X0+r.X1)*.5f,z,r.Width,.15f,y-.25f,y+.3f,Surface.Metal);
                    // Small clear clerestories seal the raised glazing's edges, instead of a floating roof.
                    o.Glass.Quad(new V3(r.X0,y,z),new V3(r.X1,y,z),new V3(r.X1,y+.15f,z),new V3(r.X0,y+.15f,z),new V3(0,0,z<center?-1:1),Surface.Plaster);
                }
                foreach(float x in new[]{r.X0,r.X1})
                {
                    int n=Math.Max(2,strips);
                    for(int i=0;i<n;i++)
                    {
                        float a=-1+2*i/(float)n,b=-1+2*(i+1)/(float)n;
                        o.Glass.Quad(new V3(x,y,center+half*a),new V3(x,y,center+half*b),
                            new V3(x,y+p.Design.RoofRise(b),center+half*b),new V3(x,y+p.Design.RoofRise(a),center+half*a),new V3(x<0?-1:1,0,0),Surface.Plaster);
                    }
                }
            }
            int lights=Math.Max(4,p.Bays/2);
            for(int i=0;i<lights;i++)
            {
                int bay=Math.Min(p.Bays-1,(int)((i+.5f)*p.Bays/lights));var r=p.Design.RoofOpenings[bay];
                // Stay off the centre-line bridges so some direct sunlight reaches the ground court.
                o.Fixtures.Add(MallLighting.Sun(p,new V3(r.X0+r.Width*.25f,y-.2f,(r.Z0+r.Z1)*.5f-2)));
            }
        }
        public static void Column(RoomGeometry o,float x,float z,float height)
        {
            // Round stone shafts, metal base and capital bands give atrial columns a distinct silhouette.
            Shapes.Column(o.Structure,x,z,.38f,0,height,12,Surface.Plaster,1.2f);
            Shapes.Column(o.Decoration,x,z,.45f,0,.18f,12,Surface.Metal,1);
            for(float y=5.1f;y<height;y+=MallPlan.FloorHeight)
            {Shapes.Column(o.Decoration,x,z,.43f,y,y+.15f,12,Surface.Accent,1);Shapes.Column(o.Decoration,x,z,.49f,y+.15f,y+.22f,12,Surface.Metal,1);}
        }
        public static void ShellDetail(RoomGeometry o,MallPlan p)
        {
            // Overhang fascias follow the actual outline of each upper-level court.
            for(int f=1;f<p.Floors;f++)foreach(var e in p.Design.Edges(f))
            {
                V3 a=e.A+V3.Up*(f*MallPlan.FloorHeight-.12f),b=e.B+V3.Up*(f*MallPlan.FloorHeight-.12f);
                ZoneShapes.Beam(o.Decoration,a,b,.12f,Surface.Plaster,1,4);
            }
        }
        public static void Sector(MallSectorGeometry result,MallPlan p,int floor,int sector,int quality)
        {
            var o=result.Geometry;float x=p.SectorCenter(sector),y=floor*MallPlan.FloorHeight;
            var condition=MallArchitectureV20.Condition(p,floor,sector);
            foreach(var s in p.Shops)if(s.Floor==floor&&s.Sector==sector)
            {
                // Detailed storefront stays shallow: nobody can snag on the shop's central entrance.
                var detail=new MeshData();float door=s.Kind==MallShopKind.Hypermarket?4:3;
                foreach(int side in new[]{-1,1})
                {
                    B(detail,side*(door+.16f),-.12f,.09f,.08f,.3f,3.25f,Surface.Metal);
                    B(detail,side*(s.Width*.5f-.4f),-.17f,.32f,.2f,0,3.3f,Surface.Accent);
                    for(int bolt=0;bolt<3;bolt++)B(detail,side*(door+.17f),-.17f,.03f,.018f,.55f+bolt*1.05f,.585f+bolt*1.05f,Surface.Metal);
                }
                B(detail,0,-.11f,door*2,.65f,.002f,.012f,Surface.Slide);
                for(int rib=-8;rib<=8;rib++)B(detail,rib*door/9,-.12f,.015f,.6f,.012f,.016f,Surface.Metal);
                detail.TranslateRotate(new V3(s.X,y,s.Side*MallPlan.ShopFront),s.Side==1?0:Shapes.Pi);o.Decoration.Append(detail);
            }
            if(sector>0&&sector<=p.Bays)
            {
                foreach(int side in new[]{-1,1})
                {
                    // Stone inlay at plazas, expansion joints, trash-bin rim and slot grilles.
                    B(o.Decoration,x,side*19.2f,15.5f,.08f,y+.004f,y+.012f,Surface.Accent);
                    B(o.Decoration,x-7.85f,side*18,.018f,7,y+.003f,y+.008f,Surface.Metal);
                    B(o.Decoration,x+6.5f,side*14.8f,.49f,.49f,y+.82f,y+.88f,Surface.Metal);
                    B(o.Decoration,x+6.5f,side*14.8f,.31f,.31f,y+.88f,y+.89f,Surface.Slide);
                    if(!MallRealism.Enabled(p))MallFurnishingV21.Plant(o.Decoration,x+5,side*14.8f,y+.9f,quality);
                }
                // Floor-wide ambience, not another per-frame behaviour or another Light.
                if(floor==p.Floors-1&&sector%3==0)
                {
                    string text="GALERIAS "+(floor+1);
                    foreach(int side in new[]{-1,1})MallGlyphs.Write(o.Decoration,text,new V3(x-3,y+3.9f,side*20.8f),.075f);
                }
            }
            if(floor==0&&sector==p.Design.PlazaSector&&Math.Abs(x)>14)
            {
                // Public food-court island beneath a wide opening; edge galleries remain clear.
                foreach(int side in new[]{-1,1})
                {MallFurnishingV21.CourtTable(o,x+side*3.6f,5.4f,y,quality);MallFurnishingV21.CourtTable(o,x+side*3.6f,-5.4f,y,quality);}
            }
            if(sector==0||sector==p.Bays+1)
            {
                MallFurnishingV21.Directory(o,p,x+5,-27,y,quality);
                MallFurnishingV21.ATM(o,x-6,28,y,quality);MallFurnishingV21.ATM(o,x-4,28,y,quality);
                foreach(int side in new[]{-1,1})
                {
                    // End-wall cladding and an open side passage to the stair plazas.
                    B(o.Decoration,x+side*10,32,.15f,8,y+.18f,y+4.7f,Surface.Accent);
                    for(int line=0;line<6;line++)B(o.Decoration,x+side*10-.1f,29+line*1.2f,.02f,.06f,y+.2f,y+4.6f,Surface.Metal);
                }
            }
            if(condition==MallCondition.Abandoned)
            {
                // Dry scuffs and a loose ceiling tile only beside walls, away from walking routes.
                foreach(int side in new[]{-1,1})B(o.Decoration,x+6.5f,side*21.55f,.9f,.8f,y+.006f,y+.024f,Surface.Plaster);
            }
        }
    }
}
