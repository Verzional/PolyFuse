using System.Collections.Generic;
using UnityEngine;

namespace PolyFuse.Juice
{
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudio : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioSource _chimeSource;
        private AudioSource _fanfareSource;
        private AudioSource _heartbeatSource;

        private AudioClip _snapClickClip;
        private AudioClip[] _comboChords;
        private AudioClip _multiLineClip;
        private AudioClip _boardWipeClip;
        private AudioClip _gameOverClip;
        private AudioClip _heartbeatClip;
        private AudioClip _closeCallClip;
        private AudioClip _newBestClip;
        private AudioClip _clutchSaveClip;
        private AudioClip _crossAxisClip;
        private AudioClip _triAxisClip;

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
            _audioSource.spatialBlend = 0f;

            // Dedicated audio source channels to prevent pitch-overlap artifacts
            _chimeSource = gameObject.AddComponent<AudioSource>();
            _chimeSource.playOnAwake = false;
            _chimeSource.spatialBlend = 0f;

            _fanfareSource = gameObject.AddComponent<AudioSource>();
            _fanfareSource.playOnAwake = false;
            _fanfareSource.spatialBlend = 0f;

            GenerateProceduralClips();
        }

        private void GenerateProceduralClips()
        {
            // 1. Tactile Snap Click: Crisp, bright wooden/acrylic click with snappy transient and high frequency air (zero low-mid boom)
            _snapClickClip = CreateSynthClip("SnapClick", 0.055f, (t, dur) =>
            {
                // Sharp high-frequency transient cracks (< 3ms)
                float clickCrack = Mathf.Sin(2f * Mathf.PI * 8200f * t) * Mathf.Exp(-t * 480f) * 0.60f;
                float clickAir = Mathf.Sin(2f * Mathf.PI * 5600f * t) * Mathf.Exp(-t * 320f) * 0.55f;
                float noiseSnap = (Mathf.Sin(14831f * t) * Mathf.Sin(7321f * t)) * Mathf.Exp(-t * 520f) * 0.40f;

                // Acrylic & polished wood modal resonances
                float acrylicMode = Mathf.Sin(2f * Mathf.PI * 3100f * t) * Mathf.Exp(-t * 130f) * 0.70f;
                float woodMode = Mathf.Sin(2f * Mathf.PI * 1850f * t) * Mathf.Exp(-t * 95f) * 0.50f;
                float tapBody = Mathf.Sin(2f * Mathf.PI * 980f * t) * Mathf.Exp(-t * 110f) * 0.35f;

                return (clickCrack + clickAir + noiseSnap + acrylicMode + woodMode + tapBody) * 0.85f;
            });

            // 2. Ascending Musical Arpeggios for Combo Streaks (Enhanced 4-note crystal chime chords)
            _comboChords = new AudioClip[ComboFrequencies.Length];
            for (int i = 0; i < ComboFrequencies.Length; i++)
            {
                float root = ComboFrequencies[i];
                float third = root * 1.25992f;  // Major 3rd
                float fifth = root * 1.49831f;  // Perfect 5th
                float octave = root * 2.0f;     // Octave harmonic
                float tenth = third * 2.0f;     // Octave + 3rd sparkle

                _comboChords[i] = CreateSynthClip($"ComboChord_{i}", 0.50f, (t, dur) =>
                {
                    // 4 ascending cascading crystal notes
                    float n1 = SynthesizeChimeNote(t, 0.000f, root, 0.90f);
                    float n2 = SynthesizeChimeNote(t, 0.032f, third, 0.80f);
                    float n3 = SynthesizeChimeNote(t, 0.064f, fifth, 0.70f);
                    float n4 = SynthesizeChimeNote(t, 0.096f, octave, 0.65f);

                    // High shimmer bell harmonics
                    float shimmer1 = (t > 0.110f) ? Mathf.Sin(2f * Mathf.PI * tenth * (t - 0.110f)) * 0.25f * Mathf.Exp(-(t - 0.110f) * 11.0f) : 0f;
                    float shimmer2 = (t > 0.040f) ? Mathf.Sin(2f * Mathf.PI * (octave * 1.5f) * (t - 0.040f)) * 0.20f * Mathf.Exp(-(t - 0.040f) * 13.0f) : 0f;

                    return (n1 + n2 + n3 + n4 + shimmer1 + shimmer2) * 0.65f;
                });
            }

            // 3. Multi-Line Clear: Crystal-clear shimmering laser chord with tight sub punch
            _multiLineClip = CreateSynthClip("MultiLineDetonate", 0.65f, (t, dur) =>
            {
                // Layer 1: Tight sub punch (Phase-continuous 150Hz -> 42Hz)
                float phiSub = 2f * Mathf.PI * (42f * t + (108f / 28f) * (1f - Mathf.Exp(-28f * t)));
                float sub = Mathf.Sin(phiSub) * Mathf.Exp(-t * 18.0f) * 0.65f;

                // Layer 2: Laser chirp / zap (Phase-continuous 3800Hz -> 800Hz)
                float phiLaser = 2f * Mathf.PI * (800f * t + (3000f / 38f) * (1f - Mathf.Exp(-38f * t)));
                float laser = Mathf.Sin(phiLaser) * Mathf.Exp(-t * 26.0f) * 0.45f;
                float laserSpark = Mathf.Sin(2f * Mathf.PI * 4600f * t) * Mathf.Exp(-t * 45.0f) * 0.30f;

                // Layer 3: Shimmering crystal laser chord
                float chordEnv = Mathf.Exp(-t * 4.2f);
                float c5 = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) + Mathf.Sin(2f * Mathf.PI * 525.5f * t) * 0.5f) * 0.45f;
                float e5 = (Mathf.Sin(2f * Mathf.PI * 659.25f * t) + Mathf.Sin(2f * Mathf.PI * 657.0f * t) * 0.5f) * 0.40f;
                float g5 = Mathf.Sin(2f * Mathf.PI * 783.99f * t) * 0.40f;
                float b5 = Mathf.Sin(2f * Mathf.PI * 987.77f * t) * 0.35f;
                float c6 = Mathf.Sin(2f * Mathf.PI * 1046.50f * t) * 0.35f;
                float shimmer = (Mathf.Sin(2f * Mathf.PI * 1567.98f * t) + Mathf.Sin(2f * Mathf.PI * 2093.00f * t) * 0.7f) * 0.30f * Mathf.Exp(-t * 8.0f);

                float chord = (c5 + e5 + g5 + b5 + c6 + shimmer) * chordEnv;

                return (sub + laser + laserSpark + chord) * 0.70f;
            });

            // 4. Board Wipe Fanfare: Victorious ascending crystal triad
            _boardWipeClip = CreateSynthClip("BoardWipe", 0.90f, (t, dur) =>
            {
                float n1 = SynthesizeChimeNote(t, 0.000f, 523.25f, 0.70f);  // C5
                float n2 = SynthesizeChimeNote(t, 0.045f, 659.25f, 0.75f);  // E5
                float n3 = SynthesizeChimeNote(t, 0.090f, 783.99f, 0.80f);  // G5
                float n4 = SynthesizeChimeNote(t, 0.135f, 1046.50f, 0.85f); // C6

                // Sustained grand chord at t = 0.180s
                float c1 = SynthesizeChimeNote(t, 0.180f, 523.25f, 0.55f);  // C5
                float c2 = SynthesizeChimeNote(t, 0.180f, 659.25f, 0.55f);  // E5
                float c3 = SynthesizeChimeNote(t, 0.180f, 783.99f, 0.60f);  // G5
                float c4 = SynthesizeChimeNote(t, 0.180f, 1046.50f, 0.65f); // C6
                float c5 = SynthesizeChimeNote(t, 0.180f, 1318.51f, 0.50f); // E6
                float c6 = SynthesizeChimeNote(t, 0.180f, 1567.98f, 0.45f); // G6

                float shimmer = (t > 0.180f) ? Mathf.Sin(2f * Mathf.PI * 2093.00f * (t - 0.180f)) * 0.25f * Mathf.Exp(-(t - 0.180f) * 6.0f) : 0f;

                return (n1 + n2 + n3 + n4 + c1 + c2 + c3 + c4 + c5 + c6 + shimmer) * 0.52f;
            });

            // 5. Game Over Descending Tone
            _gameOverClip = CreateSynthClip("GameOver", 0.65f, (t, dur) =>
            {
                float env = Mathf.Exp(-t * 3.5f);
                float phi = 2f * Mathf.PI * (90f * t + (270f / 4.5f) * (1f - Mathf.Exp(-4.5f * t)));
                float fund = Mathf.Sin(phi);
                float harm = Mathf.Sin(2f * phi) * 0.35f;
                return (fund + harm) * env * 0.75f;
            });

            // 6. Danger Heartbeat Loop: Punchy, speaker-audible Lub-Dub (~62 BPM)
            _heartbeatClip = CreateSynthClip("HeartbeatLoop", 0.96f, (t, dur) =>
            {
                // "Lub" (First thump at t = 0.0s - 0.20s)
                if (t < 0.20f)
                {
                    float env = Mathf.Exp(-t * 22.0f);
                    float phi = 2f * Mathf.PI * (75f * t + (130f / 32f) * (1f - Mathf.Exp(-32f * t)));
                    float fund = Mathf.Sin(phi);
                    float harm = Mathf.Sin(2f * phi) * 0.55f;
                    float harm3 = Mathf.Sin(3f * phi) * 0.25f;
                    float click = Mathf.Sin(2f * Mathf.PI * 340f * t) * Mathf.Exp(-t * 65.0f) * 0.40f;
                    return (fund + harm + harm3 + click) * env * 0.95f;
                }
                // "Dub" (Second thump at t = 0.25s - 0.46s)
                else if (t >= 0.25f && t < 0.46f)
                {
                    float t2 = t - 0.25f;
                    float env = Mathf.Exp(-t2 * 24.0f);
                    float phi = 2f * Mathf.PI * (65f * t2 + (110f / 34f) * (1f - Mathf.Exp(-34f * t2)));
                    float fund = Mathf.Sin(phi);
                    float harm = Mathf.Sin(2f * phi) * 0.50f;
                    float harm3 = Mathf.Sin(3f * phi) * 0.20f;
                    float click = Mathf.Sin(2f * Mathf.PI * 300f * t2) * Mathf.Exp(-t2 * 65.0f) * 0.30f;
                    return (fund + harm + harm3 + click) * env * 0.85f;
                }
                return 0f;
            });

            // 7. "CLOSE CALL!" Escape Fanfare: Bright, vibrant brass harmonies without mud
            _closeCallClip = CreateSynthClip("CloseCall", 0.80f, (t, dur) =>
            {
                float s1 = SynthesizeBrassNote(t, 0.000f, 440.00f, 0.06f, 0.70f); // A4
                float s2 = SynthesizeBrassNote(t, 0.055f, 554.37f, 0.06f, 0.75f); // C#5
                float s3 = SynthesizeBrassNote(t, 0.110f, 659.25f, 0.06f, 0.80f); // E5

                // Sustained triumphant A Major fanfare chord at t = 0.165s
                float c1 = SynthesizeBrassNote(t, 0.165f, 440.00f, 0.60f, 0.60f); // A4
                float c2 = SynthesizeBrassNote(t, 0.165f, 659.25f, 0.60f, 0.55f); // E5
                float c3 = SynthesizeBrassNote(t, 0.165f, 880.00f, 0.60f, 0.65f); // A5
                float c4 = SynthesizeBrassNote(t, 0.165f, 1108.73f, 0.60f, 0.50f); // C#6
                float c5 = SynthesizeBrassNote(t, 0.165f, 1318.51f, 0.60f, 0.40f); // E6

                // High triumphant chime ring
                float chime = (t > 0.165f) ? Mathf.Sin(2f * Mathf.PI * 1760f * (t - 0.165f)) * 0.25f * Mathf.Exp(-(t - 0.165f) * 6.5f) : 0f;

                return (s1 + s2 + s3 + c1 + c2 + c3 + c4 + c5 + chime) * 0.55f;
            });

            // 8. "NEW BEST!" Sparkling Chime Fanfare: Bright, vibrant chime harmonies without mud
            _newBestClip = CreateSynthClip("NewBest", 1.0f, (t, dur) =>
            {
                float n1 = SynthesizeChimeNote(t, 0.000f, 587.33f, 0.70f);  // D5
                float n2 = SynthesizeChimeNote(t, 0.045f, 739.99f, 0.75f);  // F#5
                float n3 = SynthesizeChimeNote(t, 0.090f, 880.00f, 0.80f);  // A5
                float n4 = SynthesizeChimeNote(t, 0.135f, 1174.66f, 0.85f); // D6
                float n5 = SynthesizeChimeNote(t, 0.180f, 1479.98f, 0.85f); // F#6

                // Grand celebration chord at t = 0.225s
                float c1 = SynthesizeChimeNote(t, 0.225f, 587.33f, 0.55f);  // D5
                float c2 = SynthesizeChimeNote(t, 0.225f, 880.00f, 0.60f);   // A5
                float c3 = SynthesizeChimeNote(t, 0.225f, 1174.66f, 0.65f); // D6
                float c4 = SynthesizeChimeNote(t, 0.225f, 1479.98f, 0.60f); // F#6
                float c5 = SynthesizeChimeNote(t, 0.225f, 1760.00f, 0.50f); // A6
                float c6 = SynthesizeChimeNote(t, 0.225f, 2349.32f, 0.40f); // D7

                // Shimmering celestial sparkle tail
                float shimmer = 0f;
                if (t > 0.225f)
                {
                    float tc = t - 0.225f;
                    float s1 = Mathf.Sin(2f * Mathf.PI * 3520.00f * tc) * 0.25f;
                    float s2 = Mathf.Sin(2f * Mathf.PI * 4698.64f * tc) * 0.20f;
                    float s3 = Mathf.Sin(2f * Mathf.PI * 5873.30f * tc) * 0.15f;
                    shimmer = (s1 + s2 + s3) * Mathf.Exp(-tc * 5.0f);
                }

                return (n1 + n2 + n3 + n4 + n5 + c1 + c2 + c3 + c4 + c5 + c6 + shimmer) * 0.50f;
            });

            // 9. "CLUTCH SAVE!" Heroic Crystal Surge: Rapid ascending arpeggio with sharp metallic resonance
            _clutchSaveClip = CreateSynthClip("ClutchSave", 0.65f, (t, dur) =>
            {
                float n1 = SynthesizeChimeNote(t, 0.000f, 739.99f, 0.70f);  // F#5
                float n2 = SynthesizeChimeNote(t, 0.038f, 880.00f, 0.75f);  // A5
                float n3 = SynthesizeChimeNote(t, 0.076f, 1108.73f, 0.85f); // C#6
                float n4 = SynthesizeChimeNote(t, 0.114f, 1479.98f, 0.90f); // F#6

                // Heroic chord surge at t = 0.140s
                float c1 = SynthesizeChimeNote(t, 0.140f, 739.99f, 0.50f);
                float c2 = SynthesizeChimeNote(t, 0.140f, 1108.73f, 0.60f);
                float c3 = SynthesizeChimeNote(t, 0.140f, 1479.98f, 0.65f);
                float c4 = SynthesizeChimeNote(t, 0.140f, 1760.00f, 0.50f);

                // High metallic strike ping
                float ping = 0f;
                if (t > 0.140f)
                {
                    float tp = t - 0.140f;
                    ping = Mathf.Sin(2f * Mathf.PI * 3520f * tp) * 0.25f * Mathf.Exp(-tp * 18.0f);
                }

                return (n1 + n2 + n3 + n4 + c1 + c2 + c3 + c4 + ping) * 0.60f;
            });

            // 10. "DUAL-AXIS CROSS!" Harmonious 2-pole geometric convergence chord
            _crossAxisClip = CreateSynthClip("CrossAxis", 0.70f, (t, dur) =>
            {
                float n1 = SynthesizeChimeNote(t, 0.000f, 659.25f, 0.70f);  // E5
                float n2 = SynthesizeChimeNote(t, 0.035f, 830.61f, 0.75f);  // G#5
                float n3 = SynthesizeChimeNote(t, 0.070f, 987.77f, 0.80f);  // B5

                // Convergence chord
                float c1 = SynthesizeChimeNote(t, 0.095f, 659.25f, 0.55f);  // E5
                float c2 = SynthesizeChimeNote(t, 0.095f, 987.77f, 0.60f);  // B5
                float c3 = SynthesizeChimeNote(t, 0.095f, 1318.51f, 0.65f); // E6
                float c4 = SynthesizeChimeNote(t, 0.095f, 1661.22f, 0.50f); // G#6

                return (n1 + n2 + n3 + c1 + c2 + c3 + c4) * 0.60f;
            });

            // 11. "TRI-AXIS TRINITY!" Celestial 3-frequency major triad alignment
            _triAxisClip = CreateSynthClip("TriAxis", 0.85f, (t, dur) =>
            {
                float n1 = SynthesizeChimeNote(t, 0.000f, 523.25f, 0.65f);  // C5
                float n2 = SynthesizeChimeNote(t, 0.032f, 659.25f, 0.70f);  // E5
                float n3 = SynthesizeChimeNote(t, 0.064f, 783.99f, 0.75f);  // G5
                float n4 = SynthesizeChimeNote(t, 0.096f, 1046.50f, 0.80f); // C6

                // Grand Trinity Chord at t = 0.125s
                float c1 = SynthesizeChimeNote(t, 0.125f, 523.25f, 0.50f);  // C5
                float c2 = SynthesizeChimeNote(t, 0.125f, 783.99f, 0.55f);  // G5
                float c3 = SynthesizeChimeNote(t, 0.125f, 1046.50f, 0.60f); // C6
                float c4 = SynthesizeChimeNote(t, 0.125f, 1318.51f, 0.55f); // E6
                float c5 = SynthesizeChimeNote(t, 0.125f, 1567.98f, 0.50f); // G6
                float c6 = SynthesizeChimeNote(t, 0.125f, 2093.00f, 0.40f); // C7

                // Tight Sub punch for dimensional weight
                float sub = 0f;
                if (t > 0.125f)
                {
                    float ts = t - 0.125f;
                    sub = Mathf.Sin(2f * Mathf.PI * 65.41f * ts) * 0.45f * Mathf.Exp(-ts * 14.0f);
                }

                return (n1 + n2 + n3 + n4 + c1 + c2 + c3 + c4 + c5 + c6 + sub) * 0.50f;
            });

            InitHeartbeatSource();
            UpdateMuteState();
        }


        private static float SynthesizeChimeNote(float t, float onset, float freq, float ampWeight)
        {
            float tn = t - onset;
            if (tn < 0f) return 0f;

            // Smooth 2.5ms attack to avoid raw clicks / DC offset
            float attack = (tn < 0.0025f) ? (tn / 0.0025f) : 1.0f;
            float decay = Mathf.Exp(-tn * 6.2f);

            // 1. Fundamental
            float fund = Mathf.Sin(2f * Mathf.PI * freq * tn);
            // 2. 2nd Harmonic (Body / Warmth)
            float harm2 = Mathf.Sin(4f * Mathf.PI * freq * tn) * 0.35f * Mathf.Exp(-tn * 7.5f);
            // 3. 3rd Harmonic (Presence)
            float harm3 = Mathf.Sin(6f * Mathf.PI * freq * tn) * 0.18f * Mathf.Exp(-tn * 9.5f);
            // 4. Glass / Chime Metallic Inharmonic (2.756f)
            float chime = Mathf.Sin(2f * Mathf.PI * (freq * 2.756f) * tn) * 0.22f * Mathf.Exp(-tn * 14.0f);
            // 5. Mallet strike transient ping (4.2f)
            float ping = Mathf.Sin(2f * Mathf.PI * (freq * 4.2f) * tn) * 0.20f * Mathf.Exp(-tn * 40.0f);

            return (fund + harm2 + harm3 + chime + ping) * attack * decay * ampWeight;
        }

        private static float SynthesizeBrassNote(float t, float onset, float freq, float duration, float amp)
        {
            float tn = t - onset;
            if (tn < 0f) return 0f;

            float attack = (tn < 0.003f) ? (tn / 0.003f) : 1.0f;
            float decay = Mathf.Exp(-tn * (duration > 0.20f ? 3.8f : 9.5f));

            // Brass harmonic spectrum: Fundamental + 2nd, 3rd, 4th harmonics
            float f1 = Mathf.Sin(2f * Mathf.PI * freq * tn);
            float f2 = Mathf.Sin(4f * Mathf.PI * freq * tn) * 0.55f;
            float f3 = Mathf.Sin(6f * Mathf.PI * freq * tn) * 0.30f;
            float f4 = Mathf.Sin(8f * Mathf.PI * freq * tn) * 0.15f;

            // Brass bite transient on onset
            float bite = Mathf.Sin(2f * Mathf.PI * (freq * 3.5f) * tn) * 0.25f * Mathf.Exp(-tn * 35.0f);

            return (f1 + f2 + f3 + f4 + bite) * attack * decay * amp;
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
            if (_chimeSource != null) _chimeSource.mute = !_soundEnabled;
            if (_fanfareSource != null) _fanfareSource.mute = !_soundEnabled;
            if (_heartbeatSource != null) _heartbeatSource.mute = !_soundEnabled;
        }

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
            if (_fanfareSource == null || _closeCallClip == null || !_soundEnabled) return;
            _fanfareSource.pitch = 1.0f;
            _fanfareSource.PlayOneShot(_closeCallClip, 1.0f);
        }

        public void PlayNewBestFanfare()
        {
            if (_fanfareSource == null || _newBestClip == null || !_soundEnabled) return;
            _fanfareSource.pitch = 1.0f;
            _fanfareSource.PlayOneShot(_newBestClip, 1.0f);
        }

        private AudioClip CreateSynthClip(string name, float duration, System.Func<float, float, float> generator)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            float maxAmp = 0.0001f;
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float s = generator(t, duration);
                float absS = Mathf.Abs(s);
                if (absS > maxAmp) maxAmp = absS;
                data[i] = s;
            }

            // Target peak amplitude: 0.90 to leave clean headroom without digital clipping
            float targetPeak = 0.90f;
            float gain = (maxAmp > 0.0001f) ? (targetPeak / maxAmp) : 1.0f;
            if (gain > 3.0f) gain = 3.0f; // Limit excessive boost for quiet generators

            int fadeInSamples = Mathf.Min(64, totalSamples / 8);   // ~1.4ms smooth fade in
            int fadeOutSamples = Mathf.Min(384, totalSamples / 4); // ~8.7ms smooth quadratic fade out

            for (int i = 0; i < totalSamples; i++)
            {
                float sample = data[i] * gain;

                // Smooth soft-limiting / saturation to guarantee zero hard-clipping
                if (sample > 1.0f) sample = 1.0f - Mathf.Exp(-sample);
                else if (sample < -1.0f) sample = -1.0f + Mathf.Exp(sample);
                else sample = Mathf.Clamp(sample, -1.0f, 1.0f);

                // De-clicking envelope at edges
                if (i < fadeInSamples)
                {
                    sample *= (float)i / fadeInSamples;
                }
                else if (i >= totalSamples - fadeOutSamples)
                {
                    float f = (float)(totalSamples - 1 - i) / fadeOutSamples;
                    sample *= f * f;
                }

                data[i] = sample;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayPieceSnap()
        {
            if (_audioSource == null || _snapClickClip == null || !_soundEnabled) return;
            _audioSource.pitch = Random.Range(0.97f, 1.03f);
            _audioSource.PlayOneShot(_snapClickClip, 0.95f);
        }

        public void PlayLineClear(int comboStreak)
        {
            if (_chimeSource == null || _comboChords == null || _comboChords.Length == 0 || !_soundEnabled) return;

            int clampedStreak = Mathf.Max(1, comboStreak);
            int index = Mathf.Clamp(clampedStreak - 1, 0, _comboChords.Length - 1);
            AudioClip clip = _comboChords[index];

            // Pitch escalation: higher combos beyond the scale array scale pitch upwards
            float pitchMult = 1.0f;
            if (clampedStreak > _comboChords.Length)
            {
                pitchMult = Mathf.Pow(1.05946f, clampedStreak - _comboChords.Length);
            }

            _chimeSource.pitch = pitchMult;
            _chimeSource.PlayOneShot(clip, 1.0f);
        }

        public void PlayMultiLineClear(int lineCount, int comboStreak)
        {
            if (!_soundEnabled) return;

            if (_fanfareSource != null && _multiLineClip != null)
            {
                _fanfareSource.pitch = 1.0f;
                _fanfareSource.PlayOneShot(_multiLineClip, 1.0f);
            }
            PlayLineClear(comboStreak);
        }

        public void PlayBoardWipe()
        {
            if (_fanfareSource == null || _boardWipeClip == null || !_soundEnabled) return;
            _fanfareSource.pitch = 1.0f;
            _fanfareSource.PlayOneShot(_boardWipeClip, 1.0f);
        }

        public void PlayClutchSave()
        {
            if (_fanfareSource == null || _clutchSaveClip == null || !_soundEnabled) return;
            _fanfareSource.pitch = 1.0f;
            _fanfareSource.PlayOneShot(_clutchSaveClip, 1.0f);
        }

        public void PlayCrossAxisConvergence(int distinctAxes)
        {
            if (_fanfareSource == null || !_soundEnabled) return;
            AudioClip clip = (distinctAxes >= 3) ? _triAxisClip : _crossAxisClip;
            if (clip == null) return;
            _fanfareSource.pitch = 1.0f;
            _fanfareSource.PlayOneShot(clip, 1.0f);
        }

        public void PlayGameOver()
        {
            PlayHeartbeat(false);
            if (_fanfareSource == null || _gameOverClip == null || !_soundEnabled) return;
            _fanfareSource.pitch = 1.0f;
            _fanfareSource.PlayOneShot(_gameOverClip, 0.95f);
        }
    }
}
