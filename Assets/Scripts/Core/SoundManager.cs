using UnityEngine;

namespace HazeBuster.Core
{
    /// <summary>
    /// Sound manager capable of playing AudioClips, but also features a built-in
    /// procedural 8-bit retro sound synthesizer! That way, sound effects work
    /// immediately even without importing external .wav/.mp3 audio files.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource audioSource;

        [Header("Audio Clips (Optional - will use procedural sounds if unassigned)")]
        public AudioClip shootClip;
        public AudioClip dashClip;
        public AudioClip hitClip;
        public AudioClip dewCollectClip;
        public AudioClip levelUpClip;
        public AudioClip explosionClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
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

        public void PlayShootSound()
        {
            if (shootClip != null)
                audioSource.PlayOneShot(shootClip, 0.4f);
            else
                PlayProceduralTone(440f, 660f, 0.08f, 0.25f); // Quick chirp for water squirt
        }

        public void PlayDashSound()
        {
            if (dashClip != null)
                audioSource.PlayOneShot(dashClip, 0.6f);
            else
                PlayProceduralTone(220f, 110f, 0.15f, 0.35f); // Whoosh drop
        }

        public void PlayHitSound()
        {
            if (hitClip != null)
                audioSource.PlayOneShot(hitClip, 0.5f);
            else
                PlayProceduralTone(180f, 80f, 0.12f, 0.4f); // Low thud
        }

        public void PlayDewCollectSound()
        {
            if (dewCollectClip != null)
                audioSource.PlayOneShot(dewCollectClip, 0.5f);
            else
                PlayProceduralTone(587f, 880f, 0.1f, 0.3f); // Happy chime
        }

        public void PlayLevelUpSound()
        {
            if (levelUpClip != null)
                audioSource.PlayOneShot(levelUpClip, 0.7f);
            else
                PlayProceduralFanfare();
        }

        public void PlayExplosionSound()
        {
            if (explosionClip != null)
                audioSource.PlayOneShot(explosionClip, 0.8f);
            else
                PlayProceduralNoise(0.25f, 0.5f);
        }

        /// <summary>
        /// Generates a procedural pitch sweep tone at runtime.
        /// </summary>
        private void PlayProceduralTone(float startFreq, float endFreq, float duration, float volume)
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

            AudioClip clip = AudioClip.Create("ProcTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, volume);
        }

        private void PlayProceduralNoise(float duration, float volume)
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

            AudioClip clip = AudioClip.Create("ProcNoise", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, volume);
        }

        private void PlayProceduralFanfare()
        {
            PlayProceduralTone(440f, 880f, 0.35f, 0.4f);
        }
    }
}
