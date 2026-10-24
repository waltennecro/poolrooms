using System;

namespace Poolrooms.Core
{
    public enum Level10Action { GrainMill,HandPump,WindVane,SeedCabinet,FieldNotes,Rest,WheatBundle,SeedPacket }
    public readonly struct Level10ActionSite
    {
        public readonly Level10Action Kind;public readonly V3 Bottom;public readonly int Slot;
        public Level10ActionSite(Level10Action kind,V3 bottom,int slot){Kind=kind;Bottom=bottom;Slot=slot;}
    }
    public sealed class Level10ActionState
    {
        public readonly Level10Action Kind;public readonly int Target;
        public int Position {get;private set;}
        public int Actions {get;private set;}
        public bool Complete {get;private set;}
        public Level10ActionState(Level10Action kind,uint seed)
        {Kind=kind;Target=kind==Level10Action.WindVane?(int)(seed%4):kind==Level10Action.GrainMill?3:kind==Level10Action.HandPump?4:2;Position=kind==Level10Action.WindVane?(Target+1)%4:0;}
        public bool Advance()
        {
            if(Complete)return false;Actions++;
            Position=Kind==Level10Action.WindVane?(Position+1)%4:Math.Min(Target,Position+1);
            Complete=Position==Target;return true;
        }
        public void Restore(int position,bool completed)
        {
            int maximum=Kind==Level10Action.WindVane?3:Target;
            if(position<0||position>maximum)throw new ArgumentOutOfRangeException(nameof(position));
            Position=position;Complete=completed&&Position==Target;
        }
        public string Feedback()
        {
            if(Complete)return Kind==Level10Action.HandPump?"El agua ya corre hacia el pequeño abrevadero. Puedes guardar una ficha del trabajo.":Kind==Level10Action.GrainMill?"El molino ha completado su vuelta. La muestra queda preparada.":Kind==Level10Action.SeedCabinet?"Las semillas quedan ordenadas y el mueble abierto para quien llegue después.":"La veleta coincide con la dirección marcada en el cuaderno.";
            return Kind==Level10Action.WindVane?"Dirección "+(Position+1)+"; la marca del cuaderno indica "+(Target+1)+".":"Paso "+Position+" de "+Target+". No hay prisa ni salidas bloqueadas.";
        }
    }
    public static class Level10Activities
    {
        public const int KindCount=8,ItemCount=3;
        public static string Title(Level10Place place)
        {
            switch(place)
            {
                case Level10Place.BarnLoft:return "El granero de la galería";
                case Level10Place.Stable:return "Los establos vacíos";
                case Level10Place.Granary:return "El almacén de semillas";
                case Level10Place.PumpCourt:return "La casa de la bomba";
                case Level10Place.Lakeside:return "La laguna del camino";
                case Level10Place.ToolShed:return "El cobertizo del carpintero";
                case Level10Place.HarvestCourt:return "El patio de la cosecha";
                case Level10Place.HayShelter:return "La nave del heno";
                case Level10Place.SeedHouse:return "El semillero acristalado";
                case Level10Place.OrchardRest:return "La pérgola del descanso";
                case Level10Place.LoadingPorch:return "El porche de carga";
                default:return "El trigo que continúa";
            }
        }
        public static string Story(Level10Place place)
        {
            string[] notes={
                "Dos rodadas siguen entre las espigas. Alguien ha dejado libre la franja de hierba del centro, como si todavía esperara la próxima visita.",
                "La puerta del granero quedó abierta. En la galería hay sitio para mirar los campos; la taza de abajo parece haberse preparado para una conversación.",
                "No hay animales. Las divisiones están limpias y los pequeños pesebres preparados. Una nota pide no dejar herramientas en el pasillo central.",
                "Las semillas se guardan por dibujos, no por fechas. En la pared alguien escribió: deja una muestra para quien venga detrás.",
                "La bomba mueve agua hacia el abrevadero. No hace falta beber ni llenar un medidor: es un trabajo pequeño que deja este rincón cuidado.",
                "El fondo se ve entre las orillas. El tablón del agua tiene marcas de mediciones antiguas y un lugar seco para sentarse.",
                "Las baldas conservan el orden de un taller usado con calma. Hay sitio para devolver una herramienta y dejar el banco preparado.",
                "La cosecha quedó bajo techo y junto al camino. Entre las cajas hay un hueco que parece reservado para la siguiente entrega.",
                "El heno ocupa el lateral de la nave; la escalera conduce a una galería abierta. Nada está apilado contra las salidas.",
                "El vidrio del tejado deja entrar el día gris. Los recipientes del semillero tienen pequeñas marcas hechas a mano.",
                "La pérgola mira hacia las hileras de trigo. Una cesta vacía invita a dejar aquí un recuerdo del paseo.",
                "El porche tiene una mesa y un sitio libre. La siguiente entrega podría tardar, pero el camino sigue abierto."};
            return notes[(int)place]+"\n\nNivel 10 — adaptación tranquila de Bounty. El camino a las afueras de la ciudad permite volver andando al Nivel 11. No hay criaturas ni necesidades de supervivencia.";
        }
        public static Level10ActionSite[] Plan(Level10Plan p)
        {
            var primary=(Level10Action)((int)p.Place%4);
            var a=new V3(p.Rest.X+1.85f,0,p.Rest.Z+.1f);
            var b=new V3(p.Notes.X,0,p.Notes.Z);
            a.Y=Level10Terrain.MeshHeight(p,a.X,a.Z);b.Y=Level10Terrain.MeshHeight(p,b.X,b.Z);
            return new[]{new Level10ActionSite(primary,a,0),new Level10ActionSite(Level10Action.FieldNotes,b,1),
                new Level10ActionSite(Level10Action.Rest,new V3(p.Rest.X,0,p.Rest.Z-1.1f),2),
                new Level10ActionSite(Level10Action.WheatBundle,new V3(-2.1f,0,1.0f),3),
                new Level10ActionSite(Level10Action.SeedPacket,new V3(1.0f,0,-2.1f),4)};
        }
        public static string Name(Level10Action kind)
        {
            switch(kind)
            {
                case Level10Action.GrainMill:return "Molino manual de grano";
                case Level10Action.HandPump:return "Bomba del abrevadero";
                case Level10Action.WindVane:return "La veleta del camino";
                case Level10Action.SeedCabinet:return "El armario de las semillas";
                case Level10Action.FieldNotes:return "Cuaderno de los campos";
                case Level10Action.Rest:return "Banco de la próxima visita";
                case Level10Action.WheatBundle:return "Pequeño haz de trigo";
                default:return "Sobre de semillas";
            }
        }
        public static V3 Size(Level10Action kind)
        {
            switch(kind)
            {
                case Level10Action.WindVane:return new V3(.8f,1.8f,.8f);
                case Level10Action.FieldNotes:return new V3(.74f,.92f,.58f);
                case Level10Action.Rest:return new V3(3.2f,1.0f,.7f);
                case Level10Action.WheatBundle:return new V3(.32f,.38f,.32f);
                case Level10Action.SeedPacket:return new V3(.20f,.04f,.26f);
                default:return new V3(.90f,1.15f,.8f);
            }
        }
        public static ExplorationItem Item(int id)
        {
            switch(id)
            {
                case 0:return new ExplorationItem("level10:0","Haz de trigo","Unas espigas del Nivel 10. Son un recuerdo del paisaje, sin hambre ni tareas obligatorias.",ItemUse.Read);
                case 1:return new ExplorationItem("level10:1","Sobre de semillas","Un pequeño paquete con marcas de siembra. Puedes guardarlo y volver a colocarlo.",ItemUse.Read);
                case 2:return new ExplorationItem("level10:2","Ficha de los campos","Un registro del molino, el agua y el viento. El rincón queda listo para quien llegue después.",ItemUse.Read);
                default:return null;
            }
        }
        public static string Key(int seed,int x,int z,int slot){return "level10:"+seed+":"+x+":"+z+":"+slot;}
        public static DressingAnchor Panel(RoomGeometry g,float x,float z)
        {
            var m=new MeshData{Theme=SurfaceTheme.Level10Timber};
            Level10Architecture.Box(m,x,z,.10f,.10f,0,.8f);Level10Architecture.Box(m,x,z,1.4f,.10f,.75f,1.9f,Surface.Plaster);
            g.VisualMeshes.Add(m);g.CollisionOnly.Append(m);
            V3 a=new V3(x-.68f,.78f,z+.051f),b=new V3(x+.68f,.78f,z+.051f),c=new V3(x-.68f,1.87f,z+.051f);
            return new DressingAnchor(new V3(x-.36f,1.15f,z+.051f),new V3(0,0,1),a,b,c,0,0);
        }
    }
}
