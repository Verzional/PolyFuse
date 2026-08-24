using System;
using System.Collections;
using PolyFuse.Core;
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

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propBlock;
        private PolygonCollider2D _collider;
        private Coroutine _animCoroutine;

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

            // Procedural mesh
            _meshFilter.sharedMesh = TriangleMeshHelper.CreateTriangleMesh(IsPointingUp, 0.93f);
            _meshRenderer.sharedMaterial = TriangleMeshHelper.GetDefaultMaterial();
            _meshRenderer.sortingOrder = 0;

            // Setup Collider for raycasting
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
            if (_isOccupied) return;

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

        public void PlayPlacementPop()
        {
            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimatePop());
        }

        private IEnumerator AnimatePop()
        {
            Vector3 origScale = Vector3.one;
            Vector3 popScale = new Vector3(1.22f, 1.22f, 1f);
            float elapsed = 0f;
            float dur = 0.16f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                float s = Mathf.Sin(t * Mathf.PI);
                transform.localScale = Vector3.Lerp(origScale, popScale, s);
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
            Color startColor = Color.white * 1.5f;
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
