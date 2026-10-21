#if UNITY_INCLUDE_TESTS
using System;
using NUnit.Framework;
using Poolrooms.Core;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Poolrooms.Tests
{
    public sealed class ImmersionStationTests
    {
        [Test]
        public void RegistryExcludesOtherWorldsAndUnregistersDisabledStations()
        {
            GameObject a=null,b=null;
            try
            {
                a=new GameObject("Station world A");a.SetActive(false);var worldA=a.AddComponent<PoolroomsWorld>();worldA.enabled=false;worldA.generateOnStart=false;
                b=new GameObject("Station world B");b.SetActive(false);var worldB=b.AddComponent<PoolroomsWorld>();worldB.enabled=false;worldB.generateOnStart=false;
                var nodeA=new GameObject("Station A");nodeA.transform.SetParent(a.transform,false);nodeA.transform.position=Vector3.forward*5;var stationA=nodeA.AddComponent<PoolroomsStation>();stationA.world=worldA;
                var nodeB=new GameObject("Station B");nodeB.transform.SetParent(b.transform,false);nodeB.transform.position=Vector3.forward*4;var stationB=nodeB.AddComponent<PoolroomsStation>();stationB.world=worldB;
                a.SetActive(true);b.SetActive(true);int cursor=0;
                Assert.That(PoolroomsStation.Nearby(worldA,Vector3.zero,ref cursor),Is.EqualTo(stationA));
                nodeA.SetActive(false);Assert.That(PoolroomsStation.Nearby(worldA,Vector3.zero,ref cursor),Is.Null);
                nodeA.SetActive(true);Assert.That(PoolroomsStation.Nearby(worldA,Vector3.zero,ref cursor),Is.EqualTo(stationA));
            }
            finally{if(a!=null)Object.DestroyImmediate(a);if(b!=null)Object.DestroyImmediate(b);}
        }
        [Test]
        public void OldPreferencesEnableNewRoomDetailsWhileExplicitOptOutStaysOff()
        {
            string key=PoolroomsGameSettings.PreferencesKey;bool had=PlayerPrefs.HasKey(key);string old=PlayerPrefs.GetString(key,"");GameObject actor=null;
            int rate=Application.targetFrameRate,sync=QualitySettings.vSyncCount;
            try
            {
                foreach(bool missing in new[]{true,false})
                {
                    var preferences=new GamePreferences();preferences.soundscape.roomDetails=false;
                    string json=JsonUtility.ToJson(preferences);
                    if(missing)json=json.Replace("\"roomDetails\":false,","");
                    PlayerPrefs.SetString(key,json);actor=new GameObject("Sound preference migration");actor.SetActive(false);actor.AddComponent<PoolroomsExplorer>().enabled=false;
                    var settings=actor.AddComponent<PoolroomsGameSettings>();settings.Initialize();
                    Assert.That(settings.Preferences.soundscape.roomDetails,Is.EqualTo(missing));Object.DestroyImmediate(actor);actor=null;
                }
            }
            finally
            {
                if(actor!=null)Object.DestroyImmediate(actor);if(had)PlayerPrefs.SetString(key,old);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();Application.targetFrameRate=rate;QualitySettings.vSyncCount=sync;
            }
        }
        [Test]
        public void OriginalFoleyLoadsAsBoundedMonoPcm()
        {
            var clips=Resources.LoadAll<AudioClip>("ImmersionAudio");Assert.That(clips.Length,Is.EqualTo(32));
            foreach(var clip in clips)
            {
                Assert.That(clip.channels,Is.EqualTo(1));Assert.That(clip.frequency,Is.EqualTo(24000));Assert.That(clip.LoadAudioData(),Is.True);
                var data=new float[clip.samples];Assert.That(clip.GetData(data,0),Is.True);float peak=0;
                foreach(float value in data){Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False);peak=Math.Max(peak,Math.Abs(value));}
                Assert.That(peak,Is.GreaterThan(.01f).And.LessThan(.461f));Assert.That(data[0],Is.Zero);Assert.That(data[data.Length-1],Is.Zero);
            }
        }
    }
}
#endif
