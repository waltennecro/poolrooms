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
        public PoolroomsViewRig Views {get;private set;}
        public PoolroomsWings Wings {get;private set;}
        public PoolroomsInventory Inventory {get;private set;}
        public PoolroomsSeating Seating {get;private set;}
#if ENABLE_LEGACY_INPUT_MANAGER
        private static readonly KeyCode[] barKeys={KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha6,KeyCode.Alpha7,KeyCode.Alpha8,KeyCode.Alpha9};
#endif
        private bool inventoryToggle,hotbarToggle,storePressed,useItemPressed,moveItemPressed;
        private int hotbarSelection=-1;
        private float hotbarScroll;
        public void FeedInventoryInput(bool inventory,bool bar,bool store,bool use,bool moveObject,int slot=-1,float scroll=0)
        {inventoryToggle=inventory;hotbarToggle=bar;storePressed=store;useItemPressed=use;moveItemPressed=moveObject;hotbarSelection=slot;hotbarScroll=scroll;}
        private bool wingToggle,wingSpeed;
        public void FeedWingInput(bool toggle,bool cycleSpeed){wingToggle=toggle;wingSpeed=cycleSpeed;}
        // Interactions and swimming stay at the player's head, independent of the visible camera.
        public Transform Aim {get{return Views!=null&&Views.isActiveAndEnabled?Views.Aim:eyes!=null?eyes:transform;}}
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
        private readonly Collider[] repairClearance=new Collider[32];
        private bool chatPressed,masterPressed,flashPressed,flashCycle,ascend,descend;
        private bool grabPressed,dropPressed;
        private PoolroomsCarry carry;
        private PoolroomsSlide slide;
        private PoolroomsAquaticTraversal aquatic;
        public bool IsAquaticRiding {get{return aquatic!=null&&aquatic.Riding;}}
        public void SuspendForAquaticRide(){Wings?.Stop();dash.Cancel();if(slide!=null)slide.Stop(true);locomotion.Suspend();ClearViewMotion();}
        public void FinishAquaticRide(Vector3 feet){transform.position=feet;locomotion.Suspend();ClearViewMotion();waterContacts.Reset();}

        private bool slideHeld;
        public bool allowSlide=true;
        public float slideSpeed=ExplorerSlide.DefaultSpeed;
        public bool IsSliding { get { return slide!=null&&slide.IsSliding; } }
        public Vector3 StanceCameraOffset { get { return Seating!=null&&Seating.IsSeated?Seating.CameraOffset:IsAquaticRiding?aquatic.CameraOffset:slide!=null?slide.CameraOffset:Vector3.zero; } }
        private PoolroomsMasterMode masterMode;
        private PoolroomsFlashlight flashlight;
        private float pitch;
        private readonly ExplorerLocomotion locomotion=new ExplorerLocomotion();
        private readonly ExplorerDash dash=new ExplorerDash();
        private bool dashPressed;
        public bool IsDashing { get { return dash.Active; } }
        public PoolroomsVehicle Vehicle {get;private set;}
        public bool BeginDriving(PoolroomsVehicle vehicle)
        {if(Vehicle!=null||vehicle==null||IsPaused||InputCaptured)return false;Wings?.Stop();Vehicle=vehicle;dash.Cancel();if(slide!=null)slide.Stop(true);locomotion.Suspend();body.enabled=false;ClearViewMotion();return true;}
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
        public bool IsFlying { get { return masterMode!=null&&masterMode.Flying||Wings!=null&&Wings.Flying; } }
        public bool InputCaptured { get { return masterMode!=null&&masterMode.IsInterfaceOpen||Inventory!=null&&Inventory.InterfaceOpen; } }
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
            if(GetComponent<PoolroomsForest39Ambience>()==null)gameObject.AddComponent<PoolroomsForest39Ambience>();
            if(GetComponent<PoolroomsSky63Safety>()==null)gameObject.AddComponent<PoolroomsSky63Safety>();
            if(GetComponent<PoolroomsTravelLoading>()==null)gameObject.AddComponent<PoolroomsTravelLoading>();
            aquatic=GetComponent<PoolroomsAquaticTraversal>();if(aquatic==null)aquatic=gameObject.AddComponent<PoolroomsAquaticTraversal>();
            slide=GetComponent<PoolroomsSlide>();if(slide==null)slide=gameObject.AddComponent<PoolroomsSlide>();
            ambientAudio=GetComponent<PoolroomsAmbientAudio>();
            if(ambientAudio==null)ambientAudio=gameObject.AddComponent<PoolroomsAmbientAudio>();
            ambientAudio.explorer=this;ambientAudio.world=world;
            masterMode=GetComponent<PoolroomsMasterMode>();if(masterMode==null)masterMode=gameObject.AddComponent<PoolroomsMasterMode>();
            flashlight=GetComponent<PoolroomsFlashlight>();if(flashlight==null)flashlight=gameObject.AddComponent<PoolroomsFlashlight>();
            carry=GetComponent<PoolroomsCarry>();if(carry==null)carry=gameObject.AddComponent<PoolroomsCarry>();
            if(GetComponent<PoolroomsGameSettings>()==null)gameObject.AddComponent<PoolroomsGameSettings>();
            Inventory=PoolroomsInventory.For(this);Seating=GetComponent<PoolroomsSeating>();if(Seating==null)Seating=gameObject.AddComponent<PoolroomsSeating>();
            if(GetComponent<PoolroomsMusicPlayer>()==null)gameObject.AddComponent<PoolroomsMusicPlayer>();
            if(GetComponent<PoolroomsSoundscape>()==null)gameObject.AddComponent<PoolroomsSoundscape>();
            Wings=GetComponent<PoolroomsWings>();if(Wings==null)Wings=gameObject.AddComponent<PoolroomsWings>();
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
                if(GetComponent<PoolroomsAvatar>()==null)gameObject.AddComponent<PoolroomsAvatar>();
                Views=camera.GetComponent<PoolroomsViewRig>();if(Views==null)Views=camera.gameObject.AddComponent<PoolroomsViewRig>();Views.Bind(this);
                var bridge=Type.GetType("Poolrooms.HDRP.PoolroomsCameraPresentation, Poolrooms.HDRP");
                if(bridge!=null&&UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline!=null&&
                    UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("HDRenderPipeline")&&
                    camera.GetComponent(bridge)==null)camera.gameObject.AddComponent(bridge);
                pauseMenu.ApplyPreferences();
            }
            if(!IsPaused){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            if(showStartMenu)pauseMenu.OpenStartMenu();
        }
        public void SetPaused(bool value)
        {IsPaused=value;Wings?.Suspend();FeedWingInput(false,false);locomotion.ClearJumpIntent();inspectHeld=false;dash.Cancel();dashPressed=false;ClearViewMotion();sprintPreference.Reset();if(slide!=null)slide.Stop();slideHeld=false;FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);}
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
            Seating?.LeaveForRelocation();Inventory?.CloseInterface();
            Wings?.Stop();FeedWingInput(false,false);
            if(aquatic!=null)aquatic.Cancel(false);
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
        [Tooltip("Open the title screen before exploration starts.")] public bool showStartMenu=true;
        private bool inspectHeld;
        private bool emotePressed;
        public void FeedEmoteInput(bool pressed){emotePressed=pressed;}
        public void FeedInspectInput(bool held){inspectHeld=held;}
        public void FeedCarryInput(bool grab,bool drop){grabPressed=grab;dropPressed=drop;}
        public void FeedSlideInput(bool held){slideHeld=held;}
        private bool renderingShortcut;
        public void FeedRenderingShortcut(bool pressed){renderingShortcut=pressed;}
        private bool viewCycle,photoPressed,photoCapture,photoHide,photoLook,photoFast;
        private float photoVertical,photoRoll,photoScroll;
        public void FeedViewInput(bool cycle,bool photo,bool capture,bool hide,bool looking,float vertical,float roll,float scroll,bool fast)
        {viewCycle=cycle;photoPressed=photo;photoCapture=capture;photoHide=hide;photoLook=looking;photoVertical=vertical;photoRoll=roll;photoScroll=scroll;photoFast=fast;}
        public void ResetMotionForMasterMode(bool repairGround=false)
        {
            Wings?.Stop();FeedWingInput(false,false);
            if(aquatic!=null)aquatic.Cancel(true);
            if(Vehicle!=null){var car=Vehicle;Vehicle=null;car.DetachDriver(this);body.enabled=true;repairGround=true;}
            dash.Cancel();dashPressed=false;
            ClearViewMotion();if(slide!=null)slide.Stop(true);slideHeld=false;locomotion.Reset();checkSupport=repairGround;waterContacts.Reset();jump=false;sprintPreference.Reset();
            var sound=ambientAudio!=null?ambientAudio:GetComponent<PoolroomsAmbientAudio>();if(sound!=null)sound.ResetMotion();
            var effects=eyes!=null?eyes.GetComponent<PoolroomsCameraEffects>():null;if(effects!=null)effects.ClearTransientEffects();
        }
        private bool waitForDestination;
        private void Update()
        {
            if(PoolroomsFrontend.Instance!=null&&PoolroomsFrontend.Instance.Flow.Visible&&PoolroomsFrontend.Instance.Owns(this))return;
            ClearViewMotion();
            if(!externalInput)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                move=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
                look=new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"))*20;
                jump=Input.GetKeyDown(KeyCode.Space);sprint=Input.GetKey(KeyCode.Backslash)||Input.GetKey(KeyCode.BackQuote)||Input.GetKey(KeyCode.Less);FeedDashInput(Input.GetKeyDown(KeyCode.LeftShift));FeedSlideInput(Input.GetKey(KeyCode.LeftControl));
                escape=Input.GetKeyDown(KeyCode.Escape);click=Input.GetMouseButtonDown(0);FeedRenderingShortcut(Input.GetKeyDown(KeyCode.F7));
                FeedUtilityInput(Input.GetKeyDown(KeyCode.Y),Input.GetKeyDown(KeyCode.M),Input.GetKeyDown(KeyCode.F),Input.GetKeyDown(KeyCode.V),Input.GetKey(KeyCode.Space),Input.GetKey(KeyCode.LeftControl));
                FeedCarryInput(Input.GetKeyDown(KeyCode.E),Input.GetKeyDown(KeyCode.Q));FeedInspectInput(Input.GetKey(KeyCode.R));FeedEmoteInput(Input.GetKeyDown(KeyCode.B));
                FeedWingInput(Input.GetKeyDown(KeyCode.G),Input.GetKeyDown(KeyCode.H));
                int selected=-1;
                for(int i=0;i<barKeys.Length;i++)if(Input.GetKeyDown(barKeys[i]))selected=i;
                FeedInventoryInput(Input.GetKeyDown(KeyCode.Tab),Input.GetKeyDown(KeyCode.Alpha5),Input.GetKeyDown(KeyCode.Z),Input.GetKeyDown(KeyCode.X),Input.GetKeyDown(KeyCode.T),selected,Input.mouseScrollDelta.y);
                FeedViewInput(Input.GetKeyDown(KeyCode.F5),Input.GetKeyDown(KeyCode.F6),Input.GetKeyDown(KeyCode.F9),Input.GetKeyDown(KeyCode.F8),Input.GetMouseButton(1),
                    (Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0),(Input.GetKey(KeyCode.C)?1:0)-(Input.GetKey(KeyCode.Z)?1:0),Input.mouseScrollDelta.y,Input.GetKey(KeyCode.LeftShift));
#else
                FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);FeedUtilityInput(false,false,false,false,false,false);FeedCarryInput(false,false);FeedSlideInput(false);FeedDashInput(false);FeedWingInput(false,false);
#endif
            }
            if(Inventory!=null)
            {
                if(Inventory.HandleInput(inventoryToggle,hotbarToggle,storePressed,useItemPressed,moveItemPressed,hotbarSelection,hotbarScroll,escape))escape=click=false;
                if(Inventory.InterfaceOpen){photoPressed=viewCycle=photoCapture=false;}
            }
            FeedInventoryInput(false,false,false,false,false);
            bool viewConsumed=Views!=null&&Views.isActiveAndEnabled&&Views.HandleInput(move,look,viewCycle,photoPressed,escape,photoCapture,photoHide,photoLook,photoVertical,photoRoll,photoScroll,photoFast);
            viewCycle=photoPressed=photoCapture=photoHide=false;photoScroll=0;
            if(viewConsumed){Wings?.Suspend();FeedWingInput(false,false);emotePressed=false;escape=click=renderingShortcut=jump=dashPressed=grabPressed=dropPressed=chatPressed=masterPressed=flashPressed=flashCycle=false;slideHeld=inspectHeld=false;return;}
            if(renderingShortcut&&pauseMenu!=null&&pauseMenu.isActiveAndEnabled){Inventory?.CloseInterface();pauseMenu.OpenRenderingSettings();}
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
                else if(masterPressed&&masterMode!=null&&(Inventory==null||!Inventory.InterfaceOpen))masterMode.ToggleMenu();
                if(!InputCaptured&&(masterMode==null||!masterMode.ClosedThisFrame)&&flashlight!=null&&flashlight.isActiveAndEnabled)
                {if(flashPressed)flashlight.Toggle();if(flashCycle)flashlight.Cycle();}
            }
            chatPressed=masterPressed=flashPressed=flashCycle=false;
            bool blocked=IsPaused||InputCaptured||masterMode!=null&&masterMode.ClosedThisFrame||Time.timeScale<=0;
            bool emote=emotePressed;emotePressed=false;bool burst=dashPressed;dashPressed=false;
            bool toggleWings=wingToggle,cycleWings=wingSpeed;FeedWingInput(false,false);
            bool sprintRequested=sprintPreference.Sample(sprint,toggleSprint,blocked||Cursor.lockState!=CursorLockMode.Locked||world!=null&&!world.ViewerReady);
            bool grab=grabPressed,drop=dropPressed;grabPressed=dropPressed=false;
            if(blocked){Wings?.Suspend();locomotion.ClearJumpIntent();if(Vehicle!=null)Vehicle.Control(0,0,true);dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            if(world!=null&&!world.ViewerReady&&(waitForDestination||masterMode==null||!masterMode.Noclip))
            {
                // Also pause after a teleport to an unloaded room, not only at the initial spawn.
                Wings?.Suspend();dash.Cancel();if(slide!=null)slide.Stop();ready=false;checkSupport=true;locomotion.Suspend();jump=false;waterContacts.Reset();return;
            }
            waitForDestination=false;ready=true;
            if(Cursor.lockState!=CursorLockMode.Locked){Wings?.Suspend();dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            if(Seating!=null&&Seating.IsSeated){if(grab||jump||move.sqrMagnitude>.1f)Seating.TryStand();}
            else if(Vehicle!=null){if(grab)Vehicle.TryExit();}
            else if(!IsAquaticRiding&&carry!=null&&carry.isActiveAndEnabled){if(drop)carry.Drop();else if(grab&&(aquatic==null||!aquatic.TryInteract()))carry.Interact();}
            if(world!=null&&!world.ViewerReady&&(waitForDestination||masterMode==null||!masterMode.Noclip)){Wings?.Suspend();dash.Cancel();if(slide!=null)slide.Stop();jump=false;return;}
            if(inspectHeld&&!IsAquaticRiding&&Vehicle==null&&carry!=null&&carry.Inspect(look))look=Vector2.zero;
            float previousPitch=pitch;
            transform.Rotate(0,look.x*lookSensitivity,0);
            pitch=Mathf.Clamp(pitch-look.y*lookSensitivity*(invertLookY?-1:1),-85,85);
            if(eyes!=null)eyes.localRotation=Quaternion.Euler(pitch,0,0);
            if(Time.deltaTime>0)ViewTurnRate=new Vector2(look.x*lookSensitivity/Time.deltaTime,(pitch-previousPitch)/Time.deltaTime);
            if(Seating!=null&&Seating.IsSeated){Seating.Tick();jump=false;return;}
            if(IsAquaticRiding){aquatic.Tick(Time.deltaTime);jump=false;return;}
            if(masterMode!=null&&masterMode.Flying)
            {
                dash.Cancel();
                if(slide!=null)slide.Stop();
                Vector3 velocity=masterMode.Velocity(Aim,move,(ascend?1:0)-(descend?1:0),sprint);
                if(masterMode.Noclip)transform.position+=velocity*Time.deltaTime;
                else if(body.enabled)body.Move(velocity*Time.deltaTime);
                ClearViewMotion();locomotion.Reset();waterContacts.Reset();jump=false;return;
            }
            if(Wings!=null)
            {
                if(cycleWings)Wings.CycleSpeed();
                if(toggleWings){Wings.ToggleFlight();locomotion.Suspend();jump=false;}
                if(Wings.Flying&&body.enabled)
                {
                    if(checkSupport){checkSupport=false;RepairEmbeddedFeet();}
                    dash.Cancel();if(slide!=null)slide.Stop(true);locomotion.Suspend();waterContacts.Reset();
                    Vector3 beforeFlight=transform.position;
                    Wings.TickFlight(Aim.forward*move.y+Aim.right*move.x+Vector3.up*((ascend?1:0)-(descend?1:0)),Time.deltaTime);
                    ViewVelocity=Time.deltaTime>0?transform.InverseTransformDirection((transform.position-beforeFlight)/Time.deltaTime):Vector3.zero;
                    ViewGrounded=ViewSprinting=false;jump=false;return;
                }
                if(Wings.Flying)Wings.Stop();
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
                surfaceKick|=swimming&&pulse&&Aim.position.y>level+.1f;
                Vector3 horizontal=(transform.right*axis.x+transform.forward*axis.y)*(swimming?2.1f:(sprintRequested?sprintSpeed:walkSpeed));
                // Resizing a controller can clear isGrounded before Move. Keep the real contact
                // from this substep so jumping out of a slide does not consume the air jump.
                bool grounded=body.isGrounded;
                if(slide!=null)horizontal=slide.Step(dt,slideHeld,grounded&&locomotion.VerticalSpeed<=0,swimming,pulse,horizontal,transform.forward,slideSpeed,allowSlide);
                V3 dashVelocity=dash.Step(dt,burst&&step==0,!swimming,new V3(horizontal.x,0,horizontal.z),new V3(transform.forward.x,0,transform.forward.z));
                if(dash.Started&&slide!=null)slide.Stop();horizontal=new Vector3(dashVelocity.X,0,dashVelocity.Z);
                if(aquatic!=null)horizontal+=aquatic.CurrentVelocity();
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
            if(emote&&!blocked){var avatar=GetComponent<PoolroomsAvatar>();if(avatar!=null)avatar.PlayEmote(avatar.Preferences.emote);}
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
            // A saturated query cannot establish which floor is nearest. Never guess.
            if(count>=supportHits.Length)return;
            float above=float.PositiveInfinity;bool supported=false;
            for(int i=0;i<count;i++)
            {
                var hit=supportHits[i];if(hit.collider==null||hit.normal.y<.6f||hit.collider.transform.IsChildOf(transform)||hit.collider.attachedRigidbody!=null||hit.collider.GetComponentInParent<PoolroomsPickup>()!=null)continue;
                float delta=hit.point.y-feet.y;
                if(delta<=body.skinWidth+.005f&&delta>=-.2f)supported=true;
                else if(delta>0&&delta<=Mathf.Min(1.25f,extent*.65f))above=Mathf.Min(above,hit.point.y);
            }
            if(supported||float.IsInfinity(above))return;
            // Verify the entire destination capsule before lifting an embedded actor.
            // A floor below a low ceiling is not a safe standing destination.
            float lift=above-feet.y+Mathf.Max(.025f,body.skinWidth);
            Vector3 candidate=feet+Vector3.up*lift;
            Vector3 scale=transform.lossyScale;
            float radius=body.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            float height=Mathf.Max(extent,2*radius),margin=Mathf.Max(.005f,body.skinWidth);
            int overlaps=Physics.OverlapCapsuleNonAlloc(candidate+Vector3.up*radius,candidate+Vector3.up*(height-radius),Mathf.Max(.01f,radius-margin),repairClearance,~0,QueryTriggerInteraction.Ignore);
            if(overlaps>=repairClearance.Length)return;
            for(int i=0;i<overlaps;i++)
                if(repairClearance[i]!=null&&!repairClearance[i].transform.IsChildOf(transform))return;
            // The probe ignores rendered water; a pool can only supply its real bottom or steps.
            body.enabled=false;transform.position+=Vector3.up*lift;body.enabled=true;
            locomotion.Suspend();waterContacts.Reset();ClearViewMotion();
            if(ambientAudio!=null)ambientAudio.ResetMotion();Relocated?.Invoke();
        }
        private void OnDisable()
        {
            Wings?.Stop();FeedWingInput(false,false);
            if(aquatic!=null)aquatic.Cancel(true);
            if(Vehicle!=null){var car=Vehicle;Vehicle=null;car.DetachDriver(this);if(body!=null)body.enabled=true;checkSupport=true;}
            dash.Cancel();dashPressed=false;
            if(slide!=null)slide.Stop(true);slideHeld=false;ClearViewMotion();
            if(pauseMenu!=null)pauseMenu.ReleasePause();
            if(ambientAudio!=null)ambientAudio.ResetMotion();
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
    }
}
