using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(CharacterController))]
    public sealed class PoolroomsSlide : MonoBehaviour
    {
        private readonly ExplorerSlide motion=new ExplorerSlide();
        private readonly Collider[] clearance=new Collider[32];
        private CharacterController body;
        private PoolroomsExplorer explorer;
        private float standingHeight,standingStep;
        private Transform fallbackEyes;
        private Vector3 fallbackRest;
        public bool IsSliding { get { return motion.Sliding; } }
        public bool IsLow { get { return body!=null&&body.height<standingHeight-.01f; } }
        public float CameraDrop { get; private set; }
        public Vector3 CameraOffset
        {
            get
            {
                if(explorer==null||explorer.eyes==null||explorer.eyes.parent==null)return Vector3.zero;
                return explorer.eyes.parent.InverseTransformVector(-transform.up*CameraDrop*Mathf.Abs(transform.lossyScale.y));
            }
        }
        private void Awake()
        {
            body=GetComponent<CharacterController>();explorer=GetComponent<PoolroomsExplorer>();
            standingHeight=body.height;standingStep=body.stepOffset;
        }
        public Vector3 Step(float dt,bool held,bool grounded,bool swimming,bool jump,Vector3 desired,Vector3 facing,float speed,bool allowed)
        {
            if(!isActiveAndEnabled){Stop(true);return desired;}
            V3 v=motion.Step(dt,held,grounded,swimming,jump,ToCore(desired),ToCore(facing),speed,allowed);
            SetLow(motion.Sliding||held&&motion.HasMomentum&&!motion.RequiresRelease);return new Vector3(v.X,0,v.Z);
        }
        public void ReportMove(Vector3 requested,Vector3 displacement,float dt,CollisionFlags flags)
        {
            if(!motion.HasMomentum||dt<=0||(flags&CollisionFlags.Sides)==0)return;
            // A glancing wall permits sliding along it; a frontal wall ends the boost.
            if(Vector3.Dot(displacement,requested.normalized)<requested.magnitude*dt*.25f)Stop();
        }
        public void Stop(bool instantCamera=false)
        {
            motion.Stop();if(body==null)return;SetLow(false);
            if(instantCamera)CameraDrop=Mathf.Max(0,standingHeight-body.height);
        }
        private void SetLow(bool low)
        {
            if(body==null||!body.enabled)return;
            float height=low?ExplorerSlide.LowHeight(standingHeight,body.radius,body.skinWidth):standingHeight;
            if(height>body.height+.001f&&!CanStand())return;
            if(Mathf.Abs(body.height-height)<.001f)return;
            // Resize around the feet. Never translate the actor into or out of the floor.
            Vector3 feet=body.center-Vector3.up*(body.height*.5f);
            // Keep the ordinary stair allowance (Poolrooms steps are commonly 0.21 m).
            body.height=height;body.center=feet+Vector3.up*(height*.5f);body.stepOffset=Mathf.Min(standingStep,height*.45f);
            if(height<standingHeight)CameraDrop=standingHeight-height;
            else body.stepOffset=standingStep;
        }
        public bool CanStand()
        {
            if(body==null)return false;
            Vector3 scale=transform.lossyScale;float vertical=Mathf.Abs(scale.y),radial=Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            Vector3 feet=transform.TransformPoint(body.center-Vector3.up*(body.height*.5f));
            float radius=body.radius*radial,margin=Mathf.Max(.005f,body.skinWidth*radial*1.2f);
            float height=Mathf.Max(standingHeight*vertical,2*radius);
            int count=Physics.OverlapCapsuleNonAlloc(feet+transform.up*radius,feet+transform.up*(height-radius),Mathf.Max(.01f,radius-margin),clearance,~0,QueryTriggerInteraction.Ignore);
            if(count>=clearance.Length)return false; // Full buffer is inconclusive, so stay safely low.
            for(int i=0;i<count;i++)if(clearance[i]!=null&&!clearance[i].transform.IsChildOf(transform))return false;
            return true;
        }
        private void LateUpdate()
        {
            if(explorer==null)return;
            // Try again after leaving a low ceiling, even with Ctrl released.
            if(!motion.Sliding&&!motion.HasMomentum&&body.enabled&&!explorer.IsPaused&&!explorer.InputCaptured)SetLow(false);
            if(!explorer.IsPaused&&!explorer.InputCaptured&&Time.deltaTime>0)
                CameraDrop=Mathf.MoveTowards(CameraDrop,Mathf.Max(0,standingHeight-body.height),Time.deltaTime*8);
            Transform eyes=explorer.eyes;if(eyes==null)return;
            if(fallbackEyes!=eyes){fallbackEyes=eyes;fallbackRest=eyes.localPosition-CameraOffset;}
            var effects=eyes.GetComponent<PoolroomsCameraEffects>();
            if(effects!=null&&effects.isActiveAndEnabled){fallbackRest=eyes.localPosition-CameraOffset;return;}
            if(effects!=null&&effects.HasRestPose)fallbackRest=effects.RestLocalPosition;
            eyes.localPosition=fallbackRest+CameraOffset;
        }
        private void OnDisable(){Stop(true);}
        private static V3 ToCore(Vector3 v){return new V3(v.x,v.y,v.z);}
    }
}
