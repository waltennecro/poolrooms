using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum GardenHall { PalmCourt, ReadingGarden, TeaGallery, FernHall, DryGarden }
    public sealed class OrangeriePlan
    {
        public const int CatalogCount=2500;
        public readonly int Recipe,Roof,Bay,Colonnade,Canopy;
        public readonly GardenHall Hall;
        public readonly uint Seed;
        public readonly PlanRect[] Beds;
        public readonly QuietPropPlan[] Props;
        public readonly float Clear,Height;
        public OrangeriePlan(int recipe,uint seed,PlanRect[] beds,QuietPropPlan[] props,float clear)
        {
            if(recipe<0||recipe>=CatalogCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            Recipe=recipe;Hall=(GardenHall)(recipe%5);Roof=recipe/5%5;Bay=recipe/25%5;
            Colonnade=recipe/125%5;Canopy=recipe/625;Seed=seed;Beds=beds;Props=props;Clear=clear;Height=8+Canopy*1.2f;
        }
        public bool Valid(Room room,LayoutConfig c)
        {
            if(room.Environment!=LiminalEnvironment.Orangerie||room.Width!=c.CellSize||room.Depth!=c.CellSize||room.Height!=Height||Beds==null||Beds.Length!=4||Props==null||Props.Length!=16||Clear<c.DoorWidth*.5f+.8f)return false;
            float half=c.CellSize*.5f;
            for(int i=0;i<Beds.Length;i++)
            {
                var b=Beds[i];if(b.Width<1||b.Depth<1||Math.Abs(b.X)-b.Width*.5f<Clear+.5f||Math.Abs(b.Z)-b.Depth*.5f<Clear+.5f||Math.Abs(b.X)+b.Width*.5f>half-2.4f||Math.Abs(b.Z)+b.Depth*.5f>half-2.4f)return false;
                for(int j=0;j<i;j++)if(b.Overlaps(Beds[j],.1f))return false;
            }
            foreach(var p in Props)
                if(p.Bottom.Y<0||Math.Abs(p.Bottom.X)<Clear+.1f||Math.Abs(p.Bottom.Z)<Clear+.1f||Math.Abs(p.Bottom.X)>half-.65f||Math.Abs(p.Bottom.Z)>half-.65f)return false;
            return true;
        }
        public static string Name(GardenHall hall)
        {switch(hall){case GardenHall.PalmCourt:return "Patio de palmeras";case GardenHall.ReadingGarden:return "Jardín de lectura";case GardenHall.TeaGallery:return "Galería de té";case GardenHall.FernHall:return "Salón de helechos";default:return "Jardín mineral";}}
    }
    public static class OrangeriePlanner
    {
        // A seed-specific affine permutation visits every recipe exactly once in each
        // 50 x 50 coordinate tile. The topology still chooses arbitrary routes/regions.
        public static int RecipeAt(int seed,int x,int z)
        {
            long xx=((long)x%50+50)%50,zz=((long)z%50+50)%50;
            int gx=ContinuousTopology.FloorDivide(x,50),gz=ContinuousTopology.FloorDivide(z,50);
            uint h=LiminalRegions.Hash(seed,gx,gz,0x190A01u);
            int multiplier=1+2*(int)(h%1250);while(multiplier%5==0)multiplier+=2;
            return (int)(((xx+zz*50)*multiplier+(h>>12))%OrangeriePlan.CatalogCount);
        }
        public static void Describe(Room room,LayoutConfig c)
        {DescribeRecipe(room,c,RecipeAt(c.Seed,room.X,room.Z));}
        public static void DescribeRecipe(Room room,LayoutConfig c,int recipe)
        {
            if(recipe<0||recipe>=OrangeriePlan.CatalogCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            room.Environment=LiminalEnvironment.Orangerie;room.ZoneId=-1;room.Width=room.Depth=c.CellSize;
            uint seed=LiminalRegions.Hash(c.Seed,room.X,room.Z,0x190A02u);int bay=recipe/25%5,hall=recipe%5;
            float half=c.CellSize*.5f,clear=OrangerieArchitecture.MaxOpening(c)*.5f+.8f;
            float center=(clear+half-2.4f)*.5f,available=half-2.4f-clear-1.1f;
            float width=Math.Min(4.6f,available*(.53f+bay*.045f)),depth=Math.Min(5.8f,available*(.66f+hall*.035f));
            var beds=new PlanRect[4];var props=new List<QuietPropPlan>();
            for(int i=0;i<4;i++)
            {
                int sx=i%2==0?-1:1,sz=i<2?-1:1;float x=sx*center,z=sz*center;
                // Alternating courts rotate their long beds, changing the actual walking bays.
                float w=(bay+i)%2==0?width:depth,d=(bay+i)%2==0?depth:width;
                beds[i]=new PlanRect(x-w*.5f,z-d*.5f,x+w*.5f,z+d*.5f);
                float furnitureX=sx*(half-1.3f),furnitureZ=sz*center;
                string seat=hall==1?"I_ChairDamaskPurplegold":hall==2?"B_CafeChair":hall==4?"SheenChair":"U_SheenWoodLeatherSofa";
                float yaw=sx<0?90:-90;
                // The table and two supported objects use a separate, wall-side pocket.
                props.Add(new QuietPropPlan(new V3(furnitureX,.02f,furnitureZ+sz*1.5f),-1,seat,yaw));
                float tx=sx*(half-1.1f),tz=furnitureZ-sz*1.2f;
                props.Add(new QuietPropPlan(new V3(tx,.02f,tz),-1,"B_CafeTable"));
                props.Add(new QuietPropPlan(new V3(tx-.16f,.86249021f,tz),-1,"WaterBottle"));
                props.Add(new QuietPropPlan(new V3(tx+.15f,.86249021f,tz),((hall==1?0:20)*8)+(recipe+i)%8));
            }
            var p=new OrangeriePlan(recipe,seed,beds,props.ToArray(),clear);room.Height=p.Height;
            room.Plan=new ProceduralPlan(room.Environment,new PlannedModule[0],new WaterRegion[0],clear,0,0,garden:p);
            if(!p.Valid(room,c))throw new ArgumentException("Invalid Orangerie circulation or furnishing plan.");
        }
    }
}
