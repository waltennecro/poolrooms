using System;

namespace Poolrooms.Core
{
    public readonly struct SolarRecipe
    {
        public readonly int Id,PoolFamily,Structure,SpatialVariant;
        public string Code { get { return "SA-"+(Id+1).ToString("000"); } }
        public string Name { get { return Code+" / "+SolarCatalog.PoolNames[PoolFamily]+" · "+SolarCatalog.StructureNames[Structure]+" · "+SolarCatalog.SpaceNames[SpatialVariant]; } }
        public SolarRecipe(int id){Id=id;PoolFamily=id%24;Structure=(id/24)%8;SpatialVariant=id/192;}
    }
    public static class SolarCatalog
    {
        // Each axis changes built geometry. Lighting, colour and seed are NOT counted as recipes.
        public const int Count=768;
        public static readonly string[] PoolNames={"Laguna amplia","Dos piscinas hermanas","Carril de natación","Bahía lateral","Terrazas de agua","Estanque redondeado",
            "Canales paralelos","Cuatro baños","Piscina de esquina","Lámina larga","Pozas compactas","Laguna profunda",
            "Piscina de descanso","Baños escalonados","Canal desplazado","Patio acuático","Orillas generosas","Piscina estrecha",
            "Ensenadas dobles","Tres estanques","Carril profundo","Pozas desplazadas","Piscina con descansos","Lago interior"};
        public static readonly string[] StructureNames={"Columnas redondas","Arcadas sucesivas","Muros calados","Pilares cuadrados","Bóvedas","Pórticos dobles","Techo casetonado","Bancos y terrazas"};
        public static readonly string[] SpaceNames={"Sala transversal","Galería longitudinal","Patio con recodos","Salón de doble altura"};
        public static SolarRecipe Get(int id){if(id<0||id>=Count)throw new ArgumentOutOfRangeException(nameof(id));return new SolarRecipe(id);}
        public static int At(LayoutConfig c,int x,int z)
        {
            // Four interleaved coordinate banks keep immediate neighbours from using an identical
            // recipe. There is no sequence of rooms and no distance stage. Within a bank: independent seed hash.
            int bank=(x&1)|((z&1)<<1);
            int raw=bank*192+(int)(LiminalRegions.Hash(c.Seed,x,z,0x1901u)%192);
            // A bijection mixes all four spatial variants into every bank. Coordinate parity
            // therefore avoids an identical neighbour without imposing a checkerboard of room shapes.
            int offset=(int)(LiminalRegions.Hash(c.Seed,0,0,0x1902u)%Count);
            return (raw*149+offset)%Count;
        }
    }
    public enum PoolClimate { Sunlit,DiffuseDaylight,Enclosed,Dim,Sunset }
    public readonly struct PoolClimateDescription
    {
        public readonly PoolClimate Kind;
        public readonly int RegionX,RegionZ;
        public readonly V3 SunDirection,SunColor;
        public readonly float Brightness;
        public PoolClimateDescription(PoolClimate kind,int x,int z,V3 direction,V3 color,float brightness)
        {Kind=kind;RegionX=x;RegionZ=z;SunDirection=direction;SunColor=color;Brightness=brightness;}
    }
    public static class SolarClimateRegions
    {
        public static PoolClimateDescription At(LayoutConfig c,int x,int z)
        {
            int size=PoolroomArchitectureMix.Enabled(c)?c.CommonClimateRegionSize:c.PoolClimateRegionSize,rx=ContinuousTopology.FloorDivide(x,size),rz=ContinuousTopology.FloorDivide(z,size),chosenX=rx,chosenZ=rz;
            double best=double.MaxValue;
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                int a=rx+dx,b=rz+dz;
                double px=(long)a*size+Unit(LiminalRegions.Hash(c.Seed,a,b,0x1910u))*size;
                double pz=(long)b*size+Unit(LiminalRegions.Hash(c.Seed,a,b,0x1911u))*size;
                double xx=x-px,zz=z-pz,distance=xx*xx+zz*zz;
                if(distance<best){best=distance;chosenX=a;chosenZ=b;}
            }
            float pick=Unit(LiminalRegions.Hash(c.Seed,chosenX,chosenZ,0x1912u));PoolClimate kind;
            if(pick<(PoolroomArchitectureMix.Enabled(c)?c.CommonSolarShare:c.SunlitRegionShare))
            {
                float sub=Unit(LiminalRegions.Hash(c.Seed,chosenX,chosenZ,0x1913u));
                kind=sub<.64f?PoolClimate.Sunlit:sub<.89f?PoolClimate.DiffuseDaylight:PoolClimate.Sunset;
            }
            else kind=Unit(LiminalRegions.Hash(c.Seed,chosenX,chosenZ,0x1914u))<.66f?PoolClimate.Enclosed:PoolClimate.Dim;
            float azimuth=Unit(LiminalRegions.Hash(c.Seed,chosenX,chosenZ,0x1915u))*Shapes.Pi*2;
            float slope=kind==PoolClimate.Sunset?.8f:kind==PoolClimate.DiffuseDaylight?.15f:.5f;
            var direction=new V3(Shapes.Cos(azimuth)*slope,-1,Shapes.Sin(azimuth)*slope).Unit;
            V3 color=kind==PoolClimate.Sunset?new V3(1,.74f,.46f):kind==PoolClimate.DiffuseDaylight?new V3(.78f,.9f,1):new V3(1,.96f,.84f);
            return new PoolClimateDescription(kind,chosenX,chosenZ,direction,color,kind==PoolClimate.Sunset?.8f:1);
        }
        public static bool HasSky(PoolClimate climate){return climate==PoolClimate.Sunlit||climate==PoolClimate.DiffuseDaylight||climate==PoolClimate.Sunset;}
        public static ZoneLightStyle LightStyle(PoolClimate climate)
        {return climate==PoolClimate.Dim?ZoneLightStyle.HalfPower:climate==PoolClimate.Enclosed?ZoneLightStyle.CleanWhite:climate==PoolClimate.Sunset?ZoneLightStyle.SunsetRose:ZoneLightStyle.SoftDaylight;}
        private static float Unit(uint n){return (n>>8)*(1f/16777216f);}
    }
}
