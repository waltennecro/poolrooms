using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public readonly struct MallLooseProp
    {
        public readonly int Id,ItemId;
        public readonly float Yaw;
        public readonly bool Hanging;
        public readonly V3 Position;
        public MallLooseProp(int id,V3 position){Id=id;Position=position;ItemId=-1;Yaw=0;Hanging=false;}
        public MallLooseProp(int id,V3 position,int itemId,float yaw,bool hanging=false){Id=id;Position=position;ItemId=itemId;Yaw=yaw;Hanging=hanging;}
    }
    public sealed class MallSectorGeometry
    {
        public readonly RoomGeometry Geometry=new RoomGeometry();
        public readonly List<MallLooseProp> Pickups=new List<MallLooseProp>();
    }
    public static class MallArchitecture
    {
        public static RoomGeometry Shell(MallPlan plan,float connectedDoorWidth=0,float connectedDoorHeight=4.8f)
        {
            var o=NewGeometry();var m=o.Structure;float half=plan.Width*.5f,deep=plan.Depth*.5f,height=plan.Floors*MallPlan.FloorHeight;
            for(int f=0;f<plan.Floors;f++)
            {
                float y=f*MallPlan.FloorHeight;
                foreach(var rect in plan.FloorPlates(f))Box(m,(rect.X0+rect.X1)*.5f,(rect.Z0+rect.Z1)*.5f,rect.Width,rect.Depth,y-.25f,y,Surface.Tile);
                if(f>0)
                {
                    if(plan.Refined)MallArchitectureV21.Balconies(o,plan,f);
                    else
                    {
                        Rail(m,new V3(-plan.ShopHalf,y,-12),new V3(plan.ShopHalf,y,-12));Rail(m,new V3(-plan.ShopHalf,y,12),new V3(plan.ShopHalf,y,12));
                        Rail(m,new V3(-plan.ShopHalf,y,-12),new V3(-plan.ShopHalf,y,12));Rail(m,new V3(plan.ShopHalf,y,-12),new V3(plan.ShopHalf,y,12));
                    }
                    foreach(int side in new[]{-1,1})
                    {
                        var h=plan.StairHole(side);
                        Rail(m,new V3(h.X0,y,h.Z0),new V3(h.X0,y,h.Z1));Rail(m,new V3(h.X1,y,h.Z0),new V3(h.X1,y,h.Z1));
                        Rail(m,new V3(h.X0,y,h.Z1),new V3(h.X1,y,h.Z1));
                    }
                }
                if(f<plan.Floors-1)foreach(int side in new[]{-1,1})
                {Stairs(o,plan,f,side);if(plan.Enhanced)MallArchitectureV20.Escalators(o,plan,f,side);}
            }
            Box(m,0,-deep,plan.Width,.4f,-.25f,height,Surface.Plaster);Box(m,0,deep,plan.Width,.4f,-.25f,height,Surface.Plaster);
            if(connectedDoorWidth<=0)
            {Box(m,-half,0,.4f,plan.Depth,0,height,Surface.Plaster);Box(m,half,0,.4f,plan.Depth,0,height,Surface.Plaster);}
            else foreach(int side in new[]{-1,1})
            {
                float z0=18-connectedDoorWidth*.5f,z1=18+connectedDoorWidth*.5f;
                Box(m,side*half,(-deep+z0)*.5f,.4f,z0+deep,0,height,Surface.Plaster);
                Box(m,side*half,(z1+deep)*.5f,.4f,deep-z1,0,height,Surface.Plaster);
                Box(m,side*half,18,.4f,connectedDoorWidth,connectedDoorHeight,height,Surface.Plaster);
            }
            if(plan.Refined)MallArchitectureV21.Roof(o,plan);
            else if(plan.Enhanced)MallArchitectureV20.Roof(o,plan);
            else Box(m,0,0,plan.Width,plan.Depth,height,height+.25f,Surface.Plaster);
            foreach(var s in plan.Shops)
            {
                if(plan.Enhanced)MallArchitectureV20.Storefront(o,s);
                var wall=new MeshData();
                if(!plan.Enhanced)ZoneShapes.Portal(wall,0,0,0,s.Width-.15f,MallPlan.FloorHeight-.25f,0,s.Kind==MallShopKind.Hypermarket?8:6,3.5f,.18f,3,12,1.2f);
                wall.TranslateRotate(new V3(s.X,s.Floor*MallPlan.FloorHeight,s.Side*MallPlan.ShopFront),0);m.Append(wall);
                foreach(int edge in new[]{-1,1})Box(m,s.X+edge*s.Width*.5f,s.Side*(MallPlan.ShopFront+s.Depth*.5f),.18f,s.Depth,s.Floor*MallPlan.FloorHeight,(s.Floor+1)*MallPlan.FloorHeight-.25f,Surface.Plaster);
            }
            for(float x=-plan.ShopHalf;x<=plan.ShopHalf;x+=MallPlan.BayWidth)foreach(int side in new[]{-1,1})
            {if(plan.Refined)MallArchitectureV21.Column(o,x,side*14,height);
             else{Box(m,x,side*14,.6f,.6f,0,height,Surface.Plaster);Box(o.Decoration,x,side*14,.67f,.67f,0,.25f,Surface.Accent);}}
            // Luminous roof strips suggest the skylights in the supplied mall photographs.
            if(!plan.Enhanced)for(float x=-plan.ShopHalf+6;x<plan.ShopHalf;x+=16)Box(o.Decoration,x,0,8,14,height-.05f,height,Surface.Glow);
            if(plan.Enhanced)MallArchitectureV20.ShellDetail(o,plan);
            if(plan.Refined)MallArchitectureV21.ShellDetail(o,plan);
            if(plan.Enhanced)SetMallTheme(o);
            return o;
        }
        internal static void SetMallTheme(RoomGeometry o)
        {o.Structure.Theme=o.Decoration.Theme=SurfaceTheme.Mall;o.Glass.Theme=SurfaceTheme.Glass;}
        private static RoomGeometry NewGeometry()
        {
            var o=new RoomGeometry();o.Structure.RetailPalette=o.Decoration.RetailPalette=1;
            o.Structure.PaletteVariant=o.Decoration.PaletteVariant=3;
            o.Decoration.EmissionTint=new V3(1,.91f,.76f);o.Decoration.EmissionStrength=.07f;return o;
        }
        public static MallSectorGeometry Sector(MallPlan plan,int floor,int sector,int quality)
        {
            if(floor<0||floor>=plan.Floors||sector<0||sector>=plan.SectorCount)throw new ArgumentOutOfRangeException();
            var result=new MallSectorGeometry();var o=result.Geometry;
            o.Structure.RetailPalette=o.Decoration.RetailPalette=1;o.Structure.PaletteVariant=o.Decoration.PaletteVariant=3;
            o.Decoration.EmissionTint=new V3(1,.9f,.74f);o.Decoration.EmissionStrength=.04f;
            float x=plan.SectorCenter(sector),y=floor*MallPlan.FloorHeight;
            foreach(var store in plan.Shops)if(store.Floor==floor&&store.Sector==sector)
            {
                var collision=new MeshData();var detail=new MeshData();if(store.Program!=null)store.Program.Build(collision,detail,quality);
                else MallFurnishing.Store(collision,detail,store,quality,!plan.Enhanced);
                if(plan.Refined&&store.Program==null)MallFurnishingV21.Store(collision,detail,o.Glass,store,quality);
                if(store.FurnishingMirror<0){MallSpatialDesign.Mirror(collision);MallSpatialDesign.Mirror(detail);}
                collision.TranslateRotate(new V3(store.X,y,store.Side*MallPlan.ShopFront),store.Side==1?0:Shapes.Pi);
                detail.TranslateRotate(new V3(store.X,y,store.Side*MallPlan.ShopFront),store.Side==1?0:Shapes.Pi);
                o.Structure.Append(collision);o.Decoration.Append(detail);
                string shopLabel=plan.Enhanced?MallArchitectureV20.ShopLabel(store):store.Name;
                var sign=new MeshData();Box(sign,0,.03f,Math.Min(store.Width-1,14),.2f,3.55f,4.5f,Surface.Slide);
                float letter=plan.Enhanced?Math.Min(.08f,12f/Math.Max(1,shopLabel.Length*6)):.08f;
                MallGlyphs.Write(sign,shopLabel,new V3(plan.Enhanced?-shopLabel.Length*3*letter:-shopLabel.Length*.28f,3.72f,-.09f),letter);
                sign.TranslateRotate(new V3(store.X,y,store.Side*MallPlan.ShopFront),store.Side==1?0:Shapes.Pi);o.Decoration.Append(sign);
                // A guaranteed bag at the front counter, with a stable store identifier.
                if(store.Kind==MallShopKind.Hypermarket||store.Kind==MallShopKind.Cafe||store.Kind==MallShopKind.FoodCourt)
                    result.Pickups.Add(new MallLooseProp(store.Id,store.World(store.Kind==MallShopKind.Hypermarket?-10:3.8f,1.35f,store.Kind==MallShopKind.Hypermarket?3:3.8f)));
                o.Fixtures.Add(new Fixture{Position=store.World(0,4.6f,8),Width=2,Depth=.3f,Power=store.Kind==MallShopKind.Cinema?650:1900,Temperature=store.Kind==MallShopKind.Cafe?3200:4100,Range=22});
                if(store.Kind==MallShopKind.Hypermarket)o.Fixtures.Add(new Fixture{Position=store.World(-16,4.6f,15),Width=2,Depth=.3f,Power=2600,Temperature=4200,Range=28});
            }
            // Corridor detail belongs to this column, never an object per distant product.
            foreach(int side in new[]{-1,1})
            {
                float z=side*18;
                if(sector>0&&sector<=plan.Bays)
                {
                    if(plan.Refined)MallFurnishingV21.PublicBench(o,x-4,side*20.5f,0,y,quality);
                    else MallFurnishing.Bench(o.Structure,x-4,side*20.5f,0,y);
                    float plantZ=side*14.8f;
                    Box(o.Structure,x+5,plantZ,1,1,y,y+.9f,Surface.Accent);Box(o.Decoration,x+5,plantZ,.8f,.8f,y+.86f,y+.9f,Surface.Slide);
                    // Stylized leaf strips; no fungi or character decorations.
                    for(int i=0;i<5;i++)ZoneShapes.Beam(o.Decoration,new V3(x+5,y+.9f,plantZ),new V3(x+5+(i-2)*.2f,y+1.7f,plantZ+(i%2==0?.3f:-.3f)),.05f,Surface.Slide,1.2f,4);
                    Box(o.Structure,x+6.5f,plantZ,.45f,.45f,y,y+.85f,Surface.Metal);
                }
                Box(o.Decoration,x,z,2.2f,.8f,y+4.95f,y+5,Surface.Glow);
                Box(o.Decoration,x,z+side*3,1,.15f,y+4.93f,y+5,Surface.Metal);
                for(int blade=0;blade<4;blade++)Box(o.Decoration,x-.4f+blade*.26f,z+side*3,.07f,.3f,y+4.91f,y+4.93f,Surface.Slide);
                o.Fixtures.Add(new Fixture{Position=new V3(x,y+4.7f,z),Width=1.8f,Depth=.6f,Power=1700,Temperature=3500,Range=24});
            }
            if(sector==0||sector==plan.Bays+1)
            {
                // ATMs, directory stand and waiting seats in the end plazas, away from stair apertures.
                foreach(float z in new[]{-28f,22f,30f})
                    {if(plan.Refined)MallFurnishingV21.PublicBench(o,x,z,0,y,quality);else MallFurnishing.Bench(o.Structure,x,z,0,y);}
                for(int i=0;i<2;i++){Box(o.Structure,x-6+i*2,28,1,1,y,y+1.7f,Surface.Metal);Box(o.Decoration,x-6+i*2,27.45f,.65f,.08f,y+.9f,y+1.5f,Surface.Slide);}
                Box(o.Structure,x+5,-27,1.8f,.8f,y,y+1.8f,Surface.Metal);
                MallGlyphs.Write(o.Decoration,"NIVEL "+(floor+1),new V3(x+3,-.0f+y+1.4f,-27.5f),.075f);
            }
            if(floor==0&&sector==plan.SectorAt(0))
            {
                if(plan.Refined)MallFurnishingV21.DryFountain(o,quality);
                else
                {
                // An empty fountain basin is dry geometry, independent of Poolrooms HDRP water.
                foreach(int side in new[]{-1,1})
                {Box(o.Structure,0,side*3,8,.3f,0,.65f,Surface.Tile);Box(o.Structure,side*4,0,.3f,6,0,.65f,Surface.Tile);MallFurnishing.Bench(o.Structure,side*6,0,side*Shapes.Pi*.5f,0);}
                Box(o.Decoration,0,0,7.7f,5.7f,.001f,.015f,Surface.Slide);
                Box(o.Structure,0,0,1.2f,1.2f,0,1.3f,Surface.Plaster);
                MallGlyphs.Write(o.Decoration,"PLAZA 2010",new V3(-3,1.6f,-.8f),.09f);
                }
            }

            if(floor==0&&sector>0&&sector<=plan.Bays&&sector%3==0&&Math.Abs(x)>14)
            {
                // Small mobile-phone/accessory islands in the atrium's ground-floor plaza.
                Box(o.Structure,x,0,4,3,0,1.05f,Surface.Accent);Box(o.Decoration,x,0,4.2f,3.2f,1.05f,1.12f,Surface.Plaster);
                foreach(int side in new[]{-1,1}){Box(o.Structure,x+side*1.8f,0,.12f,.12f,1.1f,2.9f,Surface.Metal);Box(o.Decoration,x+side*.8f,-1,.5f,.4f,1.12f,1.18f,Surface.Slide);}
                Box(o.Decoration,x,0,4,.3f,2.9f,3.4f,Surface.Slide);MallGlyphs.Write(o.Decoration,"MOVIL",new V3(x-1,3,-.2f),.055f);
            }
            if(plan.Enhanced)
            {result.Pickups.Clear();SetMallTheme(o);MallArchitectureV20.Sector(result,plan,floor,sector,quality);
             if(plan.Refined)MallArchitectureV21.Sector(result,plan,floor,sector,quality);
             MallLighting.Sector(result,plan,floor,sector);}
            return result;
        }
        public static void Box(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface surface)
        {ZoneShapes.Box(m,x,z,w,d,bottom,top,surface,1.2f);}
        private static void Rail(MeshData m,V3 a,V3 b)
        {
            ZoneShapes.Beam(m,a+V3.Up*1.1f,b+V3.Up*1.1f,.05f,Surface.Metal,1.2f,4);
            ZoneShapes.Beam(m,a+V3.Up*.55f,b+V3.Up*.55f,.035f,Surface.Metal,1.2f,4);
            int count=Math.Max(1,(int)Math.Ceiling((b-a).Length/1.4f));
            for(int i=0;i<=count;i++){V3 p=a+(b-a)*(i/(float)count);ZoneShapes.Beam(m,p,p+V3.Up*1.1f,.04f,Surface.Metal,1.2f,4);}
            // Invisible gaps between posts cannot let the controller fall through: low solid curb.
            V3 mid=(a+b)*.5f;Box(m,mid.X,mid.Z,Math.Max(.14f,Math.Abs(b.X-a.X)),Math.Max(.14f,Math.Abs(b.Z-a.Z)),mid.Y,mid.Y+.2f,Surface.Accent);
        }
        private static void Stairs(RoomGeometry o,MallPlan plan,int floor,int side)
        {
            float y=floor*MallPlan.FloorHeight,x1=side*(plan.ShopHalf+7),x2=side*(plan.ShopHalf+13),rise=MallPlan.FloorHeight*.5f;
            Flight(o,x1,y,-6.1f,0,rise);Flight(o,x2,y+rise,-6.1f,Shapes.Pi,rise);
            Box(o.Structure,(x1+x2)*.5f,-.2f,Math.Abs(x2-x1)+3.4f,4,y+rise-.22f,y+rise,Surface.Tile);
            Box(o.Structure,x1,-12,3.4f,4,y-.22f,y,Surface.Tile);Box(o.Structure,x2,-12,3.4f,4,y+MallPlan.FloorHeight-.22f,y+MallPlan.FloorHeight,Surface.Tile);
            Rail(o.Structure,new V3(Math.Min(x1,x2)-1.8f,y+rise,1.8f),new V3(Math.Max(x1,x2)+1.8f,y+rise,1.8f));
        }
        private static void Flight(RoomGeometry o,float x,float y,float z,float yaw,float rise)
        {
            var physical=new MeshData();ZoneShapes.Ramp(physical,0,0,3.4f,7.8f,rise,0,1.2f);
            // Flat collision ramp under decorative treads avoids repeated controller snags.
            var detail=new MeshData();int count=18;
            for(int i=0;i<count;i++)Box(detail,0,-3.9f+(i+.5f)*7.8f/count,3.35f,7.8f/count,(i+.1f)*rise/count,(i+1)*rise/count,Surface.Accent);
            foreach(int side in new[]{-1,1})Rail(physical,new V3(side*1.78f,0,-3.9f),new V3(side*1.78f,rise,3.9f));
            physical.TranslateRotate(new V3(x,y,z),yaw);detail.TranslateRotate(new V3(x,y,z),yaw);o.Structure.Append(physical);o.Decoration.Append(detail);
        }
    }
}
