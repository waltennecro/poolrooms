using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsPickup : MonoBehaviour
    {
        public string SpawnKey { get; private set; }
        private Collider shape;
        private Rigidbody body;
        private PoolroomsSoundscape soundscape;
        private float nextAudioImpact;
        private float mass=.08f;
        private PickupSurface surface=PickupSurface.Packaging;
        private PhysicsMaterial contactMaterial;
        private bool streamingFrozen,wasSleeping;
        private Vector3 streamingVelocity,streamingAngularVelocity;
        internal Vector3 StreamingVelocity {get{return streamingFrozen?streamingVelocity:body!=null?body.linearVelocity:Vector3.zero;}}
        public int CatalogId { get; private set; }=-1;
        public float CarryRadius { get; private set; }=.27f;
        public static PoolroomsPickup CreateSourceSeat(string key,SourceFurniturePlacement seat,Transform parent,float offsetY=0)
        {
            var p=seat.Bottom;var item=CreateSource(key,SourceFurniture.Id(seat.Source),new Vector3(p.X,p.Y+offsetY,p.Z),parent,SourceFurniture.Longest(seat.Source));
            if(item!=null)item.transform.localRotation=Quaternion.Euler(0,seat.Yaw,0);return item;
        }
        public static void CreateMallSeats(MallPlan plan,RoomGeometry data,float baseY,Transform parent,PoolroomsCarry carry)
        {
            foreach(var seat in data.SourceSeats)
            {
                string key="mall:"+plan.Seed+":"+plan.AddressX+":"+plan.AddressZ+":source-seat:"+seat.Slot;
                if(carry==null||!carry.WasTaken(key))CreateSourceSeat(key,seat,parent,-baseY);
            }
        }
        public static PoolroomsPickup CreateSource(string key,string source,Vector3 bottom,Transform parent,float longest)
        {
            var world=parent!=null?parent.GetComponentInParent<PoolroomsWorld>():null;
            var settings=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;
            bool low=settings!=null&&settings.Preferences!=null&&settings.Preferences.graphicsQuality==0;
            var go=City11Sources.Create(source,parent,bottom,longest,out var size,low?1:0);if(go==null)return null;
            go.transform.localPosition=bottom+Vector3.up*(size.y*.5f);
            var collider=go.AddComponent<BoxCollider>();collider.size=size;
            var item=go.AddComponent<PoolroomsPickup>();item.shape=collider;item.SpawnKey=key;item.surface=PickupSurface.Rigid;
            item.mass=source=="WaterBottle"?.45f:source=="I_GlamVelvetSofa"||source=="U_SheenWoodLeatherSofa"?24:source=="SheenChair"||source=="I_ChairDamaskPurplegold"?6.5f:source=="U_AntiqueCamera"?9:source=="B_CafeTable"?12:source=="B_CafeChair"?4:1.8f;item.CarryRadius=size.magnitude*.5f+.015f;return item;
        }
        public static PoolroomsPickup Create(string key,Vector3 position,Transform parent,PoolroomsProfile profile)
        {
            var go=new GameObject("Bolsa de papas · E agarrar / Q soltar");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            // The object owns its meshes/materials, so carrying it survives sector unloading.
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            var packaging=MallGlyphs.SnackBag();packaging.EmissionStrength=0;packaging.EmissionTint=new V3(.95f,.9f,.76f);
            MeshFactory.Create("Snack packaging",packaging,go.transform,profile,owner,false);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;
            var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(.33f,.4f,.16f);item.shape=collider;
            return item;
        }
        public static PoolroomsPickup CreateCatalog(string key,int catalogId,Vector3 position,Transform parent,PoolroomsProfile profile)
        {
            var definition=MallItemCatalog.Get(catalogId);
            var go=new GameObject(definition.Name+" · E tomar / Q soltar");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            PoolroomsPickupVisualCache.Create(catalogId,go.transform,profile);
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.CatalogId=catalogId;item.mass=definition.Mass;item.CarryRadius=definition.Radius;
            item.surface=PickupSurface.Rigid;
            var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(definition.Size.X,definition.Size.Y,definition.Size.Z);item.shape=collider;
            return item;
        }
        public static PoolroomsPickup CreatePlayProp(string key,int kind,int colour,Vector3 position,Transform parent,PoolroomsProfile profile)
        {
            string name=kind==0?"Pelota blanda":kind==1?"Bloque de espuma":"Aro acolchado";
            var go=new GameObject(name+" · E tomar / Q soltar");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            MeshFactory.Create("Juguete",SoftPlayArchitecture.Prop(kind,colour),go.transform,profile,owner,false);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.mass=.08f;item.CarryRadius=.235f;
            item.surface=kind==0?PickupSurface.SoftBall:kind==1?PickupSurface.Foam:PickupSurface.Rubber;
            if(kind==0){var collider=go.AddComponent<SphereCollider>();collider.radius=.22f;item.shape=collider;}
            else {var collider=go.AddComponent<BoxCollider>();collider.size=kind==1?Vector3.one*.38f:new Vector3(.46f,.46f,.12f);item.shape=collider;}
            // No Rigidbody simulation until actually released by the player.
            return item;
        }
        public void Attach(Transform grip)
        {
            soundscape=grip.GetComponentInParent<PoolroomsSoundscape>();
            if(soundscape!=null)soundscape.Handling(transform.position,surface!=PickupSurface.Rigid);
            if(shape==null)shape=GetComponent<Collider>();shape.enabled=false;
            streamingFrozen=false;
            if(body!=null){if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.collisionDetectionMode=CollisionDetectionMode.Discrete;body.isKinematic=true;body.detectCollisions=false;}
            transform.SetParent(grip,true);transform.localPosition=new Vector3(.28f,-.28f,.7f);transform.localRotation=Quaternion.Euler(5,-12,-8);
        }
        public void Release(Transform parent,Vector3 position)
        {Release(parent,position,Vector3.zero);}
        public void Release(Transform parent,Vector3 position,Vector3 playerVelocity)
        {
            if(shape==null)shape=GetComponent<Collider>();
            transform.SetParent(parent,true);transform.position=position;shape.enabled=true;
            var tuning=new PickupPhysics(surface);
            if(contactMaterial==null)
            {
                contactMaterial=new PhysicsMaterial("Pickup contact / "+surface){dynamicFriction=tuning.Friction,staticFriction=tuning.Friction+.05f,bounciness=tuning.Restitution,
                    frictionCombine=PhysicsMaterialCombine.Average,bounceCombine=PhysicsMaterialCombine.Average};
                shape.sharedMaterial=contactMaterial;
            }
            if(body==null)body=gameObject.AddComponent<Rigidbody>();body.mass=GamePreferences.Clamp(mass,.02f,80,.08f);body.linearDamping=tuning.AirDamping;body.angularDamping=tuning.AngularDamping;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.solverIterations=8;body.solverVelocityIterations=2;body.maxDepenetrationVelocity=3;body.maxAngularVelocity=25;
            streamingFrozen=false;body.detectCollisions=true;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var velocity=PickupPhysics.ReleaseVelocity(new V3(playerVelocity.x,playerVelocity.y,playerVelocity.z));
            body.linearVelocity=new Vector3(velocity.X,velocity.Y,velocity.Z);body.angularVelocity=Vector3.zero;body.WakeUp();
        }
        internal void SetStreamingSupport(bool supported)
        {
            if(body==null)return;
            if(!supported&&!streamingFrozen&&!body.isKinematic)
            {
                wasSleeping=body.IsSleeping();streamingVelocity=body.linearVelocity;streamingAngularVelocity=body.angularVelocity;
                body.linearVelocity=body.angularVelocity=Vector3.zero;body.collisionDetectionMode=CollisionDetectionMode.Discrete;body.isKinematic=true;streamingFrozen=true;
            }
            else if(supported&&streamingFrozen)
            {
                body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                body.linearVelocity=streamingVelocity;body.angularVelocity=streamingAngularVelocity;streamingFrozen=false;
                if(wasSleeping)body.Sleep();else body.WakeUp();
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if(soundscape==null||body==null||body.isKinematic||streamingFrozen||Time.time<nextAudioImpact||collision.contactCount==0)return;
            var other=collision.collider.GetComponentInParent<PoolroomsPickup>();
            if(other!=null&&other.soundscape!=null&&GetInstanceID()>other.GetInstanceID())return;
            nextAudioImpact=Time.time+.16f;
            soundscape.Impact(collision.GetContact(0).point,collision.relativeVelocity.magnitude,mass,surface);
        }
        private void OnDestroy(){if(contactMaterial!=null){if(Application.isPlaying)Destroy(contactMaterial);else DestroyImmediate(contactMaterial);}}
    }
}
