using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DefaultExecutionOrder(100),DisallowMultipleComponent,RequireComponent(typeof(Camera))]
    public sealed class PoolroomsCameraEffects : MonoBehaviour
    {
        public PoolroomsExplorer explorer;
        public PoolroomsWorld world;
        public LayerMask occlusionMask=~0;
        public CameraPreferences preferences=new CameraPreferences();
        public readonly Vector4[] flarePositions=new Vector4[CameraPresentation.MaxFlares];
        public readonly Vector4[] flareColors=new Vector4[CameraPresentation.MaxFlares];
        public float Wetness { get; private set; }
        public float EffectTime { get; private set; }
        public float ImpactPulse { get; private set; }
        public float WetSeed { get; private set; }
        public float WetElapsed { get; private set; }
        public int FlareCount { get; private set; }
        public int VisibleSourceBudget { get; set; }=CameraPresentation.MaxFlares;
        public float SourceIntensityScale { get; set; }=1;
        public Vector4 Analog { get; set; }
        public float Diffusion { get; set; }
        private readonly CameraLanding landing=new CameraLanding();
        private readonly CameraMotionTracker motion=new CameraMotionTracker();
        public Vector3 FlashlightOffset { get { return ToUnity(motion.Frame.FlashlightPosition); } }
        public Vector3 FlashlightAngles { get { return ToUnity(motion.Frame.FlashlightAngles); } }
        private Quaternion restRotation;
        private Camera view;
        private PoolroomsViewRig viewRig;
        private PoolroomsExplorer subscribed;
        private Vector3 restPosition;
        public bool HasRestPose { get { return view!=null; } }
        public Vector3 RestLocalPosition { get { return restPosition; } }
        private float restFov,nextScan;
        private ParticleSystem spray;
        private Material sprayMaterial;
        private uint spraySequence=0x1539u;
        private readonly ParticleSystem.Particle[] shiftedParticles=new ParticleSystem.Particle[256];
        private readonly RaycastHit[] hits=new RaycastHit[8];
        private struct Source { public int Id;public Vector3 Image,Surface,Light;public Color Color;public float Strength,Scale;public bool Reflected; }
        private readonly Source[] candidates=new Source[8],selected=new Source[CameraPresentation.MaxFlares];
        private int candidateCount;
        private readonly int[] oldIds=new int[CameraPresentation.MaxFlares];
        private readonly float[] oldWeights=new float[CameraPresentation.MaxFlares];

        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            BindExplorer();
            view=GetComponent<Camera>();restPosition=transform.localPosition-StanceOffset;restRotation=transform.localRotation;restFov=view.fieldOfView;
            ClearTransientEffects();
        }
        private void BindExplorer()
        {
            if(explorer==null)explorer=GetComponentInParent<PoolroomsExplorer>();
            if(world==null&&explorer!=null)world=explorer.world;
            if(subscribed==explorer)return;
            if(subscribed!=null)subscribed.WaterImpact-=OnWaterImpact;
            subscribed=explorer;if(subscribed!=null)subscribed.WaterImpact+=OnWaterImpact;
        }
        private void LateUpdate()
        {
            BindExplorer();
            if(explorer!=null&&(!explorer.IsReady||!explorer.isActiveAndEnabled)){ResetMotionPose();return;}
            ApplyMotion();
            if(viewRig==null)viewRig=GetComponent<PoolroomsViewRig>();
            if(viewRig!=null&&viewRig.isActiveAndEnabled)viewRig.ComposeView();
            // Settings can be previewed while paused; only optical source visibility refreshes.
            // World movement, particle simulation, droplets and analog animation use scaled time.
            if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.12f;ScanSources();}
            for(int i=0;i<FlareCount;i++)
            {
                var source=selected[i];Vector3 uv=view.WorldToViewportPoint(source.Image);
                float visible=uv.z>0&&uv.x>-.1f&&uv.x<1.1f&&uv.y>-.1f&&uv.y<1.1f?source.Strength:0;
                float strength=CameraOptics.Fade(flarePositions[i].z,visible,Time.unscaledDeltaTime);
                flarePositions[i]=new Vector4(uv.x,uv.y,strength,source.Reflected?1:0);
            }
            float dt=Time.deltaTime;if(dt<=0||(explorer!=null&&explorer.IsPaused))return;
            EffectTime+=dt;WetElapsed+=dt;
            Wetness=Mathf.MoveTowards(Wetness,0,dt*.2f);ImpactPulse=Mathf.MoveTowards(ImpactPulse,0,dt*2.4f);
            if(!preferences.waterEffects){Wetness=0;ImpactPulse=0;}
            float dip=preferences.cameraMotion?ImpactPulse*preferences.splashIntensity:0;
            if(viewRig==null||!viewRig.isActiveAndEnabled||!viewRig.ExternalPose)
            {transform.localPosition=restPosition+StanceOffset+ToUnity(motion.Frame.Position)+Vector3.up*landing.Offset+Vector3.down*(dip*.025f);view.fieldOfView=restFov+motion.Frame.FieldOfView+dip*.65f;}
        }
        private Vector3 StanceOffset { get { return explorer!=null&&explorer.eyes==transform?explorer.StanceCameraOffset:Vector3.zero; } }
        private Quaternion BaseRotation { get { return explorer!=null&&explorer.eyes==transform?Quaternion.Euler(explorer.ViewPitch,0,0):restRotation; } }
        private void ApplyMotion()
        {
            bool ownsEyes=explorer!=null&&explorer.eyes==transform;
            Vector3 velocity=ownsEyes?explorer.ViewVelocity:Vector3.zero;
            float speed=new Vector2(velocity.x,velocity.z).magnitude;
            bool enabled=ownsEyes&&preferences.locomotionMotion&&!preferences.reduceMotion;
            bool walking=enabled&&!explorer.InputCaptured&&!explorer.IsFlying&&Cursor.lockState==CursorLockMode.Locked;
            var frame=motion.Step(walking?speed:0,walking?velocity.x:0,walking&&explorer.ViewSprinting,walking&&explorer.ViewGrounded&&!explorer.IsSliding,
                enabled,walking?explorer.ViewTurnRate.x:0,walking?explorer.ViewTurnRate.y:0,preferences.motionIntensity,explorer!=null&&explorer.IsPaused?0:Time.deltaTime);
            landing.Step(ownsEyes&&explorer.ViewGrounded,velocity.y,walking&&preferences.landingMotion&&!explorer.IsSliding,preferences.motionIntensity,explorer!=null&&explorer.IsPaused?0:Time.deltaTime);
            transform.localRotation=BaseRotation*Quaternion.Euler(ToUnity(frame.Angles));
            float dip=preferences.cameraMotion&&preferences.waterEffects?ImpactPulse*preferences.splashIntensity:0;
            transform.localPosition=restPosition+StanceOffset+ToUnity(frame.Position)+Vector3.up*landing.Offset+Vector3.down*(dip*.025f);
            view.fieldOfView=restFov+frame.FieldOfView+dip*.65f;
        }
        private void ResetMotionPose(){motion.Reset();landing.Reset();if(view!=null){transform.localPosition=restPosition+StanceOffset;transform.localRotation=BaseRotation;view.fieldOfView=restFov;}}
        private float Score(Source source)
        {float score=source.Strength;for(int i=0;i<FlareCount;i++)if(selected[i].Id==source.Id){score*=1.12f;break;}return score;}
        private void Insert(Source source)
        {
            int capacity=Mathf.Clamp(VisibleSourceBudget,1,CameraPresentation.MaxFlares)*2;
            int index=candidateCount;
            for(int i=0;i<candidateCount;i++)
                if(Score(source)>Score(candidates[i])||(Score(source)==Score(candidates[i])&&source.Id<candidates[i].Id)){index=i;break;}
            if(index>=capacity)return;
            int end=Mathf.Min(candidateCount,capacity-1);
            for(int i=end;i>index;i--)candidates[i]=candidates[i-1];
            candidates[index]=source;candidateCount=Mathf.Min(capacity,candidateCount+1);
        }
        private void ScanSources()
        {
            candidateCount=0;
            if(world!=null&&preferences.flareIntensity>0)
            {
                Vector3 eye=transform.position;
                var lamps=world.VisibleLuminaires;
                for(int i=0;i<lamps.Count;i++)
                {
                    var lamp=lamps[i];
                    if(lamp==null||lamp.source==null||!lamp.source.isActiveAndEnabled||!lamp.gameObject.activeInHierarchy)continue;
                    Vector3 light=lamp.transform.position,delta=light-eye;
                    float distance=delta.magnitude;if(distance>60)continue;
                    // Area sources point down. Do not show their back faces through ceilings.
                    float facing=Mathf.Clamp01(Vector3.Dot(lamp.transform.forward,-delta.normalized));
                    int id=lamp.GetInstanceID();Color color=lamp.source.color.linear;
                    // Unity supplies the Kelvin conversion in linear RGB already.
                    if(lamp.source.useColorTemperature)color*=Mathf.CorrelatedColorTemperatureToRGB(lamp.source.colorTemperature);
                    AddCandidate(id,light,light,light,color,lamp.lumens,distance,facing,false,lamp.width,lamp.depth);
                    if(world.CurrentLayout==null)continue;
                    float level=world.transform.position.y+world.CurrentLayout.Config.WaterLevel;
                    V3 surface,image;
                    if(!CameraPresentation.ReflectOnWater(ToCore(eye),ToCore(light),level,out surface,out image))continue;
                    Vector3 point=ToUnity(surface);float actualLevel;
                    if(!world.TryGetWater(point,out actualLevel)||Mathf.Abs(actualLevel-level)>.01f)continue;
                    Vector3 virtualLight=ToUnity(image);
                    float reflectedFacing=Mathf.Clamp01(Vector3.Dot(lamp.transform.forward,(point-light).normalized));
                    AddCandidate(~id,virtualLight,point,light,color,lamp.lumens,(virtualLight-eye).magnitude,reflectedFacing,true,lamp.width,lamp.depth);
                }
            }
            // At most eight direct rays, with one extra ray for each mirrored source; no per-light
            // raycasts across the whole loaded map. Preserve weights only for the same light ID.
            int previousCount=FlareCount;
            for(int i=0;i<previousCount;i++){oldIds[i]=selected[i].Id;oldWeights[i]=flarePositions[i].z;}
            FlareCount=0;
            int budget=Mathf.Clamp(VisibleSourceBudget,1,selected.Length);
            for(int i=0;i<candidateCount&&FlareCount<budget;i++)
            {
                var c=candidates[i];
                bool blocked=c.Reflected?Blocked(transform.position,c.Surface+Vector3.up*.025f)||Blocked(c.Surface+Vector3.up*.025f,c.Light):Blocked(transform.position,c.Light);
                if(blocked)continue;
                int slot=FlareCount++;selected[slot]=c;
                float weight=0;for(int j=0;j<previousCount;j++)if(oldIds[j]==c.Id){weight=oldWeights[j];break;}
                Vector3 uv=view.WorldToViewportPoint(c.Image);
                flarePositions[slot]=new Vector4(uv.x,uv.y,weight,c.Reflected?1:0);
                flareColors[slot]=new Vector4(c.Color.r,c.Color.g,c.Color.b,c.Scale);
            }
            for(int i=FlareCount;i<selected.Length;i++){flarePositions[i]=Vector4.zero;flareColors[i]=Vector4.zero;}
        }
        private void AddCandidate(int id,Vector3 image,Vector3 surface,Vector3 light,Color color,float lumens,float distance,float facing,bool reflected,float width,float depth)
        {
            Vector3 uv=view.WorldToViewportPoint(image);
            if(uv.z<=0||uv.x<-.08f||uv.x>1.08f||uv.y<-.08f||uv.y>1.08f)return;
            float strength=CameraPresentation.FlareStrength(lumens*Mathf.Max(0,SourceIntensityScale),distance,facing,reflected);
            if(reflected)strength*=CameraOptics.WaterGlint(ToCore(transform.position),ToCore(surface));
            if(strength<.008f)return;
            Insert(new Source{Id=id,Image=image,Surface=surface,Light=light,Color=color,Strength=strength,Scale=CameraOptics.SourceScale(width,depth,distance,restFov),Reflected=reflected});
        }
        private bool Blocked(Vector3 from,Vector3 to)
        {
            Vector3 delta=to-from;float distance=delta.magnitude;if(distance<.12f)return false;
            int count=Physics.RaycastNonAlloc(from,delta/distance,hits,distance-.1f,occlusionMask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(hits[i].collider!=null&&(explorer==null||!hits[i].collider.transform.IsChildOf(explorer.transform)))return true;
            return false;
        }
        private void OnWaterImpact(Vector3 position,float strength)
        {
            if(!isActiveAndEnabled||!preferences.waterEffects||preferences.splashIntensity<=0)return;
            float amount=strength*preferences.splashIntensity;
            ImpactPulse=Mathf.Max(ImpactPulse,strength);
            if(strength>.22f){Wetness=Mathf.Max(Wetness,strength);WetSeed=(WetSeed+1)%1024;WetElapsed=0;}
            EnsureSpray();if(spray==null)return;
            Color tint=Color.white;
            if(world!=null&&world.CurrentLayout!=null)
            {
                var room=world.RoomAtViewer();
                if(room!=null)
                {
                    var lighting=ZoneLighting.Get(ZoneLighting.Resolve(room,world.CurrentLayout.Config));V3 c=lighting.Color;
                    tint=Color.Lerp(Color.white,new Color(c.X,c.Y,c.Z),.65f)*Mathf.Clamp(lighting.Power*.45f,.025f,.75f);
                }
            }
            var random=new StableRandom(++spraySequence);int count=Mathf.RoundToInt(8+amount*48);
            for(int i=0;i<count;i++)
            {
                float a=random.Range(0,Mathf.PI*2),speed=random.Range(.35f,1.4f)*(1+amount);
                var particle=new ParticleSystem.EmitParams{
                    position=position+new Vector3(Mathf.Cos(a)*.12f,.04f,Mathf.Sin(a)*.12f),
                    velocity=new Vector3(Mathf.Cos(a)*speed,random.Range(.7f,3.2f)*(1+amount),Mathf.Sin(a)*speed),
                    startLifetime=random.Range(.3f,.7f),startSize=random.Range(.012f,.035f),startColor=tint};
                spray.Emit(particle,1);
            }
        }
        private void EnsureSpray()
        {
            if(spray!=null)return;
            Shader shader=Resources.Load<Shader>("PoolroomsSplash");if(shader==null||!shader.isSupported)return;
            var go=new GameObject("Poolrooms water contact spray");
            if(world!=null)go.transform.SetParent(world.transform,false);
            spray=go.AddComponent<ParticleSystem>();var main=spray.main;
            main.playOnAwake=false;main.loop=true;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.maxParticles=256;main.gravityModifier=.7f;main.startSpeed=0;
            var emission=spray.emission;emission.enabled=false;
            var shape=spray.shape;shape.enabled=false;
            var size=spray.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            spray.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            spray.Play();
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            sprayMaterial=new Material(shader){name="Poolrooms droplets (runtime)",hideFlags=HideFlags.DontSave};
            renderer.sharedMaterial=sprayMaterial;
        }
        public void ShiftOrigin(Vector3 shift)
        {
            for(int i=0;i<selected.Length;i++)
            {selected[i].Image+=shift;selected[i].Surface+=shift;selected[i].Light+=shift;}
            nextScan=0;
            if(spray!=null)
            {
                int count=spray.GetParticles(shiftedParticles);
                for(int i=0;i<count;i++)shiftedParticles[i].position+=shift;
                spray.SetParticles(shiftedParticles,count);
            }
        }
        public void ClearTransientEffects()
        {
            ResetMotionPose();Wetness=0;ImpactPulse=0;FlareCount=0;nextScan=0;
            for(int i=0;i<selected.Length;i++){flarePositions[i]=Vector4.zero;flareColors[i]=Vector4.zero;}
            if(spray!=null)spray.Clear();
            if(view!=null){transform.localPosition=restPosition+StanceOffset;view.fieldOfView=restFov;}
        }
        public void RefreshPreferences()
        {
            if(!preferences.locomotionMotion||preferences.reduceMotion||preferences.motionIntensity<=0)ResetMotionPose();
            if(!preferences.waterEffects||preferences.splashIntensity<=0)
            {Wetness=0;ImpactPulse=0;if(spray!=null)spray.Clear();}
            if(view!=null)ApplyMotion();
            nextScan=0;
        }
        public void SetFieldOfView(float value)
        {restFov=Mathf.Clamp(value,50,105);if(view==null)view=GetComponent<Camera>();if(view!=null)view.fieldOfView=restFov;}
        private void OnDisable()
        {
            if(subscribed!=null)subscribed.WaterImpact-=OnWaterImpact;subscribed=null;
            ClearTransientEffects();
            if(spray!=null){if(Application.isPlaying)Destroy(spray.gameObject);else DestroyImmediate(spray.gameObject);}
            if(sprayMaterial!=null){if(Application.isPlaying)Destroy(sprayMaterial);else DestroyImmediate(sprayMaterial);}
            spray=null;sprayMaterial=null;
        }
        private static V3 ToCore(Vector3 v){return new V3(v.x,v.y,v.z);}
        private static Vector3 ToUnity(V3 v){return new Vector3(v.X,v.Y,v.Z);}
    }
}
