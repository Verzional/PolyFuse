using UnityEngine;

namespace PolyFuse.Juice
{
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudio : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioClip _snapClickClip;
        private AudioClip _lineClearClip;
        private AudioClip _hexDetonateClip;
        private AudioClip _comboStreakClip;
        private AudioClip _gameOverClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;

            GenerateProceduralClips();
        }

        private void GenerateProceduralClips()
        {
            _snapClickClip = CreateSynthClip("SnapClick", 0.08f, (t, dur) =>
            {
                // Crisp click: exponential decay sine burst with wood knock
                float env = Mathf.Exp(-t * 45f);
                float freq = Mathf.Lerp(600f, 150f, t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            });

            _lineClearClip = CreateSynthClip("LineClear", 0.32f, (t, dur) =>
            {
                // Sharp chime / harmonic bell
                float env = Mathf.Exp(-t * 9f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 587.33f * t); // D5
                float f2 = Mathf.Sin(2f * Mathf.PI * 880.00f * t) * 0.6f; // A5
                float f3 = Mathf.Sin(2f * Mathf.PI * 1174.66f * t) * 0.4f; // D6
                return (f1 + f2 + f3) * env * 0.5f;
            });

            _hexDetonateClip = CreateSynthClip("HexDetonate", 0.55f, (t, dur) =>
            {
                // Deep resonant bass boom + metallic ring
                float env = Mathf.Exp(-t * 6f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 45f, t / dur) * t);
                float ring = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.4f * Mathf.Exp(-t * 12f);
                return (sub * 0.8f + ring) * env * 0.8f;
            });

            _comboStreakClip = CreateSynthClip("ComboStreak", 0.35f, (t, dur) =>
            {
                // Ascending bright chime
                float env = Mathf.Exp(-t * 8f);
                float freq = Mathf.Lerp(440f, 880f, t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.6f;
            });

            _gameOverClip = CreateSynthClip("GameOver", 0.6f, (t, dur) =>
            {
                // Descending tone
                float env = Mathf.Exp(-t * 4f);
                float freq = Mathf.Lerp(350f, 110f, t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.7f;
            });
        }

        private AudioClip CreateSynthClip(string name, float duration, System.Func<float, float, float> generator)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(generator(t, duration), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayPieceSnap()
        {
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_snapClickClip, 0.8f);
        }

        public void PlayLineClear(float pitchMultiplier)
        {
            _audioSource.pitch = Mathf.Clamp(pitchMultiplier, 0.8f, 2.5f);
            _audioSource.PlayOneShot(_lineClearClip, 0.9f);
        }

        public void PlayHexDetonate(float pitchMultiplier)
        {
            _audioSource.pitch = Mathf.Clamp(pitchMultiplier, 0.8f, 2.5f);
            _audioSource.PlayOneShot(_hexDetonateClip, 1.0f);
        }

        public void PlayGameOver()
        {
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_gameOverClip, 0.9f);
        }
    }
}
