using UnityEngine;
using Poolrooms.Core;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsSeating : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private PoolroomsInteractable seat;
        private Collider[] seatColliders;
        private bool[] seatCollisionEnabled;
        private readonly Collider[] overlaps=new Collider[24];
        private readonly RaycastHit[] hits=new RaycastHit[24];
        private Vector3 entryFeet;
        private float seatFloor;
        private bool restoreController;
        public bool IsSeated {get{return seat!=null;}}
        public Vector3 CameraOffset {get{return IsSeated?Vector3.down*.45f:Vector3.zero;}}
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();}
        private void OnEnable(){if(explorer==null)explorer=GetComponent<PoolroomsExplorer>();explorer.OriginShifted+=Shift;}
        private void OnDisable(){if(explorer!=null)explorer.OriginShifted-=Shift;LeaveForRelocation();}
        public bool Sit(PoolroomsInteractable target)
        {
            if(target==null||IsSeated||body==null||!body.enabled||explorer.IsFlying||explorer.Vehicle!=null||explorer.IsAquaticRiding||explorer.IsPaused||explorer.InputCaptured)return false;
            if(Vector3.Distance(target.transform.position,explorer.transform.position)>3.2f)return false;
            var held=GetComponent<PoolroomsCarry>()?.Held;if(held!=null&&held.CarryRadius>.45f){explorer.Inventory?.Toast("Deja el objeto grande antes de sentarte.");return false;}
            var shape=target.GetComponent<Collider>();seatFloor=shape!=null?shape.bounds.min.y:target.transform.position.y-.45f;
            var destination=new Vector3(target.transform.position.x,seatFloor+.28f,target.transform.position.z);
            // A seated capsule still needs headroom, independent of the chair's collider.
            int count=Physics.OverlapCapsuleNonAlloc(destination+Vector3.up*.6f,destination+Vector3.up*1.4f,.28f,overlaps,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length)return false;
            for(int i=0;i<count;i++)if(overlaps[i]!=null&&!overlaps[i].transform.IsChildOf(transform)&&!overlaps[i].transform.IsChildOf(target.transform))return false;
            entryFeet=transform.position;seat=target;restoreController=body.enabled;body.enabled=false;seatColliders=target.GetComponentsInChildren<Collider>();seatCollisionEnabled=new bool[seatColliders.Length];for(int i=0;i<seatColliders.Length;i++){seatCollisionEnabled[i]=seatColliders[i].enabled;seatColliders[i].enabled=false;}
            explorer.Wings?.Stop();transform.position=destination;transform.rotation=Quaternion.Euler(0,target.transform.eulerAngles.y,0);
            explorer.Inventory?.Toast("Un momento de descanso. E, Espacio o caminar para levantarte.");return true;
        }
        public bool TryStand()
        {
            if(!IsSeated)return false;
            for(int i=0;i<5;i++)
            {
                Vector3 candidate=i==4?entryFeet:seat.transform.position+Quaternion.Euler(0,i*90,0)*seat.transform.forward*1.25f;
                int n=Physics.RaycastNonAlloc(candidate+Vector3.up*1.2f,Vector3.down,hits,2.7f,~0,QueryTriggerInteraction.Ignore);if(n==hits.Length)continue;
                float nearest=float.MaxValue;Vector3 floor=Vector3.zero;bool found=false;
                for(int h=0;h<n;h++)if(hits[h].collider!=null&&!hits[h].collider.transform.IsChildOf(transform)&&!hits[h].collider.transform.IsChildOf(seat.transform)&&hits[h].normal.y>.7f&&hits[h].distance<nearest){nearest=hits[h].distance;floor=hits[h].point+Vector3.up*.025f;found=true;}
                if(!found||explorer.world!=null&&!explorer.world.IsPositionStreamed(floor))continue;
                int c=Physics.OverlapCapsuleNonAlloc(floor+Vector3.up*(body.radius+.03f),floor+Vector3.up*(body.height-body.radius),body.radius,overlaps,~0,QueryTriggerInteraction.Ignore);
                if(c==overlaps.Length)continue;bool blocked=false;
                for(int h=0;h<c;h++)if(overlaps[h]!=null&&!overlaps[h].transform.IsChildOf(transform))blocked=true;
                // A disabled chair collider is not in the query: its footprint must also clear us.
                var delta=floor-seat.transform.position;delta.y=0;if(delta.sqrMagnitude<.9f*.9f)blocked=true;
                if(blocked)continue;
                RestoreSeat();seat=null;transform.position=floor;body.enabled=restoreController;return true;
            }
            explorer.Inventory?.Toast("No hay espacio libre para levantarse por ese lado.");return false;
        }
        private void Update(){if(seatColliders!=null&&seat==null)LeaveForRelocation();}
        public void Tick()
        {
            if(seat==null){LeaveForRelocation();return;}
            transform.position=new Vector3(seat.transform.position.x,seatFloor+.28f,seat.transform.position.z);
        }
        private void Shift(Vector3 offset){entryFeet+=offset;seatFloor+=offset.y;}
        private void RestoreSeat(){if(seatColliders!=null)for(int i=0;i<seatColliders.Length;i++)if(seatColliders[i]!=null)seatColliders[i].enabled=seatCollisionEnabled[i];seatColliders=null;seatCollisionEnabled=null;}
        public void LeaveForRelocation(){if(seatColliders==null&&seat==null)return;RestoreSeat();seat=null;if(body!=null)body.enabled=restoreController;}
    }
}
