using UnityEngine;

namespace PolyFuse.Core
{
    [ExecuteAlways]
    public class MobileResolutionAdapter : MonoBehaviour
    {
        [Header("Target Design Framing (Portrait)")]
        [SerializeField] private float _targetVisibleWorldWidth = 7.0f; // Width needed to frame board & tray comfortably
        [SerializeField] private float _minOrthographicSize = 6.2f;
        [SerializeField] private Vector3 _targetCameraCenter = new Vector3(0f, -0.75f, -10f);

        private Camera _cam;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            ApplyOrientationSettings();
            AdaptResolution();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                AdaptResolution();
            }
        }

        private void ApplyOrientationSettings()
        {
#if !UNITY_EDITOR
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
#endif
        }

        public void AdaptResolution()
        {
            if (_cam == null) _cam = GetComponent<Camera>() ?? Camera.main;
            if (_cam == null || !_cam.orthographic) return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (_lastWidth <= 0 || _lastHeight <= 0) return;

            float currentAspect = (float)_lastWidth / _lastHeight;

            // In orthographic 2D: visible half-width = orthographicSize * aspect
            // We want: visible half-width = _targetVisibleWorldWidth * 0.5f
            // Therefore: orthographicSize = (_targetVisibleWorldWidth * 0.5f) / aspect
            float requiredOrthoSize = (_targetVisibleWorldWidth * 0.5f) / currentAspect;

            _cam.orthographicSize = Mathf.Max(requiredOrthoSize, _minOrthographicSize);
            _cam.transform.position = _targetCameraCenter;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            AdaptResolution();
        }
#endif
    }
}
