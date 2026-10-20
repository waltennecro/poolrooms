#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Poolrooms.Tests
{
    public sealed class ImmersiveSoundTests
    {
        [Test]
        public void ShippedContactAndAtmosphereClipsLoadWithBoundedPeaks()
        {
            var clips=Resources.LoadAll<AudioClip>("PoolroomsAudio");Assert.That(clips.Length,Is.EqualTo(48));
            foreach(var clip in clips)
            {
                Assert.That(clip.LoadAudioData(),Is.True,clip.name);
                Assert.That(clip.length,Is.GreaterThan(.05f));Assert.That(clip.frequency,Is.EqualTo(24000));
                var samples=new float[clip.samples*clip.channels];Assert.That(clip.GetData(samples,0),Is.True,clip.name);
                Assert.That(samples.Max(x=>Math.Abs(x)),Is.LessThan(.651f),clip.name);
                Assert.That(samples.Any(x=>Math.Abs(x)>.01f),Is.True,clip.name);
            }
        }
        [UnityTest]
        public IEnumerator MusicDecodesUnicodePathsCancelsLoadsAndKeepsLastSongOnMissingFile()
        {
            GameObject actor=null;PoolroomsGameSettings settings=null;string path=null,missing=null;
            string key=PoolroomsGameSettings.PreferencesKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key,"");
            bool hadLast=PlayerPrefs.HasKey("Poolrooms.Music.LastFile");string last=PlayerPrefs.GetString("Poolrooms.Music.LastFile","");
            float scale=Time.timeScale;bool paused=AudioListener.pause;
            try
            {
                // Prevent Explorer.Start from creating a world/menu; exercise the real player independently.
                PlayerPrefs.DeleteKey(key);actor=new GameObject("Local music regression");actor.SetActive(false);
                var explorer=actor.AddComponent<PoolroomsExplorer>();explorer.enabled=false;
                settings=actor.AddComponent<PoolroomsGameSettings>();var player=actor.AddComponent<PoolroomsMusicPlayer>();
                actor.SetActive(true);yield return null;
                float deadline=Time.realtimeSinceStartup+10;
                while(player.Scanning&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(player.Scanning,Is.False);Assert.That(Directory.Exists(player.Folder),Is.True);
                missing=Path.Combine(player.Folder,"Prueba ausente "+Guid.NewGuid().ToString("N")+".wav");WriteWave(missing);
                path=Path.Combine(player.Folder,"Prueba música # "+Guid.NewGuid().ToString("N")+".wav");WriteWave(path);
                int revision=player.LibraryRevision;player.Refresh();deadline=Time.realtimeSinceStartup+10;
                while(player.Scanning&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(player.LibraryRevision,Is.GreaterThan(revision));
                int index=player.Tracks.ToList().IndexOf(path);Assert.That(index,Is.GreaterThanOrEqualTo(0));
                settings.Preferences.music.crossfade=0;settings.Preferences.music.repeat=MusicRepeat.One;
                Time.timeScale=0;AudioListener.pause=true;player.Play(index);player.Stop();
                Assert.That(player.Loading,Is.False);Assert.That(player.Duration,Is.EqualTo(0));player.Play(index);
                deadline=Time.realtimeSinceStartup+10;while(player.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(player.Loading,Is.False,player.Status);Assert.That(player.Duration,Is.InRange(3.9f,4.1f),player.Status);
                var voices=actor.GetComponentsInChildren<AudioSource>();Assert.That(voices.Length,Is.EqualTo(2));
                Assert.That(voices.All(x=>x.ignoreListenerPause&&x.spatialBlend==0),Is.True,"Music should remain controllable from paused menu.");
                settings.Preferences.masterVolume=0;settings.Change(SettingsChange.Audio);yield return null;
                Assert.That(voices.All(x=>x.volume==0),Is.True,"General mute left a music voice audible.");
                settings.Preferences.masterVolume=.85f;settings.Change(SettingsChange.Audio);
                player.PlayPause();Assert.That(player.Paused,Is.True);player.Seek(.4f);Assert.That(player.Position,Is.InRange(1.4f,1.8f));
                player.PlayPause();Assert.That(player.Paused,Is.False);
                int missingIndex=player.Tracks.ToList().IndexOf(missing);Assert.That(missingIndex,Is.GreaterThanOrEqualTo(0));
                File.Delete(missing);
                player.Play(missingIndex);yield return null;
                Assert.That(player.Status,Does.Contain("No se pudo reproducir"));Assert.That(player.Duration,Is.GreaterThan(0),"Bad input discarded the playing clip.");
                player.Stop();Assert.That(player.Loading,Is.False);Assert.That(player.Duration,Is.EqualTo(0));
                Assert.That(voices.All(x=>x.clip==null),Is.True,"Stop retained decoded clips.");
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(path!=null&&File.Exists(path))File.Delete(path);
                if(missing!=null&&File.Exists(missing))File.Delete(missing);
                if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
                if(hadLast)PlayerPrefs.SetString("Poolrooms.Music.LastFile",last);else PlayerPrefs.DeleteKey("Poolrooms.Music.LastFile");
                PlayerPrefs.Save();Time.timeScale=scale;AudioListener.pause=paused;
            }
        }
        private static void WriteWave(string path)
        {
            const int rate=24000,frames=rate*4;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+frames*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(frames*2);
                for(int i=0;i<frames;i++)writer.Write((short)(Math.Sin(i*2*Math.PI*220/rate)*1000));
            }
        }
    }
}
#endif
