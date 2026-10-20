using System;
using System.Collections.Generic;
using System.IO;

namespace Poolrooms.Core
{
    public enum MusicRepeat { Off,All,One }
    [Serializable]
    public sealed class MusicPreferences
    {
        public float volume=.5f,crossfade=2;
        public bool autoPlay,shuffle;
        public MusicRepeat repeat=MusicRepeat.All;
        public void Sanitize()
        {
            volume=GamePreferences.Clamp(volume,0,1,.5f);crossfade=GamePreferences.Clamp(crossfade,0,5,2);
            if(!Enum.IsDefined(typeof(MusicRepeat),repeat))repeat=MusicRepeat.All;
        }
    }
    public sealed class MusicScan
    {
        public readonly List<string> Files=new List<string>();
        public int Skipped;
        public bool Truncated;
    }
    public static class LocalMusic
    {
        public const int MaxTracks=1024,MaxEntries=8192,MaxDepth=3;
        public const long MaxBytes=128L*1024*1024;
        public static bool Supported(string file)
        {
            switch(Path.GetExtension(file??"").ToLowerInvariant())
            {case ".mp3":case ".ogg":case ".wav":case ".aif":case ".aiff":return true;default:return false;}
        }
        public static MusicScan Scan(string folder)
        {
            var result=new MusicScan();var pending=new Stack<(string folder,int depth)>();pending.Push((Path.GetFullPath(folder),0));int entries=0;
            while(pending.Count>0&&!result.Truncated)
            {
                var at=pending.Pop();
                try
                {
                    foreach(string path in Directory.EnumerateFileSystemEntries(at.folder))
                    {
                        if(++entries>MaxEntries||result.Files.Count>=MaxTracks){result.Truncated=true;break;}
                        try
                        {
                            var attributes=File.GetAttributes(path);if((attributes&FileAttributes.ReparsePoint)!=0){result.Skipped++;continue;}
                            if((attributes&FileAttributes.Directory)!=0){if(at.depth<MaxDepth)pending.Push((path,at.depth+1));continue;}
                            if(!Supported(path))continue;
                            long size=new FileInfo(path).Length;
                            if(size<=0||size>MaxBytes){result.Skipped++;continue;}
                            result.Files.Add(Path.GetFullPath(path));
                        }
                        catch(IOException){result.Skipped++;}catch(UnauthorizedAccessException){result.Skipped++;}
                    }
                }
                catch(IOException){result.Skipped++;}catch(UnauthorizedAccessException){result.Skipped++;}
            }
            result.Files.Sort(StringComparer.OrdinalIgnoreCase);return result;
        }
    }
    public sealed class MusicQueue
    {
        private readonly List<string> tracks=new List<string>();
        private readonly List<int> shuffleBag=new List<int>();
        private readonly List<int> history=new List<int>();
        private readonly HashSet<int> played=new HashSet<int>();
        private StableRandom random=new StableRandom(0xC1B3u);
        public int Selected {get;private set;}=-1;
        public IReadOnlyList<string> Tracks {get{return tracks;}}
        public void Replace(IReadOnlyList<string> paths,string selected)
        {
            tracks.Clear();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var p in paths)if(tracks.Count<LocalMusic.MaxTracks&&seen.Add(p))tracks.Add(p);
            Selected=-1;for(int i=0;i<tracks.Count;i++)if(string.Equals(selected,tracks[i],StringComparison.OrdinalIgnoreCase))Selected=i;
            shuffleBag.Clear();history.Clear();played.Clear();if(Selected>=0)played.Add(Selected);
        }
        public bool Select(int index,bool remember=true)
        {if(index<0||index>=tracks.Count)return false;if(remember&&Selected>=0&&Selected!=index){history.Add(Selected);if(history.Count>256)history.RemoveAt(0);}Selected=index;played.Add(index);shuffleBag.Remove(index);return true;}
        public void ResetOrder(){shuffleBag.Clear();played.Clear();if(Selected>=0)played.Add(Selected);}
        public int Next(bool manual,MusicRepeat repeat,bool shuffle)
        {
            if(tracks.Count==0)return -1;
            if(!manual&&repeat==MusicRepeat.One&&Selected>=0)return Selected;
            if(shuffle)
            {
                if(shuffleBag.Count==0)
                {
                    // Without repeat, an exhausted pass stops until a manual action.
                    if(played.Count>=tracks.Count)
                    {if(!manual&&repeat==MusicRepeat.Off)return -1;played.Clear();if(Selected>=0)played.Add(Selected);}
                    for(int i=0;i<tracks.Count;i++)if(!played.Contains(i))shuffleBag.Add(i);
                }
                if(shuffleBag.Count==0)return manual||repeat!=MusicRepeat.Off?0:-1;
                int at=random.Range(shuffleBag.Count),next=shuffleBag[at];shuffleBag.RemoveAt(at);return next;
            }
            int n=Selected+1;return n<tracks.Count?n:manual||repeat!=MusicRepeat.Off?0:-1;
        }
        public int Previous()
        {if(history.Count>0){int at=history[history.Count-1];history.RemoveAt(history.Count-1);return at;}return tracks.Count==0?-1:Selected>0?Selected-1:tracks.Count-1;}
    }
}
