using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum MallShopKind { Fashion, Shoes, Electronics, Books, Sports, Home, Toys, Pharmacy, Cafe, FoodCourt, Arcade, Cinema, Accessories, Restrooms, Hypermarket }
    public readonly struct MallRect
    {
        public readonly float X0,Z0,X1,Z1;
        public float Width { get { return X1-X0; } }
        public float Depth { get { return Z1-Z0; } }
        public MallRect(float x0,float z0,float x1,float z1){X0=x0;Z0=z0;X1=x1;Z1=z1;}
        public bool Contains(float x,float z,float inset=0){return x>=X0+inset&&x<=X1-inset&&z>=Z0+inset&&z<=Z1-inset;}
        public IEnumerable<MallRect> Subtract(MallRect hole)
        {
            float a=Math.Max(X0,hole.X0),b=Math.Min(X1,hole.X1),c=Math.Max(Z0,hole.Z0),d=Math.Min(Z1,hole.Z1);
            if(a>=b||c>=d){yield return this;yield break;}
            if(a>X0)yield return new MallRect(X0,Z0,a,Z1);
            if(b<X1)yield return new MallRect(b,Z0,X1,Z1);
            if(c>Z0)yield return new MallRect(a,Z0,b,c);
            if(d<Z1)yield return new MallRect(a,d,b,Z1);
        }
    }
    public sealed class MallShop
    {
        public readonly int Id,Floor,Side,Sector;
        public readonly MallShopKind Kind;
        public readonly float X,Width,Depth;
        public readonly uint Seed;
        public readonly bool Refined,Realistic;
        public readonly MallGeneratedProgram Program;
        public int FurnishingMirror { get { return Refined&&(Seed&1)!=0?-1:1; } }
        public string Name { get { return MallPlan.ShopName(Kind); } }
        public MallShop(int id,int floor,int side,int sector,MallShopKind kind,float x,float width,float depth,uint seed,bool refined=false,bool autonomous=false,bool realistic=false)
        {Realistic=realistic;Id=id;Floor=floor;Side=side;Sector=sector;Kind=kind;X=x;Width=width;Depth=depth;Seed=seed;Refined=refined;Program=autonomous?new MallGeneratedProgram(this):null;}
        public V3 World(float x,float y,float z){return new V3(X+Side*x*FurnishingMirror,Floor*MallPlan.FloorHeight+y,Side*(MallPlan.ShopFront+z));}
        public V3 Entrance { get { return World(0,.1f,-1); } }
    }
    public sealed class MallPlan
    {
        public const float FloorHeight=5.4f,ShopFront=22,BayWidth=16;
        public readonly int Seed,AddressX,AddressZ,Floors,Bays;
        public readonly bool Enhanced,Refined,Autonomous,ExpandedArea,Spacious,Realistic;
        public readonly MallSpatialDesign Design;
        public readonly float Width,Depth,ShopHalf;
        public readonly MallShop[] Shops;
        public V3 Spawn { get { return new V3(0,.1f,-18); } }
        public int SectorCount { get { return Bays+2; } }
        private MallPlan(int seed,int x,int z,int floors,int bays,float depth,MallShop[] shops,bool enhanced,bool refined,bool autonomous,bool larger,bool spacious,bool realistic)
        {Realistic=realistic&&enhanced&&refined&&autonomous;Spacious=spacious;ExpandedArea=larger;Enhanced=enhanced;Refined=enhanced&&refined;Autonomous=Enhanced&&Refined&&autonomous;Seed=seed;AddressX=x;AddressZ=z;Floors=floors;Bays=bays;ShopHalf=bays*BayWidth*.5f;Width=ShopHalf*2+56;Depth=depth;Shops=shops;Design=Refined?new MallSpatialDesign(this):null;}
        public static MallPlan Create(int seed,int x,int z,bool enhanced=false,bool refined=false,bool autonomous=false,bool expanded=false,bool larger=false,bool spacious=false,bool realistic=false)
        {
            var random=new StableRandom(LiminalRegions.Hash(seed,x,z,0xCA110u));
            int floors,bays;float depth;Dimensions(ref random,seed,x,z,expanded,larger,out floors,out bays,out depth,spacious);
            var shops=new List<MallShop>();
            var kinds=new List<MallShopKind>();
            foreach(MallShopKind k in Enum.GetValues(typeof(MallShopKind)))if(k!=MallShopKind.Hypermarket&&k!=MallShopKind.Restrooms)kinds.Add(k);
            // Reshuffle each bag: category coverage never becomes a repeated shop sequence.
            Shuffle(kinds,ref random);
            int ordinal=0;
            for(int floor=0;floor<floors;floor++)for(int side=-1;side<=1;side+=2)for(int bay=0;bay<bays;)
            {
                bool anchor=floor==0&&side==1&&bay==0;int span=anchor?3:1;
                MallShopKind kind;
                if(anchor)kind=MallShopKind.Hypermarket;
                else if(bay==bays-1&&side==-1)kind=MallShopKind.Restrooms;
                else
                {
                    if(ordinal>0&&ordinal%kinds.Count==0)Shuffle(kinds,ref random);
                    kind=kinds[ordinal++%kinds.Count];
                }
                shops.Add(new MallShop(shops.Count,floor,side,bay+1+(span-1)/2,kind,-bays*BayWidth*.5f+(bay+span*.5f)*BayWidth,span*BayWidth,depth*.5f-ShopFront-.4f,random.Next(),enhanced&&refined,enhanced&&refined&&autonomous,realistic&&enhanced&&refined&&autonomous));bay+=span;
            }
            return new MallPlan(seed,x,z,floors,bays,depth,shops.ToArray(),enhanced,refined,autonomous,larger,spacious,realistic);
        }
        public static void Dimensions(ref StableRandom random,int seed,int x,int z,bool expanded,bool larger,out int floors,out int bays,out float depth,bool spacious=false)
        {
            floors=2+random.Range(5);bays=8+random.Range(5);depth=SelectDepth(ref random,seed,x,z,expanded);
            if(spacious){bays=24+(int)(LiminalRegions.Hash(seed,x,z,0x410CA1u)%5);depth=180+16*(LiminalRegions.Hash(seed,x,z,0x410CA2u)%4);return;}
            if(!larger)return;
            float width=bays*BayWidth+56;
            bays=(int)Math.Ceiling(bays*BiomeExtent.AreaMultiplier);
            depth=(float)(depth*BiomeExtent.AreaMultiplier*width/(bays*BayWidth+56));
        }
        public static float SelectDepth(ref StableRandom random,int seed,int x,int z,bool expanded)
        {
            float legacy=random.Range(2)==0?96:112;
            return expanded&&LiminalRegions.Hash(seed,x,z,0x310CA1u)%3==2?128:legacy;
        }
        private static void Shuffle(List<MallShopKind> kinds,ref StableRandom random)
        {for(int i=kinds.Count-1;i>0;i--){int j=random.Range(i+1);var tmp=kinds[i];kinds[i]=kinds[j];kinds[j]=tmp;}}
        public int FloorAt(float y){return Math.Max(0,Math.Min(Floors-1,(int)Math.Floor((y+.15f)/FloorHeight)));}
        public int SectorAt(float x){if(x<-ShopHalf)return 0;if(x>=ShopHalf)return Bays+1;return 1+(int)Math.Floor((x+ShopHalf)/BayWidth);}
        public float SectorCenter(int sector){return sector==0?-ShopHalf-14:sector==Bays+1?ShopHalf+14:-ShopHalf+(sector-.5f)*BayWidth;}
        public MallRect StairHole(int side){float a=side*(ShopHalf+7),b=side*(ShopHalf+13);return new MallRect(Math.Min(a,b)-2.2f,-12,Math.Max(a,b)+2.2f,2.6f);}
        public List<MallRect> FloorPlates(int floor)
        {
            var result=new List<MallRect>{new MallRect(-Width*.5f,-Depth*.5f,Width*.5f,Depth*.5f)};
            if(floor==0)return result;
            var holes=Refined?new List<MallRect>(Design.Voids(floor)):new List<MallRect>{new MallRect(-ShopHalf,-12,ShopHalf,12)};
            holes.Add(StairHole(-1));holes.Add(StairHole(1));
            foreach(var hole in holes)
            {var next=new List<MallRect>();foreach(var p in result)next.AddRange(p.Subtract(hole));result=next;}
            return result;
        }
        public bool FloorSupported(int floor,float x,float z){foreach(var p in FloorPlates(floor))if(p.Contains(x,z))return true;return false;}
        public bool Valid()
        {
            if(Floors<2||Floors>6||Bays<(Spacious?24:ExpandedArea?12:8)||Bays>(Spacious?28:ExpandedArea?18:12)||SectorCount>=32||Shops.Length<30)return false;
            var catalog=new HashSet<MallShopKind>();int anchors=0;
            foreach(var s in Shops)
            {
                catalog.Add(s.Kind);if(s.Kind==MallShopKind.Hypermarket)anchors++;
                if(s.Floor<0||s.Floor>=Floors||Math.Abs(s.Side)!=1||s.Width<16||s.Depth<20||s.Sector<1||s.Sector>Bays)return false;
                if(!FloorSupported(s.Floor,s.X,s.Side*18))return false;
            }
            return anchors==1&&catalog.Count==Enum.GetValues(typeof(MallShopKind)).Length;
        }
        public static string ShopName(MallShopKind kind)
        {
            switch(kind)
            {
                case MallShopKind.Fashion:return "MODA";case MallShopKind.Shoes:return "CALZADO";case MallShopKind.Electronics:return "TECNO 2010";
                case MallShopKind.Books:return "LIBRERIA";case MallShopKind.Sports:return "DEPORTES";case MallShopKind.Home:return "HOGAR";
                case MallShopKind.Toys:return "JUGUETES";case MallShopKind.Pharmacy:return "FARMACIA";case MallShopKind.Cafe:return "CAFE";
                case MallShopKind.FoodCourt:return "COMIDAS";case MallShopKind.Arcade:return "ARCADE";case MallShopKind.Cinema:return "CINE";
                case MallShopKind.Accessories:return "ACCESORIOS";case MallShopKind.Restrooms:return "SERVICIOS";default:return "HIPERMERCADO";
            }
        }
    }
    public static class CarryModel
    {
        public const float Reach=2.6f;
        public static bool CanGrab(bool occupied,bool inputCaptured,bool paused,float distance)
        {return !occupied&&!inputCaptured&&!paused&&!float.IsNaN(distance)&&!float.IsInfinity(distance)&&distance>=0&&distance<=Reach;}
    }
}
