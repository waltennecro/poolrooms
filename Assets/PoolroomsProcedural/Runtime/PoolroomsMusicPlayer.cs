using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Poolrooms
{
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsMusicPlayer : MonoBehaviour
    {
        private readonly MusicQueue queue=new MusicQueue();
        private readonly AudioSource[] sources=new AudioSource[2];
        private readonly float[] weights=new float[2];
        private PoolroomsGameSettings settings;
        private GameObject root;
        private UnityWebRequest request;
        private Coroutine loading;
        private AudioClip pendingClip;
        private int generation,active;
        private float fade,fadeSeconds;
        private bool fading,endingBlocked,autoStart=true;
        private string currentPath="";
        public string Folder {get;private set;}="";
        public string Status {get;private set;}="Preparando biblioteca…";
        public bool Scanning {get;private set;}
        public bool Loading {get;private set;}
        public bool Paused {get;private set;}
        public bool Playing {get{return root!=null&&sources[active]!=null&&sources[active].clip!=null&&!Paused;}}
        public float AudibleGain {get;private set;}
        public int Selected {get{return queue.Selected;}}
        public System.Collections.Generic.IReadOnlyList<string> Tracks {get{return queue.Tracks;}}
        public string Title {get{return string.IsNullOrEmpty(currentPath)?"Sin canción":Path.GetFileNameWithoutExtension(currentPath);}}
        public float Duration {get{return root!=null&&sources[active].clip!=null?sources[active].clip.length:0;}}
        public float Position {get{return Duration>0?sources[active].time:0;}}
        private MusicPreferences Preferences {get{return settings.Preferences.music;}}
        private void Start()
        {
            settings=GetComponent<PoolroomsGameSettings>();if(settings==null)settings=gameObject.AddComponent<PoolroomsGameSettings>();settings.Initialize();
            root=new GameObject("Local music player");root.transform.SetParent(transform,false);
            for(int i=0;i<2;i++)
            {
                var go=new GameObject("Music crossfade "+i);go.transform.SetParent(root.transform,false);var s=go.AddComponent<AudioSource>();sources[i]=s;
                s.playOnAwake=false;s.spatialBlend=0;s.dopplerLevel=0;s.reverbZoneMix=0;s.priority=100;s.ignoreListenerPause=true;s.volume=0;
            }
            try{Folder=PrepareFolder(Path.GetFullPath(Path.Combine(Application.dataPath,"..","Music")));}
            catch(Exception){try{Folder=PrepareFolder(Path.Combine(Application.persistentDataPath,"Music"));}catch(Exception e){Status="No se puede abrir la carpeta de música: "+e.Message;return;}}
            Refresh();
        }
        private static string PrepareFolder(string path)
        {
            Directory.CreateDirectory(path);string note=Path.Combine(path,"LEEME_MUSICA.txt");
            if(!File.Exists(note))File.WriteAllText(note,"Coloca aquí tus MP3, OGG, WAV o AIFF. Subcarpetas: hasta 3 niveles. Máximo 1024 canciones, 128 MB por archivo.\nEn el juego: Ajustes > Sonido > Tu música > Actualizar lista.\nLos archivos permanecen locales. El juego no los sube ni los incluye en las actualizaciones.\n");
            return path;
        }
        public void OpenFolder()
        {if(string.IsNullOrEmpty(Folder))return;try{Application.OpenURL(new Uri(Folder+Path.DirectorySeparatorChar).AbsoluteUri);}catch(Exception e){Status=e.Message;}}
        public void Refresh(){if(!Scanning&&!string.IsNullOrEmpty(Folder)&&isActiveAndEnabled)StartCoroutine(Scan());}
        private IEnumerator Scan()
        {
            Scanning=true;Status="Buscando canciones…";string folder=Folder;var task=Task.Run(()=>LocalMusic.Scan(folder));
            while(!task.IsCompleted)yield return null;
            Scanning=false;
            if(task.IsFaulted){Status="No se pudo leer la biblioteca: "+task.Exception.GetBaseException().Message;yield break;}
            var scan=task.Result;CancelLoad();queue.Replace(scan.Files,currentPath.Length>0?currentPath:PlayerPrefs.GetString("Poolrooms.Music.LastFile",""));
            Status=scan.Files.Count+" canciones"+(scan.Skipped>0?" · "+scan.Skipped+" omitidas":"")+(scan.Truncated?" · se alcanzó el límite de biblioteca":"");
            if(autoStart&&Preferences.autoPlay&&scan.Files.Count>0)Play(queue.Selected>=0?queue.Selected:0);autoStart=false;
        }
        public void PlayPause()
        {
            if(root==null)return;
            if(sources[active].clip==null){Play(queue.Selected>=0?queue.Selected:0);return;}
            Paused=!Paused;foreach(var source in sources)if(Paused)source.Pause();else source.UnPause();
        }
        public void Next(){if(settings==null)return;int n=queue.Next(true,Preferences.repeat,Preferences.shuffle);if(n>=0)Play(n);}
        public void Previous(){if(settings==null)return;int n=queue.Previous();if(n>=0)Play(n,false);}
        public void ResetOrder(){queue.ResetOrder();}
        public void Play(int index){Play(index,true);}
        private void Play(int index,bool remember)
        {
            if(root==null||index<0||index>=queue.Tracks.Count)return;
            CancelLoad();endingBlocked=false;Paused=false;
            foreach(var source in sources)source.UnPause();
            loading=StartCoroutine(LoadTrack(queue.Tracks[index],index,remember,generation));
        }
        private static AudioType TypeFor(string file)
        {
            switch(Path.GetExtension(file).ToLowerInvariant())
            {case ".mp3":return AudioType.MPEG;case ".ogg":return AudioType.OGGVORBIS;case ".aif":case ".aiff":return AudioType.AIFF;default:return AudioType.WAV;}
        }
        private IEnumerator LoadTrack(string path,int index,bool remember,int token)
        {
            Loading=true;Status="Cargando "+Path.GetFileName(path)+"…";
            UnityWebRequest web=null;UnityWebRequestAsyncOperation operation=null;string error=null;
            try
            {
                var file=new FileInfo(path);if(!file.Exists||file.Length<=0||file.Length>LocalMusic.MaxBytes||!LocalMusic.Supported(path))throw new IOException("Archivo ausente, vacío o mayor de 128 MB.");
                var uri=new Uri(Path.GetFullPath(path));if(!uri.IsFile)throw new IOException("Selecciona un archivo local.");
                web=UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri,TypeFor(path));
                ((DownloadHandlerAudioClip)web.downloadHandler).streamAudio=true;request=web;operation=web.SendWebRequest();
            }
            catch(Exception e){error=e.GetBaseException().Message;}
            if(error!=null){if(web!=null)web.Dispose();request=null;Fail(error);yield break;}
            yield return operation;
            if(token!=generation||request!=web)yield break;
            try
            {
                if(web.result!=UnityWebRequest.Result.Success)error=web.error;
                else{pendingClip=DownloadHandlerAudioClip.GetContent(web);if(pendingClip==null||pendingClip.length<=0)error="El formato no se pudo decodificar.";}
            }
            catch(Exception e){error=e.GetBaseException().Message;}
            web.Dispose();request=null;
            if(error!=null){if(pendingClip!=null)Destroy(pendingClip);pendingClip=null;Fail(error);yield break;}
            // A manual skip waits for an existing fade, rather than cutting its outgoing voice.
            while(fading&&token==generation)yield return null;if(token!=generation)yield break;
            int incoming=1-active;Release(incoming);sources[incoming].clip=pendingClip;pendingClip=null;
            bool hadSong=sources[active].clip!=null;queue.Select(index,remember);currentPath=path;
            weights[incoming]=0;sources[incoming].volume=0;sources[incoming].Play();if(Paused)sources[incoming].Pause();
            active=incoming;fade=0;fadeSeconds=hadSong?Mathf.Min(Preferences.crossfade,sources[active].clip.length*.2f):.15f;fading=fadeSeconds>.001f;
            if(!fading){Release(1-active);weights[active]=1;}
            Loading=false;loading=null;Status="Reproduciendo · "+Title;PlayerPrefs.SetString("Poolrooms.Music.LastFile",path);PlayerPrefs.Save();
        }
        private void Fail(string message){Loading=false;loading=null;endingBlocked=true;Status="No se pudo reproducir: "+message;}
        public void Seek(float normalized)
        {
            if(Duration<=0||Loading)return;
            try{sources[active].time=Mathf.Clamp01(normalized)*Mathf.Max(0,Duration-.05f);endingBlocked=false;}
            catch(Exception e){Status="Esta pista no permite buscar esa posición: "+e.Message;}
        }
        private void Update()
        {
            if(root==null||settings==null)return;
            if(fading&&!Paused)
            {
                fade+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(fade/Mathf.Max(.001f,fadeSeconds));
                // Constant-sum fade avoids boosting unrelated full-scale user masters by 3 dB.
                weights[active]=t;weights[1-active]=1-t;if(t>=1){fading=false;Release(1-active);}
            }
            float gain=Preferences.volume*settings.EffectiveMasterVolume;
            AudibleGain=0;for(int i=0;i<2;i++){sources[i].volume=weights[i]*gain;if(!Paused&&sources[i].isPlaying)AudibleGain+=sources[i].volume;}
            if(!Paused&&!Loading&&!fading&&!endingBlocked&&sources[active].clip!=null)
            {
                float left=Duration-Position;
                if(!sources[active].isPlaying||left<=Mathf.Min(Preferences.crossfade,Duration*.2f)+.03f)
                {int n=queue.Next(false,Preferences.repeat,Preferences.shuffle);if(n>=0)Play(n);else if(!sources[active].isPlaying){Stop();Status="Lista terminada";}}
            }
        }
        private void CancelLoad()
        {
            generation++;if(loading!=null){StopCoroutine(loading);loading=null;}
            if(request!=null){request.Abort();request.Dispose();request=null;}
            if(pendingClip!=null)Destroy(pendingClip);pendingClip=null;Loading=false;
        }
        private void Release(int index)
        {var source=sources[index];if(source==null)return;source.Stop();var clip=source.clip;source.clip=null;weights[index]=0;if(clip!=null)Destroy(clip);}
        public void Stop()
        {CancelLoad();fading=false;Paused=false;AudibleGain=0;for(int i=0;i<2;i++)Release(i);Status="Reproductor detenido";}
        private void OnDisable(){Stop();StopAllCoroutines();Scanning=false;}
        private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
