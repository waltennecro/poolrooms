using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // One listener-wide pair of sources. Never one AudioSource per tree/cell.
    [DisallowMultipleComponent]
    public sealed class PoolroomsLevel10Ambience : MonoBehaviour
    {
        PoolroomsExplorer explorer;
        PoolroomsAmbientAudio ambience;
        PoolroomsGameSettings settings;
        PoolroomsSoundscape soundscape;
        AudioSource wind, leaves;
        AudioLowPassFilter windFilter;
        AudioClip[] rustles;
        GameObject root;
        Room room;
        Level10Plan plan;
        float nextRoomCheck, nextRustle, gain;
        int sequence;
        bool paused;

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            explorer = GetComponent<PoolroomsExplorer>();
            ambience = GetComponent<PoolroomsAmbientAudio>();
            settings = GetComponent<PoolroomsGameSettings>();
            soundscape = GetComponent<PoolroomsSoundscape>();
            root = new GameObject("Level 10 / listener-wide field ambience");
            wind = Source("Field wind");
            leaves = Source("Nearby wheat rustle");
            windFilter = wind.gameObject.AddComponent<AudioLowPassFilter>();
            windFilter.cutoffFrequency = 16000;
            wind.clip = Resources.Load<AudioClip>("Level10/Audio/FieldWind");
            wind.loop = true;
            wind.volume = 0;
            if (wind.clip != null) wind.Play();
            rustles = new AudioClip[3];
            for (int index = 0; index < rustles.Length; index++)
                rustles[index] = Resources.Load<AudioClip>("Level10/Audio/GrainRustle" + index);
            nextRoomCheck = 0;
            nextRustle = Time.unscaledTime + 4;
            gain = 0;
            paused = false;
        }

        AudioSource Source(string label)
        {
            var go = new GameObject(label);
            go.transform.SetParent(root.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1;
            source.dopplerLevel = 0;
            source.minDistance = 7;
            source.maxDistance = 45;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.spread = 130;
            source.reverbZoneMix = 0;
            source.priority = 160;
            return source;
        }

        void Update()
        {
            if (root == null || explorer == null || wind == null || explorer.world == null) return;
            if(ambience==null)ambience=GetComponent<PoolroomsAmbientAudio>();
            if(settings==null)settings=GetComponent<PoolroomsGameSettings>();
            if(soundscape==null)soundscape=GetComponent<PoolroomsSoundscape>();
            bool shouldPause = explorer.IsPaused || !explorer.IsReady;
            if (paused != shouldPause)
            {
                paused = shouldPause;
                if (paused)
                {
                    wind.Pause();
                    leaves.Pause();
                }
                else
                {
                    wind.UnPause();
                    leaves.UnPause();
                    nextRoomCheck = 0;
                    nextRustle = Time.unscaledTime + 3;
                }
            }
            if (paused) return;
            if (Time.unscaledTime >= nextRoomCheck)
            {
                nextRoomCheck = Time.unscaledTime + .35f;
                room = explorer.world.RoomAtViewer();
                plan = room?.Plan?.Level10;
                if (plan != null) AnchorSources();
            }
            float master = settings != null ? settings.EffectiveMasterVolume : ambience != null ? ambience.masterVolume : .8f;
            float environment = ambience != null ? ambience.environmentVolume : .8f;
            float duck = soundscape != null ? soundscape.AmbienceGain : 1;
            float target = plan == null ? 0 : Mathf.Clamp01(master) * Mathf.Clamp01(environment) * duck;
            gain = master <= 0 ? 0 : Mathf.MoveTowards(gain, target, Time.unscaledDeltaTime * .45f);
            leaves.volume = gain * .18f;
            wind.volume = gain * .21f;
            bool spatial = ambience == null || ambience.spatialAmbience;
            wind.spatialBlend = leaves.spatialBlend = spatial ? 1 : 0;
            if (windFilter != null)
                windFilter.cutoffFrequency = Mathf.Lerp(windFilter.cutoffFrequency,
                    Underwater() ? 1100 : 16000, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 3));
            if (plan == null || gain < .01f || Time.unscaledTime < nextRustle || leaves.isPlaying) return;
            sequence = (sequence + 1) % rustles.Length;
            nextRustle = Time.unscaledTime + 4 + (plan.Seed % 5);
            leaves.volume = gain * .18f;
            if (rustles[sequence] != null) leaves.PlayOneShot(rustles[sequence]);
        }

        bool Underwater()
        {
            return explorer.world.TryGetWater(transform.position,out float level)&&explorer.Aim!=null&&explorer.Aim.position.y<level-.08f;
        }

        void AnchorSources()
        {
            Vector3 origin = explorer.world.RoomWorldPosition(room);
            Vector3 ear = explorer.Aim != null ? explorer.Aim.position : transform.position;
            int nearest = -1;
            float distance = float.MaxValue;
            for (int index = 0; index < plan.Stalks.Length; index++)
            {
                var tree = plan.Stalks[index];
                Vector3 point = origin + new Vector3(tree.Foot.X, tree.Foot.Y + .8f, tree.Foot.Z);
                float squared = (point - ear).sqrMagnitude;
                if (squared >= distance) continue;
                nearest = index;
                distance = squared;
            }
            if (nearest < 0)
            {
                wind.transform.position = ear + Vector3.up * 4;
                leaves.transform.position = wind.transform.position;
                return;
            }
            var selected = plan.Stalks[nearest];
            wind.transform.position = origin + new Vector3(selected.Foot.X, selected.Foot.Y + 1.2f, selected.Foot.Z);
            leaves.transform.position = origin + new Vector3(selected.Foot.X, selected.Foot.Y + .9f, selected.Foot.Z);
        }

        void OnDisable()
        {
            if (wind != null) wind.Stop();
            if (leaves != null) leaves.Stop();
            if (root != null) Destroy(root);
            root = null;
            plan = null;
            room = null;
        }
    }
}
