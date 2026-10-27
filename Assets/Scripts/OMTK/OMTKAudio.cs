using System.Collections.Generic;
using UnityEngine;

namespace OMTK
{
    public class OMTKAudio
    {
        private AudioSource music;
        private AudioSource voice;
        private AudioSource effects;
        private AudioSource conductor;
        private AudioLowPassFilter conductorFilter;
        private GameObject host;
        private float musicLevel = 0.2f;
        private float duckCurrent = 1f;
        private float duckTarget = 1f;
        private readonly Dictionary<string, AudioClip> voiceCache = new Dictionary<string, AudioClip>();

        public AudioClip WindupStalker { get; private set; }
        public AudioClip WindupCharger { get; private set; }
        public AudioClip WindupConjurer { get; private set; }
        public AudioClip PresenceStalker { get; private set; }
        public AudioClip PresenceCharger { get; private set; }
        public AudioClip PresenceConjurer { get; private set; }
        public AudioClip ParryClip { get; private set; }
        public AudioClip PerfectClip { get; private set; }
        public AudioClip ForesightClip { get; private set; }
        public AudioClip StaggerClip { get; private set; }
        public AudioClip HitClip { get; private set; }
        public AudioClip SlashClip { get; private set; }
        public AudioClip WhiffClip { get; private set; }
        public AudioClip DeathClip { get; private set; }
        public AudioClip HeartbeatClip { get; private set; }
        public AudioClip EchoUnlockClip { get; private set; }
        public AudioClip VictoryClip { get; private set; }
        public AudioClip TickClip { get; private set; }
        public AudioClip ConductorReadyClip { get; private set; }

        public void Init(GameObject owner)
        {
            host = owner;
            music = CreateSource("Music", false);
            voice = CreateSource("Voice", false);
            effects = CreateSource("Effects", false);
            music.clip = AudioSynth.Drone();
            music.loop = true;
            music.volume = musicLevel;
            music.Play();

            WindupStalker = AudioSynth.Windup(EnemyVariant.Stalker);
            WindupCharger = AudioSynth.Windup(EnemyVariant.Charger);
            WindupConjurer = AudioSynth.Windup(EnemyVariant.Conjurer);
            PresenceStalker = AudioSynth.Presence(EnemyVariant.Stalker);
            PresenceCharger = AudioSynth.Presence(EnemyVariant.Charger);
            PresenceConjurer = AudioSynth.Presence(EnemyVariant.Conjurer);
            ParryClip = AudioSynth.Parry();
            PerfectClip = AudioSynth.Perfect();
            ForesightClip = AudioSynth.Foresight();
            StaggerClip = AudioSynth.Stagger();
            HitClip = AudioSynth.Hit();
            SlashClip = AudioSynth.Slash();
            WhiffClip = AudioSynth.Whiff();
            DeathClip = AudioSynth.Death();
            HeartbeatClip = AudioSynth.Heartbeat();
            EchoUnlockClip = AudioSynth.EchoUnlock();
            VictoryClip = AudioSynth.Victory();
            TickClip = AudioSynth.Tick();
            ConductorReadyClip = AudioSynth.ConductorReady();
        }

        public AudioClip Windup(EnemyVariant variant)
        {
            return variant == EnemyVariant.Charger ? WindupCharger : variant == EnemyVariant.Conjurer ? WindupConjurer : WindupStalker;
        }

        public AudioClip Presence(EnemyVariant variant)
        {
            return variant == EnemyVariant.Charger ? PresenceCharger : variant == EnemyVariant.Conjurer ? PresenceConjurer : PresenceStalker;
        }

        public void Tick()
        {
            duckCurrent = Mathf.MoveTowards(duckCurrent, duckTarget, Time.unscaledDeltaTime * 2.8f);
            if (music != null)
            {
                music.volume = musicLevel * duckCurrent;
            }
        }

        public void Duck(float level)
        {
            duckTarget = Mathf.Clamp01(level);
        }

        public void Unduck()
        {
            duckTarget = 1f;
        }

        public void PlayMusic()
        {
            if (music != null && !music.isPlaying)
            {
                music.Play();
            }
        }

        public void StopMusic()
        {
            if (music != null)
            {
                music.Stop();
            }
        }

        public void Speak(string text)
        {
            if (voice == null)
            {
                return;
            }

            AudioClip clip;
            if (!voiceCache.TryGetValue(text, out clip))
            {
                clip = AudioSynth.VoiceLine(text);
                voiceCache[text] = clip;
            }

            voice.Stop();
            voice.clip = clip;
            voice.Play();
        }

        public void StopVoice()
        {
            if (voice != null)
            {
                voice.Stop();
            }
        }

        public void Play2D(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || effects == null)
            {
                return;
            }

            effects.pitch = pitch;
            effects.PlayOneShot(clip, volume);
        }

        public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, bool lowPass = false)
        {
            if (clip == null)
            {
                return;
            }

            GameObject go = new GameObject("SpatialCue");
            go.transform.SetParent(host.transform, false);
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 14f;
            source.dopplerLevel = 0f;
            source.pitch = pitch;
            source.volume = volume;
            if (lowPass)
            {
                AudioLowPassFilter filter = go.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = 1600f;
            }
            source.PlayOneShot(clip);
            Object.Destroy(go, clip.length + 0.15f);
        }

        public void PlayFatal(AudioClip clip, Vector3 position)
        {
            Duck(0.05f);
            PlayAt(clip, position, 1f, 0.9f, false);
        }

        public void StartConductor(Vector3 position)
        {
            StopConductor();
            GameObject go = new GameObject("Conductor");
            go.transform.SetParent(host.transform, false);
            go.transform.position = position;
            conductor = go.AddComponent<AudioSource>();
            conductor.playOnAwake = false;
            conductor.loop = true;
            conductor.spatialBlend = 1f;
            conductor.rolloffMode = AudioRolloffMode.Linear;
            conductor.minDistance = 2f;
            conductor.maxDistance = 18f;
            conductor.dopplerLevel = 0f;
            conductor.volume = 0f;
            conductor.clip = AudioSynth.ConductorHum();
            conductorFilter = go.AddComponent<AudioLowPassFilter>();
            conductorFilter.cutoffFrequency = 700f;
            conductor.Play();
        }

        public void UpdateConductor(float playerYaw)
        {
            if (conductor == null || conductorFilter == null)
            {
                return;
            }

            float angle = Mathf.Abs(Mathf.DeltaAngle(playerYaw, 180f));
            float facing = 1f - Mathf.Clamp01(angle / 180f);
            conductorFilter.cutoffFrequency = Mathf.Lerp(650f, 5000f, facing * facing);
            conductor.volume = Mathf.Lerp(0.12f, 0.85f, facing);
        }

        public void StopConductor()
        {
            if (conductor != null)
            {
                Object.Destroy(conductor.gameObject);
                conductor = null;
                conductorFilter = null;
            }
        }

        private AudioSource CreateSource(string name, bool spatial)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(host.transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatial ? 1f : 0f;
            return source;
        }
    }
}
