using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsAmbientAudio : MonoBehaviour
    {
        public PoolroomsExplorer explorer;
        public PoolroomsWorld world;
        [Header("Sonido tranquilo — niveles locales")]
        [Range(0,1)] public float masterVolume=.85f;
        [Range(0,1)] public float environmentVolume=.8f,footstepVolume=.65f,waterVolume=.75f;
        public bool occasionalDrips=true,roomReverb=true;
        public bool spatialAmbience=true,soundOcclusion=true;
        private bool ventilationBlocked,waterBlocked;
        private OccludedSound ventilationTransmission=new OccludedSound(1,18000),waterTransmission=new OccludedSound(1,18000);
        public LayerMask surfaceMask=~0;
        private GameObject audioRoot;
        private AudioSource ventilation,waterBed;
        private AudioReverbZone reverb;
        private readonly AudioSource[] voices=new AudioSource[4];
        private readonly AudioLowPassFilter[] filters=new AudioLowPassFilter[8];
        private readonly AudioSource[] slideVoices=new AudioSource[2];
        private readonly AudioClip[] slideClips=new AudioClip[4];
        private readonly SlideAudioEnvelope slideEnvelope=new SlideAudioEnvelope();
        private int slideVoice;
        private float slideBlend=1,nextSurfaceUpdate;
        private readonly AudioClip[] tile=new AudioClip[3],metal=new AudioClip[3],wetTile=new AudioClip[3],wade=new AudioClip[3],drips=new AudioClip[3],splashes=new AudioClip[2];
        private readonly int[] lastVariants={-1,-1,-1,-1,-1,-1,-1,-1,-1};
        private readonly RaycastHit[] hits=new RaycastHit[8];
        private readonly List<Material> materials=new List<Material>(8);
        private readonly AudioStrideTracker stride=new AudioStrideTracker();
        private CharacterController body;
        private PoolroomsSoundscape soundscape;
        private PoolroomsGameSettings settings;
        private readonly float[] voiceGain=new float[4];
        private readonly int[] voiceBus=new int[4];
        private readonly AudioClip[] soft=new AudioClip[3],grass=new AudioClip[3],wood=new AudioClip[3];
        private PoolroomsExplorer subscribed;
        private Layout cachedLayout;
        private Room cachedRoom;
        private WaterRegion[] regions;
        private AcousticLook target,smoothed;
        private StableRandom random=new StableRandom(0xA0D10u);
        private Vector3 previousPosition;
        private bool positioned,ownedPause;
        private float nextAcousticUpdate,dripIn=8,wetFeet,splashCooldown,cutoff=18000;
        public int SourceBudget { get { return 8; } }
        public float SlideGain { get { return slideEnvelope.Gain; } }

        private void OnEnable()
        {
            if(!Application.isPlaying)return;
            BindExplorer();body=GetComponent<CharacterController>();CreateAudio();ResetMotion();
        }
        private void BindExplorer()
        {
            if(explorer==null)explorer=GetComponent<PoolroomsExplorer>();
            if(world==null&&explorer!=null)world=explorer.world;
            if(subscribed==explorer)return;
            if(subscribed!=null)subscribed.WaterImpact-=OnWaterImpact;
            subscribed=explorer;if(subscribed!=null)subscribed.WaterImpact+=OnWaterImpact;
        }
        private void CreateAudio()
        {
            audioRoot=new GameObject("Poolrooms sound (runtime)");
            if(world!=null)audioRoot.transform.SetParent(world.transform,false);
            ventilation=Source("Soft ventilation",false,0);waterBed=Source("Pool circulation",false,1);
            ventilation.clip=Load("VentilationLoop");waterBed.clip=Load("WaterLoop");
            if(ventilation.clip==null||waterBed.clip==null)
                Debug.LogWarning("Poolrooms: faltan sonidos. Copia la carpeta Runtime/Resources/PoolroomsAudio completa.",this);
            ventilation.loop=waterBed.loop=true;ventilation.volume=waterBed.volume=0;
            if(ventilation.clip!=null)ventilation.Play();if(waterBed.clip!=null)waterBed.Play();
            for(int i=0;i<voices.Length;i++)voices[i]=Source("Contact voice "+i,true,i+2);
            for(int i=0;i<slideVoices.Length;i++)
            {slideVoices[i]=Source("Continuous slide friction "+i,true,i+6);slideVoices[i].loop=true;slideVoices[i].volume=0;}
            string[] slideNames={"SlideTileLoop","SlideMetalLoop","SlideSoftLoop","SlideWetLoop"};
            for(int i=0;i<slideNames.Length;i++)slideClips[i]=Load(slideNames[i]);
            LoadSet(tile,"Tile");LoadSet(metal,"Metal");LoadSet(wetTile,"WetTile");LoadSet(wade,"Wade");LoadSet(drips,"Drip");LoadSet(splashes,"Splash");
            LoadSet(soft,"Horizonte/SoftStep");LoadSet(grass,"Horizonte/GrassStep");LoadSet(wood,"Horizonte/WoodStep");
            var zone=new GameObject("Room acoustics");zone.transform.SetParent(audioRoot.transform,false);
            reverb=zone.AddComponent<AudioReverbZone>();reverb.reverbPreset=AudioReverbPreset.User;
            reverb.minDistance=.5f;reverb.maxDistance=1.5f;reverb.room=-1800;reverb.roomHF=-1000;reverb.roomLF=-300;
            reverb.reflections=-1900;reverb.reverb=-1400;reverb.HFReference=5000;reverb.LFReference=250;
            reverb.diffusion=85;reverb.density=90;reverb.enabled=false;
            smoothed=target=AmbientAudioModel.ForRoom(null);
        }
        private AudioSource Source(string name,bool positional,int filterIndex)
        {
            var go=new GameObject(name);go.transform.SetParent(audioRoot.transform,false);
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.dopplerLevel=0;
            source.spatialBlend=positional?.85f:0;source.rolloffMode=AudioRolloffMode.Logarithmic;
            source.minDistance=1.8f;source.maxDistance=22;source.priority=positional?160:200;
            source.reverbZoneMix=positional?.8f:.15f;source.ignoreListenerPause=false;
            filters[filterIndex]=go.AddComponent<AudioLowPassFilter>();filters[filterIndex].lowpassResonanceQ=1;
            filters[filterIndex].cutoffFrequency=18000;filters[filterIndex].enabled=false;
            return source;
        }
        private static AudioClip Load(string name){return Resources.Load<AudioClip>("PoolroomsAudio/"+name);}
        private static void LoadSet(AudioClip[] clips,string prefix){for(int i=0;i<clips.Length;i++)clips[i]=Load(prefix+i);}

        private void LateUpdate()
        {
            if(audioRoot==null)return;BindExplorer();
            if(explorer==null)return;
            if(soundscape==null)soundscape=GetComponent<PoolroomsSoundscape>();
            if(settings==null)settings=GetComponent<PoolroomsGameSettings>();
            for(int i=0;i<voices.Length;i++)if(voices[i]!=null)voices[i].volume=voiceGain[i]*Unit(masterVolume)*BusGain(voiceBus[i]);
            bool paused=explorer.IsPaused||Time.deltaTime<=0||AudioListener.pause;
            SetOwnedPause(paused);if(paused){previousPosition=transform.position;return;}
            float dt=Time.deltaTime,k=1-Mathf.Exp(-dt*2.5f);
            bool ready=explorer.isActiveAndEnabled&&explorer.IsReady&&world!=null&&world.isActiveAndEnabled&&world.CurrentLayout!=null;
            if(!ready)
            {
                ventilation.volume=masterVolume<=0?0:Mathf.Lerp(ventilation.volume,0,k);waterBed.volume=masterVolume<=0?0:Mathf.Lerp(waterBed.volume,0,k);
                reverb.enabled=false;positioned=false;stride.Reset();StopSlideAudio();return;
            }
            Transform ears=explorer.Aim;
            if(!ventilation.isPlaying&&ventilation.clip!=null)ventilation.Play();
            if(!waterBed.isPlaying&&waterBed.clip!=null)waterBed.Play();
            reverb.transform.position=ears.position;
            bool feetWet=false,swimming=false,underwater=false;float level;
            if(world.TryGetWater(transform.position,out level))
            {
                feetWet=body!=null&&body.bounds.min.y<level-.015f;
                swimming=transform.position.y<level-.75f;
                underwater=ears.position.y<level-.08f;
            }
            wetFeet=Mathf.Max(0,wetFeet-dt);if(feetWet)wetFeet=2.8f;
            splashCooldown=Mathf.Max(0,splashCooldown-dt);
            if(Time.time>=nextAcousticUpdate)
            {
                nextAcousticUpdate=Time.time+.25f;UpdateRoom();UpdateSpatialAnchors(ears.position);
                smoothed=AmbientAudioModel.Blend(smoothed,target,.45f);
                reverb.decayTime=smoothed.Decay;reverb.decayHFRatio=underwater?.35f:smoothed.HighDecay;
                reverb.reflectionsDelay=smoothed.ReflectionsDelay;reverb.reverbDelay=smoothed.ReverbDelay;
            }
            bool skyPaths=cachedRoom?.Plan?.Quiet?.Sky63!=null||cachedRoom?.Plan?.Forest39!=null;
            bool useReverb=!skyPaths&&roomReverb&&(world.Terrain==null||!world.Terrain.IsInside||world.Terrain.InsideHouse);
            if(reverb.enabled!=useReverb)reverb.enabled=useReverb;
            ventilationTransmission=SpatialAudioModel.Step(ventilationTransmission,ventilationBlocked,underwater,dt);
            waterTransmission=SpatialAudioModel.Step(waterTransmission,waterBlocked,underwater,dt);
            ventilation.spatialBlend=waterBed.spatialBlend=spatialAmbience?1:0;
            ventilation.volume=masterVolume<=0?0:Mathf.Lerp(ventilation.volume,ventilationTransmission.Gain*smoothed.Ventilation*(skyPaths?0:1)*Unit(masterVolume)*Unit(environmentVolume)*(soundscape!=null?soundscape.AmbienceGain:1),k);
            waterBed.volume=masterVolume<=0?0:Mathf.Lerp(waterBed.volume,waterTransmission.Gain*smoothed.Water*Unit(masterVolume)*Unit(environmentVolume)*Unit(waterVolume)*(soundscape!=null?soundscape.AmbienceGain:1),k);
            float nextCutoff=Mathf.Lerp(cutoff,underwater?900:18000,1-Mathf.Exp(-dt*6));
            if(Mathf.Abs(nextCutoff-cutoff)>20)
            {
                cutoff=nextCutoff;foreach(var filter in filters){filter.cutoffFrequency=cutoff;filter.enabled=cutoff<17500;}
            }
            ApplyTransmission(0,ventilationTransmission);ApplyTransmission(1,waterTransmission);
            Vector3 position=transform.position;
            if(!positioned){previousPosition=position;positioned=true;}
            Vector3 travel=position-previousPosition;previousPosition=position;travel.y=0;
            bool grounded=body!=null&&body.enabled&&body.isGrounded;
            UpdateSlideAudio(position,travel.magnitude,dt,grounded,swimming,feetWet||wetFeet>0);
            if(stride.Sample(travel.magnitude,!explorer.IsFlying&&grounded,!explorer.IsFlying&&swimming,feetWet||wetFeet>0,dt,explorer.IsSliding))
            {
                bool outside=world.Terrain!=null&&world.Terrain.IsInside&&!world.Terrain.InsideHouse;
                bool inHouse=world.Terrain!=null&&world.Terrain.IsInside&&world.Terrain.InsideHouse;
                bool padded=cachedRoom!=null&&(cachedRoom.Environment==LiminalEnvironment.IndoorPlay||cachedRoom.Environment==LiminalEnvironment.SoftPlay||cachedRoom.Environment==LiminalEnvironment.Office||cachedRoom.Environment==LiminalEnvironment.Hotel);
                int category=swimming?3:feetWet||wetFeet>0?2:IsMetalGround()?1:outside?7:inHouse||skyPaths?8:padded?6:0;
                var clips=category==3?wade:category==2?wetTile:category==1?metal:category==7?grass:category==8?wood:category==6?soft:tile;
                float gain=1;
                if(category==0&&cachedRoom!=null&&(cachedRoom.Environment==LiminalEnvironment.IndoorPlay||cachedRoom.Environment==LiminalEnvironment.SoftPlay))gain*=.55f;
                if(world.Terrain!=null&&world.Terrain.IsInside)gain*=world.Terrain.InsideHouse?.6f:.35f;
                Play(Variation(clips,category),position+Vector3.up*.08f,gain*.5f,140,category==3?1:0);
            }
            dripIn-=dt;
            if(dripIn<=0){dripIn=random.Range(7,18);if(occasionalDrips&&!underwater)TryDrip();}
        }
        private void UpdateSlideAudio(Vector3 position,float distance,float dt,bool grounded,bool swimming,bool wet)
        {
            slideEnvelope.Step(dt,distance,explorer.IsSliding,grounded,swimming,explorer.IsFlying);
            if(explorer.IsSliding&&grounded&&Time.time>=nextSurfaceUpdate)
            {
                nextSurfaceUpdate=Time.time+.2f;
                SlideContactSound surface=wet?SlideContactSound.Wet:IsMetalGround()?SlideContactSound.Metal:
                    world.Terrain!=null&&world.Terrain.IsInside||cachedRoom!=null&&
                    (cachedRoom.Environment==LiminalEnvironment.IndoorPlay||cachedRoom.Environment==LiminalEnvironment.SoftPlay||cachedRoom.Environment==LiminalEnvironment.Office||cachedRoom.Environment==LiminalEnvironment.Hotel)?SlideContactSound.Soft:SlideContactSound.Tile;
                if(slideClips[(int)surface]!=null&&slideClips[(int)surface]!=slideVoices[slideVoice].clip)
                {
                    slideVoice=1-slideVoice;slideBlend=0;
                    var incoming=slideVoices[slideVoice];incoming.Stop();incoming.clip=slideClips[(int)surface];incoming.volume=0;incoming.Play();
                }
            }
            slideBlend=Mathf.MoveTowards(slideBlend,1,dt*8);
            for(int i=0;i<slideVoices.Length;i++)
            {
                var source=slideVoices[i];source.transform.position=position+Vector3.up*.1f;
                source.pitch=slideEnvelope.Pitch;
                float mix=i==slideVoice?slideBlend:1-slideBlend;
                source.volume=slideEnvelope.Gain*mix*Unit(masterVolume)*Unit(footstepVolume);
                if(source.volume>.0001f&&!source.isPlaying&&source.clip!=null)source.Play();
                else if(source.volume<=.0001f&&source.isPlaying)source.Stop();
            }
        }
        private void UpdateSpatialAnchors(Vector3 ears)
        {
            ventilationBlocked=waterBlocked=false;
            if(cachedRoom==null||world==null||world.Terrain!=null&&world.Terrain.IsInside)
            {ventilation.transform.position=waterBed.transform.position=ears;return;}
            var origin=world.RoomWorldPosition(cachedRoom);
            var vent=origin+new Vector3(cachedRoom.Width*.5f-.45f,Mathf.Min(cachedRoom.Height-.6f,3.2f),-cachedRoom.Depth*.28f);
            ventilation.transform.position=vent;ventilation.minDistance=6;ventilation.maxDistance=90;ventilation.spread=55;
            var listener=ears-origin;bool found;
            var anchor=SpatialAudioModel.WaterAnchor(regions,new V3(listener.x,listener.y,listener.z),out found);
            var wet=found?origin+new Vector3(anchor.X,anchor.Y,anchor.Z):ears;
            waterBed.transform.position=wet;waterBed.minDistance=5;waterBed.maxDistance=75;waterBed.spread=75;
            if(!spatialAmbience||!soundOcclusion)return;
            ventilationBlocked=Occluded(ears,vent);waterBlocked=found&&Occluded(ears,wet);
        }
        private bool Occluded(Vector3 ears,Vector3 point)
        {
            var delta=point-ears;float distance=delta.magnitude;if(distance<.2f)return false;
            int count=Physics.RaycastNonAlloc(ears,delta/distance,hits,distance-.12f,surfaceMask,QueryTriggerInteraction.Ignore);
            if(count==hits.Length)return true;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform))return true;
            return false;
        }
        private void ApplyTransmission(int index,OccludedSound sound)
        {
            var filter=filters[index];if(filter==null)return;
            if(Mathf.Abs(filter.cutoffFrequency-sound.Cutoff)>10)filter.cutoffFrequency=sound.Cutoff;
            filter.enabled=sound.Cutoff<17500;
        }
        private void UpdateRoom()
        {
            if(world.Terrain!=null&&world.Terrain.IsInside)
            {
                target=world.Terrain.InsideHouse?new AcousticLook(.025f,0,.65f,.4f,.012f,.015f):new AcousticLook(0,0,.5f,.3f,.01f,.012f);
                cachedRoom=null;regions=null;return;
            }
            Room room=world.RoomAtViewer();Layout layout=world.CurrentLayout;
            if(room==null){target=AmbientAudioModel.ForRoom(null);return;}
            if(layout!=cachedLayout||room!=cachedRoom)
            {
                if(layout!=cachedLayout)random=new StableRandom(StableRandom.Hash(layout.Config.Seed,0,0,0xA0D10u));
                cachedRoom=room;cachedLayout=layout;regions=PoolroomsWorld.GetWaterRegions(room,layout.Config);
                dripIn=random.Range(5,13);
            }
            var current=AmbientAudioModel.ForRoom(room);target=current;
            Vector3 local=transform.position-world.RoomWorldPosition(room);
            float best=0;
            for(int d=0;d<4;d++)if(room.HasExit(d))
            {
                Room neighbour=layout.Neighbour(room,d);if(neighbour==null)continue;
                bool z=d%2==0;float distance=local.x*Directions.DX[d]+local.z*Directions.DZ[d];
                float blend=AmbientAudioModel.PassageBlend(distance,z?room.Depth:room.Width,z?neighbour.Depth:neighbour.Width,layout.Config.CellSize);
                if(blend>best){best=blend;target=AmbientAudioModel.Blend(current,AmbientAudioModel.ForRoom(neighbour),blend);}
            }
        }
        private AudioClip Variation(AudioClip[] clips,int category)
        {
            int index=random.Range(clips.Length);
            if(index==lastVariants[category])index=(index+1)%clips.Length;
            lastVariants[category]=index;return clips[index];
        }
        private float BusGain(int bus){return bus==0?Unit(footstepVolume):bus==1?Unit(waterVolume):Unit(environmentVolume)*Unit(waterVolume)*(soundscape!=null?soundscape.AmbienceGain:1);}
        private void Play(AudioClip clip,Vector3 position,float volume,int importance,int bus=0)
        {
            float baseGain=ImmersiveAudio.Transient(volume,settings!=null?settings.Preferences.soundscape.dynamicRange:.55f);
            volume=baseGain*Unit(masterVolume)*BusGain(bus);if(clip==null||volume<=.0001f)return;
            AudioSource chosen=null;
            foreach(var voice in voices)
            {
                if(!voice.isPlaying){chosen=voice;break;}
                if(chosen==null||voice.priority>chosen.priority)chosen=voice;
            }
            if(chosen==null||(chosen.isPlaying&&chosen.priority<importance))return;
            for(int i=0;i<voices.Length;i++)if(voices[i]==chosen){voiceGain[i]=baseGain;voiceBus[i]=bus;}
            chosen.Stop();chosen.transform.position=position;chosen.clip=clip;chosen.volume=volume;chosen.spatialBlend=spatialAmbience?.85f:0;
            chosen.pitch=random.Range(.95f,1.045f);chosen.priority=importance;chosen.Play();
        }
        private void OnWaterImpact(Vector3 position,float strength)
        {
            if(audioRoot==null||ownedPause||!isActiveAndEnabled||splashCooldown>0||explorer==null||!explorer.IsReady||explorer.IsPaused)return;
            splashCooldown=.18f;
            Play(strength>.22f?Variation(splashes,5):Variation(wade,3),position,
                AmbientAudioModel.SplashVolume(strength),110,1);
        }
        private void TryDrip()
        {
            if(cachedRoom==null||regions==null||regions.Length==0||target.Water<=0)return;
            WaterRegion region=regions[random.Range(regions.Length)];
            Vector3 point=world.RoomWorldPosition(cachedRoom)+new Vector3(region.CenterX+random.Range(-.2f,.2f)*region.Width,
                region.Level+.04f,region.CenterZ+random.Range(-.2f,.2f)*region.Depth);
            float level;if(!world.TryGetWater(point,out level)||(point-transform.position).sqrMagnitude>225)return;
            Vector3 ears=explorer.Aim.position;
            Vector3 delta=point-ears;float distance=delta.magnitude;if(distance<.1f)return;
            int count=Physics.RaycastNonAlloc(ears,delta/distance,hits,distance-.07f,surfaceMask,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform))return;
            Play(Variation(drips,4),point,.2f,220,2);
        }
        private bool IsMetalGround()
        {
            if(body==null||world.profile==null||world.profile.metal==null)return false;
            Vector3 origin=transform.position;origin.y=body.bounds.min.y+.18f;
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,hits,.5f,surfaceMask,QueryTriggerInteraction.Ignore);
            int nearest=-1;float distance=float.MaxValue;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(transform)&&hits[i].distance<distance)
            {nearest=i;distance=hits[i].distance;}
            if(nearest<0)return false;
            var hit=hits[nearest];var collider=hit.collider as MeshCollider;
            var renderer=hit.collider.GetComponent<MeshRenderer>();
            if(collider==null||collider.sharedMesh==null||renderer==null||hit.triangleIndex<0)return false;
            Mesh mesh=collider.sharedMesh;int triangle=hit.triangleIndex*3,total=0;
            materials.Clear();renderer.GetSharedMaterials(materials);
            for(int i=0;i<mesh.subMeshCount;i++)
            {
                total+=mesh.GetSubMesh(i).indexCount;
                if(triangle<total)return i<materials.Count&&materials[i]==world.profile.metal;
            }
            return false;
        }
        private void SetOwnedPause(bool value)
        {
            if(ownedPause==value)return;ownedPause=value;
            if(value){ventilation.Pause();waterBed.Pause();foreach(var voice in voices)voice.Pause();foreach(var voice in slideVoices)voice.Pause();}
            else{ventilation.UnPause();waterBed.UnPause();foreach(var voice in voices)voice.UnPause();foreach(var voice in slideVoices)voice.UnPause();}
        }
        public void ShiftOrigin(Vector3 shift)
        {
            previousPosition+=shift;
            if(ventilation!=null)ventilation.transform.position+=shift;
            if(waterBed!=null)waterBed.transform.position+=shift;
            if(reverb!=null)reverb.transform.position+=shift;
            foreach(var voice in voices)if(voice!=null)voice.transform.position+=shift;
            foreach(var voice in slideVoices)if(voice!=null)voice.transform.position+=shift;
        }
        public void ResetMotion()
        {
            previousPosition=transform.position;positioned=false;stride.Reset();wetFeet=0;splashCooldown=0;
            cachedRoom=null;cachedLayout=null;regions=null;ventilationBlocked=waterBlocked=false;
            ventilationTransmission=waterTransmission=new OccludedSound(1,18000);nextAcousticUpdate=0;dripIn=8;
            foreach(var voice in voices)if(voice!=null)voice.Stop();
            StopSlideAudio();
        }
        private void StopSlideAudio()
        {slideEnvelope.Reset();nextSurfaceUpdate=0;slideBlend=1;foreach(var voice in slideVoices)if(voice!=null){voice.Stop();voice.volume=0;}}
        private void OnDisable()
        {
            if(subscribed!=null)subscribed.WaterImpact-=OnWaterImpact;subscribed=null;
            if(audioRoot!=null){audioRoot.SetActive(false);if(Application.isPlaying)Destroy(audioRoot);else DestroyImmediate(audioRoot);}
            audioRoot=null;ventilation=null;waterBed=null;reverb=null;ownedPause=false;ResetMotion();
        }
        private static float Unit(float value){return CameraPresentation.Unit(value);}
    }
}
