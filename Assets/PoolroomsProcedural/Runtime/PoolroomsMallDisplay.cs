using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Merchandise is a combined mesh until picked up: no Rigidbody, Collider or Update per product.
    [DisallowMultipleComponent]
    public sealed class PoolroomsMallDisplay : MonoBehaviour
    {
        private MallLooseProp[] slots;
        private bool[] removed;
        private MallPlan plan;
        private float baseY;
        private PoolroomsProfile profile;
        private PoolroomsChunk owner;
        private PoolroomsCarry carry;
        private GameObject batch;
        public int DisplayedCount { get; private set; }
        public void Initialize(MallPlan mall,MallSectorGeometry sector,float y,PoolroomsProfile p,PoolroomsChunk chunk,PoolroomsCarry c)
        {plan=mall;slots=sector.Pickups.ToArray();removed=new bool[slots.Length];baseY=y;profile=p;owner=chunk;carry=c;Rebuild();}
        public bool Find(Vector3 position,Vector3 direction,float maximum,out int index,out float distance)
        {
            index=-1;distance=maximum;
            if(!isActiveAndEnabled||slots==null||carry==null||carry.Held!=null)return false;
            Vector3 local=transform.InverseTransformPoint(position),dir=transform.InverseTransformDirection(direction);
            V3 origin=new V3(local.x,local.y,local.z),forward=new V3(dir.x,dir.y,dir.z);
            for(int i=0;i<slots.Length;i++)
            {
                if(removed[i]||carry.WasTaken(MallMerchandise.Key(plan,slots[i])))continue;
                V3 center=slots[i].Position-new V3(0,baseY,0);float t;
                if(MallMerchandise.RayBox(origin,forward,center,MallItemCatalog.Get(slots[i].ItemId).Size,distance,out t))
                {index=i;distance=t;}
            }
            return index>=0;
        }
        public bool Take(int index)
        {
            if(index<0||index>=slots.Length||removed[index]||carry==null||carry.Held!=null)return false;
            MallLooseProp slot=slots[index];string key=MallMerchandise.Key(plan,slot);
            if(carry.WasTaken(key))return false;
            var pickup=PoolroomsPickup.CreateCatalog(key,slot.ItemId,PoolroomsMallSystem.ToUnity(slot.Position)-Vector3.up*baseY,transform,profile);
            pickup.transform.localRotation=Quaternion.Euler(0,slot.Yaw*Mathf.Rad2Deg,0);
            if(!carry.Grab(pickup)){Destroy(pickup.gameObject);return false;}
            removed[index]=true;Rebuild();return true;
        }
        private void Rebuild()
        {
            if(batch!=null)
            {
                // Remove the old mesh from the chunk's ownership list to keep repeated picks bounded.
                var filter=batch.GetComponent<MeshFilter>();var mesh=filter!=null?filter.sharedMesh:null;
                batch.SetActive(false);Destroy(batch);
                if(mesh!=null){owner.ownedMeshes.Remove(mesh);Destroy(mesh);}batch=null;
            }
            int count;
            var m=MallDisplayMesh.Build(slots,baseY,i=>!removed[i]&&(carry==null||!carry.WasTaken(MallMerchandise.Key(plan,slots[i]))),out count);
            DisplayedCount=count;
            batch=MeshFactory.Create("Articulos · E tomar / Q soltar",m,transform,profile,owner,false);
        }
    }
}
