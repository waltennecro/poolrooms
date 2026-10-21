using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // This fit-out is opt-in at the pure core boundary. Runtime mall systems
    // enable it independently of region size; old fixtures keep their geometry.
    public static class MallRealism
    {
        public const int FinishCount=20,SourceStart=14,SourceCount=14;
        public static bool Enabled(MallPlan p){return p.Realistic;}
        public static bool IsTheme(SurfaceTheme t){return t>=SurfaceTheme.MallGallery&&t<=SurfaceTheme.MallHypermarket;}
        public static SurfaceTheme Theme(MallShopKind kind){return (SurfaceTheme)((int)SurfaceTheme.MallFashion+(int)kind);}
        // Palette indices match the audited, pinned native source finish catalogue.
        public static int Finish(SurfaceTheme theme,Surface slot,int variant=0)
        {
            if(!IsTheme(theme)||slot==Surface.Glow)return -1;
            variant=(variant%4+4)%4;
            if(slot==Surface.Metal)return theme==SurfaceTheme.MallHypermarket?19:variant==1?11:variant==2?12:10;
            if(theme==SurfaceTheme.MallGallery)return slot==Surface.Tile?(variant==1?2:1):slot==Surface.Plaster?0:slot==Surface.Accent?7:slot==Surface.Slide?14:10;
            var kind=(MallShopKind)((int)theme-(int)SurfaceTheme.MallFashion);
            if(slot==Surface.Plaster)return kind==MallShopKind.Cafe||kind==MallShopKind.FoodCourt?4:kind==MallShopKind.Cinema||kind==MallShopKind.Arcade?5:variant==1?3:0;
            if(slot==Surface.Accent)return kind==MallShopKind.Electronics||kind==MallShopKind.Pharmacy?10:kind==MallShopKind.Cafe||kind==MallShopKind.FoodCourt?18:kind==MallShopKind.Hypermarket?16:variant==2?9:7;
            if(slot==Surface.Slide)return kind==MallShopKind.Books?17:kind==MallShopKind.Restrooms?13:variant==3?15:14;
            switch(kind)
            {
                case MallShopKind.Cafe:case MallShopKind.FoodCourt:case MallShopKind.Restrooms:return 13;
                case MallShopKind.Books:case MallShopKind.Home:case MallShopKind.Fashion:return variant==2?8:6;
                case MallShopKind.Cinema:case MallShopKind.Arcade:return 14;
                case MallShopKind.Toys:case MallShopKind.Sports:return 16;
                default:return variant==3?2:1;
            }
        }
        public static void FitOut(MeshData physical,MeshData detail,MallShop shop)
        {
            float w=shop.Width,d=shop.Depth;
            // Continuous shell slabs own collision. This inset finish sits 6 mm
            // above them; no duplicate coplanar faces or tiny collision steps.
            MallArchitecture.Box(detail,0,(d+.2f)*.5f,w-.24f,d-.4f,.006f,.012f,Surface.Tile);
            foreach(int side in new[]{-1,1})
            {
                float x=side*(w*.5f-.14f);
                MallArchitecture.Box(detail,side*(w*.5f-.105f),d*.5f,.02f,d-.2f,.14f,4.9f,Surface.Plaster);
                MallArchitecture.Box(detail,x,d*.5f,.035f,d-.4f,.02f,.14f,Surface.Accent);
                // Wall reveal, shallow acoustic panels and flush service grille.
                for(float z=5;z<d-2;z+=7)
                {
                    MallArchitecture.Box(detail,x,z,.03f,3.8f,2.25f,3.55f,Surface.Accent);
                    for(int rib=0;rib<7;rib++)MallArchitecture.Box(detail,x-side*.025f,z-1.5f+rib*.5f,.025f,.025f,2.3f,3.5f,Surface.Metal);
                }
            }
            MallArchitecture.Box(detail,0,d+.175f,w-.24f,.025f,.14f,4.9f,Surface.Plaster);
            foreach(int side in new[]{-1,1})
                MallArchitecture.Box(detail,side*(w*.5f-1.3f),.7f,.9f,.35f,3.31f,3.32f,Surface.Glow);
            int ceiling=(int)((shop.Seed>>8)%6);
            // Six genuinely different ceiling constructions, not six colour names.
            // All details stay above 4.1 m and retain the same clear walking routes.
            if(ceiling==0)for(float z=3;z<d-1;z+=3)
                MallArchitecture.Box(detail,0,z,w-.8f,.065f,4.7f,4.85f,Surface.Accent);
            else if(ceiling==1)for(float x=-w*.5f+1;x<w*.5f;x+=2)
                MallArchitecture.Box(detail,x,d*.5f,.06f,d-1,4.75f,4.95f,Surface.Accent);
            else if(ceiling==2)for(float z=3;z<d-1;z+=6)foreach(int side in new[]{-1,1})
                ZoneShapes.Beam(detail,new V3(side*(w*.5f-.4f),4.2f,z),new V3(0,4.8f,z),.07f,Surface.Metal,1,8);
            else if(ceiling==3)for(float z=5;z<d-3;z+=7)
                foreach(int side in new[]{-1,1})Shapes.Column(detail,side*w*.25f,z,1.5f,4.85f,4.94f,24,Surface.Plaster,1);
            else if(ceiling==4)for(float z=3;z<d-1;z+=4)
                for(int rib=0;rib<4;rib++)MallArchitecture.Box(detail,0,z+rib*.12f,w-.9f,.035f,4.65f,4.8f,Surface.Accent);
            else for(float z=4;z<d-2;z+=8)
                MallArchitecture.Box(detail,0,z,w-.8f,.12f,4.25f,4.7f,Surface.Slide);
            // A suspended raft and visible recessed luminaires share their exact
            // positions with the real light description. Entrance headroom > 4 m.
            foreach(var point in MallRetailLighting.Grid(shop))
            {
                float x=point.X,z=point.Z;
                MallArchitecture.Box(detail,x,z,Math.Min(5,w*.35f),3.3f,5.02f,5.10f,Surface.Plaster);
                MallArchitecture.Box(detail,x,z,2.4f,.16f,5.005f,5.02f,Surface.Metal);
                MallArchitecture.Box(detail,x,z,2.28f,.11f,4.995f,5.005f,Surface.Glow);
            }
            if(shop.Kind==MallShopKind.Books||shop.Kind==MallShopKind.Fashion||shop.Kind==MallShopKind.Home)
                for(int panel=-2;panel<=2;panel++)MallArchitecture.Box(detail,panel*1.2f,d-.13f,.045f,.045f,.25f,4.0f,Surface.Accent);
        }
        public static bool Replaces(MallShop s,GeneratedAssembly a)
        {
            if(!s.Realistic)return false;
            if(a.Kind==AssemblyKind.Table&&(s.Kind==MallShopKind.Cafe||s.Kind==MallShopKind.FoodCourt))return a.Bounds.Width>=2.6f&&a.Bounds.Depth>=2.6f;
            return a.Kind==AssemblyKind.Bench||a.Kind==AssemblyKind.SeatGroup;
        }
        public static IEnumerable<SourceFurniturePlacement> AssemblySources(MallShop s,GeneratedAssembly a,int ordinal)
        {
            int start=1000000+s.Id*2048+ordinal*8;float angle=a.Turns*90;
            var list=new List<SourceFurniturePlacement>();
            if(a.Kind==AssemblyKind.Table)
            {
                int table=(a.Seed&1)==0?7:23,chair=table==7?8:22;
                list.Add(new SourceFurniturePlacement(start,table,a.Point(0,.014f,0),angle));
                foreach(int sign in new[]{-1,1})
                    list.Add(new SourceFurniturePlacement(start+list.Count,chair,a.Point(sign*.96f,0,0),angle+(sign<0?270:90)));
                list.Add(new SourceFurniturePlacement(start+4,11,a.Point(0,SourceFurniture.Dimensions(table).Y+.018f,0),angle));
                if((a.Seed&3)==1)
                {
                    list[list.Count-1]=new SourceFurniturePlacement(start+4,11,a.Point(-.2f,SourceFurniture.Dimensions(table).Y+.018f,0),angle);
                    list.Add(new SourceFurniturePlacement(start+5,26,a.Point(.2f,SourceFurniture.Dimensions(table).Y+.018f,0),angle));
                }
                if((a.Seed&3)==0)
                {
                    list[list.Count-1]=new SourceFurniturePlacement(start+4,11,a.Point(-.2f,SourceFurniture.Dimensions(table).Y+.018f,0),angle);
                    list.Add(new SourceFurniturePlacement(start+5,21,a.Point(.2f,SourceFurniture.Dimensions(table).Y+.018f,0),angle));
                }
            }
            else
            {
                int source=a.Bounds.Width>2.2f&&a.Bounds.Depth>1.2f?3:(a.Seed&3)==0?25:15;
                // The pouf is under 0.8 m; a narrow original bench envelope may be
                // shallower than that. Keep the authored bench in that case.
                var half=SourceFurniture.HalfExtents(source,angle,.02f);
                if(half.X*2>a.Bounds.Width||half.Z*2>a.Bounds.Depth)yield break;
                list.Add(new SourceFurniturePlacement(start,source,a.Point(0,.014f,0),angle));
            }
            foreach(var p in list)
            {
                V3 position=s.World(p.Bottom.X,p.Bottom.Y,p.Bottom.Z);
                float yaw=(s.Side>0?0:180)+p.Yaw*(s.FurnishingMirror<0?-1:1);
                yield return new SourceFurniturePlacement(p.Slot,p.Source,position,yaw);
            }
        }
        public static void AddSources(RoomGeometry geometry,MallPlan p,MallShop shop)
        {
            if(shop.Program==null)return;
            int ordinal=0;
            foreach(var assembly in shop.Program.Assemblies)
            {
                if(Replaces(shop,assembly))foreach(var source in AssemblySources(shop,assembly,ordinal))geometry.SourceSeats.Add(source);
                ordinal++;
            }
            // Rear presentation bays stay outside the central and transverse aisles.
            // Candidates are checked against the actual generated furniture triangles.
            if(shop.Kind==MallShopKind.Hypermarket)
            {
                foreach(int side in new[]{-1,1})
                {int source=side<0?19:20;geometry.SourceSeats.Add(new SourceFurniturePlacement(3000000+shop.Id*2+(side>0?1:0),source,shop.World(side*5,1.008f,1.63f),0));}
            }
            if(shop.Kind==MallShopKind.Cafe||shop.Kind==MallShopKind.FoodCourt)
            {
                geometry.SourceSeats.Add(new SourceFurniturePlacement(4000000+shop.Id*16,24,shop.World(4.8f,1.154f,5),shop.Side>0?0:180));
                geometry.SourceSeats.Add(new SourceFurniturePlacement(4000001+shop.Id*16,27,shop.World(shop.Width*.5f-1.5f,0,-1.1f),shop.Side>0?0:180));
            }
            int slot=2000000+shop.Id*16;
            foreach(int side in new[]{-1,1})
            {
                int source=shop.Kind==MallShopKind.Home?17:shop.Kind==MallShopKind.Shoes?16:shop.Kind==MallShopKind.Hypermarket?14:shop.Kind==MallShopKind.Accessories?18:-1;
                if(source<0)continue;
                float x=side*(shop.Width*.5f-2),z=shop.Depth-4;
                float y=source==16||source==17||source==18?1.024f:.014f;
                if(y>1)
                {
                    var plinth=new MeshData{Theme=Theme(shop.Kind),PaletteVariant=(int)(shop.Seed%4)};
                    MallArchitecture.Box(plinth,x,z,1.3f,1.2f,0,1.02f,Surface.Accent);
                    if(shop.FurnishingMirror<0)MallSpatialDesign.Mirror(plinth);
                    plinth.TranslateRotate(new V3(shop.X,shop.Floor*MallPlan.FloorHeight,shop.Side*MallPlan.ShopFront),shop.Side==1?0:Shapes.Pi);
                    // Plinth acceptance and item placement use the same footprint.
                    var support=new SourceFurniturePlacement(slot,16,shop.World(x,0,z),0);
                    if(!SourceFurniture.Clear(geometry.CollisionOnly,support,.15f))continue;
                    geometry.CollisionOnly.Append(plinth);geometry.VisualMeshes.Add(plinth);
                }
                var candidate=new SourceFurniturePlacement(slot++,source,shop.World(x,y,z),shop.Side>0?0:180);
                if(SourceFurniture.Clear(geometry.CollisionOnly,candidate)&&SourceFurniture.Clear(geometry.Structure,candidate)&&SourceFurniture.Clear(geometry.Glass,candidate))geometry.SourceSeats.Add(candidate);
            }
        }
    }
}
