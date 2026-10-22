using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using BigInteger=System.Numerics.BigInteger;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed partial class PoolroomsInventory : MonoBehaviour
    {
        public readonly ExplorationInventory Items=new ExplorationInventory();
        public readonly ExplorationJournal Journal=new ExplorationJournal();
        private readonly HashSet<string> collected=new HashSet<string>();
        private readonly Dictionary<string,bool> states=new Dictionary<string,bool>();
        private readonly Queue<string> stateOrder=new Queue<string>();
        private readonly Dictionary<string,PairingGame> games=new Dictionary<string,PairingGame>();
        private PoolroomsExplorer explorer;
        private PoolroomsCarry carry;
        private PoolroomsSoundscape sound;
        private readonly RaycastHit[] query=new RaycastHit[32];
        private float probeAt,toastUntil,gestureLeft,gestureDuration;
        private long lastRoom=long.MinValue;
        private int lastSeed;
        private PoolroomsWorld lastWorld;
        public bool InterfaceOpen {get;private set;}
        public bool HotbarVisible {get;private set;}
        public int SelectedHotbar {get;private set;}
        public HumanoidGesture Gesture {get;private set;}
        public float GestureProgress {get{return gestureDuration>0?1-gestureLeft/gestureDuration:0;}}
        public string Status {get;private set;}="";
        private PoolroomsInteractable focused;
        private PoolroomsPickup focusedPickup;
        private bool closingThisFrame;
        private CursorLockMode previousLock;
        private bool previousCursor;
        public static PoolroomsInventory For(PoolroomsExplorer actor)
        {if(actor==null)return null;var inventory=actor.GetComponent<PoolroomsInventory>();return inventory!=null?inventory:actor.gameObject.AddComponent<PoolroomsInventory>();}
        private void Awake()
        {
            explorer=GetComponent<PoolroomsExplorer>();carry=GetComponent<PoolroomsCarry>();if(carry==null)carry=gameObject.AddComponent<PoolroomsCarry>();
            for(int i=0;i<ExplorationItems.ToolCount;i++)Items.Add(ExplorationItems.Tool(i).Key,i==7||i==8?new BigInteger(4):BigInteger.One);
            for(int i=0;i<ExplorationInventory.HotbarSize;i++)Items.Bind(i,i);
        }
        private void Start(){sound=GetComponent<PoolroomsSoundscape>();Toast("Tab · inventario    5 · barra rápida    E · usar / tomar    T · mover    Z · guardar    X · usar objeto",9);}
        public void Toast(string message,float seconds=3.5f){Status=message;toastUntil=Time.unscaledTime+seconds;RefreshToast();}
        public bool WasCollected(string key){return key!=null&&collected.Contains(key);}
        public void MarkCollected(string key)
        {if(key!=null)collected.Add(key);}
        public bool State(string key,bool fallback=false){return key!=null&&states.TryGetValue(key,out bool value)?value:fallback;}
        public void SetState(string key,bool value)
        {if(!states.ContainsKey(key)){stateOrder.Enqueue(key);while(stateOrder.Count>8192)states.Remove(stateOrder.Dequeue());}states[key]=value;}
        public void Animate(HumanoidGesture gesture,float seconds=1.3f)
        {Gesture=gesture;gestureDuration=gestureLeft=Mathf.Max(.2f,seconds);
            if(gesture==HumanoidGesture.Reach||gesture==HumanoidGesture.Place||gesture==HumanoidGesture.OpenDoor||gesture==HumanoidGesture.Adjust)sound?.Handling(explorer.Aim.position+explorer.Aim.forward*.5f,true);}
        public bool HandleInput(bool toggleInventory,bool toggleBar,bool store,bool use,bool moveObject,int hotbar,float scroll,bool escape)
        {
            closingThisFrame=false;
            if(explorer.Views!=null&&explorer.Views.PhotoActive)return false;
            if(InterfaceOpen&&(escape||toggleInventory)){CloseInterface();closingThisFrame=true;return true;}
            if(toggleInventory&&!explorer.IsPaused&&explorer.IsReady&&!explorer.InputCaptured&&explorer.Vehicle==null&&!explorer.IsAquaticRiding){OpenInventory();return true;}
            if(explorer.IsPaused||explorer.InputCaptured||!explorer.IsReady||closingThisFrame)return false;
            if(toggleBar){HotbarVisible=!HotbarVisible;EnsureUI();RefreshHotbar();}
            if(HotbarVisible)
            {
                if(hotbar>=0&&hotbar<ExplorationInventory.HotbarSize)SelectedHotbar=hotbar;
                if(Mathf.Abs(scroll)>.01f)SelectedHotbar=(SelectedHotbar+(scroll>0?-1:1)+ExplorationInventory.HotbarSize)%ExplorationInventory.HotbarSize;
                RefreshHotbar();
            }
            if(store)StoreHeld();else if(use)Use(Items.Binding(SelectedHotbar));else if(moveObject)carry.Interact(true);
            return false;
        }
        public bool StoreHeld()
        {
            var item=carry.Held;if(item==null){Toast("Primero toma un objeto con E o T.");return false;}
            if(!carry.TryStoreHeld(Items)){Toast("No hay espacio para otro tipo de objeto. Las pilas existentes no tienen límite.");return false;}
            Animate(HumanoidGesture.Place);Toast("Objeto guardado.");RefreshAll();return true;
        }
        public bool Equip(string key)
        {
            var item=ExplorationItems.Resolve(key);if(item==null||Items.Count(key)<=0)return false;
            if(carry.Held!=null){Toast("Guarda o suelta primero el objeto de tus manos.");return false;}
            var world=explorer.world;if(world==null||world.profile==null||!world.ViewerReady)return false;
            var spawned=Spawn(item,world.profile,explorer.Aim.position);
            if(spawned==null){Toast("No se pudo preparar ese modelo.");return false;}
            if(!carry.GrabFromInventory(spawned)){Destroy(spawned.gameObject);return false;}
            if(!Items.Remove(key,BigInteger.One))throw new InvalidOperationException("Inventory transaction changed during equip.");
            CloseInterface();Animate(HumanoidGesture.Reach);RefreshAll();return true;
        }
        private static PoolroomsPickup Spawn(ExplorationItem item,PoolroomsProfile profile,Vector3 position)
        {
            string key="carried:"+Guid.NewGuid().ToString("N");PoolroomsPickup result;
            if(item.Key.StartsWith("tool:",StringComparison.Ordinal)&&int.TryParse(item.Key.Substring(5),out int tool)&&EncounterTools.Available(tool))result=PoolroomsPickup.CreateTool(key,tool,position,profile);
            else if(item.Source!=null)result=PoolroomsPickup.CreateSource(key,item.Source,position,null,SourceFurniture.Longest(item.Source));
            else if(item.PlayKind>=0)result=PoolroomsPickup.CreatePlayProp(key,item.PlayKind,item.PlayColour,position,null,profile);
            else result=PoolroomsPickup.CreateCatalog(key,item.CatalogId>=0?item.CatalogId:0,position,null,profile);
            if(result!=null)result.AssignInventoryKey(item.Key);return result;
        }
        public bool Use(string key)
        {
            if(explorer.IsPaused||!explorer.IsReady||explorer.Vehicle!=null||explorer.IsAquaticRiding||gestureLeft>.1f)return false;
            var item=ExplorationItems.Resolve(key);if(item==null||Items.Count(key)<=0){Toast("Elige un objeto que tengas en el inventario.");return false;}
            switch(item.Use)
            {
                case ItemUse.Journal:OpenJournal();break;
                case ItemUse.Read:Animate(HumanoidGesture.Read,2.5f);ShowText(item.Name,RoomMemories.At(explorer.world.CurrentLayout.Config.Seed,CurrentCell().x,CurrentCell().y,CurrentBiome()).Note);break;
                case ItemUse.Drink:Animate(HumanoidGesture.Drink,1.8f);Toast("Una pausa para beber.");Journal.Record(RoomActivity.Tea,CurrentCell()+":"+key);break;
                case ItemUse.Eat:Animate(HumanoidGesture.Eat,1.7f);Toast("Un pequeño descanso. No hay medidor de hambre.");break;
                case ItemUse.Photograph:
                    if(InterfaceOpen)CloseInterface();Animate(HumanoidGesture.Photograph,1.1f);TakePhoto();break;
                case ItemUse.Water:case ItemUse.Clean:
                    var target=FindTarget();if(target==null||!target.ApplyItem(explorer,item.Use)){Toast(item.Use==ItemUse.Water?"Mira una maceta cercana para cuidarla.":"Mira un mueble cercano para limpiarlo.");return false;}break;
                case ItemUse.Mark:
                    if(!PlaceMarker()){Toast("Hace falta un suelo cercano y libre para dejar una señal.");return false;}
                    Animate(HumanoidGesture.Place);Toast("Señal de tiza colocada durante esta sesión.");break;
                case ItemUse.Listen:
                    var player=GetComponent<PoolroomsMusicPlayer>();if(player==null||player.Tracks.Count==0){Toast("Añade canciones desde Ajustes → Sonido → Tu música.");return false;}player.PlayPause();Animate(HumanoidGesture.Adjust);break;
                case ItemUse.Play:case ItemUse.None:return Equip(key);
                default:return false;
            }
            if(item.Use==ItemUse.Drink||item.Use==ItemUse.Eat||item.Use==ItemUse.Read||item.Use==ItemUse.Photograph)ShowPresentation(item);
            if(item.Consumable&&!Items.Remove(key,BigInteger.One))throw new InvalidOperationException("Inventory consumption changed unexpectedly.");
            RefreshAll();return true;
        }
        private Vector2Int CurrentCell(){var room=explorer.world!=null?explorer.world.RoomAtViewer():null;return room!=null?new Vector2Int(room.X,room.Z):new Vector2Int(Mathf.FloorToInt(transform.position.x/36),Mathf.FloorToInt(transform.position.z/36));}
        public LiminalEnvironment CurrentBiome(){if(explorer.world!=null&&explorer.world.Terrain!=null&&explorer.world.Terrain.IsInside)return LiminalEnvironment.RainHouses;return explorer.world!=null?explorer.world.RoomAtViewer()?.Environment??LiminalEnvironment.Poolrooms:LiminalEnvironment.Poolrooms;}
        private PoolroomsInteractable FindTarget()
        {
            var aim=explorer.Aim;int count=Physics.RaycastNonAlloc(aim.position,aim.forward,query,CarryModel.Reach,~0,QueryTriggerInteraction.Collide);if(count==query.Length)return null;
            PoolroomsInteractable found=null;float distance=float.MaxValue;
            for(int i=0;i<count;i++)
            {var hit=query[i];if(hit.collider==null||hit.collider.transform.IsChildOf(transform)||hit.collider.isTrigger)continue;if(hit.distance<distance){distance=hit.distance;found=hit.collider.GetComponentInParent<PoolroomsInteractable>();}}
            return found;
        }
        private void Update()
        {
            if(explorer==null)return;if(gestureLeft>0&&!explorer.IsPaused&&Time.deltaTime>0){gestureLeft=Mathf.Max(0,gestureLeft-Time.deltaTime);if(gestureLeft<=0)Gesture=HumanoidGesture.None;}
            if(root!=null){root.style.display=photographing||explorer.IsPaused||explorer.Views!=null&&explorer.Views.PhotoActive?UnityEngine.UIElements.DisplayStyle.None:UnityEngine.UIElements.DisplayStyle.Flex;}
            UpdatePresentation();
            if(explorer.IsPaused||!explorer.IsReady||explorer.InputCaptured)return;
            if(Time.time>=probeAt)
            {
                probeAt=Time.time+.22f;focused=FindTarget();focusedPickup=null;
                if(focused==null)
                {
                    var aim=explorer.Aim;int count=Physics.RaycastNonAlloc(aim.position,aim.forward,query,CarryModel.Reach,~0,QueryTriggerInteraction.Ignore);
                    float distance=float.MaxValue;if(count<query.Length)for(int i=0;i<count;i++)if(query[i].collider!=null&&!query[i].collider.transform.IsChildOf(transform)&&query[i].distance<distance){distance=query[i].distance;focusedPickup=query[i].collider.GetComponentInParent<PoolroomsPickup>();}
                }
                RefreshPrompt();
                var world=explorer.world;var room=world!=null?world.RoomAtViewer():null;
                if(room!=null&&world.CurrentLayout!=null)
                {
                    long key=ContinuousTopology.CellKey(room.X,room.Z);int seed=world.CurrentLayout.Config.Seed;
                    if(world.Terrain!=null&&world.Terrain.IsInside){if(Journal.VisitLandmark("Level94"))Toast("Nuevo ambiente · Colinas del nivel 94");}
                    else if(world.InsideMall){if(Journal.VisitLandmark("CompleteMall"))Toast("Nuevo ambiente · Centro comercial completo");}
                    else if(key!=lastRoom||world!=lastWorld||seed!=lastSeed){lastRoom=key;lastWorld=world;lastSeed=seed;UpdateRoute(room);if(Journal.Visit(room.X,room.Z,room.Environment))Toast("Nuevo ambiente · "+EnvironmentName(room.Environment));}
                }
            }
            if(root!=null&&Time.unscaledTime>toastUntil)toastLabel.text="";
        }
        public static string EnvironmentName(LiminalEnvironment e)
        {if(HorizonLevelPlanner.IsLevel(e))return HorizonLevelPlanner.Name(e);if(JourneyLevelPlanner.IsLevel(e))return JourneyLevelPlanner.Name(e);if(SpecialRegionPlanner.IsLevel(e))return SpecialRegionPlanner.Name(e);switch(e){case LiminalEnvironment.Poolrooms:return "Galerías de agua";case LiminalEnvironment.Supermarket:return "Galerías comerciales";case LiminalEnvironment.RainHouses:return "Casas entre colinas";case LiminalEnvironment.SoftPlay:return "Juegos de varios pisos";case LiminalEnvironment.City11:return "Ciudad de la tarde";case LiminalEnvironment.Orangerie:return "Invernaderos";case LiminalEnvironment.AquaticPark:return "Parque acuático";case LiminalEnvironment.SunsetLibrary:return "Biblioteca del atardecer";default:return e.ToString();}}
        private void OnDisable(){CloseInterface();StopAllCoroutines();findingRoute=false;photographing=false;if(presentation!=null)Destroy(presentation);presentation=null;DisposeUI();}
        private void OnDestroy(){DisposeMarkers();}
    }
}
