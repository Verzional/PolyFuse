using System.Collections.Generic;
using PolyFuse.Core;
using UnityEngine;

namespace PolyFuse.Grid
{
    public class HexBoard : MonoBehaviour
    {
        [Header("Board Settings")]
        [SerializeField] private int _radius = 3;
        [SerializeField] private Transform _tilesParent;

        private readonly Dictionary<GridCoord, TriangleTile> _tiles = new Dictionary<GridCoord, TriangleTile>();
        private readonly List<GridCoord> _currentGhostCoords = new List<GridCoord>();

        public int Radius => _radius;
        public IReadOnlyDictionary<GridCoord, TriangleTile> Tiles => _tiles;
        public int TotalTileCount => _tiles.Count;

        public int OccupiedTileCount
        {
            get
            {
                int count = 0;
                foreach (var kvp in _tiles)
                {
                    if (kvp.Value != null && kvp.Value.IsOccupied) count++;
                }
                return count;
            }
        }

        public float BoardFillRatio => TotalTileCount > 0 ? ((float)OccupiedTileCount / TotalTileCount) : 0f;

        private void Awake()
        {
            if (_tiles.Count == 0)
            {
                GenerateBoard();
            }
        }

        public void SetRadius(int newRadius)
        {
            _radius = Mathf.Max(2, newRadius);
            GenerateBoard();
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

            // Symmetrical Smooth Hexagon Layout parameterized by Radius (Default: 3):
            // For Radius = 3: 6 rows (r = 0..5), half-widths [3, 4, 5, 5, 4, 3]
            // Row counts: 7, 9, 11, 11, 9, 7 (Total = 54 tiles)
            int totalRows = _radius * 2;

            for (int r = 0; r < totalRows; r++)
            {
                int distFromCenter = Mathf.Abs(r - _radius + (r < _radius ? 1 : 0));
                int hw = _radius + (_radius - 1 - distFromCenter);

                for (int c = -hw; c <= hw; c++)
                {
                    GridCoord coord = new GridCoord(r, c);
                    CreateTile(coord);
                }
            }

            Debug.Log($"[HexBoard] Symmetrical Hexagon Board generated with Radius {_radius} ({_tiles.Count} tiles).");
        }

        private void CreateTile(GridCoord coord)
        {
            GameObject tileObj = new GameObject($"Tile_R{coord.r}_C{coord.c}");
            tileObj.transform.SetParent(_tilesParent, false);
            tileObj.transform.localPosition = TriangleMeshHelper.GridToWorldPosition(coord.r, coord.c, _radius);

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

        private readonly List<GridCoord> _currentAnticipationCoords = new List<GridCoord>();

        public List<GridCoord> GetCompletedLinesIfPlaced(ShapeDefinition shape, GridCoord anchor)
        {
            List<GridCoord> completedTiles = new List<GridCoord>();
            if (shape == null || !CanPlaceShape(shape, anchor)) return completedTiles;

            // Collect temporary virtual occupied set
            HashSet<GridCoord> virtualOccupied = new HashSet<GridCoord>();
            foreach (var kvp in _tiles)
            {
                if (kvp.Value.IsOccupied) virtualOccupied.Add(kvp.Key);
            }
            for (int i = 0; i < shape.relativeOffsets.Length; i++)
            {
                virtualOccupied.Add(anchor + shape.relativeOffsets[i]);
            }

            // Check Horizontal lines
            Dictionary<int, List<GridCoord>> rows = new Dictionary<int, List<GridCoord>>();
            Dictionary<int, List<GridCoord>> slash = new Dictionary<int, List<GridCoord>>();
            Dictionary<int, List<GridCoord>> backslash = new Dictionary<int, List<GridCoord>>();

            foreach (var coord in _tiles.Keys)
            {
                if (!rows.TryGetValue(coord.r, out var rList)) { rList = new List<GridCoord>(); rows[coord.r] = rList; }
                rList.Add(coord);

                int sKey = coord.c - coord.r - (coord.IsPointingUp ? 0 : 1);
                if (!slash.TryGetValue(sKey, out var sList)) { sList = new List<GridCoord>(); slash[sKey] = sList; }
                sList.Add(coord);

                int bsKey = coord.r + coord.c + (coord.IsPointingUp ? 0 : 1);
                if (!backslash.TryGetValue(bsKey, out var bsList)) { bsList = new List<GridCoord>(); backslash[bsKey] = bsList; }
                bsList.Add(coord);
            }

            List<List<GridCoord>> allLines = new List<List<GridCoord>>();
            allLines.AddRange(rows.Values);
            foreach (var l in slash.Values) if (l.Count >= 3) allLines.Add(l);
            foreach (var l in backslash.Values) if (l.Count >= 3) allLines.Add(l);

            HashSet<GridCoord> uniqueCompleted = new HashSet<GridCoord>();
            foreach (var line in allLines)
            {
                bool full = true;
                for (int i = 0; i < line.Count; i++)
                {
                    if (!virtualOccupied.Contains(line[i]))
                    {
                        full = false;
                        break;
                    }
                }
                if (full)
                {
                    for (int i = 0; i < line.Count; i++) uniqueCompleted.Add(line[i]);
                }
            }

            completedTiles.AddRange(uniqueCompleted);
            return completedTiles;
        }

        public void SetAnticipationGlow(IEnumerable<GridCoord> coords, Color color)
        {
            ClearAnticipationGlow();
            if (coords == null) return;

            foreach (var coord in coords)
            {
                if (_tiles.TryGetValue(coord, out TriangleTile tile))
                {
                    tile.SetAnticipationGlow(true, color);
                    _currentAnticipationCoords.Add(coord);
                }
            }
        }

        public void ClearAnticipationGlow()
        {
            for (int i = 0; i < _currentAnticipationCoords.Count; i++)
            {
                if (_tiles.TryGetValue(_currentAnticipationCoords[i], out TriangleTile tile))
                {
                    tile.ClearAnticipationGlow();
                }
            }
            _currentAnticipationCoords.Clear();
        }

        public void ResetBoard()
        {
            ClearGhostPreviews();
            ClearAnticipationGlow();
            foreach (var kvp in _tiles)
            {
                kvp.Value.SetOccupied(false, Color.white);
            }
        }
    }
}
