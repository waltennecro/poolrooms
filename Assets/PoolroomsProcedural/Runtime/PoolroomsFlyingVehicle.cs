using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // Shared pause, loading, origin shift, chase view and grounded exit behavior.
    // Aircraft-specific forces and geometry remain in their concrete components.
    public abstract class PoolroomsFlyingVehicle : PoolroomsVehicle
    {
        protected PoolroomsWorld world;
        protected PoolroomsExplorer driver;
        private PoolroomsExplorer originActor;
        protected Rigidbody body;
        protected string spawnKey;
        protected Vector3 size;
        private Vector3 savedVelocity,savedAngular;
        protected float turn,throttle,climb;
        private float yaw,lastYaw;
        protected bool airbrake;
        private bool detached,frozen;
        private readonly RaycastHit[] hits=new RaycastHit[32];
        public override bool IsDriving {get{return driver!=null;}}
        protected abstract AircraftForces FlightForces(float dt,float speed,float verticalSpeed,float altitude);
        protected abstract void ResetFlight();
        protected abstract void AnimateRotors(float dt);
        public override bool Enter(PoolroomsExplorer actor)
        {
            if(actor==null||actor.Vehicle!=null||actor.IsFlying||actor.IsPaused||actor.InputCaptured||driver!=null||world==null||!world.ViewerReady)return false;
            if(!actor.BeginDriving(this))return false;
            driver=actor;ResetFlight();yaw=lastYaw=transform.eulerAngles.y;body.isKinematic=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            transform.SetParent(world.GeneratedRoot,true);detached=true;
            if(originActor==null){originActor=actor;originActor.OriginShifted+=ShiftDetached;}
            var carry=actor.GetComponent<PoolroomsCarry>();if(carry!=null)carry.MarkTaken(spawnKey);
            UpdateDriverPose();return true;
        }
        public override void Control(float horizontal,float vertical,bool brake,float lift)
        {turn=Mathf.Clamp(horizontal,-1,1);throttle=Mathf.Clamp(vertical,-1,1);climb=Mathf.Clamp(lift,-1,1);airbrake=brake;}
        protected void FixedUpdate()
        {
            if(driver==null)return;
            Vector3 velocity=frozen?savedVelocity:body.linearVelocity;
            bool unavailable=driver.IsPaused||driver.InputCaptured||world==null||!world.ViewerReady||!world.IsPositionStreamed(body.position+Vector3.ClampMagnitude(velocity*.25f,world.profile.cellSize*.6f));
            if(unavailable){Freeze();return;}Unfreeze();
            Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            float speed=Vector3.Dot(body.linearVelocity,forward),altitude=body.position.y-world.transform.position.y;
            var forces=FlightForces(Time.fixedDeltaTime,speed,body.linearVelocity.y,altitude);
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
        protected void LateUpdate()
        {
            if(driver!=null)
            {
                UpdateDriverPose();
                if(!driver.IsPaused&&!driver.InputCaptured&&!frozen)
                AnimateRotors(Time.deltaTime);
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
                var actor=driver;driver=null;body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;ResetFlight();actor.EndDriving(this,feet);return true;
            }
            return false;
        }
        public override void DetachDriver(PoolroomsExplorer actor)
        {
            if(driver!=actor)return;driver=null;frozen=false;
            if(!body.isKinematic){body.linearVelocity=body.angularVelocity=Vector3.zero;body.isKinematic=true;}ResetFlight();
        }
        void ShiftDetached(Vector3 shift){if(!detached||body==null)return;Vector3 position=body.position+shift;body.position=position;transform.position=position;}
        protected virtual void OnDestroy()
        {if(originActor!=null)originActor.OriginShifted-=ShiftDetached;if(driver!=null){var actor=driver;driver=null;actor.EndDriving(this,actor.transform.position+Vector3.up*2);}}
    }
}
