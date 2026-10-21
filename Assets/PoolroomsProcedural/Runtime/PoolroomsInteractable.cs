using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsInteractable : MonoBehaviour
    {
        public RoomActivity activity;
        public string key,title,note;
        public PoolroomsWorld world;
        public Transform movingPart;
        public Vector3 closedPosition;
        public Quaternion closedRotation;
        public GameObject contents;
        private bool open,initialized;
        private float pose;
        private PoolroomsLuminaire lamp;
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        private Renderer[] renderers;
        public string ActionLabel {get{var station=GetComponent<PoolroomsStation>();if(station!=null)return station.Label;return activity==RoomActivity.Notice?"leer y ver caminos":activity==RoomActivity.Rest?"sentarse":activity==RoomActivity.Plant?"cuidar la planta":activity==RoomActivity.Music?"tocar / escuchar música":activity==RoomActivity.Game?"jugar a parejas":activity==RoomActivity.Lamp?"encender / apagar lámpara":activity==RoomActivity.Drawer?(open?"mirar el interior":"abrir mueble"):activity==RoomActivity.Window?"abrir / cerrar contraventana":"leer";}}
        public static void BindSource(PoolroomsPickup pickup,string source)
        {
            RoomActivity kind;
            if(source=="SheenChair"||source=="I_ChairDamaskPurplegold"||source=="I_GlamVelvetSofa"||source=="U_SheenWoodLeatherSofa"||source=="B_CafeChair"||source=="M_Pouf"||source=="M_IndoorChair"||source=="M_Stool")kind=RoomActivity.Rest;
            else if(source=="G_PottedPlant"||source=="R_FlowerVase")kind=RoomActivity.Plant;
            else if(source=="T_ArtNouveauLamp"||source=="R_BarnLamp"||source=="Lantern"||source=="M_StainedLamp"||source=="M_DeskLamp")kind=RoomActivity.Lamp;
            else if(source=="BoomBox")kind=RoomActivity.Music;
            else if(source=="J_Basket"||source=="J_CoatRackA"||source=="J_CoatRackB"||source=="J_Radiator"||source=="J_PlantPot"||source=="M_Refrigerator"||source=="M_MenuSign")kind=RoomActivity.Reading;
            else if(source=="M_CashRegister")kind=RoomActivity.Game;
            else return;
            var interaction=pickup.gameObject.AddComponent<PoolroomsInteractable>();interaction.activity=kind;interaction.key=pickup.SpawnKey;
            interaction.title=ExplorationItems.Resolve("source:"+source)?.Name??source;
            if(source=="J_Basket")interaction.note="La cesta está preparada para compartir. Una nota dice: volvemos después de recoger el té; guarda un sitio en la mesa.";
            if(source=="J_CoatRackA"||source=="J_CoatRackB")interaction.note="Alguien dejó aquí un perchero para las próximas visitas. El lugar sigue esperando a quienes regresan.";
            if(source=="J_Radiator")interaction.note="El taller conserva sus radiadores antiguos. La próxima reunión está anotada para esta tarde; dejaron los bancos preparados.";
            if(source=="J_PlantPot")interaction.note="Esta maceta espera el próximo trasplante. En el borde dejaron una etiqueta con los turnos de riego.";
            if(source=="M_MenuSign")interaction.note="Menú de la tarde: café, té, fruta y una mesa libre. En el reverso alguien escribió: guarda sitio, vuelvo pronto.";
            if(source=="M_Refrigerator")interaction.note="La reserva está preparada para la siguiente apertura. Quedó una lista de reposición junto al vidrio: agua, fruta y algo para compartir. Puedes guardar y mover este expositor; las botellas del modelo son decorativas.";
            interaction.world=pickup.GetComponentInParent<PoolroomsWorld>();
        }
        private static uint StableKey(string value){uint h=2166136261;foreach(char c in value??"")h=unchecked((h^c)*16777619);return h;}
        private void Initialize(PoolroomsInventory inventory)
        {
            if(initialized)return;initialized=true;renderers=GetComponentsInChildren<Renderer>();open=inventory.State("open:"+key);
            if(movingPart!=null){closedPosition=movingPart.localPosition;closedRotation=movingPart.localRotation;pose=open?1:0;ApplyPose();}
            if(activity==RoomActivity.Lamp)
            {
                if(world==null)world=GetComponentInParent<PoolroomsWorld>();
                lamp=world!=null?world.AddInteractiveLamp(transform,Vector3.up*.2f):null;
                if(lamp!=null){lamp.userEnabled=inventory.State("lamp:"+key,false);world.RequestLightingRefresh();}
            }
            if(contents!=null)contents.SetActive(!inventory.WasCollected(key));
            if(activity==RoomActivity.Lamp)ApplyLampVisual();
            if(activity==RoomActivity.Plant&&inventory.State("watered:"+key))ApplyPlantVisual();
        }
        public bool Use(PoolroomsExplorer actor)
        {
            if(actor==null||actor.IsPaused||actor.InputCaptured||!actor.IsReady)return false;
            var station=GetComponent<PoolroomsStation>();if(station!=null)return station.Use(actor);
            var inventory=PoolroomsInventory.For(actor);if(world==null)world=actor.world;Initialize(inventory);
            switch(activity)
            {
                case RoomActivity.Notice:case RoomActivity.Reading:
                    actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Page,transform.position);inventory.Journal.Read(key);inventory.Animate(HumanoidGesture.Read,2.4f);inventory.ShowText(title,note+Directions(actor));return true;
                case RoomActivity.Rest:
                    if(actor.Seating!=null&&actor.Seating.Sit(this)){actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Fabric,transform.position);inventory.Journal.Record(RoomActivity.Rest,key);return true;}return false;
                case RoomActivity.Plant:
                    return ApplyItem(actor,ItemUse.Water);
                case RoomActivity.Lamp:
                    if(lamp==null)return false;actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Switch,transform.position);lamp.userEnabled=!lamp.userEnabled;inventory.SetState("lamp:"+key,lamp.userEnabled);world.RequestLightingRefresh();ApplyLampVisual();
                    inventory.Animate(HumanoidGesture.Adjust);inventory.Toast(lamp.userEnabled?"Lámpara encendida.":"Lámpara apagada.");return true;
                case RoomActivity.Music:
                    if(movingPart!=null){inventory.OpenPiano(key,transform.position,GetComponent<PoolroomsPianoKeys>());return true;}
                    var player=actor.GetComponent<PoolroomsMusicPlayer>();if(player==null||player.Tracks.Count==0){inventory.Toast("Puedes añadir tus canciones desde Ajustes → Sonido.");return false;}
                    player.PlayPause();inventory.Animate(HumanoidGesture.Adjust);inventory.Journal.Record(RoomActivity.Music,key);return true;
                case RoomActivity.Game:inventory.OpenGame(key,LiminalRegions.Hash(1,unchecked((int)StableKey(key)),0,0xCAFEu),transform.position);return true;
                case RoomActivity.Drawer:
                    if(!open){actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Latch,transform.position);open=true;inventory.SetState("open:"+key,true);inventory.Animate(HumanoidGesture.OpenDoor);return true;}
                    inventory.ShowContainer(this);return true;
                case RoomActivity.Window:
                    actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Latch,transform.position);open=!open;inventory.SetState("open:"+key,open);inventory.Animate(HumanoidGesture.Adjust);return true;
                default:return false;
            }
        }
        public bool CollectContents(PoolroomsInventory inventory)
        {
            if(inventory.WasCollected(key)){inventory.Toast("La caja ya está vacía. Gracias por dejarla ordenada.");return false;}
            // A single successful transaction; full inventories cannot consume the container.
            if(!inventory.Items.Add("tool:8",2)){inventory.Toast("Hace falta un espacio para las galletas.");return false;}
            inventory.MarkCollected(key);if(contents!=null)contents.SetActive(false);inventory.Animate(HumanoidGesture.Reach);
            inventory.Toast("Dos porciones de galletas guardadas.");inventory.RefreshAll();return true;
        }
        public void CloseCabinet(PoolroomsInventory inventory){open=false;inventory.SetState("open:"+key,false);inventory.Animate(HumanoidGesture.OpenDoor);}
        public bool ApplyItem(PoolroomsExplorer actor,ItemUse use)
        {
            var inventory=PoolroomsInventory.For(actor);if(world==null)world=actor.world;Initialize(inventory);
            if(use==ItemUse.Water&&activity==RoomActivity.Plant)
            {
                if(inventory.State("watered:"+key)){inventory.Toast("La planta ya tiene agua suficiente.");return false;}
                actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Pour,transform.position);inventory.SetState("watered:"+key,true);inventory.Journal.Record(RoomActivity.Plant,key);inventory.Animate(HumanoidGesture.Water,2);
                ApplyPlantVisual();inventory.Toast("Planta cuidada. Alguien agradecerá encontrarla así.");return true;
            }
            if(use==ItemUse.Clean&&activity!=RoomActivity.Notice)
            {
                actor.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Fabric,transform.position);inventory.SetState("clean:"+key,true);inventory.Animate(HumanoidGesture.Clean,2.2f);
                // Mild physical roughness change through a property block, preserving all source maps/materials.
                foreach(var renderer in renderers)if(renderer!=null){renderer.GetPropertyBlock(properties);properties.SetFloat("_Smoothness",.36f);properties.SetFloat("_Glossiness",.36f);renderer.SetPropertyBlock(properties);}
                inventory.Toast("Rincón limpio y listo para otra visita.");return true;
            }
            return false;
        }
        private void ApplyLampVisual()
        {
            if(renderers==null)return;
            foreach(var renderer in renderers)if(renderer!=null)
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var material=materials[i];if(material==null)continue;renderer.GetPropertyBlock(properties,i);
                    foreach(string name in new[]{"_EmissiveColor","_EmissionColor"})if(material.HasProperty(name))properties.SetColor(name,lamp!=null&&lamp.userEnabled?material.GetColor(name):Color.black);
                    renderer.SetPropertyBlock(properties,i);
                }
            }
        }
        private void ApplyPlantVisual()
        {
            // Only darken the pot near its base; leaf alpha and all normal/roughness maps stay intact.
            foreach(var renderer in renderers)if(renderer!=null&&renderer.bounds.center.y<transform.position.y+.08f)
            {
                var material=renderer.sharedMaterial;if(material==null)continue;renderer.GetPropertyBlock(properties);
                if(material.HasProperty("_BaseColor"))properties.SetColor("_BaseColor",material.GetColor("_BaseColor")*new Color(.94f,.96f,.94f,1));
                renderer.SetPropertyBlock(properties);
            }
        }
        private string Directions(PoolroomsExplorer actor)
        {
            var layout=world!=null?world.CurrentLayout:actor.world!=null?actor.world.CurrentLayout:null;var room=actor.world!=null?actor.world.RoomAtViewer():null;
            if(layout==null||room==null)return "";string text="\n\nCaminos desde esta sala:";string[] cardinal={"Norte","Este","Sur","Oeste"};
            for(int d=0;d<4;d++)if(room.HasExit(d)){var neighbor=layout.Neighbour(room,d);if(neighbor!=null)text+="\n"+cardinal[d]+" → "+PoolroomsInventory.EnvironmentName(neighbor.Environment);}
            return text+"\n\nLos carteles son orientaciones. No necesitas administrador ni teletransporte.";
        }
        private void Update()
        {
            if(!initialized||movingPart==null||Time.deltaTime<=0)return;
            float target=open?1:0;if(Mathf.Abs(pose-target)<.0001f)return;pose=Mathf.MoveTowards(pose,target,Time.deltaTime*1.8f);ApplyPose();
        }
        private void ApplyPose()
        {if(movingPart!=null)movingPart.localRotation=closedRotation*Quaternion.Euler(activity==RoomActivity.Window?-pose*76:0,activity==RoomActivity.Drawer?pose*105:0,0);}
        private void OnDestroy(){if(lamp!=null&&world!=null)world.RequestLightingRefresh();}
    }
}
