using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ChickenShooter.EditorTools
{
    /// <summary>Writes procedural WAV clips (chiptune BGM + arcade SFX) into the project.</summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 22050;

        // ---------------------------------------------------------------- wav IO

        public static AudioClip WriteWav(string assetPath, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");

            using (var stream = new FileStream(assetPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int sampleCount = samples.Length;
                int dataBytes = sampleCount * 2;

                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);

                for (int i = 0; i < sampleCount; i++)
                {
                    float value = Mathf.Clamp(samples[i], -1f, 1f);
                    writer.Write((short)Mathf.RoundToInt(value * 32000f));
                }
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer != null)
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        // ---------------------------------------------------------------- synth primitives

        private static float[] Buffer(float seconds) => new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate))];

        private static float Osc(float phase, int kind)
        {
            switch (kind)
            {
                case 1: return Mathf.Sign(Mathf.Sin(phase));
                case 2: return 2f * (phase / (Mathf.PI * 2f) - Mathf.Floor(phase / (Mathf.PI * 2f) + 0.5f));
                default: return Mathf.Sin(phase);
            }
        }

        private static void AddTone(float[] buffer, float startSeconds, float duration, float startFrequency, float endFrequency,
            float volume, int kind = 0, float vibrato = 0f)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(duration * SampleRate);
            double phase = 0.0;

            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;

                float t = i / (float)Mathf.Max(1, length);
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t);
                if (vibrato > 0f) frequency *= 1f + Mathf.Sin(i * 0.35f) * vibrato;

                phase += frequency / SampleRate * Mathf.PI * 2.0;
                float envelope = Mathf.Pow(1f - t, 1.7f) * Mathf.Clamp01(i / (SampleRate * 0.004f));
                buffer[index] += (float)Osc((float)phase, kind) * volume * envelope;
            }
        }

        private static void AddNoise(float[] buffer, float startSeconds, float duration, float volume, float decay = 2.2f, int seed = 3)
        {
            int start = Mathf.RoundToInt(startSeconds * SampleRate);
            int length = Mathf.RoundToInt(duration * SampleRate);
            var random = new System.Random(seed);
            float smoothed = 0f;

            for (int i = 0; i < length; i++)
            {
                int index = start + i;
                if (index < 0 || index >= buffer.Length) continue;

                float noise = (float)random.NextDouble() * 2f - 1f;
                smoothed = Mathf.Lerp(smoothed, noise, 0.35f);

                float t = i / (float)Mathf.Max(1, length);
                buffer[index] += smoothed * volume * Mathf.Pow(1f - t, decay);
            }
        }

        private static void Normalise(float[] buffer, float peak = 0.9f)
        {
            float max = 0.0001f;
            for (int i = 0; i < buffer.Length; i++) max = Mathf.Max(max, Mathf.Abs(buffer[i]));
            float scale = peak / max;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= scale;
        }

        // ---------------------------------------------------------------- SFX

        public static float[] Laser()
        {
            var buffer = Buffer(0.17f);
            AddTone(buffer, 0f, 0.15f, 1700f, 420f, 0.5f, 1);
            AddTone(buffer, 0f, 0.12f, 2400f, 900f, 0.25f, 2);
            AddNoise(buffer, 0f, 0.06f, 0.15f, 3.5f, 5);
            Normalise(buffer, 0.85f);
            return buffer;
        }

        public static float[] Explosion()
        {
            var buffer = Buffer(0.85f);
            AddNoise(buffer, 0f, 0.8f, 0.9f, 1.7f, 12);
            AddTone(buffer, 0f, 0.5f, 120f, 40f, 0.6f, 0);
            AddTone(buffer, 0.02f, 0.25f, 300f, 80f, 0.3f, 2);
            Normalise(buffer, 0.95f);
            return buffer;
        }

        public static float[] Cluck()
        {
            var buffer = Buffer(0.22f);
            AddTone(buffer, 0f, 0.08f, 900f, 1300f, 0.42f, 1);
            AddTone(buffer, 0.09f, 0.1f, 700f, 1050f, 0.38f, 1);
            AddNoise(buffer, 0f, 0.05f, 0.1f, 4f, 9);
            Normalise(buffer, 0.7f);
            return buffer;
        }

        public static float[] ChickenDeath()
        {
            var buffer = Buffer(0.42f);
            AddTone(buffer, 0f, 0.3f, 1200f, 200f, 0.45f, 1);
            AddNoise(buffer, 0.05f, 0.3f, 0.35f, 2.6f, 21);
            AddTone(buffer, 0.12f, 0.22f, 400f, 120f, 0.3f, 2);
            Normalise(buffer, 0.8f);
            return buffer;
        }

        public static float[] EggCrack()
        {
            var buffer = Buffer(0.16f);
            AddNoise(buffer, 0f, 0.13f, 0.8f, 3.2f, 33);
            AddTone(buffer, 0f, 0.08f, 1400f, 600f, 0.3f, 1);
            Normalise(buffer, 0.75f);
            return buffer;
        }

        public static float[] PickupFood()
        {
            var buffer = Buffer(0.28f);
            AddTone(buffer, 0f, 0.09f, 660f, 660f, 0.35f, 0);
            AddTone(buffer, 0.07f, 0.09f, 880f, 880f, 0.35f, 0);
            AddTone(buffer, 0.14f, 0.12f, 1320f, 1320f, 0.3f, 0);
            AddTone(buffer, 0.14f, 0.14f, 1320f, 1500f, 0.15f, 1);
            Normalise(buffer, 0.7f);
            return buffer;
        }

        public static float[] PickupGift()
        {
            var buffer = Buffer(0.4f);
            AddTone(buffer, 0f, 0.08f, 784f, 784f, 0.32f, 0);
            AddTone(buffer, 0.06f, 0.08f, 988f, 988f, 0.32f, 0);
            AddTone(buffer, 0.12f, 0.08f, 1175f, 1175f, 0.32f, 0);
            AddTone(buffer, 0.18f, 0.2f, 1568f, 1568f, 0.3f, 0, 0.01f);
            AddTone(buffer, 0.18f, 0.2f, 2093f, 2093f, 0.12f, 1);
            Normalise(buffer, 0.75f);
            return buffer;
        }

        public static float[] PlayerHit()
        {
            var buffer = Buffer(0.5f);
            AddTone(buffer, 0f, 0.4f, 220f, 70f, 0.5f, 2);
            AddTone(buffer, 0f, 0.3f, 110f, 55f, 0.4f, 1);
            AddNoise(buffer, 0f, 0.25f, 0.4f, 1.8f, 44);
            Normalise(buffer, 0.85f);
            return buffer;
        }

        // ---------------------------------------------------------------- BGM

        public sealed class BgmSpec
        {
            public int Bpm;
            public int Bars;
            public int[] ChordRoots;      // semitone offset of each bar's root (relative to A4)
            public int[] ArpPattern;      // semitone offsets played within the chord
            public int LeadKind;
            public float Brightness;
            public int Seed;
        }

        public static float[] BuildBgm(BgmSpec spec)
        {
            float secondsPerBeat = 60f / Mathf.Max(60, spec.Bpm);
            float secondsPerBar = secondsPerBeat * 4f;
            float totalSeconds = secondsPerBar * spec.Bars + 0.15f;

            var buffer = Buffer(totalSeconds);
            var random = new System.Random(spec.Seed);

            for (int bar = 0; bar < spec.Bars; bar++)
            {
                int root = spec.ChordRoots[bar % spec.ChordRoots.Length];
                float barStart = bar * secondsPerBar;
                float rootFrequency = NoteFrequency(root - 12);

                // Bass on beats 1 and 3
                AddTone(buffer, barStart, secondsPerBeat * 0.9f, rootFrequency, rootFrequency * 0.99f, 0.28f, 2);
                AddTone(buffer, barStart + secondsPerBeat * 2f, secondsPerBeat * 0.8f, rootFrequency * 1.5f,
                    rootFrequency * 1.5f, 0.2f, 2);

                // Arpeggio, one note per eighth
                for (int step = 0; step < 8; step++)
                {
                    int patternIndex = (step + bar) % spec.ArpPattern.Length;
                    int semitone = root + spec.ArpPattern[patternIndex];
                    float frequency = NoteFrequency(semitone);
                    float start = barStart + step * (secondsPerBeat * 0.5f);
                    float volume = (step % 2 == 0 ? 0.2f : 0.13f) * (0.8f + spec.Brightness * 0.4f);

                    AddTone(buffer, start, secondsPerBeat * 0.45f, frequency, frequency * 0.995f, volume, spec.LeadKind);
                }

                // Percussion: kick on 1 and 3, hat on every offbeat
                AddTone(buffer, barStart, 0.12f, 90f, 45f, 0.3f, 0);
                AddTone(buffer, barStart + secondsPerBeat * 2f, 0.12f, 90f, 45f, 0.26f, 0);

                for (int hat = 0; hat < 8; hat++)
                {
                    AddNoise(buffer, barStart + hat * secondsPerBeat * 0.5f + secondsPerBeat * 0.25f, 0.035f,
                        0.07f + (float)random.NextDouble() * 0.04f, 5f, spec.Seed + bar * 10 + hat);
                }

                // Sparkle lead on the last bar of each 4-bar phrase
                if (bar % 4 == 3)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float frequency = NoteFrequency(root + 12 + i * 3);
                        AddTone(buffer, barStart + secondsPerBeat * (2f + i * 0.25f), 0.2f, frequency, frequency, 0.12f, 1);
                    }
                }
            }

            // Short fade in/out keeps the loop seam clean.
            int fade = Mathf.RoundToInt(0.08f * SampleRate);
            for (int i = 0; i < fade && i < buffer.Length; i++)
            {
                float factor = i / (float)fade;
                buffer[i] *= factor;
                buffer[buffer.Length - 1 - i] *= factor;
            }

            Normalise(buffer, 0.72f);
            return buffer;
        }

        private static float NoteFrequency(int semitonesFromA4) => 440f * Mathf.Pow(2f, semitonesFromA4 / 12f);
    }
}
