using UnityEngine;

namespace Poolrooms
{
    // First ground-vehicle implementation; native wheel/PhysX validation is still required.
    [DisallowMultipleComponent]
    public sealed class PoolroomsCityVehicle : MonoBehaviour
    {
        private PoolroomsWorld world;
        private PoolroomsExplorer driver;
        private PoolroomsExplorer originActor;
        private Rigidbody body;
        private WheelCollider[] wheels;
        private string spawnKey;
        private Vector3 size;
        private float steer,throttle,lastYaw;
        private bool brake,detached,exitRequested;
        private bool waitingSupport;
        private readonly RaycastHit[] support=new RaycastHit[16];
        public bool IsDriving {get{return driver!=null;}}
        public static PoolroomsCityVehicle Create(PoolroomsWorld world,Transform parent,Vector3 position,string key)
        {
            var settings=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsGameSettings>():null;
            bool low=settings!=null&&settings.Preferences!=null&&settings.Preferences.graphicsQuality==0;
            var go=City11Sources.Create("ToyCar",parent,position,4.2f,out var size,low?1:0);if(go==null)return null;
            go.name="Coche · E conducir / salir · Ctrl frenar";go.transform.localPosition=position+Vector3.up*(size.y*.5f+.03f);
            var car=go.AddComponent<PoolroomsCityVehicle>();car.world=world;car.spawnKey=key;car.size=size;
            var shape=go.AddComponent<BoxCollider>();shape.size=new Vector3(size.x*.92f,size.y*.65f,size.z*.86f);shape.center=Vector3.up*size.y*.08f;
            car.body=go.AddComponent<Rigidbody>();car.body.mass=1100;car.body.centerOfMass=Vector3.down*size.y*.18f;
            car.body.linearDamping=.025f;car.body.angularDamping=.55f;car.body.interpolation=RigidbodyInterpolation.Interpolate;car.body.maxDepenetrationVelocity=3;car.body.isKinematic=true;
            car.wheels=new WheelCollider[4];int index=0;
            for(int z=-1;z<=1;z+=2)for(int x=-1;x<=1;x+=2)
            {
                var wheel=new GameObject("Wheel contact");wheel.transform.SetParent(go.transform,false);
                float radius=Mathf.Clamp(size.y*.23f,.25f,.38f);
                wheel.transform.localPosition=new Vector3(x*size.x*.41f,-size.y*.5f+radius+.1f,z*size.z*.33f);
                var contact=wheel.AddComponent<WheelCollider>();contact.radius=radius;contact.mass=30;contact.suspensionDistance=.2f;
                var spring=contact.suspensionSpring;spring.spring=35000;spring.damper=4500;spring.targetPosition=.5f;contact.suspensionSpring=spring;
                contact.brakeTorque=1800;contact.ConfigureVehicleSubsteps(5,8,3);contact.enabled=false;car.wheels[index++]=contact;
            }
            return car;
        }
        public bool Enter(PoolroomsExplorer actor)
        {
            if(driver!=null||actor==null||actor.IsFlying||actor.IsPaused||actor.InputCaptured||actor.Vehicle!=null||world==null||!world.ViewerReady)return false;
            if(!actor.BeginDriving(this))return false;
            driver=actor;lastYaw=transform.eulerAngles.y;foreach(var wheel in wheels)wheel.enabled=true;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            transform.SetParent(world.GeneratedRoot,true);detached=true;
            if(originActor==null){originActor=actor;originActor.OriginShifted+=ShiftDetached;}
            var carry=actor.GetComponent<PoolroomsCarry>();if(carry!=null)carry.MarkTaken(spawnKey);
            actor.transform.rotation=Quaternion.Euler(0,lastYaw,0);UpdateDriverPose();return true;
        }
        public void Control(float horizontal,float vertical,bool handbrake)
        {steer=Mathf.Clamp(horizontal,-1,1);throttle=Mathf.Clamp(vertical,-1,1);brake=handbrake;}
        void FixedUpdate()
        {
            if(driver==null)return;
            if(world==null||!world.ViewerReady||!SupportedAhead())
            {
                if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
                waitingSupport=true;return;
            }
            if(waitingSupport){waitingSupport=false;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;}
            bool blocked=driver.IsPaused||driver.InputCaptured||world==null||!world.ViewerReady||exitRequested;
            float speed=body.linearVelocity.magnitude;
            for(int i=0;i<wheels.Length;i++)
            {
                wheels[i].steerAngle=i>=2&&!blocked?steer*Mathf.Lerp(28,10,Mathf.Clamp01(speed/18)):0;
                wheels[i].motorTorque=!blocked&&!brake&&speed<20?throttle*950:0;
                wheels[i].brakeTorque=blocked||brake?1800:Mathf.Abs(throttle)<.01f?80:0;
            }
        }
        void LateUpdate()
        {
            if(driver!=null){UpdateDriverPose();if(exitRequested&&body.linearVelocity.sqrMagnitude<.5f)TryExit();}
            else if(detached&&world!=null&&world.viewer!=null&&Vector3.Distance(world.viewer.position,transform.position)>world.LoadRadius*world.profile.cellSize+16)Destroy(gameObject);
        }
        void UpdateDriverPose()
        {
            if(driver==null)return;
            float yaw=transform.eulerAngles.y;driver.transform.Rotate(0,Mathf.DeltaAngle(lastYaw,yaw),0,Space.World);lastYaw=yaw;
            // The eyes retain their own optics; the inactive capsule does not collide with the car.
            float eyeHeight=driver.eyes!=null?driver.eyes.localPosition.y:1.65f;
            driver.transform.position=transform.TransformPoint(new Vector3(-size.x*.2f,size.y*.12f,size.z*.02f))-Vector3.up*eyeHeight;
        }
        bool SupportedAhead()
        {
            Vector3 travel=body.isKinematic?Vector3.zero:body.linearVelocity*.15f;
            foreach(var wheel in wheels)
            {
                Vector3 probe=wheel.transform.position+travel+Vector3.up*1.5f;bool found=false;
                int count=Physics.RaycastNonAlloc(probe,Vector3.down,support,4,~0,QueryTriggerInteraction.Ignore);
                if(count==support.Length)return false;
                for(int i=0;i<count;i++)if(support[i].rigidbody==null&&support[i].normal.y>.55f&&support[i].point.y>=transform.position.y-size.y*.5f-.6f){found=true;break;}
                if(!found)return false;
            }
            return true;
        }
        public void DetachDriver(PoolroomsExplorer actor)
        {
            if(driver!=actor)return;driver=null;exitRequested=waitingSupport=false;
            if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.isKinematic=true;}
            foreach(var wheel in wheels)wheel.enabled=false;
        }
        public bool TryExit()
        {
            if(driver==null)return false;exitRequested=true;
            if(body.linearVelocity.sqrMagnitude>.5f)return false;
            var capsule=driver.GetComponent<CharacterController>();float radius=capsule.radius,height=capsule.height;
            foreach(Vector3 local in new[]{new Vector3(size.x*.5f+1.1f,0,0),new Vector3(-size.x*.5f-1.1f,0,0),new Vector3(0,0,-size.z*.5f-1.1f)})
            {
                Vector3 candidate=transform.TransformPoint(local)+Vector3.up*2;
                if(!Physics.Raycast(candidate,Vector3.down,out var ground,5,~0,QueryTriggerInteraction.Ignore)||ground.normal.y<.65f||ground.rigidbody!=null)continue;
                Vector3 feet=ground.point+Vector3.up*.04f;
                if(Physics.CheckCapsule(feet+Vector3.up*radius,feet+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore))continue;
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
