using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsCarry : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private PoolroomsPickup held;
        private readonly HashSet<string> taken=new HashSet<string>();
        private readonly Queue<string> takenOrder=new Queue<string>();
        private readonly List<PoolroomsPickup> loose=new List<PoolroomsPickup>();
        private readonly RaycastHit[] hits=new RaycastHit[32];
        private readonly Collider[] overlaps=new Collider[32];
        private Transform exteriorLooseRoot;
        public PoolroomsPickup Held { get { return held; } }
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();}
        private void OnEnable(){explorer=GetComponent<PoolroomsExplorer>();explorer.OriginShifted+=ShiftLoose;}
        private void OnDisable(){if(explorer!=null)explorer.OriginShifted-=ShiftLoose;ReleaseHeld(false);}
        private void OnDestroy(){if(exteriorLooseRoot!=null)Destroy(exteriorLooseRoot.gameObject);}
        public void ShowExteriorObjects(bool visible){if(exteriorLooseRoot!=null)exteriorLooseRoot.gameObject.SetActive(visible);}
        public bool WasTaken(string key){return taken.Contains(key);}
        public void MarkTaken(string key)
        {
            if(!taken.Add(key))return;takenOrder.Enqueue(key);
            while(takenOrder.Count>32768)taken.Remove(takenOrder.Dequeue());
        }
        private Transform Eyes { get { return explorer.eyes!=null?explorer.eyes:transform; } }
        public void Interact()
        {
            if(explorer.IsPaused||explorer.InputCaptured||!isActiveAndEnabled)return;
            Transform eyes=Eyes;int count=Physics.RaycastNonAlloc(eyes.position,eyes.forward,hits,CarryModel.Reach,~0,QueryTriggerInteraction.Collide);
            if(count==hits.Length)return; // A truncated query may omit a nearer wall.
            float solid=float.PositiveInfinity,candidate=float.PositiveInfinity;PoolroomsPickup pickup=null;PoolroomsMallPortal portal=null;PoolroomsTerrainPortal terrainPortal=null;PoolroomsCityVehicle vehicle=null;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.collider==null||hit.collider.transform.IsChildOf(transform))continue;
                var item=hit.collider.GetComponentInParent<PoolroomsPickup>();var door=hit.collider.GetComponentInParent<PoolroomsMallPortal>();var landscape=hit.collider.GetComponentInParent<PoolroomsTerrainPortal>();
                var car=hit.collider.GetComponentInParent<PoolroomsCityVehicle>();
                if(!hit.collider.isTrigger)solid=Mathf.Min(solid,hit.distance);
                if((item!=null||door!=null||landscape!=null||car!=null)&&hit.distance<candidate){candidate=hit.distance;pickup=item;portal=door;terrainPortal=landscape;vehicle=car;}
            }
            var world=explorer.world;
            PoolroomsMallDisplay display;int slot;float displayDistance;
            if(held==null&&world!=null&&world.FindMallDisplay(eyes.position,eyes.forward,CarryModel.Reach,out display,out slot,out displayDistance)&&displayDistance<candidate&&displayDistance<=solid+.005f)
            {display.Take(slot);return;}
            if(candidate>solid+.02f)return;
            if(vehicle!=null){vehicle.Enter(explorer);return;}
            if(terrainPortal!=null){terrainPortal.Use();return;}
            if(portal!=null){portal.Use();return;}
            if(pickup!=null&&CarryModel.CanGrab(held!=null,explorer.InputCaptured,explorer.IsPaused,candidate))Grab(pickup);
        }
        public bool Grab(PoolroomsPickup item)
        {
            if(item==null||held!=null||explorer.IsPaused||explorer.InputCaptured)return false;
            held=item;MarkTaken(item.SpawnKey);item.Attach(Eyes);return true;
        }
        public bool Drop()
        {
            if(held==null||explorer.IsPaused||explorer.InputCaptured)return false;
            Vector3 direction=(Eyes.forward+Vector3.down*.15f).normalized;
            float distance=FreeDistance(Eyes.position,direction,.9f);
            if(distance<.3f)return false;
            Vector3 release=Eyes.position+direction*distance;
            int contactCount=Physics.OverlapSphereNonAlloc(release,held.CarryRadius,overlaps,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<contactCount;i++)if(overlaps[i]!=null&&!overlaps[i].transform.IsChildOf(transform))return false;
            Transform target;
            var malls=explorer.world!=null?explorer.world.Malls:null;
            if(explorer.world!=null&&explorer.world.Terrain!=null&&explorer.world.Terrain.IsInside)target=explorer.world.Terrain.LooseRoot;
            else if(malls!=null&&malls.IsInside)target=malls.InteriorRoot;
            else
            {
                if(exteriorLooseRoot==null){var go=new GameObject("Loose exploration objects");exteriorLooseRoot=go.transform;if(explorer.world!=null)exteriorLooseRoot.SetParent(explorer.world.transform,false);}
                target=exteriorLooseRoot;
            }
            Vector3 localVelocity=explorer.ViewVelocity;
            Vector3 releaseVelocity=explorer.transform.TransformDirection(localVelocity);
            var item=held;held=null;item.Release(target,release,releaseVelocity);loose.RemoveAll(p=>p==null);loose.Add(item);
            while(loose.Count>32){var oldest=loose[0];loose.RemoveAt(0);if(oldest!=null&&oldest!=held)Destroy(oldest.gameObject);}
            return true;
        }
        private void ReleaseHeld(bool physics)
        {
            if(held==null)return;
            if(physics){Drop();return;}
            var item=held;held=null;Destroy(item.gameObject);
        }
        private float FreeDistance(Vector3 position,Vector3 direction,float max)
        {
            int count=Physics.SphereCastNonAlloc(position,held!=null?Mathf.Max(.19f,held.CarryRadius):.19f,direction,hits,max,~0,QueryTriggerInteraction.Ignore);float distance=max;
            if(count==hits.Length)return 0;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform))distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-.03f));
            // SphereCast misses colliders overlapping its starting sphere: the centre ray
            // also prevents a close wall from letting the held/dropped bag pass through.
            count=Physics.RaycastNonAlloc(position,direction,hits,max,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return 0;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform))distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-(held!=null?held.CarryRadius:.27f)));
            return distance;
        }
        private void LateUpdate()
        {
            if(held==null||explorer.IsPaused||Time.deltaTime<=0)return;
            Vector3 target=new Vector3(.28f,-.28f,.7f),world=Eyes.TransformVector(target);
            float available=FreeDistance(Eyes.position,world.normalized,world.magnitude);
            target*=Mathf.Max(.1f,available/world.magnitude);
            held.transform.localPosition=Vector3.Lerp(held.transform.localPosition,target,1-Mathf.Exp(-Time.deltaTime*15));
            held.transform.localRotation=Quaternion.Slerp(held.transform.localRotation,Quaternion.Euler(5,-12,-8),1-Mathf.Exp(-Time.deltaTime*15));
        }
        private void ShiftLoose(Vector3 shift){if(exteriorLooseRoot!=null)exteriorLooseRoot.position+=shift;}
    }
}
