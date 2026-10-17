using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Assisted twin-prop aircraft. No damage, death, fuel or mandatory objectives.
    // Native import, PhysX and piloting tests must still be executed in Unity.
    [DisallowMultipleComponent]
    public sealed class PoolroomsAircraft : PoolroomsVehicle
    {
        private PoolroomsWorld world;
        private PoolroomsExplorer driver,originActor;
        private Rigidbody body;
        private PhysicsMaterial landingContact;
        private string spawnKey;
        private Vector3 size,savedVelocity,savedAngular;
        private float turn,throttle,climb,yaw,lastYaw,propellerAngle;
        private bool airbrake,detached,frozen;
        private readonly AircraftDynamics flight=new AircraftDynamics();
        private readonly List<Transform> propellers=new List<Transform>();
        private readonly RaycastHit[] hits=new RaycastHit[32];
        public override bool IsDriving {get{return driver!=null;}}
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
        public override bool Enter(PoolroomsExplorer actor)
        {
            if(actor==null||actor.Vehicle!=null||actor.IsFlying||actor.IsPaused||actor.InputCaptured||driver!=null||world==null||!world.ViewerReady)return false;
            if(!actor.BeginDriving(this))return false;
            driver=actor;flight.Reset();yaw=lastYaw=transform.eulerAngles.y;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            transform.SetParent(world.GeneratedRoot,true);detached=true;
            if(originActor==null){originActor=actor;originActor.OriginShifted+=ShiftDetached;}
            var carry=actor.GetComponent<PoolroomsCarry>();if(carry!=null)carry.MarkTaken(spawnKey);
            UpdateDriverPose();return true;
        }
        public override void Control(float horizontal,float vertical,bool brake,float lift)
        {turn=Mathf.Clamp(horizontal,-1,1);throttle=Mathf.Clamp(vertical,-1,1);climb=Mathf.Clamp(lift,-1,1);airbrake=brake;}
        void FixedUpdate()
        {
            if(driver==null)return;
            Vector3 velocity=frozen?savedVelocity:body.linearVelocity;
            bool unavailable=driver.IsPaused||driver.InputCaptured||world==null||!world.ViewerReady||!world.IsPositionStreamed(body.position+Vector3.ClampMagnitude(velocity*.25f,world.profile.cellSize*.6f));
            if(unavailable){Freeze();return;}Unfreeze();
            Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            float speed=Vector3.Dot(body.linearVelocity,forward),altitude=body.position.y-world.transform.position.y;
            var forces=flight.Step(Time.fixedDeltaTime,speed,body.linearVelocity.y,altitude,throttle,turn,climb,airbrake);
            Vector3 lateral=body.linearVelocity-forward*speed-Vector3.up*body.linearVelocity.y;
            body.AddForce(forward*forces.Forward+Vector3.up*forces.Vertical-lateral*1.8f,ForceMode.Acceleration);
            yaw+=forces.YawRate*Time.fixedDeltaTime;
            Quaternion orientation=Quaternion.Euler(-forces.Pitch,yaw,forces.Roll);
            body.MoveRotation(Quaternion.Slerp(body.rotation,orientation,1-Mathf.Exp(-Time.fixedDeltaTime*2.2f)));
        }
        void Freeze()
        {if(frozen)return;savedVelocity=body.linearVelocity;savedAngular=body.angularVelocity;body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;frozen=true;}
        void Unfreeze()
        {if(!frozen)return;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=savedVelocity;body.angularVelocity=savedAngular;frozen=false;}
        void LateUpdate()
        {
            if(driver!=null)
            {
                UpdateDriverPose();
                if(!driver.IsPaused&&!driver.InputCaptured&&!frozen)
                {propellerAngle=(propellerAngle+(500+flight.Throttle*1600)*6*Time.deltaTime)%360;foreach(var pivot in propellers)pivot.localRotation=Quaternion.Euler(0,0,propellerAngle);}
            }
            else if(detached&&world!=null&&world.viewer!=null&&Vector3.Distance(world.viewer.position,transform.position)>world.UnloadRadius*world.profile.cellSize+48)Destroy(gameObject);
        }
        void UpdateDriverPose()
        {
            if(driver==null)return;
            float current=transform.eulerAngles.y;driver.transform.Rotate(0,Mathf.DeltaAngle(lastYaw,current),0,Space.World);lastYaw=current;
            float eyeHeight=driver.eyes!=null?driver.eyes.localPosition.y:1.65f;
            // The source has an exterior, not a modeled cockpit. A chase view keeps
            // the camera outside the opaque fuselage and does not invent an interior.
            Vector3 anchor=transform.TransformPoint(new Vector3(0,size.y*.55f,0)),eye=transform.TransformPoint(new Vector3(0,size.y*.8f,-size.z*.8f));
            Vector3 direction=eye-anchor;float distance=direction.magnitude;
            int count=Physics.RaycastNonAlloc(anchor,direction.normalized,hits,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(transform)&&!hits[i].transform.IsChildOf(driver.transform)&&hits[i].distance<distance){distance=hits[i].distance;eye=hits[i].point+hits[i].normal*.35f;}
            driver.transform.position=eye-Vector3.up*eyeHeight;
        }
        public override bool TryExit()
        {
            if(driver==null||frozen||body.linearVelocity.sqrMagnitude>1)return false;
            int underneath=Physics.RaycastNonAlloc(body.position,Vector3.down,hits,size.y*.5f+.3f,~0,QueryTriggerInteraction.Ignore);bool landed=false;
            if(underneath==hits.Length)return false;
            for(int i=0;i<underneath;i++)if(hits[i].rigidbody==null&&!hits[i].transform.IsChildOf(transform)&&hits[i].normal.y>.65f)landed=true;
            if(!landed)return false;
            var capsule=driver.GetComponent<CharacterController>();float radius=capsule.radius,height=capsule.height;
            foreach(var local in new[]{new Vector3(-size.x*.5f-1,0,0),new Vector3(size.x*.5f+1,0,0),new Vector3(0,0,size.z*.5f+1)})
            {
                Vector3 candidate=transform.TransformPoint(local)+Vector3.up*1.5f;int count=Physics.RaycastNonAlloc(candidate,Vector3.down,hits,4.5f,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)continue;float best=float.PositiveInfinity;Vector3 ground=Vector3.zero;
                for(int i=0;i<count;i++)if(hits[i].rigidbody==null&&!hits[i].transform.IsChildOf(transform)&&hits[i].normal.y>.65f&&hits[i].distance<best){ground=hits[i].point;best=hits[i].distance;}
                if(float.IsPositiveInfinity(best))continue;Vector3 feet=ground+Vector3.up*.04f;
                if(!world.IsPositionStreamed(feet)||Physics.CheckCapsule(feet+Vector3.up*radius,feet+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore))continue;
                var actor=driver;driver=null;body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;flight.Reset();actor.EndDriving(this,feet);return true;
            }
            return false;
        }
        public override void DetachDriver(PoolroomsExplorer actor)
        {
            if(driver!=actor)return;driver=null;frozen=false;
            if(!body.isKinematic){body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;}flight.Reset();
        }
        void ShiftDetached(Vector3 shift){if(!detached||body==null)return;Vector3 position=body.position+shift;body.position=position;transform.position=position;}
        void OnDestroy()
        {if(landingContact!=null)Destroy(landingContact);if(originActor!=null)originActor.OriginShifted-=ShiftDetached;if(driver!=null){var actor=driver;driver=null;actor.EndDriving(this,actor.transform.position+Vector3.up*2);}}
    }
}
