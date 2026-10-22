using System;
using System.Globalization;

namespace Poolrooms.Core
{
    public sealed class ProvisionDefinition
    {
        public readonly int Id;
        public readonly string Name,Source,Family,FoodType;
        public readonly bool Drink;
        public readonly V3 Size;
        public string Key { get { return "food:"+Id.ToString(CultureInfo.InvariantCulture); } }
        public float Longest { get { return Math.Max(Size.X,Math.Max(Size.Y,Size.Z)); } }
        public ProvisionDefinition(int id,string name,string source,string family,string foodType,bool drink,V3 size)
        {Id=id;Name=name;Source=source;Family=family;FoodType=foodType;Drink=drink;Size=size;}
    }
    public static partial class ProvisionCatalog
    {
        public const int FoodCount=117,DistinctFoodTypes=103,DrinkCount=8,AlmondWater=124;
        public static int Count { get { return entries.Length; } }
        public static ProvisionDefinition Get(int id)
        {if(id<0||id>=entries.Length)throw new ArgumentOutOfRangeException(nameof(id));return entries[id];}
        public static ExplorationItem Item(int id)
        {
            var d=Get(id);
            return new ExplorationItem(d.Key,d.Name,d.Drink?
                "Una bebida preparada para compartir. Beber es opcional; no hay sed ni medidores.":
                "Comida para llevar o compartir. Comer es opcional; no hay hambre ni medidores.",
                d.Drink?ItemUse.Drink:ItemUse.Eat,true,source:d.Source);
        }
    }
    public readonly struct ProvisionCandidate
    {
        public readonly int Slot;
        public readonly V3 Bottom;
        public ProvisionCandidate(int slot,V3 bottom){Slot=slot;Bottom=bottom;}
    }
    public static class ProvisionDistribution
    {
        public const int MaximumCandidates=36,ItemsPerStation=4;
        public static ProvisionCandidate[] Candidates(int seed,int x,int z,float width,float depth,float floor,bool city=false)
        {
            if(!ScenerySafety.Finite(width)||!ScenerySafety.Finite(depth)||!ScenerySafety.Finite(floor)||width<4||depth<4||width>4096||depth>4096)return Array.Empty<ProvisionCandidate>();
            var points=new ProvisionCandidate[MaximumCandidates];
            uint start=LiminalRegions.Hash(seed,x,z,0xF00110u);
            // Stable slots survive rejected candidates. The small cross-side bays
            // serve narrow galleries; the outer bands serve broad court/house plans.
            for(int i=0;i<points.Length;i++)
            {
                int index=(i+(int)(start%36))%36,corner=index%4,band=index/4;
                float sx=(corner&1)==0?-1:1,sz=(corner&2)==0?-1:1;
                float fx=.16f+(band%3)*.15f,fz=.16f+(band/3)*.15f;
                float px=sx*Math.Min(width*.5f-1.05f,width*fx),pz=sz*Math.Min(depth*.5f-.9f,depth*fz);
                if(city)
                {
                    // Sidewalks are between the road at |axis|=3 and facades at 4.8.
                    // A table is aligned along the street, never across the driving cross.
                    px=sx*(4.25f+(band/3)*.04f);pz=sz*(6.3f+(band%3)*2.1f);
                    if(Math.Abs(pz)>depth*.5f-.9f)pz=sz*(depth*.5f-.9f);
                }
                points[i]=new ProvisionCandidate(index,new V3(px,floor,pz));
            }
            return points;
        }
        public static int Item(int seed,int x,int z,int candidate,int item)
        {
            if(item<0||item>=ItemsPerStation)throw new ArgumentOutOfRangeException(nameof(item));
            if(item==3)return ProvisionCatalog.AlmondWater;
            uint h=LiminalRegions.Hash(seed,x,z,0xF00D00u+(uint)candidate);
            if(item==2&&((h>>8)%4)==0)return ProvisionCatalog.FoodCount+(int)((h>>12)%7);
            // Coprime stride exposes every food type without replacing the
            // seeded selection with a table of recolours or repeated keys.
            return (int)((h%ProvisionCatalog.FoodCount+(uint)(item*37))%ProvisionCatalog.FoodCount);
        }
        public static V3 OnTable(int item)
        {
            if(item<0||item>=ItemsPerStation)throw new ArgumentOutOfRangeException(nameof(item));
            return item==3?new V3(0,SourceFurniture.Dimensions(23).Y+.004f,.23f):new V3((item-1)*.43f,SourceFurniture.Dimensions(23).Y+.004f,-.10f);
        }
        public static string Key(string domain,int seed,int x,int z,int slot,int piece)
        {return "provisions:"+domain+":"+seed.ToString(CultureInfo.InvariantCulture)+":"+x.ToString(CultureInfo.InvariantCulture)+":"+z.ToString(CultureInfo.InvariantCulture)+":"+slot.ToString(CultureInfo.InvariantCulture)+":"+piece.ToString(CultureInfo.InvariantCulture);}
    }
}
