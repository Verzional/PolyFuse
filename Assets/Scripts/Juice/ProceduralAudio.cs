using System.Collections.Generic;
using UnityEngine;

namespace PolyFuse.Juice
{
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudio : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioClip _snapClickClip;
        private AudioClip[] _comboChords;
        private AudioClip _multiLineClip;
        private AudioClip _boardWipeClip;
        private AudioClip _gameOverClip;

        // Ascending Pentatonic / Major Scale root frequencies (C4 to C6)
        private static readonly float[] ComboFrequencies = new float[]
        {
            261.63f, // C4
            293.66f, // D4
            329.63f, // E4
            392.00f, // G4
            440.00f, // A4
            523.25f, // C5
            587.33f, // D5
            659.25f, // E5
            783.99f, // G5
            880.00f, // A5
            1046.50f // C6
        };

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
            // 1. Tactile Snap Click: Crisp wooden/stone pop with subtle sub knock
            _snapClickClip = CreateSynthClip("SnapClick", 0.09f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 55f);
                float knock = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(800f, 180f, t / dur) * t);
                float body = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.4f * Mathf.Exp(-t * 30f);
                return (knock + body) * env * 0.85f;
            });

            // 2. Ascending Musical Chords for Combo Streaks
            _comboChords = new AudioClip[ComboFrequencies.Length];
            for (int i = 0; i < ComboFrequencies.Length; i++)
            {
                float root = ComboFrequencies[i];
                float fifth = root * 1.4983f;  // Perfect 5th
                float octave = root * 2.0f;     // Octave harmonic
                float majThird = root * 1.2599f;// Major 3rd

                int chordIndex = i;
                _comboChords[i] = CreateSynthClip($"ComboChord_{i}", 0.38f, (t, dur) =>
                {
                    float env = Mathf.Exp(-t * 7.5f);
                    // Rich bell / marimba timbre with harmonic overtones
                    float s1 = Mathf.Sin(2f * Mathf.PI * root * t);
                    float s2 = Mathf.Sin(2f * Mathf.PI * fifth * t) * 0.55f;
                    float s3 = Mathf.Sin(2f * Mathf.PI * octave * t) * 0.35f * Mathf.Exp(-t * 12f);
                    float s4 = (chordIndex >= 2) ? Mathf.Sin(2f * Mathf.PI * majThird * t) * 0.4f : 0f;

                    return (s1 + s2 + s3 + s4) * env * 0.55f;
                });
            }

            // 3. Multi-Line Clear: Layered sparkle chord with sub-bass impact
            _multiLineClip = CreateSynthClip("MultiLineDetonate", 0.60f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 5.0f);
                float sub = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(130f, 40f, t / dur) * t) * 0.9f;
                float c5 = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.6f;
                float g5 = Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.5f;
                float c6 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * 0.4f * Mathf.Exp(-t * 9f);
                float shimmer = Mathf.Sin(2f * Mathf.PI * 1567.98f * t) * 0.25f * Mathf.Exp(-t * 15f);

                return (sub + c5 + g5 + c6 + shimmer) * env * 0.65f;
            });

            // 4. Board Wipe Fanfare: Victorious ascending triad
            _boardWipeClip = CreateSynthClip("BoardWipe", 0.85f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 3.5f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t); // C5
                float f2 = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.8f; // E5
                float f3 = Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.8f; // G5
                float f4 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * 0.6f; // C6
                return (f1 + f2 + f3 + f4) * env * 0.6f;
            });

            // 5. Game Over Descending Tone
            _gameOverClip = CreateSynthClip("GameOver", 0.65f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 3.5f);
                float freq = Mathf.Lerp(380f, 95f, t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.75f;
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
            if (_audioSource == null || _snapClickClip == null) return;
            _audioSource.pitch = Random.Range(0.96f, 1.04f);
            _audioSource.PlayOneShot(_snapClickClip, 0.9f);
        }

        public void PlayLineClear(int comboStreak)
        {
            if (_audioSource == null || _comboChords == null || _comboChords.Length == 0) return;

            int index = Mathf.Clamp(comboStreak - 1, 0, _comboChords.Length - 1);
            AudioClip clip = _comboChords[index];

            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(clip, 0.95f);
        }

        public void PlayMultiLineClear(int lineCount, int comboStreak)
        {
            if (_audioSource == null) return;

            _audioSource.pitch = 1.0f;
            if (_multiLineClip != null)
            {
                _audioSource.PlayOneShot(_multiLineClip, 1.0f);
            }
            PlayLineClear(comboStreak);
        }

        public void PlayBoardWipe()
        {
            if (_audioSource == null || _boardWipeClip == null) return;
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_boardWipeClip, 1.0f);
        }

        public void PlayGameOver()
        {
            if (_audioSource == null || _gameOverClip == null) return;
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_gameOverClip, 0.9f);
        }
    }
}
