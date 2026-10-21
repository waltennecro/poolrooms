using System;

namespace Poolrooms.Core
{
    // IDs follow the original twelve stations. Never reorder serialized values.
    public static class WorkshopCatalog
    {
        public const int First = 12;
        public const int Count = 12;
        public const int StationCount = First + Count;

        private static readonly string[] names =
        {
            "Circuito de faroles", "Canales del jardín", "Telar de viaje",
            "Herbario compartido", "Reloj de la tarde", "Balanza de paquetes",
            "Carta de estrellas", "Melodía de campanas", "Compuertas de marea",
            "Encuadernación de colores", "Rosa de los vientos", "Vivero de semillas"
        };

        private static readonly string[] instructions =
        {
            "Enciende los nueve faroles. Cada interruptor cambia su farol y los vecinos que comparten un lado.",
            "Gira las tuberías para conectar la entrada superior izquierda con la salida inferior izquierda. El agua debe recorrer las dieciséis piezas.",
            "Elige el color de cada hilo siguiendo la muestra. No hay límite de tiempo ni materiales que gastar.",
            "Ordena las seis etiquetas del herbario. Cada pulsación intercambia una etiqueta con la siguiente.",
            "Ajusta los tres discos del reloj a las horas anotadas por la última visita. Cada disco gira de forma independiente.",
            "Coloca pesas en la balanza hasta igualar el peso del paquete. Puedes retirar cualquier pesa que ya esté colocada.",
            "Une las estrellas siguiendo sus números. Las estrellas ya unidas permanecen marcadas; una elección incorrecta no borra el dibujo.",
            "Repite la melodía de la tarjeta. La referencia siempre está disponible y también puedes escucharla antes de tocar.",
            "Traslada una medida de agua entre depósitos hasta alcanzar las marcas. El agua se conserva y ningún depósito puede desbordarse.",
            "Gira cada grupo de tres páginas hasta reconstruir el orden de colores de la muestra.",
            "Orienta las cuatro agujas según la tarjeta. Cada rueda gira también la siguiente: forman un mecanismo compartido.",
            "Lleva la humedad de cada semillero a su marca. Riega o vacía una medida; aquí las plantas no mueren ni hay penalizaciones."
        };

        private static readonly string[] stories =
        {
            "Dejamos los faroles conectados para recibir a quienes lleguen al anochecer. Falta revisar algunos interruptores.",
            "El jardín espera su riego de la tarde. Alguien limpió los canales y dejó las piezas preparadas sobre el banco.",
            "Este tejido se hace entre muchas visitas. En la cesta hay una muestra para que puedas continuar donde lo dejaron.",
            "Las hojas se recogieron cerca de los patios. Queda espacio en el álbum para la próxima excursión compartida.",
            "La mesa está reservada para cuando las tres agujas coincidan con la nota. Puedes poner el reloj en hora.",
            "Son paquetes para el siguiente encuentro. La balanza permite preparar las entregas sin abrir las cajas.",
            "En el observatorio dejaron una carta del cielo de esta noche. Al terminarla sabremos dónde colocar el telescopio.",
            "Alguien apuntó esta pequeña melodía para tocarla al regresar. Las campanas no tienen que sonar deprisa.",
            "Los depósitos mantienen el agua del patio a la misma altura. Las marcas las dibujó quien cuidó este rincón ayer.",
            "Hay un cuaderno a medio encuadernar. Sus páginas de colores cuentan los lugares por los que pasaron los viajeros.",
            "El taller prepara una señal para el próximo paseo. La tarjeta indica hacia dónde deben mirar sus agujas.",
            "Dejaron semillas plantadas para la próxima temporada. La nota dice: gracias por cuidarlas mientras volvemos."
        };

        private static readonly StationSound[] sounds =
        {
            StationSound.Switch, StationSound.WaterDrop, StationSound.Fabric,
            StationSound.Page, StationSound.Latch, StationSound.Parcel,
            StationSound.Tile, StationSound.Chime, StationSound.Pour,
            StationSound.Paper, StationSound.Latch, StationSound.Leaves
        };

        private static readonly int[][] preferences =
        {
            new[] { 6, 9, 7, 10 },
            new[] { 1, 8, 4, 6 },
            new[] { 3, 11, 2, 7 },
            new[] { 5, 0, 9, 10 }
        };

        public static bool IsWorkshop(StationKind kind)
        {
            return (int)kind >= First && (int)kind < StationCount;
        }

        public static int Index(StationKind kind)
        {
            if (!IsWorkshop(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            return (int)kind - First;
        }

        public static string Name(StationKind kind) { return names[Index(kind)]; }
        public static string Instructions(StationKind kind) { return instructions[Index(kind)]; }
        public static string Story(StationKind kind) { return stories[Index(kind)]; }
        public static StationSound Sound(StationKind kind) { return sounds[Index(kind)]; }

        public static StationKind Choose(LiminalEnvironment environment, uint hash)
        {
            int family;
            switch (environment)
            {
                case LiminalEnvironment.Poolrooms:
                case LiminalEnvironment.AquaticPark:
                case LiminalEnvironment.ThermalBaths:
                case LiminalEnvironment.CloudBaths:
                case LiminalEnvironment.MosaicCistern:
                case LiminalEnvironment.WatermillHalls:
                case LiminalEnvironment.SunkenLibrary:
                    family = 1;
                    break;
                case LiminalEnvironment.Orangerie:
                case LiminalEnvironment.Conservatory:
                case LiminalEnvironment.WinterGarden:
                case LiminalEnvironment.BambooRetreat:
                case LiminalEnvironment.HangingOrchards:
                case LiminalEnvironment.LunarGreenhouse:
                    family = 2;
                    break;
                case LiminalEnvironment.Supermarket:
                case LiminalEnvironment.City11:
                case LiminalEnvironment.AmberRestaurant:
                case LiminalEnvironment.IndoorBoardwalk:
                    family = 3;
                    break;
                default:
                    family = 0;
                    break;
            }
            // A quarter of selections use the whole bank, so no biome locks
            // a game out permanently. The remaining choices follow its use.
            if ((hash & 3u) == 0) return (StationKind)(First + (hash >> 2) % Count);
            int choice = (int)((hash >> 4) % 4);
            return (StationKind)(First + preferences[family][choice]);
        }

        public static WorkshopBoard Create(StationKind kind, uint seed)
        {
            switch (kind)
            {
                case StationKind.LanternCircuit: return new LanternBoard(seed);
                case StationKind.WaterChannels: return new ChannelBoard(seed);
                case StationKind.Weaving: return new WeavingBoard(seed);
                case StationKind.Herbarium: return new HerbariumBoard(seed);
                case StationKind.Sundial: return new SundialBoard(seed);
                case StationKind.ParcelBalance: return new BalanceBoard(seed);
                case StationKind.StarChart: return new StarBoard(seed);
                case StationKind.BellSequence: return new BellBoard(seed);
                case StationKind.TideLocks: return new TideBoard(seed);
                case StationKind.Bookbinding: return new BindingBoard(seed);
                case StationKind.WindRose: return new WindBoard(seed);
                case StationKind.SeedNursery: return new NurseryBoard(seed);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
