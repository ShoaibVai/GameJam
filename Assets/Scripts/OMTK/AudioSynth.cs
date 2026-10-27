using System;
using UnityEngine;

namespace OMTK
{
    public static class AudioSynth
    {
        private enum Wave
        {
            Sine,
            Triangle,
            Saw,
            Square
        }

        public static AudioClip Windup(EnemyVariant variant)
        {
            float duration = variant == EnemyVariant.Charger ? 0.65f : variant == EnemyVariant.Stalker ? 0.85f : 0.95f;
            float[] data = Buffer(duration);

            if (variant == EnemyVariant.Stalker)
            {
                AddSweep(data, 170f, 720f, 0f, duration, 0.24f, Wave.Triangle, 0.015f, 0.12f, 9f, 0.025f);
                AddSweep(data, 340f, 1440f, 0.01f, duration, 0.08f, Wave.Sine, 0.01f, 0.18f, 11f, 0.02f);
                AddFilteredNoise(data, 0f, duration, 0.05f, 1400f, 0.18f);
            }
            else if (variant == EnemyVariant.Charger)
            {
                AddSweep(data, 95f, 1500f, 0f, duration, 0.26f, Wave.Saw, 0.008f, 0.1f, 18f, 0.02f);
                AddSweep(data, 500f, 2100f, 0.03f, duration, 0.07f, Wave.Square, 0.005f, 0.15f);
                AddClick(data, 0.02f, 0.18f, 750f);
                AddClick(data, 0.14f, 0.12f, 620f);
            }
            else
            {
                AddTone(data, 68f, 0f, duration, 0.22f, Wave.Sine, 0.02f, 0.15f, 4.2f, 0.03f);
                AddTone(data, 136f, 0f, duration, 0.1f, Wave.Triangle, 0.02f, 0.2f, 4.2f, 0.015f);
                AddTone(data, 980f, 0f, duration, 0.045f, Wave.Sine, 0.02f, 0.25f, 6.5f, 0.03f);
                AddFilteredNoise(data, 0.02f, duration, 0.04f, 2200f, 0.12f);
            }

            Normalize(data, 0.72f);
            FadeEdges(data, 0.012f);
            return Make("windup_" + variant, data);
        }

        public static AudioClip Presence(EnemyVariant variant)
        {
            float duration = 4f;
            float[] data = Buffer(duration);

            if (variant == EnemyVariant.Stalker)
            {
                AddPulseTone(data, 45f, duration, 0.12f, Wave.Sine, 0.25f);
                AddPulseTone(data, 67.5f, duration, 0.06f, Wave.Triangle, 0.25f);
            }
            else if (variant == EnemyVariant.Charger)
            {
                AddPulseTone(data, 55f, duration, 0.14f, Wave.Saw, 1f);
                AddPulseTone(data, 82.5f, duration, 0.07f, Wave.Sine, 1f);
            }
            else
            {
                AddPulseTone(data, 40f, duration, 0.13f, Wave.Sine, 0.3333333f);
                AddPulseTone(data, 60f, duration, 0.07f, Wave.Triangle, 0.3333333f);
                AddPulseTone(data, 120f, duration, 0.035f, Wave.Sine, 0.3333333f);
            }

            Normalize(data, 0.65f);
            return Make("presence_" + variant, data);
        }

        public static AudioClip Drone()
        {
            float[] data = Buffer(17f);
            AddNoiseBed(data, 0f, 16f, 0.018f, 850f, 2.5f, 2.5f, 0.09f);
            AddChord(data, 0f, 4.6f, new[] { 45, 52, 57, 60, 64 }, 0.085f, 0.9f, 1.6f, 0.0015f);
            AddChord(data, 4f, 4.6f, new[] { 41, 48, 53, 57, 60, 64 }, 0.08f, 1.1f, 1.8f, 0.0012f);
            AddChord(data, 8f, 4.6f, new[] { 38, 45, 50, 53, 57, 64 }, 0.08f, 1.1f, 1.8f, 0.0014f);
            AddChord(data, 12f, 4.6f, new[] { 40, 47, 52, 56, 59, 64 }, 0.085f, 0.9f, 1.6f, 0.0013f);
            AddBell(data, Midi(69), 1.3f, 0.06f, 2.4f);
            AddBell(data, Midi(72), 3.1f, 0.045f, 2f);
            AddBell(data, Midi(76), 5.2f, 0.055f, 2.2f);
            AddBell(data, Midi(72), 7f, 0.04f, 1.9f);
            AddBell(data, Midi(74), 9.2f, 0.055f, 2.3f);
            AddBell(data, Midi(77), 11f, 0.04f, 1.8f);
            AddBell(data, Midi(71), 13.2f, 0.055f, 2.2f);
            AddBell(data, Midi(68), 14.8f, 0.045f, 1.9f);
            ApplyReverb(data, 0.32f, 0.72f, true);
            Normalize(data, 0.8f);
            return Make("drone", LoopCrossfade(data, 16f, 1f));
        }

        public static AudioClip Tension()
        {
            float[] data = Buffer(9f);
            AddPulseTone(data, 41.2f, 8f, 0.09f, Wave.Sine, 0.5f);
            AddPulseTone(data, 55f, 8f, 0.035f, Wave.Triangle, 0.25f);
            AddTone(data, Midi(56), 0f, 8f, 0.024f, Wave.Triangle, 2f, 2f, 3.2f, 0.002f);
            AddTone(data, Midi(57), 0f, 8f, 0.02f, Wave.Sine, 2.5f, 2.5f, 3.6f, 0.002f);
            AddTone(data, Midi(63), 0f, 8f, 0.01f, Wave.Sine, 3f, 3f, 2.8f, 0.003f);
            AddNoiseBed(data, 0f, 8f, 0.01f, 1200f, 2f, 2f, 0.18f);
            AddBell(data, Midi(80), 1.8f, 0.016f, 3f);
            AddBell(data, Midi(81), 4.8f, 0.012f, 3f);
            ApplyReverb(data, 0.32f, 0.7f, true);
            Normalize(data, 0.62f);
            return Make("tension", LoopCrossfade(data, 8f, 1f));
        }

        public static AudioClip ConductorHum()
        {
            float duration = 6f;
            float[] data = Buffer(duration);
            AddPulseTone(data, 55f, duration, 0.16f, Wave.Sine, 0.1666667f);
            AddPulseTone(data, 110f, duration, 0.08f, Wave.Triangle, 0.1666667f);
            AddPulseTone(data, 165f, duration, 0.055f, Wave.Sine, 0.3333333f);
            AddPulseTone(data, 220f, duration, 0.035f, Wave.Sine, 0.1666667f);
            AddPulseTone(data, 1318.5f, duration, 0.01f, Wave.Sine, 0.5f);
            Normalize(data, 0.72f);
            return Make("conductor_hum", data);
        }

        public static AudioClip VoiceLine(string text)
        {
            int seed = 17;
            for (int i = 0; i < text.Length; i++)
            {
                seed = seed * 31 + text[i];
            }

            System.Random rng = new System.Random(seed);
            int syllables = Mathf.Clamp(2 + text.Length / 9, 2, 9);
            float cursor = 0.04f;
            float total = 0.08f;
            for (int i = 0; i < syllables; i++)
            {
                float syllable = 0.09f + (float)rng.NextDouble() * 0.1f;
                total += syllable + 0.035f;
                cursor += syllable + 0.035f;
            }

            float[] data = Buffer(Mathf.Max(cursor, total));
            cursor = 0.04f;
            for (int i = 0; i < syllables; i++)
            {
                float syllable = 0.09f + (float)rng.NextDouble() * 0.1f;
                float baseFreq = 78f + (float)rng.NextDouble() * 42f;
                float amp = 0.13f + (float)rng.NextDouble() * 0.05f;
                AddTone(data, baseFreq, cursor, syllable, amp, Wave.Saw, 0.012f, 0.055f, 5.5f, 0.018f);
                AddTone(data, baseFreq * 2f, cursor, syllable, amp * 0.5f, Wave.Sine, 0.012f, 0.06f, 5.5f, 0.012f);
                AddTone(data, baseFreq * 3f, cursor, syllable, amp * 0.22f, Wave.Sine, 0.01f, 0.05f, 6.2f, 0.01f);
                AddFilteredNoise(data, cursor, syllable, 0.018f, 1500f + (float)rng.NextDouble() * 1600f, 0.14f);
                cursor += syllable + 0.035f;
            }

            Normalize(data, 0.68f);
            FadeEdges(data, 0.01f);
            return Make("voice", data);
        }

        public static AudioClip Parry()
        {
            float[] data = Buffer(0.36f);
            AddTone(data, 1180f, 0f, 0.36f, 0.2f, Wave.Sine, 0.002f, 0.34f);
            AddTone(data, 1760f, 0f, 0.26f, 0.13f, Wave.Sine, 0.002f, 0.24f);
            AddTone(data, 2410f, 0f, 0.18f, 0.08f, Wave.Sine, 0.002f, 0.16f);
            AddFilteredNoise(data, 0f, 0.08f, 0.08f, 5000f, 0.35f);
            Normalize(data, 0.72f);
            FadeEdges(data, 0.008f);
            return Make("parry", data);
        }

        public static AudioClip Perfect()
        {
            float[] data = Buffer(0.62f);
            AddTone(data, 880f, 0f, 0.6f, 0.15f, Wave.Sine, 0.002f, 0.56f);
            AddTone(data, 1320f, 0.04f, 0.54f, 0.11f, Wave.Sine, 0.002f, 0.5f);
            AddTone(data, 1760f, 0.08f, 0.5f, 0.09f, Wave.Sine, 0.002f, 0.46f);
            AddTone(data, 2640f, 0.12f, 0.4f, 0.05f, Wave.Sine, 0.002f, 0.36f);
            Normalize(data, 0.78f);
            FadeEdges(data, 0.008f);
            return Make("perfect", data);
        }

        public static AudioClip Foresight()
        {
            float[] data = Buffer(0.25f);
            AddTone(data, 1760f, 0f, 0.24f, 0.15f, Wave.Sine, 0.002f, 0.2f);
            AddTone(data, 2640f, 0f, 0.16f, 0.08f, Wave.Sine, 0.002f, 0.12f);
            Normalize(data, 0.62f);
            FadeEdges(data, 0.008f);
            return Make("foresight", data);
        }

        public static AudioClip Stagger()
        {
            float[] data = Buffer(0.48f);
            AddSweep(data, 540f, 160f, 0f, 0.48f, 0.2f, Wave.Triangle, 0.005f, 0.3f, 7f, 0.05f);
            AddFilteredNoise(data, 0f, 0.2f, 0.08f, 1200f, 0.3f);
            Normalize(data, 0.7f);
            FadeEdges(data, 0.01f);
            return Make("stagger", data);
        }

        public static AudioClip Hit()
        {
            float[] data = Buffer(0.38f);
            AddSweep(data, 110f, 42f, 0f, 0.38f, 0.32f, Wave.Sine, 0.002f, 0.3f);
            AddFilteredNoise(data, 0f, 0.16f, 0.14f, 740f, 0.35f);
            Normalize(data, 0.82f);
            FadeEdges(data, 0.008f);
            return Make("hit", data);
        }

        public static AudioClip Slash()
        {
            float[] data = Buffer(0.22f);
            AddFilteredNoise(data, 0f, 0.2f, 0.16f, 5200f, 0.55f);
            AddSweep(data, 900f, 2600f, 0f, 0.16f, 0.05f, Wave.Saw, 0.002f, 0.14f);
            Normalize(data, 0.62f);
            FadeEdges(data, 0.006f);
            return Make("slash", data);
        }

        public static AudioClip Whiff()
        {
            float[] data = Buffer(0.16f);
            AddFilteredNoise(data, 0f, 0.14f, 0.09f, 1200f, 0.35f);
            Normalize(data, 0.42f);
            FadeEdges(data, 0.008f);
            return Make("whiff", data);
        }

        public static AudioClip Death()
        {
            float[] data = Buffer(1.25f);
            AddSweep(data, 260f, 38f, 0f, 1.25f, 0.24f, Wave.Triangle, 0.005f, 0.8f, 4f, 0.03f);
            AddFilteredNoise(data, 0f, 0.8f, 0.08f, 900f, 0.25f);
            AddTone(data, 55f, 0.1f, 1.1f, 0.12f, Wave.Sine, 0.02f, 0.9f);
            Normalize(data, 0.78f);
            FadeEdges(data, 0.01f);
            return Make("death", data);
        }

        public static AudioClip Heartbeat()
        {
            float[] data = Buffer(0.55f);
            AddSweep(data, 75f, 36f, 0f, 0.18f, 0.2f, Wave.Sine, 0.002f, 0.15f);
            AddSweep(data, 70f, 34f, 0.15f, 0.2f, 0.17f, Wave.Sine, 0.002f, 0.15f);
            Normalize(data, 0.8f);
            FadeEdges(data, 0.008f);
            return Make("heartbeat", data);
        }

        public static AudioClip EchoUnlock()
        {
            float[] data = Buffer(0.9f);
            AddTone(data, 660f, 0f, 0.85f, 0.12f, Wave.Sine, 0.01f, 0.7f);
            AddTone(data, 990f, 0.1f, 0.72f, 0.09f, Wave.Sine, 0.01f, 0.6f);
            AddTone(data, 1320f, 0.2f, 0.6f, 0.07f, Wave.Sine, 0.01f, 0.5f);
            Normalize(data, 0.66f);
            FadeEdges(data, 0.01f);
            return Make("echo_unlock", data);
        }

        public static AudioClip Victory()
        {
            float[] data = Buffer(2.2f);
            AddTone(data, 220f, 0f, 2.1f, 0.13f, Wave.Sine, 0.02f, 1.8f);
            AddTone(data, 330f, 0.12f, 1.9f, 0.1f, Wave.Sine, 0.02f, 1.6f);
            AddTone(data, 440f, 0.24f, 1.7f, 0.08f, Wave.Sine, 0.02f, 1.4f);
            AddTone(data, 660f, 0.36f, 1.5f, 0.06f, Wave.Sine, 0.02f, 1.2f);
            AddTone(data, 880f, 0.48f, 1.3f, 0.04f, Wave.Sine, 0.02f, 1f);
            Normalize(data, 0.7f);
            FadeEdges(data, 0.01f);
            return Make("victory", data);
        }

        public static AudioClip Tick()
        {
            float[] data = Buffer(0.05f);
            AddTone(data, 1900f, 0f, 0.045f, 0.2f, Wave.Sine, 0.001f, 0.04f);
            Normalize(data, 0.5f);
            FadeEdges(data, 0.003f);
            return Make("tick", data);
        }

        public static AudioClip ConductorReady()
        {
            float[] data = Buffer(0.42f);
            AddTone(data, 440f, 0f, 0.4f, 0.15f, Wave.Sine, 0.005f, 0.34f);
            AddTone(data, 660f, 0.06f, 0.32f, 0.1f, Wave.Sine, 0.005f, 0.28f);
            AddTone(data, 880f, 0.12f, 0.25f, 0.07f, Wave.Sine, 0.005f, 0.22f);
            Normalize(data, 0.65f);
            FadeEdges(data, 0.008f);
            return Make("conductor_ready", data);
        }

        public static AudioClip RadarPing()
        {
            float[] data = Buffer(0.24f);
            AddSweep(data, 1560f, 1040f, 0f, 0.22f, 0.16f, Wave.Sine, 0.005f, 0.2f);
            AddDecayTone(data, 2340f, 0.002f, 0.13f, 0.035f, Wave.Sine, 0.002f, 0.04f);
            Normalize(data, 0.5f);
            FadeEdges(data, 0.004f);
            return Make("radar_ping", data);
        }

        public static AudioClip Tinnitus()
        {
            float duration = OMTKConfig.TinnitusDuration;
            float[] data = Buffer(duration);
            AddTone(data, OMTKConfig.TinnitusFrequency, 0f, duration, 0.16f, Wave.Sine, 0.018f, 0.24f);
            AddTone(data, OMTKConfig.TinnitusFrequency * 1.006f, 0f, duration, 0.08f, Wave.Sine, 0.02f, 0.24f);
            AddTone(data, OMTKConfig.TinnitusFrequency * 2f, 0f, duration, 0.055f, Wave.Sine, 0.024f, 0.28f);
            AddTone(data, OMTKConfig.TinnitusFrequency * 3f, 0f, duration * 0.72f, 0.025f, Wave.Sine, 0.03f, 0.24f);
            Normalize(data, 0.56f);
            FadeEdges(data, 0.02f);
            return Make("tinnitus", data);
        }

        public static AudioClip Glance()
        {
            float[] data = Buffer(0.3f);
            AddSweep(data, 2300f, 620f, 0f, 0.27f, 0.16f, Wave.Triangle, 0.003f, 0.2f, 24f, 0.04f);
            AddTone(data, 380f, 0f, 0.18f, 0.11f, Wave.Sine, 0.003f, 0.14f);
            AddFilteredNoise(data, 0f, 0.16f, 0.1f, 3400f, 0.28f);
            Normalize(data, 0.66f);
            FadeEdges(data, 0.006f);
            return Make("glance", data);
        }

        public static AudioClip OffAxis()
        {
            float[] data = Buffer(0.22f);
            AddFilteredNoise(data, 0f, 0.2f, 0.12f, 760f, 0.2f);
            AddSweep(data, 720f, 240f, 0f, 0.2f, 0.06f, Wave.Triangle, 0.004f, 0.16f);
            Normalize(data, 0.46f);
            FadeEdges(data, 0.008f);
            return Make("off_axis", data);
        }

        public static AudioClip ResonanceLock(EnemyVariant variant)
        {
            float[] data = Buffer(0.28f);
            if (variant == EnemyVariant.Stalker)
            {
                AddTone(data, 720f, 0f, 0.26f, 0.1f, Wave.Sine, 0.006f, 0.24f);
                AddTone(data, 1080f, 0.02f, 0.22f, 0.06f, Wave.Sine, 0.006f, 0.2f);
                AddTone(data, 1440f, 0.04f, 0.18f, 0.035f, Wave.Sine, 0.006f, 0.16f);
            }
            else if (variant == EnemyVariant.Charger)
            {
                AddTone(data, 330f, 0f, 0.26f, 0.11f, Wave.Triangle, 0.006f, 0.24f);
                AddTone(data, 660f, 0.02f, 0.22f, 0.07f, Wave.Sine, 0.006f, 0.2f);
                AddTone(data, 990f, 0.04f, 0.18f, 0.04f, Wave.Sine, 0.006f, 0.16f);
            }
            else
            {
                AddTone(data, 980f, 0f, 0.26f, 0.1f, Wave.Sine, 0.006f, 0.24f);
                AddTone(data, 1470f, 0.02f, 0.22f, 0.06f, Wave.Sine, 0.006f, 0.2f);
                AddTone(data, 1960f, 0.04f, 0.18f, 0.035f, Wave.Sine, 0.006f, 0.16f);
            }

            AddFilteredNoise(data, 0f, 0.08f, 0.025f, 4200f, 0.7f);
            Normalize(data, 0.5f);
            FadeEdges(data, 0.006f);
            return Make("resonance_lock_" + variant, data);
        }

        private static float Midi(int note)
        {
            return 440f * Mathf.Pow(2f, (note - 69) / 12f);
        }

        private static void AddChord(float[] data, float start, float duration, int[] notes, float amplitude, float attack, float release, float detune)
        {
            for (int i = 0; i < notes.Length; i++)
            {
                float frequency = Midi(notes[i]);
                AddTone(data, frequency * (1f - detune), start, duration, amplitude, Wave.Triangle, attack, release, 4.2f + i * 0.05f, 0.002f);
                AddTone(data, frequency * (1f + detune), start, duration, amplitude * 0.6f, Wave.Sine, attack, release, 4f + i * 0.04f, 0.0015f);
                AddTone(data, frequency * 2f, start, duration, amplitude * 0.12f, Wave.Sine, attack, release, 3.7f, 0.001f);
                if (i == 0)
                {
                    AddTone(data, frequency * 0.5f, start, duration, amplitude * 0.55f, Wave.Sine, attack, release);
                }
            }
        }

        private static void AddDecayTone(float[] data, float frequency, float start, float duration, float amplitude, Wave wave, float attack, float decay, float vibratoFrequency = 0f, float vibratoDepth = 0f)
        {
            int startSample = Mathf.Clamp(Mathf.RoundToInt(start * OMTKConfig.SampleRate), 0, data.Length - 1);
            int endSample = Mathf.Clamp(startSample + Mathf.RoundToInt(duration * OMTKConfig.SampleRate), 0, data.Length);
            float phase = 0f;
            for (int i = startSample; i < endSample; i++)
            {
                float time = (i - startSample) / (float)OMTKConfig.SampleRate;
                float envelope = (attack <= 0f ? 1f : Mathf.Clamp01(time / attack)) * Mathf.Exp(-time / Mathf.Max(0.001f, decay));
                envelope *= Mathf.Clamp01((duration - time) / 0.02f);
                float modulation = vibratoDepth * Mathf.Sin(time * vibratoFrequency * Mathf.PI * 2f);
                phase += frequency * (1f + modulation) / OMTKConfig.SampleRate;
                phase -= Mathf.Floor(phase);
                data[i] += WaveSample(wave, phase) * amplitude * envelope;
            }
        }

        private static void AddBell(float[] data, float frequency, float start, float amplitude, float duration)
        {
            AddDecayTone(data, frequency, start, duration, amplitude, Wave.Sine, 0.003f, duration * 0.45f);
            AddDecayTone(data, frequency * 2.01f, start, duration * 0.9f, amplitude * 0.35f, Wave.Sine, 0.002f, duration * 0.35f);
            AddDecayTone(data, frequency * 2.99f, start, duration * 0.7f, amplitude * 0.18f, Wave.Sine, 0.002f, duration * 0.28f);
            AddDecayTone(data, frequency * 4.18f, start, duration * 0.5f, amplitude * 0.08f, Wave.Sine, 0.002f, duration * 0.2f);
        }

        private static void AddNoiseBed(float[] data, float start, float duration, float amplitude, float cutoff, float attack, float release, float modulationFrequency)
        {
            int startSample = Mathf.Clamp(Mathf.RoundToInt(start * OMTKConfig.SampleRate), 0, data.Length - 1);
            int endSample = Mathf.Clamp(startSample + Mathf.RoundToInt(duration * OMTKConfig.SampleRate), 0, data.Length);
            System.Random rng = new System.Random(startSample + 117);
            float filtered = 0f;
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / OMTKConfig.SampleRate);
            for (int i = startSample; i < endSample; i++)
            {
                float time = (i - startSample) / (float)OMTKConfig.SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                filtered += alpha * (noise - filtered);
                float modulation = 0.65f + 0.35f * Mathf.Sin(time * modulationFrequency * Mathf.PI * 2f + 0.7f);
                float envelope = Envelope(time, duration, attack, release);
                data[i] += filtered * amplitude * envelope * modulation;
            }
        }

        private static void ApplyReverb(float[] data, float wet, float decay, bool wrap)
        {
            int length = data.Length;
            int tail = wrap ? Mathf.Min(OMTKConfig.SampleRate, length / 2) : 0;
            int total = length + tail;
            float[] wetData = new float[length];
            int[] delays = { 1557, 1617, 1491, 1422 };
            for (int c = 0; c < delays.Length; c++)
            {
                int delay = delays[c];
                float[] buffer = new float[delay];
                float low = 0f;
                int index = 0;
                for (int i = 0; i < total; i++)
                {
                    float input = i < length ? data[i] : 0f;
                    float delayed = buffer[index];
                    low += 0.38f * (delayed - low);
                    buffer[index] = input + low * decay;
                    int outIndex = i < length ? i : i - length;
                    wetData[outIndex] += delayed * 0.25f;
                    index++;
                    if (index >= delay)
                    {
                        index = 0;
                    }
                }
            }

            for (int i = 0; i < length; i++)
            {
                data[i] = data[i] * (1f - wet * 0.55f) + wetData[i] * wet;
            }
        }

        private static float[] LoopCrossfade(float[] data, float loopDuration, float fadeDuration)
        {
            int loopSamples = Mathf.Clamp(Mathf.RoundToInt(loopDuration * OMTKConfig.SampleRate), 1, data.Length);
            int fadeSamples = Mathf.Clamp(Mathf.RoundToInt(fadeDuration * OMTKConfig.SampleRate), 0, data.Length - loopSamples);
            float[] result = new float[loopSamples];
            Array.Copy(data, result, loopSamples);
            for (int i = 0; i < fadeSamples; i++)
            {
                float t = i / (float)Mathf.Max(1, fadeSamples);
                result[i] = data[i] * t + data[loopSamples + i] * (1f - t);
            }

            return result;
        }

        private static float[] Buffer(float duration)
        {
            return new float[Mathf.Max(1, Mathf.RoundToInt(duration * OMTKConfig.SampleRate))];
        }

        private static AudioClip Make(string name, float[] data)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, OMTKConfig.SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void AddTone(float[] data, float frequency, float start, float duration, float amplitude, Wave wave, float attack, float release, float vibratoFrequency = 0f, float vibratoDepth = 0f)
        {
            int startSample = Mathf.Clamp(Mathf.RoundToInt(start * OMTKConfig.SampleRate), 0, data.Length - 1);
            int endSample = Mathf.Clamp(startSample + Mathf.RoundToInt(duration * OMTKConfig.SampleRate), 0, data.Length);
            float phase = 0f;
            for (int i = startSample; i < endSample; i++)
            {
                float time = (i - startSample) / (float)OMTKConfig.SampleRate;
                float envelope = Envelope(time, duration, attack, release);
                float modulation = vibratoDepth * Mathf.Sin(time * vibratoFrequency * Mathf.PI * 2f);
                phase += frequency * (1f + modulation) / OMTKConfig.SampleRate;
                phase -= Mathf.Floor(phase);
                data[i] += WaveSample(wave, phase) * amplitude * envelope;
            }
        }

        private static void AddPulseTone(float[] data, float frequency, float duration, float amplitude, Wave wave, float pulseFrequency)
        {
            int samples = Mathf.Clamp(Mathf.RoundToInt(duration * OMTKConfig.SampleRate), 1, data.Length);
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)OMTKConfig.SampleRate;
                float pulse = 0.55f + 0.45f * Mathf.Sin(time * pulseFrequency * Mathf.PI * 2f);
                phase += frequency / OMTKConfig.SampleRate;
                phase -= Mathf.Floor(phase);
                data[i] += WaveSample(wave, phase) * amplitude * pulse;
            }
        }

        private static void AddSweep(float[] data, float startFrequency, float endFrequency, float start, float duration, float amplitude, Wave wave, float attack = 0.01f, float release = 0.1f, float vibratoFrequency = 0f, float vibratoDepth = 0f)
        {
            int startSample = Mathf.Clamp(Mathf.RoundToInt(start * OMTKConfig.SampleRate), 0, data.Length - 1);
            int endSample = Mathf.Clamp(startSample + Mathf.RoundToInt(duration * OMTKConfig.SampleRate), 0, data.Length);
            float phase = 0f;
            for (int i = startSample; i < endSample; i++)
            {
                float time = (i - startSample) / (float)OMTKConfig.SampleRate;
                float progress = Mathf.Clamp01(time / Mathf.Max(0.0001f, duration));
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                float envelope = Envelope(time, duration, attack, release);
                float modulation = vibratoDepth * Mathf.Sin(time * vibratoFrequency * Mathf.PI * 2f);
                phase += frequency * (1f + modulation) / OMTKConfig.SampleRate;
                phase -= Mathf.Floor(phase);
                data[i] += WaveSample(wave, phase) * amplitude * envelope;
            }
        }

        private static void AddFilteredNoise(float[] data, float start, float duration, float amplitude, float cutoff, float sweep)
        {
            int startSample = Mathf.Clamp(Mathf.RoundToInt(start * OMTKConfig.SampleRate), 0, data.Length - 1);
            int endSample = Mathf.Clamp(startSample + Mathf.RoundToInt(duration * OMTKConfig.SampleRate), 0, data.Length);
            System.Random rng = new System.Random(startSample + 91);
            float filtered = 0f;
            for (int i = startSample; i < endSample; i++)
            {
                float time = (i - startSample) / (float)OMTKConfig.SampleRate;
                float progress = Mathf.Clamp01(time / Mathf.Max(0.0001f, duration));
                float currentCutoff = Mathf.Lerp(cutoff, cutoff * sweep, progress);
                float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * currentCutoff / OMTKConfig.SampleRate);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                filtered += alpha * (noise - filtered);
                float envelope = Envelope(time, duration, 0.01f, 0.08f);
                data[i] += filtered * amplitude * envelope;
            }
        }

        private static void AddClick(float[] data, float start, float amplitude, float frequency)
        {
            AddTone(data, frequency, start, 0.035f, amplitude, Wave.Sine, 0.001f, 0.03f);
            AddFilteredNoise(data, start, 0.025f, amplitude * 0.5f, 4000f, 1f);
        }

        private static float Envelope(float time, float duration, float attack, float release)
        {
            float inValue = attack <= 0f ? 1f : Mathf.Clamp01(time / attack);
            float outValue = release <= 0f ? 1f : Mathf.Clamp01((duration - time) / release);
            return Mathf.Min(inValue, outValue);
        }

        private static float WaveSample(Wave wave, float phase)
        {
            if (wave == Wave.Triangle)
            {
                return 1f - 4f * Mathf.Abs(phase - 0.5f);
            }

            if (wave == Wave.Saw)
            {
                return phase * 2f - 1f;
            }

            if (wave == Wave.Square)
            {
                return phase < 0.5f ? 1f : -1f;
            }

            return Mathf.Sin(phase * Mathf.PI * 2f);
        }

        private static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                max = Mathf.Max(max, Mathf.Abs(data[i]));
            }

            if (max <= 0.0001f)
            {
                return;
            }

            float gain = peak / max;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= gain;
            }
        }

        private static void FadeEdges(float[] data, float duration)
        {
            int samples = Mathf.Max(1, Mathf.RoundToInt(duration * OMTKConfig.SampleRate));
            samples = Mathf.Min(samples, data.Length / 2);
            for (int i = 0; i < samples; i++)
            {
                float gain = i / (float)samples;
                data[i] *= gain;
                data[data.Length - 1 - i] *= gain;
            }
        }
    }
}
