using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsPickup : MonoBehaviour
    {
        public string SpawnKey { get; private set; }
        public string InventoryKey {get;private set;}
        public void AssignInventoryKey(string key){if(ExplorationItems.Resolve(key)!=null)InventoryKey=key;}
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
        public bool IsProvision {get{return InventoryKey!=null&&InventoryKey.StartsWith("food:",System.StringComparison.Ordinal);}}
        public static PoolroomsPickup CreateLevel10Item(int id,string key,Vector3 position,PoolroomsProfile profile)
        {
            var definition=Level10Activities.Item(id);if(definition==null)return null;
            var go=new GameObject(definition.Name);go.transform.position=position;var owner=go.AddComponent<PoolroomsChunk>();
            var visual=MeshFactory.Create("Level10 keepsake",Level10Equipment.Keepsake(id),go.transform,profile,owner,false);
            if(visual!=null)visual.GetComponent<Renderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            V3 size=id==0?new V3(.32f,.4f,.32f):id==1?new V3(.20f,.04f,.26f):new V3(.30f,.04f,.22f);
            var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(size.X,size.Y,size.Z);collider.center=Vector3.up*size.Y*.5f;
            var pickup=go.AddComponent<PoolroomsPickup>();pickup.SpawnKey=key;pickup.InventoryKey="level10:"+id;pickup.shape=collider;
            pickup.mass=.025f;pickup.CarryRadius=size.Length*.5f+.015f;pickup.surface=PickupSurface.Rigid;return pickup;
        }
        public static PoolroomsPickup CreateForestItem(int id, string key, Vector3 position, PoolroomsProfile profile)
        {
            if (Forest39Activities.Item(id) == null) return null;
            var kind = id == 0 ? Forest39Activity.FallenLeaf : id == 1 ? Forest39Activity.Acorn : Forest39Activity.FieldNotebook;
            var go = new GameObject(Forest39Activities.Item(id).Name);
            go.transform.position = position;
            var owner = go.AddComponent<PoolroomsChunk>();
            MeshFactory.Create("Forest keepsake", Forest39ActivityGeometry.Keepsake(id), go.transform, profile, owner, false);
            var size = id==2?new V3(.32f,.035f,.24f):Forest39ActivityGeometry.Size(kind);
            var shape = go.AddComponent<BoxCollider>();
            shape.size = new Vector3(size.X,size.Y,size.Z);
            shape.center = Vector3.up * size.Y * .5f;
            var pickup = go.AddComponent<PoolroomsPickup>();
            pickup.SpawnKey = key;
            pickup.InventoryKey = "forest39:" + id;
            pickup.shape = shape;
            pickup.mass = id == 0 ? .005f : .02f;
            pickup.CarryRadius = size.Length * .5f + .015f;
            pickup.surface = PickupSurface.Rigid;
            return pickup;
        }

        public static PoolroomsPickup CreateFood(string key,int id,Vector3 bottom,Transform parent,PoolroomsProfile profile)
        {
            var definition=ProvisionCatalog.Get(id);
            var item=CreateSource(key,definition.Source,bottom,parent,definition.Longest);
            if(item==null)return null;
            item.name=definition.Name;item.InventoryKey=definition.Key;item.mass=definition.Drink?.35f:.18f;
            item.surface=definition.Drink?PickupSurface.Packaging:PickupSurface.Foam;
            if(id==ProvisionCatalog.AlmondWater)
            {
                var owner=item.gameObject.AddComponent<PoolroomsChunk>();var mesh=new MeshData();var s=definition.Size;
                float w=s.X*.34f,h=s.Y*.22f,y=-s.Y*.04f,z=-s.Z*.5f-.0008f;
                var normal=new V3(0,0,-1);
                mesh.QuadUV(new V3(-w,y-h,z),new V3(-w,y+h,z),new V3(w,y+h,z),new V3(w,y-h,z),normal,normal,normal,normal,new V2(0,0),new V2(0,1),new V2(1,1),new V2(1,0),Surface.Tile);
                var mat=City11Sources.MaterialFor(new City11Sources.MaterialDefinition{name="Food_almond_label",albedo="FoodSources/Textures/almond_label_albedo",normal="FoodSources/Textures/drink_normal",mask="FoodSources/Textures/drink_mask",colour=new[]{1f,1f,1f,1f},roughness=.6f});
                var visual=MeshFactory.Create("Agua de almendra · etiqueta original",mesh,item.transform,profile,owner,false,materialOverrides:new[]{mat,mat,mat,mat,mat,mat});
                visual.GetComponent<MeshRenderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            }
            return item;
        }
        public static PoolroomsPickup CreateTool(string key,int id,Vector3 position,PoolroomsProfile profile)
        {
            if(!EncounterTools.Available(id))return null;
            var go=new GameObject(ExplorationItems.Tool(id).Name);go.transform.position=position;
            var owner=go.AddComponent<PoolroomsChunk>();var visual=MeshFactory.Create("Detailed travel tool",EncounterTools.Build(id),go.transform,profile,owner,false);
            visual.GetComponent<MeshRenderer>().rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            var size=EncounterTools.Size(id);var shape=go.AddComponent<BoxCollider>();shape.size=new Vector3(size.X,size.Y,size.Z);shape.center=Vector3.up*size.Y*.5f+(id==2?Vector3.right*.045f:Vector3.zero);
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.InventoryKey="tool:"+id;item.shape=shape;item.mass=id==2?.5f:.05f;item.CarryRadius=size.Length*.5f+.015f;item.surface=PickupSurface.Rigid;return item;
        }
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
            var item=go.AddComponent<PoolroomsPickup>();item.shape=collider;item.SpawnKey=key;item.InventoryKey="source:"+source;item.surface=PickupSurface.Rigid;
            PoolroomsInteractable.BindSource(item,source);
            item.mass=source=="J_Radiator"?22:source=="J_CoatRackA"?4:source=="J_CoatRackB"?.4f:source=="J_Basket"?.2f:source=="J_PlateA"||source=="J_PlateB"?.3f:source=="J_NapkinPile"?.12f:source=="J_PlantPot"?2.8f:source=="M_Refrigerator"?65:source=="M_Pouf"?4:source=="M_Shoe"?.35f:source=="M_Mandarin"?.09f:source=="M_Avocado"?.18f:source=="M_OliveDish"?.45f:source=="M_IndoorChair"?4:source=="M_IndoorTable"?18:source=="M_CashRegister"?5:source=="M_Stool"?6:source=="M_Napkins"?.12f:source=="M_MenuSign"?1.2f:source=="M_StainedLamp"?4.2f:source=="WaterBottle"?.45f:source=="I_GlamVelvetSofa"||source=="U_SheenWoodLeatherSofa"?24:source=="SheenChair"||source=="I_ChairDamaskPurplegold"?6.5f:source=="U_AntiqueCamera"?9:source=="B_CafeTable"?12:source=="B_CafeChair"?4:1.8f;item.CarryRadius=size.magnitude*.5f+.015f;return item;
        }
        public static PoolroomsPickup Create(string key,Vector3 position,Transform parent,PoolroomsProfile profile)
        {
            var go=new GameObject("Bolsa de papas · E agarrar / Q soltar");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            // The object owns its meshes/materials, so carrying it survives sector unloading.
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            var packaging=MallGlyphs.SnackBag();packaging.EmissionStrength=0;packaging.EmissionTint=new V3(.95f,.9f,.76f);
            MeshFactory.Create("Snack packaging",packaging,go.transform,profile,owner,false);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.InventoryKey="catalog:192";
            var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(.33f,.4f,.16f);item.shape=collider;
            return item;
        }
        public static PoolroomsPickup CreateCatalog(string key,int catalogId,Vector3 position,Transform parent,PoolroomsProfile profile)
        {
            var definition=MallItemCatalog.Get(catalogId);
            var go=new GameObject(definition.Name+" · E tomar / Q soltar");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            PoolroomsPickupVisualCache.Create(catalogId,go.transform,profile);
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.CatalogId=catalogId;item.InventoryKey="catalog:"+catalogId;item.mass=definition.Mass;item.CarryRadius=definition.Radius;
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
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.InventoryKey="play:"+kind+":"+colour;item.mass=.08f;item.CarryRadius=.235f;
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
