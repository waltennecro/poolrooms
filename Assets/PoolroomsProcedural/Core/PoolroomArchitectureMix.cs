namespace Poolrooms.Core
{
    // Both architectures belong to the ordinary Poolrooms biome. Coordinate climates choose
    // the family; no traversal counter, separate scene, portal, or tool activation is involved.
    public static class PoolroomArchitectureMix
    {
        public static bool Enabled(LayoutConfig c)
        {return c.CommonPoolrooms&&c.Zones!=ZoneMode.Catalog&&c.Zones!=ZoneMode.Single&&c.ZoneFamilyFilter<0;}
        public static bool Applies(LayoutConfig c,int x,int z)
        {return Enabled(c)&&LiminalRegions.At(c,x,z)==LiminalEnvironment.Poolrooms;}
        public static bool UsesSolar(LayoutConfig c,int x,int z)
        {return Applies(c,x,z)&&SolarClimateRegions.HasSky(SolarClimateRegions.At(c,x,z).Kind);}
    }
}
