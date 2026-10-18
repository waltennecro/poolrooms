using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // One bounded emitter and one audio voice for the entire streamed district.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsWorld))]
    public sealed class PoolroomsRainWeather : MonoBehaviour
    {
        private PoolroomsWorld world;
        private GameObject root;
        private ParticleSystem rain;
        private Material material;
        private AudioSource audioSource;
        private AudioLowPassFilter lowpass;
        private PoolroomsExplorer explorer;
        private PoolroomsGameSettings settings;
        private PoolroomsAmbientAudio mixer;
        private Transform boundViewer;
        private Vector3 previousPosition;
        private float accumulator;
        private StableRandom random=new StableRandom(0x240B01u);
        public int ParticleBudget {get{return 640;}}
        private void Start()
        {
            if(!Application.isPlaying)return;world=GetComponent<PoolroomsWorld>();
            root=new GameObject("Rain houses weather");root.transform.SetParent(transform,false);
            rain=root.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=rain.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=ParticleBudget;
            main.startSpeed=0;main.startSize=.018f;main.startColor=new Color(.7f,.8f,.86f,.28f);main.gravityModifier=0;
            var emission=rain.emission;emission.enabled=false;var shape=rain.shape;shape.enabled=false;
            var fade=rain.colorOverLifetime;fade.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(1,.8f),new GradientAlphaKey(0,1)});fade.color=gradient;
            var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2;renderer.velocityScale=.018f;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var pipeline=GraphicsSettings.currentRenderPipeline;
            bool hdrp=pipeline!=null&&pipeline.GetType().Name.Contains("HDRenderPipeline");
            var shader=Shader.Find(hdrp?"HDRP/Unlit":pipeline!=null?"Universal Render Pipeline/Particles/Unlit":"Particles/Standard Unlit");
            if(shader==null)shader=Shader.Find(hdrp?"HDRP/Lit":pipeline!=null?"Universal Render Pipeline/Lit":"Standard");
            if(shader!=null)
            {
                material=new Material(shader){name="Soft rain streaks (original)",renderQueue=3100};
                foreach(string property in new[]{"_SurfaceType","_Surface"})Set(property,1);
                Set("_SrcBlend",(float)BlendMode.SrcAlpha);Set("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);Set("_ZWrite",0);Set("_TransparentZWrite",0);Set("_CullMode",0);Set("_CullModeForward",0);
                var tint=new Color(.68f,.76f,.8f,.22f);foreach(string property in new[]{"_UnlitColor","_BaseColor","_Color"})if(material.HasProperty(property))material.SetColor(property,tint);
                var texture=Resources.Load<Texture2D>("PoolroomsRain/RainStreak");foreach(string property in new[]{"_UnlitColorMap","_BaseMap","_MainTex"})if(material.HasProperty(property))material.SetTexture(property,texture);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.SetOverrideTag("RenderType","Transparent");PoolroomsBiomeMaterialFactory.Validate(material);renderer.sharedMaterial=material;
            }
            else renderer.enabled=false;
            audioSource=root.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.loop=true;audioSource.spatialBlend=0;audioSource.volume=0;audioSource.priority=190;audioSource.dopplerLevel=0;audioSource.ignoreListenerPause=false;
            audioSource.clip=Resources.Load<AudioClip>("PoolroomsRain/QuietRainLoop");if(audioSource.clip!=null)audioSource.Play();
            lowpass=root.AddComponent<AudioLowPassFilter>();lowpass.cutoffFrequency=16000;
        }
        private void Set(string property,float value){if(material.HasProperty(property))material.SetFloat(property,value);}
        private void Update()
        {
            if(root==null||world==null||world.viewer==null)return;
            if(boundViewer!=world.viewer)
            {boundViewer=world.viewer;explorer=boundViewer.GetComponentInParent<PoolroomsExplorer>();settings=boundViewer.GetComponentInParent<PoolroomsGameSettings>();mixer=boundViewer.GetComponentInParent<PoolroomsAmbientAudio>();rain.Clear();previousPosition=boundViewer.position;}
            bool paused=explorer!=null&&explorer.IsPaused||Time.deltaTime<=0||AudioListener.pause;
            if(paused){if(rain.isPlaying)rain.Pause();if(audioSource.isPlaying)audioSource.Pause();return;}
            if(!rain.isPlaying)rain.Play();if(!audioSource.isPlaying&&audioSource.clip!=null){audioSource.UnPause();if(!audioSource.isPlaying)audioSource.Play();}
            Room room=world.isActiveAndEnabled?world.RoomAtViewer():null;var plan=room?.Plan?.Rain;
            bool active=plan!=null&&world.CurrentLayout!=null&&explorer!=null&&explorer.IsReady;
            Vector3 local=room!=null?boundViewer.position-world.RoomWorldPosition(room):Vector3.zero;
            bool sheltered=active&&plan.Sheltered(local.x,local.z);
            float volume=active?(sheltered?.11f:.24f):0;
            if(mixer!=null)volume*=Mathf.Clamp01(mixer.masterVolume)*Mathf.Clamp01(mixer.environmentVolume);
            else if(settings!=null)volume*=settings.Preferences.masterVolume*settings.Preferences.environmentVolume;
            audioSource.volume=Mathf.Lerp(audioSource.volume,volume,1-Mathf.Exp(-Time.deltaTime*3));
            lowpass.cutoffFrequency=Mathf.Lerp(lowpass.cutoffFrequency,sheltered?2600:14000,1-Mathf.Exp(-Time.deltaTime*3));
            if((boundViewer.position-previousPosition).sqrMagnitude>400)rain.Clear();previousPosition=boundViewer.position;
            if(!active){accumulator=0;return;}
            int quality=settings!=null?settings.Preferences.graphicsQuality:-1;
            accumulator+=Mathf.Min(Time.deltaTime,.1f)*(quality==0?180:420);int count=Mathf.Min(48,(int)accumulator);accumulator-=count;
            for(int i=0;i<count;i++)Emit(room,local);
        }
        private void Emit(Room room,Vector3 viewerLocal)
        {
            var cfg=world.CurrentLayout.Config;float span=cfg.CellSize;
            float x=viewerLocal.x+(random.Value()-.5f)*32,z=viewerLocal.z+(random.Value()-.5f)*32;
            int dx=Mathf.FloorToInt((x+span*.5f)/span),dz=Mathf.FloorToInt((z+span*.5f)/span);
            long rx=(long)room.X+dx,rz=(long)room.Z+dz;if(rx<int.MinValue||rx>int.MaxValue||rz<int.MinValue||rz>int.MaxValue||!world.IsRoomLoaded((int)rx,(int)rz))return;
            var candidate=world.CurrentLayout.At((int)rx,(int)rz);var p=candidate.Plan?.Rain;
            if(p==null||p.Sheltered(x-dx*span,z-dz*span,.2f))return;
            Vector3 origin=world.RoomWorldPosition(candidate);float y=5.5f+Mathf.Clamp(viewerLocal.y,0,4)+random.Value()*2;
            var emit=new ParticleSystem.EmitParams{position=origin+new Vector3(x-dx*span,y,z-dz*span),velocity=new Vector3(0,-11,0),startLifetime=(y-.04f)/11,startSize=.012f+random.Value()*.012f};rain.Emit(emit,1);
        }
        private void OnDisable(){if(rain!=null)rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);if(audioSource!=null)audioSource.Stop();}
        private void OnDestroy(){if(root!=null)Destroy(root);if(material!=null)Destroy(material);}
    }
}
