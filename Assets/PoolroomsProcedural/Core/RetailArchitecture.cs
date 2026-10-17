using System;

namespace Poolrooms.Core
{
    public static class RetailArchitecture
    {
        public static RoomGeometry Build(Room room,LayoutConfig c)
        {
            if(c.MallDistricts)return MallArchitectureV20.Gateway(room,c);
            var plan=room.Plan;if(plan==null||plan.Store==null||!plan.Valid(room,c))throw new ArgumentException("Invalid supermarket plan.");
            var o=new RoomGeometry();o.Structure.RetailPalette=1;o.Decoration.RetailPalette=1;
            o.Structure.PaletteVariant=plan.Palette;o.Decoration.PaletteVariant=plan.Palette;
            ProceduralArchitecture.DryEnvelope(o,room,c);
            RetailStore store=plan.Store;float uv=c.TextureWorldSize;
            // Back-of-house partition with a real opening; no locked door or decorative dead end.
            var partition=new MeshData();ZoneShapes.Portal(partition,0,0,0,store.Width,room.Height,0,Math.Max(2.8f,c.DoorWidth),c.DoorHeight,c.WallThickness,3,c.CurveSegments/2,uv);
            V2 wp=store.World(0,store.StockBoundary);partition.TranslateRotate(new V3(wp.X,0,wp.Y),store.QuarterTurns*Shapes.Pi*.5f);o.Structure.Append(partition);
            Ceiling(o,room,c);
            int slots=0;foreach(PlannedModule m in plan.Modules)if(m.Kind==LiminalModule.Shelf||m.Kind==LiminalModule.StockRack||m.Kind==LiminalModule.ProduceIsland)slots++;
            int remaining=store.ProductLimit(c),ordinal=0;
            foreach(PlannedModule p in plan.Modules)
            {
                bool products=p.Kind==LiminalModule.Shelf||p.Kind==LiminalModule.StockRack||p.Kind==LiminalModule.ProduceIsland;
                int allowance=products?remaining/Math.Max(1,slots-ordinal):0;
                if(products){ordinal++;remaining-=allowance;}
                Module(o,p,c,store,allowance);
            }
            return o;
        }
        private static void Ceiling(RoomGeometry o,Room room,LayoutConfig c)
        {
            RetailStore store=room.Plan.Store;float height=room.Height-.3f,uv=c.TextureWorldSize;
            // Long fluorescent fittings, metal suspension and a finite ceiling grid.
            int nx=Math.Max(2,Math.Min(4,(int)(store.Width/7))),nz=Math.Max(3,Math.Min(5,(int)(store.Depth/6)));
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
            {
                float px=(x+.5f)*store.Width/nx-store.Width*.5f,pz=(z+.5f)*store.Depth/nz-store.Depth*.5f;
                var lamp=new MeshData();ZoneShapes.Box(lamp,px,pz,.26f,2.3f,height-.05f,height+.02f,Surface.Plaster,uv);
                ZoneShapes.Box(lamp,px,pz,.14f,2.15f,height-.08f,height-.05f,Surface.Glow,uv);
                lamp.TranslateRotate(new V3(),store.QuarterTurns*Shapes.Pi*.5f);o.Structure.Append(lamp);
                V2 pos=store.World(px,pz);bool stock=pz>store.StockBoundary;
                o.Fixtures.Add(new Fixture{Position=new V3(pos.X,height-.11f,pos.Y),Width=store.QuarterTurns==0?.14f:2.15f,Depth=store.QuarterTurns==0?2.15f:.14f,Power=stock?520:760,Temperature=4200});
            }
            var grid=new MeshData();
            for(float x=-store.Width*.5f+1.5f;x<store.Width*.5f;x+=2.4f)ZoneShapes.Box(grid,x,0,.05f,store.Depth,room.Height-.07f,room.Height,Surface.Metal,uv);
            for(float z=-store.Depth*.5f+1.5f;z<store.Depth*.5f;z+=2.4f)ZoneShapes.Box(grid,0,z,store.Width,.05f,room.Height-.07f,room.Height,Surface.Metal,uv);
            if(store.Variant==RetailVariant.Wholesale)
                for(int side=-1;side<=1;side+=2)ZoneShapes.Box(grid,side*store.Width*.26f,0,.18f,store.Depth,room.Height-.4f,room.Height,Surface.Metal,uv);
            grid.TranslateRotate(new V3(),store.QuarterTurns*Shapes.Pi*.5f);o.Structure.Append(grid);
        }
        private static void Module(RoomGeometry o,PlannedModule p,LayoutConfig c,RetailStore store,int allowance)
        {
            var m=new MeshData();var decor=new MeshData();float w=p.LocalWidth,d=p.LocalDepth,h=p.Rise,uv=c.TextureWorldSize;
            var random=new StableRandom(p.Seed);
            switch(p.Kind)
            {
                case LiminalModule.Shelf:case LiminalModule.StockRack:
                    bool rack=p.Kind==LiminalModule.StockRack;int levels=rack?3:4;
                    ZoneShapes.Box(m,0,0,.1f,d,.12f,h,Surface.Metal,uv);
                    for(int end=-1;end<=1;end+=2)ZoneShapes.Box(m,0,end*(d*.5f-.055f),w,.11f,0,h,Surface.Accent,uv);
                    for(int level=0;level<levels;level++)
                    {
                        float y=.15f+level*(h-.3f)/(levels-1);
                        ZoneShapes.Box(m,0,0,w,d,y-.06f,y,Surface.Metal,uv);
                        for(int side=-1;side<=1;side+=2)
                            ZoneShapes.Box(decor,side*(w*.5f-.025f),0,.04f,d,y-.03f,y+.025f,Surface.Plaster,uv);
                    }
                    Products(decor,p,store,allowance,levels,ref random,uv);break;
                case LiminalModule.RefrigeratedCase:
                    ZoneShapes.Box(m,0,0,w,d,0,h,Surface.Plaster,uv);
                    // Satin reflective panels and frames. No transparent screen-space shader per cabinet.
                    for(int side=-1;side<=1;side+=2)
                    {
                        ZoneShapes.Box(m,side*(w*.5f+.002f),0,.025f,d-.12f,.3f,h-.12f,Surface.Slide,uv);
                        for(int door=0;door<2;door++)
                        {
                            float z=(door-.5f)*(d-.12f)*.5f;
                            ZoneShapes.Box(m,side*(w*.5f+.04f),z,.06f,.055f,.8f,1.35f,Surface.Metal,uv);
                        }
                        ZoneShapes.Box(decor,side*(w*.5f+.015f),0,.035f,d-.1f,.09f,.15f,Surface.Metal,uv);
                    }
                    break;
                case LiminalModule.Checkout:
                    ZoneShapes.Box(m,0,0,w,d,0,.88f,Surface.Accent,uv);
                    ZoneShapes.Box(m,0,-d*.12f,w-.12f,d*.62f,.88f,.91f,Surface.Metal,uv);
                    ZoneShapes.Box(m,0,d*.3f,w,d*.3f,.88f,h,Surface.Plaster,uv);
                    ZoneShapes.Box(decor,0,d*.25f,.38f,.3f,h,h+.15f,Surface.Metal,uv);
                    ZoneShapes.Box(decor,0,d*.34f,.04f,.08f,h+.12f,h+.45f,Surface.Metal,uv);
                    ZoneShapes.Box(decor,0,d*.37f,.32f,.06f,h+.35f,h+.55f,Surface.Slide,uv);
                    break;
                case LiminalModule.ProduceIsland:
                    ZoneShapes.Box(m,0,0,w,d,0,h,Surface.Accent,uv);
                    ZoneShapes.Box(m,0,0,w-.12f,d-.12f,h,h+.05f,Surface.Metal,uv);
                    for(int side=-1;side<=1;side+=2)ZoneShapes.Box(m,side*(w*.5f-.04f),0,.08f,d,h,h+.15f,Surface.Plaster,uv);
                    for(int end=-1;end<=1;end+=2)ZoneShapes.Box(m,0,end*(d*.5f-.04f),w,.08f,h,h+.15f,Surface.Plaster,uv);
                    for(int i=0;i<(int)(allowance*store.StockDensity);i++)
                        ZoneShapes.Box(decor,random.Range(-w*.3f,w*.3f),random.Range(-d*.35f,d*.35f),.16f,.16f,h+.05f,h+.18f,(i&1)==0?Surface.Accent:Surface.Plaster,uv);
                    break;
                case LiminalModule.ServiceCounter:
                    ZoneShapes.Box(m,0,0,w,d,0,.9f,Surface.Plaster,uv);
                    ZoneShapes.Box(m,0,0,w+.06f,d,.9f,1,Surface.Metal,uv);
                    ZoneShapes.Box(decor,0,d*.3f,w-.1f,.18f,1,h+.3f,Surface.Slide,uv);break;
                case LiminalModule.CartBay:
                    // One simplified static trolley; no moving physics objects or mesh per wire.
                    ZoneShapes.Box(m,0,0,w*.8f,d*.8f,0,.22f,Surface.Metal,uv);
                    ZoneShapes.Box(m,0,0,w*.75f,d*.65f,.4f,.5f,Surface.Metal,uv);
                    for(int side=-1;side<=1;side+=2)ZoneShapes.Box(m,side*w*.36f,0,.05f,d*.65f,.45f,h,Surface.Metal,uv);
                    ZoneShapes.Box(m,0,d*.31f,w*.75f,.06f,.45f,h,Surface.Metal,uv);
                    ZoneShapes.Box(m,0,-d*.31f,w*.75f,.06f,.45f,h*.85f,Surface.Metal,uv);
                    for(int side=-1;side<=1;side+=2)ZoneShapes.Box(m,side*w*.37f,-d*.42f,.07f,.07f,.2f,h,Surface.Metal,uv);
                    ZoneShapes.Box(m,0,-d*.42f,w*.8f,.07f,h-.08f,h,Surface.Accent,uv);break;
            }
            V3 offset=new V3(p.X,0,p.Z);float yaw=p.QuarterTurns*Shapes.Pi*.5f;
            m.TranslateRotate(offset,yaw);decor.TranslateRotate(offset,yaw);o.Structure.Append(m);o.Decoration.Append(decor);
        }
        private static void Products(MeshData mesh,PlannedModule p,RetailStore store,int allowance,int levels,ref StableRandom random,float uv)
        {
            int count=(int)(allowance*store.StockDensity);float w=p.LocalWidth,d=p.LocalDepth;
            for(int i=0;i<count;i++)
            {
                int level=i%levels,side=(i/levels)%2==0?-1:1;
                float y=.15f+level*(p.Rise-.3f)/(levels-1),width=Math.Min(.3f,w*.3f),height=random.Range(.18f,.32f);
                float z=random.Range(-d*.5f+.22f,d*.5f-.22f),x=side*w*.26f;
                Surface mat=p.Kind==LiminalModule.StockRack?Surface.Accent:(i%3==0?Surface.Accent:i%3==1?Surface.Plaster:Surface.Tile);
                ZoneShapes.Box(mesh,x,z,width,.24f,y,y+height,mat,uv);
                ZoneShapes.Box(mesh,x+side*width*.51f,z,.008f,.16f,y+height*.3f,y+height*.7f,Surface.Plaster,uv);
            }
        }
    }
}
