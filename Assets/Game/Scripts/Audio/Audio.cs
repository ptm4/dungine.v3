using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Dungine.Audio
{
    /// <summary>DSP helpers for procedural sound.</summary>
    public static class Dsp
    {
        public const int SR = 44100;
        public const int MusicSR = 32000;
        public static float Sine(float ph) => Mathf.Sin(ph * Mathf.PI * 2);
        public static float Saw(float ph) => 2f * (ph - Mathf.Floor(ph + 0.5f));
        public static float Tri(float ph) => 1f - 4f * Mathf.Abs(Mathf.Round(ph - 0.25f) - (ph - 0.25f));
        public static float Sqr(float ph) => ph - Mathf.Floor(ph) < 0.5f ? 1f : -1f;
        public static float Midi(float n) => 440f * Mathf.Pow(2f, (n - 69) / 12f);
        public static float Env(float t, float a, float d, float s, float r, float len) { if (t < a) return t / a; if (t < a + d) return 1 - (1 - s) * (t - a) / d; if (t < len) return s; return Mathf.Max(0, s * (1 - (t - len) / r)); }
        public static float Perc(float t, float a, float decay) => t < a ? t / a : Mathf.Exp(-(t - a) / decay);

        public class Rand { uint s; public Rand(uint seed) { s = seed * 2654435761u + 1; } public float Next() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return (s & 0xFFFFFF) / 16777215f * 2f - 1f; } public float Next01() => Next() * 0.5f + 0.5f; }

        public class LP { float y; public float Process(float x, float a) { y += a * (x - y); return y; } }
        public class HP { float y, px; public float Process(float x, float a) { y = a * (y + x - px); px = x; return y; } }

        /// <summary>Resonant state-variable filter.</summary>
        public class SVF
        {
            float low, band;
            public float Process(float x, float cutoff, float q, int sr, out float hp)
            {
                float f = 2f * Mathf.Sin(Mathf.PI * Mathf.Min(cutoff, sr * 0.2f) / sr);
                low += f * band; hp = x - low - q * band; band += f * hp;
                return low;
            }
        }

        /// <summary>Schroeder reverb.</summary>
        public class Reverb
        {
            readonly float[][] comb; readonly int[] ci; readonly float[] cfb;
            readonly float[][] ap; readonly int[] ai;
            float damp; readonly float[] lpState;
            public Reverb(int sr, float size = 1f, float dampen = 0.3f)
            {
                int[] cl = { 1557, 1617, 1491, 1422 }; int[] al = { 225, 556 };
                comb = new float[4][]; ci = new int[4]; cfb = new float[4]; lpState = new float[4];
                for (int i = 0; i < 4; i++) { comb[i] = new float[(int)(cl[i] * size * sr / 44100f)]; cfb[i] = 0.83f; }
                ap = new float[2][]; ai = new int[2];
                for (int i = 0; i < 2; i++) ap[i] = new float[(int)(al[i] * sr / 44100f)];
                damp = dampen;
            }
            public float Process(float x)
            {
                float o = 0;
                for (int i = 0; i < 4; i++)
                {
                    var b = comb[i]; float y = b[ci[i]];
                    lpState[i] = y * (1 - damp) + lpState[i] * damp;
                    b[ci[i]] = x + lpState[i] * cfb[i];
                    ci[i] = (ci[i] + 1) % b.Length; o += y;
                }
                o *= 0.25f;
                for (int i = 0; i < 2; i++)
                {
                    var b = ap[i]; float bo = b[ai[i]];
                    float y = -o + bo; b[ai[i]] = o + bo * 0.5f; ai[i] = (ai[i] + 1) % b.Length; o = y;
                }
                return o;
            }
        }

        public static float SoftClip(float x) => x / (1f + Mathf.Abs(x));
    }

    /// <summary>Procedurally synthesised sound effects.</summary>
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();

        public static void Play(string id, float vol = 1f, float pitch = 1f)
        {
            if (AudioSys.I == null) return;
            if (lastPlayed.TryGetValue(id, out var t) && Time.unscaledTime - t < 0.04f) return;
            lastPlayed[id] = Time.unscaledTime;
            var c = Get(id);
            if (c != null) AudioSys.I.PlayOneShot(c, vol, pitch * UnityEngine.Random.Range(0.96f, 1.04f));
        }

        public static AudioClip Get(string id)
        {
            if (clips.TryGetValue(id, out var c)) return c;
            c = Make(id);
            clips[id] = c;
            return c;
        }

        static AudioClip Render(string name, float dur, Func<float, Dsp.Rand, float> fn, float reverb = 0f)
        {
            int n = (int)(dur * Dsp.SR);
            var data = new float[n];
            var r = new Dsp.Rand((uint)name.GetHashCode());
            var rv = reverb > 0 ? new Dsp.Reverb(Dsp.SR, 0.8f) : null;
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Dsp.SR;
                float x = fn(t, r);
                if (rv != null) x = x + rv.Process(x) * reverb;
                data[i] = x; peak = Mathf.Max(peak, Mathf.Abs(x));
            }
            float g = 0.85f / peak;
            for (int i = 0; i < n; i++) { data[i] *= g; if (i > n - 400) data[i] *= (n - i) / 400f; }
            var clip = AudioClip.Create(name, n, 1, Dsp.SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Make(string id)
        {
            var lp = new Dsp.LP(); var lp2 = new Dsp.LP(); var hp = new Dsp.HP(); var svf = new Dsp.SVF();
            switch (id)
            {
                case "click": return Render(id, 0.06f, (t, r) => (r.Next() * 0.4f + Dsp.Sine(t * 1800)) * Dsp.Perc(t, 0.001f, 0.012f));
                case "hover": return Render(id, 0.05f, (t, r) => Dsp.Sine(t * 2400) * Dsp.Perc(t, 0.002f, 0.01f) * 0.3f);
                case "open": return Render(id, 0.3f, (t, r) => lp.Process(r.Next(), 0.08f) * Dsp.Perc(t, 0.02f, 0.08f) + Dsp.Sine(t * 520) * Dsp.Perc(t, 0.01f, 0.1f) * 0.3f, 0.3f);
                case "close": return Render(id, 0.25f, (t, r) => lp.Process(r.Next(), 0.05f) * Dsp.Perc(t, 0.005f, 0.05f) + Dsp.Sine(t * 390) * Dsp.Perc(t, 0.005f, 0.08f) * 0.3f);
                case "select": return Render(id, 0.2f, (t, r) => (Dsp.Sine(t * 660) + Dsp.Sine(t * 990) * 0.5f) * Dsp.Perc(t, 0.005f, 0.07f), 0.3f);
                case "error": return Render(id, 0.25f, (t, r) => Dsp.Sqr(t * 140) * 0.3f * Dsp.Perc(t, 0.005f, 0.1f));
                case "page": return Render(id, 0.35f, (t, r) => hp.Process(r.Next(), 0.9f) * Dsp.Env(t, 0.03f, 0.1f, 0.3f, 0.15f, 0.15f) * (0.6f + 0.4f * Mathf.Sin(t * 60)));
                case "equip": return Render(id, 0.3f, (t, r) => (Dsp.Sine(t * 1320 + Dsp.Sine(t * 7) * 2) * 0.4f + hp.Process(r.Next(), 0.95f) * 0.6f) * Dsp.Perc(t, 0.002f, 0.06f), 0.3f);
                case "pickup": return Render(id, 0.3f, (t, r) => lp.Process(r.Next(), 0.2f) * Dsp.Perc(t, 0.005f, 0.05f) + Dsp.Sine(t * 880 * (1 + t)) * Dsp.Perc(t, 0.01f, 0.1f) * 0.4f);
                case "gold": return Render(id, 0.6f, (t, r) => { float s = 0; for (int k = 0; k < 4; k++) { float tt = t - k * 0.06f; if (tt > 0) s += Dsp.Sine(tt * (2600 + k * 340)) * Dsp.Perc(tt, 0.001f, 0.08f); } return s * 0.5f; }, 0.4f);
                case "chest": return Render(id, 0.8f, (t, r) => lp.Process(r.Next(), 0.03f + 0.02f * Mathf.Sin(t * 30)) * Dsp.Env(t, 0.05f, 0.3f, 0.3f, 0.2f, 0.5f) * 3f + Dsp.Saw(t * (90 + 40 * t)) * 0.15f * Dsp.Perc(t, 0.05f, 0.3f));
                case "door": return Render(id, 1.2f, (t, r) => { float creak = Dsp.Saw(t * (180 + 60 * Mathf.Sin(t * 5)) + Dsp.Sine(t * 13) * 0.2f) * Dsp.Env(t, 0.1f, 0.4f, 0.4f, 0.3f, 0.7f); float thud = lp.Process(r.Next(), 0.05f) * Dsp.Perc(t - 0.8f, 0.005f, 0.15f) * (t > 0.8f ? 4f : 0); return svf.Process(creak, 1200, 0.4f, Dsp.SR, out _) * 0.4f + thud; }, 0.25f);
                case "unlock": return Render(id, 0.4f, (t, r) => (hp.Process(r.Next(), 0.95f) * Dsp.Perc(t, 0.001f, 0.02f) + hp.Process(r.Next(), 0.95f) * Dsp.Perc(t - 0.18f, 0.001f, 0.02f) * (t > 0.18f ? 1 : 0)) + Dsp.Sine(t * 2200) * Dsp.Perc(t - 0.18f, 0.001f, 0.05f) * (t > 0.18f ? 0.4f : 0));
                case "locked": return Render(id, 0.3f, (t, r) => lp.Process(r.Next(), 0.15f) * (Dsp.Perc(t, 0.001f, 0.03f) + Dsp.Perc(t - 0.1f, 0.001f, 0.03f) * (t > 0.1f ? 1 : 0)));
                case "discover": return Render(id, 1.2f, (t, r) => { float s = 0; float[] notes = { 74, 78, 81, 86 }; for (int k = 0; k < 4; k++) { float tt = t - k * 0.09f; if (tt > 0) s += Dsp.Sine(tt * Dsp.Midi(notes[k])) * Dsp.Perc(tt, 0.005f, 0.4f); } return s * 0.4f; }, 0.6f);
                case "journal": return Render(id, 1.0f, (t, r) => (Dsp.Sine(t * Dsp.Midi(69)) * 0.5f + Dsp.Sine(t * Dsp.Midi(76)) * 0.3f * (t > 0.12f ? 1 : 0)) * Dsp.Perc(t, 0.01f, 0.4f) + hp.Process(r.Next(), 0.9f) * Dsp.Perc(t, 0.02f, 0.08f) * 0.3f, 0.5f);
                case "quest_done": return Render(id, 2.0f, (t, r) => { float s = 0; float[] ch = { 62, 66, 69, 74 }; foreach (var n in ch) s += Dsp.Tri(t * Dsp.Midi(n)) * Dsp.Env(t, 0.02f, 0.5f, 0.4f, 0.8f, 1.0f); return s * 0.25f; }, 0.6f);
                case "rest": return Render(id, 2.5f, (t, r) => { float s = 0; float[] ch = { 50, 57, 62, 65 }; for (int k = 0; k < 4; k++) s += Dsp.Sine(t * Dsp.Midi(ch[k])) * Dsp.Env(t - k * 0.2f, 0.4f, 0.5f, 0.5f, 1f, 1.2f) * (t > k * 0.2f ? 1 : 0); return s * 0.3f; }, 0.7f);
                case "levelup": case "levelup_ready": return Render(id, 2.4f, (t, r) => { float s = 0; float[] notes = { 62, 69, 74, 78, 81 }; for (int k = 0; k < notes.Length; k++) { float tt = t - k * 0.11f; if (tt > 0) s += (Dsp.Tri(tt * Dsp.Midi(notes[k])) + Dsp.Sine(tt * Dsp.Midi(notes[k] + 12)) * 0.3f) * Dsp.Perc(tt, 0.005f, 0.9f); } return s * 0.3f; }, 0.7f);
                case "combat_start": return Render(id, 2.0f, (t, r) => { float drum = Dsp.Sine(t * (70 - 30 * t)) * Dsp.Perc(t, 0.002f, 0.3f) * 1.4f + lp.Process(r.Next(), 0.1f) * Dsp.Perc(t, 0.002f, 0.1f); float brass = svf.Process(Dsp.Saw(t * Dsp.Midi(38)) + Dsp.Saw(t * Dsp.Midi(45)) * 0.7f, 400 + 900 * Dsp.Perc(t - 0.1f, 0.2f, 0.6f), 0.3f, Dsp.SR, out _) * Dsp.Env(t - 0.1f, 0.1f, 0.4f, 0.5f, 0.6f, 1.0f) * (t > 0.1f ? 0.5f : 0); return drum + brass; }, 0.5f);
                case "your_turn": return Render(id, 0.6f, (t, r) => (Dsp.Sine(t * Dsp.Midi(74)) + Dsp.Sine(t * Dsp.Midi(81)) * 0.5f * (t > 0.08f ? 1 : 0)) * Dsp.Perc(t, 0.005f, 0.2f) * 0.5f, 0.5f);
                case "end_turn": return Render(id, 0.4f, (t, r) => Dsp.Sine(t * Dsp.Midi(62)) * Dsp.Perc(t, 0.005f, 0.12f) * 0.5f, 0.4f);
                case "victory": return Render(id, 3.0f, (t, r) => { float s = 0; float[][] seq = { new float[] { 62, 66, 69 }, new float[] { 64, 67, 71 }, new float[] { 66, 69, 74 } }; for (int k = 0; k < 3; k++) { float tt = t - k * 0.35f; if (tt > 0) foreach (var n in seq[k]) s += (Dsp.Saw(tt * Dsp.Midi(n)) * 0.4f + Dsp.Tri(tt * Dsp.Midi(n))) * Dsp.Env(tt, 0.02f, 0.3f, 0.5f, 0.8f, k == 2 ? 1.2f : 0.3f); } return svf.Process(s, 2400, 0.4f, Dsp.SR, out _) * 0.3f; }, 0.6f);
                case "swing_miss": return Render(id, 0.35f, (t, r) => svf.Process(r.Next(), 600 + 2600 * Dsp.Perc(t, 0.12f, 0.08f), 0.6f, Dsp.SR, out _) * Dsp.Env(t, 0.08f, 0.1f, 0.2f, 0.1f, 0.2f));
                case "spell_miss": return Render(id, 0.5f, (t, r) => svf.Process(r.Next(), 2000 - 1500 * t, 0.3f, Dsp.SR, out _) * Dsp.Perc(t, 0.01f, 0.2f) * 0.6f, 0.4f);
                case "crit": return Render(id, 0.6f, (t, r) => Dsp.Sine(t * (120 - 60 * t)) * Dsp.Perc(t, 0.001f, 0.2f) * 1.5f + hp.Process(r.Next(), 0.9f) * Dsp.Perc(t, 0.001f, 0.05f) + Dsp.Sine(t * 3200) * Dsp.Perc(t, 0.001f, 0.15f) * 0.3f, 0.3f);
                case "slash_hit": return Render(id, 0.35f, (t, r) => svf.Process(r.Next(), 3500 - 3000 * t, 0.5f, Dsp.SR, out _) * Dsp.Perc(t, 0.002f, 0.06f) + lp.Process(r.Next(), 0.1f) * Dsp.Perc(t, 0.001f, 0.08f) * 1.5f);
                case "pierce_hit": return Render(id, 0.3f, (t, r) => lp.Process(r.Next(), 0.2f) * Dsp.Perc(t, 0.001f, 0.05f) * 1.5f + Dsp.Sine(t * 200) * Dsp.Perc(t, 0.001f, 0.06f));
                case "blunt_hit": return Render(id, 0.4f, (t, r) => Dsp.Sine(t * (110 - 60 * t)) * Dsp.Perc(t, 0.001f, 0.12f) * 1.5f + lp.Process(r.Next(), 0.06f) * Dsp.Perc(t, 0.001f, 0.08f) * 2f);
                case "fire_hit": return Render(id, 0.9f, (t, r) => lp.Process(r.Next(), 0.08f + 0.2f * Dsp.Perc(t, 0.01f, 0.1f)) * Dsp.Env(t, 0.01f, 0.2f, 0.4f, 0.4f, 0.4f) * 2.2f + lp2.Process(r.Next(), 0.02f) * Dsp.Perc(t, 0.001f, 0.2f) * 2f, 0.3f);
                case "frost_hit": return Render(id, 0.8f, (t, r) => { float s = hp.Process(r.Next(), 0.97f) * Dsp.Perc(t, 0.002f, 0.08f); for (int k = 0; k < 5; k++) s += Dsp.Sine(t * (2400 + k * 700)) * Dsp.Perc(t - k * 0.03f, 0.001f, 0.2f) * (t > k * 0.03f ? 0.2f : 0); return s; }, 0.5f);
                case "lightning_hit": return Render(id, 0.9f, (t, r) => { float crackle = hp.Process(r.Next() * (r.Next01() > 0.7f ? 1 : 0.2f), 0.9f) * Dsp.Perc(t, 0.001f, 0.15f); float boom = lp.Process(r.Next(), 0.03f) * Dsp.Perc(t, 0.01f, 0.4f) * 3f; return crackle * 1.5f + boom; }, 0.3f);
                case "radiant_hit": return Render(id, 1.2f, (t, r) => { float s = 0; float[] ch = { 74, 78, 81, 86 }; foreach (var n in ch) s += Dsp.Sine(t * Dsp.Midi(n)) * Dsp.Perc(t, 0.02f, 0.5f); return s * 0.3f + hp.Process(r.Next(), 0.97f) * Dsp.Perc(t, 0.01f, 0.2f) * 0.3f; }, 0.7f);
                case "necrotic_hit": return Render(id, 0.9f, (t, r) => svf.Process(Dsp.Saw(t * (80 + 20 * Mathf.Sin(t * 30))) + r.Next() * 0.3f, 500, 0.8f, Dsp.SR, out _) * Dsp.Env(t, 0.02f, 0.3f, 0.3f, 0.3f, 0.5f) * 0.8f, 0.4f);
                case "force_hit": return Render(id, 0.6f, (t, r) => Dsp.Sine(t * (600 - 400 * t) + Dsp.Sine(t * 80) * 3) * Dsp.Perc(t, 0.002f, 0.2f) * 0.7f + lp.Process(r.Next(), 0.1f) * Dsp.Perc(t, 0.001f, 0.05f), 0.4f);
                case "thunder_hit": return Render(id, 1.5f, (t, r) => lp.Process(r.Next(), 0.015f + 0.03f * Dsp.Perc(t, 0.01f, 0.3f)) * Dsp.Env(t, 0.005f, 0.3f, 0.5f, 0.8f, 0.6f) * 6f, 0.4f);
                case "downed": return Render(id, 1.2f, (t, r) => (Dsp.Tri(t * Dsp.Midi(50 - t * 4)) * 0.6f + Dsp.Sine(t * Dsp.Midi(38)) * 0.6f) * Dsp.Env(t, 0.02f, 0.4f, 0.3f, 0.5f, 0.6f), 0.5f);
                case "death": return Render(id, 0.8f, (t, r) => lp.Process(r.Next(), 0.05f) * Dsp.Perc(t, 0.005f, 0.2f) * 2f + Dsp.Sine(t * (90 - 40 * t)) * Dsp.Perc(t, 0.005f, 0.3f), 0.3f);
                case "death_pc": return Render(id, 2.5f, (t, r) => { float s = 0; float[] ch = { 50, 53, 57 }; foreach (var n in ch) s += Dsp.Saw(t * Dsp.Midi(n - t * 1.5f)) * 0.3f; return svf.Process(s, 800, 0.4f, Dsp.SR, out _) * Dsp.Env(t, 0.1f, 0.8f, 0.4f, 1.0f, 1.3f); }, 0.6f);
                case "dash": return Render(id, 0.4f, (t, r) => svf.Process(r.Next(), 400 + 3000 * t, 0.4f, Dsp.SR, out _) * Dsp.Env(t, 0.05f, 0.15f, 0.3f, 0.1f, 0.25f));
                case "teleport": return Render(id, 0.8f, (t, r) => Dsp.Sine(t * (300 + 1400 * t) + Dsp.Sine(t * 40) * 2) * Dsp.Env(t, 0.05f, 0.3f, 0.4f, 0.3f, 0.5f) * 0.5f + svf.Process(r.Next(), 3000, 0.3f, Dsp.SR, out _) * Dsp.Perc(t, 0.05f, 0.2f) * 0.3f, 0.6f);
                case "buff": case "cast": return Render(id, 0.9f, (t, r) => { float s = 0; for (int k = 0; k < 6; k++) s += Dsp.Sine(t * Dsp.Midi(69 + k * 4) * (1 + 0.003f * Mathf.Sin(t * 20 + k))) * Dsp.Env(t - k * 0.04f, 0.05f, 0.3f, 0.3f, 0.3f, 0.4f) * (t > k * 0.04f ? 1 : 0); return s * 0.2f; }, 0.6f);
                case "wildshape": return Render(id, 1.4f, (t, r) => svf.Process(r.Next() + Dsp.Saw(t * (60 + 80 * t)) * 0.5f, 300 + 1200 * Dsp.Perc(t, 0.3f, 0.5f), 0.7f, Dsp.SR, out _) * Dsp.Env(t, 0.2f, 0.4f, 0.5f, 0.4f, 0.8f), 0.4f);
                case "revive": return Render(id, 2.0f, (t, r) => { float s = 0; float[] ch = { 62, 69, 74, 81 }; for (int k = 0; k < 4; k++) s += Dsp.Sine(t * Dsp.Midi(ch[k])) * Dsp.Env(t - k * 0.15f, 0.2f, 0.5f, 0.5f, 0.6f, 0.9f) * (t > k * 0.15f ? 1 : 0); return s * 0.3f; }, 0.8f);
                case "dice_roll": return Render(id, 1.0f, (t, r) => { float s = 0; for (int k = 0; k < 9; k++) { float tt = t - k * 0.1f * (1 - k * 0.04f); if (tt > 0) s += hp.Process(r.Next(), 0.85f) * Dsp.Perc(tt, 0.001f, 0.012f) * (1 - k * 0.07f); } return s; });
                case "dice_land": return Render(id, 0.4f, (t, r) => lp.Process(r.Next(), 0.3f) * Dsp.Perc(t, 0.001f, 0.03f) + Dsp.Sine(t * 900) * Dsp.Perc(t, 0.001f, 0.05f) * 0.3f);
                case "success": return Render(id, 1.6f, (t, r) => { float s = 0; float[] n = { 69, 73, 76, 81 }; for (int k = 0; k < 4; k++) { float tt = t - k * 0.07f; if (tt > 0) s += Dsp.Tri(tt * Dsp.Midi(n[k])) * Dsp.Perc(tt, 0.005f, 0.6f); } return s * 0.3f; }, 0.6f);
                case "failure": return Render(id, 1.4f, (t, r) => { float s = 0; float[] n = { 62, 61, 57 }; for (int k = 0; k < 3; k++) { float tt = t - k * 0.18f; if (tt > 0) s += Dsp.Saw(tt * Dsp.Midi(n[k])) * Dsp.Perc(tt, 0.01f, 0.35f); } return svf.Process(s, 900, 0.3f, Dsp.SR, out _) * 0.4f; }, 0.5f);
                case "footstep": return Render(id, 0.12f, (t, r) => lp.Process(r.Next(), 0.1f) * Dsp.Perc(t, 0.002f, 0.03f));
                case "howl": return Render(id, 3.5f, (t, r) => { float f = 420 + 180 * Mathf.Sin(Mathf.Min(t, 2.5f) / 2.5f * Mathf.PI) - 120 * Mathf.Max(0, t - 2.5f); float v = Dsp.Sine(t * f + Dsp.Sine(t * 5) * 1.5f) + Dsp.Sine(t * f * 2) * 0.2f; return svf.Process(v + r.Next() * 0.05f, 1400, 0.2f, Dsp.SR, out _) * Dsp.Env(t, 0.6f, 1f, 0.7f, 0.9f, 2.5f) * 0.6f; }, 0.9f);
                case "crow": return Render(id, 0.8f, (t, r) => { float s = 0; for (int k = 0; k < 2; k++) { float tt = t - k * 0.32f; if (tt > 0 && tt < 0.25f) s += svf.Process(Dsp.Saw(tt * (700 - 300 * tt)) + r.Next() * 0.8f, 1600, 0.6f, Dsp.SR, out _) * Dsp.Env(tt, 0.02f, 0.1f, 0.4f, 0.05f, 0.18f); } return s; }, 0.6f);
                case "bell_toll": return Render(id, 5f, (t, r) => { float s = 0; float[] partials = { 1, 2.0f, 2.4f, 3.0f, 4.2f, 5.4f }; for (int k = 0; k < partials.Length; k++) s += Dsp.Sine(t * 110 * partials[k]) * Mathf.Exp(-t * (0.6f + k * 0.5f)) / (k + 1); return s; }, 0.7f);
                case "heartbeat": return Render(id, 1.0f, (t, r) => Dsp.Sine(t * 55) * (Dsp.Perc(t, 0.01f, 0.08f) + Dsp.Perc(t - 0.25f, 0.01f, 0.1f) * (t > 0.25f ? 0.8f : 0)) * 1.5f);
                case "whisper": return Render(id, 2.5f, (t, r) => svf.Process(r.Next(), 1800 + 1200 * Mathf.Sin(t * 9) * Mathf.Sin(t * 3.1f), 0.2f, Dsp.SR, out _) * Dsp.Env(t, 0.4f, 0.5f, 0.6f, 0.6f, 1.8f) * (0.5f + 0.5f * Mathf.Sin(t * 13)), 0.8f);
                case "child_laugh": return Render(id, 1.6f, (t, r) => { float s = 0; for (int k = 0; k < 5; k++) { float tt = t - k * 0.16f; if (tt > 0 && tt < 0.13f) s += Dsp.Tri(tt * (900 + 200 * Mathf.Sin(tt * 30)) + Dsp.Sine(tt * 30)) * Dsp.Env(tt, 0.02f, 0.05f, 0.5f, 0.05f, 0.1f); } return s * 0.5f; }, 0.9f);
                default: return Render(id, 0.2f, (t, r) => Dsp.Sine(t * 800) * Dsp.Perc(t, 0.002f, 0.05f) * 0.3f);
            }
        }
    }

    /// <summary>Music, ambience and one-shot playback with crossfading.</summary>
    public class AudioSys : MonoBehaviour
    {
        public static AudioSys I;
        AudioSource musicA, musicB, amb, ambB;
        readonly List<AudioSource> pool = new List<AudioSource>();
        int poolIdx;
        string currentMusic, currentAmb;
        readonly Dictionary<string, AudioClip> tracks = new Dictionary<string, AudioClip>();
        readonly HashSet<string> generating = new HashSet<string>();
        float ambEventT = 5f;

        void Awake()
        {
            I = this;
            musicA = Src(true); musicB = Src(true); amb = Src(true); ambB = Src(true);
            for (int i = 0; i < 12; i++) pool.Add(Src(false));
            ApplyVolumes();
        }

        AudioSource Src(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = loop; s.playOnAwake = false; s.spatialBlend = 0;
            return s;
        }

        public void ApplyVolumes()
        {
            AudioListener.volume = Settings.Master;
            foreach (var p in pool) p.volume = Settings.Effects;
        }

        public void PlayOneShot(AudioClip c, float vol, float pitch)
        {
            var s = pool[poolIdx]; poolIdx = (poolIdx + 1) % pool.Count;
            s.pitch = pitch; s.volume = Settings.Effects * vol;
            s.clip = c; s.Play();
        }

        public void PlayMusic(string id)
        {
            if (id == currentMusic) return;
            currentMusic = id;
            StartCoroutine(SwitchMusic(id));
        }

        public void PlayAmbience(string id)
        {
            if (id == currentAmb) return;
            currentAmb = id;
            StartCoroutine(SwitchAmb(id));
        }

        IEnumerator Obtain(string key, Func<float[]> render, int sr, Action<AudioClip> done)
        {
            if (tracks.TryGetValue(key, out var c)) { done(c); yield break; }
            if (generating.Contains(key)) { while (!tracks.ContainsKey(key)) yield return null; done(tracks[key]); yield break; }
            generating.Add(key);
            float[] data = null;
            var task = Task.Run(() => { try { data = render(); } catch (Exception e) { Debug.LogException(e); data = new float[sr]; } });
            while (!task.IsCompleted) yield return null;
            int ch = 2;
            var clip = AudioClip.Create(key, data.Length / ch, ch, sr, false);
            clip.SetData(data, 0);
            tracks[key] = clip;
            generating.Remove(key);
            done(clip);
        }

        IEnumerator SwitchMusic(string id)
        {
            AudioClip clip = null;
            yield return Obtain("music_" + id, () => Music.Render(id), Dsp.MusicSR, c => clip = c);
            if (currentMusic != id) yield break;
            var from = musicA.isPlaying ? musicA : musicB; var to = from == musicA ? musicB : musicA;
            to.clip = clip; to.volume = 0; to.Play();
            float t = 0;
            while (t < 2f) { t += Time.unscaledDeltaTime; to.volume = Settings.Music * (t / 2f); from.volume = Settings.Music * (1 - t / 2f); yield return null; }
            from.Stop();
        }

        IEnumerator SwitchAmb(string id)
        {
            AudioClip clip = null;
            yield return Obtain("amb_" + id, () => Music.RenderAmbience(id), Dsp.MusicSR, c => clip = c);
            if (currentAmb != id) yield break;
            var from = amb.isPlaying ? amb : ambB; var to = from == amb ? ambB : amb;
            to.clip = clip; to.volume = 0; to.Play();
            float t = 0;
            while (t < 2.5f) { t += Time.unscaledDeltaTime; to.volume = Settings.Ambience * 0.8f * (t / 2.5f); from.volume = Settings.Ambience * 0.8f * (1 - t / 2.5f); yield return null; }
            from.Stop();
        }

        void Update()
        {
            var active = musicA.isPlaying && musicA.volume > 0.01f ? musicA : musicB;
            if (active.isPlaying) active.volume = Mathf.MoveTowards(active.volume, Settings.Music, Time.unscaledDeltaTime);
            // occasional ambient events outdoors
            if (currentAmb == "wind" || currentAmb == "night")
            {
                ambEventT -= Time.deltaTime;
                if (ambEventT <= 0)
                {
                    ambEventT = UnityEngine.Random.Range(14f, 32f);
                    Sfx.Play(UnityEngine.Random.value < 0.45f ? "howl" : "crow", 0.25f * Settings.Ambience, UnityEngine.Random.Range(0.85f, 1.1f));
                }
            }
        }
    }

    /// <summary>Procedural composition: dark D-minor pieces for each mood.</summary>
    public static class Music
    {
        public static float[] Render(string id)
        {
            switch (id)
            {
                case "combat": return Combat();
                case "menu": return Organ(62f, 0.9f, true);
                case "strahd": return Organ(56f, 1.2f, false);
                case "creation": return Harp();
                case "defeat": return Pad(24f, new[] { new[] { 50, 53, 57 }, new[] { 46, 50, 53 } }, 0.5f, 1234);
                case "interior": return Pad(64f, new[] { new[] { 38, 45, 50 }, new[] { 36, 43, 48 }, new[] { 34, 41, 50 }, new[] { 33, 40, 49 } }, 0.35f, 99, true);
                case "tavern": return Harp(true);
                default: return Pad(72f, new[] { new[] { 50, 57, 62, 65 }, new[] { 46, 53, 58, 62 }, new[] { 43, 50, 55, 58 }, new[] { 45, 52, 57, 61 } }, 0.55f, 7, true);
            }
        }

        static float[] Stereo(int n) => new float[n * 2];

        static void Normalize(float[] d, float target = 0.8f)
        {
            float peak = 0.0001f; foreach (var x in d) peak = Mathf.Max(peak, Mathf.Abs(x));
            float g = target / peak; for (int i = 0; i < d.Length; i++) d[i] *= g;
            // fade loop seam
            int f = 2000;
            for (int i = 0; i < f && i * 2 + 1 < d.Length; i++) { float k = i / (float)f; d[i * 2] *= k; d[i * 2 + 1] *= k; int j = d.Length / 2 - 1 - i; d[j * 2] *= k; d[j * 2 + 1] *= k; }
        }

        /// <summary>Slow evolving string/choir pad with sparse melody.</summary>
        static float[] Pad(float dur, int[][] chords, float bright, uint seed, bool melody = false)
        {
            int sr = Dsp.MusicSR; int n = (int)(dur * sr);
            var d = Stereo(n);
            var rv = new Dsp.Reverb(sr, 1.3f, 0.5f); var rv2 = new Dsp.Reverb(sr, 1.37f, 0.5f);
            var rnd = new Dsp.Rand(seed);
            float chordLen = dur / chords.Length;
            var lpL = new Dsp.LP(); var lpR = new Dsp.LP();
            var ph = new float[16];
            int[] scale = { 0, 2, 3, 5, 7, 8, 10 };
            var melNotes = new List<(float t, int n, float len)>();
            if (melody) { float t = 3f; int deg = 4; while (t < dur - 4) { deg = Mathf.Clamp(deg + (int)(rnd.Next() * 2.5f), 0, 9); int note = 62 + scale[deg % 7] + 12 * (deg / 7); float len = 1.2f + rnd.Next01() * 2f; melNotes.Add((t, note, len)); t += len + rnd.Next01() * 3f; } }
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                int ci = Mathf.Min(chords.Length - 1, (int)(t / chordLen));
                float ct = t - ci * chordLen;
                float xf = Mathf.Clamp01(ct / 2f);
                int prev = (ci + chords.Length - 1) % chords.Length;
                float s = 0;
                for (int v = 0; v < chords[ci].Length; v++)
                {
                    float f = Dsp.Midi(chords[ci][v]);
                    float fp = Dsp.Midi(chords[prev][Mathf.Min(v, chords[prev].Length - 1)]);
                    float freq = Mathf.Lerp(fp, f, MathX.Smoothstep(0, 1, xf));
                    ph[v] += freq / sr * (1 + 0.002f * Mathf.Sin(t * (0.3f + v * 0.13f)));
                    ph[v + 8] += freq * 1.004f / sr;
                    s += (Dsp.Saw(ph[v]) + Dsp.Saw(ph[v + 8])) * 0.5f * (v == 0 ? 1.3f : 0.8f);
                }
                float swell = 0.6f + 0.4f * Mathf.Sin(t * 0.21f) * Mathf.Sin(t * 0.13f + 1);
                float cut = 0.03f + 0.05f * bright * swell;
                float l = lpL.Process(s, cut), r = lpR.Process(s, cut * 0.97f);
                // melody (bell-like harp)
                float mel = 0;
                foreach (var m in melNotes) { float tt = t - m.t; if (tt >= 0 && tt < m.len + 2f) mel += (Dsp.Sine(tt * Dsp.Midi(m.n)) + Dsp.Sine(tt * Dsp.Midi(m.n) * 2.01f) * 0.25f) * Dsp.Perc(tt, 0.005f, 1.2f) * 0.35f; }
                l += mel; r += mel;
                // low drone
                float drone = Dsp.Sine(t * Dsp.Midi(26)) * 0.35f + Dsp.Sine(t * Dsp.Midi(38)) * 0.12f;
                l += drone; r += drone;
                float wl = rv.Process(l * 0.5f), wr = rv2.Process(r * 0.5f);
                d[i * 2] = l * 0.55f + wl * 0.9f; d[i * 2 + 1] = r * 0.55f + wr * 0.9f;
            }
            Normalize(d, 0.6f);
            return d;
        }

        /// <summary>Gothic organ with pedal and a slow, original theme.</summary>
        static float[] Organ(float dur, float tempo, bool gentle)
        {
            int sr = Dsp.MusicSR; int n = (int)(dur * sr);
            var d = Stereo(n);
            var rv = new Dsp.Reverb(sr, 1.6f, 0.4f); var rv2 = new Dsp.Reverb(sr, 1.67f, 0.4f);
            // theme in D minor (original): scale degrees with durations in beats
            int[] mel = { 62, 65, 64, 62, 61, 62, 69, 70, 69, 67, 65, 64, 65, 62, 58, 57, 62, 65, 69, 74, 72, 70, 69, 67, 65, 64, 62, 61, 62 };
            float[] len = { 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 2, 2, 2, 2, 4, 2, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 4 };
            int[] bass = { 38, 38, 46, 46, 43, 43, 45, 45, 38, 38, 34, 34, 43, 45, 38, 38 };
            float beat = 0.62f * tempo;
            var notes = new List<(float t, int n, float l)>(); float tt0 = 1f;
            for (int rep = 0; rep < 3 && tt0 < dur - 6; rep++) for (int k = 0; k < mel.Length && tt0 < dur - 3; k++) { notes.Add((tt0, mel[k] + (gentle ? 0 : -12), len[k] * beat)); tt0 += len[k] * beat; }
            var ph = new float[8];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float s = 0;
                foreach (var no in notes)
                {
                    float x = t - no.t; if (x < 0 || x > no.l + 0.6f) continue;
                    float f = Dsp.Midi(no.n);
                    float env = Dsp.Env(x, 0.04f, 0.1f, 0.85f, 0.4f, no.l);
                    s += (Dsp.Sine(x * f) + Dsp.Sine(x * f * 2) * 0.5f + Dsp.Sine(x * f * 3) * 0.25f + Dsp.Sine(x * f * 4) * 0.15f + Dsp.Sine(x * f * 0.5f) * 0.3f) * env * 0.35f;
                }
                int bi = Mathf.Clamp((int)(t / (beat * 4)) % bass.Length, 0, bass.Length - 1);
                float bf = Dsp.Midi(bass[bi] - 12);
                ph[0] += bf / sr; ph[1] += bf * 1.5f / sr; ph[2] += bf * 2 / sr;
                s += (Dsp.Sine(ph[0]) * 0.5f + Dsp.Sine(ph[1]) * 0.18f + Dsp.Sine(ph[2]) * 0.2f) * 0.6f;
                float wl = rv.Process(s * 0.5f), wr = rv2.Process(s * 0.5f);
                d[i * 2] = s * 0.5f + wl; d[i * 2 + 1] = s * 0.5f + wr;
            }
            Normalize(d, 0.6f);
            return d;
        }

        static float[] Harp(bool tavern = false)
        {
            int sr = Dsp.MusicSR; float dur = tavern ? 48f : 56f; int n = (int)(dur * sr);
            var d = Stereo(n);
            var rv = new Dsp.Reverb(sr, 1.1f, 0.4f); var rv2 = new Dsp.Reverb(sr, 1.18f, 0.4f);
            int[][] prog = tavern ? new[] { new[] { 50, 57, 62, 65 }, new[] { 48, 55, 60, 64 }, new[] { 46, 53, 58, 62 }, new[] { 45, 52, 57, 61 } } : new[] { new[] { 50, 57, 62, 65, 69 }, new[] { 46, 53, 58, 62, 65 }, new[] { 41, 48, 53, 57, 60 }, new[] { 45, 52, 57, 61, 64 } };
            float step = tavern ? 0.2f : 0.28f;
            var ev = new List<(float t, int n, float pan)>();
            var rnd = new Dsp.Rand(42);
            float t0 = 0.5f; int k = 0;
            while (t0 < dur - 2)
            {
                var ch = prog[(int)(t0 / (step * 16)) % prog.Length];
                int idx = tavern ? new[] { 0, 2, 1, 3, 2, 1 }[k % 6] : new[] { 0, 1, 2, 3, 4, 3, 2, 1 }[k % 8];
                ev.Add((t0, ch[Mathf.Min(idx, ch.Length - 1)] + (tavern && k % 12 == 11 ? 12 : 0), rnd.Next() * 0.5f));
                t0 += step * (tavern && k % 3 == 2 ? 2 : 1); k++;
            }
            int lo = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float l = 0, r = 0;
                while (lo < ev.Count && ev[lo].t < t - 3f) lo++;
                for (int e = lo; e < ev.Count && ev[e].t <= t; e++)
                {
                    float x = t - ev[e].t; if (x > 3f) continue;
                    float f = Dsp.Midi(ev[e].n);
                    float v = (Dsp.Tri(x * f) * 0.6f + Dsp.Sine(x * f * 2) * 0.3f + Dsp.Sine(x * f * 3) * 0.1f) * Dsp.Perc(x, 0.003f, tavern ? 0.6f : 1.1f) * 0.3f;
                    l += v * (1 - ev[e].pan); r += v * (1 + ev[e].pan);
                }
                if (tavern) { float drum = Dsp.Sine(t * 70) * Dsp.Perc((t % (step * 6)), 0.002f, 0.08f) * 0.3f; l += drum; r += drum; }
                float wl = rv.Process(l * 0.5f), wr = rv2.Process(r * 0.5f);
                d[i * 2] = l + wl * 0.7f; d[i * 2 + 1] = r + wr * 0.7f;
            }
            Normalize(d, 0.55f);
            return d;
        }

        /// <summary>Driving combat piece: war drums, low strings ostinato, brass stabs.</summary>
        static float[] Combat()
        {
            int sr = Dsp.MusicSR; float bpm = 132; float beat = 60f / bpm; float dur = beat * 4 * 16; int n = (int)(dur * sr);
            var d = Stereo(n);
            var rv = new Dsp.Reverb(sr, 1.0f, 0.4f); var rv2 = new Dsp.Reverb(sr, 1.05f, 0.4f);
            var rnd = new Dsp.Rand(77);
            var svf = new Dsp.SVF(); var svf2 = new Dsp.SVF();
            int[] roots = { 38, 38, 34, 36, 38, 38, 33, 33, 38, 41, 34, 36, 38, 38, 33, 33 };
            var ph = new float[6];
            var noiseLP = new Dsp.LP();
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float b = t / beat; int bar = (int)(b / 4) % 16; float inBeat = b - Mathf.Floor(b); float eighth = (b * 2) - Mathf.Floor(b * 2);
                int root = roots[bar];
                // strings ostinato: 8th notes root / fifth / octave pattern
                int[] pat = { 0, 0, 7, 0, 12, 0, 7, 10 };
                int step = (int)(b * 2) % 8;
                float f = Dsp.Midi(root + 12 + pat[step]);
                ph[0] += f / sr; ph[1] += f * 1.005f / sr;
                float str = (Dsp.Saw(ph[0]) + Dsp.Saw(ph[1])) * 0.5f * Dsp.Perc(eighth * beat * 0.5f, 0.005f, 0.09f);
                str = svf.Process(str, 1400 + 600 * Mathf.Sin(t * 0.5f), 0.3f, sr, out _) * 0.55f;
                // bass
                ph[2] += Dsp.Midi(root - 12) / sr;
                float bass = Dsp.Saw(ph[2]) * 0.4f;
                bass = svf2.Process(bass, 300, 0.2f, sr, out _);
                // drums: taiko on 1 and 3, toms syncopated
                float kick = 0;
                int beatIdx = (int)b % 4;
                float bt = inBeat * beat;
                if (beatIdx == 0 || beatIdx == 2) kick += Dsp.Sine(bt * (65 - 30 * bt)) * Dsp.Perc(bt, 0.002f, 0.25f) * 1.3f;
                if ((int)(b * 2) % 8 == 7) { float et = eighth * beat * 0.5f; kick += Dsp.Sine(et * (110 - 40 * et)) * Dsp.Perc(et, 0.002f, 0.12f) * 0.8f; }
                float snare = 0;
                if (beatIdx == 1 || beatIdx == 3) snare = noiseLP.Process(rnd.Next(), 0.35f) * Dsp.Perc(bt, 0.002f, 0.07f) * 0.5f;
                // brass stab each 2 bars
                float brass = 0;
                if (bar % 2 == 0 && b % 8 < 1.5f)
                {
                    float x = (b % 8) * beat;
                    foreach (var iv in new[] { 0, 3, 7 }) brass += Dsp.Saw(x * Dsp.Midi(root + 12 + iv)) * 0.3f;
                    brass *= Dsp.Env(x, 0.02f, 0.2f, 0.4f, 0.3f, beat * 1.2f);
                }
                float s = str + bass + kick + snare + brass * 0.6f;
                float wl = rv.Process(s * 0.3f), wr = rv2.Process(s * 0.3f);
                d[i * 2] = Dsp.SoftClip(s * 0.8f + wl * 0.6f); d[i * 2 + 1] = Dsp.SoftClip(s * 0.8f + wr * 0.6f);
            }
            Normalize(d, 0.65f);
            return d;
        }

        public static float[] RenderAmbience(string id)
        {
            int sr = Dsp.MusicSR; float dur = 30f; int n = (int)(dur * sr);
            var d = Stereo(n);
            var rnd = new Dsp.Rand((uint)id.GetHashCode());
            var sL = new Dsp.SVF(); var sR = new Dsp.SVF(); var lp = new Dsp.LP(); var lp2 = new Dsp.LP();
            var rv = new Dsp.Reverb(sr, 1.2f, 0.5f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float l = 0, r = 0;
                switch (id)
                {
                    case "interior":
                        {
                            float rumble = lp.Process(rnd.Next(), 0.004f) * 3f;
                            float drip = 0; float period = 3.7f; float x = t % period; if (x < 0.2f) drip = Dsp.Sine(x * (1400 - 2000 * x)) * Dsp.Perc(x, 0.001f, 0.03f) * 0.4f;
                            float creak = 0; float cp = 11.3f; float cx = t % cp; if (cx < 0.8f) creak = sL.Process(Dsp.Saw(cx * (140 + 40 * Mathf.Sin(cx * 7))), 700, 0.5f, sr, out _) * Dsp.Env(cx, 0.1f, 0.3f, 0.4f, 0.2f, 0.5f) * 0.15f;
                            l = rumble + drip + creak; r = rumble + rv.Process(drip) * 0.8f + creak * 0.7f;
                            break;
                        }
                    case "tavern":
                        {
                            float fire = lp.Process(rnd.Next(), 0.1f) * (rnd.Next01() > 0.997f ? 3f : 0.3f);
                            float murmur = sL.Process(rnd.Next(), 380 + 120 * Mathf.Sin(t * 2.3f), 0.8f, sr, out _) * (0.4f + 0.3f * Mathf.Sin(t * 0.7f)) * 0.5f;
                            l = fire + murmur; r = fire * 0.6f + murmur;
                            break;
                        }
                    default:
                        {
                            float gust = 0.5f + 0.35f * Mathf.Sin(t * 0.23f) + 0.2f * Mathf.Sin(t * 0.61f + 1) + 0.1f * Mathf.Sin(t * 1.7f);
                            float cut = 300 + 500 * gust;
                            l = sL.Process(rnd.Next(), cut, 0.6f, sr, out _) * gust * 0.7f;
                            r = sR.Process(rnd.Next(), cut * 1.1f, 0.6f, sr, out _) * gust * 0.7f;
                            float low = lp.Process(rnd.Next(), 0.003f) * 2f;
                            l += low; r += low;
                            if (id == "night") { float c = Dsp.Sine(t * 3800) * (Mathf.Sin(t * 40) > 0.95f ? 0.03f : 0); l += c; }
                            break;
                        }
                }
                d[i * 2] = l; d[i * 2 + 1] = r;
            }
            Normalize(d, 0.5f);
            return d;
        }
    }
}
