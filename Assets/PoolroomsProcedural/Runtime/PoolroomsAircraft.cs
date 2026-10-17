using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsAircraft : PoolroomsFlyingVehicle
    {
        private PhysicsMaterial landingContact;
        private float propellerAngle;
        private readonly AircraftDynamics flight=new AircraftDynamics();
        private readonly List<Transform> propellers=new List<Transform>();
        public static PoolroomsAircraft Create(PoolroomsWorld world,Transform parent,Vector3 bottom,string key)
        {
            var go=City11Sources.Create("A_CesiumAir",parent,bottom,12,out var size);if(go==null)return null;
            go.name="Avión civil · E pilotar / salir · W/S potencia · A/D giro · Espacio/Ctrl altura";
            var aircraft=go.AddComponent<PoolroomsAircraft>();aircraft.world=world;aircraft.spawnKey=key;
            var source=go.transform.GetChild(0);
            // Use imported native part positions, so importer handedness cannot reverse
            // the nose. Both propeller discs lie ahead of the model's center.
            Vector3 propCenter=Vector3.zero;int found=0;
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(filter.name.Contains("_Prop")&&filter.sharedMesh!=null)
            {propCenter+=City11Sources.ModelPoint(filter.transform,go.transform,filter.sharedMesh.bounds.center);found++;}
            if(found!=4){Destroy(go);Debug.LogError("Aircraft requires two propellers, each with its opaque hub and transparent disc.");return null;}
            propCenter/=found;propCenter.y=0;
            if(propCenter.sqrMagnitude<.01f){Destroy(go);return null;}
            source.localRotation=Quaternion.FromToRotation(propCenter.normalized,Vector3.forward)*source.localRotation;
            if(!City11Sources.TryLocalBounds(go.transform,out var bounds)){Destroy(go);return null;}
            source.localPosition-=bounds.center;size=bounds.size;aircraft.size=size;
            go.transform.localPosition=bottom+Vector3.up*(size.y*.5f+.08f);
            for(int engine=0;engine<2;engine++)
            {
                var pieces=new List<MeshFilter>();Vector3 center=Vector3.zero;
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(filter.name.Contains("_Prop")&&filter.sharedMesh!=null&&filter.name.Contains("Prop__2_")==(engine==1))
                {pieces.Add(filter);center+=City11Sources.ModelPoint(filter.transform,go.transform,filter.sharedMesh.bounds.center);}
                if(pieces.Count!=2){Destroy(go);return null;}center/=pieces.Count;
                var pivot=new GameObject("Engine propeller pivot");pivot.transform.SetParent(go.transform,false);pivot.transform.localPosition=center;
                foreach(var filter in pieces)
                {filter.transform.SetParent(pivot.transform,true);var renderer=filter.GetComponent<Renderer>();if(renderer!=null&&filter.name.Contains("_m1"))renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
                aircraft.propellers.Add(pivot.transform);
            }
            // Compound fuselage/wing volumes and three landing contacts, with no per-face physics.
            AddBox(go,new Vector3(size.x*.22f,size.y*.62f,size.z*.88f),new Vector3(0,0,0));
            AddBox(go,new Vector3(size.x*.96f,.1f,size.z*.17f),new Vector3(0,size.y*.1f,0));
            AddBox(go,new Vector3(size.x*.38f,.1f,size.z*.15f),new Vector3(0,size.y*.13f,-size.z*.38f));
            aircraft.landingContact=new PhysicsMaterial("Aircraft rolling contacts"){staticFriction=.03f,dynamicFriction=.02f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            foreach(var p in new[]{new Vector3(-size.x*.11f,-size.y*.5f+.16f,-size.z*.05f),new Vector3(size.x*.11f,-size.y*.5f+.16f,-size.z*.05f),new Vector3(0,-size.y*.5f+.16f,size.z*.32f)})
            {var contact=go.AddComponent<SphereCollider>();contact.center=p;contact.radius=.16f;contact.sharedMaterial=aircraft.landingContact;}
            aircraft.body=go.AddComponent<Rigidbody>();aircraft.body.mass=2800;aircraft.body.centerOfMass=Vector3.down*.15f;aircraft.body.useGravity=true;
            aircraft.body.linearDamping=.01f;aircraft.body.angularDamping=2;aircraft.body.solverIterations=10;aircraft.body.solverVelocityIterations=4;aircraft.body.interpolation=RigidbodyInterpolation.Interpolate;aircraft.body.maxDepenetrationVelocity=3;aircraft.body.isKinematic=true;
            return aircraft;
        }
        static void AddBox(GameObject go,Vector3 size,Vector3 center){var collider=go.AddComponent<BoxCollider>();collider.size=size;collider.center=center;}
        protected override AircraftForces FlightForces(float dt,float speed,float verticalSpeed,float altitude)
        {return flight.Step(dt,speed,verticalSpeed,altitude,throttle,turn,climb,airbrake);}
        protected override void ResetFlight(){flight.Reset();}
        protected override void AnimateRotors(float dt)
        {propellerAngle=(propellerAngle+(500+flight.Throttle*1600)*6*dt)%360;foreach(var pivot in propellers)pivot.localRotation=Quaternion.Euler(0,0,propellerAngle);}
        protected override void OnDestroy(){if(landingContact!=null)Destroy(landingContact);base.OnDestroy();}
    }
}
