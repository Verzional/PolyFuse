using System;
using System.Collections;
using System.Collections.Generic;
using PolyFuse.Core;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Interaction
{
    public class DraggablePiece : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private ShapeDefinition _shape;
        [SerializeField] private int _slotIndex;

        [Header("Tuning")]
        [SerializeField] private float _trayScale = 0.58f;
        [SerializeField] private float _dragScale = 1.0f;
        [SerializeField] private float _dragVerticalOffset = 0.95f; // Elevated comfortably above thumb
        [SerializeField] private float _snapDistanceThreshold = 1.35f;

        private HexBoard _board;
        private Camera _mainCamera;
        private Vector3 _slotRestPosition;
        private Vector3 _cachedCentroid;
        private bool _isDragging;
        private GridCoord? _currentHoverAnchor;
        private List<MeshRenderer> _meshRenderers = new List<MeshRenderer>();
        private Coroutine _returnAnim;
        private BoxCollider2D _collider;

        public ShapeDefinition Shape => _shape;
        public int SlotIndex => _slotIndex;
        public bool IsDragging => _isDragging;

        public event Action<DraggablePiece, GridCoord> OnPiecePlaced;

        public void Initialize(ShapeDefinition shape, int slotIndex, Vector3 slotRestPos, HexBoard board, float dealDelay = 0f)
        {
            _shape = shape;
            _slotIndex = slotIndex;
            _slotRestPosition = slotRestPos;
            _board = board;
            _mainCamera = Camera.main;
            _cachedCentroid = CalculateShapeCentroid();

            BuildVisuals();
            StartCoroutine(AnimateDealDrop(dealDelay));
        }

        private IEnumerator AnimateDealDrop(float dealDelay)
        {
            Vector3 startPos = _slotRestPosition + Vector3.up * 0.40f;
            Vector3 targetPos = _slotRestPosition;
            Vector3 startScale = Vector3.one * (_trayScale * 0.20f);
            Vector3 peakScale = Vector3.one * (_trayScale * 1.15f);
            Vector3 finalScale = Vector3.one * _trayScale;

            transform.position = startPos;
            transform.localScale = (dealDelay > 0f) ? Vector3.zero : startScale;

            if (dealDelay > 0f)
            {
                yield return new WaitForSeconds(dealDelay);
                transform.localScale = startScale;
            }

            float dur = 0.22f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                if (_isDragging) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);

                float posT = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.position = Vector3.Lerp(startPos, targetPos, posT);

                if (t < 0.65f)
                {
                    float scaleT = t / 0.65f;
                    transform.localScale = Vector3.Lerp(startScale, peakScale, scaleT);
                }
                else
                {
                    float scaleT = (t - 0.65f) / 0.35f;
                    transform.localScale = Vector3.Lerp(peakScale, finalScale, scaleT);
                }

                yield return null;
            }

            if (!_isDragging)
            {
                transform.position = targetPos;
                transform.localScale = finalScale;
            }
        }

        private void BuildVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            _meshRenderers.Clear();

            if (_shape == null || _shape.relativeOffsets == null) return;

            Vector3 centerOffset = CalculateShapeCentroid();

            for (int i = 0; i < _shape.relativeOffsets.Length; i++)
            {
                GridCoord offset = _shape.relativeOffsets[i];
                int offsetSum = Mathf.Abs(offset.r + offset.c);
                bool isOffsetEven = (offsetSum % 2 == 0);
                
                // Parity: even sum is Up
                bool isUp = _shape.anchorRequiresUp ? isOffsetEven : !isOffsetEven;

                GameObject unitObj = new GameObject($"Unit_{i}_R{offset.r}_C{offset.c}");
                unitObj.transform.SetParent(transform, false);

                Vector3 unitLocalPos = CalculateLocalUnitPos(offset.r, offset.c, isUp) - centerOffset;
                unitObj.transform.localPosition = unitLocalPos;

                MeshFilter mf = unitObj.AddComponent<MeshFilter>();
                MeshRenderer mr = unitObj.AddComponent<MeshRenderer>();

                mf.sharedMesh = TriangleMeshHelper.CreateTriangleMesh(isUp, 0.92f);
                mr.sharedMaterial = TriangleMeshHelper.GetDefaultMaterial();
                mr.sortingOrder = 10;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                mr.GetPropertyBlock(block);
                block.SetColor("_Color", _shape.defaultColor);
                block.SetColor("_BaseColor", _shape.defaultColor);
                mr.SetPropertyBlock(block);

                _meshRenderers.Add(mr);
            }

            _collider = GetComponent<BoxCollider2D>();
            if (_collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = new Vector2(3.0f, 3.0f);
            _collider.isTrigger = true;
        }

        private Vector3 CalculateLocalUnitPos(int dr, int dc, bool isUp)
        {
            float x = dc * TriangleMeshHelper.HalfWidth;
            float y = dr * TriangleMeshHelper.Height + (isUp ? (TriangleMeshHelper.Height / 3f) : (2f * TriangleMeshHelper.Height / 3f));
            return new Vector3(x, y, 0f);
        }

        private Vector3 CalculateShapeCentroid()
        {
            if (_shape == null || _shape.relativeOffsets == null || _shape.relativeOffsets.Length == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < _shape.relativeOffsets.Length; i++)
            {
                GridCoord offset = _shape.relativeOffsets[i];
                int offsetSum = Mathf.Abs(offset.r + offset.c);
                bool isOffsetEven = (offsetSum % 2 == 0);
                bool isUp = _shape.anchorRequiresUp ? isOffsetEven : !isOffsetEven;

                sum += CalculateLocalUnitPos(offset.r, offset.c, isUp);
            }
            return sum / _shape.relativeOffsets.Length;
        }

        public static DraggablePiece ActivePiece { get; private set; }
        private static readonly List<DraggablePiece> _allPieces = new List<DraggablePiece>();

        private void OnEnable()
        {
            if (!_allPieces.Contains(this))
            {
                _allPieces.Add(this);
            }
        }

        private void OnDestroy()
        {
            _allPieces.Remove(this);
            if (ActivePiece == this)
            {
                ActivePiece = null;
            }
        }

        private void Update()
        {
            // If Game Over or game is paused, cancel any active drag and ignore new drags
            if (Time.timeScale == 0f || (PolyFuse.Gameplay.GameManager.Instance != null && PolyFuse.Gameplay.GameManager.Instance.IsGameOver))
            {
                if (_isDragging)
                {
                    EndDragging();
                }
                return;
            }

            if (ActivePiece == null && !_isDragging && InputHelper.IsPointerDown())
            {
                Vector3 pointerWorld = GetPointerWorldPosition();
                float dist = Vector2.Distance(pointerWorld, transform.position);
                if (dist < 1.75f && IsClosestActivePiece(pointerWorld, dist))
                {
                    StartDragging();
                }
            }

            if (ActivePiece == this && _isDragging)
            {
                if (InputHelper.IsPointerHeld())
                {
                    Vector3 pointerWorld = GetPointerWorldPosition();
                    pointerWorld.y += _dragVerticalOffset;
                    transform.position = pointerWorld;
                    EvaluateHoverPlacement();
                }
                else if (InputHelper.IsPointerUp())
                {
                    EndDragging();
                }
            }
            else if (_isDragging && ActivePiece != this)
            {
                _isDragging = false;
                if (_board != null)
                {
                    _board.ClearGhostPreviews();
                    _board.ClearAnticipationGlow();
                }
                _returnAnim = StartCoroutine(ReturnToSlot());
            }
        }

        private bool IsClosestActivePiece(Vector3 pointerPos, float myDist)
        {
            for (int i = 0; i < _allPieces.Count; i++)
            {
                DraggablePiece other = _allPieces[i];
                if (other != null && other != this && other.gameObject.activeInHierarchy)
                {
                    float otherDist = Vector2.Distance(pointerPos, other.transform.position);
                    if (otherDist < myDist)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Vector3 GetPointerWorldPosition()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return transform.position;

            Vector2 screenPos = InputHelper.GetPointerScreenPosition();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_mainCamera.transform.position.z));
            worldPos.z = 0f;
            return worldPos;
        }

        private void StartDragging()
        {
            if (ActivePiece != null && ActivePiece != this) return;
            ActivePiece = this;

            if (_returnAnim != null) StopCoroutine(_returnAnim);
            _isDragging = true;
            SetDisabled(false); // Instantly restore 100% saturation and brightness on touch
            StartCoroutine(AnimateScale(_dragScale, 0.10f));
        }

        private void EndDragging()
        {
            if (ActivePiece == this)
            {
                ActivePiece = null;
            }
            _isDragging = false;
            if (_board != null)
            {
                _board.ClearGhostPreviews();
                _board.ClearAnticipationGlow();
            }

            if (_currentHoverAnchor.HasValue && _board != null && _board.CanPlaceShape(_shape, _currentHoverAnchor.Value))
            {
                GridCoord placedAnchor = _currentHoverAnchor.Value;
                _currentHoverAnchor = null;
                OnPiecePlaced?.Invoke(this, placedAnchor);
            }
            else
            {
                _currentHoverAnchor = null;
                _returnAnim = StartCoroutine(ReturnToSlot());
            }
        }

        private void EvaluateHoverPlacement()
        {
            if (_board == null) return;

            Vector3 anchorWorldPos = transform.position - _cachedCentroid;

            TriangleTile closestTile = null;
            float minDist = _snapDistanceThreshold;

            IReadOnlyList<TriangleTile> tiles = _board.TileList;
            for (int i = 0; i < tiles.Count; i++)
            {
                TriangleTile tile = tiles[i];
                if (tile == null || tile.IsPointingUp != _shape.anchorRequiresUp) continue;

                float dx = anchorWorldPos.x - tile.transform.position.x;
                float dy = anchorWorldPos.y - tile.transform.position.y;

                // Approaching from below (dy < 0) has generous upward reach
                float weightY = (dy < 0f) ? 0.50f : 1.30f;
                float dist = Mathf.Sqrt(dx * dx + (dy * dy) * weightY);

                if (dist < minDist)
                {
                    minDist = dist;
                    closestTile = tile;
                }
            }

            if (closestTile != null && _board.CanPlaceShape(_shape, closestTile.Coord))
            {
                // Magnetic snap pull smoothly locks piece over target slot
                if (minDist < 0.85f)
                {
                    Vector3 targetPos = closestTile.transform.position + _cachedCentroid;
                    transform.position = Vector3.Lerp(transform.position, targetPos, 0.40f);
                }

                _currentHoverAnchor = closestTile.Coord;
                _board.SetGhostPreview(_shape, closestTile.Coord);

                // Pre-Snap Line Clear Anticipation Glow
                List<GridCoord> previewLines = _board.GetCompletedLinesIfPlaced(_shape, closestTile.Coord);
                if (previewLines != null && previewLines.Count > 0)
                {
                    _board.SetAnticipationGlow(previewLines, _shape.defaultColor);
                }
                else
                {
                    _board.ClearAnticipationGlow();
                }
            }
            else
            {
                _currentHoverAnchor = null;
                _board.ClearGhostPreviews();
                _board.ClearAnticipationGlow();
            }
        }

        private void OnDisable()
        {
            _allPieces.Remove(this);
            if (ActivePiece == this)
            {
                ActivePiece = null;
            }

            if (_returnAnim != null)
            {
                StopCoroutine(_returnAnim);
                _returnAnim = null;
                transform.position = _slotRestPosition;
                transform.localScale = Vector3.one * _trayScale;
            }
        }

        private IEnumerator AnimateScale(float targetScale, float duration)
        {
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
                yield return null;
            }
            transform.localScale = endScale;
        }

        /// <summary>
        /// Elastic cubic overshoot easing (Out-Back) for juicy tactile spring return.
        /// </summary>
        private static float EaseOutBack(float t, float overshoot = 1.65f)
        {
            float t1 = t - 1f;
            return 1f + (overshoot + 1f) * t1 * t1 * t1 + overshoot * t1 * t1;
        }

        private IEnumerator ReturnToSlot()
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.one * _trayScale;
            float elapsed = 0f;
            float duration = 0.26f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Elastic spring overshoot on position
                float easePos = EaseOutBack(t, 1.65f);
                transform.position = Vector3.LerpUnclamped(startPos, _slotRestPosition, easePos);

                // Smooth cubic ease on scale return
                float easeScale = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, endScale, easeScale);

                yield return null;
            }

            transform.position = _slotRestPosition;
            transform.localScale = endScale;
            _returnAnim = null;
        }

        public void SetDisabled(bool disabled)
        {
            float alpha = disabled ? 0.38f : 1.0f;
            Color baseColor = disabled 
                ? Color.Lerp(_shape.defaultColor, new Color(0.30f, 0.35f, 0.45f), 0.65f) 
                : _shape.defaultColor;
            baseColor.a = alpha;

            for (int i = 0; i < _meshRenderers.Count; i++)
            {
                if (_meshRenderers[i] != null)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    _meshRenderers[i].GetPropertyBlock(block);
                    block.SetColor("_Color", baseColor);
                    block.SetColor("_BaseColor", baseColor);
                    _meshRenderers[i].SetPropertyBlock(block);
                }
            }
        }
    }
}
