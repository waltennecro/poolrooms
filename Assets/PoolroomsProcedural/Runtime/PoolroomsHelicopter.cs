using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsHelicopter : PoolroomsFlyingVehicle
    {
        private readonly HelicopterDynamics flight=new HelicopterDynamics();
        private Transform mainRotor,tailRotor;
        private float rotorAngle,tailAngle;
        private PhysicsMaterial skidContact;
        public static PoolroomsHelicopter Create(PoolroomsWorld world,Transform parent,Vector3 bottom,string key)
        {
            var settings=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;
            bool low=settings!=null&&settings.Preferences!=null&&settings.Preferences.graphicsQuality==0;
            var go=City11Sources.Create("H_MH6",parent,bottom,9.4f,out var size,low?1:0);if(go==null)return null;
            go.name="Helicóptero · E pilotar / salir · WASD desplazar / girar · Espacio/Ctrl altura";
            var heli=go.AddComponent<PoolroomsHelicopter>();heli.world=world;heli.spawnKey=key;
            // Use all imported native LODs and actual tail geometry to determine its
            // nose; do not assume the model importer's handedness.
            if(!Marker(go.transform,"main_pivot",out var main)||!Marker(go.transform,"nose_axis",out var nose)){Destroy(go);return null;}
            Vector3 direction=nose-main;direction.y=0;if(direction.sqrMagnitude<.01f){Destroy(go);return null;}
            Quaternion turn=Quaternion.FromToRotation(direction.normalized,Vector3.forward);
            foreach(Transform tier in go.transform)tier.localRotation=turn*tier.localRotation;
            if(!City11Sources.TryLocalBounds(go.transform,out var bounds)){Destroy(go);return null;}
            foreach(Transform tier in go.transform)tier.localPosition-=bounds.center;
            size=bounds.size;heli.size=size;go.transform.localPosition=bottom+Vector3.up*(size.y*.5f+.04f);
            heli.mainRotor=Pivot(go.transform,"main_rotor","Main rotor pivot");heli.tailRotor=Pivot(go.transform,"tail_rotor","Tail rotor pivot");
            if(heli.mainRotor==null||heli.tailRotor==null){Destroy(go);return null;}
            // Rotor blades are visual only. The cabin, tail and landing skids collide;
            // there is no rotor damage or combat interaction.
            var cabin=go.AddComponent<BoxCollider>();cabin.size=new Vector3(size.x*.24f,size.y*.5f,size.z*.43f);cabin.center=new Vector3(0,-size.y*.1f,size.z*.24f);
            var tailShape=go.AddComponent<CapsuleCollider>();tailShape.direction=2;tailShape.radius=.18f;tailShape.height=size.z*.52f;tailShape.center=new Vector3(0,size.y*.08f,-size.z*.22f);
            heli.skidContact=new PhysicsMaterial("Helicopter skid contacts"){staticFriction=.55f,dynamicFriction=.4f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Average,bounceCombine=PhysicsMaterialCombine.Minimum};
            foreach(int side in new[]{-1,1})
            {var skid=go.AddComponent<CapsuleCollider>();skid.direction=2;skid.radius=.09f;skid.height=size.z*.43f;skid.center=new Vector3(side*size.x*.13f,-size.y*.5f+.09f,size.z*.19f);skid.sharedMaterial=heli.skidContact;}
            heli.body=go.AddComponent<Rigidbody>();heli.body.mass=1350;heli.body.useGravity=true;heli.body.centerOfMass=new Vector3(0,-.2f,size.z*.16f);
            heli.body.linearDamping=.02f;heli.body.angularDamping=2;heli.body.solverIterations=10;heli.body.solverVelocityIterations=4;heli.body.interpolation=RigidbodyInterpolation.Interpolate;heli.body.maxDepenetrationVelocity=3;heli.body.isKinematic=true;
            var lod=go.GetComponent<LODGroup>();
            if(lod!=null){var tiers=lod.GetLODs();for(int i=0;i<tiers.Length;i++)tiers[i].renderers=System.Array.FindAll(tiers[i].renderers,r=>r!=null&&!IsMarker(r.name));lod.SetLODs(tiers);lod.RecalculateBounds();}
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(IsMarker(filter.name)){var renderer=filter.GetComponent<Renderer>();if(renderer!=null)renderer.enabled=false;Destroy(filter.gameObject);}
            return heli;
        }
        static Transform Pivot(Transform root,string part,string label)
        {
            var meshes=new List<MeshFilter>();Vector3 center=Vector3.zero;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null&&filter.name.Contains(part))
            {meshes.Add(filter);center+=City11Sources.ModelPoint(filter.transform,root,filter.sharedMesh.bounds.center);}
            if(meshes.Count==0||!Marker(root,part=="main_rotor"?"main_pivot":"tail_pivot",out center))return null;
            var pivot=new GameObject(label).transform;pivot.SetParent(root,false);pivot.localPosition=center;
            foreach(var filter in meshes)filter.transform.SetParent(pivot,true);return pivot;
        }
        static bool IsMarker(string name){return name.Contains("_pivot")||name.Contains("nose_axis");}
        static bool Marker(Transform root,string part,out Vector3 point)
        {
            point=Vector3.zero;int count=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null&&filter.name.Contains(part))
            {point+=City11Sources.ModelPoint(filter.transform,root,filter.sharedMesh.bounds.center);count++;}
            if(count==0)return false;point/=count;return true;
        }
        protected override AircraftForces FlightForces(float dt,float speed,float verticalSpeed,float altitude)
        {return flight.Step(dt,speed,verticalSpeed,altitude,throttle,turn,climb,airbrake);}
        protected override void ResetFlight(){flight.Reset();}
        protected override void AnimateRotors(float dt)
        {
            rotorAngle=(rotorAngle+flight.Rotor*480*6*dt)%360;tailAngle=(tailAngle+flight.Rotor*2700*6*dt)%360;
            mainRotor.localRotation=Quaternion.Euler(0,rotorAngle,0);tailRotor.localRotation=Quaternion.Euler(tailAngle,0,0);
        }
        protected override void OnDestroy(){if(skidContact!=null)Destroy(skidContact);base.OnDestroy();}
    }
}
