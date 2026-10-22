using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Appended IDs: existing worlds and imported inventory retain their meanings.
    // Recipes describe structure; they are not counted as external source models.
    public static class JourneyLevelPlanner
    {
        public const int FirstEnvironment=38,Count=20;
        static readonly string[] Names={"Galerías del acuario","Refugio de bambú","Caravasar del desierto","Museo del cristal","Baños entre nubes","Biblioteca sumergida","Estación de invierno","Pueblo de cerámica","Invernadero lunar","Restaurante de ámbar","Archivo de cartógrafos","Patios de escultura","Villa de faroles","Salones del molino","Paseo marítimo interior","Teatro de terciopelo","Huertos suspendidos","Librerías de lluvia","Cisterna de mosaicos","Galería aeronáutica"};
        static readonly string[] Notes={
            "La galería se llena de luz al caer la tarde. Dejamos un cuaderno junto al vidrio para dibujar lo que imaginas al otro lado.",
            "Las esterillas están preparadas. Volveremos a compartir el té después de regar el bambú; puedes servirte mientras tanto.",
            "La caravana descansará aquí esta noche. Hay agua fresca y un lugar a la sombra para cada viajero que llegue.",
            "Hoy terminamos de ordenar la colección. Dejamos algunas piezas fuera de las vitrinas para observarlas con calma.",
            "El vapor se ha disipado y el agua está tranquila. Hemos dejado las toallas junto al banco para quienes lleguen tarde.",
            "Los libros permanecen a salvo sobre los canales. El grupo volverá por el puente después de leer; hay otra taza preparada.",
            "El último tren dejó aquí unas macetas. Mañana vendrán a recogerlas; mientras tanto, cuidamos de este pequeño jardín.",
            "Las casas se abren a los patios. Estamos preparando piezas para la feria del domingo; elige un color para la próxima tanda.",
            "Las plantas crecen bajo las cúpulas. Dejamos el registro de riego sobre la mesa; habrá una nueva cosecha al regresar.",
            "La mesa está preparada para la cena. Las lámparas se encienden al atardecer y siempre guardamos un asiento más.",
            "Los mapas no señalan un destino obligatorio. Añade un dibujo de tu camino y lo compartiremos con los próximos viajeros.",
            "Esta exposición cambia con la luz. Los bancos están colocados para mirar las esculturas desde otro ángulo.",
            "Hoy colgamos los faroles en las calles interiores. Volveremos con la merienda; dejamos una luz para encontrarnos.",
            "El molino descansa junto al canal. Nos reuniremos después de limpiar las herramientas; hay agua y pan sobre la mesa.",
            "Las persianas están abiertas y el paseo está tranquilo. Hemos preparado postales para quienes extrañan el mar.",
            "El ensayo ha terminado. Puedes sentarte en las primeras filas o tocar la caja de música mientras volvemos.",
            "Las ramas quedan suspendidas sobre los bancos. La cesta de la cosecha está lista para compartirla cuando llegue el grupo.",
            "Mientras llueve sobre el tragaluz, los libros esperan a sus lectores. Dejamos una taza junto a la ventana.",
            "Las teselas se colocaron una a una. Guarda un momento para observar sus reflejos; regresaremos por la galería seca.",
            "El taller abre de nuevo mañana. Dejamos los planos desplegados y un banco libre para quien quiera imaginar otro viaje."};
        static readonly StationKind[] Activities={StationKind.Sketch,StationKind.Tea,StationKind.Water,StationKind.Mosaic,StationKind.RestBell,StationKind.Books,StationKind.Postcards,StationKind.Mosaic,StationKind.Sketch,StationKind.Tea,StationKind.Postcards,StationKind.Sketch,StationKind.Chimes,StationKind.Water,StationKind.Postcards,StationKind.MusicBox,StationKind.Water,StationKind.Books,StationKind.Mosaic,StationKind.SlidingPuzzle};
        public static bool IsLevel(LiminalEnvironment e){return (int)e>=FirstEnvironment&&(int)e<FirstEnvironment+Count;}
        public static bool IsMode(LiminalGeneration m){return m>=LiminalGeneration.AquariumGalleries&&m<=LiminalGeneration.AeronauticalGallery;}
        public static bool IsTheme(SurfaceTheme t){return t>=SurfaceTheme.AquariumGalleries&&t<=SurfaceTheme.AeronauticalGallery;}
        static int Index(LiminalEnvironment e){if(!IsLevel(e))throw new ArgumentOutOfRangeException(nameof(e));return (int)e-FirstEnvironment;}
        public static LiminalEnvironment ForMode(LiminalGeneration m)
        {if(!IsMode(m))throw new ArgumentOutOfRangeException(nameof(m));return (LiminalEnvironment)(FirstEnvironment+(int)m-(int)LiminalGeneration.AquariumGalleries);}
        public static SurfaceTheme Theme(LiminalEnvironment e){return (SurfaceTheme)((int)SurfaceTheme.AquariumGalleries+Index(e));}
        public static string Name(LiminalEnvironment e){return Names[Index(e)];}
        public static RoomVignette Memory(LiminalEnvironment e)
        {int i=Index(e);return new RoomVignette(Names[i],Notes[i],i==15?RoomActivity.Music:i==10||i==14?RoomActivity.Parcel:i==5||i==17?RoomActivity.Reading:RoomActivity.Rest,i);}
        public static StationKind Activity(LiminalEnvironment e,uint hash)
        {return hash%4==0?StationKind.Light:hash%4==1?StationKind.SlidingPuzzle:Activities[Index(e)];}
        public static bool WarmLight(LiminalEnvironment e)
        {return e==LiminalEnvironment.DesertCaravanserai||e==LiminalEnvironment.AmberRestaurant||e==LiminalEnvironment.PaperLanternTown||e==LiminalEnvironment.IndoorBoardwalk||e==LiminalEnvironment.VelvetTheatre;}
        public static bool HasPools(LiminalEnvironment e)
        {return e==LiminalEnvironment.CloudBaths||e==LiminalEnvironment.SunkenLibrary||e==LiminalEnvironment.WatermillHalls||e==LiminalEnvironment.IndoorBoardwalk||e==LiminalEnvironment.MosaicCistern;}
        public static bool Absorbent(LiminalEnvironment e)
        {return e==LiminalEnvironment.BambooRetreat||e==LiminalEnvironment.VelvetTheatre||e==LiminalEnvironment.RainyBookshops||e==LiminalEnvironment.MapmakersArchive;}
        // Fallbacks also work if users deliberately remove the optional finish bank.
        public static string TextureSet(SurfaceTheme t,Surface s)
        {if(s==Surface.Glow)return null;if(s==Surface.Metal)return "EncounterBrass";if(s==Surface.Plaster)return "GardenLimewash";if(s==Surface.Accent)return "EncounterOak";return s==Surface.Slide?"EncounterLinen":"GardenLimestone";}
        public static void Furnish(LiminalEnvironment e,List<QuietPropPlan> props)
        {
            int kind=Index(e);
            for(int i=0;i<props.Count;i++)
            {
                var p=props[i];string source=p.Source;
                if(source=="G_PottedPlant")source=kind==6?"J_Radiator":kind==17||kind==15?"J_CoatRackA":kind==7||kind==8||kind==16?"J_PlantPot":"G_PottedPlant";
                if(source=="B_CafeChair")source=Absorbent(e)?"I_ChairDamaskPurplegold":kind%3==0?"M_IndoorChair":"B_CafeChair";
                if(source=="R_FlowerVase"&&kind==19)source="BoomBox";
                if(source=="R_BarnLamp"&&WarmLight(e))source="M_StainedLamp";
                if(source!=p.Source)props[i]=new QuietPropPlan(p.Bottom,p.CatalogId,source,p.Yaw);
            }
        }
        // All indices refer to attributed authored surfaces, not duplicate colour copies.
        static readonly int[] Floors={0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19};
        static readonly int[] Walls={20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39};
        public static int Finish(SurfaceTheme theme,Surface slot)
        {
            if(HorizonLevelPlanner.IsTheme(theme))return HorizonLevelPlanner.Finish(theme,slot);
            if(!IsTheme(theme)||slot==Surface.Glow)return -1;
            int i=(int)theme-(int)SurfaceTheme.AquariumGalleries;
            return slot==Surface.Tile?Floors[i]:slot==Surface.Plaster?Walls[i]:slot==Surface.Accent?40:slot==Surface.Metal?41:42;
        }
    }
}
