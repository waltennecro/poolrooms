using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    // Stable values: existing serialized profiles and saved room IDs must not change meaning.
    public enum ZoneMode { Legacy, Catalog, Mixed, Single, Random }
    public enum ZoneFamily
    {
        Enfilades, Hypostyles, Meanders, Rotundas, Natatoriums, ThermalCells,
        Cloisters, Terraces, Bridges, LightWells, Vaults, Niches,
        ChangingRooms, Showers, Waterparks, ServiceGalleries, RampHalls,
        SplitGalleries, IslandBaths, Reservoirs,
        RecessedCourts, LanternHalls, RibbedChannels, ApsidalBaths, RaisedPromenades,
        FilterScreens, SteppedOculi, Aqueducts, QuietLounges, ConcentricPassages
    }
    public enum PoolPlan { Shallow, TwinLong, FourCourts, TwinCross, FourRound, SixBaths, Dry, FourSlits, CornerRounds, TwinOvals, OffsetBaths }

    public sealed class ZoneRecipe
    {
        public readonly int Index, Variant,BaseIndex,ExpansionStyle;
        public readonly ZoneFamily Family;
        public readonly string Name, Description;
        public readonly PoolPlan Pools;
        public readonly float Height;
        public string Code { get { return "PR-" + (Index + 1).ToString("000"); } }
        public float Floor { get { return Pools == PoolPlan.Shallow ? -.26f : 0; } }
        internal ZoneRecipe(int index, string name, string description, PoolPlan pools, float height)
        {
            Index=BaseIndex=index; Family=index<120?(ZoneFamily)(index/6):(ZoneFamily)(20+(index-120)/5); Variant=index<120?index%6:(index-120)%5;
            Name=name; Description=description; Pools=pools; Height=height;
        }
        internal ZoneRecipe(int index,ZoneRecipe original,int style)
        {Index=index;BaseIndex=original.Index;Variant=original.Variant;Family=original.Family;ExpansionStyle=style;Pools=original.Pools;Height=original.Height+.45f;Name=original.Name+(style==1?" · arcos segmentales":" · galería escalonada");Description=original.Description+" Nueva cubierta construida, conservando sus accesos y vasos.";}
    }

    public static class ZoneCatalog
    {
        public const int BaseCount=170,Count=255, FamilyCount=30, VariantsPerFamily=6;
        private static readonly ZoneRecipe[] recipes=CreateExpanded();
        private static ZoneRecipe[] CreateExpanded()
        {
            var result=Create();int next=BaseCount;
            for(int family=0;family<FamilyCount;family++)for(int v=0;v<(family<25?3:2);v++)
            {int start=family<20?family*6:120+(family-20)*5;result[next]=new ZoneRecipe(next,result[start+v],1+(v+family)%2);next++;}
            return result;
        }
        public static ZoneRecipe Get(int index)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            return recipes[index];
        }
        public static int FamilySize(int family) { return family<20?6:5; }
        public static string FamilyName(ZoneFamily family) { return familyNames[(int)family]; }
        private static readonly string[] familyNames={
            "Enfiladas y umbrales", "Salas hipóstilas", "Pasillos sinuosos", "Rotondas y anillos",
            "Naves de natación", "Celdas termales", "Claustros acuáticos", "Terrazas y gradas",
            "Puentes y canales", "Patios de luz", "Bóvedas", "Galerías de nichos",
            "Vestuarios vacíos", "Duchas colectivas", "Parques acuáticos", "Galerías de servicio",
            "Rampas y miradores", "Galerías divididas", "Baños con islas", "Cisternas y depósitos",
            "Patios escalonados", "Salas de linternas", "Canales entre costillas", "Baños absidales", "Paseos elevados",
            "Celosías cerámicas", "Óculos escalonados", "Acueductos interiores", "Salones de reposo", "Pasajes concéntricos"};

        private static ZoneRecipe[] Create()
        {
            string[][] names={
                new[]{"Umbrales en fuga","Arcada de seis pasos","Pórticos gemelos","Enfilada transversal","Umbrales alternos","Galería de dinteles"},
                new[]{"Bosque de columnas","Pilares octogonales","Columnas pareadas","Hipóstila de grandes apoyos","Retícula de pilares finos","Columnas con capiteles"},
                new[]{"Meandro doble","Canal de curvas suaves","Paseo ondulante","Pantallas sinuosas","Eses enfrentadas","Galerías entre ondas"},
                new[]{"Rotonda abierta","Doble anillo","Cuatro exedras","Anillo con columnas","Rotonda de ocho paños","Exedras concéntricas"},
                new[]{"Dos calles de natación","Piscinas y graderío","Cuatro vasos deportivos","Nave de carriles cortos","Galería de salida","Piscinas transversales"},
                new[]{"Cuatro baños íntimos","Baños bajo dinteles","Celdas de doble umbral","Seis pozas termales","Baños en exedra","Cámaras de descanso"},
                new[]{"Claustro de pilares","Claustro porticado","Patio de cuatro pozas","Pórtico doble","Claustro de arcos bajos","Claustro con bancos"},
                new[]{"Gradas junto al agua","Terrazas enfrentadas","Terrazas con escalinatas","Graderío envolvente","Bancos escalonados","Terrazas de observación"},
                new[]{"Puentes de dos ojos","Canales cruzados","Pasarelas paralelas","Puentes con antepechos","Puentes y apoyos dobles","Canales bajo pórticos"},
                new[]{"Patios de luz cuadrados","Lucernarios en secuencia","Pozos de luz profundos","Patios con pilares","Ranuras cenitales","Cuatro atrios blancos"},
                new[]{"Bóvedas paralelas","Cañones transversales","Bóveda de nervios","Pabellones abovedados","Bóvedas y columnas","Galería de cañones cortos"},
                new[]{"Nichos de descanso","Alcobas con arcos","Nichos enfrentados","Galería de bancos","Gabinetes junto al agua","Nichos de doble fondo"},
                new[]{"Bancos y taquillas","Cabinas enfrentadas","Vestuarios en peine","Taquillas en islas","Vestuarios con antesala","Bancos bajo vigas"},
                new[]{"Duchas en batería","Duchas con separadores","Cuatro cabinas húmedas","Duchas en peine","Bancos de enjuague","Galería de duchas"},
                new[]{"Toboganes gemelos","Toboganes divergentes","Tobogán y piscina de llegada","Dos canales de descenso","Toboganes con miradores","Descensos bajo vigas"},
                new[]{"Paseo de tuberías","Galería técnica doble","Bombas junto al canal","Conductos y pilares","Cámaras de mantenimiento","Galería de colectores"},
                new[]{"Rampas enfrentadas","Cuatro miradores","Paseo ascendente","Rampas bajo pórticos","Miradores sobre pozas","Rampas con antepechos"},
                new[]{"Tres galerías paralelas","Galerías transversales","Pasillos de umbrales","Pantallas dobles","Galerías con ventanas","Pasillos entre pilares"},
                new[]{"Islas cuadradas","Islas redondas","Islas con bancos","Cuatro pabellones","Islas con escalinatas","Islas porticadas"},
                new[]{"Cisterna de contrafuertes","Depósito de cuatro vasos","Cisterna de pilares","Nave de vigas profundas","Cisterna con galerías","Depósito de seis pozas"}
            };
            // Each row defines six explicit recipes. The family builder additionally changes the
            // number, placement and form of structural elements for each Variant (never colour only).
            PoolPlan S=PoolPlan.Shallow,T=PoolPlan.TwinLong,Q=PoolPlan.FourCourts,
                X=PoolPlan.TwinCross,R=PoolPlan.FourRound,H=PoolPlan.SixBaths,D=PoolPlan.Dry;
            PoolPlan[][] plans={
                new[]{S,S,T,X,Q,S},new[]{S,Q,T,R,H,S},new[]{S,T,S,Q,R,X},new[]{S,R,Q,S,R,Q},
                new[]{T,T,Q,H,T,X},new[]{Q,Q,R,H,R,Q},new[]{S,T,Q,R,S,Q},new[]{T,X,Q,R,H,T},
                new[]{T,X,T,Q,R,X},new[]{Q,T,R,S,X,Q},new[]{T,X,S,Q,R,H},new[]{S,T,Q,S,R,X},
                new[]{D,D,D,D,D,D},new[]{D,S,D,S,D,S},new[]{T,Q,T,X,R,T},new[]{S,T,Q,S,H,X},
                new[]{T,Q,S,R,Q,X},new[]{S,T,Q,S,X,R},new[]{Q,R,Q,R,Q,R},new[]{T,Q,R,X,T,H}
            };
            string[] descriptions={
                "Umbrales sucesivos; cambia el número, la forma o el eje de los pórticos.",
                "Retícula de apoyos; cambia densidad, sección, parejas o capiteles.",
                "Tabiques curvos; cambia frecuencia, separación, longitud y número de pantallas.",
                "Muros de arco circular con huecos cardinales; cambia radio, anillos y apoyos.",
                "Vasos deportivos con carriles, salidas y gradas; cambia distribución del agua y equipamiento.",
                "Recintos pequeños alrededor de pozas; cambia planta, entradas y bancos.",
                "Paseo perimetral entre soportes y agua; cambia ritmo y profundidad de los pórticos.",
                "Gradas construidas y escaleras laterales; cambia número, altura y orientación.",
                "Pasarelas sobre vasos excavados; cambia eje, apoyos y antepechos.",
                "Pozos cenitales delimitados por vigas; cambia secuencia de huecos y soportes.",
                "Cubiertas curvas de dos caras; cambia eje, longitud, nervios y soportes.",
                "Alcobas laterales con asiento; cambia número, profundidad y remate.",
                "Dependencias secas con taquillas, bancos y cabinas; cambia la distribución interior.",
                "Dependencias húmedas con platos, desagües, rociadores y mamparas; cambia su agrupación.",
                "Toboganes abiertos con soportes y escaleras hasta la plataforma; cambia trazado y disposición.",
                "Tuberías, colectores y plintos técnicos; cambia recorrido y agrupación de instalaciones.",
                "Rampas transitables hacia pequeñas plataformas; cambia número, altura y orientación.",
                "Tabiques con pasos reales; cambia eje, número y secuencia de aperturas.",
                "Plataformas dentro de los vasos conectadas por pasarelas; cambia forma y pabellones.",
                "Vasos entre contrafuertes, apoyos y vigas; cambia organización y escala de la estructura."
            };
            var result=new ZoneRecipe[Count];
            for(int family=0;family<20;family++)for(int v=0;v<6;v++)
            {
                int i=family*6+v;
                float height=family==14?9f:family==19?8.5f:6.2f+(v%3)*.6f;
                result[i]=new ZoneRecipe(i,names[family][v],descriptions[family],plans[family][v],height);
            }
            AddExpansion(result);
            return result;
        }
        private static void AddExpansion(ZoneRecipe[] result)
        {
            string[][] names={
                new[]{"Patios de peldaños anchos","Cuatro graderíos sumergidos","Patios con escaleras gemelas","Plataformas de descanso","Gradas entre piscinas ovales"},
                new[]{"Linternas sobre las pozas","Pabellones suspendidos","Cuadrícula de luminarias","Faroles de hormigón","Marcos de luz escalonados"},
                new[]{"Canales con costillas altas","Aletas de cerámica","Canales entre contrafuertes","Galerías de nervios dobles","Costillas bajo dinteles"},
                new[]{"Cuatro baños absidales","Exedras con asientos","Ábsides bajo doseles","Pozas entre medias lunas","Exedras con pilares finos"},
                new[]{"Paseos sobre el borde","Balcones con rampas","Miradores de doble acceso","Paseos entre plataformas","Balcones escalonados"},
                new[]{"Filtros de lamas anchas","Celosías de piezas cortas","Pantallas en peine doble","Salas de luz tamizada","Filtros entre bancos"},
                new[]{"Óculos de tres coronas","Pozos escalonados profundos","Óculos de anillos anchos","Cuatro coronas cenitales","Cúpulas de peldaños inversos"},
                new[]{"Acueductos de arcos anchos","Doble galería sobre canales","Arquerías con estribos","Acueductos de ojos bajos","Puentes bajo la arcada"},
                new[]{"Sala de bancos insulares","Reposo entre piscinas ovales","Islas con tumbonas cerámicas","Salón de bancos enfrentados","Pabellones de descanso vacío"},
                new[]{"Pasajes de dos anillos","Anillos con portales cardinales","Tres segmentos concéntricos","Corredores circulares abiertos","Anillos de muros y columnas"}
            };
            string[] descriptions={
                "Patios con plataformas laterales y vuelos de peldaños; cambia altura, profundidad, acceso y número de gradas.",
                "Marcos y pantallas suspendidos sobre los vasos; cambia la retícula, los niveles y la forma de los doseles.",
                "Aletas estructurales repetidas junto a canales; cambia ritmo, sección, remates y apoyos dobles.",
                "Muros semicirculares alrededor de las pozas; cambia apertura, radio, bancos y coronación.",
                "Pequeños paseos elevados con rampas y barandillas; cambia la cota, el acceso y los descansillos.",
                "Pantallas de lamas cerámicas; cambia orientación, densidad, altura y bancos entre filtros.",
                "Óculos construidos como anillos escalonados bajo el techo; cambia radio, número de coronas y descenso.",
                "Arquerías cruzadas por pasos altos; cambia aperturas, estribos y puentes bajos sobre piscinas.",
                "Salones sin mobiliario orgánico, con bancos y plataformas cerámicas; cambia distribución, cabeceros y doseles.",
                "Tramos curvos concéntricos con pasos cardinales libres; cambia número, espesor, altura y remates de los anillos."
            };
            PoolPlan[][] plans={
                new[]{PoolPlan.FourCourts,PoolPlan.FourSlits,PoolPlan.OffsetBaths,PoolPlan.CornerRounds,PoolPlan.TwinOvals},
                new[]{PoolPlan.FourRound,PoolPlan.TwinLong,PoolPlan.FourCourts,PoolPlan.SixBaths,PoolPlan.OffsetBaths},
                new[]{PoolPlan.FourSlits,PoolPlan.TwinLong,PoolPlan.TwinCross,PoolPlan.FourSlits,PoolPlan.Shallow},
                new[]{PoolPlan.CornerRounds,PoolPlan.FourRound,PoolPlan.OffsetBaths,PoolPlan.FourCourts,PoolPlan.CornerRounds},
                new[]{PoolPlan.FourSlits,PoolPlan.TwinOvals,PoolPlan.OffsetBaths,PoolPlan.FourCourts,PoolPlan.TwinLong},
                new[]{PoolPlan.Shallow,PoolPlan.Dry,PoolPlan.FourSlits,PoolPlan.TwinCross,PoolPlan.OffsetBaths},
                new[]{PoolPlan.CornerRounds,PoolPlan.FourRound,PoolPlan.FourCourts,PoolPlan.OffsetBaths,PoolPlan.FourRound},
                new[]{PoolPlan.TwinLong,PoolPlan.TwinCross,PoolPlan.FourSlits,PoolPlan.FourCourts,PoolPlan.TwinOvals},
                new[]{PoolPlan.FourCourts,PoolPlan.TwinOvals,PoolPlan.FourRound,PoolPlan.OffsetBaths,PoolPlan.SixBaths},
                new[]{PoolPlan.Shallow,PoolPlan.Shallow,PoolPlan.CornerRounds,PoolPlan.Dry,PoolPlan.FourRound}
            };
            for(int family=0;family<10;family++)for(int v=0;v<5;v++)
            {
                int index=120+family*5+v;
                float height=(family==1||family==6||family==7?8.2f:6.8f)+v*.22f;
                result[index]=new ZoneRecipe(index,names[family][v],descriptions[family],plans[family][v],height);
            }
        }

        // Stateless coordinate selection: revisit/order/map dimensions do not change a recipe.
        // Repetition is possible naturally; there is no catalogue-completion cycle.
        public static int RandomAt(LayoutConfig c,int x,int z)
        {
            uint hash=StableRandom.Hash(c.Seed,x,z,0xA1E47u);
            if(c.ZoneFamilyFilter<0)return (int)(hash%(uint)(c.ExpandedVariety?Count:BaseCount));
            int family=c.ZoneFamilyFilter,start=family<20?family*6:120+(family-20)*5;
            int original=FamilySize(family),extra=c.ExpandedVariety?(family<25?3:2):0,pick=(int)(hash%(uint)(original+extra));
            return pick<original?start+pick:BaseCount+(family<25?family*3:75+(family-25)*2)+pick-original;
        }

        // Shuffled bags guarantee every enabled recipe occurs before any recipe repeats.
        // Selection uses its own random stream; corridor topology and original details stay stable.
        public static int[] Assign(LayoutConfig c,int roomCount)
        {
            var result=new int[roomCount];for(int i=0;i<result.Length;i++)result[i]=-1;
            if(c.Zones==ZoneMode.Legacy)return result;
            if(c.Zones==ZoneMode.Random)
            {for(int i=0;i<roomCount;i++)result[i]=RandomAt(c,i%c.Columns,i/c.Columns);return result;}
            if(c.Zones==ZoneMode.Single){for(int i=0;i<roomCount;i++)result[i]=c.SingleZone;return result;}
            var bag=new List<int>();
            for(int i=0;i<(c.ExpandedVariety?Count:BaseCount);i++)if(c.ZoneFamilyFilter<0||(int)Get(i).Family==c.ZoneFamilyFilter)bag.Add(i);
            var random=new StableRandom(StableRandom.Hash(c.Seed,120,20,0xCA7A10u));
            int cursor=bag.Count;
            for(int room=0;room<roomCount;room++)
            {
                if(c.Zones==ZoneMode.Mixed&&room%8==0)continue;
                if(cursor==bag.Count)
                {
                    for(int j=bag.Count-1;j>0;j--){int k=random.Range(j+1),tmp=bag[j];bag[j]=bag[k];bag[k]=tmp;}
                    cursor=0;
                }
                result[room]=bag[cursor++];
            }
            return result;
        }
    }
}
