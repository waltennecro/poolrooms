using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // A recipe is a structural parameter tuple. Ten programmes are ten families;
    // these 131,072 combinations per programme must never be advertised as source models.
    public sealed class RestfulLevelPlan
    {
        public const int RecipeCount=131072;
        public readonly int Recipe,Layout,Span,Reach,Elevation,Roof,Rhythm;
        public readonly LiminalEnvironment Environment;
        public readonly float Clear,Height;
        public readonly RegionSection Section;
        public readonly bool Large;
        public readonly PlanRect[] Bays;
        public readonly QuietPropPlan[] Props;
        public readonly WaterRegion[] Pools;
        public RestfulLevelPlan(int recipe,LiminalEnvironment environment,float clear,PlanRect[] bays,QuietPropPlan[] props,WaterRegion[] pools,bool large=false,RegionSection section=RegionSection.Galleries)
        {
            if(recipe<0||recipe>=RecipeCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            Recipe=recipe;Layout=recipe%4;Span=recipe/4%8;Reach=recipe/32%8;
            Elevation=recipe/256%8;Roof=recipe/2048%8;Rhythm=recipe/16384%8;
            Large=large;Section=section;Environment=environment;Clear=clear;Height=large?10.4f:7.2f+Elevation*.42f;Bays=bays;Props=props;Pools=pools;
        }
        public bool Valid(Room room,LayoutConfig c)
        {
            int planted=Environment==LiminalEnvironment.WinterGarden?4:0;
            if(!RestfulLevelPlanner.IsLevel(Environment)||room.Environment!=Environment||room.Width!=c.CellSize||room.Depth!=c.CellSize||room.Height!=Height||Bays==null||Bays.Length!=4||Props==null||Props.Length!=4*(6+3*c.ObjectAbundance)+planted||Clear<c.DoorWidth*.5f+.8f)return false;
            float half=c.CellSize*.5f;
            for(int i=0;i<4;i++)
            {
                var b=Bays[i];if(b.Width<2||b.Depth<2||Math.Abs(b.X)-b.Width*.5f<Clear||Math.Abs(b.Z)-b.Depth*.5f<Clear||Math.Abs(b.X)+b.Width*.5f>half-3.1f||Math.Abs(b.Z)+b.Depth*.5f>half-3.1f)return false;
                for(int j=0;j<i;j++)if(b.Overlaps(Bays[j],.2f))return false;
            }
            foreach(var p in Props)
            {
                if(p.Bottom.Y<0||Math.Abs(p.Bottom.X)<Clear||Math.Abs(p.Bottom.Z)<Clear||Math.Abs(p.Bottom.X)>half-.35f||Math.Abs(p.Bottom.Z)>half-.35f)return false;
                if(p.CatalogId>=0){if(p.CatalogId>=MallItemCatalog.Count)return false;}
                else if(p.Source==null)return false;
            }
            foreach(var pool in Pools)if(pool.Bottom>=pool.Level||pool.Level>=0||Math.Abs(pool.CenterX)-pool.Width*.5f<Clear||Math.Abs(pool.CenterZ)-pool.Depth*.5f<Clear)return false;
            return true;
        }
    }
    public static class RestfulLevelPlanner
    {
        public const int LevelCount=10,EnvironmentCount=38;
        public static bool IsLevel(LiminalEnvironment e){return SpecialRegionPlanner.IsLevel(e)||(int)e>=(int)LiminalEnvironment.SunsetLibrary&&(int)e<= (int)LiminalEnvironment.MineralSanctuary;}
        public static bool IsMode(LiminalGeneration m){return (int)m>=(int)LiminalGeneration.SunsetLibrary&&(int)m<=(int)LiminalGeneration.MineralSanctuary;}
        public static LiminalEnvironment ForMode(LiminalGeneration m)
        {if(!IsMode(m))throw new ArgumentOutOfRangeException(nameof(m));return (LiminalEnvironment)((int)LiminalEnvironment.SunsetLibrary+(int)m-(int)LiminalGeneration.SunsetLibrary);}
        public static bool IsTheme(SurfaceTheme t){return (int)t>=(int)SurfaceTheme.SunsetLibrary&&(int)t<=(int)SurfaceTheme.MineralSanctuary;}
        public static SurfaceTheme Theme(LiminalEnvironment e)
        {if(SpecialRegionPlanner.IsLevel(e))return SpecialRegionPlanner.Theme(e);if(!IsLevel(e))throw new ArgumentOutOfRangeException(nameof(e));return (SurfaceTheme)((int)SurfaceTheme.SunsetLibrary+(int)e-(int)LiminalEnvironment.SunsetLibrary);}
        public static string Name(LiminalEnvironment e)
        {
            if(SpecialRegionPlanner.IsLevel(e))return SpecialRegionPlanner.Name(e);
            switch(e)
            {
                case LiminalEnvironment.SunsetLibrary:return "Biblioteca del atardecer";
                case LiminalEnvironment.CloudTerminal:return "Terminal de nubes";
                case LiminalEnvironment.TeaPavilions:return "Pabellones de té";
                case LiminalEnvironment.CeramicGalleria:return "Galería de cerámica";
                case LiminalEnvironment.WinterGarden:return "Jardín de invierno";
                case LiminalEnvironment.CanalGalleries:return "Galerías de los canales";
                case LiminalEnvironment.MusicStudios:return "Estudios del silencio";
                case LiminalEnvironment.LanternCourts:return "Patios de faroles";
                case LiminalEnvironment.LinenLaundries:return "Lavanderías de lino";
                case LiminalEnvironment.MineralSanctuary:return "Santuario mineral";
                default:throw new ArgumentOutOfRangeException(nameof(e));
            }
        }
        public static string TextureSet(SurfaceTheme t,Surface s)
        {
            if(s==Surface.Glow)return null;if(s==Surface.Metal)return "Steel";
            if(s==Surface.Plaster)return t==SurfaceTheme.MusicStudio?"Fabric":"GardenLimewash";
            if(s==Surface.Accent)return t==SurfaceTheme.CeramicGalleria?"GardenTerracotta":"Wood";
            if(s==Surface.Slide)return t==SurfaceTheme.WinterGarden?"GardenTerracotta":t==SurfaceTheme.CanalGallery||t==SurfaceTheme.MineralSanctuary?"BathTravertineWet":"Fabric";
            return t==SurfaceTheme.SunsetLibrary||t==SurfaceTheme.TeaPavilion?"RainOak":t==SurfaceTheme.CloudTerminal||t==SurfaceTheme.LinenLaundry?"Terrazzo":t==SurfaceTheme.CeramicGalleria?"GardenTerracotta":"GardenLimestone";
        }
        public static int RecipeAt(LayoutConfig c,LiminalEnvironment e,int x,int z)
        {return BiomeVariety.RecipeAt(c.Seed,x,z,512,256,0x330100u+(uint)e);}
        public static void Describe(Room r,LayoutConfig c){DescribeRecipe(r,c,RecipeAt(c,r.Environment,r.X,r.Z));}
        public static void DescribeRecipe(Room r,LayoutConfig c,int recipe)
        {
            if(!IsLevel(r.Environment)||recipe<0||recipe>=RestfulLevelPlan.RecipeCount)throw new ArgumentOutOfRangeException(nameof(recipe));
            float half=c.CellSize*.5f,clear=Math.Max(c.DoorWidth*.5f+.85f,3.1f),inner=clear+.4f,outer=half-3.3f;
            float available=outer-inner,span=available*(.58f+(recipe/4%8)*.045f),reach=available*(.58f+(recipe/32%8)*.045f);
            float center=(inner+outer)*.5f;var bays=new PlanRect[4];var props=new List<QuietPropPlan>();var pools=new List<WaterRegion>();
            int kind=(int)r.Environment-(int)LiminalEnvironment.SunsetLibrary;
            for(int i=0;i<4;i++)
            {
                int sx=i%2==0?-1:1,sz=i<2?-1:1;float x=sx*center,z=sz*center;
                bays[i]=new PlanRect(x-span*.5f,z-reach*.5f,x+span*.5f,z+reach*.5f);
                // A physically separate rear alcove supports all loose props. It is not a
                // random point in the room and never overlaps a pool, doorway or column.
                float tx=x,tz=sz*(half-1.25f),y=RestfulLevelArchitecture.TableTop;
                props.Add(new QuietPropPlan(new V3(tx+sx*1.95f,.006f,tz),-1,kind==0?"I_ChairDamaskPurplegold":kind==4?"SheenChair":"B_CafeChair",sx<0?90:-90));
                props.Add(new QuietPropPlan(new V3(tx-sx*2.0f,.006f,tz),-1,"G_PottedPlant"));
                props.Add(new QuietPropPlan(new V3(tx+.8f,y+.006f,tz),-1,"WaterBottle"));
                props.Add(new QuietPropPlan(new V3(tx+.6f,y+.006f,tz-.38f),-1,"R_Teacup"));
                props.Add(new QuietPropPlan(new V3(tx+1.05f,y+.006f,tz+.38f),-1,"R_FlowerVase"));
                props.Add(new QuietPropPlan(new V3(sx*(half-1),.006f,sz*(clear+1.5f)),-1,"R_BarnLamp",sx<0?90:-90));
                int shape=kind==0?0:kind==2?20:kind==3?22:kind==6?19:kind==8?31:1;
                for(int n=0;n<3*c.ObjectAbundance;n++)
                {
                    float px=tx-.63f+(n%3)*.42f,pz=tz-.38f+(n/3)*.38f;
                    props.Add(new QuietPropPlan(new V3(px,y+.006f,pz),shape*8+(recipe+i+n)%8));
                }
                if(r.Environment==LiminalEnvironment.CanalGalleries||r.Environment==LiminalEnvironment.MineralSanctuary||r.Environment==LiminalEnvironment.IndoorMarina)
                    pools.Add(new WaterRegion{CenterX=x,CenterZ=z,Width=span-.6f,Depth=reach-.6f,Bottom=-.72f,Level=c.WaterLevel,Radius=0});
            }
            // Append after all original slots: saved/taken pickup IDs retain their
            // meaning. Existing alpha-masked licensed foliage uses its own PBR maps.
            if(r.Environment==LiminalEnvironment.WinterGarden)
                foreach(var bay in bays)props.Add(new QuietPropPlan(new V3(bay.X,.401f,bay.Z),-1,"G_PottedPlant",(recipe%4)*90));
            if(SpecialRegionPlanner.IsLevel(r.Environment))SpecialRegionPlanner.Furnish(r.Environment,props);
            var plan=new RestfulLevelPlan(recipe,r.Environment,clear,bays,props.ToArray(),pools.ToArray(),LargeRegions.Enabled(c),LargeRegions.Section(c,r.X,r.Z,r.Environment));
            r.ZoneId=-1;r.Width=r.Depth=c.CellSize;r.Height=plan.Height;
            r.Plan=new ProceduralPlan(r.Environment,new PlannedModule[0],plan.Pools,clear,0,0,restful:plan);
            if(!plan.Valid(r,c))throw new ArgumentException("Invalid restful level recipe "+r.Environment+" / "+recipe);
        }
    }
}
