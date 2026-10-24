using BigInteger = System.Numerics.BigInteger;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsLevel10Activity : MonoBehaviour
    {
        Level10Action kind;Level10ActionState state;string key,note;GameObject moving;
        bool restored,animating;float began,waterUntil;GameObject water;PoolroomsExplorer operatorActor;Quaternion from,to;
        public string Label
        {
            get
            {
                switch(kind)
                {
                    case Level10Action.FieldNotes:return "leer el cuaderno y los caminos";
                    case Level10Action.Rest:return "sentarse y escuchar los campos";
                    case Level10Action.WheatBundle:case Level10Action.SeedPacket:return "guardar recuerdo";
                    case Level10Action.GrainMill:return state!=null&&state.Complete?"recoger ficha del molino":"girar el molino";
                    case Level10Action.HandPump:return state!=null&&state.Complete?"recoger ficha del agua":"accionar la bomba";
                    case Level10Action.SeedCabinet:return state!=null&&state.Complete?"recoger ficha del armario":"ordenar y abrir el armario";
                    default:return state!=null&&state.Complete?"recoger ficha de la veleta":"orientar la veleta";
                }
            }
        }
        public void Configure(Level10Action action,string address,uint seed,GameObject mechanism,GameObject flow,string story)
        {kind=action;key=address;state=new Level10ActionState(action,seed);moving=mechanism;water=flow;note=story;}
        public void Restore(PoolroomsInventory inventory)
        {
            if(restored||inventory==null||state==null)return;restored=true;
            int position=0;for(int bit=0;bit<3;bit++)if(inventory.State(key+":p"+bit))position|=1<<bit;
            if(inventory.State(key+":saved"))state.Restore(position,inventory.State(key+":complete"));
            if(moving!=null)moving.transform.localRotation=Pose();
        }
        public bool Use(PoolroomsExplorer actor)
        {
            if(actor==null||actor.IsPaused||actor.InputCaptured||!actor.IsReady||state==null)return false;
            var inventory=PoolroomsInventory.For(actor);if(inventory==null)return false;operatorActor=actor;Restore(inventory);
            var sound=actor.GetComponent<PoolroomsSoundscape>();
            if(kind==Level10Action.FieldNotes)
            {
                inventory.Journal.Read(key);inventory.ShowText("Cuaderno del Nivel 10",note+"\n\n"+PoolroomsLevel10.Routes(actor));
                inventory.Animate(HumanoidGesture.Read,2.5f);sound?.PlayStation(StationSound.Page,transform.position);return true;
            }
            if(kind==Level10Action.Rest)
            {inventory.Journal.Record(RoomActivity.Rest,key);actor.Seating?.Sit(GetComponent<PoolroomsInteractable>());inventory.Toast("El viento pasa entre las espigas. El banco queda libre para descansar.",5);return true;}
            if(kind==Level10Action.WheatBundle||kind==Level10Action.SeedPacket)
            {
                if(inventory.WasCollected(key))return false;int id=kind==Level10Action.WheatBundle?0:1;
                if(!inventory.Items.Add("level10:"+id,BigInteger.One)){inventory.Toast("El inventario está lleno. El recuerdo sigue aquí.");return true;}
                inventory.MarkCollected(key);inventory.Animate(HumanoidGesture.Reach);sound?.PlayStation(StationSound.Leaves,transform.position);inventory.Toast("Guardado: "+Level10Activities.Item(id).Name);gameObject.SetActive(false);return true;
            }
            if(state.Complete)
            {
                if(kind==Level10Action.HandPump){from=moving!=null?moving.transform.localRotation:Quaternion.identity;to=Pose();began=Time.unscaledTime;animating=true;RunWater();sound?.PlayStation(StationSound.WaterDrop,transform.position);}
                return Reward(inventory);
            }
            if(animating)return true;
            state.Advance();Save(inventory);if(kind==Level10Action.HandPump)RunWater();
            if(moving!=null){from=moving.transform.localRotation;to=Pose();began=Time.unscaledTime;animating=true;}
            inventory.Animate(kind==Level10Action.HandPump?HumanoidGesture.Adjust:HumanoidGesture.Reach);
            sound?.PlayStation(kind==Level10Action.HandPump?StationSound.WaterDrop:kind==Level10Action.GrainMill?StationSound.Ceramic:StationSound.Latch,transform.position);
            inventory.Toast(state.Feedback(),6);if(state.Complete)Reward(inventory);return true;
        }
        bool Reward(PoolroomsInventory inventory)
        {
            if(inventory.WasCollected(key+":reward")){inventory.Toast("El rincón ya está preparado. Puedes continuar el camino.");return true;}
            string reward=kind==Level10Action.SeedCabinet?"level10:1":"level10:2";
            if(!inventory.Items.Add(reward,BigInteger.One)){inventory.Toast("El trabajo queda guardado; libera un espacio para su ficha.");return true;}
            inventory.MarkCollected(key+":reward");inventory.Journal.Record(RoomActivity.Game,key);inventory.Toast("Guardado: "+ExplorationItems.Resolve(reward).Name,5);return true;
        }
        void Save(PoolroomsInventory inventory)
        {inventory.SetState(key+":saved",true);for(int bit=0;bit<3;bit++)inventory.SetState(key+":p"+bit,(state.Position&(1<<bit))!=0);inventory.SetState(key+":complete",state.Complete);}
        Quaternion Pose()
        {
            if(kind==Level10Action.GrainMill)return Quaternion.Euler(0,state.Position*120,0);
            if(kind==Level10Action.WindVane)return Quaternion.Euler(0,state.Position*90,0);
            if(kind==Level10Action.SeedCabinet)return Quaternion.Euler(0,-state.Position*55,0);
            return Quaternion.identity;
        }
        void RunWater(){waterUntil=Time.unscaledTime+1.8f;if(water!=null)water.SetActive(true);}
        void Update()
        {
            if(operatorActor!=null&&(operatorActor.IsPaused||operatorActor.Views!=null&&operatorActor.Views.PhotoActive)){began+=Time.unscaledDeltaTime;waterUntil+=Time.unscaledDeltaTime;return;}
            if(water!=null&&water.activeSelf&&Time.unscaledTime>=waterUntil)water.SetActive(false);
            if(!animating||moving==null)return;
            float t=Mathf.Clamp01((Time.unscaledTime-began)/.55f),smooth=t*t*(3-2*t);
            moving.transform.localRotation=kind==Level10Action.HandPump?Quaternion.Euler(Mathf.Sin(t*Mathf.PI)*-35,0,0):Quaternion.Slerp(from,to,smooth);
            if(t>=1){moving.transform.localRotation=to;animating=false;}
        }
    }
}
