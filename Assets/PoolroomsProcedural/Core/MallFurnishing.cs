using System;

namespace Poolrooms.Core
{
    public static class MallFurnishing
    {
        private static void B(MeshData m,float x,float z,float w,float d,float a,float b,Surface mat=Surface.Accent)
        {MallArchitecture.Box(m,x,z,w,d,a,b,mat);}
        public static void Bench(MeshData m,float x,float z,float yaw,float y)
        {
            var part=new MeshData();B(part,0,0,2.6f,.65f,.4f,.52f);B(part,0,.28f,2.6f,.12f,.52f,1);
            foreach(int side in new[]{-1,1}){B(part,side*.95f,0,.12f,.5f,0,.4f,Surface.Metal);B(part,side*1.25f,0,.08f,.6f,.48f,.7f,Surface.Metal);}
            part.TranslateRotate(new V3(x,y,z),yaw);m.Append(part);
        }
        public static void Store(MeshData physical,MeshData detail,MallShop shop,int quality,bool staticMerchandise=true)
        {
            int count=staticMerchandise?(quality<=12?6:quality<=32?12:20):0;var random=new StableRandom(shop.Seed);
            float w=shop.Width,d=shop.Depth;
            // Perimeter skirting, ceiling panels, vents, sockets and entrance security pedestals.
            B(detail,-w*.5f+.18f,d*.5f,.1f,d,0,.12f,Surface.Plaster);B(detail,w*.5f-.18f,d*.5f,.1f,d,0,.12f,Surface.Plaster);
            for(float z=4;z<d-2;z+=5)
            {
                B(detail,0,z,1.2f,1.2f,4.8f,4.86f,Surface.Glow);
                B(detail,w*.5f-.35f,z,.07f,.22f,.28f,.4f,Surface.Plaster);
                B(detail,-w*.5f+.35f,z,.07f,.22f,.28f,.4f,Surface.Plaster);
                for(int i=-1;i<=1;i++)B(detail,3,z+i*.16f,.8f,.06f,4.76f,4.79f,Surface.Metal);
            }
            if(shop.Kind!=MallShopKind.Restrooms&&shop.Kind!=MallShopKind.Cafe&&shop.Kind!=MallShopKind.FoodCourt)
                foreach(int side in new[]{-1,1})B(physical,side*3.25f,.9f,.15f,.7f,0,1.2f,Surface.Metal);
            switch(shop.Kind)
            {
                case MallShopKind.Hypermarket:Hypermarket(physical,detail,w,d,count,ref random);break;
                case MallShopKind.Fashion:
                    for(float z=6;z<d-7;z+=5)foreach(int side in new[]{-1,1})Clothes(physical,detail,side*4.6f,z,count);
                    B(physical,-4,d-3,4,2,0,.8f);Folded(detail,-4,d-3,count);Counter(physical,detail,4,d-3);break;
                case MallShopKind.Shoes:
                    SideShelves(physical,detail,d,count,shop.Kind,ref random);if(shop.Refined)MallFurnishingV21.Seating(physical,detail,-2.8f,d*.55f,Shapes.Pi*.5f,0);else Bench(physical,-2.8f,d*.55f,Shapes.Pi*.5f,0);
                    B(physical,4,staticMerchandise?3.5f:d-5.5f,2,2,0,.8f);if(staticMerchandise)Shoes(detail,4,3.5f,.82f);Counter(physical,detail,4,d-3);break;
                case MallShopKind.Electronics:
                    for(float z=7;z<d-6;z+=5)foreach(int side in new[]{-1,1})
                    {B(physical,side*4.4f,z,2.5f,2,0,.95f,Surface.Plaster);if(staticMerchandise){Laptop(detail,side*4.4f-.5f,z,.96f);Laptop(detail,side*4.4f+.6f,z,.96f);}}
                    for(int i=-2;i<=2;i++){B(physical,i*2,d-1,.07f,.25f,1,2.2f,Surface.Metal);B(detail,i*2,d-1,.07f,.28f,1.2f,2,Surface.Slide);}
                    Counter(physical,detail,4,d-4);break;
                case MallShopKind.Books:SideShelves(physical,detail,d,count,shop.Kind,ref random);Counter(physical,detail,4,d-3);if(shop.Refined)MallFurnishingV21.Seating(physical,detail,-2.5f,d-3,Shapes.Pi*.5f,0);else Bench(physical,-2.5f,d-3,Shapes.Pi*.5f,0);break;
                case MallShopKind.Sports:
                    SideShelves(physical,detail,d,count,shop.Kind,ref random);if(staticMerchandise)for(int i=0;i<3;i++)B(detail,-4+i,3.5f,.6f,.6f,.85f,1.45f,Surface.Slide);
                    B(physical,-3,staticMerchandise?3.5f:d-4,3,2,0,.8f);Counter(physical,detail,4,d-3);break;
                case MallShopKind.Home:
                    foreach(int side in new[]{-1,1})
                    {if(shop.Refined)MallFurnishingV21.Seating(physical,detail,side*4.3f,7,0,0);else Bench(physical,side*4.3f,7,0,0);B(physical,side*4.3f,11,2.5f,1.4f,0,.5f);Table(physical,detail,side*4.3f,16,staticMerchandise,shop.Refined);}
                    B(physical,-4,d-4,3,4,0,.55f);B(detail,-4,d-4,2.8f,3.7f,.55f,.7f,Surface.Plaster);
                    B(detail,-4,d-5.2f,2.7f,.7f,.7f,.85f,Surface.Plaster);Counter(physical,detail,4,d-3);break;
                case MallShopKind.Toys:SideShelves(physical,detail,d,count,shop.Kind,ref random);B(physical,-4,staticMerchandise?3.5f:d-4,2.5f,2,0,.75f);Products(detail,-4,3.5f,2.2f,1.6f,.75f,count,ref random);Counter(physical,detail,4,d-3);break;
                case MallShopKind.Pharmacy:SideShelves(physical,detail,d,count,shop.Kind,ref random);Counter(physical,detail,0,d-3);B(physical,-4,d-3,2,1.5f,0,2.4f,Surface.Plaster);break;
                case MallShopKind.Cafe:
                case MallShopKind.FoodCourt:
                    // The entrance-side counter is also a reliable support for a grabbable snack.
                    Counter(physical,detail,3.8f,3.8f);
                    for(float z=10;z<d-4;z+=6)foreach(int side in new[]{-1,1})Table(physical,detail,side*4.3f,z,staticMerchandise,shop.Refined);
                    Counter(physical,detail,0,d-2);B(detail,-2,d-2,.8f,.6f,1.1f,1.6f,Surface.Metal);B(detail,0,d-2,1.2f,.7f,1.1f,1.55f,Surface.Metal);
                    B(detail,2,d-1.8f,.8f,.8f,1.1f,1.6f,Surface.Plaster);
                    for(int i=0;i<3;i++)B(detail,-3+i*3,d-.5f,2,.08f,2.1f,3.1f,Surface.Slide);
                    break;
                case MallShopKind.Arcade:
                    for(float z=6;z<d-3;z+=3)foreach(int side in new[]{-1,1})
                    {float x=side*5;B(physical,x,z,1.1f,1.1f,0,1.8f,Surface.Slide);B(detail,x,z-.57f,.8f,.08f,.9f,1.6f,Surface.Glow);B(detail,x,z-.7f,.85f,.25f,.8f,.9f,Surface.Metal);for(int i=0;i<3;i++)B(detail,x-.2f+i*.2f,z-.72f,.06f,.06f,.9f,.96f,Surface.Accent);}
                    break;
                case MallShopKind.Cinema:
                    // Screening room has a central aisle and actual seating, not a locked facade.
                    for(float z=6;z<d-5;z+=2)foreach(int side in new[]{-1,1}){if(shop.Refined)MallFurnishingV21.Seating(physical,detail,side*4.3f,z,Shapes.Pi,0,true,quality>12);else Bench(physical,side*4.3f,z,Shapes.Pi,0);}
                    B(physical,0,d-1,w-1,.4f,.3f,3.7f,Surface.Slide);B(detail,0,d-1.23f,w-2,.02f,.8f,3.3f,Surface.Plaster);break;
                case MallShopKind.Restrooms:Restrooms(physical,detail,w,d);break;
                default:
                    for(float z=7;z<d-3;z+=6)foreach(int side in new[]{-1,1})
                    {B(physical,side*4.4f,z,2.4f,2.4f,0,staticMerchandise?1.05f:1.2f,Surface.Metal);B(detail,side*4.4f,z,2.2f,2.2f,1.05f,1.2f,Surface.Plaster);Products(detail,side*4.4f,z,2,2,1.2f,count,ref random);}
                    Counter(physical,detail,4,d-3);break;
            }
        }
        private static void Counter(MeshData m,MeshData d,float x,float z)
        {
            B(m,x,z,3,1.5f,0,1.08f);B(d,x,z,3.15f,1.6f,1.08f,1.15f,Surface.Plaster);
            B(d,x+.7f,z,.5f,.4f,1.15f,1.25f,Surface.Metal);B(d,x+.7f,z+.12f,.45f,.08f,1.25f,1.55f,Surface.Slide);
            for(int i=0;i<3;i++)B(d,x-1+i*.18f,z,.12f,.2f,1.15f,1.3f,Surface.Plaster);
        }
        private static void Table(MeshData m,MeshData d,float x,float z,bool merchandise=true,bool refined=false)
        {
            if(refined){MallFurnishingV21.Table(m,d,x,z);return;}
            B(m,x,z,.12f,.12f,0,.75f,Surface.Metal);B(m,x,z,1.8f,1.5f,.75f,.85f);
            foreach(int side in new[]{-1,1}){B(m,x,z+side*1.25f,.65f,.65f,.4f,.5f);B(m,x,z+side*1.5f,.65f,.1f,.5f,.95f);B(m,x,z+side*1.25f,.1f,.1f,0,.4f,Surface.Metal);}
            if(merchandise){B(d,x,z,.25f,.25f,.85f,.92f,Surface.Plaster);B(d,x+.3f,z,.1f,.1f,.85f,1.05f,Surface.Plaster);}
        }
        private static void Laptop(MeshData d,float x,float z,float y)
        {B(d,x,z,.55f,.35f,y,y+.04f,Surface.Metal);B(d,x,z+.14f,.55f,.04f,y,y+.36f,Surface.Slide);B(d,x,z,.36f,.2f,y+.04f,y+.045f,Surface.Plaster);}
        private static void Folded(MeshData d,float x,float z,int n){for(int i=0;i<n;i++)B(d,x-.8f+(i%3)*.8f,z-.5f+(i%2),.55f,.5f,.82f+(i/6)*.09f,.89f+(i/6)*.09f,i%2==0?Surface.Slide:Surface.Plaster);}
        private static void Shoes(MeshData d,float x,float z,float y){foreach(int side in new[]{-1,1}){B(d,x+side*.18f,z,.2f,.5f,y,y+.07f,Surface.Plaster);B(d,x+side*.18f,z+.1f,.18f,.27f,y+.07f,y+.23f,Surface.Slide);}}
        private static void Clothes(MeshData m,MeshData d,float x,float z,int n)
        {
            foreach(int side in new[]{-1,1})B(m,x,z+side*1.8f,.15f,.15f,0,1.8f,Surface.Metal);
            B(m,x,z,.08f,3.6f,1.72f,1.8f,Surface.Metal);
            for(int i=0;i<n;i++){float at=z-1.5f+3*i/Math.Max(1f,n-1);B(d,x,at,.6f,.09f,.65f,1.62f,i%2==0?Surface.Slide:Surface.Plaster);B(d,x,at,.95f,.08f,1.3f,1.5f,Surface.Accent);}
        }
        private static void Shelf(MeshData m,MeshData d,float x,float z,float width,float depth,int n,ref StableRandom random,bool longAisle=false)
        {
            B(m,x,z,width,depth,0,.12f,Surface.Plaster);if(longAisle)
            {
                B(m,x,z,.07f,depth,.12f,2.1f,Surface.Plaster);
                foreach(int side in new[]{-1,1})B(m,x,z+side*(depth*.5f-.04f),width,.08f,.12f,2.1f,Surface.Metal);
            }
            else
            {
                B(m,x,z,width,.07f,.12f,2.1f,Surface.Plaster);
                foreach(int side in new[]{-1,1})B(m,x+side*(width*.5f-.04f),z,.08f,depth,.12f,2.1f,Surface.Metal);
            }
            for(int shelf=0;shelf<4;shelf++){float y=.25f+shelf*.45f;B(m,x,z,width,depth,y,y+.07f,Surface.Plaster);Products(d,x,z,width-.2f,depth-.12f,y+.07f,n,ref random);}
        }
        private static void Products(MeshData d,float x,float z,float width,float depth,float y,int n,ref StableRandom random)
        {
            int rows=Math.Max(1,(n+3)/4);
            for(int i=0;i<n;i++)
            {float px=x-width*.4f+(i%4)*(width*.8f/3),pz=z+(rows==1?0:(i/4)/(float)(rows-1)-.5f)*depth*.7f;float h=random.Range(.12f,.3f);
                B(d,px,pz,Math.Min(.22f,width/6),Math.Min(.2f,depth/4),y,y+h,(Surface)(i%3));
                B(d,px,pz-.11f,.09f,.01f,y+.03f,y+h*.7f,Surface.Plaster);}
        }
        private static void SideShelves(MeshData m,MeshData d,float depth,int n,MallShopKind kind,ref StableRandom random)
        {
            for(float z=7;z<depth-6;z+=5)foreach(int side in new[]{-1,1})
            {
                Shelf(m,d,side*5,z,2.2f,3,n,ref random);
                if(kind==MallShopKind.Shoes&&n>0)for(int row=0;row<3;row++)Shoes(d,side*5,z,.34f+row*.45f);
                if(kind==MallShopKind.Books)for(int i=0;i<n;i++)B(d,side*5-.75f+i*1.5f/n,z-1.55f,.08f,.04f,.8f,1.12f,i%2==0?Surface.Accent:Surface.Slide);
            }
        }
        private static void Hypermarket(MeshData m,MeshData d,float width,float depth,int n,ref StableRandom random)
        {
            foreach(float x in new[]{-18f,-10f,10f,18f})
            {
                Shelf(m,d,x,13,1.3f,12,n*2,ref random,n==0);Counter(m,d,x,3);
                B(m,x,depth-1,3,1,0,2.4f,Surface.Plaster);B(d,x,depth-1.53f,2.8f,.05f,.4f,2.15f,Surface.Slide);
                for(int i=-1;i<=1;i++)B(d,x+i,depth-1.57f,.035f,.05f,.35f,2.2f,Surface.Metal);
            }
            foreach(int side in new[]{-1,1})
            {B(m,side*3.8f,8,3,2,0,.8f);Products(d,side*3.8f,8,2.8f,1.8f,.8f,n,ref random);}
            ZoneShapes.Portal(m,0,depth-5,0,width-.4f,3.4f,0,4.4f,3.2f,.18f,3,12,1.2f);
            foreach(int side in new[]{-1,1})
            {Shelf(m,d,side*5,depth-2,5,2,n,ref random);B(m,side*(width*.5f-1),12,1,6,0,2.35f,Surface.Plaster);B(d,side*(width*.5f-1),12,1.03f,5.7f,.5f,2.2f,Surface.Slide);}
        }
        private static void Restrooms(MeshData m,MeshData d,float width,float depth)
        {
            // Open stall entrances, central aisle and sinks all remain physically accessible.
            for(float z=8;z<depth-2;z+=4)foreach(int side in new[]{-1,1})
            {
                B(m,side*4,z-1.8f,5,.12f,0,2.15f,Surface.Plaster);
                B(m,side*5.8f,z,.9f,.8f,0,.5f,Surface.Plaster);B(m,side*5.8f,z+.32f,.8f,.25f,.5f,1,Surface.Plaster);
                B(d,side*5.8f,z,.65f,.5f,.5f,.58f,Surface.Slide);B(d,side*6.8f,z-.7f,.12f,.2f,.8f,.96f,Surface.Metal);
            }
            foreach(int side in new[]{-1,1})
            {B(m,side*4,4,3,1.2f,.7f,.85f,Surface.Plaster);B(d,side*4,4,.7f,.5f,.85f,.92f,Surface.Metal);B(d,side*4,4.5f,2,.06f,1.2f,2.1f,Surface.Metal);B(d,side*4,4.35f,.06f,.2f,.92f,1.15f,Surface.Metal);}
        }
    }
}
