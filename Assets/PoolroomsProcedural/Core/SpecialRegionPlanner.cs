using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Ten architectural programmes, not ten renamed palettes. All serialized values
    // append after the original 28 environments; the old recipe banks stay unchanged.
    public static class SpecialRegionPlanner
    {
        public const int FirstEnvironment=28,Count=10;
        static readonly string[] Names={"Observatorio solar","Mercado cubierto","Taller textil","Galería postal","Marina interior","Galería de relojes","Taller de papel","Pabellón de ajedrez","Salón de radio","Galería de espejos"};
        static readonly string[] Notes={
            "Dejamos el telescopio junto al tragaluz. Esta tarde vamos a dibujar el cielo; hay papel para quien llegue primero.",
            "El mercado descansa después de comer. Dejamos algunas cajas preparadas y agua fresca para la próxima visita.",
            "La tela sigue en el bastidor. Nos reuniremos aquí para terminarla; mientras tanto puedes elegir los colores del próximo trabajo.",
            "Las postales esperan a sus viajeros. Si encuentras un lugar bonito, deja una nota para que podamos visitarlo también.",
            "Las pequeñas barcas se quedan bajo techo. Hemos preparado té junto al embarcadero para cuando vuelva el grupo.",
            "Nadie tiene prisa en esta galería. Volveremos después de merendar para poner los relojes a la misma hora.",
            "Las hojas están secándose. Hemos guardado los dibujos de quienes pasaron antes; puedes añadir uno al tablón.",
            "La partida quedó preparada para mañana. Mientras esperamos puedes resolver un rompecabezas y descansar un momento.",
            "La radio descansa entre canciones. Dejamos el cuaderno de melodías junto a la mesa: puedes tocar unas notas sin molestar a nadie.",
            "Limpiamos los espejos para que entre mejor la luz de la tarde. Dejamos sitio en el banco para la próxima visita."};
        public static bool IsLevel(LiminalEnvironment e){return (int)e>=FirstEnvironment&&(int)e<FirstEnvironment+Count;}
        public static bool IsMode(LiminalGeneration m){return m>=LiminalGeneration.SolarObservatory&&m<=LiminalGeneration.MirrorArcade;}
        public static bool IsTheme(SurfaceTheme t){return t>=SurfaceTheme.SolarObservatory&&t<=SurfaceTheme.MirrorArcade;}
        public static LiminalEnvironment ForMode(LiminalGeneration m)
        {if(!IsMode(m))throw new ArgumentOutOfRangeException(nameof(m));return (LiminalEnvironment)(FirstEnvironment+(int)m-(int)LiminalGeneration.SolarObservatory);}
        public static SurfaceTheme Theme(LiminalEnvironment e)
        {if(!IsLevel(e))throw new ArgumentOutOfRangeException(nameof(e));return (SurfaceTheme)((int)SurfaceTheme.SolarObservatory+(int)e-FirstEnvironment);}
        public static string Name(LiminalEnvironment e)
        {if(!IsLevel(e))throw new ArgumentOutOfRangeException(nameof(e));return Names[(int)e-FirstEnvironment];}
        public static bool WarmLight(LiminalEnvironment e)
        {return e==LiminalEnvironment.SolarObservatory||e==LiminalEnvironment.IndoorMarina||e==LiminalEnvironment.ClockGallery||e==LiminalEnvironment.MirrorArcade;}
        public static RoomVignette Memory(LiminalEnvironment e)
        {int i=(int)e-FirstEnvironment;if(i<0||i>=Count)throw new ArgumentOutOfRangeException(nameof(e));return new RoomVignette(Names[i],Notes[i],i==7?RoomActivity.Game:i==8?RoomActivity.Music:i==3?RoomActivity.Parcel:i==2||i==6?RoomActivity.Reading:RoomActivity.Rest,i);}
        public static StationKind Activity(LiminalEnvironment e,uint hash)
        {
            StationKind primary;
            switch(e){case LiminalEnvironment.SolarObservatory:case LiminalEnvironment.TextileAtelier:case LiminalEnvironment.PaperWorkshop:primary=StationKind.Sketch;break;
                case LiminalEnvironment.CoveredMarket:primary=StationKind.Parcels;break;case LiminalEnvironment.PostalArcade:primary=StationKind.Postcards;break;
                case LiminalEnvironment.IndoorMarina:primary=StationKind.Tea;break;case LiminalEnvironment.ClockGallery:primary=StationKind.Chimes;break;
                case LiminalEnvironment.ChessPavilion:primary=StationKind.SlidingPuzzle;break;case LiminalEnvironment.RadioSalon:primary=StationKind.MusicBox;break;
                default:primary=StationKind.Mosaic;break;}
            return hash%3==0?StationKind.Books:hash%3==1?primary:StationKind.Water;
        }
        public static string TextureSet(SurfaceTheme t,Surface s)
        {
            if(s==Surface.Glow)return null;if(s==Surface.Metal)return t==SurfaceTheme.ClockGallery||t==SurfaceTheme.SolarObservatory?"EncounterBrass":"Steel";
            if(s==Surface.Plaster)return t==SurfaceTheme.RadioSalon||t==SurfaceTheme.TextileAtelier?"EncounterLinen":"GardenLimewash";
            if(s==Surface.Accent)return t==SurfaceTheme.CoveredMarket?"GardenTerracotta":"EncounterOak";
            if(s==Surface.Slide)return t==SurfaceTheme.PaperWorkshop||t==SurfaceTheme.PostalArcade?"EncounterPaper":t==SurfaceTheme.IndoorMarina?"BathTravertineWet":t==SurfaceTheme.TextileAtelier||t==SurfaceTheme.RadioSalon?"EncounterLinen":"EncounterCeramic";
            return t==SurfaceTheme.IndoorMarina?"BathTravertineDry":t==SurfaceTheme.MirrorArcade||t==SurfaceTheme.ChessPavilion?"GardenLimestone":t==SurfaceTheme.CoveredMarket?"GardenTerracotta":"RainOak";
        }
        public static void Furnish(LiminalEnvironment e,List<QuietPropPlan> props)
        {
            for(int i=0;i<props.Count;i++)
            {
                var p=props[i];string source=p.Source;
                if(e==LiminalEnvironment.SolarObservatory&&source=="G_PottedPlant")source="U_AntiqueCamera";
                if(e==LiminalEnvironment.RadioSalon&&source=="R_FlowerVase")source="BoomBox";
                if(e==LiminalEnvironment.TextileAtelier&&source=="B_CafeChair")source="I_ChairDamaskPurplegold";
                if(e==LiminalEnvironment.MirrorArcade&&source=="B_CafeChair")source="SheenChair";
                if(source!=p.Source)props[i]=new QuietPropPlan(p.Bottom,p.CatalogId,source,p.Yaw);
            }
        }
    }
}
