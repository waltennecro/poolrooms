using System;

namespace Poolrooms.Core
{
    // All fixtures are sector meshes. Small silhouettes and joinery are render detail;
    // supports, seats and large furniture retain simple walkable collision geometry.
    public static class MallFurnishingV21
    {
        private static void B(MeshData m,float x,float z,float w,float d,float a,float b,Surface s=Surface.Accent)
        {MallArchitecture.Box(m,x,z,w,d,a,b,s);}
        private static void Beam(MeshData m,V3 a,V3 b,float w,Surface s=Surface.Metal,int n=4)
        {ZoneShapes.Beam(m,a,b,w,s,1,n);}
        private static void Rounded(MeshData m,float x,float z,float w,float d,float radius,float a,float b,Surface s,int corners=2)
        {
            var part=new MeshData();var contour=Shapes.RoundedRect(w,d,radius,corners);
            Shapes.Skirt(part,contour,a,b,s,1);Shapes.Fill(part,contour,b,s,1);Shapes.Fill(part,contour,a,s,1,false);
            part.TranslateRotate(new V3(x,0,z),0);m.Append(part);
        }
        public static void FrontDisplay(MeshData physical,MeshData detail,MallShop shop,int side,int quality=12)
        {
            float x=side*5,z=1.95f;int style=(int)((shop.Seed>>4)%3);
            // All variants share the same top and product-support contract.
            B(physical,x,z,2.6f,.9f,.9f,1,Surface.Plaster);
            if(shop.Kind==MallShopKind.Cafe||shop.Kind==MallShopKind.FoodCourt||shop.Kind==MallShopKind.Pharmacy||style==0)
            {
                B(physical,x,z,2.36f,.72f,.08f,.9f,shop.Kind==MallShopKind.Pharmacy?Surface.Plaster:Surface.Accent);
                B(detail,x,z,2.45f,.8f,.03f,.08f,Surface.Metal);
                foreach(int sign in new[]{-1,1})
                {
                    B(detail,x+sign*.6f,z-.367f,1.08f,.015f,.13f,.83f,Surface.Accent);
                    B(detail,x+sign*.6f,z-.381f,.25f,.028f,.65f,.67f,Surface.Metal);
                }
            }
            else
            {
                foreach(int leg in new[]{-1,1})
                {
                    B(physical,x+leg*1.05f,z,.07f,.75f,0,.9f,Surface.Metal);
                    B(detail,x+leg*1.05f,z,.27f,.8f,.015f,.06f,Surface.Metal);
                }
                B(detail,x,z,2.17f,.06f,.23f,.29f,Surface.Metal);
                if(style==2)B(physical,x,z,2.35f,.75f,.32f,.38f,Surface.Accent);
            }
            // Labels are on the vertical front edge, never on top of a selectable article.
            B(detail,x,z-.459f,2.52f,.016f,.91f,.982f,Surface.Metal);
            for(int i=-1;i<=1;i++)
            {
                B(detail,x+i*.8f,z-.47f,.21f,.012f,.93f,.972f,Surface.Plaster);
                if(quality>12&&shop.Kind!=MallShopKind.Restrooms)Label(detail,"2010",new V3(x+i*.8f-.09f,.936f,z-.478f),.007f,shop.FurnishingMirror);
            }
        }
        public static void Store(MeshData physical,MeshData detail,MeshData worldGlass,MallShop shop,int quality)
        {
            bool fine=quality>12;float w=shop.Width,d=shop.Depth;var glass=new MeshData();
            // Rear wall and perimeter details sit inside the actual walls, not across merchandise aisles.
            B(detail,0,d-.025f,w-.4f,.025f,.2f,3.65f,shop.Kind==MallShopKind.Cinema?Surface.Slide:Surface.Plaster);
            B(detail,0,d-.05f,w-.4f,.04f,.13f,.22f,Surface.Metal);
            foreach(int side in new[]{-1,1})
            {
                float x=side*(w*.5f-.23f);
                for(float z=3;z<d-2;z+=6)
                {
                    B(detail,x,z,.012f,2.7f,2.2f,3.45f,shop.Kind==MallShopKind.Fashion||shop.Kind==MallShopKind.Home?Surface.Accent:Surface.Slide);
                    if(fine)for(int frame=0;frame<3;frame++)B(detail,x-side*.015f,z-1+frame,.013f,.045f,2.2f,3.45f,Surface.Metal);
                }
                float px=side*(w*.5f-1.3f);
                // Window displays: empty stepped architectural stands, not inaccessible duplicate products.
                Rounded(detail,px,.7f,1.7f,1,.15f,.335f,.46f,Surface.Plaster);
                B(detail,px,.7f,.12f,.12f,.46f,1.55f,Surface.Metal);
                B(detail,px,.7f,.8f,.55f,1.5f,1.57f,Surface.Accent);
                glass.Quad(new V3(px-.84f,.35f,.12f),new V3(px+.84f,.35f,.12f),new V3(px+.84f,1.05f,.12f),new V3(px-.84f,1.05f,.12f),new V3(0,0,-1),Surface.Plaster);
            }
            switch(shop.Kind)
            {
                case MallShopKind.Fashion:
                    for(float z=6;z<d-7;z+=5)foreach(int side in new[]{-1,1})
                    {
                        float x=side*4.6f;
                        foreach(int end in new[]{-1,1})
                        {B(detail,x,z+end*1.8f,.65f,.3f,.015f,.06f,Surface.Metal);B(detail,x,z+end*1.8f,.07f,.07f,1.8f,1.85f,Surface.Metal);}
                        for(int i=0;i<4;i++)
                        {
                            float at=z-1.35f+i*.9f;
                            Beam(detail,new V3(x-.23f,1.60f,at),new V3(x,1.72f,at),.012f,Surface.Metal);
                            Beam(detail,new V3(x,1.72f,at),new V3(x+.23f,1.60f,at),.012f,Surface.Metal);
                        }
                    }
                    // Wall mirrors, changing-room architraves and softly folded curtains.
                    foreach(int side in new[]{-1,1})
                    {
                        float x=side*(w*.5f-.11f);
                        B(detail,x,d-4,.05f,2.1f,.45f,2.5f,Surface.Metal);
                        B(detail,x-side*.03f,d-4,.02f,1.85f,.57f,2.38f,Surface.Plaster);
                    }
                    for(int pleat=0;pleat<10;pleat++)B(detail,-w*.5f+.42f+pleat*.095f,d-1,.08f,.06f,.2f,2.6f,Surface.Slide);
                    Label(detail,"PROBADORES",new V3(-w*.5f+.3f,2.95f,d-.08f),.035f,shop.FurnishingMirror);break;
                case MallShopKind.Books:case MallShopKind.Shoes:case MallShopKind.Sports:case MallShopKind.Toys:case MallShopKind.Pharmacy:
                    for(float z=7;z<d-6;z+=5)foreach(int side in new[]{-1,1})
                    {
                        float x=side*5;
                        B(detail,x,z,2.32f,3.1f,2.1f,2.18f,shop.Kind==MallShopKind.Books?Surface.Accent:Surface.Metal);
                        for(int level=0;level<4;level++)
                        {
                            float h=.32f+level*.45f;
                            B(detail,x,z-1.51f,2.17f,.022f,h-.065f,h-.025f,Surface.Metal);
                            for(int i=-1;i<=1;i++)B(detail,x+i*.6f,z-1.528f,.22f,.012f,h-.06f,h-.025f,Surface.Plaster);
                        }
                        if(fine)foreach(int edge in new[]{-1,1})for(int hole=0;hole<6;hole++)
                            B(detail,x+edge*1.145f,z-1.45f,.012f,.04f,.5f+hole*.25f,.525f+hole*.25f,Surface.Slide);
                    }
                    if(shop.Kind==MallShopKind.Pharmacy)
                    {B(detail,0,d-.09f,2.7f,.04f,2.3f,3.3f,Surface.Plaster);B(detail,0,d-.12f,.15f,.02f,2.45f,3.15f,Surface.Accent);B(detail,0,d-.12f,.65f,.02f,2.72f,2.88f,Surface.Accent);}
                    if(shop.Kind==MallShopKind.Sports)
                    {B(detail,0,d-.10f,2,.02f,2.45f,3.1f,Surface.Slide);Label(detail,"SPORT",new V3(-.9f,2.64f,d-.12f),.045f,shop.FurnishingMirror);}
                    if(shop.Kind==MallShopKind.Toys)
                    {for(int i=0;i<4;i++)B(detail,(i-1.5f)*.5f,d-.1f,.3f,.03f,2.4f+(i%2)*.3f,2.7f+(i%2)*.3f,Surface.Accent);}
                    break;
                case MallShopKind.Electronics:
                    for(float z=7;z<d-6;z+=5)foreach(int side in new[]{-1,1})
                    {
                        float x=side*4.4f;
                        B(detail,x,z,2.42f,1.94f,.06f,.12f,Surface.Metal);
                        B(detail,x,z-1.015f,2.35f,.02f,.22f,.77f,Surface.Slide);
                        B(detail,x,z-1.03f,2.2f,.018f,.31f,.34f,Surface.Metal);
                        foreach(int end in new[]{-1,1})
                        {B(detail,x+end*.93f,z-1.04f,.2f,.015f,.60f,.67f,Surface.Plaster);}
                    }
                    for(int i=-2;i<=2;i++)
                    {
                        float x=i*2;
                        B(detail,x,d-.84f,1.65f,.08f,1.25f,2.3f,Surface.Metal);
                        B(detail,x,d-.89f,1.51f,.015f,1.33f,2.21f,Surface.Slide);
                        B(detail,x,d-.91f,.4f,.014f,1.4f,1.43f,Surface.Plaster);
                        B(detail,x,d-1,.3f,.28f,.98f,1.04f,Surface.Metal);
                    }break;
                case MallShopKind.Home:
                    foreach(int side in new[]{-1,1})
                    {
                        float x=side*4.3f;
                        B(detail,x,11,2.38f,1.28f,.05f,.10f,Surface.Metal);
                        B(detail,x,10.285f,2.3f,.025f,.15f,.35f,Surface.Accent);
                        B(detail,x,10.26f,.3f,.035f,.28f,.3f,Surface.Metal);
                        Lamp(detail,side*6.5f,10,0,fine);
                    }
                    // Bed frame/mattress, stitched cover and rounded pillows; no occupant or human models.
                    B(detail,-4,d-2.05f,3.05f,.14f,.15f,1.1f,Surface.Accent);
                    B(detail,-4,d-4.25f,2.79f,2.7f,.702f,.725f,Surface.Slide);
                    foreach(int side in new[]{-1,1})Rounded(detail,-4+side*.67f,d-5.2f,1.2f,.65f,.16f,.851f,.97f,Surface.Plaster);
                    if(fine)for(int stitch=0;stitch<8;stitch++)B(detail,-5.1f+stitch*.31f,d-4.5f,.017f,2.05f,.726f,.731f,Surface.Metal);
                    break;
                case MallShopKind.Cafe:case MallShopKind.FoodCourt:
                    CoffeeMachine(detail,-2,d-2,fine);
                    // Menu board, tiled backsplash, hood, pastry case and counter stools.
                    B(detail,0,d-.12f,6.6f,.06f,1.4f,2,Surface.Plaster);
                    for(int i=-3;i<=3;i++)B(detail,i*.8f,d-.17f,.035f,.018f,1.4f,2,Surface.Metal);
                    for(int i=-1;i<=1;i++)
                    {
                        B(detail,i*2.4f,d-.18f,2.1f,.08f,2.6f,3.55f,Surface.Slide);
                        Label(detail,i==0?"CAFE":"MENU",new V3(i*2.4f-.75f,3.1f,d-.23f),.04f,shop.FurnishingMirror);
                        for(int line=0;line<4;line++)B(detail,i*2.4f,d-.24f,1.3f,.013f,2.71f+line*.08f,2.735f+line*.08f,Surface.Plaster);
                    }
                    B(detail,2,d-2,1.1f,.8f,1.61f,1.67f,Surface.Metal);
                    B(detail,2,d-2,.98f,.04f,1.67f,1.9f,Surface.Metal);
                    // Rear-equipment glazing never covers the removable products on customer tables.
                    glass.Quad(new V3(1.46f,1.67f,d-2.4f),new V3(2.54f,1.67f,d-2.4f),new V3(2.54f,1.9f,d-2.4f),new V3(1.46f,1.9f,d-2.4f),new V3(0,0,-1),Surface.Plaster);
                    break;
                case MallShopKind.Arcade:
                    for(float z=6;z<d-3;z+=3)foreach(int side in new[]{-1,1})
                    {
                        float x=side*5;
                        B(detail,x,z-.56f,1.06f,.10f,1.66f,1.87f,Surface.Metal);
                        Label(detail,fine?"ARCADE":"PLAY",new V3(x-.42f,1.72f,z-.62f),.024f,shop.FurnishingMirror);
                        B(detail,x,z-.57f,.22f,.015f,.29f,.44f,Surface.Metal);
                        B(detail,x,z-.59f,.09f,.015f,.33f,.36f,Surface.Plaster);
                        Shapes.Column(detail,x-.26f,z-.7f,.027f,.9f,1.07f,8,Surface.Metal,1);
                        Shapes.Column(detail,x-.26f,z-.7f,.055f,1.04f,1.12f,8,Surface.Accent,1);
                    }break;
                case MallShopKind.Cinema:
                    foreach(int side in new[]{-1,1})for(float z=5;z<d-3;z+=4)
                    {
                        B(detail,side*(w*.5f-.14f),z,.04f,2.3f,.3f,3.6f,Surface.Slide);
                        B(detail,side*(w*.5f-.18f),z,.025f,2.1f,.4f,3.5f,Surface.Accent);
                    }
                    B(detail,0,3,1.15f,.65f,3.95f,4.25f,Surface.Metal);
                    Shapes.Column(detail,0,3,.025f,4.25f,4.8f,8,Surface.Metal,1);
                    B(detail,0,3.34f,.37f,.03f,4,4.2f,Surface.Slide);
                    B(detail,0,d-1.26f,w-2.1f,.02f,.66f,.73f,Surface.Metal);break;
                case MallShopKind.Restrooms:
                    foreach(int side in new[]{-1,1})
                    {
                        float x=side*4;
                        B(detail,x,4.49f,2.13f,.095f,1.14f,2.16f,Surface.Metal);
                        B(detail,x,4.425f,1.92f,.012f,1.24f,2.06f,Surface.Plaster);
                        Rounded(detail,x,4,.78f,.57f,.17f,.922f,.985f,Surface.Plaster);
                        // Basin is recessed inside its rim, instead of a solid metallic slab.
                        Rounded(detail,x,4,.52f,.34f,.13f,.985f,.99f,Surface.Slide);
                        Beam(detail,new V3(x,.92f,4.35f),new V3(x,1.16f,4.35f),.045f);
                        Beam(detail,new V3(x,1.16f,4.35f),new V3(x,1.16f,4.10f),.045f);
                        B(detail,x+side*1.5f,4.4f,.27f,.12f,1.1f,1.4f,Surface.Metal);
                        for(float z=8;z<d-2;z+=4)
                        {B(detail,side*5.8f,z+1.65f,.12f,.7f,.75f,1.1f,Surface.Metal);B(detail,side*5.8f,z-.14f,.06f,.04f,.97f,1.05f,Surface.Metal);}
                    }break;
                case MallShopKind.Hypermarket:
                    foreach(float x in new[]{-18f,-10f,10f,18f})
                    {
                        for(int shelf=0;shelf<4;shelf++)foreach(int side in new[]{-1,1})
                        {B(detail,x+side*.66f,13,.025f,11.8f,.28f+shelf*.45f,.32f+shelf*.45f,Surface.Metal);}
                        B(detail,x,13,1.5f,12.2f,2.1f,2.16f,Surface.Plaster);
                        Label(detail,"PASILLO",new V3(x-.62f,2.45f,6.95f),.03f,shop.FurnishingMirror);
                        for(int door=-1;door<=1;door++)
                        {B(detail,x+door+.37f,d-1.62f,.035f,.06f,.8f,1.35f,Surface.Metal);}
                    }
                    break;
                default:
                    for(float z=7;z<d-3;z+=6)foreach(int side in new[]{-1,1})
                    {
                        float x=side*4.4f;
                        B(detail,x,z,2.26f,2.26f,.08f,.14f,Surface.Accent);
                        foreach(int edge in new[]{-1,1})B(detail,x+edge*1.2f,z,.04f,2.35f,.15f,1.2f,Surface.Metal);
                        B(detail,x,z-1.21f,2.29f,.025f,.2f,.6f,Surface.Plaster);
                    }break;
            }
            // Correct signs on sockets, light fittings and mechanical ceiling services; bounded by quality.
            for(float z=4;z<d-2;z+=5)
            {
                B(detail,0,z,1.3f,1.3f,4.75f,4.8f,Surface.Metal);
                for(int slat=0;slat<(fine?6:3);slat++)B(detail,3,z-.32f+slat*(fine?.12f:.27f),.83f,.04f,4.72f,4.755f,Surface.Slide);
            }
            if(shop.FurnishingMirror<0)MallSpatialDesign.Mirror(glass);
            glass.TranslateRotate(new V3(shop.X,shop.Floor*MallPlan.FloorHeight,shop.Side*MallPlan.ShopFront),shop.Side==1?0:Shapes.Pi);
            worldGlass.Append(glass);
        }
        private static void Label(MeshData mesh,string text,V3 origin,float pixel,int mirror)
        {
            var letters=new MeshData();MallGlyphs.Write(letters,text,origin,pixel);
            if(mirror<0)
            {MallSpatialDesign.Mirror(letters);letters.TranslateRotate(new V3(2*(origin.X+text.Length*3*pixel),0,0),0);}
            mesh.Append(letters);
        }
        private static void Lamp(MeshData m,float x,float z,float y,bool fine)
        {
            Shapes.Column(m,x,z,.24f,y+.015f,y+.06f,12,Surface.Metal,1);
            Shapes.Column(m,x,z,.022f,y+.06f,y+1.6f,8,Surface.Metal,1);
            Shapes.Column(m,x,z,.3f,y+1.42f,y+1.8f,fine?16:8,Surface.Plaster,1);
            Shapes.Column(m,x,z,.23f,y+1.41f,y+1.43f,8,Surface.Glow,1);
        }
        private static void CoffeeMachine(MeshData m,float x,float z,bool fine)
        {
            Rounded(m,x,z,.84f,.64f,.08f,1.15f,1.63f,Surface.Metal);
            B(m,x,z-.335f,.67f,.015f,1.35f,1.53f,Surface.Slide);
            B(m,x,z-.42f,.71f,.35f,1.15f,1.18f,Surface.Metal);
            foreach(int side in new[]{-1,1})
            {Shapes.Column(m,x+side*.22f,z-.4f,.055f,1.25f,1.31f,8,Surface.Metal,1);Beam(m,new V3(x+side*.22f,1.30f,z-.4f),new V3(x+side*.22f,1.30f,z-.52f),.025f);}
            if(fine)for(int slat=0;slat<6;slat++)B(m,x-.28f+slat*.11f,z-.43f,.022f,.23f,1.18f,1.19f,Surface.Slide);
        }
        public static void Seating(MeshData physical,MeshData detail,float x,float z,float yaw,float y,bool cinema=false,bool fine=false)
        {
            var m=new MeshData();var d=new MeshData();
            foreach(int seat in new[]{-1,1})
            {
                float sx=seat*.67f;
                Rounded(m,sx,0,1.19f,.65f,.13f,.4f,.52f,cinema?Surface.Slide:Surface.Accent,cinema?1:2);
                Rounded(m,sx,.26f,1.19f,.17f,.1f,.52f,cinema?1.18f:1,cinema?Surface.Slide:Surface.Accent,cinema?1:2);
                foreach(int leg in new[]{-1,1})B(m,sx+leg*.42f,.02f,.075f,.47f,0,.4f,Surface.Metal);
                if(cinema)
                {
                    // Upholstery inserts are flat faces, not another solid model per seat.
                    d.Quad(new V3(sx-.51f,.57f,.169f),new V3(sx+.51f,.57f,.169f),new V3(sx+.51f,1.08f,.169f),new V3(sx-.51f,1.08f,.169f),new V3(0,0,-1),Surface.Slide);
                    d.Quad(new V3(sx-.53f,.523f,-.275f),new V3(sx+.53f,.523f,-.275f),new V3(sx+.53f,.523f,.275f),new V3(sx-.53f,.523f,.275f),V3.Up,Surface.Slide);
                }
                else
                {B(d,sx,.265f,1.02f,.19f,.57f,.92f,Surface.Slide);B(d,sx,0,1.06f,.55f,.522f,.555f,Surface.Slide);}
                foreach(int edge in new[]{-1,1})
                {B(d,sx+edge*.57f,0,.07f,.56f,.58f,.69f,Surface.Metal);if(cinema)
                    {
                        int sides=fine?8:4;var outer=new V2[sides];var inner=new V2[sides];
                        for(int i=0;i<sides;i++){float a=i*Shapes.Pi*2/sides;outer[i]=new V2(Shapes.Cos(a)*.065f,Shapes.Sin(a)*.065f);inner[i]=new V2(Shapes.Cos(a)*.05f,Shapes.Sin(a)*.05f);}
                        var cup=new MeshData();Shapes.Skirt(cup,outer,.69f,.73f,Surface.Metal,1);Shapes.Ring(cup,outer,inner,.73f,Surface.Metal,1);cup.TranslateRotate(new V3(sx+edge*.57f,0,-.2f),0);d.Append(cup);
                    }}
            }
            m.TranslateRotate(new V3(x,y,z),yaw);d.TranslateRotate(new V3(x,y,z),yaw);physical.Append(m);detail.Append(d);
        }
        public static void PublicBench(RoomGeometry o,float x,float z,float yaw,float y,int quality)
        {
            Seating(o.Structure,o.Decoration,x,z,yaw,y);
            if(quality>12)
            {
                var detail=new MeshData();for(int line=-4;line<=4;line++)B(detail,line*.27f,0,.012f,.54f,.556f,.56f,Surface.Metal);
                detail.TranslateRotate(new V3(x,y,z),yaw);o.Decoration.Append(detail);
            }
        }
        public static void Table(MeshData physical,MeshData detail,float x,float z)
        {
            Shapes.Column(physical,x,z,.28f,.015f,.065f,12,Surface.Metal,1);
            Shapes.Column(physical,x,z,.065f,.065f,.75f,8,Surface.Metal,1);
            Rounded(physical,x,z,1.8f,1.5f,.22f,.75f,.85f,Surface.Accent);
            foreach(int side in new[]{-1,1})
            {
                Rounded(physical,x,z+side*1.25f,.65f,.65f,.15f,.4f,.5f,Surface.Slide);
                Rounded(physical,x,z+side*1.5f,.65f,.12f,.05f,.5f,.95f,Surface.Accent);
                foreach(int leg in new[]{-1,1})foreach(int end in new[]{-1,1})
                    Beam(physical,new V3(x+leg*.24f,.02f,z+side*1.25f+end*.24f),new V3(x+leg*.2f,.4f,z+side*1.25f+end*.2f),.035f);
                B(detail,x,z+side*1.49f,.55f,.15f,.65f,.88f,Surface.Slide);
            }
        }
        public static void CourtTable(RoomGeometry o,float x,float z,float y,int quality)
        {
            var m=new MeshData();var d=new MeshData();Table(m,d,0,0);
            m.TranslateRotate(new V3(x,y,z),0);d.TranslateRotate(new V3(x,y,z),0);o.Structure.Append(m);o.Decoration.Append(d);
        }
        public static void Plant(MeshData detail,float x,float z,float y,int quality)
        {
            int leaves=quality<=12?7:12;
            for(int i=0;i<leaves;i++)
            {
                float angle=i*Shapes.Pi*2/leaves,reach=.4f+(i%3)*.08f;
                V3 root=new V3(x,y,z),mid=new V3(x+Shapes.Cos(angle)*reach*.6f,y+.7f+(i%2)*.12f,z+Shapes.Sin(angle)*reach*.6f);
                V3 tip=new V3(x+Shapes.Cos(angle)*reach,y+.85f+(i%3)*.13f,z+Shapes.Sin(angle)*reach);
                V3 width=new V3(-Shapes.Sin(angle)*.09f,0,Shapes.Cos(angle)*.09f);
                Beam(detail,root,mid,.015f,Surface.Accent);
                detail.Quad(mid-width,mid+width,tip+width*.15f,tip-width*.15f,V3.Cross(width,tip-mid).Unit,Surface.Slide);
            }
        }
        public static void DryFountain(RoomGeometry o,int quality)
        {
            int n=quality<=12?2:4;
            var outer=Shapes.RoundedRect(10,7,1.7f,n);var inner=Shapes.RoundedRect(9.4f,6.4f,1.4f,n);
            Shapes.Skirt(o.Structure,outer,0,.62f,Surface.Tile,1);
            Shapes.Skirt(o.Structure,inner,.04f,.62f,Surface.Tile,1,true);
            Shapes.Ring(o.Structure,outer,inner,.62f,Surface.Tile,1);
            Shapes.Fill(o.Decoration,inner,.008f,Surface.Slide,1);
            Shapes.Column(o.Structure,0,0,.85f,.02f,.50f,12,Surface.Plaster,1);
            Shapes.Column(o.Structure,0,0,.58f,.50f,1.2f,12,Surface.Plaster,1);
            Shapes.Column(o.Decoration,0,0,1.05f,1.2f,1.3f,16,Surface.Metal,1);
            foreach(int side in new[]{-1,1})PublicBench(o,side*7,0,side*Shapes.Pi*.5f,0,quality);
            MallGlyphs.Write(o.Decoration,"PLAZA 2010",new V3(-2.7f,1.63f,-.85f),.085f);
            foreach(int side in new[]{-1,1})
                B(o.Decoration,0,side*3.9f,11,.1f,.003f,.008f,Surface.Accent);
        }
        public static void Directory(RoomGeometry o,MallPlan p,float x,float z,float y,int quality)
        {
            // Existing physical directory is 1.8 x .8 x 1.8; detail stays inside that shell.
            B(o.Decoration,x,z-.418f,1.67f,.035f,y+.15f,y+1.77f,Surface.Slide);
            B(o.Decoration,x,z-.44f,1.45f,.015f,y+.44f,y+1.3f,Surface.Plaster);
            MallGlyphs.Write(o.Decoration,"PLAZA 2010",new V3(x-.7f,y+1.47f,z-.465f),.02f);
            MallGlyphs.Write(o.Decoration,p.Floors+" NIVELES",new V3(x-.70f,y+.19f,z-.465f),.014f);
            // A small true-to-plan directory: shop rows, court profile and marked stair plazas.
            foreach(var s in p.Shops)if(s.Floor==0)
                B(o.Decoration,x+s.X/p.Width*1.28f,z-.465f,.055f,.01f,y+.88f+s.Side*.19f,y+.94f+s.Side*.19f,Surface.Accent);
            B(o.Decoration,x,z-.467f,1.26f,.008f,y+.885f,y+.92f,Surface.Metal);
        }
        public static void ATM(RoomGeometry o,float x,float z,float y,int quality)
        {
            B(o.Decoration,x,z-.555f,.80f,.025f,y+.90f,y+1.55f,Surface.Metal);
            B(o.Decoration,x,z-.572f,.52f,.015f,y+1.03f,y+1.44f,Surface.Slide);
            MallGlyphs.Write(o.Decoration,"CAJERO",new V3(x-.33f,y+1.60f,z-.56f),.02f);
            B(o.Decoration,x,z-.6f,.49f,.09f,y+.67f,y+.73f,Surface.Metal);
            B(o.Decoration,x,z-.62f,.33f,.015f,y+.41f,y+.445f,Surface.Slide);
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)B(o.Decoration,x-.12f+col*.11f,z-.65f,.07f,.035f,y+.683f+row*.008f,y+.70f+row*.008f,Surface.Plaster);
        }
    }
}
