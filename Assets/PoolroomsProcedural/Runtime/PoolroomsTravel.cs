using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer),typeof(CharacterController))]
    public sealed class PoolroomsTravel : MonoBehaviour
    {
        public const string PreferenceKey="Poolrooms.Player.Travel.v1";
        public TravelPreferences Preferences {get;private set;}=new TravelPreferences();
        public bool AutoAdvance {get{return motion.AutoAdvance;}}
        public bool Grappling {get{return pull.Active;}}
        public static readonly string[] PaceNames={"Normal","Ágil · 8 m/s","Exprés · 12 m/s"};
        private readonly ExplorerTravel motion=new ExplorerTravel();
        private readonly GrapplePull pull=new GrapplePull();
        private readonly RaycastHit[] hits=new RaycastHit[48];
        private PoolroomsExplorer explorer;
        private CharacterController body;
        private Transform anchor;
        private Vector3 anchorLocal;
        private bool cyclePressed,autoPressed,held,wasHeld,dirty;
        private float saveAt;
        private LineRenderer rope;
        private GameObject ropeObject;

        private void Awake()
        {
            explorer=GetComponent<PoolroomsExplorer>();body=GetComponent<CharacterController>();
            Preferences=ReadPreferences();
        }
        public static TravelPreferences ReadPreferences()
        {
            TravelPreferences values;
            try{values=JsonUtility.FromJson<TravelPreferences>(PlayerPrefs.GetString(PreferenceKey,""))??new TravelPreferences();}
            catch{values=new TravelPreferences();}
            values.Sanitize();return values;
        }
        public void FeedInput(bool cycle,bool auto,bool grappleHeld)
        {cyclePressed=cycle;autoPressed=auto;held=grappleHeld;}
        public void SavePreferences()
        {
            Preferences.Sanitize();dirty=true;saveAt=Time.unscaledTime+.5f;
            if(!Preferences.allowAutoAdvance)motion.StopAuto();
            if(!Preferences.allowGrapple)StopGrapple();
        }
        public void ResetSettings(){Preferences=new TravelPreferences();Suspend();SavePreferences();}
        private void Flush()
        {if(!dirty)return;PlayerPrefs.SetString(PreferenceKey,JsonUtility.ToJson(Preferences));PlayerPrefs.Save();dirty=false;}
        public void Suspend(){motion.Stop();StopGrapple();cyclePressed=autoPressed=false;wasHeld=held;}
        private void StopGrapple(){pull.Stop();anchor=null;if(rope!=null)rope.enabled=false;}
        public Vector2 PrepareInput(Vector2 input)
        {
            if(!isActiveAndEnabled)return input;
            if(cyclePressed)
            {
                Preferences.pace=(TravelPace)(((int)Preferences.pace+1)%3);SavePreferences();
                explorer.Inventory?.Toast("Travesía: "+PaceNames[(int)Preferences.pace]+" · C cambia el ritmo",4);
            }
            bool toggle=autoPressed;cyclePressed=autoPressed=false;
            var resolved=motion.ResolveInput(new V2(input.x,input.y),toggle,Preferences.allowAutoAdvance);
            if(toggle)explorer.Inventory?.Toast(motion.AutoAdvance?"Avance automático · J o S para detenerte":"Avance automático detenido",4);
            return new Vector2(resolved.X,resolved.Y);
        }
        public float StepSpeed(float dt,Vector2 input,bool sprint,bool swimming,float walk,float run)
        {return motion.StepSpeed(dt,Preferences.pace,input.sqrMagnitude>.001f,input.y>.15f,sprint,swimming,Preferences.allowFastSwim&&sprint,walk,run);}
        public bool NeedsSmallSteps(bool sprint)
        {return Preferences.pace!=TravelPace.Normal||motion.AutoAdvance||Preferences.allowFastSwim&&sprint;}
        public bool CanAutoAdvance(Vector3 velocity,float dt)
        {
            if(!motion.AutoAdvance||velocity.sqrMagnitude<.001f)return true;
            Vector3 ahead=transform.position+velocity.normalized*(body.radius+.15f+velocity.magnitude*dt);
            if(!Query(ahead+Vector3.up*.6f,Vector3.down,1.1f,out var ground))return false;
            return ground.normal.y>=.55f&&ground.collider.attachedRigidbody==null;
        }
        public void StopAutomatic(){motion.StopAuto();}
        public void ReportMovement(float expected,float actual,float dt)
        {motion.ReportMovement(expected,actual,dt);}

        // Invoked by Explorer only after its input, readiness, photo, seat and vehicle gates.
        public bool TickGrapple(float dt)
        {
            if(!isActiveAndEnabled||!Preferences.allowGrapple||explorer==null||body==null||!body.enabled||
                explorer.IsPaused||explorer.InputCaptured||explorer.IsAquaticRiding||explorer.Vehicle!=null||explorer.IsFlying||
                explorer.Seating!=null&&explorer.Seating.IsSeated||explorer.Views!=null&&explorer.Views.PhotoActive||
                explorer.world!=null&&!explorer.world.ViewerReady){StopGrapple();return false;}
            bool start=held&&!wasHeld;wasHeld=held;
            if(!held){StopGrapple();return false;}
            if(start)TryAnchor();
            if(!pull.Active)return false;
            if(anchor==null||!anchor.gameObject.activeInHierarchy){StopGrapple();return false;}
            Vector3 target=anchor.TransformPoint(anchorLocal),offset=target-explorer.Aim.position;
            if(!ClearLine(target)){StopGrapple();return false;}
            V3 delta=pull.Step(new V3(offset.x,offset.y,offset.z),dt);
            if(!pull.Active){StopGrapple();return false;}
            motion.StopAuto();Vector3 displacement=new Vector3(delta.X,delta.Y,delta.Z);
            int count=ExplorerTravel.StepsFor(delta);Vector3 step=displacement/count,before=transform.position;
            bool hit=false;
            for(int i=0;i<count;i++)
            {
                if(explorer.world!=null&&!explorer.world.ViewerReady){StopGrapple();break;}
                var flags=body.Move(step);hit|=(flags&(CollisionFlags.Sides|CollisionFlags.Above))!=0;
                if((flags&CollisionFlags.Below)!=0&&step.y<0){StopGrapple();break;}
            }
            float actual=Vector3.Dot(transform.position-before,displacement.sqrMagnitude>0?displacement.normalized:Vector3.zero);
            pull.ReportMovement(displacement.magnitude,actual,dt);
            if(hit&&actual<displacement.magnitude*.2f)StopGrapple();
            if(!pull.Active)StopGrapple();else ShowRope(target);
            return true;
        }
        private bool Query(Vector3 origin,Vector3 direction,float distance,out RaycastHit first)
        {
            first=new RaycastHit();int count=Physics.RaycastNonAlloc(origin,direction,hits,distance,~0,QueryTriggerInteraction.Ignore);
            if(count>=hits.Length)return false;
            bool found=false;float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var candidate=hits[i];if(candidate.collider==null||candidate.collider.transform.IsChildOf(transform)||candidate.distance>=nearest)continue;
                first=candidate;nearest=candidate.distance;found=true;
            }
            return found;
        }
        private void TryAnchor()
        {
            if(!Query(explorer.Aim.position,explorer.Aim.forward,GrapplePull.Range,out var hit))return;
            var chunk=hit.collider.GetComponentInParent<PoolroomsChunk>();
            // Never attach to pickups, vehicles, safety nets or invisible streaming gates.
            if(chunk==null||explorer.world!=null&&chunk.GetComponentInParent<PoolroomsWorld>()!=explorer.world||hit.collider.attachedRigidbody!=null||!(hit.collider is MeshCollider)||
                hit.collider.GetComponentInParent<PoolroomsPickup>()!=null||
                hit.collider.name.StartsWith("Streaming gate",StringComparison.Ordinal)||
                hit.collider.name=="Play safety nets"||!pull.Begin(hit.distance))return;
            anchor=hit.collider.transform;anchorLocal=anchor.InverseTransformPoint(hit.point);
            explorer.GetComponent<PoolroomsSoundscape>()?.PlayStation(StationSound.Latch,hit.point);
        }
        private bool ClearLine(Vector3 target)
        {
            Vector3 offset=target-explorer.Aim.position;
            if(offset.sqrMagnitude<.001f)return false;
            return Query(explorer.Aim.position,offset.normalized,offset.magnitude+.08f,out var hit)&&hit.collider.transform==anchor;
        }
        private void ShowRope(Vector3 target)
        {
            if(rope==null)
            {
                var material=explorer.world!=null&&explorer.world.profile!=null?explorer.world.profile.metal:null;
                if(material==null)return;
                ropeObject=new GameObject("Travel grappling cable");ropeObject.transform.SetParent(transform,false);
                rope=ropeObject.AddComponent<LineRenderer>();rope.sharedMaterial=material;
                rope.useWorldSpace=true;rope.positionCount=2;rope.startWidth=rope.endWidth=.018f;
                rope.numCapVertices=3;rope.generateLightingData=true;rope.shadowCastingMode=ShadowCastingMode.Off;rope.receiveShadows=false;
            }
            rope.enabled=true;rope.SetPosition(0,explorer.Aim.position+explorer.Aim.right*.22f-Vector3.up*.22f);rope.SetPosition(1,target);
        }
        private void LateUpdate()
        {
            cyclePressed=autoPressed=false;
            if(explorer==null||explorer.IsPaused||explorer.InputCaptured||!explorer.IsReady||
                explorer.Views!=null&&explorer.Views.PhotoActive)Suspend();
            if(rope!=null&&pull.Active&&anchor!=null)ShowRope(anchor.TransformPoint(anchorLocal));
            if(!held)wasHeld=false;
            if(dirty&&Time.unscaledTime>=saveAt)Flush();
        }
        private void OnDisable(){Suspend();Flush();}
        private void OnDestroy(){if(ropeObject!=null)Destroy(ropeObject);}
    }
}
