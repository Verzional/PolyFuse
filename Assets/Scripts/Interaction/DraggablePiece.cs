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
        [SerializeField] private float _dragVerticalOffset = 1.4f;
        [SerializeField] private float _snapDistanceThreshold = 1.4f;

        private HexBoard _board;
        private Camera _mainCamera;
        private Vector3 _slotRestPosition;
        private bool _isDragging;
        private GridCoord? _currentHoverAnchor;
        private List<MeshRenderer> _meshRenderers = new List<MeshRenderer>();
        private Coroutine _returnAnim;
        private BoxCollider2D _collider;

        public ShapeDefinition Shape => _shape;
        public int SlotIndex => _slotIndex;
        public bool IsDragging => _isDragging;

        public event Action<DraggablePiece, GridCoord> OnPiecePlaced;

        public void Initialize(ShapeDefinition shape, int slotIndex, Vector3 slotRestPos, HexBoard board)
        {
            _shape = shape;
            _slotIndex = slotIndex;
            _slotRestPosition = slotRestPos;
            _board = board;
            _mainCamera = Camera.main;

            transform.position = slotRestPos;
            transform.localScale = Vector3.one * _trayScale;

            BuildVisuals();
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

        private void Update()
        {
            if (!_isDragging && InputHelper.IsPointerDown())
            {
                Vector3 pointerWorld = GetPointerWorldPosition();
                if (Vector2.Distance(pointerWorld, transform.position) < 1.6f)
                {
                    StartDragging();
                }
            }

            if (_isDragging)
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
            if (_returnAnim != null) StopCoroutine(_returnAnim);
            _isDragging = true;
            StartCoroutine(AnimateScale(_dragScale, 0.10f));
        }

        private void EndDragging()
        {
            _isDragging = false;

            if (_currentHoverAnchor.HasValue && _board.CanPlaceShape(_shape, _currentHoverAnchor.Value))
            {
                GridCoord placedAnchor = _currentHoverAnchor.Value;
                _board.ClearGhostPreviews();
                _currentHoverAnchor = null;
                OnPiecePlaced?.Invoke(this, placedAnchor);
            }
            else
            {
                _board.ClearGhostPreviews();
                _currentHoverAnchor = null;
                _returnAnim = StartCoroutine(ReturnToSlot());
            }
        }

        private void EvaluateHoverPlacement()
        {
            if (_board == null) return;

            Vector3 centroid = CalculateShapeCentroid();
            Vector3 anchorWorldPos = transform.position - centroid;

            TriangleTile closestTile = null;
            float minDist = _snapDistanceThreshold;

            foreach (var kvp in _board.Tiles)
            {
                TriangleTile tile = kvp.Value;
                if (tile.IsPointingUp != _shape.anchorRequiresUp) continue;

                float dist = Vector2.Distance(anchorWorldPos, tile.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestTile = tile;
                }
            }

            if (closestTile != null && _board.CanPlaceShape(_shape, closestTile.Coord))
            {
                _currentHoverAnchor = closestTile.Coord;
                _board.SetGhostPreview(_shape, closestTile.Coord);
            }
            else
            {
                _currentHoverAnchor = null;
                _board.ClearGhostPreviews();
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

        private IEnumerator ReturnToSlot()
        {
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = Vector3.one * _trayScale;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(startPos, _slotRestPosition, ease);
                transform.localScale = Vector3.Lerp(startScale, endScale, ease);
                yield return null;
            }

            transform.position = _slotRestPosition;
            transform.localScale = endScale;
            _returnAnim = null;
        }

        public void SetDisabled(bool disabled)
        {
            float alpha = disabled ? 0.35f : 1.0f;
            for (int i = 0; i < _meshRenderers.Count; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                _meshRenderers[i].GetPropertyBlock(block);
                Color c = _shape.defaultColor;
                c.a = alpha;
                block.SetColor("_Color", c);
                block.SetColor("_BaseColor", c);
                _meshRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
