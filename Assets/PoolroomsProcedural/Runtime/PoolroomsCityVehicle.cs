using System.Collections.Generic;
using UnityEngine;

namespace Poolrooms
{
    // Assisted, peaceful driving using the original licensed exterior model.
    [DisallowMultipleComponent]
    public sealed class PoolroomsCityVehicle : PoolroomsVehicle
    {
        private PoolroomsWorld world;
        private PoolroomsExplorer driver,originActor;
        private Rigidbody body;
        private WheelCollider[] wheels;
        private Transform[] wheelVisuals,caliperVisuals;
        private bool[] frontWheels;
        private string spawnKey;
        private Vector3 size,savedVelocity,savedAngular;
        private float steer,throttle,lastYaw;
        private bool brake,detached,exitRequested,frozen;
        private readonly RaycastHit[] hits=new RaycastHit[32];
        public override bool IsDriving {get{return driver!=null;}}
        public static PoolroomsCityVehicle Create(PoolroomsWorld world,Transform parent,Vector3 position,string key,string source="ToyCar")
        {
            var settings=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;
            bool low=settings!=null&&settings.Preferences!=null&&settings.Preferences.graphicsQuality==0;
            var go=City11Sources.Create(source,parent,position,source=="ToyCar"?4.2f:4.6f,out var size,low?1:0);if(go==null)return null;
            var definition=City11Sources.Get(source);
            if(definition.wheels==null||definition.wheels.Length!=4){Destroy(go);return null;}
            // Read the imported front/rear wheel centres to handle OBJ handedness.
            Vector3 front=Vector3.zero,rear=Vector3.zero;
            foreach(var part in definition.wheels)
            {if(!PartBounds(go.transform,part.part,out var bounds)){Destroy(go);return null;}if(part.front)front+=bounds.center;else rear+=bounds.center;}
            Vector3 direction=front-rear;direction.y=0;
            if(direction.sqrMagnitude<.01f){Destroy(go);return null;}
            Quaternion turn=Quaternion.FromToRotation(direction.normalized,Vector3.forward);
            foreach(Transform tier in go.transform)tier.localRotation=turn*tier.localRotation;
            if(!City11Sources.TryLocalBounds(go.transform,out var all)){Destroy(go);return null;}
            foreach(Transform tier in go.transform)tier.localPosition-=all.center;
            size=all.size;go.name="Coche · E conducir / salir · WASD conducir · Ctrl frenar";
            go.transform.localPosition=position+Vector3.up*(size.y*.5f+.03f);
            var car=go.AddComponent<PoolroomsCityVehicle>();car.world=world;car.spawnKey=key;car.size=size;
            var shape=go.AddComponent<BoxCollider>();shape.size=new Vector3(size.x*.92f,size.y*.65f,size.z*.86f);shape.center=Vector3.up*size.y*.08f;
            car.body=go.AddComponent<Rigidbody>();car.body.mass=1100;car.body.centerOfMass=Vector3.down*size.y*.18f;
            car.body.linearDamping=.025f;car.body.angularDamping=.8f;car.body.solverIterations=10;car.body.solverVelocityIterations=4;
            car.body.interpolation=RigidbodyInterpolation.Interpolate;car.body.maxDepenetrationVelocity=3;car.body.isKinematic=true;
            car.wheels=new WheelCollider[4];car.wheelVisuals=new Transform[4];car.caliperVisuals=new Transform[4];car.frontWheels=new bool[4];
            for(int i=0;i<4;i++)
            {
                var part=definition.wheels[i];PartBounds(go.transform,part.part,out var bounds);
                var pivot=new GameObject(part.part+" / suspension visual").transform;pivot.SetParent(go.transform,false);pivot.localPosition=bounds.center;
                var caliper=new GameObject(part.part+" / fixed caliper").transform;caliper.SetParent(go.transform,false);caliper.localPosition=bounds.center;car.caliperVisuals[i]=caliper;
                var filters=new List<MeshFilter>(go.GetComponentsInChildren<MeshFilter>());
                foreach(var filter in filters)if(filter.name.Contains(part.part))filter.transform.SetParent(filter.name.Contains("_Caliper")?caliper:pivot,true);
                car.wheelVisuals[i]=pivot;car.frontWheels[i]=part.front;
                var wheel=new GameObject(part.part+" / contact");wheel.transform.SetParent(go.transform,false);
                // WheelCollider's origin is the top of suspension travel. At the
                // half-travel rest position its centre matches the actual tyre.
                wheel.transform.localPosition=bounds.center+Vector3.up*.09f;
                var contact=wheel.AddComponent<WheelCollider>();contact.radius=Mathf.Max(bounds.extents.y,bounds.extents.z);
                contact.mass=30;contact.suspensionDistance=.18f;contact.forceAppPointDistance=.15f;
                var spring=contact.suspensionSpring;spring.spring=35000;spring.damper=4500;spring.targetPosition=.5f;contact.suspensionSpring=spring;
                contact.brakeTorque=1800;contact.ConfigureVehicleSubsteps(5,8,3);contact.enabled=false;car.wheels[i]=contact;
            }
            var lod=go.GetComponent<LODGroup>();if(lod!=null)
            {var tiers=lod.GetLODs();for(int i=0;i<tiers.Length;i++)tiers[i].screenRelativeTransitionHeight=i==0?.26f:i==1?.075f:.012f;lod.SetLODs(tiers);lod.RecalculateBounds();}return car;
        }
        static bool PartBounds(Transform root,string part,out Bounds bounds)
        {
            bounds=new Bounds();MeshFilter best=null;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null&&filter.name.Contains(part)&&
                !filter.name.Contains("_Caliper")&&(best==null||filter.sharedMesh.vertexCount>best.sharedMesh.vertexCount))best=filter;
            if(best==null)return false;
            var local=best.sharedMesh.bounds;
            for(int i=0;i<8;i++)
            {
                Vector3 p=City11Sources.ModelPoint(best.transform,root,local.center+Vector3.Scale(local.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                if(i==0)bounds=new Bounds(p,Vector3.zero);else bounds.Encapsulate(p);
            }
            return true;
        }
        public override bool Enter(PoolroomsExplorer actor)
        {
            if(driver!=null||actor==null||actor.IsFlying||actor.IsPaused||actor.InputCaptured||actor.Vehicle!=null||world==null||!world.ViewerReady)return false;
            if(!actor.BeginDriving(this))return false;
            driver=actor;lastYaw=transform.eulerAngles.y;foreach(var wheel in wheels)wheel.enabled=true;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            transform.SetParent(world.GeneratedRoot,true);detached=true;
            if(originActor==null){originActor=actor;originActor.OriginShifted+=ShiftDetached;}
            var carry=actor.GetComponent<PoolroomsCarry>();if(carry!=null)carry.MarkTaken(spawnKey);
            actor.transform.rotation=Quaternion.Euler(0,lastYaw,0);UpdateDriverPose();return true;
        }
        public override void Control(float horizontal,float vertical,bool handbrake,float climb)
        {steer=Mathf.Clamp(horizontal,-1,1);throttle=Mathf.Clamp(vertical,-1,1);brake=handbrake;}
        void FixedUpdate()
        {
            if(driver==null)return;
            if(driver.IsPaused||driver.InputCaptured||world==null||!world.ViewerReady||!SupportedAhead()){Freeze();return;}
            Unfreeze();float speed=body.linearVelocity.magnitude;
            for(int i=0;i<wheels.Length;i++)
            {
                wheels[i].steerAngle=frontWheels[i]&&!exitRequested?steer*Mathf.Lerp(28,10,Mathf.Clamp01(speed/18)):0;
                wheels[i].motorTorque=!exitRequested&&!brake&&speed<20?throttle*950:0;
                wheels[i].brakeTorque=exitRequested||brake?1800:Mathf.Abs(throttle)<.01f?80:0;
            }
        }
        void Freeze()
        {if(frozen)return;savedVelocity=body.linearVelocity;savedAngular=body.angularVelocity;body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;frozen=true;}
        void Unfreeze()
        {if(!frozen)return;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=savedVelocity;body.angularVelocity=savedAngular;frozen=false;}
        void LateUpdate()
        {
            if(driver!=null)
            {
                if(!frozen)for(int i=0;i<wheels.Length;i++)
                {wheels[i].GetWorldPose(out var position,out var rotation);wheelVisuals[i].SetPositionAndRotation(position,rotation);caliperVisuals[i].SetPositionAndRotation(position,transform.rotation*Quaternion.Euler(0,wheels[i].steerAngle,0));}
                UpdateDriverPose();if(exitRequested&&!frozen&&body.linearVelocity.sqrMagnitude<.5f)TryExit();
            }
            else if(detached&&world!=null&&world.viewer!=null&&Vector3.Distance(world.viewer.position,transform.position)>world.UnloadRadius*world.profile.cellSize+16)Destroy(gameObject);
        }
        void UpdateDriverPose()
        {
            if(driver==null)return;
            float yaw=transform.eulerAngles.y;driver.transform.Rotate(0,Mathf.DeltaAngle(lastYaw,yaw),0,Space.World);lastYaw=yaw;
            float eyeHeight=driver.eyes!=null?driver.eyes.localPosition.y:1.65f;
            // This asset has no interior cabin. Keep the player's optics outside it.
            Vector3 anchor=transform.TransformPoint(new Vector3(0,size.y*.55f,0));
            Vector3 eye=transform.TransformPoint(new Vector3(0,size.y*1.15f,-size.z*1.15f));
            Vector3 direction=eye-anchor;float distance=direction.magnitude;
            int count=Physics.SphereCastNonAlloc(anchor,.22f,direction.normalized,hits,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(transform)&&!hits[i].transform.IsChildOf(driver.transform)&&hits[i].distance<distance)
            {distance=hits[i].distance;eye=anchor+direction.normalized*Mathf.Max(0,distance-.08f);}
            driver.transform.position=eye-Vector3.up*eyeHeight;
        }
        bool SupportedAhead()
        {
            Vector3 velocity=frozen?savedVelocity:body.linearVelocity,travel=Vector3.ClampMagnitude(velocity*.2f,4);
            foreach(var wheel in wheels)
            {
                Vector3 probe=wheel.transform.position+travel+Vector3.up*1.5f;bool found=false;
                if(!world.IsPositionStreamed(probe-Vector3.up*1.5f))return false;
                int count=Physics.RaycastNonAlloc(probe,Vector3.down,hits,4,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)return false;
                for(int i=0;i<count;i++)if(hits[i].rigidbody==null&&!hits[i].transform.IsChildOf(transform)&&hits[i].normal.y>.55f&&hits[i].point.y>=transform.position.y-size.y*.5f-.6f){found=true;break;}
                if(!found)return false;
            }
            return true;
        }
        public override void DetachDriver(PoolroomsExplorer actor)
        {
            if(driver!=actor)return;driver=null;exitRequested=frozen=false;
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
            foreach(var wheel in wheels)wheel.enabled=false;
        }
        public override bool TryExit()
        {
            if(driver==null)return false;exitRequested=true;
            if(frozen||body.linearVelocity.sqrMagnitude>.5f)return false;
            var capsule=driver.GetComponent<CharacterController>();float radius=capsule.radius,height=capsule.height;
            foreach(Vector3 local in new[]{new Vector3(size.x*.5f+1.1f,0,0),new Vector3(-size.x*.5f-1.1f,0,0),new Vector3(0,0,-size.z*.5f-1.1f)})
            {
                Vector3 candidate=transform.TransformPoint(local)+Vector3.up*2;
                int count=Physics.RaycastNonAlloc(candidate,Vector3.down,hits,5,~0,QueryTriggerInteraction.Ignore);
                if(count==hits.Length)continue;float best=float.PositiveInfinity;Vector3 ground=Vector3.zero;
                for(int i=0;i<count;i++)if(hits[i].rigidbody==null&&!hits[i].transform.IsChildOf(transform)&&hits[i].normal.y>.65f&&hits[i].distance<best){best=hits[i].distance;ground=hits[i].point;}
                if(float.IsPositiveInfinity(best))continue;Vector3 feet=ground+Vector3.up*.04f;
                if(!world.IsPositionStreamed(feet)||Physics.CheckCapsule(feet+Vector3.up*radius,feet+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore))continue;
                var actor=driver;driver=null;exitRequested=false;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;
                foreach(var wheel in wheels)wheel.enabled=false;
                actor.EndDriving(this,feet);return true;
            }
            return false;
        }
        void ShiftDetached(Vector3 shift)
        {if(!detached||body==null)return;Vector3 position=body.position+shift;body.position=position;transform.position=position;}
        void OnDestroy()
        {
            if(originActor!=null)originActor.OriginShifted-=ShiftDetached;
            if(driver!=null){var actor=driver;driver=null;actor.EndDriving(this,actor.transform.position+Vector3.up*2);}
        }
    }
}
