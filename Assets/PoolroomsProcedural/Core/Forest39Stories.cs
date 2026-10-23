namespace Poolrooms.Core
{
    // Original environmental stories, not translated or copied Wiki prose.
    public static class Forest39Stories
    {
        static readonly string[] titles =
        {
            "El jardín de musgo", "El estanque de la tarde", "Los libros del roble",
            "El herbario abierto", "La rama que descansa", "El círculo de piedras",
            "La mesa compartida", "La hondonada de helechos", "El cuaderno del claro",
            "El bosque que escucha", "El refugio de madera", "El descanso del camino"
        };
        static readonly string[] notes =
        {
            "Unas marcas pequeñas rodean el musgo nuevo. La última nota pide caminar por la tierra firme y dejar este rincón verde para quienes lleguen mañana.",
            "Dos piedras lisas señalan dónde sentarse. En el agua se distingue el fondo. Una tarjeta dice: aquí esperamos a que termine la tarde; nunca nos dimos prisa.",
            "El libro tiene una hoja de roble entre sus páginas. En la última página alguien escribió que volvería a buscarla después del paseo. Hay sitio para dejar otra historia.",
            "Las fichas describen las nervaduras de las hojas. No arranques ramas: basta con recoger una hoja caída. Hay una prensa preparada sobre la mesa.",
            "Alguien apartó una rama caída del sendero y dejó una cesta junto al banco. Una cinta recuerda que los robles permanecen aquí, incluso cuando nadie los mira.",
            "Las piedras están colocadas de menor a mayor. La siguiente persona podrá continuar el círculo. Un dibujo invita a dejar una abertura hacia el camino de vuelta.",
            "Quedan dos tazas, una cesta y una invitación: toma lo que necesites y guarda algo para la siguiente visita. Nadie ha recogido la mesa todavía.",
            "Las huellas se detienen antes de los helechos. Una libreta registra cómo cambia el agua alrededor de las raíces. Hoy alguien añadió una nueva medida.",
            "El cuaderno está abierto en un dibujo a medio terminar. La copa del roble continúa fuera de la página. Puedes sentarte y buscar tu propio encuadre.",
            "Una placa propone escuchar el viento durante un minuto. Después, comparar el roce de las hojas con el sonido más grave de las ramas. Aquí no hace falta hablar.",
            "El tejado conserva pequeñas marcas de lluvia. Bajo él hay espacio para secar el cuaderno. En una esquina alguien dejó anotada la siguiente parada del paseo.",
            "El banco mira hacia dos senderos. Una nota dice: si llegas primero, descansa; sabremos encontrarnos. La taza sigue templada bajo esta luz que no termina."
        };

        public static string Title(Forest39Clearing clearing) { return titles[(int)clearing]; }
        public static string Note(Forest39Clearing clearing) { return notes[(int)clearing]; }
    }
}
