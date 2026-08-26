using System.Collections.Generic;
using PolyFuse.Core;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Gameplay
{
    public struct ClearEvaluationResult
    {
        public int horizontalLines;
        public int slashLines;
        public int backslashLines;
        public HashSet<GridCoord> tilesToClear;

        public int TotalLines => horizontalLines + slashLines + backslashLines;
        public bool HasAnyClear => TotalLines > 0;
        public int TotalTilesCount => tilesToClear != null ? tilesToClear.Count : 0;
    }

    public class DualClearEvaluator
    {
        private readonly HexBoard _board;

        private readonly Dictionary<int, List<GridCoord>> _horizontalGroups = new Dictionary<int, List<GridCoord>>();
        private readonly Dictionary<int, List<GridCoord>> _slashGroups = new Dictionary<int, List<GridCoord>>();
        private readonly Dictionary<int, List<GridCoord>> _backslashGroups = new Dictionary<int, List<GridCoord>>();

        public DualClearEvaluator(HexBoard board)
        {
            _board = board;
            PrecomputeGroups();
        }

        private void PrecomputeGroups()
        {
            _horizontalGroups.Clear();
            _slashGroups.Clear();
            _backslashGroups.Clear();

            foreach (var kvp in _board.Tiles)
            {
                GridCoord coord = kvp.Key;

                // 1. Horizontal group (Axis 0: 0°)
                if (!_horizontalGroups.TryGetValue(coord.r, out var hList))
                {
                    hList = new List<GridCoord>();
                    _horizontalGroups[coord.r] = hList;
                }
                hList.Add(coord);

                // 2. Slash (+60°) group (Axis 1)
                int slashKey = coord.c - coord.r - (coord.IsPointingUp ? 0 : 1);
                if (!_slashGroups.TryGetValue(slashKey, out var sList))
                {
                    sList = new List<GridCoord>();
                    _slashGroups[slashKey] = sList;
                }
                sList.Add(coord);

                // 3. Backslash (120°) group (Axis 2)
                int backslashKey = coord.r + coord.c + (coord.IsPointingUp ? 0 : 1);
                if (!_backslashGroups.TryGetValue(backslashKey, out var bsList))
                {
                    bsList = new List<GridCoord>();
                    _backslashGroups[backslashKey] = bsList;
                }
                bsList.Add(coord);
            }
        }

        public ClearEvaluationResult EvaluateBoard()
        {
            ClearEvaluationResult result = new ClearEvaluationResult
            {
                tilesToClear = new HashSet<GridCoord>()
            };

            // 1. Horizontal Lines (0°)
            foreach (var kvp in _horizontalGroups)
            {
                if (IsGroupFullyOccupied(kvp.Value))
                {
                    result.horizontalLines++;
                    foreach (var coord in kvp.Value) result.tilesToClear.Add(coord);
                }
            }

            // 2. Slash Lines (+60°)
            foreach (var kvp in _slashGroups)
            {
                if (kvp.Value.Count >= 3 && IsGroupFullyOccupied(kvp.Value))
                {
                    result.slashLines++;
                    foreach (var coord in kvp.Value) result.tilesToClear.Add(coord);
                }
            }

            // 3. Backslash Lines (120°)
            foreach (var kvp in _backslashGroups)
            {
                if (kvp.Value.Count >= 3 && IsGroupFullyOccupied(kvp.Value))
                {
                    result.backslashLines++;
                    foreach (var coord in kvp.Value) result.tilesToClear.Add(coord);
                }
            }

            return result;
        }

        private bool IsGroupFullyOccupied(IList<GridCoord> group)
        {
            for (int i = 0; i < group.Count; i++)
            {
                TriangleTile tile = _board.GetTile(group[i]);
                if (tile == null || !tile.IsOccupied)
                    return false;
            }
            return true;
        }
    }
}
