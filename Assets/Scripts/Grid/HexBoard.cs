using System.Collections.Generic;
using PolyFuse.Core;
using UnityEngine;

namespace PolyFuse.Grid
{
    public class HexBoard : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private Transform _tilesParent;

        private readonly Dictionary<GridCoord, TriangleTile> _tiles = new Dictionary<GridCoord, TriangleTile>();
        private readonly List<GridCoord> _currentGhostCoords = new List<GridCoord>();

        public IReadOnlyDictionary<GridCoord, TriangleTile> Tiles => _tiles;
        public int TotalTileCount => _tiles.Count;

        private void Awake()
        {
            if (_tiles.Count == 0)
            {
                GenerateBoard();
            }
        }

        public void GenerateBoard()
        {
            if (_tilesParent == null)
            {
                GameObject parentObj = new GameObject("TilesContainer");
                parentObj.transform.SetParent(transform, false);
                _tilesParent = parentObj.transform;
            }
            else
            {
                for (int i = _tilesParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(_tilesParent.GetChild(i).gameObject);
                }
            }

            _tiles.Clear();

            // Smooth Regular Hexagon Layout (112 tiles):
            // Row 0 & 7: 11 tiles (c from -5 to 5)
            // Row 1 & 6: 13 tiles (c from -6 to 6)
            // Row 2 & 5: 15 tiles (c from -7 to 7)
            // Row 3 & 4: 17 tiles (c from -8 to 8)
            int[] halfWidths = new int[] { 3, 4, 5, 6, 6, 5, 4, 3 };

            for (int r = 0; r < 8; r++)
            {
                int hw = halfWidths[r];
                for (int c = -hw; c <= hw; c++)
                {
                    GridCoord coord = new GridCoord(r, c);
                    CreateTile(coord);
                }
            }

            Debug.Log($"[HexBoard] Symmetrical Smooth Hexagon Board generated with {_tiles.Count} tiles.");
        }

        private void CreateTile(GridCoord coord)
        {
            GameObject tileObj = new GameObject($"Tile_R{coord.r}_C{coord.c}");
            tileObj.transform.SetParent(_tilesParent, false);
            tileObj.transform.localPosition = TriangleMeshHelper.GridToWorldPosition(coord.r, coord.c);

            TriangleTile tile = tileObj.AddComponent<TriangleTile>();
            tile.Initialize(coord.r, coord.c);

            _tiles[coord] = tile;
        }

        public bool IsValidCoord(GridCoord coord)
        {
            return _tiles.ContainsKey(coord);
        }

        public TriangleTile GetTile(GridCoord coord)
        {
            _tiles.TryGetValue(coord, out TriangleTile tile);
            return tile;
        }

        public bool CanPlaceShape(ShapeDefinition shape, GridCoord anchor)
        {
            if (shape == null || shape.relativeOffsets == null || shape.relativeOffsets.Length == 0)
                return false;

            if (anchor.IsPointingUp != shape.anchorRequiresUp)
                return false;

            for (int i = 0; i < shape.relativeOffsets.Length; i++)
            {
                GridCoord targetCoord = anchor + shape.relativeOffsets[i];
                if (!IsValidCoord(targetCoord))
                    return false;

                TriangleTile tile = _tiles[targetCoord];
                if (tile.IsOccupied)
                    return false;
            }

            return true;
        }

        public void PlaceShape(ShapeDefinition shape, GridCoord anchor)
        {
            if (!CanPlaceShape(shape, anchor))
                return;

            ClearGhostPreviews();

            for (int i = 0; i < shape.relativeOffsets.Length; i++)
            {
                GridCoord targetCoord = anchor + shape.relativeOffsets[i];
                TriangleTile tile = _tiles[targetCoord];
                tile.SetOccupied(true, shape.defaultColor);
            }
        }

        public void SetGhostPreview(ShapeDefinition shape, GridCoord anchor)
        {
            ClearGhostPreviews();

            if (shape == null || !CanPlaceShape(shape, anchor))
                return;

            for (int i = 0; i < shape.relativeOffsets.Length; i++)
            {
                GridCoord targetCoord = anchor + shape.relativeOffsets[i];
                if (_tiles.TryGetValue(targetCoord, out TriangleTile tile))
                {
                    tile.SetGhostPreview(true, shape.defaultColor);
                    _currentGhostCoords.Add(targetCoord);
                }
            }
        }

        public void ClearGhostPreviews()
        {
            for (int i = 0; i < _currentGhostCoords.Count; i++)
            {
                if (_tiles.TryGetValue(_currentGhostCoords[i], out TriangleTile tile))
                {
                    tile.SetGhostPreview(false, Color.white);
                }
            }
            _currentGhostCoords.Clear();
        }

        public void ResetBoard()
        {
            ClearGhostPreviews();
            foreach (var kvp in _tiles)
            {
                kvp.Value.SetOccupied(false, Color.white);
            }
        }
    }
}
