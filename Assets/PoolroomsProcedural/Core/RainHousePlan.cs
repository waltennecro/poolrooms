using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum RainHouseUse { Reading, Tea, Bedroom, Workshop, GardenRoom }
    public readonly struct RainLodge
    {
        public readonly float X,Z,Width,Depth,WallHeight;
        public readonly int Turns;
        public readonly RainHouseUse Use;
        public RainLodge(float x,float z,float width,float depth,float height,int turns,RainHouseUse use)
        {X=x;Z=z;Width=width;Depth=depth;WallHeight=height;Turns=turns;Use=use;}
        public V3 ToRoom(V3 p){return Turns==0?new V3(X+p.X,p.Y,Z+p.Z):new V3(X-p.X,p.Y,Z-p.Z);}
        public V3 ToLocal(V3 p){return Turns==0?new V3(p.X-X,p.Y,p.Z-Z):new V3(X-p.X,p.Y,Z-p.Z);}
        public V3 Door {get{return ToRoom(new V3(0,0,-Depth*.5f));}}
        public bool Covers(float x,float z,float margin=0)
        {
            var p=ToLocal(new V3(x,0,z));
            return Math.Abs(p.X)<Width*.5f+.28f+margin&&p.Z>-Depth*.5f-1.35f-margin&&p.Z<Depth*.5f+.28f+margin;
        }
    }
    public sealed class RainHousePlan
    {
        public const int BaseCatalogCount=2500,CatalogCount=3750;
        public readonly int Recipe,Programme,Footprint,Roof,Rhythm,Detail;
        public readonly float Clear;
        public readonly RainLodge[] Houses;
        public readonly QuietPropPlan[] Props;
        public RainHousePlan(int recipe,float clear,RainLodge[] houses,QuietPropPlan[] props)
        {Recipe=recipe;Programme=recipe%5;Footprint=recipe/5%5;Roof=recipe/25%5;Rhythm=recipe/125%5;Detail=recipe/625;Clear=clear;Houses=houses;Props=props;}
        public bool InsideLodge(float x,float z)
        {foreach(var h in Houses)if(Math.Abs(x-h.X)<h.Width*.5f&&Math.Abs(z-h.Z)<h.Depth*.5f)return true;return false;}
        public bool Sheltered(float x,float z,float margin=0)
        {
            if(Math.Abs(x)<Clear-.25f+margin||Math.Abs(z)<Clear-.25f+margin)return true;
            foreach(var h in Houses)if(h.Covers(x,z,margin))return true;return false;
        }
        public bool Valid(Room room,LayoutConfig c)
        {
            if(Recipe<0||Recipe>=CatalogCount||room.Environment!=LiminalEnvironment.RainHouses||room.Width!=c.CellSize||room.Depth!=c.CellSize||Houses.Length!=4||Props.Length!=32||Clear<c.DoorWidth*.5f+.7f)return false;
            foreach(var h in Houses)
                if(h.Width<6||h.Depth<5.4f||Math.Abs(h.X)+h.Width*.5f+.28f>c.CellSize*.5f-.1f||Math.Abs(h.Z)+h.Depth*.5f+.28f>c.CellSize*.5f-.1f||Math.Abs(h.X)-h.Width*.5f<Clear||Math.Abs(h.Door.Z)<Clear+1.2f)return false;
            foreach(var p in Props)if(Math.Abs(p.Bottom.X)<Clear||Math.Abs(p.Bottom.Z)<Clear||p.Bottom.Y<0)return false;
            return true;
        }
    }
    public static class RainHousePlanner
    {
        public static int RecipeAt(int seed,int x,int z)
        {
            long xx=((long)x%50+50)%50,zz=((long)z%50+50)%50;
            uint h=LiminalRegions.Hash(seed,ContinuousTopology.FloorDivide(x,50),ContinuousTopology.FloorDivide(z,50),0x240A01u);
            int m=1+2*(int)(h%1250);while(m%5==0)m+=2;
            return (int)(((xx*50+zz)*m+(h>>10))%2500);
        }
        public static void Describe(Room r,LayoutConfig c){DescribeRecipe(r,c,(c.ExpandedVariety?BiomeVariety.RecipeAt(c.Seed,r.X,r.Z,75,50,0x310240u):RecipeAt(c.Seed,r.X,r.Z)));}
        public static void DescribeRecipe(Room r,LayoutConfig c,int recipe)
        {
            if(recipe<0||recipe>=RainHousePlan.CatalogCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            float clear=RainHouseArchitecture.MaxOpening(c)*.5f+.8f,space=c.CellSize*.5f-clear-.65f;
            int footprint=recipe/5%5,detail=recipe/625;var houses=new RainLodge[4];var props=new List<QuietPropPlan>();
            for(int i=0;i<4;i++)
            {
                int sx=i%2==0?-1:1,sz=i<2?-1:1;
                float w=Math.Min(10,space-.45f)*(.85f+.03f*((footprint+i)%5)),d=Math.Min(9,space-1.5f)*(.86f+.03f*((footprint+2+i)%5));
                var h=new RainLodge(sx*(clear+space*.5f),sz*(clear+1.3f+d*.5f),w,d,3.15f+detail*.18f,sz>0?0:2,(RainHouseUse)((recipe%5+i)%5));houses[i]=h;
                // All sources are existing licensed adaptations, on explicitly built supports.
                string chair=h.Use==RainHouseUse.Tea?"B_CafeChair":h.Use==RainHouseUse.Reading?"I_ChairDamaskPurplegold":"U_SheenWoodLeatherSofa";
                props.Add(new QuietPropPlan(h.ToRoom(new V3(-w*.5f+1,.02f,-d*.5f+1.8f)),-1,chair,90+h.Turns*90));
                props.Add(new QuietPropPlan(h.ToRoom(new V3(w*.5f-1.1f,.02f,-d*.5f+1.3f)),-1,"G_PottedPlant",h.Turns*90));
                float tx=w*.5f-.95f,tz=d*.5f-1.05f;
                props.Add(new QuietPropPlan(h.ToRoom(new V3(tx,.02f,tz)),-1,"B_CafeTable",h.Turns*90));
                props.Add(new QuietPropPlan(h.ToRoom(new V3(tx-.2f,.86249021f,tz)),-1,"WaterBottle"));
                props.Add(new QuietPropPlan(h.ToRoom(new V3(tx+.18f,.86249021f,tz)),-1,"T_ArtNouveauLamp",h.Turns*90));
                for(int item=0;item<3;item++)props.Add(new QuietPropPlan(h.ToRoom(new V3(-.55f+item*.55f,.93f,d*.5f-.45f)),(h.Use==RainHouseUse.Reading?0:h.Use==RainHouseUse.Tea?20:31)*8+(recipe+item+i)%8));
            }
            var p=new RainHousePlan(recipe,clear,houses,props.ToArray());r.Environment=LiminalEnvironment.RainHouses;r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=8.5f;
            r.Plan=new ProceduralPlan(r.Environment,new PlannedModule[0],new WaterRegion[0],clear,0,0,rain:p);
            if(!p.Valid(r,c))throw new ArgumentException("Invalid rain lodge plan: "+recipe);
        }
    }
}
