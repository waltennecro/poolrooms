using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // A seated, arc-distance ride. Room-relative coordinates survive floating-origin shifts.
    [DefaultExecutionOrder(110),DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsAquaticTraversal : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private readonly AquaticRideMotion motion=new AquaticRideMotion();
        private Room ridingRoom;
        private AquaticSlidePath path;
        private bool restoreBody;
        public bool Riding {get{return path!=null;}}
        private Transform fallbackEyes;
        private Vector3 fallbackRest;
        private bool fallbackApplied;
        public Vector3 CameraOffset
        {
            get
            {
                if(!Riding||explorer==null||explorer.eyes==null||explorer.eyes.parent==null)return Vector3.zero;
                return explorer.eyes.parent.InverseTransformVector(-transform.up*Mathf.Max(0,body.height-.92f));
            }
        }
        private void Awake(){explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();}
        public bool TryInteract()
        {
            if(Riding||explorer==null||explorer.IsPaused||explorer.InputCaptured||explorer.IsFlying||explorer.Vehicle!=null||body==null||!body.enabled||explorer.world==null||!explorer.world.ViewerReady)return false;
            Room room=explorer.world.RoomAtViewer();var plan=room?.Plan?.Aquatic;if(plan==null)return false;
            Vector3 local=transform.position-explorer.world.RoomWorldPosition(room);
            var control=explorer.world.AquaticControlAt(room);
            if(control!=null&&(local-ToUnity(plan.Valve)).sqrMagnitude<4.4f)
            {control.Toggle();return true;}
            var carry=explorer.GetComponent<PoolroomsCarry>();if(carry!=null&&carry.Held!=null)return false;
            foreach(var candidate in plan.Slides)
            {
                var start=ToUnity(candidate.Points[0]);
                // Approach on the top deck, never activate through another floor or from below.
                if(Mathf.Abs(local.y-start.y)>.32f||(new Vector2(local.x-start.x,local.z-start.z)).sqrMagnitude>2.8f)continue;
                var scale=transform.lossyScale;
                if(body.height>2.1f||body.radius>.4f||Mathf.Abs(scale.x-1)>.02f||Mathf.Abs(scale.y-1)>.02f||Mathf.Abs(scale.z-1)>.02f)return false;
                ridingRoom=room;path=candidate;motion.Reset();restoreBody=body.enabled;
                explorer.SuspendForAquaticRide();body.enabled=false;return true;
            }
            return false;
        }
        public void Tick(float dt)
        {
            if(!Riding||dt<=0||explorer.IsPaused||explorer.InputCaptured)return;
            var world=explorer.world;
            if(world==null||!world.isActiveAndEnabled||world.RoomAtViewer()!=ridingRoom||!world.ViewerReady||explorer.IsFlying)
            {Cancel(true);return;}
            V3 point=motion.Advance(path,dt);
            transform.position=world.RoomWorldPosition(ridingRoom)+ToUnity(point)+Vector3.up*.1f;
            if(motion.Finished)
            {
                Vector3 end=transform.position;Cancel(false);explorer.FinishAquaticRide(end);
            }
        }
        public Vector3 CurrentVelocity()
        {
            if(Riding||explorer==null||explorer.world==null||body==null||!body.enabled)return Vector3.zero;
            Room room=explorer.world.RoomAtViewer();var plan=room?.Plan?.Aquatic;if(plan==null)return Vector3.zero;
            var control=explorer.world.AquaticControlAt(room);if(control==null||!control.CurrentActive)return Vector3.zero;
            Vector3 p=transform.position-explorer.world.RoomWorldPosition(room);
            foreach(var pool in plan.Pools)if(pool.Contains(p.x,p.z)&&p.y<pool.Level-.12f)
            {
                var velocity=new Vector3(-(p.z-pool.CenterZ),0,p.x-pool.CenterX).normalized*.55f;
                if(!pool.Contains(p.x+velocity.x*.4f,p.z+velocity.z*.4f))velocity=new Vector3(pool.CenterX-p.x,0,pool.CenterZ-p.z).normalized*.25f;
                return velocity;
            }
            return Vector3.zero;
        }
        public void Cancel(bool returnToDeck)
        {
            if(!Riding)return;
            if(returnToDeck&&explorer!=null&&explorer.world!=null&&ridingRoom!=null)
            {
                // Cancellation restores a supported open deck, never stands up inside a curved tube.
                var start=path.Points[0];transform.position=explorer.world.RoomWorldPosition(ridingRoom)+ToUnity(start)+new Vector3(0,.04f,-.75f);
            }
            path=null;ridingRoom=null;if(body!=null)body.enabled=restoreBody;
        }
        private void LateUpdate()
        {
            var eyes=explorer!=null?explorer.eyes:null;if(eyes==null)return;
            if(!Riding)
            {
                if(fallbackApplied){eyes.localPosition=fallbackRest+explorer.StanceCameraOffset;fallbackApplied=false;}
                return;
            }
            var effects=eyes.GetComponent<PoolroomsCameraEffects>();
            if(effects!=null&&effects.isActiveAndEnabled){fallbackApplied=false;return;}
            if(fallbackEyes!=eyes){fallbackEyes=eyes;fallbackRest=eyes.localPosition;}
            if(effects!=null&&effects.HasRestPose)fallbackRest=effects.RestLocalPosition;
            eyes.localPosition=fallbackRest+CameraOffset;fallbackApplied=true;
        }
        private void OnDisable()
        {
            Cancel(true);
            if(fallbackApplied&&fallbackEyes!=null){fallbackEyes.localPosition=fallbackRest;fallbackApplied=false;}
        }
        private static Vector3 ToUnity(V3 v){return new Vector3(v.X,v.Y,v.Z);}
    }
    [DisallowMultipleComponent]
    public sealed class PoolroomsAquaticControl : MonoBehaviour
    {
        public bool CurrentActive {get;private set;}
        private Transform wheel;
        public void Toggle(){CurrentActive=!CurrentActive;if(wheel!=null)wheel.localRotation=Quaternion.Euler(0,CurrentActive?95:0,0);}
        public static void Attach(PoolroomsWorld world,Room room,Transform parent)
        {
            if(parent.GetComponent<PoolroomsAquaticControl>()!=null)return;
            var owner=parent.GetComponent<PoolroomsChunk>();if(owner==null)return;
            var controller=parent.gameObject.AddComponent<PoolroomsAquaticControl>();
            var obj=MeshFactory.Create("Interactive gentle-current valve",AquaticParkArchitecture.ValveWheel(),parent,world.profile,owner,false);
            if(obj!=null){var v=room.Plan.Aquatic.Valve;obj.transform.localPosition=new Vector3(v.X,v.Y,v.Z);controller.wheel=obj.transform;}
        }
    }
}
