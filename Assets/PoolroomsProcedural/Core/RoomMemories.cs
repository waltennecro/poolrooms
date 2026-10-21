using System;
using System.Collections.Generic;

namespace Poolrooms.Core
{
    public enum RoomActivity { Notice,Tea,Reading,Plant,Music,Game,Parcel,Window,Lamp,Drawer,Rest }
    public readonly struct RoomVignette
    {
        public readonly string Title,Note;
        public readonly RoomActivity Activity;
        public readonly int Edition;
        public RoomVignette(string title,string note,RoomActivity activity,int edition){Title=title;Note=note;Activity=activity;Edition=edition;}
    }
    public static class RoomMemories
    {
        private static readonly string[] Beginnings={"La mesa junto a la ventana","Un descanso compartido","Antes de la próxima visita","El rincón de la tarde","Un lugar reservado","La última taza","Los preparativos","La mesa de los domingos","Una pausa en el camino","La reunión de mañana","Cosas por terminar","El pequeño taller"};
        private static readonly string[] Notes={
            "Dejamos dos tazas, por si alguien llega antes. El agua está junto al cuaderno. Vuelve a poner las sillas cuando te marches.",
            "Nos turnamos para cuidar las plantas. Hoy le toca a la maceta pequeña. Gracias por dejarla preparada para la próxima visita.",
            "El libro quedó abierto en una página bonita. Puedes seguir leyendo; no hace falta devolverlo hoy.",
            "Estamos preparando la mesa para mañana. Elegimos este rincón por la luz de la tarde. Si llegas primero, puedes quedarte un rato.",
            "La música sonaba bajito mientras ordenábamos. Quedó una partida a medias y unas galletas para compartir.",
            "Pasamos por aquí después de nadar. Acercamos las sillas para compartir el descanso; siéntate cuando quieras.",
            "La jornada está en pausa. Dejamos este rincón ordenado y una nota para quien llegue después. Hay algo útil en el mueble.",
            "Alguien trajo flores del patio. Dejamos el jarrón donde se ve desde la entrada. Puedes moverlo a un sitio más bonito.",
            "La caja contiene pequeñas cosas que nos han dejado los viajeros. Toma algo útil y deja el rincón recogido.",
            "Nos gusta volver por las galerías del jardín. Dejamos la mesa preparada para descansar antes de continuar el camino.",
            "Cada visita cambia un poco este lugar. Hoy basta con sentarse, mirar la luz y descansar un momento.",
            "Hay una invitación para la próxima merienda: trae una historia, una canción o simplemente ganas de estar aquí."};
        public static RoomVignette At(int seed,int x,int z,LiminalEnvironment environment)
        {if(JourneyLevelPlanner.IsLevel(environment))return JourneyLevelPlanner.Memory(environment);if(SpecialRegionPlanner.IsLevel(environment))return SpecialRegionPlanner.Memory(environment);uint h=LiminalRegions.Hash(seed,x,z,0xCAFE02u);int edition=(int)(h%144);RoomActivity[] activities={RoomActivity.Tea,RoomActivity.Plant,RoomActivity.Reading,RoomActivity.Window,RoomActivity.Game,RoomActivity.Rest,RoomActivity.Parcel,RoomActivity.Plant,RoomActivity.Parcel,RoomActivity.Window,RoomActivity.Rest,RoomActivity.Music};return new RoomVignette(Beginnings[edition%12],Notes[edition/12],activities[edition/12],edition);}
    }
    public sealed class ExplorationJournal
    {
        private readonly HashSet<long> visited=new HashSet<long>();
        private readonly HashSet<LiminalEnvironment> biomes=new HashSet<LiminalEnvironment>(),photographed=new HashSet<LiminalEnvironment>();
        private readonly HashSet<string> memories=new HashSet<string>(),actions=new HashSet<string>();
        public int Version {get;private set;}
        public int Rooms {get{return visited.Count;}}
        public int Biomes {get{return biomes.Count+landmarks.Count;}}
        public int Photos {get{return photographed.Count+landmarkPhotos.Count;}}
        private readonly HashSet<string> landmarks=new HashSet<string>(),landmarkPhotos=new HashSet<string>();
        public bool VisitLandmark(string key){bool added=!string.IsNullOrEmpty(key)&&landmarks.Count<64&&landmarks.Add(key);if(added)Version++;return added;}
        public bool PhotographLandmark(string key){bool added=!string.IsNullOrEmpty(key)&&landmarkPhotos.Count<64&&landmarkPhotos.Add(key);if(added)Version++;return added;}
        public int Memories {get{return memories.Count;}}
        public readonly Dictionary<RoomActivity,int> Activities=new Dictionary<RoomActivity,int>();
        public bool Visit(int x,int z,LiminalEnvironment biome)
        {bool added=visited.Count<32768&&visited.Add(ContinuousTopology.CellKey(x,z));bool fresh=biomes.Add(biome);if(added||fresh)Version++;return fresh;}
        public bool Photograph(LiminalEnvironment biome){bool added=photographed.Add(biome);if(added)Version++;return added;}
        public bool Read(string key){bool added=memories.Count<4096&&memories.Add(key);if(added)Version++;return added;}
        public bool Record(RoomActivity activity,string key)
        {
            if(actions.Count>=8192||!actions.Add(activity+":"+key))return false;
            Activities.TryGetValue(activity,out int n);Activities[activity]=n+1;Version++;return true;
        }
        public int Count(RoomActivity activity){return Activities.TryGetValue(activity,out int n)?n:0;}
        public string[] Invitations()
        {return new[]{"Conocer ambientes · "+Biomes+" / 8","Fotografiar lugares distintos · "+Photos+" / 5","Leer notas de otros viajeros · "+Memories+" / 6","Cuidar plantas · "+Count(RoomActivity.Plant)+" / 3","Tocar música · "+Count(RoomActivity.Music)+" / 3","Completar juegos de mesa · "+Count(RoomActivity.Game)+" / 3","Compartir un descanso · "+Count(RoomActivity.Rest)+" / 3"};}
    }
    public sealed class PairingGame
    {
        public readonly int[] Cards=new int[16];
        public int First {get;private set;}=-1;
        public int Second {get;private set;}=-1;
        public int Matched {get;private set;}
        public int Attempts {get;private set;}
        public int Mask {get;private set;}
        public bool Complete {get{return Mask==65535;}}
        public PairingGame(uint seed)
        {var random=new StableRandom(seed);for(int i=0;i<16;i++)Cards[i]=i/2;for(int i=15;i>0;i--){int j=random.Range(i+1);int v=Cards[j];Cards[j]=Cards[i];Cards[i]=v;}}
        public bool Turn(int index)
        {
            if(index<0||index>=16||(Mask&(1<<index))!=0||index==First||Second>=0)return false;
            if(First<0){First=index;return true;}Second=index;Attempts++;
            if(Cards[First]==Cards[Second]){Mask|=1<<First|1<<Second;Matched++;First=Second=-1;}return true;
        }
        public void HideMiss(){if(Second>=0)First=Second=-1;}
        public bool Visible(int index){return index>=0&&index<16&&((Mask&(1<<index))!=0||index==First||index==Second);}
    }
}
