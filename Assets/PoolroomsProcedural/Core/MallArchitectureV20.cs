using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum MallCondition { Maintained, PartialLighting, Abandoned }
    public static class MallArchitectureV20
    {
        public static string ShopLabel(MallShop s)
        {string[] names={"AURORA","NOVA","NORTE","LUMEN","PASEO","ORBITA","METRO","ALAMEDA"};return s.Name+" "+names[s.Seed%8];}
        public static MallCondition Condition(MallPlan p,int floor,int sector)
        {
            // Neighbouring bays share a condition; a dark shop does not turn the whole mall dark.
            uint h=LiminalRegions.Hash(p.Seed,p.AddressX^floor,p.AddressZ^Math.Max(0,sector-1)/3,0xCA120u);
            return h%10<2?MallCondition.Abandoned:h%10<4?MallCondition.PartialLighting:MallCondition.Maintained;
        }
        public static MallRect Skylight(MallPlan p){return new MallRect(-p.ShopHalf+3,-10.5f,p.ShopHalf-3,10.5f);}
        public static void Roof(RoomGeometry o,MallPlan p)
        {
            float y=p.Floors*MallPlan.FloorHeight;MallRect hole=Skylight(p);
            foreach(var r in new MallRect(-p.Width*.5f,-p.Depth*.5f,p.Width*.5f,p.Depth*.5f).Subtract(hole))
                MallArchitecture.Box(o.Structure,(r.X0+r.X1)*.5f,(r.Z0+r.Z1)*.5f,r.Width,r.Depth,y,y+.25f,Surface.Plaster);
            o.Glass.Theme=SurfaceTheme.Glass;
            // Actual opening and non-shadow-casting glass, with a metal glazing grid.
            for(float x=hole.X0;x<hole.X1;x+=8)
            {
                float width=Math.Min(8,hole.X1-x);
                MallArchitecture.Box(o.Glass,x+width*.5f,0,width-.12f,20.8f,y+.1f,y+.125f,Surface.Plaster);
                MallArchitecture.Box(o.Structure,x,0,.12f,21,y-.18f,y+.38f,Surface.Metal);
            }
            foreach(float z in new[]{-10.5f,0,10.5f})MallArchitecture.Box(o.Structure,0,z,hole.Width,.14f,y-.16f,y+.4f,Surface.Metal);
            int lights=Math.Max(4,Math.Min(8,p.Bays/2));
            for(int i=0;i<lights;i++)
            {
                float x=hole.X0+(i+.5f)*hole.Width/lights;
                o.Fixtures.Add(MallLighting.Sun(p,new V3(x,y-.2f,-3)));
            }
        }
        public static void Storefront(RoomGeometry o,MallShop s)
        {
            var frame=new MeshData();var glass=new MeshData();float half=s.Width*.5f-.1f,door=s.Kind==MallShopKind.Hypermarket?4:3;
            foreach(int side in new[]{-1,1})
            {
                float width=half-door-.08f,center=side*(half+door)*.5f;
                MallArchitecture.Box(frame,center,0,width,.18f,0,.25f,Surface.Metal);
                MallArchitecture.Box(glass,center,0,width,.035f,.25f,3.4f,Surface.Plaster);
                foreach(float x in new[]{side*door,side*half})MallArchitecture.Box(frame,x,0,.075f,.18f,.2f,3.5f,Surface.Metal);
                for(float x=door+3.2f;x<half-.2f;x+=3.2f)MallArchitecture.Box(frame,side*x,0,.05f,.12f,.25f,3.5f,Surface.Metal);
            }
            MallArchitecture.Box(frame,0,0,s.Width-.15f,.18f,3.4f,MallPlan.FloorHeight-.25f,Surface.Plaster);
            MallArchitecture.Box(frame,0,0,s.Width-.1f,.22f,3.35f,3.45f,Surface.Metal);
            V3 position=new V3(s.X,s.Floor*MallPlan.FloorHeight,s.Side*MallPlan.ShopFront);
            frame.TranslateRotate(position,0);glass.TranslateRotate(position,0);o.Structure.Append(frame);o.Glass.Append(glass);
        }
        public static void ShellDetail(RoomGeometry o,MallPlan p)
        {
            // Balcony glass is backed by the existing collision curb and handrail.
            if(!p.Refined)for(int f=1;f<p.Floors;f++)foreach(int side in new[]{-1,1})
                MallArchitecture.Box(o.Glass,0,side*12,p.ShopHalf*2,.035f,f*MallPlan.FloorHeight+.22f,f*MallPlan.FloorHeight+1.03f,Surface.Plaster);
            for(int f=0;f<p.Floors;f++)
            {
                float y=f*MallPlan.FloorHeight;
                foreach(int side in new[]{-1,1})
                {
                    MallArchitecture.Box(o.Decoration,0,side*21.5f,p.ShopHalf*2,.25f,y+.008f,y+.015f,Surface.Metal);
                    // Bulkhead over galleries stops shops being lit by a roof panel in another floor.
                    MallArchitecture.Box(o.Decoration,0,side*18,p.ShopHalf*2,7,y+5.02f,y+5.09f,Surface.Plaster);
                    for(float x=-p.ShopHalf;x<p.ShopHalf;x+=8)
                        MallArchitecture.Box(o.Decoration,x,side*18,.07f,7,y+4.99f,y+5.02f,Surface.Metal);
                }
            }
        }
        public static void Escalators(RoomGeometry o,MallPlan p,int floor,int side)
        {
            float rise=MallPlan.FloorHeight*.5f;
            for(int flight=0;flight<2;flight++)
            {
                var trim=new MeshData();var glass=new MeshData();float y=floor*MallPlan.FloorHeight+flight*rise;
                float x=side*(p.ShopHalf+(flight==0?7:13)),yaw=flight==0?0:Shapes.Pi;
                foreach(int edge in new[]{-1,1})
                {
                    V3 a=new V3(edge*1.69f,0,-3.9f),b=new V3(edge*1.69f,rise,3.9f);
                    ZoneShapes.Beam(trim,a+V3.Up*.12f,b+V3.Up*.12f,.13f,Surface.Metal,1,4);
                    ZoneShapes.Beam(trim,a+V3.Up*1.03f,b+V3.Up*1.03f,.09f,Surface.Slide,1,8);
                    glass.Quad(a+V3.Up*.2f,b+V3.Up*.2f,b+V3.Up*.97f,a+V3.Up*.97f,new V3(edge,0,0),Surface.Plaster);
                }
                for(int step=0;step<18;step++)
                {
                    float z=-3.9f+(step+1)*7.8f/18,yTop=(step+1)*rise/18+.002f;
                    // Metal tread tops and yellow safety lips; stationary, no moving physics.
                    MallArchitecture.Box(trim,0,z-.22f,3.3f,.42f,yTop,yTop+.006f,Surface.Metal);
                    MallArchitecture.Box(trim,0,z-.012f,3.3f,.024f,yTop+.006f,yTop+.013f,Surface.Accent);
                    for(int rib=-6;rib<=6;rib++)
                    {
                        float a=rib*.24f-.006f,b=rib*.24f+.006f,treadY=yTop+.01f;
                        trim.Quad(new V3(a,treadY,z-.41f),new V3(b,treadY,z-.41f),new V3(b,treadY,z-.03f),new V3(a,treadY,z-.03f),V3.Up,Surface.Metal);
                    }
                }
                foreach(float z in new[]{-4.1f,4.1f})MallArchitecture.Box(trim,0,z,3.3f,.25f,z<0?.003f:rise+.003f,z<0?.018f:rise+.018f,Surface.Metal);
                trim.TranslateRotate(new V3(x,y,-6.1f),yaw);glass.TranslateRotate(new V3(x,y,-6.1f),yaw);
                o.Decoration.Append(trim);o.Glass.Append(glass);
            }
        }
        public static void Sector(MallSectorGeometry result,MallPlan p,int floor,int sector,int quality=12)
        {
            var o=result.Geometry;MallCondition condition=Condition(p,floor,sector);float y=floor*MallPlan.FloorHeight;
            o.Structure.Weathering=o.Decoration.Weathering=condition==MallCondition.Abandoned?1:0;
            o.Decoration.EmissionStrength=condition==MallCondition.Maintained?.12f:condition==MallCondition.PartialLighting?.065f:.018f;
            for(int i=0;i<o.Fixtures.Count;i++)
            {
                Fixture f=o.Fixtures[i];bool corridor=Math.Abs(f.Position.Z)<MallPlan.ShopFront;
                // Abandoned wings still receive skylight spill and a few functioning lamps.
                if(!corridor)
                {
                    foreach(var shop in p.Shops)if(shop.Floor==floor&&shop.Sector==sector&&Math.Abs(f.Position.X-shop.X)<.01f)
                    {f.Position=shop.World(0,4.6f,shop.Depth*.47f);f.Range=Math.Max(22,shop.Depth*.85f);break;}
                }
                f.Power*=condition==MallCondition.Maintained?2.35f:condition==MallCondition.PartialLighting?1.3f:corridor?.55f:.3f;
                f.Temperature=corridor?3900:f.Temperature;o.Fixtures[i]=f;
            }
            foreach(var s in p.Shops)if(s.Floor==floor&&s.Sector==sector)
            {
                var local=new MeshData();var detail=new MeshData();
                // Two actual supported merchandise tables. The 3-metre central aisle stays open.
                foreach(int side in new[]{-1,1})
                {
                    if(s.Refined)MallFurnishingV21.FrontDisplay(local,detail,s,side,quality);
                    else
                    {MallArchitecture.Box(local,side*5,1.95f,2.6f,.9f,.9f,1,Surface.Plaster);
                    foreach(int leg in new[]{-1,1})MallArchitecture.Box(local,side*5+leg*1.05f,1.95f,.07f,.75f,0,.9f,Surface.Metal);}
                }
                float height=MallPlan.FloorHeight-.35f;
                // 2010 suspended ceiling, slot vents, spot tracks and storefront display plinths.
                for(float z=1.2f;z<s.Depth;z+=2.4f)MallArchitecture.Box(detail,0,z,s.Width-.4f,.045f,height-.055f,height,Surface.Metal);
                for(float x=-s.Width*.5f+2.4f;x<s.Width*.5f;x+=2.4f)MallArchitecture.Box(detail,x,s.Depth*.5f,.045f,s.Depth,height-.055f,height,Surface.Metal);
                foreach(int side in new[]{-1,1})
                {
                    MallArchitecture.Box(local,side*(s.Width*.5f-1.3f),.7f,1.8f,1.1f,0,.3f,Surface.Accent);
                    MallArchitecture.Box(detail,side*(s.Width*.5f-1.3f),.7f,1.9f,1.2f,.3f,.33f,Surface.Plaster);
                }
                if(condition==MallCondition.Abandoned)
                {
                    // Raised shutter, stains and missing panel frames; never lock the doorway.
                    MallArchitecture.Box(detail,0,.16f,Math.Min(6,s.Width-1),.16f,2.75f,3.38f,Surface.Metal);
                    for(int row=0;row<6;row++)MallArchitecture.Box(detail,0,.06f,Math.Min(6,s.Width-1),.06f,2.8f+row*.09f,2.815f+row*.09f,Surface.Slide);
                    for(int i=0;i<7;i++)
                    {
                        float x=s.Width*.5f-.55f-(i%3)*.35f,z=5+(i*3.1f)%(s.Depth-7);
                        MallArchitecture.Box(detail,x,z,.3f,.5f,.006f,.012f,Surface.Slide);
                    }
                    foreach(int side in new[]{-1,1})MallArchitecture.Box(detail,side*(s.Width*.5f-.12f),s.Depth*.5f,.02f,s.Depth-.3f,.15f,3.15f,Surface.Plaster);
                    // Visible ductwork above one open ceiling bay.
                    MallArchitecture.Box(detail,-4,s.Depth*.6f,.45f,3.8f,4.9f,5.02f,Surface.Metal);
                }
                if(s.FurnishingMirror<0){MallSpatialDesign.Mirror(local);MallSpatialDesign.Mirror(detail);}
                local.TranslateRotate(new V3(s.X,y,s.Side*MallPlan.ShopFront),s.Side==1?0:Shapes.Pi);
                detail.TranslateRotate(new V3(s.X,y,s.Side*MallPlan.ShopFront),s.Side==1?0:Shapes.Pi);
                o.Structure.Append(local);o.Decoration.Append(detail);
                foreach(var item in MallMerchandise.ForShop(s,condition==MallCondition.Abandoned))result.Pickups.Add(item);
            }
            if(sector==0||sector==p.Bays+1)
            {
                float x=p.SectorCenter(sector);string label=condition==MallCondition.Abandoned?"ALA NORTE":"PLAZA CENTRAL";
                MallGlyphs.Write(o.Decoration,label,new V3(x-4,y+3.65f,32),.07f);
                MallGlyphs.Write(o.Decoration,"MODA TECNO CAFE",new V3(x-4,y+2.15f,-27.5f),.035f);
            }
        }
        public static RoomGeometry Gateway(Room room,LayoutConfig c)
        {
            var o=new RoomGeometry();MallArchitecture.SetMallTheme(o);o.Decoration.EmissionTint=new V3(.94f,.97f,1);o.Decoration.EmissionStrength=.2f;
            ProceduralArchitecture.DryEnvelope(o,room,c);
            foreach(int side in new[]{-1,1})
            {
                MallFurnishing.Bench(o.Structure,side*(room.Width*.5f-2.5f),room.MallGateway?room.Depth*.28f:0,side*Shapes.Pi*.5f,0);
                MallArchitecture.Box(o.Structure,side*(room.Width*.5f-1.5f),room.Depth*.25f,1,1,0,.8f,Surface.Accent);
                o.Fixtures.Add(new Fixture{Position=new V3(side*room.Width*.25f,room.Height-.3f,0),Width=2,Depth=1,Power=7000,Temperature=4300,Range=24});
            }
            MallGlyphs.Write(o.Decoration,"CENTRO COMERCIAL",new V3(-6,3.2f,-room.Depth*.5f+3.8f),.09f);
            MallGlyphs.Write(o.Decoration,"PLAZA 2010",new V3(-2.2f,2.6f,-room.Depth*.5f+3.8f),.065f);
            return o;
        }
    }
}
