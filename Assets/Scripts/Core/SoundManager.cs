using System.Collections.Generic;
using UnityEngine;

namespace HazeBuster.Core
{
    /// <summary>
    /// Sound manager capable of playing AudioClips, but also features a built-in
    /// procedural 8-bit retro sound synthesizer! That way, sound effects work
    /// immediately even without importing external .wav/.mp3 audio files.
    /// Procedural clips are generated once, cached, and reused, so long runs do not leak memory.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource audioSource;

        // Generated clips by name. Built the first time a sound is needed, then reused.
        private readonly Dictionary<string, AudioClip> proceduralClips = new Dictionary<string, AudioClip>();

        [Header("Audio Clips (Optional - will use procedural sounds if unassigned)")]
        public AudioClip shootClip;
        public AudioClip dashClip;
        public AudioClip hitClip;
        public AudioClip dewCollectClip;
        public AudioClip levelUpClip;
        public AudioClip explosionClip;

        private void Awake()
        {
            // Scene-scoped singleton ("latest wins"): a Retry reloads the scene and builds a fresh
            // SoundManager. Only a duplicate in the SAME scene is removed.
            if (Instance != null && Instance != this && Instance.gameObject.scene == gameObject.scene)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, AudioClip> pair in proceduralClips)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value);
                }
            }
            proceduralClips.Clear();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayShootSound()
        {
            if (shootClip != null)
                audioSource.PlayOneShot(shootClip, 0.4f);
            else
                PlayProceduralTone("shoot", 440f, 660f, 0.08f, 0.25f); // Quick chirp for water squirt
        }

        public void PlayDashSound()
        {
            if (dashClip != null)
                audioSource.PlayOneShot(dashClip, 0.6f);
            else
                PlayProceduralTone("dash", 220f, 110f, 0.15f, 0.35f); // Whoosh drop
        }

        public void PlayHitSound()
        {
            if (hitClip != null)
                audioSource.PlayOneShot(hitClip, 0.5f);
            else
                PlayProceduralTone("hit", 180f, 80f, 0.12f, 0.4f); // Low thud
        }

        public void PlayDewCollectSound()
        {
            if (dewCollectClip != null)
                audioSource.PlayOneShot(dewCollectClip, 0.5f);
            else
                PlayProceduralTone("dew", 587f, 880f, 0.1f, 0.3f); // Happy chime
        }

        public void PlayLevelUpSound()
        {
            if (levelUpClip != null)
                audioSource.PlayOneShot(levelUpClip, 0.7f);
            else
                PlayProceduralTone("levelup", 440f, 880f, 0.35f, 0.4f); // Rising fanfare
        }

        public void PlayExplosionSound()
        {
            if (explosionClip != null)
                audioSource.PlayOneShot(explosionClip, 0.8f);
            else
                PlayProceduralNoise("explosion", 0.25f, 0.5f);
        }

        /// <summary>
        /// Plays a procedural pitch sweep tone. The clip is generated on first use and cached under "key".
        /// </summary>
        private void PlayProceduralTone(string key, float startFreq, float endFreq, float duration, float volume)
        {
            if (audioSource == null) return;

            AudioClip clip;
            if (!proceduralClips.TryGetValue(key, out clip) || clip == null)
            {
                clip = BuildToneClip(key, startFreq, endFreq, duration, volume);
                proceduralClips[key] = clip;
            }
            audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Plays a procedural noise burst. The clip is generated on first use and cached under "key".
        /// </summary>
        private void PlayProceduralNoise(string key, float duration, float volume)
        {
            if (audioSource == null) return;

            AudioClip clip;
            if (!proceduralClips.TryGetValue(key, out clip) || clip == null)
            {
                clip = BuildNoiseClip(key, duration, volume);
                proceduralClips[key] = clip;
            }
            audioSource.PlayOneShot(clip, volume);
        }

        private static AudioClip BuildToneClip(string key, float startFreq, float endFreq, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float envelope = 1f - t; // Linear decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)sampleRate)) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_" + key, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip BuildNoiseClip(string key, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float envelope = Mathf.Pow(1f - t, 2f);
                samples[i] = (Random.value * 2f - 1f) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create("Proc_" + key, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
