using System;
using System.Collections.Generic;
using System.IO;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Profiling;

namespace Poolrooms
{
    // Opt-in diagnostic capture only; never added to normal gameplay or its HUD.
    public sealed class PoolroomsPerformanceCapture : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public bool completed,editor,graphicsAvailable,rayTracingSupported;
            public string unityVersion,device,gpu,graphicsApi,startedUtc;
            public int systemMemoryMB,gpuMemoryMB,frames,quality,loadedRooms,gcCollections,waitingFrames;
            public float measuredSeconds,medianFrameMs,p95FrameMs,p99FrameMs,maximumFrameMs;
            public float waitingSeconds,waitingP95FrameMs,waitingMaximumFrameMs,wallSeconds;
            public long managedBytes,totalAllocatedBytes,totalReservedBytes;
            public string[] observedRegions;
            public string scope="Observed frame times on this machine; not a visual-quality certification or a GPU-only timing";
        }
        private readonly List<float> samples=new List<float>();
        private readonly List<float> waiting=new List<float>();
        private readonly HashSet<string> regions=new HashSet<string>();
        private PoolroomsWorld world;
        private PoolroomsExplorer actor;
        private string path,started;
        private float began,measured,nextRegion,waited;
        private int firstGc;
        private bool written;
        public void Begin(PoolroomsWorld target,string output)
        {world=target;actor=world!=null&&world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;path=output;began=Time.unscaledTime;firstGc=GC.CollectionCount(0);started=DateTime.UtcNow.ToString("o");}
        void Update()
        {
            if(written||string.IsNullOrEmpty(path))return;
            // Separate waiting on chunks from loaded exploration; don't hide the
            // frame-time spikes observed during streaming behind a good percentile.
            if(Time.unscaledTime-began>=180){Save(false);return;}
            if(Time.unscaledTime-began<3||world==null||actor!=null&&(actor.IsPaused||actor.InputCaptured))return;
            float dt=Time.unscaledDeltaTime;if(dt<=0||!float.IsFinite(dt))return;
            if(!world.ViewerReady){waiting.Add(dt*1000);waited+=dt;return;}
            samples.Add(dt*1000);measured+=dt;
            if(Time.unscaledTime>=nextRegion)
            {nextRegion=Time.unscaledTime+.5f;var room=world.RoomAtViewer();if(world.InsideMall)regions.Add("Centro comercial");else if(world.Terrain!=null&&world.Terrain.IsInside)regions.Add("Nivel 94");else if(room!=null)regions.Add(InteriorPlanner.Name(room.Environment));}
            if(measured>=30)Save(true);
        }
        static float Percentile(List<float> values,float fraction)
        {if(values.Count==0)return 0;return values[Mathf.Clamp(Mathf.CeilToInt(values.Count*fraction)-1,0,values.Count-1)];}
        void Save(bool complete)
        {
            if(written||string.IsNullOrEmpty(path))return;written=true;samples.Sort();waiting.Sort();
            var settings=actor!=null?actor.GetComponent<PoolroomsGameSettings>():null;
            var report=new Report{completed=complete,editor=Application.isEditor,graphicsAvailable=SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null,
                rayTracingSupported=SystemInfo.supportsRayTracing,unityVersion=Application.unityVersion,device=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),startedUtc=started,
                systemMemoryMB=SystemInfo.systemMemorySize,gpuMemoryMB=SystemInfo.graphicsMemorySize,frames=samples.Count,measuredSeconds=measured,quality=settings!=null?settings.Preferences.graphicsQuality:-1,
                medianFrameMs=Percentile(samples,.5f),p95FrameMs=Percentile(samples,.95f),p99FrameMs=Percentile(samples,.99f),maximumFrameMs=Percentile(samples,1),gcCollections=GC.CollectionCount(0)-firstGc,
                waitingFrames=waiting.Count,waitingSeconds=waited,waitingP95FrameMs=Percentile(waiting,.95f),waitingMaximumFrameMs=Percentile(waiting,1),wallSeconds=Time.unscaledTime-began,
                managedBytes=Profiler.GetMonoUsedSizeLong(),totalAllocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),totalReservedBytes=Profiler.GetTotalReservedMemoryLong(),observedRegions=new List<string>(regions).ToArray()};
            // Read the public chunk enumeration without changing streaming settings.
            foreach(var chunk in world.Chunks)if(chunk!=null)report.loadedRooms++;
            try{string folder=Path.GetDirectoryName(path);if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);File.WriteAllText(path,JsonUtility.ToJson(report,true));Debug.Log("Poolrooms: rendimiento registrado en "+path);}
            catch(Exception error){Debug.LogException(error,this);}enabled=false;
        }
        void OnDisable(){if(!written&&world!=null)Save(false);}
    }
}
