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
        [SerializeField] private float _hitStopDuration = 0.05f;

        private Vector3 _originalCameraPos;
        private Coroutine _shakeCoroutine;
        private Coroutine _hitStopCoroutine;

        private void Awake()
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
            if (_cameraTransform != null)
            {
                _originalCameraPos = _cameraTransform.position;
            }
        }

        public void TriggerHitStop()
        {
            if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = StartCoroutine(DoHitStop());
        }

        private IEnumerator DoHitStop()
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(_hitStopDuration);
            Time.timeScale = 1f;
            _hitStopCoroutine = null;
        }

        public void TriggerLineClearShake()
        {
            TriggerShake(_shakeIntensityLine);
        }

        public void TriggerHexCoreShake()
        {
            TriggerShake(_shakeIntensityCore);
        }

        public void TriggerShake(float intensity)
        {
            if (_cameraTransform == null) return;
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(DoShake(intensity));
        }

        private IEnumerator DoShake(float intensity)
        {
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - (elapsed / _shakeDuration);
                Vector2 offset = Random.insideUnitCircle * intensity * damp;
                _cameraTransform.position = _originalCameraPos + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }
            _cameraTransform.position = _originalCameraPos;
            _shakeCoroutine = null;
        }
    }
}
