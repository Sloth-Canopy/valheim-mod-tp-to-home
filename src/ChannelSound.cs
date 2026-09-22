using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Homeward
{
    /// <summary>
    /// Gentle looping sound while channeling: a low, slowly swelling drone with
    /// sparse bell-like chimes. Synthesized once at runtime into an AudioClip —
    /// no audio asset, no decoder. Routed through a vanilla SFX mixer group so the
    /// game's SFX volume slider applies.
    /// </summary>
    internal static class ChannelSound
    {
        private const int SampleRate = 44100;
        private const float LoopSeconds = 8f;

        private static AudioClip _clip;
        private static AudioMixerGroup _mixer;
        private static bool _mixerSearched;

        public static AudioSource Attach(GameObject root, float volume)
        {
            AudioSource src = root.AddComponent<AudioSource>();
            src.clip = Clip();
            src.outputAudioMixerGroup = Mixer();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 1f;      // 3D: it's coming from the ring
            src.minDistance = 2f;
            src.maxDistance = 25f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.dopplerLevel = 0f;
            src.volume = volume;
            src.Play();
            return src;
        }

        private static AudioClip Clip()
        {
            if (_clip != null)
            {
                return _clip;
            }
            int n = (int)(SampleRate * LoopSeconds);
            float[] data = new float[n];

            // --- Drone: a root, a fifth and an octave, each slightly detuned, with a slow swell.
            float root = 110f; // A2
            float[] partials = { root, root * 1.005f, root * 1.5f, root * 1.498f, root * 2f };
            float[] gains    = { 0.18f, 0.14f, 0.10f, 0.09f, 0.06f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float swell = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * t / LoopSeconds); // one full breath per loop
                float s = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    s += gains[p] * Mathf.Sin(2f * Mathf.PI * partials[p] * t);
                }
                data[i] = s * swell;
            }

            // --- Chimes: fixed times so the loop is seamless and deterministic.
            // A minor pentatonic, two octaves up from the drone.
            float[] scale = { 440f, 523.25f, 587.33f, 659.25f, 783.99f, 880f };
            (float time, int note, float gain)[] chimes =
            {
                (0.9f, 0, 0.16f), (2.6f, 2, 0.13f), (3.9f, 4, 0.15f),
                (5.4f, 1, 0.12f), (6.7f, 3, 0.14f),
            };
            foreach ((float time, int note, float gain) in chimes)
            {
                float f = scale[note];
                int start = (int)(time * SampleRate);
                int len = (int)(2.2f * SampleRate);
                for (int k = 0; k < len; k++)
                {
                    int i = (start + k) % n; // wrap so a tail near the end continues at the loop start
                    float tt = k / (float)SampleRate;
                    float env = Mathf.Exp(-tt * 2.2f) * Mathf.Min(1f, tt * 60f); // fast attack, long decay
                    float tone = Mathf.Sin(2f * Mathf.PI * f * tt)
                               + 0.35f * Mathf.Sin(2f * Mathf.PI * f * 2f * tt) * Mathf.Exp(-tt * 3.5f)
                               + 0.15f * Mathf.Sin(2f * Mathf.PI * f * 3.01f * tt) * Mathf.Exp(-tt * 5f);
                    data[i] += gain * env * tone;
                }
            }

            // --- Normalize to a soft peak.
            float peak = 0f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak > 0f)
            {
                float norm = 0.45f / peak;
                for (int i = 0; i < n; i++) data[i] *= norm;
            }

            _clip = AudioClip.Create("Homeward_Channel", n, 1, SampleRate, false);
            _clip.SetData(data, 0);
            return _clip;
        }

        /// <summary>
        /// Vanilla sound prefabs carry their mixer group on the AudioSource; borrow the
        /// first one we can find so the SFX volume slider applies to us too.
        /// </summary>
        private static AudioMixerGroup Mixer()
        {
            if (_mixerSearched)
            {
                return _mixer;
            }
            _mixerSearched = true;
            if (ObjectDB.instance != null)
            {
                foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
                {
                    foreach (EffectList.EffectData fx in se.m_startEffects.m_effectPrefabs)
                    {
                        AudioSource a = fx.m_prefab != null ? fx.m_prefab.GetComponentInChildren<AudioSource>(true) : null;
                        if (a != null && a.outputAudioMixerGroup != null)
                        {
                            _mixer = a.outputAudioMixerGroup;
                            HomewardPlugin.Log.LogInfo($"Channel sound mixer: {_mixer.name}");
                            return _mixer;
                        }
                    }
                }
            }
            HomewardPlugin.Log.LogWarning("Channel sound: no vanilla mixer group found; SFX volume slider won't apply");
            return null;
        }
    }
}
