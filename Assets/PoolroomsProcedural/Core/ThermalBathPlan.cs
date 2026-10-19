using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum BathHall { ThermalCourt, MineralGallery, TeaLounge, WarmCloister, SkylightBath }
    public sealed class ThermalBathPlan
    {
        public const int BaseCatalogCount=2500,CatalogCount=3750;
        public readonly int Recipe,Shape,Roof,Rhythm,DepthClass;
        public readonly BathHall Hall;
        public readonly uint Seed;
        public readonly float Clear,Height;
        public readonly WaterRegion[] Pools;
        public readonly PlanRect[] Courts;
        public readonly QuietPropPlan[] Props;
        public ThermalBathPlan(int recipe,uint seed,float clear,WaterRegion[] pools,PlanRect[] courts,QuietPropPlan[] props)
        {
            Recipe=recipe;Hall=(BathHall)(recipe%5);Shape=recipe/5%5;Roof=recipe/25%5;Rhythm=recipe/125%5;DepthClass=recipe/625;
            Seed=seed;Clear=clear;Height=7.8f+DepthClass*.5f;Pools=pools;Courts=courts;Props=props;
        }
        public bool Valid(Room r,LayoutConfig c)
        {
            if(Recipe<0||Recipe>=CatalogCount||r.Environment!=LiminalEnvironment.ThermalBaths||r.Width!=c.CellSize||r.Depth!=c.CellSize||r.Height!=Height||Pools==null||Pools.Length<1||Pools.Length>4||Courts==null||Courts.Length!=4||Props==null||Props.Length!=16)return false;
            if(Clear<ThermalBathArchitecture.MaxOpening(c)*.5f+.8f)return false;
            foreach(var p in Pools)
            {
                int steps=(int)Math.Round(-p.Bottom/.21f);
                if(steps<2||steps>5||Math.Min(p.Width,p.Depth)-steps*.6f<1.2f||p.Level!=c.WaterLevel||p.Bottom>=p.Level||p.Radius<0||p.Radius>Math.Min(p.Width,p.Depth)*.5f)return false;
                if(Math.Abs(p.CenterX)-p.Width*.5f<Clear+.5f||Math.Abs(p.CenterZ)-p.Depth*.5f<Clear+.5f||Math.Abs(p.CenterX)+p.Width*.5f>c.CellSize*.5f-2||Math.Abs(p.CenterZ)+p.Depth*.5f>c.CellSize*.5f-2)return false;
            }
            for(int i=0;i<Pools.Length;i++)for(int j=0;j<i;j++)if(Math.Abs(Pools[i].CenterX-Pools[j].CenterX)<(Pools[i].Width+Pools[j].Width)*.5f+.3f&&Math.Abs(Pools[i].CenterZ-Pools[j].CenterZ)<(Pools[i].Depth+Pools[j].Depth)*.5f+.3f)return false;
            foreach(var p in Props)if(Math.Abs(p.Bottom.X)<Clear+.2f||Math.Abs(p.Bottom.Z)<Clear+.2f||p.Bottom.Y<0)return false;
            return true;
        }
    }
    public static class ThermalBathPlanner
    {
        public static int RecipeAt(int seed,int x,int z)
        {
            long xx=((long)x%50+50)%50,zz=((long)z%50+50)%50;
            uint h=LiminalRegions.Hash(seed,ContinuousTopology.FloorDivide(x,50),ContinuousTopology.FloorDivide(z,50),0x210BA1u);
            int multiplier=1+2*(int)(h%1250);while(multiplier%5==0)multiplier+=2;
            return (int)(((zz+xx*50)*multiplier+(h>>10))%2500);
        }
        public static bool WetCourt(BathHall hall,int index)
        {return hall==BathHall.ThermalCourt||hall==BathHall.MineralGallery&&(index==0||index==3)||hall==BathHall.TeaLounge&&index==0||hall==BathHall.WarmCloister&&(index==1||index==2)||hall==BathHall.SkylightBath&&index!=3;}
        public static void Describe(Room r,LayoutConfig c){DescribeRecipe(r,c,(c.ExpandedVariety?BiomeVariety.RecipeAt(c.Seed,r.X,r.Z,75,50,0x310210u):RecipeAt(c.Seed,r.X,r.Z)));}
        public static void DescribeRecipe(Room r,LayoutConfig c,int recipe)
        {
            if(recipe<0||recipe>=ThermalBathPlan.CatalogCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            var hall=(BathHall)(recipe%5);int shape=recipe/5%5,depth=recipe/625%4;
            float half=c.CellSize*.5f,clear=ThermalBathArchitecture.MaxOpening(c)*.5f+.9f,center=(clear+half-2.2f)*.5f;
            float span=Math.Min(8.4f,(half-2.2f-clear-1.2f)*.86f);
            float w=Math.Max(4.3f,span*(shape==1?.9f:shape==3?1:.95f)),d=Math.Max(4.3f,span*(shape==1?1:shape==3?.9f:.96f));
            float radius=shape==0?0:shape==1?.35f:shape==2?.8f:shape==3?1.25f:Math.Min(w,d)*.5f;
            if(shape==4)w=d=Math.Min(w,d);
            var pools=new List<WaterRegion>();var courts=new PlanRect[4];var props=new List<QuietPropPlan>();
            for(int i=0;i<4;i++)
            {
                int sx=i%2==0?-1:1,sz=i<2?-1:1;float x=sx*center,z=sz*center;
                courts[i]=new PlanRect(x-w*.5f,z-d*.5f,x+w*.5f,z+d*.5f);
                if(WetCourt(hall,i))pools.Add(new WaterRegion{CenterX=x,CenterZ=z,Width=w,Depth=d,Radius=radius,Bottom=-(depth+2)*.21f,Level=c.WaterLevel});
                float tx=sx*(half-1.05f),tz=sz*center;
                props.Add(new QuietPropPlan(new V3(sx*(half-1.2f),.02f,tz+sz*1.6f),-1,hall==BathHall.TeaLounge?"B_CafeChair":"U_SheenWoodLeatherSofa",sx<0?90:-90));
                props.Add(new QuietPropPlan(new V3(tx,.02f,tz-sz*1.1f),-1,"B_CafeTable"));
                props.Add(new QuietPropPlan(new V3(tx-.21f,.86249021f,tz-sz*1.1f),-1,"WaterBottle"));
                props.Add(i%2==0?new QuietPropPlan(new V3(tx+.17f,.86249021f,tz-sz*1.1f),-1,"T_ArtNouveauLamp"):
                    new QuietPropPlan(new V3(tx+.16f,.86249021f,tz-sz*1.1f),(hall==BathHall.TeaLounge?20:31)*8+(recipe+i)%8));
            }
            uint seed=LiminalRegions.Hash(c.Seed,r.X,r.Z,0x210BA2u);var plan=new ThermalBathPlan(recipe,seed,clear,pools.ToArray(),courts,props.ToArray());
            r.Environment=LiminalEnvironment.ThermalBaths;r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=plan.Height;
            r.Plan=new ProceduralPlan(r.Environment,new PlannedModule[0],plan.Pools,clear,0,0,baths:plan);
            if(!plan.Valid(r,c))throw new ArgumentException("Invalid thermal bath route, pool or furnishing plan.");
        }
    }
}
