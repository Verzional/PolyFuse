using UnityEngine;

namespace PolyFuse.Juice
{
    public class HapticFeedbackManager : MonoBehaviour
    {
        public static HapticFeedbackManager Instance { get; private set; }

        private const string HapticsPrefKey = "PolyFuse_HapticsEnabled";
        private bool _hapticsEnabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private AndroidJavaClass _vibrationEffectClass;
        private bool _hasCustomVibrations;
#endif

        public bool IsHapticsEnabled => _hapticsEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _hapticsEnabled = PlayerPrefs.GetInt(HapticsPrefKey, 1) == 1;
            InitializePlatformHaptics();
        }

        private void InitializePlatformHaptics()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdkInt = buildVersion.GetStatic<int>("SDK_INT");
                    if (sdkInt >= 26)
                    {
                        _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                        _hasCustomVibrations = true;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PolyFuse] Android haptics init warning: " + e.Message);
            }
#endif
        }

        public void SetHapticsEnabled(bool enabled)
        {
            _hapticsEnabled = enabled;
            PlayerPrefs.SetInt(HapticsPrefKey, _hapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            if (_hapticsEnabled)
            {
                PlayLight();
            }
        }

        public void ToggleHaptics()
        {
            SetHapticsEnabled(!_hapticsEnabled);
        }

        public void PlayLight()
        {
            if (!_hapticsEnabled) return;
            Vibrate(16, 75);
        }

        public void PlayMedium()
        {
            if (!_hapticsEnabled) return;
            Vibrate(38, 160);
        }

        public void PlayHeavy()
        {
            if (!_hapticsEnabled) return;
            Vibrate(65, 255);
        }

        private void Vibrate(long milliseconds, int amplitude)
        {
#if UNITY_EDITOR
            // Safe no-op in Editor
            return;
#elif UNITY_ANDROID
            if (_vibrator == null) return;
            try
            {
                if (_hasCustomVibrations && _vibrationEffectClass != null)
                {
                    AndroidJavaObject effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch { }
#elif UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
