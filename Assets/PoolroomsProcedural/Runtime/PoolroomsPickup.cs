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
        private float mass=.08f;
        public int CatalogId { get; private set; }=-1;
        public float CarryRadius { get; private set; }=.27f;
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
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            MeshFactory.Create("Articulo",MallItemCatalog.Model(catalogId),go.transform,profile,owner,false);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())renderer.rayTracingMode=UnityEngine.Experimental.Rendering.RayTracingMode.DynamicTransform;
            var item=go.AddComponent<PoolroomsPickup>();item.SpawnKey=key;item.CatalogId=catalogId;item.mass=definition.Mass;item.CarryRadius=definition.Radius;
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
            if(kind==0){var collider=go.AddComponent<SphereCollider>();collider.radius=.22f;item.shape=collider;}
            else {var collider=go.AddComponent<BoxCollider>();collider.size=kind==1?Vector3.one*.38f:new Vector3(.46f,.46f,.12f);item.shape=collider;}
            // No Rigidbody simulation until actually released by the player.
            return item;
        }
        public void Attach(Transform grip)
        {
            if(shape==null)shape=GetComponent<Collider>();shape.enabled=false;
            if(body!=null){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;body.detectCollisions=false;}
            transform.SetParent(grip,true);transform.localPosition=new Vector3(.28f,-.28f,.7f);transform.localRotation=Quaternion.Euler(5,-12,-8);
        }
        public void Release(Transform parent,Vector3 position)
        {
            transform.SetParent(parent,true);transform.position=position;shape.enabled=true;
            if(body==null)body=gameObject.AddComponent<Rigidbody>();body.mass=mass;body.linearDamping=.35f;body.angularDamping=.6f;
            body.detectCollisions=true;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;
        }
    }
}
