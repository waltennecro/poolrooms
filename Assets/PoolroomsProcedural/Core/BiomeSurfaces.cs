namespace Poolrooms.Core
{
    // Six existing mesh slots are retained; a theme selects a wholly separate PBR library.
    public enum SurfaceTheme { Automatic, Poolrooms, IndoorPlay, Supermarket, Mall, Office, Hotel, WaitingHall, DiningHall, Glass, Merchandise, SoftPlay, SoftPlayFinish, SoftPlayNet, SoftPlayFloor, CityStreet, CityFacade, ArchiveWood, CourtyardStone, CoastSand, Airfield, Conservatory, TransitGallery, Orangerie, OrangerieFoliage, ThermalStone, ThermalWicker }
    public static class BiomeSurfaces
    {
        public static SurfaceTheme ForEnvironment(LiminalEnvironment environment)
        {
            switch(environment)
            {
                case LiminalEnvironment.ThermalBaths:return SurfaceTheme.ThermalStone;
                case LiminalEnvironment.Orangerie:return SurfaceTheme.Orangerie;
                case LiminalEnvironment.Archive906:return SurfaceTheme.ArchiveWood;
                case LiminalEnvironment.Conservatory:return SurfaceTheme.Conservatory;
                case LiminalEnvironment.TransitGallery:return SurfaceTheme.TransitGallery;
                case LiminalEnvironment.Courtyard188:return SurfaceTheme.CourtyardStone;
                case LiminalEnvironment.Coast48:return SurfaceTheme.CoastSand;
                case LiminalEnvironment.City11:return SurfaceTheme.CityFacade;
                case LiminalEnvironment.SoftPlay:return SurfaceTheme.SoftPlay;
                case LiminalEnvironment.IndoorPlay:return SurfaceTheme.IndoorPlay;
                case LiminalEnvironment.Supermarket:return SurfaceTheme.Supermarket;
                case LiminalEnvironment.Office:return SurfaceTheme.Office;
                case LiminalEnvironment.Hotel:return SurfaceTheme.Hotel;
                case LiminalEnvironment.WaitingHall:return SurfaceTheme.WaitingHall;
                case LiminalEnvironment.DiningHall:return SurfaceTheme.DiningHall;
                default:return SurfaceTheme.Poolrooms;
            }
        }
        public static SurfaceTheme Resolve(MeshData data)
        {
            if(data.Theme!=SurfaceTheme.Automatic)return data.Theme;
            if(data.RetailPalette>0)
            {
                if(InteriorPalette.Uses(data.PaletteVariant))return (SurfaceTheme)((int)SurfaceTheme.Office+(data.PaletteVariant-4)/3);
                return data.PaletteVariant==3?SurfaceTheme.Mall:SurfaceTheme.Supermarket;
            }
            return data.PlayPalette>0?SurfaceTheme.IndoorPlay:SurfaceTheme.Poolrooms;
        }
        public static string TextureSet(SurfaceTheme theme,Surface slot)
        {
            if(theme==SurfaceTheme.ThermalWicker)return slot==Surface.Plaster?"Fabric":"BathWicker";
            if(theme==SurfaceTheme.ThermalStone)return slot==Surface.Glow?null:slot==Surface.Tile?"BathTravertineDry":slot==Surface.Slide?"BathTravertineWet":slot==Surface.Plaster?"GardenLimewash":slot==Surface.Metal?"Steel":"Wood";
            if(theme==SurfaceTheme.OrangerieFoliage)return "GardenLeaves";
            if(theme==SurfaceTheme.Orangerie)return slot==Surface.Glow?null:slot==Surface.Tile?"GardenLimestone":slot==Surface.Plaster?"GardenLimewash":slot==Surface.Slide?"GardenTerracotta":slot==Surface.Metal?"Steel":"Wood";
            if(theme==SurfaceTheme.Conservatory||theme==SurfaceTheme.TransitGallery)return slot==Surface.Glow?null:slot==Surface.Tile?"Terrazzo":slot==Surface.Metal?"Steel":slot==Surface.Accent?"Wood":"Paint";
            if(theme==SurfaceTheme.Airfield)return slot==Surface.Tile?"AirfieldAsphalt":slot==Surface.Metal?"Steel":slot==Surface.Glow?null:"Paint";
            if(theme==SurfaceTheme.ArchiveWood)return slot==Surface.Glow?null:slot==Surface.Tile||slot==Surface.Accent?"Wood":slot==Surface.Plaster?"Paint":slot==Surface.Metal?"Steel":"Fabric";
            if(theme==SurfaceTheme.CourtyardStone)return slot==Surface.Glow?null:slot==Surface.Tile?"Terrazzo":slot==Surface.Plaster?"Paint":slot==Surface.Accent?"Wood":slot==Surface.Metal?"Steel":"Fabric";
            if(theme==SurfaceTheme.CoastSand)return slot==Surface.Glow?null:slot==Surface.Tile?"QuietSand":slot==Surface.Plaster?"WeatheredPaint":slot==Surface.Accent?"Wood":slot==Surface.Metal?"Steel":"Fabric";
            if(theme==SurfaceTheme.CityStreet)return slot==Surface.Tile?"DustyStone":"Paint";
            if(theme==SurfaceTheme.CityFacade)return slot==Surface.Tile?"DustyStone":slot==Surface.Plaster?"Paint":slot==Surface.Accent?"Wood":slot==Surface.Metal?"Steel":slot==Surface.Glow?null:"RetailVinyl";
            if(theme==SurfaceTheme.SoftPlayFloor)return "PlayFinishFloor";
            if(theme==SurfaceTheme.SoftPlayNet)return "PlayFinishNet";
            if(theme==SurfaceTheme.SoftPlayFinish)return slot==Surface.Glow?null:slot==Surface.Plaster?"PlayFinishWall":slot==Surface.Tile?"PlayFinishGreen":slot==Surface.Accent?"PlayFinishYellow":slot==Surface.Slide?"PlayFinishRed":"PlayFinishBlue";
            if(theme==SurfaceTheme.SoftPlay)return slot==Surface.Glow?null:slot==Surface.Plaster?"PlayWall":slot==Surface.Tile?"PlayFoam":slot==Surface.Accent?"PlayYellow":slot==Surface.Slide?"PlayRed":"PlayBlue";
            if(slot==Surface.Metal)return theme==SurfaceTheme.IndoorPlay?"Rubber":"Steel";
            if(slot==Surface.Glow||theme==SurfaceTheme.Glass)return null;
            if(theme==SurfaceTheme.Merchandise)return "Products";
            if(slot==Surface.Plaster)return theme==SurfaceTheme.Hotel?"Wallpaper":"Paint";
            if(theme==SurfaceTheme.IndoorPlay)return slot==Surface.Tile||slot==Surface.Accent?"Rubber":slot==Surface.Slide?"Fabric":"Plastic";
            if(slot==Surface.Accent)return "Wood";
            if(slot==Surface.Slide)return "Fabric";
            switch(theme)
            {
                case SurfaceTheme.Mall:return "MallStone";
                case SurfaceTheme.Office:return "OfficeCarpet";
                case SurfaceTheme.Hotel:return "HotelCarpet";
                case SurfaceTheme.WaitingHall:return "Vinyl";
                case SurfaceTheme.DiningHall:return "Terrazzo";
                default:return "RetailVinyl";
            }
        }
        public static float Smoothness(string set)
        {
            if(set!=null&&set.StartsWith("PlayFinish",System.StringComparison.Ordinal))return set=="PlayFinishFloor"?.2f:set=="PlayFinishNet"?.15f:set=="PlayFinishWall"?.22f:.48f;
            switch(set){case "BathTravertineDry":return .4f;case "BathTravertineWet":return .68f;case "BathWicker":return .35f;case "GardenLimestone":return .46f;case "GardenTerracotta":return .19f;case "GardenLimewash":return .13f;case "GardenLeaves":return .33f;case "PlayRed":case "PlayYellow":case "PlayBlue":return .38f;case "PlayFoam":return .16f;case "PlayWall":return .2f;case "MallStone":return .76f;case "Terrazzo":return .55f;case "Steel":return .64f;case "Plastic":return .35f;
                case "RetailVinyl":case "Vinyl":return .3f;case "Wood":return .3f;case "Packaging":return .2f;default:return .1f;}
        }
    }
}
