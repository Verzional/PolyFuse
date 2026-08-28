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

            // 2. Ascending Musical Arpeggios for Combo Streaks
            _comboChords = new AudioClip[ComboFrequencies.Length];
            for (int i = 0; i < ComboFrequencies.Length; i++)
            {
                float root = ComboFrequencies[i];
                float third = root * 1.2599f;  // Major 3rd
                float fifth = root * 1.4983f;  // Perfect 5th
                float octave = root * 2.0f;     // Octave harmonic

                _comboChords[i] = CreateSynthClip($"ComboChord_{i}", 0.45f, (t, dur) =>
                {
                    float env = Mathf.Exp(-t * 6.5f);
                    // Rapid 4-note ascending crystal arpeggio on each hit
                    float n1 = Mathf.Sin(2f * Mathf.PI * root * t);
                    float n2 = (t > 0.035f) ? Mathf.Sin(2f * Mathf.PI * third * (t - 0.035f)) * 0.75f : 0f;
                    float n3 = (t > 0.070f) ? Mathf.Sin(2f * Mathf.PI * fifth * (t - 0.070f)) * 0.65f : 0f;
                    float n4 = (t > 0.105f) ? Mathf.Sin(2f * Mathf.PI * octave * (t - 0.105f)) * 0.50f : 0f;
                    float sparkle = Mathf.Sin(2f * Mathf.PI * (octave * 1.5f) * t) * 0.25f * Mathf.Exp(-t * 14f);

                    return (n1 + n2 + n3 + n4 + sparkle) * env * 0.70f;
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

            // 6. Danger Heartbeat Loop: Punchy, speaker-audible Lub-Dub (~62 BPM)
            _heartbeatClip = CreateSynthClip("HeartbeatLoop", 0.96f, (t, dur) =>
            {
                // "Lub" (First thump at t = 0.0s - 0.20s)
                if (t < 0.20f)
                {
                    float env = Mathf.Exp(-t * 24.0f);
                    float freq = 75f + 130f * Mathf.Exp(-t * 32.0f); // 205Hz down to 75Hz
                    float fund = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float harm = Mathf.Sin(4f * Mathf.PI * freq * t) * 0.55f; // 2nd harmonic
                    float harm3 = Mathf.Sin(6f * Mathf.PI * freq * t) * 0.25f; // 3rd harmonic
                    float click = Mathf.Sin(2f * Mathf.PI * 340f * t) * Mathf.Exp(-t * 65.0f) * 0.40f;
                    return (fund + harm + harm3 + click) * env * 0.95f;
                }
                // "Dub" (Second thump at t = 0.26s - 0.46s)
                else if (t >= 0.25f && t < 0.46f)
                {
                    float t2 = t - 0.25f;
                    float env = Mathf.Exp(-t2 * 26.0f);
                    float freq = 65f + 110f * Mathf.Exp(-t2 * 34.0f); // 175Hz down to 65Hz
                    float fund = Mathf.Sin(2f * Mathf.PI * freq * t2);
                    float harm = Mathf.Sin(4f * Mathf.PI * freq * t2) * 0.50f;
                    float harm3 = Mathf.Sin(6f * Mathf.PI * freq * t2) * 0.20f;
                    float click = Mathf.Sin(2f * Mathf.PI * 300f * t2) * Mathf.Exp(-t2 * 65.0f) * 0.30f;
                    return (fund + harm + harm3 + click) * env * 0.85f;
                }
                return 0f;
            });

            // 7. "CLOSE CALL!" Escape Fanfare
            _closeCallClip = CreateSynthClip("CloseCall", 0.75f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 4.0f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 440f * t);        // A4
                float f2 = Mathf.Sin(2f * Mathf.PI * 554.37f * t) * 0.8f; // C#5
                float f3 = Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.8f; // E5
                float f4 = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.6f;    // A5
                return (f1 + f2 + f3 + f4) * env * 0.55f;
            });

            // 8. "NEW BEST!" Sparkling Chime Fanfare
            _newBestClip = CreateSynthClip("NewBest", 0.90f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 3.2f);
                float f1 = Mathf.Sin(2f * Mathf.PI * 587.33f * t); // D5
                float f2 = Mathf.Sin(2f * Mathf.PI * 739.99f * t) * 0.8f; // F#5
                float f3 = Mathf.Sin(2f * Mathf.PI * 880.00f * t) * 0.8f; // A5
                float f4 = Mathf.Sin(2f * Mathf.PI * 1174.66f * t) * 0.6f; // D6
                float shimmer = Mathf.Sin(2f * Mathf.PI * 2349.32f * t) * 0.35f * Mathf.Exp(-t * 10f);
                return (f1 + f2 + f3 + f4 + shimmer) * env * 0.5f;
            });

            InitHeartbeatSource();
            UpdateMuteState();
        }

        private void InitHeartbeatSource()
        {
            if (_heartbeatSource == null)
            {
                _heartbeatSource = gameObject.AddComponent<AudioSource>();
            }
            _heartbeatSource.clip = _heartbeatClip;
            _heartbeatSource.loop = true;
            _heartbeatSource.playOnAwake = false;
            _heartbeatSource.volume = 1.0f;
            _heartbeatSource.spatialBlend = 0f;
        }

        private const string SoundPrefKey = "PolyFuse_SoundEnabled";
        private bool _soundEnabled = true;
        public bool IsSoundEnabled => _soundEnabled;

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
            PlayerPrefs.SetInt(SoundPrefKey, _soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            UpdateMuteState();
        }

        public void ToggleSound()
        {
            SetSoundEnabled(!_soundEnabled);
        }

        private void UpdateMuteState()
        {
            _soundEnabled = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
            if (_audioSource != null) _audioSource.mute = !_soundEnabled;
            if (_heartbeatSource != null) _heartbeatSource.mute = !_soundEnabled;
        }

        private AudioSource _heartbeatSource;
        private AudioClip _heartbeatClip;
        private AudioClip _closeCallClip;
        private AudioClip _newBestClip;

        public void PlayHeartbeat(bool active)
        {
            if (_heartbeatSource == null) return;
            if (active && !_heartbeatSource.isPlaying && _soundEnabled)
            {
                _heartbeatSource.Play();
            }
            else if (!active && _heartbeatSource.isPlaying)
            {
                _heartbeatSource.Stop();
            }
        }

        public void PlayCloseCallFanfare()
        {
            if (_audioSource == null || _closeCallClip == null || !_soundEnabled) return;
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_closeCallClip, 1.0f);
        }

        public void PlayNewBestFanfare()
        {
            if (_audioSource == null || _newBestClip == null || !_soundEnabled) return;
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_newBestClip, 1.0f);
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
            if (_audioSource == null || _snapClickClip == null || !_soundEnabled) return;
            _audioSource.pitch = Random.Range(0.96f, 1.04f);
            _audioSource.PlayOneShot(_snapClickClip, 0.9f);
        }

        public void PlayLineClear(int comboStreak)
        {
            if (_audioSource == null || _comboChords == null || _comboChords.Length == 0 || !_soundEnabled) return;

            int clampedStreak = Mathf.Max(1, comboStreak);
            int index = Mathf.Clamp(clampedStreak - 1, 0, _comboChords.Length - 1);
            AudioClip clip = _comboChords[index];

            // Pitch escalation: higher combos scale pitch upwards
            float pitchMult = 1.0f;
            if (clampedStreak > _comboChords.Length)
            {
                pitchMult = Mathf.Pow(1.05946f, clampedStreak - _comboChords.Length);
            }

            _audioSource.pitch = pitchMult;
            _audioSource.PlayOneShot(clip, 1.0f);
        }

        public void PlayMultiLineClear(int lineCount, int comboStreak)
        {
            if (_audioSource == null || !_soundEnabled) return;

            _audioSource.pitch = 1.0f;
            if (_multiLineClip != null)
            {
                _audioSource.PlayOneShot(_multiLineClip, 1.0f);
            }
            PlayLineClear(comboStreak);
        }

        public void PlayBoardWipe()
        {
            if (_audioSource == null || _boardWipeClip == null || !_soundEnabled) return;
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_boardWipeClip, 1.0f);
        }

        public void PlayGameOver()
        {
            if (_audioSource == null || _gameOverClip == null || !_soundEnabled) return;
            PlayHeartbeat(false);
            _audioSource.pitch = 1.0f;
            _audioSource.PlayOneShot(_gameOverClip, 0.9f);
        }
    }
}
