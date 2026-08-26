using System;
using System.Collections;
using PolyFuse.Core;
using PolyFuse.Juice;
using UnityEngine;

namespace PolyFuse.Grid
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TriangleTile : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private int _row;
        [SerializeField] private int _col;
        [SerializeField] private bool _isOccupied;

        [Header("Visuals")]
        [SerializeField] private Color _emptyColor = new Color(0.14f, 0.16f, 0.22f, 1.0f);
        private Color _currentColor;
        private Color _occupiedColor;
        private bool _isAnticipationGlow;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propBlock;
        private PolygonCollider2D _collider;
        private Coroutine _animCoroutine;
        private Coroutine _glowCoroutine;

        public GridCoord Coord => new GridCoord(_row, _col);
        public int Row => _row;
        public int Col => _col;
        public bool IsOccupied => _isOccupied;
        public bool IsPointingUp => Coord.IsPointingUp;
        public Color OccupiedColor => _occupiedColor;

        public void Initialize(int row, int col)
        {
            _row = row;
            _col = col;
            _isOccupied = false;

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();

            _meshFilter.sharedMesh = TriangleMeshHelper.CreateTriangleMesh(IsPointingUp, 0.93f);
            _meshRenderer.sharedMaterial = TriangleMeshHelper.GetDefaultMaterial();
            _meshRenderer.sortingOrder = 0;

            _collider = GetComponent<PolygonCollider2D>();
            if (_collider == null)
            {
                _collider = gameObject.AddComponent<PolygonCollider2D>();
            }
            UpdateColliderPoints();

            SetColor(_emptyColor);
        }

        private void UpdateColliderPoints()
        {
            float h = TriangleMeshHelper.Height;
            float hw = TriangleMeshHelper.HalfWidth;
            Vector2[] points = new Vector2[3];

            if (IsPointingUp)
            {
                points[0] = new Vector2(0f, 2f * h / 3f);
                points[1] = new Vector2(hw, -h / 3f);
                points[2] = new Vector2(-hw, -h / 3f);
            }
            else
            {
                points[0] = new Vector2(0f, -2f * h / 3f);
                points[1] = new Vector2(-hw, h / 3f);
                points[2] = new Vector2(hw, h / 3f);
            }
            _collider.points = points;
        }

        public void SetOccupied(bool occupied, Color color)
        {
            _isOccupied = occupied;
            _occupiedColor = color;
            ClearAnticipationGlow();

            if (occupied)
            {
                SetColor(color);
                PlayPlacementPop();
            }
            else
            {
                SetColor(_emptyColor);
            }
        }

        public void SetGhostPreview(bool active, Color color)
        {
            if (_isOccupied || _isAnticipationGlow) return;

            if (active)
            {
                Color previewColor = new Color(color.r, color.g, color.b, 0.45f);
                SetColor(previewColor);
            }
            else
            {
                SetColor(_emptyColor);
            }
        }

        public void SetAnticipationGlow(bool active, Color shapeColor)
        {
            _isAnticipationGlow = active;

            if (active)
            {
                if (_glowCoroutine != null) StopCoroutine(_glowCoroutine);
                _glowCoroutine = StartCoroutine(AnimateAnticipationPulse(shapeColor));
            }
            else
            {
                ClearAnticipationGlow();
            }
        }

        public void ClearAnticipationGlow()
        {
            _isAnticipationGlow = false;
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
            SetColor(_isOccupied ? _occupiedColor : _emptyColor);
            transform.localScale = Vector3.one;
        }

        private IEnumerator AnimateAnticipationPulse(Color shapeColor)
        {
            Color baseColor = _isOccupied ? _occupiedColor : shapeColor;
            Color glowColor = Color.white * 1.6f;

            while (_isAnticipationGlow)
            {
                float t = (Mathf.Sin(Time.unscaledTime * 14f) + 1f) * 0.5f;
                SetColor(Color.Lerp(baseColor, glowColor, t * 0.75f));
                float s = Mathf.Lerp(1.0f, 1.08f, t);
                transform.localScale = Vector3.one * s;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        public void SetColor(Color c)
        {
            _currentColor = c;
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _meshRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", c);
            _propBlock.SetColor("_BaseColor", c);
            _meshRenderer.SetPropertyBlock(_propBlock);
        }

        private void OnDisable()
        {
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            if (_glowCoroutine != null)
            {
                StopCoroutine(_glowCoroutine);
                _glowCoroutine = null;
            }
            transform.localScale = Vector3.one;
        }

        public void PlayPlacementPop()
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimatePop());
        }

        private IEnumerator AnimatePop()
        {
            Vector3 origScale = Vector3.one;
            float elapsed = 0f;
            float dur = 0.22f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);

                float scaleFactor;
                if (t < 0.35f)
                {
                    // Elastic Punch: 1.0 -> 1.18x
                    float subT = t / 0.35f;
                    float ease = Mathf.Sin(subT * Mathf.PI * 0.5f);
                    scaleFactor = Mathf.Lerp(1.0f, 1.18f, ease);
                }
                else if (t < 0.70f)
                {
                    // Elastic Squash: 1.18x -> 0.92x
                    float subT = (t - 0.35f) / 0.35f;
                    float ease = 0.5f * (1f - Mathf.Cos(subT * Mathf.PI));
                    scaleFactor = Mathf.Lerp(1.18f, 0.92f, ease);
                }
                else
                {
                    // Elastic Settle: 0.92x -> 1.0x
                    float subT = (t - 0.70f) / 0.30f;
                    float ease = Mathf.Sin(subT * Mathf.PI * 0.5f);
                    scaleFactor = Mathf.Lerp(0.92f, 1.0f, ease);
                }

                transform.localScale = new Vector3(origScale.x * scaleFactor, origScale.y * scaleFactor, origScale.z);
                yield return null;
            }

            transform.localScale = origScale;
            _animCoroutine = null;
        }

        public void PlayClearFlash(Action onComplete)
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateClearFlash(onComplete));
        }

        private IEnumerator AnimateClearFlash(Action onComplete)
        {
            Color burstColor = _occupiedColor != Color.clear ? _occupiedColor : Color.cyan;
            if (ProceduralParticleManager.Instance != null)
            {
                ProceduralParticleManager.Instance.SpawnTileShatter(transform.position, burstColor, IsPointingUp);
            }

            Color startColor = Color.white * 1.8f;
            Color targetColor = _emptyColor;
            Vector3 origScale = Vector3.one;
            float elapsed = 0f;
            float dur = 0.22f;

            _isOccupied = false;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                SetColor(Color.Lerp(startColor, targetColor, t * t));
                transform.localScale = Vector3.Lerp(origScale, Vector3.zero, Mathf.Sin(t * Mathf.PI * 0.5f) * 0.35f);
                yield return null;
            }

            transform.localScale = origScale;
            SetColor(_emptyColor);
            _animCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
