using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum MallItemShape { Book, Notebook, PencilCase, Shirt, Shoe, Cap, Handbag, Glasses, Watch, Phone, Tablet, Camera,
        Headphones, Gamepad, Keyboard, Mouse, Laptop, Speaker, Radio, DiscCase, Mug, Bottle, Vase, Plate, Snack, FoodBox, ToyCar, Blocks, Ball, Backpack, Towel, Soap }
    public sealed class MallItemDefinition
    {
        public readonly int Id,Variant;
        public readonly MallItemShape Shape;
        public readonly string Name;
        public readonly V3 Size;
        public readonly float Mass;
        internal MallItemDefinition(int id,MallItemShape shape,int variant,string name,V3 size,float mass)
        {Id=id;Shape=shape;Variant=variant;Name=name;Size=size;Mass=mass;}
        public float Radius { get { return Size.Length*.5f+.015f; } }
    }
    public static class MallItemCatalog
    {
        public const int Count=256;
        private static readonly string[] Names={"Libro", "Cuaderno", "Estuche", "Camiseta doblada", "Zapatilla", "Gorra", "Bolso", "Gafas", "Reloj", "Telefono 2010", "Tableta", "Camara compacta", "Auriculares", "Mando", "Teclado", "Raton", "Portatil", "Altavoz", "Radio", "Caja DVD", "Taza", "Botella", "Jarron", "Plato", "Bolsa de papas", "Caja de galletas", "Coche de juguete", "Bloques", "Pelota", "Mochila", "Toalla", "Jabon"};
        private static readonly string[] Editions={"marfil", "azul", "terracota", "menta", "grafito", "ambar", "lavanda", "coral"};
        private static readonly V3[] Dimensions={new V3(.22f,.045f,.3f),new V3(.2f,.025f,.27f),new V3(.25f,.08f,.12f),new V3(.36f,.08f,.3f),new V3(.14f,.16f,.3f),new V3(.24f,.12f,.26f),new V3(.25f,.29f,.12f),new V3(.16f,.04f,.15f),new V3(.065f,.02f,.17f),new V3(.07f,.015f,.13f),new V3(.17f,.015f,.24f),new V3(.12f,.065f,.06f),new V3(.19f,.2f,.08f),new V3(.17f,.055f,.11f),new V3(.4f,.028f,.14f),new V3(.06f,.035f,.1f),new V3(.38f,.27f,.25f),new V3(.13f,.19f,.11f),new V3(.24f,.14f,.1f),new V3(.14f,.015f,.19f),new V3(.12f,.1f,.1f),new V3(.065f,.26f,.065f),new V3(.13f,.25f,.13f),new V3(.23f,.018f,.23f),new V3(.23f,.31f,.1f),new V3(.2f,.13f,.09f),new V3(.15f,.065f,.07f),new V3(.2f,.12f,.15f),new V3(.18f,.18f,.18f),new V3(.27f,.34f,.16f),new V3(.29f,.06f,.2f),new V3(.09f,.025f,.06f)};
        private static readonly MallItemDefinition[] Items=Create();
        private static MallItemDefinition[] Create()
        {
            var result=new MallItemDefinition[Count];
            for(int shape=0;shape<32;shape++)for(int variant=0;variant<8;variant++)
            {
                int id=shape*8+variant;float scale=.86f+variant*.035f;
                V3 dimensions=shape==(int)MallItemShape.Shirt&&variant>=4?new V3(.36f,.42f,.055f)*scale:Dimensions[shape]*scale;
                float mass=shape==16?1.6f:shape==0?.32f:shape==21?.45f:shape==24?.06f:.15f;
                result[id]=new MallItemDefinition(id,(MallItemShape)shape,variant,Names[shape]+" · "+Editions[variant],dimensions,mass);
            }
            return result;
        }
        public static MallItemDefinition Get(int id)
        {if(id<0||id>=Count)throw new ArgumentOutOfRangeException(nameof(id));return Items[id];}
        public static IEnumerable<MallItemDefinition> All { get { return Items; } }
        public static MeshData Model(int id)
        {
            MallItemDefinition item=Get(id);var m=new MeshData{Theme=SurfaceTheme.Merchandise,EmissionStrength=0};
            V3 size=item.Size;float w=size.X,h=size.Y,d=size.Z;
            // Models use a centre pivot and a guaranteed bounding box for ray selection and carrying.
            switch(item.Shape)
            {
                case MallItemShape.Book:case MallItemShape.Notebook:case MallItemShape.DiscCase:
                    B(m,0,0,w,d,-h*.5f,h*.5f,Surface.Accent);
                    B(m,w*.025f,-d*.01f,w*.94f,d*.95f,-h*.31f,h*.31f,Surface.Plaster);
                    B(m,-w*.45f,0,w*.09f,d,-h*.5f,h*.5f,Surface.Slide);
                    for(int i=0;i<3;i++)B(m,w*.1f,-d*.28f+i*d*.07f,w*.38f,d*.025f,h*.498f,h*.5f,Surface.Metal);
                    break;
                case MallItemShape.Shirt:case MallItemShape.Towel:
                    if(item.Shape==MallItemShape.Shirt&&h>.2f)
                    {
                        B(m,0,0,w*.6f,d,-h*.5f,h*.4f,Surface.Slide);
                        B(m,0,0,w,d*.9f,h*.14f,h*.35f,Surface.Slide);
                        B(m,0,0,w*.24f,d*.8f,h*.35f,h*.5f,Surface.Plaster);break;
                    }
                    B(m,0,0,w*.85f,d,-h*.5f,h*.5f,Surface.Slide);
                    B(m,0,-d*.3f,w,d*.3f,-h*.35f,h*.35f,Surface.Slide);
                    B(m,0,-d*.31f,w*.25f,d*.13f,h*.36f,h*.49f,Surface.Plaster);break;
                case MallItemShape.Shoe:
                    B(m,0,0,w,d,-h*.5f,-h*.3f,Surface.Plaster);
                    B(m,0,-d*.08f,w*.95f,d*.83f,-h*.3f,h*.12f,Surface.Slide);
                    B(m,0,d*.24f,w*.9f,d*.4f,-h*.3f,h*.5f,Surface.Slide);
                    for(int i=0;i<4;i++)B(m,0,-d*.16f+i*d*.08f,w*.65f,d*.025f,h*.13f,h*.15f,Surface.Plaster);break;
                case MallItemShape.Cap:
                    Cylinder(m,new V3(0,-h*.5f,-d*.08f),w*.42f,h,12,Surface.Slide);
                    B(m,0,d*.24f,w,d*.45f,-h*.48f,-h*.36f,Surface.Accent);break;
                case MallItemShape.Handbag:case MallItemShape.Backpack:
                    B(m,0,0,w,d,-h*.5f,h*.25f,Surface.Slide);
                    B(m,0,-d*.44f,w*.72f,d*.1f,-h*.37f,-h*.02f,Surface.Accent);
                    foreach(int side in new[]{-1,1})B(m,side*w*.23f,0,w*.055f,d*.2f,h*.22f,h*.5f,Surface.Metal);
                    B(m,0,0,w*.5f,d*.2f,h*.45f,h*.5f,Surface.Metal);break;
                case MallItemShape.Glasses:
                    foreach(int side in new[]{-1,1})
                    {B(m,side*w*.26f,-d*.34f,w*.43f,d*.09f,-h*.5f,h*.5f,Surface.Metal);B(m,side*w*.48f,d*.05f,w*.035f,d*.88f,-h*.03f,h*.12f,Surface.Metal);}
                    B(m,0,-d*.34f,w*.12f,d*.08f,-h*.07f,h*.07f,Surface.Metal);break;
                case MallItemShape.Watch:
                    B(m,0,0,w*.6f,d,-h*.28f,h*.1f,Surface.Slide);
                    Cylinder(m,new V3(0,-h*.3f,0),w*.5f,h*.8f,12,Surface.Metal);
                    B(m,0,0,w*.6f,d*.28f,h*.35f,h*.4f,Surface.Plaster);break;
                case MallItemShape.Phone:case MallItemShape.Tablet:
                    B(m,0,0,w,d,-h*.5f,h*.5f,Surface.Metal);B(m,0,0,w*.85f,d*.81f,h*.495f,h*.5f,Surface.Slide);
                    B(m,0,-d*.45f,w*.08f,d*.025f,h*.495f,h*.5f,Surface.Plaster);break;
                case MallItemShape.Camera:
                    B(m,0,0,w,d*.7f,-h*.5f,h*.5f,Surface.Metal);
                    var lens=new MeshData();Cylinder(lens,new V3(0,-d*.16f,0),h*.38f,d*.3f,12,Surface.Metal);
                    // Rotate cylinder's axis into Z without altering the surrounding model's pivot.
                    RotateX(lens,(float)Math.PI*.5f);lens.TranslateRotate(new V3(w*.12f,0,-d*.34f),0);m.Append(lens);
                    B(m,-w*.32f,-d*.36f,w*.2f,d*.02f,h*.17f,h*.36f,Surface.Plaster);break;
                case MallItemShape.Headphones:
                    foreach(int side in new[]{-1,1})
                    {B(m,side*w*.4f,0,w*.2f,d,-h*.5f,h*.12f,Surface.Slide);B(m,side*w*.43f,0,w*.09f,d*.4f,-h*.1f,h*.4f,Surface.Metal);}
                    B(m,0,0,w*.85f,d*.4f,h*.38f,h*.5f,Surface.Slide);break;
                case MallItemShape.Gamepad:
                    B(m,0,0,w,d*.72f,-h*.5f,h*.3f,Surface.Slide);
                    foreach(int side in new[]{-1,1})
                    {B(m,side*w*.32f,d*.28f,w*.22f,d*.4f,-h*.5f,h*.2f,Surface.Slide);Cylinder(m,new V3(side*w*.2f,h*.29f,-d*.13f),w*.07f,h*.21f,8,Surface.Metal);}
                    break;
                case MallItemShape.Keyboard:
                    B(m,0,0,w,d,-h*.5f,h*.1f,Surface.Metal);
                    for(int row=0;row<4;row++)for(int col=0;col<10;col++)B(m,(col-4.5f)*w*.09f,(row-1.5f)*d*.21f,w*.065f,d*.14f,h*.1f,h*.5f,Surface.Plaster);break;
                case MallItemShape.Mouse:
                    B(m,0,0,w,d,-h*.5f,h*.32f,Surface.Slide);
                    B(m,0,-d*.19f,w*.09f,d*.2f,h*.33f,h*.5f,Surface.Metal);break;
                case MallItemShape.Laptop:
                    B(m,0,0,w,d,-h*.5f,-h*.38f,Surface.Metal);
                    B(m,0,d*.46f,w,d*.08f,-h*.4f,h*.5f,Surface.Metal);
                    B(m,0,d*.411f,w*.88f,d*.005f,-h*.28f,h*.38f,Surface.Slide);
                    B(m,0,-d*.05f,w*.82f,d*.47f,-h*.378f,-h*.37f,Surface.Plaster);break;
                case MallItemShape.Speaker:case MallItemShape.Radio:
                    B(m,0,0,w,d,-h*.5f,h*.5f,Surface.Accent);
                    foreach(int side in new[]{-1,1})
                    {B(m,side*w*.23f,-d*.48f,w*.35f,d*.02f,-h*.28f,h*.25f,Surface.Metal);}
                    B(m,0,-d*.491f,w*.14f,d*.009f,h*.3f,h*.4f,Surface.Plaster);break;
                case MallItemShape.Mug:
                    Cylinder(m,new V3(-w*.12f,-h*.5f,0),d*.43f,h,12,Surface.Plaster);
                    B(m,w*.35f,0,w*.26f,d*.16f,-h*.3f,h*.3f,Surface.Metal);break;
                case MallItemShape.Bottle:case MallItemShape.Vase:
                    Cylinder(m,new V3(0,-h*.5f,0),Math.Min(w,d)*.47f,h*.7f,12,Surface.Accent);
                    Cylinder(m,new V3(0,h*.2f,0),Math.Min(w,d)*.24f,h*.3f,12,Surface.Metal);
                    B(m,0,-d*.474f,w*.65f,d*.005f,-h*.2f,h*.09f,Surface.Plaster);break;
                case MallItemShape.Plate:
                    Cylinder(m,new V3(0,-h*.5f,0),Math.Min(w,d)*.5f,h,16,Surface.Plaster);break;
                case MallItemShape.Snack:
                    B(m,0,0,w*.9f,d,-h*.44f,h*.44f,Surface.Accent);
                    foreach(int side in new[]{-1,1})B(m,0,0,w,d*.4f,side<0?-h*.5f:h*.44f,side<0?-h*.44f:h*.5f,Surface.Metal);
                    B(m,0,-d*.49f,w*.6f,d*.01f,-h*.1f,h*.14f,Surface.Plaster);break;
                case MallItemShape.ToyCar:
                    B(m,0,0,w,d*.9f,-h*.35f,h*.07f,Surface.Accent);
                    B(m,0,d*.02f,w*.5f,d*.83f,h*.07f,h*.5f,Surface.Slide);
                    foreach(int side in new[]{-1,1})foreach(int axle in new[]{-1,1})B(m,axle*w*.32f,side*d*.4f,w*.17f,d*.2f,-h*.5f,-h*.16f,Surface.Metal);break;
                case MallItemShape.Blocks:
                    for(int i=0;i<3;i++)B(m,(i-1)*w*.31f,0,w*.28f,d,-h*.5f,h*.25f,Surface.Accent);
                    foreach(int side in new[]{-1,1})Cylinder(m,new V3(side*w*.3f,h*.25f,0),w*.08f,h*.25f,8,Surface.Plaster);break;
                case MallItemShape.Ball:
                    Sphere(m,w*.5f,12,8,Surface.Slide);break;
                default:
                    B(m,0,0,w,d,-h*.5f,h*.5f,Surface.Accent);
                    B(m,0,-d*.49f,w*.72f,d*.01f,-h*.21f,h*.2f,Surface.Plaster);break;
            }
            // Atlas UVs carry colour through standard HDRP/Lit, without a custom shader or draw per item.
            for(int i=0;i<m.UV.Count;i++)
            {V2 uv=m.UV[i];int row=0; /* one colour family, model geometry supplies shape differences */
                m.UV[i]=new V2((item.Variant+.12f+(uv.X/(1+Math.Abs(uv.X)))*.76f)/8,(row+.12f+(uv.Y/(1+Math.Abs(uv.Y)))*.76f)/8);}
            return m;
        }
        private static void B(MeshData m,float x,float z,float w,float d,float bottom,float top,Surface s)
        {MallArchitecture.Box(m,x,z,w,d,bottom,top,s);}
        private static void Cylinder(MeshData m,V3 bottom,float radius,float height,int n,Surface s)
        {Shapes.Column(m,bottom.X,bottom.Z,radius,bottom.Y,bottom.Y+height,n,s,1);}
        private static void RotateX(MeshData m,float angle)
        {
            float c=(float)Math.Cos(angle),s=(float)Math.Sin(angle);
            for(int i=0;i<m.Positions.Count;i++){V3 p=m.Positions[i];m.Positions[i]=new V3(p.X,c*p.Y-s*p.Z,s*p.Y+c*p.Z);p=m.Normals[i];m.Normals[i]=new V3(p.X,c*p.Y-s*p.Z,s*p.Y+c*p.Z);}
        }
        private static void Sphere(MeshData m,float r,int longitude,int latitude,Surface surface)
        {
            for(int lat=0;lat<latitude;lat++)for(int lon=0;lon<longitude;lon++)
            {
                V3 a=Point(lat,lon,r,longitude,latitude),b=Point(lat,lon+1,r,longitude,latitude),c=Point(lat+1,lon+1,r,longitude,latitude),d=Point(lat+1,lon,r,longitude,latitude);
                m.QuadUV(a,b,c,d,a.Unit,b.Unit,c.Unit,d.Unit,new V2(),new V2(1,0),new V2(1,1),new V2(0,1),surface);
            }
        }
        private static V3 Point(int lat,int lon,float r,int n,int k)
        {float p=Shapes.Pi*lat/k,t=Shapes.Pi*2*lon/n;return new V3(r*(float)Math.Sin(p)*(float)Math.Cos(t),r*(float)Math.Cos(p),r*(float)Math.Sin(p)*(float)Math.Sin(t));}
    }
    public static class MallMerchandise
    {
        public const int PerShop=12;
        public static IEnumerable<MallLooseProp> ForShop(MallShop shop,bool abandoned=false)
        {
            if(shop.Program!=null)return shop.Program.Stock();
            var points=new List<MallLooseProp>();var random=new StableRandom(shop.Seed^0xCA123u);
            MallItemShape[] shapes=Categories(shop.Kind);
            Action<MallItemShape,float,float,float,int,bool> add=(shape,x,top,z,variant,hanging)=>
            {
                int id=(int)shape*8+(variant<0?random.Range(8):variant);var definition=MallItemCatalog.Get(id);
                points.Add(new MallLooseProp(shop.Id*4096+points.Count,shop.World(x,hanging?1.7f-definition.Size.Y*.5f:top+definition.Size.Y*.5f+.003f,z),id,shop.Side==1?0:Shapes.Pi,hanging));
            };
            for(int i=0;i<PerShop;i++)
            {
                var shape=shapes[random.Range(shapes.Length)];
                int side=i<6?-1:1,column=i%3,row=(i%6)/3;
                // Flat garments on the front tables, hanging garments only on actual clothing rails.
                add(shape,side*5+(column-1)*.8f,1,1.95f+(row==0?-.22f:.22f),shape==MallItemShape.Shirt?random.Range(4):-1,false);
            }
            if(abandoned)return points;
            float depth=shop.Depth;
            switch(shop.Kind)
            {
                case MallShopKind.Fashion:
                    for(float z=6;z<depth-7;z+=5)foreach(int side in new[]{-1,1})for(int i=0;i<4;i++)
                        add(MallItemShape.Shirt,side*4.6f,1.06f,z-1.35f+i*.9f,4+random.Range(4),true);
                    for(int i=0;i<4;i++)add(MallItemShape.Shirt,-4+(i%2==0?-.65f:.65f),.8f,depth-3+(i<2?-.5f:.5f),random.Range(4),false);
                    break;
                case MallShopKind.Books:case MallShopKind.Shoes:case MallShopKind.Sports:case MallShopKind.Toys:case MallShopKind.Pharmacy:
                    for(float z=7;z<depth-6;z+=5)foreach(int side in new[]{-1,1})for(int level=0;level<4;level++)for(int col=0;col<3;col++)
                        add(shapes[random.Range(shapes.Length)],side*5+(col-1)*.6f,.32f+level*.45f,z-1.2f,-1,false);
                    if(shop.Kind==MallShopKind.Shoes)for(int i=0;i<2;i++)add(MallItemShape.Shoe,4+(i==0?-.3f:.3f),.8f,depth-5.5f,-1,false);
                    if(shop.Kind==MallShopKind.Sports)for(int i=0;i<3;i++)add(MallItemShape.Ball,-4+i,.8f,depth-4,-1,false);
                    if(shop.Kind==MallShopKind.Toys)for(int i=0;i<3;i++)add(shapes[random.Range(shapes.Length)],-4+(i-1)*.65f,.75f,depth-4,-1,false);
                    break;
                case MallShopKind.Electronics:
                    for(float z=7;z<depth-6;z+=5)foreach(int side in new[]{-1,1})for(int i=0;i<2;i++)
                        add(shapes[random.Range(shapes.Length)],side*4.4f+(i==0?-.55f:.55f),.95f,z,-1,false);break;
                case MallShopKind.Home:
                    foreach(int side in new[]{-1,1})
                    {add(MallItemShape.Vase,side*4.3f,.5f,11,-1,false);add(MallItemShape.Mug,side*4.3f-.4f,.85f,16,-1,false);add(MallItemShape.Plate,side*4.3f+.4f,.85f,16,-1,false);}break;
                case MallShopKind.Cafe:case MallShopKind.FoodCourt:
                    for(float z=10;z<depth-4;z+=6)foreach(int side in new[]{-1,1})
                    {add(MallItemShape.Mug,side*4.3f-.35f,.85f,z,-1,false);add(MallItemShape.Bottle,side*4.3f+.35f,.85f,z,-1,false);}break;
                case MallShopKind.Accessories:
                    for(float z=7;z<depth-3;z+=6)foreach(int side in new[]{-1,1})for(int i=0;i<4;i++)
                        add(shapes[random.Range(shapes.Length)],side*4.4f+(i%2==0?-.55f:.55f),1.2f,z+(i<2?-.55f:.55f),-1,false);break;
                case MallShopKind.Hypermarket:
                    foreach(float x in new[]{-18f,-10f,10f,18f})for(int row=0;row<8;row++)for(int level=0;level<4;level++)
                        add(shapes[random.Range(shapes.Length)],x+(row%2==0?-.46f:.46f),.32f+level*.45f,7.7f+(row/2)*3.1f,-1,false);break;
            }
            return points;
        }
        public static string Key(MallPlan p,MallLooseProp item){return (p.Autonomous?"mall22:":"mall20:")+p.Seed+":"+p.AddressX+":"+p.AddressZ+":"+item.Id;}
        public static MallItemShape[] Categories(MallShopKind kind)
        {
            switch(kind)
            {
                case MallShopKind.Fashion:return new[]{MallItemShape.Shirt,MallItemShape.Cap,MallItemShape.Handbag,MallItemShape.Backpack};
                case MallShopKind.Shoes:return new[]{MallItemShape.Shoe,MallItemShape.Cap,MallItemShape.Backpack};
                case MallShopKind.Electronics:return new[]{MallItemShape.Phone,MallItemShape.Tablet,MallItemShape.Camera,MallItemShape.Headphones,MallItemShape.Gamepad,MallItemShape.Keyboard,MallItemShape.Mouse,MallItemShape.Laptop,MallItemShape.Speaker,MallItemShape.Radio};
                case MallShopKind.Books:return new[]{MallItemShape.Book,MallItemShape.Notebook,MallItemShape.PencilCase,MallItemShape.DiscCase};
                case MallShopKind.Sports:return new[]{MallItemShape.Ball,MallItemShape.Shoe,MallItemShape.Bottle,MallItemShape.Cap,MallItemShape.Backpack};
                case MallShopKind.Home:return new[]{MallItemShape.Mug,MallItemShape.Vase,MallItemShape.Plate,MallItemShape.Towel,MallItemShape.Radio};
                case MallShopKind.Toys:return new[]{MallItemShape.ToyCar,MallItemShape.Blocks,MallItemShape.Ball,MallItemShape.PencilCase};
                case MallShopKind.Pharmacy:case MallShopKind.Restrooms:return new[]{MallItemShape.Soap,MallItemShape.Towel,MallItemShape.Bottle,MallItemShape.PencilCase};
                case MallShopKind.Cafe:case MallShopKind.FoodCourt:return new[]{MallItemShape.Mug,MallItemShape.Bottle,MallItemShape.Snack,MallItemShape.FoodBox,MallItemShape.Plate};
                case MallShopKind.Arcade:return new[]{MallItemShape.Gamepad,MallItemShape.DiscCase,MallItemShape.Headphones,MallItemShape.Snack};
                case MallShopKind.Cinema:return new[]{MallItemShape.DiscCase,MallItemShape.Snack,MallItemShape.Bottle};
                case MallShopKind.Accessories:return new[]{MallItemShape.Watch,MallItemShape.Glasses,MallItemShape.Handbag,MallItemShape.Cap};
                default:return new[]{MallItemShape.Snack,MallItemShape.Bottle,MallItemShape.FoodBox,MallItemShape.Soap,MallItemShape.Notebook,MallItemShape.Plate};
            }
        }
        public static bool RayBox(V3 origin,V3 direction,V3 center,V3 size,float max,out float distance)
        {
            distance=0;float end=max;
            if(float.IsNaN(max)||max<0||direction.Length<.001f)return false;
            for(int axis=0;axis<3;axis++)
            {
                float p=axis==0?origin.X:axis==1?origin.Y:origin.Z,v=axis==0?direction.X:axis==1?direction.Y:direction.Z;
                float c=axis==0?center.X:axis==1?center.Y:center.Z,s=(axis==0?size.X:axis==1?size.Y:size.Z)*.5f;
                if(Math.Abs(v)<1e-7f){if(p<c-s||p>c+s)return false;continue;}
                float lo=(c-s-p)/v,hi=(c+s-p)/v;if(lo>hi){float t=lo;lo=hi;hi=t;}
                distance=Math.Max(distance,lo);end=Math.Min(end,hi);if(end<distance)return false;
            }
            return distance<=max;
        }
    }
}
