using System;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PoolroomsExplorer : MonoBehaviour
    {
        public PoolroomsWorld world;
        public Transform eyes;
        public float walkSpeed=3.2f,sprintSpeed=5.5f,lookSensitivity=.09f;
        [HideInInspector] public bool invertLookY,toggleSprint;
        private readonly SprintPreferenceTracker sprintPreference=new SprintPreferenceTracker();
        [HideInInspector] public bool showHelp=false; // Retain old scene data; gameplay has no HUD.
        [HideInInspector] public bool externalInput;
        private CharacterController body;
        private Vector2 move,look;
        private bool jump,sprint,escape,click,ready;
        private bool checkSupport=true;
        private readonly RaycastHit[] supportHits=new RaycastHit[24];
        private bool chatPressed,masterPressed,flashPressed,flashCycle,ascend,descend;
        private bool grabPressed,dropPressed;
        private PoolroomsCarry carry;
        private PoolroomsSlide slide;
        private bool slideHeld;
        public bool allowSlide=true;
        public float slideSpeed=ExplorerSlide.DefaultSpeed;
        public bool IsSliding { get { return slide!=null&&slide.IsSliding; } }
        public Vector3 StanceCameraOffset { get { return slide!=null?slide.CameraOffset:Vector3.zero; } }
        private PoolroomsMasterMode masterMode;
        private PoolroomsFlashlight flashlight;
        private float pitch;
        private readonly ExplorerLocomotion locomotion=new ExplorerLocomotion();
        private readonly ExplorerDash dash=new ExplorerDash();
        private bool dashPressed;
        public bool IsDashing { get { return dash.Active; } }
        public PoolroomsVehicle Vehicle {get;private set;}
        public bool BeginDriving(PoolroomsVehicle vehicle)
        {if(Vehicle!=null||vehicle==null||IsPaused||InputCaptured)return false;Vehicle=vehicle;dash.Cancel();if(slide!=null)slide.Stop(true);locomotion.Suspend();body.enabled=false;ClearViewMotion();return true;}
        public void EndDriving(PoolroomsVehicle vehicle,Vector3 feet)
        {if(Vehicle!=vehicle)return;Vehicle=null;if(body==null)return;body.enabled=true;Teleport(feet);}
        public int DashCharges { get { return dash.Charges; } }
        public void FeedDashInput(bool pressed){dashPressed=pressed;}
        public int RemainingJumps { get { return locomotion.RemainingJumps; } }
        private PoolroomsPauseMenu pauseMenu;
        private PoolroomsAmbientAudio ambientAudio;
        private readonly WaterContactTracker waterContacts=new WaterContactTracker();
        public bool IsPaused { get; private set; }
        public bool IsReady { get { return ready; } }
        public bool IsFlying { get { return masterMode!=null&&masterMode.Flying; } }
        public bool InputCaptured { get { return masterMode!=null&&masterMode.IsInterfaceOpen; } }
        public PoolroomsMasterMode MasterMode { get { return masterMode; } }
        public PoolroomsFlashlight Flashlight { get { return flashlight; } }
        public Vector3 ViewVelocity { get; private set; }
        public bool ViewGrounded { get; private set; }
        public bool ViewSprinting { get; private set; }
        public Vector2 ViewTurnRate { get; private set; }
        public float ViewPitch { get { return pitch; } }
        private void ClearViewMotion(){ViewVelocity=Vector3.zero;ViewGrounded=ViewSprinting=false;ViewTurnRate=Vector2.zero;}
        public event Action<Vector3,float> WaterImpact;
        public event Action Relocated;
        public event Action<Vector3> OriginShifted;
        private void Awake(){body=GetComponent<CharacterController>();ConfigureBody();}
        public void ConfigureBody()
        {
            if(body==null)body=GetComponent<CharacterController>();
            // All world spawn/water/portal coordinates use a feet pivot, not a capsule centre.
            Vector3 centre=new Vector3(0,body.height*.5f,0);
            if((body.center-centre).sqrMagnitude>.000001f)body.center=centre;
            body.detectCollisions=true;
            if(body.minMoveDistance>0)body.minMoveDistance=0;
            if(body.skinWidth<.01f)body.skinWidth=.025f;
            if(body.stepOffset>body.height)body.stepOffset=Mathf.Min(.34f,body.height*.2f);
        }
        private void Start()
        {
            ConfigureBody();
            slide=GetComponent<PoolroomsSlide>();if(slide==null)slide=gameObject.AddComponent<PoolroomsSlide>();
            ambientAudio=GetComponent<PoolroomsAmbientAudio>();
            if(ambientAudio==null)ambientAudio=gameObject.AddComponent<PoolroomsAmbientAudio>();
            ambientAudio.explorer=this;ambientAudio.world=world;
            masterMode=GetComponent<PoolroomsMasterMode>();if(masterMode==null)masterMode=gameObject.AddComponent<PoolroomsMasterMode>();
            flashlight=GetComponent<PoolroomsFlashlight>();if(flashlight==null)flashlight=gameObject.AddComponent<PoolroomsFlashlight>();
            carry=GetComponent<PoolroomsCarry>();if(carry==null)carry=gameObject.AddComponent<PoolroomsCarry>();
            if(GetComponent<PoolroomsGameSettings>()==null)gameObject.AddComponent<PoolroomsGameSettings>();
            pauseMenu=GetComponent<PoolroomsPauseMenu>();
            if(pauseMenu==null)pauseMenu=gameObject.AddComponent<PoolroomsPauseMenu>();
            Camera camera=eyes!=null?eyes.GetComponent<Camera>():GetComponentInChildren<Camera>();
            if(eyes==null&&camera!=null)eyes=camera.transform;
            if(eyes!=null&&eyes.IsChildOf(transform))
            {
                float height=Vector3.Dot(eyes.position-transform.position,transform.up);
                if(height<.5f*transform.lossyScale.y)
                {
                    // An existing motion component cached its rest pose in OnEnable before Start.
                    var cached=eyes.GetComponent<PoolroomsCameraEffects>();bool restart=cached!=null&&cached.enabled;
                    if(restart)cached.enabled=false;
                    height=Vector3.Dot(eyes.position-transform.position,transform.up);
                    eyes.position+=transform.up*((body.height-.15f)*transform.lossyScale.y-height);
                    if(restart)cached.enabled=true;
                }
            }
            if(camera!=null)
            {
                var effects=camera.GetComponent<PoolroomsCameraEffects>();
                if(effects==null)effects=camera.gameObject.AddComponent<PoolroomsCameraEffects>();
                effects.explorer=this;effects.world=world;pauseMenu.cameraEffects=effects;
                var bridge=Type.GetType("Poolrooms.HDRP.PoolroomsCameraPresentation, Poolrooms.HDRP");
                if(bridge!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&
                    UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline")&&
                    camera.GetComponent(bridge)==null)camera.gameObject.AddComponent(bridge);
                pauseMenu.ApplyPreferences();
            }
            if(!IsPaused){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
        }
        public void SetPaused(bool value)
        {IsPaused=value;dash.Cancel();dashPressed=false;ClearViewMotion();sprintPreference.Reset();if(slide!=null)slide.Stop();slideHeld=false;FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);}
        public void ResetSprintPreference(){sprintPreference.Reset();}
        public void ResetToSpawn()
        {Teleport(world!=null?world.SpawnWorldPosition:Vector3.up*.1f);}
        public void ShiftOrigin(Vector3 shift)
        {
            if(body==null)body=GetComponent<CharacterController>();
            bool enabled=body.enabled;body.enabled=false;transform.position+=shift;body.enabled=enabled;
            var sound=ambientAudio!=null?ambientAudio:GetComponent<PoolroomsAmbientAudio>();if(sound!=null)sound.ShiftOrigin(shift);
            var effects=eyes!=null?eyes.GetComponent<PoolroomsCameraEffects>():null;if(effects!=null)effects.ShiftOrigin(shift);
            // Preserve jump/swim velocity, water contact state, view and audio cadence.
            OriginShifted?.Invoke(shift);
        }
        public void Teleport(Vector3 position)
        {
            if(Vehicle!=null){var car=Vehicle;Vehicle=null;car.DetachDriver(this);body.enabled=true;}
            dash.Reset();dashPressed=false;
            if(body==null)body=GetComponent<CharacterController>();
            bool enabled=body.enabled;body.enabled=false;transform.position=position;
            body.enabled=enabled;if(slide!=null)slide.Stop(true);slideHeld=false;waitForDestination=world!=null;ClearViewMotion();locomotion.Reset();jump=false;ready=false;checkSupport=true;waterContacts.Reset();sprintPreference.Reset();
            var sound=ambientAudio!=null?ambientAudio:GetComponent<PoolroomsAmbientAudio>();
            if(sound!=null)sound.ResetMotion();
            var effects=eyes!=null?eyes.GetComponent<PoolroomsCameraEffects>():null;
            if(effects!=null)effects.ClearTransientEffects();
            Relocated?.Invoke();
        }
        public void FeedInput(Vector2 movement,Vector2 mouseDelta,bool jumpPressed,bool sprintHeld,bool escapePressed,bool clickPressed)
        {move=movement;look=mouseDelta;jump=jumpPressed;sprint=sprintHeld;escape=escapePressed;click=clickPressed;}
        public void FeedUtilityInput(bool chat,bool master,bool flash,bool cycle,bool up,bool down)
        {chatPressed=chat;masterPressed=master;flashPressed=flash;flashCycle=cycle;ascend=up;descend=down;}
        public void FeedCarryInput(bool grab,bool drop){grabPressed=grab;dropPressed=drop;}
        public void FeedSlideInput(bool held){slideHeld=held;}
        private bool renderingShortcut;
        public void FeedRenderingShortcut(bool pressed){renderingShortcut=pressed;}
        public void ResetMotionForMasterMode(bool repairGround=false)
        {
            if(Vehicle!=null){var car=Vehicle;Vehicle=null;car.DetachDriver(this);body.enabled=true;repairGround=true;}
            dash.Cancel();dashPressed=false;
            ClearViewMotion();if(slide!=null)slide.Stop(true);slideHeld=false;locomotion.Reset();checkSupport=repairGround;waterContacts.Reset();jump=false;sprintPreference.Reset();
            var sound=ambientAudio!=null?ambientAudio:GetComponent<PoolroomsAmbientAudio>();if(sound!=null)sound.ResetMotion();
            var effects=eyes!=null?eyes.GetComponent<PoolroomsCameraEffects>():null;if(effects!=null)effects.ClearTransientEffects();
        }
        private bool waitForDestination;
        private void Update()
        {
            ClearViewMotion();
            if(!externalInput)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                move=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
                look=new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"))*20;
                jump=Input.GetKeyDown(KeyCode.Space);sprint=Input.GetKey(KeyCode.Backslash)||Input.GetKey(KeyCode.BackQuote)||Input.GetKey(KeyCode.Less);FeedDashInput(Input.GetKeyDown(KeyCode.LeftShift));FeedSlideInput(Input.GetKey(KeyCode.LeftControl));
                escape=Input.GetKeyDown(KeyCode.Escape);click=Input.GetMouseButtonDown(0);FeedRenderingShortcut(Input.GetKeyDown(KeyCode.F7));
                FeedUtilityInput(Input.GetKeyDown(KeyCode.Y),Input.GetKeyDown(KeyCode.M),Input.GetKeyDown(KeyCode.F),Input.GetKeyDown(KeyCode.V),Input.GetKey(KeyCode.Space),Input.GetKey(KeyCode.LeftControl));
                FeedCarryInput(Input.GetKeyDown(KeyCode.E),Input.GetKeyDown(KeyCode.Q));
#else
                FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);FeedUtilityInput(false,false,false,false,false,false);FeedCarryInput(false,false);FeedSlideInput(false);FeedDashInput(false);
#endif
            }
            if(renderingShortcut&&pauseMenu!=null&&pauseMenu.isActiveAndEnabled)pauseMenu.OpenRenderingSettings();
            else if(escape)
            {
                if(masterMode!=null&&masterMode.HandleEscape()){}
                else if(pauseMenu!=null&&pauseMenu.isActiveAndEnabled)pauseMenu.TogglePause();
                else{Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
            }
            else if(click&&!IsPaused&&!InputCaptured){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            escape=false;click=false;renderingShortcut=false;
            if(!IsPaused&&Time.timeScale>0)
            {
                if(chatPressed&&!InputCaptured&&masterMode!=null)masterMode.OpenChat();
                else if(masterPressed&&masterMode!=null)masterMode.ToggleMenu();
                if(!InputCaptured&&(masterMode==null||!masterMode.ClosedThisFrame)&&flashlight!=null&&flashlight.isActiveAndEnabled)
                {if(flashPressed)flashlight.Toggle();if(flashCycle)flashlight.Cycle();}
            }
            chatPressed=masterPressed=flashPressed=flashCycle=false;
            bool blocked=IsPaused||InputCaptured||masterMode!=null&&masterMode.ClosedThisFrame||Time.timeScale<=0;
            bool burst=dashPressed;dashPressed=false;
            bool sprintRequested=sprintPreference.Sample(sprint,toggleSprint,blocked||Cursor.lockState!=CursorLockMode.Locked||world!=null&&!world.ViewerReady);
            bool grab=grabPressed,drop=dropPressed;grabPressed=dropPressed=false;
            if(blocked){if(Vehicle!=null)Vehicle.Control(0,0,true);dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            if(world!=null&&!world.ViewerReady&&(waitForDestination||masterMode==null||!masterMode.Noclip))
            {
                // Also pause after a teleport to an unloaded room, not only at the initial spawn.
                dash.Cancel();if(slide!=null)slide.Stop();ready=false;checkSupport=true;locomotion.Suspend();jump=false;waterContacts.Reset();return;
            }
            waitForDestination=false;ready=true;
            if(Cursor.lockState!=CursorLockMode.Locked){dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            if(Vehicle!=null){if(grab)Vehicle.TryExit();}
            else if(carry!=null&&carry.isActiveAndEnabled){if(drop)carry.Drop();else if(grab)carry.Interact();}
            if(world!=null&&!world.ViewerReady&&(waitForDestination||masterMode==null||!masterMode.Noclip)){dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            float previousPitch=pitch;
            transform.Rotate(0,look.x*lookSensitivity,0);
            pitch=Mathf.Clamp(pitch-look.y*lookSensitivity*(invertLookY?-1:1),-85,85);
            if(eyes!=null)eyes.localRotation=Quaternion.Euler(pitch,0,0);
            if(Time.deltaTime>0)ViewTurnRate=new Vector2(look.x*lookSensitivity/Time.deltaTime,(pitch-previousPitch)/Time.deltaTime);
            if(IsFlying)
            {
                dash.Cancel();
                if(slide!=null)slide.Stop();
                Vector3 velocity=masterMode.Velocity(eyes,move,(ascend?1:0)-(descend?1:0),sprint);
                if(masterMode.Noclip)transform.position+=velocity*Time.deltaTime;
                else if(body.enabled)body.Move(velocity*Time.deltaTime);
                ClearViewMotion();locomotion.Reset();waterContacts.Reset();jump=false;return;
            }
            if(Vehicle!=null){Vehicle.Control(move.x,move.y,slideHeld,(ascend?1:0)-(descend?1:0));dash.Cancel();jump=false;return;}
            if(!body.enabled){dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            if(checkSupport){checkSupport=false;RepairEmbeddedFeet();}
            float frameTime=ExplorerLocomotion.FrameTime(Time.deltaTime);
            if(frameTime<=0){jump=false;return;}
            int steps=ExplorerLocomotion.Steps(frameTime);float dt=frameTime/steps;
            Vector3 before=transform.position;bool pressed=jump; jump=false;
            Vector2 axis=Vector2.ClampMagnitude(move,1);bool swimming=false,surfaceKick=false,swimKick=false;float downward=0;
            for(int step=0;step<steps;step++)
            {
                // The same frame may cross a water edge or step onto a landing.
                float level=0;swimming=world!=null&&world.TryGetWater(transform.position,out level)&&transform.position.y<level-.75f;
                bool pulse=pressed&&step==0;
                if(step==0)swimKick=swimming&&pressed;
                surfaceKick|=swimming&&pulse&&eyes!=null&&eyes.position.y>level+.1f;
                Vector3 horizontal=(transform.right*axis.x+transform.forward*axis.y)*(swimming?2.1f:(sprintRequested?sprintSpeed:walkSpeed));
                // Resizing a controller can clear isGrounded before Move. Keep the real contact
                // from this substep so jumping out of a slide does not consume the air jump.
                bool grounded=body.isGrounded;
                if(slide!=null)horizontal=slide.Step(dt,slideHeld,grounded&&locomotion.VerticalSpeed<=0,swimming,pulse,horizontal,transform.forward,slideSpeed,allowSlide);
                V3 dashVelocity=dash.Step(dt,burst&&step==0,!swimming,new V3(horizontal.x,0,horizontal.z),new V3(transform.forward.x,0,transform.forward.z));
                if(dash.Started&&slide!=null)slide.Stop();horizontal=new Vector3(dashVelocity.X,0,dashVelocity.Z);
                locomotion.Step(dt,grounded,swimming,level-.95f-transform.position.y,pulse||swimming&&swimKick);
                downward=Mathf.Max(downward,-locomotion.VerticalSpeed);
                Vector3 stepStart=transform.position;
                CollisionFlags flags=body.Move((horizontal+Vector3.up*locomotion.VerticalSpeed)*dt);
                if(slide!=null)slide.ReportMove(horizontal,transform.position-stepStart,dt,flags);
                if(dash.Active&&(flags&CollisionFlags.Sides)!=0&&Vector3.Dot(transform.position-stepStart,horizontal.normalized)<horizontal.magnitude*dt*.25f)dash.Cancel();
                if((flags&CollisionFlags.Above)!=0)locomotion.HitCeiling();
                if((flags&CollisionFlags.Below)!=0&&!swimming)locomotion.Land();
                if(world!=null&&!world.ViewerReady){dash.Cancel();if(slide!=null)slide.Stop();break;} // Never advance into a sector without support.
            }
            // CharacterController.velocity reports only the last substep; optics need full-frame travel.
            ViewVelocity=transform.InverseTransformDirection((transform.position-before)/Time.deltaTime);
            ViewGrounded=body.isGrounded&&!swimming;ViewSprinting=sprintRequested&&ViewGrounded&&!IsSliding&&!IsDashing;
            float contactLevel=0,impact=0;
            bool waterHere=world!=null&&world.TryGetWater(transform.position,out contactLevel);
            float feet=body.bounds.min.y;
            if(waterContacts.Sample(waterHere,feet-contactLevel,downward,Time.deltaTime,out impact,body.isGrounded))
                WaterImpact?.Invoke(new Vector3(transform.position.x,contactLevel,transform.position.z),impact);
            else if(surfaceKick&&waterHere&&waterContacts.SurfaceKick(out impact))
                WaterImpact?.Invoke(new Vector3(transform.position.x,contactLevel,transform.position.z),impact);
            if(world!=null&&world.BelowSafeFloor(transform.position))
            {
                // Reload the origin before moving if an external teleport put the player outside the map.
                ResetToSpawn();
            }
        }
        private void RepairEmbeddedFeet()
        {
            // Only after spawn/teleport/loading, never as a continuous upward ground snap.
            Physics.SyncTransforms();
            Vector3 feet=transform.TransformPoint(body.center-Vector3.up*(body.height*.5f));
            float extent=body.height*Mathf.Abs(transform.lossyScale.y);
            int count=Physics.RaycastNonAlloc(feet+Vector3.up*extent,Vector3.down,supportHits,extent+.2f,~0,QueryTriggerInteraction.Ignore);
            float above=float.PositiveInfinity;bool supported=false;
            for(int i=0;i<count;i++)
            {
                var hit=supportHits[i];if(hit.collider==null||hit.normal.y<.6f||hit.collider.transform.IsChildOf(transform)||hit.collider.attachedRigidbody!=null||hit.collider.GetComponentInParent<PoolroomsPickup>()!=null)continue;
                float delta=hit.point.y-feet.y;
                if(delta<=body.skinWidth+.005f&&delta>=-.2f)supported=true;
                else if(delta>0&&delta<=Mathf.Min(1.25f,extent*.65f))above=Mathf.Min(above,hit.point.y);
            }
            if(supported||float.IsInfinity(above))return;
            // The probe ignores rendered water; a pool can only supply its real bottom or steps.
            body.enabled=false;transform.position+=Vector3.up*(above-feet.y+Mathf.Max(.025f,body.skinWidth));body.enabled=true;
            locomotion.Suspend();waterContacts.Reset();ClearViewMotion();
            if(ambientAudio!=null)ambientAudio.ResetMotion();Relocated?.Invoke();
        }
        private void OnDisable()
        {
            if(Vehicle!=null){var car=Vehicle;Vehicle=null;car.DetachDriver(this);if(body!=null)body.enabled=true;checkSupport=true;}
            dash.Cancel();dashPressed=false;
            if(slide!=null)slide.Stop(true);slideHeld=false;ClearViewMotion();
            if(pauseMenu!=null)pauseMenu.ReleasePause();
            if(ambientAudio!=null)ambientAudio.ResetMotion();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
    }
}
