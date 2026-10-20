using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Poolrooms.Core;

static partial class Checks
{
    static void ImmersiveSoundAndMusic()
    {
        var prefs=new GamePreferences();Check(prefs.soundscape.mode==SoundscapeMode.Peaceful&&!prefs.music.autoPlay,"Sound must start peaceful and music opt-in");
        prefs.soundscape.mode=(SoundscapeMode)99;prefs.soundscape.detail=float.NaN;prefs.soundscape.tension=4;prefs.music.volume=float.NaN;prefs.music.repeat=(MusicRepeat)99;prefs.music.crossfade=100;
        prefs.Sanitize();Check(prefs.soundscape.mode==SoundscapeMode.Peaceful&&prefs.soundscape.detail==.65f&&prefs.soundscape.tension==1,"Corrupt sound preferences");
        Check(prefs.music.volume==.5f&&prefs.music.crossfade==5&&prefs.music.repeat==MusicRepeat.All,"Corrupt music preferences");
        prefs.walkSpeed=4;prefs.soundscape.mode=SoundscapeMode.Uneasy;prefs.Reset(SettingsCategory.Audio);
        Check(prefs.walkSpeed==4&&prefs.soundscape.mode==SoundscapeMode.Peaceful,"Audio reset changed movement");
        prefs.soundscape=null;prefs.music=null;prefs.Sanitize();Check(prefs.soundscape!=null&&prefs.music!=null,"Legacy preferences need defaults");
        foreach(float dt in new[]{1f/240,1f/120,1f/60,1f/30,1f/20,1f/10})
        {
            var director=new AtmosphereDirector();
            for(int i=0;i<600;i++)Check(director.Step(dt,SoundscapeMode.Peaceful,1,true)==AtmosphereCue.None,"Peaceful emits fear cues");
            float since=0;int cues=0;
            for(int i=0;i<(int)(600/dt);i++)
            {
                since+=dt;var cue=director.Step(dt,SoundscapeMode.Uneasy,1,false);
                if(cue!=AtmosphereCue.None){Check(since>=17.9f,"Fear cue too close to previous cue");Check(cue!=AtmosphereCue.RemoteSteps,"Footstep toggle ignored");since=0;cues++;}
                Check(director.Step(0,SoundscapeMode.Uneasy,1,true)==AtmosphereCue.None,"Pause advances fear timing");
            }
            Check(cues>=12&&cues<=34,"Unexpected ambience density");
            director.Reset();for(int i=0;i<500;i++)Check(director.Step(dt,SoundscapeMode.Uneasy,0,true)==AtmosphereCue.None,"Zero tension emits a cue");
            Check(director.Step(float.NaN,SoundscapeMode.Uneasy,1,true)==AtmosphereCue.None,"Invalid timestep emits cue");
            Check(director.Step(0,SoundscapeMode.Peaceful,1,true)==AtmosphereCue.None,"Mode change emits cue");
            for(int i=0;i<10;i++)Check(director.Step(1,SoundscapeMode.Uneasy,1,true)==AtmosphereCue.None,"Mode change must reset delay");
        }
        foreach(float speed in new[]{-1,0,.1f,.35f,1,4,100,float.NaN,float.PositiveInfinity})
        foreach(float mass in new[]{-1,0,.02f,.5f,80,1000,float.NaN})
        {
            float soft=ImmersiveAudio.ImpactGain(speed,mass,true),hard=ImmersiveAudio.ImpactGain(speed,mass,false);
            Check(ImmersiveAudio.Finite(soft)&&soft>=0&&soft<=.13f&&hard>=soft&&hard<=.28f,"Unbounded impact gain");
            if(speed<.35f||mass<=0||!ImmersiveAudio.Finite(speed)||!ImmersiveAudio.Finite(mass))Check(hard==0,"Invalid impact audible");
        }
        Check(ImmersiveAudio.Transient(.8f,0)<ImmersiveAudio.Transient(.8f,1),"Dynamic range control inactive");
        float smoothed=0;for(int i=0;i<600;i++){smoothed=ImmersiveAudio.Approach(smoothed,1,1f/60,1.2f);Check(smoothed>=0&&smoothed<=1,"Fade overshoots");}
        Check(smoothed>.99f&&ImmersiveAudio.Approach(.4f,1,0,1)==.4f,"Fade timing failed");
        var q=new MusicQueue();Check(q.Next(false,MusicRepeat.All,true)==-1&&q.Previous()==-1&&!q.Select(0),"Empty library fails");
        q.Replace(new[]{"a.mp3","b.ogg","c.wav","a.mp3"},"");Check(q.Tracks.Count==3&&q.Selected==-1,"Library duplicates");
        Check(q.Next(false,MusicRepeat.Off,false)==0&&q.Select(0),"Initial queue selection");
        Check(q.Next(false,MusicRepeat.One,false)==0&&q.Next(true,MusicRepeat.One,false)==1,"Manual skip must bypass repeat-one");
        q.Select(1);q.Select(2);int previous=q.Previous();q.Select(previous,false);Check(previous==1&&q.Previous()==0,"Previous history oscillates");
        q.Select(2);Check(q.Next(false,MusicRepeat.Off,false)==-1&&q.Next(false,MusicRepeat.All,false)==0,"Repeat boundaries");
        q.ResetOrder();var visited=new HashSet<int>{q.Selected};
        for(int i=0;i<2;i++){int next=q.Next(false,MusicRepeat.Off,true);Check(next>=0&&visited.Add(next),"Shuffle repeated before exhaustion");q.Select(next);}
        Check(q.Next(false,MusicRepeat.Off,true)==-1,"Shuffle no-repeat fails to stop");
        int again=q.Next(false,MusicRepeat.All,true);Check(again>=0&&again!=q.Selected,"Shuffle immediately repeats at wrap");
        q.Replace(new[]{"c.wav","a.mp3"},"c.wav");Check(q.Selected==0&&q.Tracks.Count==2,"Rescan lost selected path");
        q.Replace(new[]{"one.mp3"},"");q.Select(0);Check(q.Next(false,MusicRepeat.All,true)==0&&q.Next(false,MusicRepeat.Off,true)==-1,"Single-track shuffle");
        string folder=Path.Combine(Path.GetTempPath(),"Poolrooms-Música-"+Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder);
            foreach(string name in new[]{"atardecer #1.mp3","aire.OGG","paz.wav","interior.aiff","piano.aif","empty.mp3","not-a-song.exe","source.obj"})File.WriteAllBytes(Path.Combine(folder,name),name.StartsWith("empty")?Array.Empty<byte>():new byte[]{1,2,3});
            string tooLarge=Path.Combine(folder,"huge.mp3");using(var stream=File.Create(tooLarge))stream.SetLength(LocalMusic.MaxBytes+1);
            string deep=Path.Combine(folder,"a","b","c","d");Directory.CreateDirectory(deep);File.WriteAllBytes(Path.Combine(deep,"hidden.mp3"),new byte[]{1});File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(deep),"allowed.mp3"),new byte[]{1});
            var scan=LocalMusic.Scan(folder);Check(scan.Files.Count==6&&scan.Skipped==2,"Scan includes invalid sizes or depth");
            Check(scan.Files.Any(x=>x.EndsWith("atardecer #1.mp3"))&&!scan.Files.Any(x=>x.EndsWith("hidden.mp3")),"Unicode or depth scan failed");
            Check(scan.Files.SequenceEqual(scan.Files.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)),"Library order unstable");
            string link=Path.Combine(folder,"linked.mp3");File.CreateSymbolicLink(link,Path.Combine(folder,"paz.wav"));
            Check(LocalMusic.Scan(folder).Files.Count==6,"Scanner follows symlinks");
            for(int i=0;i<LocalMusic.MaxTracks;i++)File.WriteAllBytes(Path.Combine(folder,"song"+i+".wav"),new byte[]{1});
            scan=LocalMusic.Scan(folder);Check(scan.Truncated&&scan.Files.Count==LocalMusic.MaxTracks,"Library bound ignored");
        }
        finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        Check(!LocalMusic.Supported("https://site/music.exe")&&!LocalMusic.Supported("a.flac")&&LocalMusic.Supported("x.MP3"),"Unsupported format advertised");
        Console.WriteLine($"PASS: immersive sound and local music; {assertions} cumulative core assertions. Native decoding, spatial mix and listening unrun.");
    }
}
