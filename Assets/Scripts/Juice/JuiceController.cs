using System.Collections;
using UnityEngine;

namespace PolyFuse.Juice
{
    public class JuiceController : MonoBehaviour
    {
        [Header("Screen Shake")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private float _shakeIntensityLine = 0.12f;
        [SerializeField] private float _shakeIntensityCore = 0.28f;
        [SerializeField] private float _shakeDuration = 0.18f;

        [Header("Hit-Stop")]
        [SerializeField] private float _hitStopDuration = 0.06f;

        private Vector3 _originalCameraPos;
        private Coroutine _shakeCoroutine;
        private Coroutine _hitStopCoroutine;

        private void Awake()
        {
            EnsureCameraRef();
        }

        private void EnsureCameraRef()
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
                _originalCameraPos = _cameraTransform.position;
            }
            else if (_cameraTransform != null && _originalCameraPos == Vector3.zero)
            {
                _originalCameraPos = _cameraTransform.position;
            }
        }

        private void OnDisable()
        {
            if (_hitStopCoroutine != null)
            {
                Time.timeScale = 1f;
                _hitStopCoroutine = null;
            }
            if (_cameraTransform != null && _shakeCoroutine != null)
            {
                _cameraTransform.position = _originalCameraPos;
                _shakeCoroutine = null;
            }
        }

        /// <summary>
        /// Triggers hit-stop freeze (Time.timeScale = 0.0f) using unscaled time.
        /// </summary>
        public void TriggerHitStop(float duration = -1f)
        {
            float hitDuration = duration > 0f ? duration : _hitStopDuration;
            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                Time.timeScale = 1f;
            }
            _hitStopCoroutine = StartCoroutine(DoHitStop(hitDuration));
        }

        /// <summary>
        /// Multi-line clears (>= 2 lines) scale hit-stop freeze between 0.05s and 0.08s.
        /// </summary>
        public void TriggerMultiLineHitStop(int lineCount)
        {
            if (lineCount < 2) return;
            float duration = Mathf.Clamp(0.05f + (lineCount - 2) * 0.015f, 0.05f, 0.08f);
            TriggerHitStop(duration);
        }

        private IEnumerator DoHitStop(float duration)
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            _hitStopCoroutine = null;
        }

        public void TriggerLineClearShake()
        {
            TriggerShake(_shakeIntensityLine);
        }

        public void TriggerMultiLineClearShake(int lineCount)
        {
            float bonus = Mathf.Max(0, lineCount - 1) * 0.06f;
            TriggerShake(_shakeIntensityLine + bonus);
        }

        public void TriggerHexCoreShake()
        {
            TriggerShake(_shakeIntensityCore);
        }

        public void TriggerShake(float intensity)
        {
            EnsureCameraRef();
            if (_cameraTransform == null) return;
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _cameraTransform.position = _originalCameraPos;
            }
            _shakeCoroutine = StartCoroutine(DoShake(intensity));
        }

        private IEnumerator DoShake(float intensity)
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _shakeDuration);
                // Punchy quadratic decay curve for tactile physical impact
                float damp = (1f - t) * (1f - t);
                Vector2 offset = Random.insideUnitCircle * intensity * damp;
                _cameraTransform.position = _originalCameraPos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            _cameraTransform.position = _originalCameraPos;
            _shakeCoroutine = null;
        }
    }
}
