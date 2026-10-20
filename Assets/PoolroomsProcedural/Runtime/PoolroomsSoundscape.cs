using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsSoundscape : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private PoolroomsGameSettings settings;
        private PoolroomsMusicPlayer music;
        private GameObject root;
        private readonly AudioSource[] beds=new AudioSource[4],events=new AudioSource[4];
        private readonly AudioLowPassFilter[] eventFilters=new AudioLowPassFilter[4];
        private readonly bool[] eerie=new bool[4];
        private readonly float[] eventGain=new float[4],eventFade={1,1,1,1};
        private readonly AudioClip[] steps=new AudioClip[3];
        private AudioClip creak,pipe,air,dash,cloth,land,handling,softImpact,hardImpact;
        private readonly AtmosphereDirector director=new AtmosphereDirector();
        private readonly RaycastHit[] hits=new RaycastHit[12];
        private StableRandom random=new StableRandom(0xA11CEu);
        private float fear,outdoor,duck=1,nextStep,previousY,nextImpact,nextProbe,submergedMix=1;
        private int remainingSteps;
        private Vector3 stepPosition;
        private bool wasDashing,wasGrounded,positioned,paused;
        private SoundscapeMode previousMode;
        private SoundscapePreferences Options {get{return settings.Preferences.soundscape;}}
        public float AmbienceGain {get{return duck;}}
        public int SourceBudget {get{return ImmersiveAudio.AdditionalSourceBudget;}}
        private void Start()
        {
            explorer=GetComponent<PoolroomsExplorer>();settings=GetComponent<PoolroomsGameSettings>();if(settings==null)settings=gameObject.AddComponent<PoolroomsGameSettings>();settings.Initialize();
            music=GetComponent<PoolroomsMusicPlayer>();
            root=new GameObject("Layered soundscape");if(explorer.world!=null)root.transform.SetParent(explorer.world.transform,false);
            string[] loops={"QuietIndoorLoop","GardenWindLoop","UneasyAirLoop","FlightWindLoop"};
            for(int i=0;i<4;i++)
            {
                beds[i]=Create("Environment layer "+i,false);beds[i].clip=Load(loops[i]);beds[i].loop=true;if(beds[i].clip!=null)beds[i].Play();
                events[i]=Create("Spatial detail voice "+i,true);eventFilters[i]=events[i].gameObject.AddComponent<AudioLowPassFilter>();eventFilters[i].cutoffFrequency=18000;
            }
            for(int i=0;i<3;i++)steps[i]=Load("RemoteStep"+i);
            creak=Load("DistantCreak");pipe=Load("PipeResonance");air=Load("AirMovement");dash=Load("DashAir");cloth=Load("ClothMovement");land=Load("LandContact");handling=Load("ObjectHandling");softImpact=Load("SoftImpact");hardImpact=Load("HardImpact");
            explorer.Relocated+=Relocated;explorer.OriginShifted+=Shift;
        }
        private static AudioClip Load(string name){return Resources.Load<AudioClip>("PoolroomsAudio/Horizonte/"+name);}
        private AudioSource Create(string label,bool spatial)
        {
            var go=new GameObject(label);go.transform.SetParent(root.transform,false);var source=go.AddComponent<AudioSource>();
            source.playOnAwake=false;source.volume=0;source.dopplerLevel=0;source.spatialBlend=spatial?1:0;source.priority=spatial?180:210;
            source.minDistance=2;source.maxDistance=40;source.rolloffMode=AudioRolloffMode.Logarithmic;source.reverbZoneMix=spatial?.85f:.25f;return source;
        }
        private void Update()
        {
            if(root==null||settings==null||explorer==null)return;
            bool pause=explorer.IsPaused||AudioListener.pause||Time.deltaTime<=0;
            if(pause!=paused){paused=pause;foreach(var s in beds)if(paused)s.Pause();else s.UnPause();foreach(var s in events)if(paused)s.Pause();else s.UnPause();}
            if(paused)return;
            float dt=Time.deltaTime;var world=explorer.world;
            bool ready=explorer.isActiveAndEnabled&&explorer.IsReady&&world!=null&&world.ViewerReady;
            if(Options.mode!=previousMode){remainingSteps=0;director.Reset();previousMode=Options.mode;}
            float targetFear=ready&&Options.mode==SoundscapeMode.Uneasy?Options.tension:0;
            fear=ImmersiveAudio.Approach(fear,targetFear,dt,.4f);
            float musicTarget=Options.duckWithMusic&&music!=null&&music.AudibleGain>.01f?.48f:1;
            duck=ImmersiveAudio.Approach(duck,musicTarget,dt,1.2f);
            bool outside=ready&&world.Terrain!=null&&world.Terrain.IsInside&&!world.Terrain.InsideHouse;
            Room room=ready?world.RoomAtViewer():null;outdoor=ImmersiveAudio.Approach(outdoor,ImmersiveAudio.Outdoor(room,outside),dt,.6f);
            if(ready&&Time.time>=nextProbe)
            {nextProbe=Time.time+.25f;submergedMix=world.TryGetWater(explorer.Aim.position,out float level)&&explorer.Aim.position.y<level-.08f? .25f:1;}
            float common=ready?settings.EffectiveMasterVolume*settings.Preferences.environmentVolume*Options.detail*duck*submergedMix:0;
            for(int i=0;i<4;i++)
            {
                float target=i==0?.11f*(1-outdoor):i==1?.13f*outdoor:i==2?.13f*fear:Options.movementSounds&&explorer.IsFlying?Mathf.Clamp01(explorer.ViewVelocity.magnitude/30)*.11f:0;
                beds[i].volume=settings.EffectiveMasterVolume<=0?0:ImmersiveAudio.Approach(beds[i].volume,common*target,dt,1.2f);
                if(ready&&!beds[i].isPlaying&&beds[i].clip!=null)beds[i].Play();
                bool allowed=ready&&(!eerie[i]||Options.mode==SoundscapeMode.Uneasy&&Options.tension>0&&Options.detail>0);
                eventFade[i]=Mathf.MoveTowards(eventFade[i],allowed?1:0,dt*2);
                events[i].volume=eventGain[i]*eventFade[i]*settings.EffectiveMasterVolume*(eerie[i]?settings.Preferences.environmentVolume*duck:settings.Preferences.footstepVolume);
                if(!allowed&&eventFade[i]<=0)events[i].Stop();
            }
            if(Options.mode!=SoundscapeMode.Uneasy||Options.tension<=0||Options.detail<=0||!Options.distantSteps||!ready)remainingSteps=0;
            if(!ready){positioned=false;remainingSteps=0;director.Reset();return;}
            var cue=director.Step(dt,Options.mode,Options.tension,Options.distantSteps);
            if(cue!=AtmosphereCue.None&&Options.detail>0)Distant(cue);
            if(remainingSteps>0)nextStep-=dt;
            if(remainingSteps>0&&nextStep<=0)
            {
                nextStep=random.Range(.65f,.95f);remainingSteps--;
                if(Vector3.Distance(stepPosition,explorer.Aim.position)>=8)Detail(steps[random.Range(3)],stepPosition,.21f*Options.tension*Options.detail,220,true);
                stepPosition+=explorer.transform.right*random.Range(-.3f,.3f)-explorer.transform.forward*.45f;
            }
            if(Options.movementSounds)
            {
                bool grounded=explorer.ViewGrounded,fly=explorer.IsFlying;
                if(positioned&&!fly&&explorer.Vehicle==null&&!explorer.IsAquaticRiding)
                {
                    if(!wasDashing&&explorer.IsDashing)Detail(dash,explorer.Aim.position,.12f,130,false,true);
                    if(!grounded&&explorer.ViewVelocity.y-previousY>2)Detail(cloth,transform.position+Vector3.up,.07f,150,false,true);
                    if(!wasGrounded&&grounded&&previousY<-.8f)Detail(land,transform.position,.08f+Mathf.Min(.12f,-previousY*.012f),135);
                }
                wasGrounded=grounded;wasDashing=explorer.IsDashing;previousY=explorer.ViewVelocity.y;positioned=true;
            }
            else positioned=false;
        }
        private void Distant(AtmosphereCue cue)
        {
            Vector3 ears=explorer.Aim.position;
            Vector3 candidate=ears-explorer.transform.forward*random.Range(9,13)+explorer.transform.right*random.Range(-5,5);
            int count=Physics.RaycastNonAlloc(candidate+Vector3.up*3,Vector3.down,hits,12,~0,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return;
            float best=float.MaxValue;bool found=false;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform)&&hits[i].normal.y>.6f&&hits[i].distance<best){best=hits[i].distance;candidate=hits[i].point+Vector3.up*.08f;found=true;}
            if(!found||Vector3.Distance(candidate,ears)<8)return;
            if(cue==AtmosphereCue.RemoteSteps){stepPosition=candidate;remainingSteps=random.Range(2,5);nextStep=1.2f;return;}
            Detail(cue==AtmosphereCue.WoodCreak?creak:cue==AtmosphereCue.PipeResonance?pipe:air,candidate,.15f*Options.tension*Options.detail,225,true);
        }
        private void Detail(AudioClip clip,Vector3 position,float gain,int priority,bool unsettling=false,bool atPlayer=false)
        {
            if(clip==null||root==null||explorer.IsPaused||settings.EffectiveMasterVolume<=0)return;
            if(unsettling&&Options.mode!=SoundscapeMode.Uneasy)return;
            int chosen=-1;
            for(int i=0;i<4;i++)if(!events[i].isPlaying){chosen=i;break;}else if(events[i].priority>priority&&(chosen<0||events[i].priority>events[chosen].priority))chosen=i;
            if(chosen<0)return;
            var source=events[chosen];source.Stop();source.transform.position=position;source.clip=clip;source.priority=priority;source.pitch=random.Range(.96f,1.04f);eerie[chosen]=unsettling;
            source.spatialBlend=atPlayer?0:settings.Preferences.spatialAmbience?1:0;
            bool blocked=false;
            if(!atPlayer&&settings.Preferences.soundOcclusion)
            {
                Vector3 d=position-explorer.Aim.position;float length=d.magnitude;
                int count=length>.2f?Physics.RaycastNonAlloc(explorer.Aim.position,d.normalized,hits,length-.15f,~0,QueryTriggerInteraction.Ignore):0;
                blocked=count==hits.Length;
                for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform))blocked=true;
            }
            eventFilters[chosen].cutoffFrequency=blocked?2100:submergedMix<.5f?900:16000;
            eventGain[chosen]=ImmersiveAudio.Transient(gain,Options.dynamicRange)*(blocked?.48f:1);eventFade[chosen]=1;
            source.volume=eventGain[chosen]*settings.EffectiveMasterVolume*(unsettling?settings.Preferences.environmentVolume*duck:settings.Preferences.footstepVolume);
            source.Play();
        }
        public void Handling(Vector3 position,bool soft)
        {if(settings!=null&&Options.objectSounds)Detail(soft?handling:cloth,position,.055f,170);}
        public void Impact(Vector3 position,float speed,float mass,PickupSurface surface)
        {
            if(settings==null||!Options.objectSounds||Time.time<nextImpact||explorer==null||!explorer.IsReady||explorer.IsPaused)return;
            float gain=ImmersiveAudio.ImpactGain(speed,mass,surface!=PickupSurface.Rigid);if(gain<=0||Vector3.Distance(position,explorer.Aim.position)>24)return;
            nextImpact=Time.time+.07f;Detail(surface==PickupSurface.Rigid?hardImpact:softImpact,position,gain,155);
        }
        private void Relocated()
        {director.Reset();remainingSteps=0;positioned=false;foreach(var s in events)if(s!=null)s.Stop();}
        private void Shift(Vector3 offset)
        {stepPosition+=offset;foreach(var s in events)if(s!=null)s.transform.position+=offset;}
        private void OnDisable()
        {if(root!=null){foreach(var s in beds)if(s!=null)s.Stop();foreach(var s in events)if(s!=null)s.Stop();}positioned=false;remainingSteps=0;director.Reset();}
        private void OnDestroy()
        {if(explorer!=null){explorer.Relocated-=Relocated;explorer.OriginShifted-=Shift;}if(root!=null)Destroy(root);}
    }
}
