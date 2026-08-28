using System.Collections.Generic;
using PolyFuse.Core;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Gameplay
{
    public class PieceSpawner : MonoBehaviour
    {
        [Header("Tuning")]
        [Range(0.2f, 0.8f)]
        [SerializeField] private float _crampedThreshold = 0.45f;

        private HexBoard _board;
        private List<List<GridCoord>> _allLinesCache;

        public void Initialize(HexBoard board)
        {
            _board = board;
            RebuildLineCache();
        }

        private void RebuildLineCache()
        {
            if (_board == null || _board.Tiles.Count == 0) return;

            Dictionary<int, List<GridCoord>> rows = new Dictionary<int, List<GridCoord>>();
            Dictionary<int, List<GridCoord>> slash = new Dictionary<int, List<GridCoord>>();
            Dictionary<int, List<GridCoord>> backslash = new Dictionary<int, List<GridCoord>>();

            foreach (var kvp in _board.Tiles)
            {
                GridCoord c = kvp.Key;

                if (!rows.TryGetValue(c.r, out var rList)) { rList = new List<GridCoord>(); rows[c.r] = rList; }
                rList.Add(c);

                int sKey = c.c - c.r - (c.IsPointingUp ? 0 : 1);
                if (!slash.TryGetValue(sKey, out var sList)) { sList = new List<GridCoord>(); slash[sKey] = sList; }
                sList.Add(c);

                int bsKey = c.r + c.c + (c.IsPointingUp ? 0 : 1);
                if (!backslash.TryGetValue(bsKey, out var bsList)) { bsList = new List<GridCoord>(); backslash[bsKey] = bsList; }
                bsList.Add(c);
            }

            _allLinesCache = new List<List<GridCoord>>();
            _allLinesCache.AddRange(rows.Values);

            foreach (var list in slash.Values)
            {
                if (list.Count >= 3) _allLinesCache.Add(list);
            }

            foreach (var list in backslash.Values)
            {
                if (list.Count >= 3) _allLinesCache.Add(list);
            }
        }

        public ShapeDefinition[] GenerateHandBatch()
        {
            if (_board == null || _board.Tiles.Count == 0)
            {
                return new ShapeDefinition[]
                {
                    ShapeCatalog.GetRandomWeightedShape(),
                    ShapeCatalog.GetRandomWeightedShape(),
                    ShapeCatalog.GetRandomWeightedShape()
                };
            }

            if (_allLinesCache == null || _allLinesCache.Count == 0)
            {
                RebuildLineCache();
            }

            int totalTiles = _board.TotalTileCount;
            int occupiedCount = 0;
            int emptyUp = 0;
            int emptyDown = 0;

            foreach (var kvp in _board.Tiles)
            {
                if (kvp.Value.IsOccupied)
                {
                    occupiedCount++;
                }
                else
                {
                    if (kvp.Key.IsPointingUp) emptyUp++;
                    else emptyDown++;
                }
            }

            float fillRatio = totalTiles > 0 ? ((float)occupiedCount / totalTiles) : 0f;
            VirtualBoard currentBoard = CreateVirtualBoard();

            // 1. If Board is Cramped (>= 45% full): Construct a guaranteed rescue sequence directly
            if (fillRatio >= _crampedThreshold)
            {
                ShapeDefinition[] rescueBatch = ConstructRescueSequence(currentBoard);
                if (rescueBatch != null && IsBatchFullySolvable(currentBoard, rescueBatch))
                {
                    Shuffle(rescueBatch);
                    return rescueBatch;
                }
            }

            // 2. Try generating standard weighted batches with Adaptive Parity Bias
            ShapeDefinition[] candidateBatch = new ShapeDefinition[3];

            for (int attempt = 0; attempt < 35; attempt++)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool? parityBias = null;
                    if (emptyDown > emptyUp + 1) parityBias = (Random.value < 0.70f) ? false : true;
                    else if (emptyUp > emptyDown + 1) parityBias = (Random.value < 0.70f) ? true : false;
                    else parityBias = (Random.value < 0.50f) ? true : false;

                    candidateBatch[i] = ShapeCatalog.GetRandomWeightedShape(parityBias);
                }

                if (fillRatio > 0.35f && candidateBatch[0] != null && candidateBatch[1] != null)
                {
                    bool isFirstHeavy = candidateBatch[0].category == ShapeCategory.Core || candidateBatch[0].category == ShapeCategory.Crown;
                    bool isSecondHeavy = candidateBatch[1].category == ShapeCategory.Core || candidateBatch[1].category == ShapeCategory.Crown;
                    if (isFirstHeavy && isSecondHeavy)
                    {
                        candidateBatch[1] = ShapeCatalog.Blades[Random.Range(0, ShapeCatalog.Blades.Count)];
                    }
                }

                if (IsBatchFullySolvable(currentBoard, candidateBatch))
                {
                    return candidateBatch;
                }
            }

            // 3. Fallback: Algorithmic 100% Guaranteed Rescue Batch Construction
            ShapeDefinition[] guaranteedBatch = ConstructRescueSequence(currentBoard);
            if (guaranteedBatch != null && IsBatchFullySolvable(currentBoard, guaranteedBatch))
            {
                Shuffle(guaranteedBatch);
                return guaranteedBatch;
            }

            // Absolute Emergency: 3 playable Shards/Blades with balanced parity
            candidateBatch[0] = FindAnyPlayableShape(currentBoard, false) ?? FindAnyPlayableShape(currentBoard, true) ?? ShapeCatalog.Shards[0];
            candidateBatch[1] = FindAnyPlayableShape(currentBoard, true) ?? FindAnyPlayableShape(currentBoard, false) ?? ShapeCatalog.Shards[1];
            candidateBatch[2] = FindAnyPlayableShape(currentBoard, null) ?? ShapeCatalog.Blades[0];
            Shuffle(candidateBatch);
            return candidateBatch;
        }

        private ShapeDefinition[] ConstructRescueSequence(VirtualBoard board)
        {
            if (board == null) return null;

            ShapeDefinition[] batch = new ShapeDefinition[3];

            // Piece 1: A Line Completer that clears a crowded line
            ShapeDefinition p1 = FindLineCompleter(board) ?? FindAnyPlayableShape(board, null) ?? ShapeCatalog.Shards[Random.Range(0, ShapeCatalog.Shards.Count)];
            batch[0] = p1;

            // Simulate placing Piece 1 and clearing
            VirtualBoard boardAfterP1 = SimulateBestPlacement(board, p1);

            // Piece 2: Fits on the cleared board (favor opposite parity for balance)
            bool? p2Parity = (p1 != null) ? !p1.anchorRequiresUp : (bool?)null;
            ShapeDefinition p2 = FindLineCompleter(boardAfterP1) ?? FindAnyPlayableShape(boardAfterP1, p2Parity) ?? ShapeCatalog.Shards[Random.Range(0, ShapeCatalog.Shards.Count)];
            batch[1] = p2;

            // Simulate placing Piece 2 and clearing
            VirtualBoard boardAfterP2 = SimulateBestPlacement(boardAfterP1, p2);

            // Piece 3: Guaranteed to fit on the resulting board
            ShapeDefinition p3 = FindAnyPlayableShape(boardAfterP2, null) ?? ShapeCatalog.Shards[Random.Range(0, ShapeCatalog.Shards.Count)];
            batch[2] = p3;

            return batch;
        }

        private VirtualBoard SimulateBestPlacement(VirtualBoard board, ShapeDefinition shape)
        {
            if (board == null || shape == null) return board;

            VirtualBoard bestNextBoard = null;
            int maxCleared = -1;

            foreach (var kvp in board.Tiles)
            {
                if (!kvp.Value && kvp.Key.IsPointingUp == shape.anchorRequiresUp)
                {
                    if (board.CanPlace(shape, kvp.Key))
                    {
                        VirtualBoard next = board.PlaceAndClear(shape, kvp.Key, _allLinesCache, out int cleared);
                        if (cleared > maxCleared)
                        {
                            maxCleared = cleared;
                            bestNextBoard = next;
                        }
                    }
                }
            }

            return bestNextBoard ?? board;
        }

        private ShapeDefinition FindLineCompleter(VirtualBoard board)
        {
            if (board == null || _allLinesCache == null) return null;

            int minMissing = 999;
            List<GridCoord> bestMissing = null;

            foreach (var line in _allLinesCache)
            {
                List<GridCoord> emptyCoords = new List<GridCoord>();
                for (int i = 0; i < line.Count; i++)
                {
                    if (!board.IsOccupied(line[i]))
                    {
                        emptyCoords.Add(line[i]);
                    }
                }

                if (emptyCoords.Count > 0 && emptyCoords.Count <= 4 && emptyCoords.Count < minMissing)
                {
                    minMissing = emptyCoords.Count;
                    bestMissing = emptyCoords;
                }
            }

            if (bestMissing == null || bestMissing.Count == 0) return null;

            // 1 Missing -> Return matching Up or Down Shard
            if (bestMissing.Count == 1)
            {
                bool isUp = bestMissing[0].IsPointingUp;
                return isUp ? ShapeCatalog.Shards[0] : ShapeCatalog.Shards[1];
            }

            // 2 Missing -> Check Blades with matching orientation
            if (bestMissing.Count == 2)
            {
                List<ShapeDefinition> blades = new List<ShapeDefinition>(ShapeCatalog.Blades);
                ShuffleList(blades);

                foreach (var blade in blades)
                {
                    if (board.CanPlace(blade, bestMissing[0]) || board.CanPlace(blade, bestMissing[1]))
                    {
                        return blade;
                    }
                }
                return bestMissing[0].IsPointingUp ? ShapeCatalog.Shards[0] : ShapeCatalog.Shards[1];
            }

            // 3 Missing -> Check Cleavers
            if (bestMissing.Count == 3)
            {
                List<ShapeDefinition> cleavers = new List<ShapeDefinition>(ShapeCatalog.Cleavers);
                ShuffleList(cleavers);

                foreach (var cleaver in cleavers)
                {
                    if (board.CanPlace(cleaver, bestMissing[0]))
                    {
                        return cleaver;
                    }
                }
            }

            // 4 Missing -> Check Chevrons
            if (bestMissing.Count == 4)
            {
                List<ShapeDefinition> chevrons = new List<ShapeDefinition>(ShapeCatalog.Chevrons);
                ShuffleList(chevrons);

                foreach (var chevron in chevrons)
                {
                    if (board.CanPlace(chevron, bestMissing[0]))
                    {
                        return chevron;
                    }
                }
            }

            return null;
        }

        public bool IsBatchFullySolvable(VirtualBoard board, ShapeDefinition[] batch)
        {
            if (board == null || batch == null || batch.Length < 3) return false;
            if (batch[0] == null || batch[1] == null || batch[2] == null) return false;

            int[][] permutations = new int[][]
            {
                new int[] { 0, 1, 2 },
                new int[] { 0, 2, 1 },
                new int[] { 1, 0, 2 },
                new int[] { 1, 2, 0 },
                new int[] { 2, 0, 1 },
                new int[] { 2, 1, 0 }
            };

            for (int p = 0; p < permutations.Length; p++)
            {
                int[] order = permutations[p];
                if (CanSolveSequenceRecursive(board, batch, order, 0))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanSolveSequenceRecursive(VirtualBoard board, ShapeDefinition[] batch, int[] order, int step)
        {
            if (board == null || batch == null || order == null) return false;
            if (step >= order.Length) return true;

            int pieceIndex = order[step];
            if (pieceIndex < 0 || pieceIndex >= batch.Length) return false;

            ShapeDefinition currentShape = batch[pieceIndex];
            if (currentShape == null) return false;

            foreach (var kvp in board.Tiles)
            {
                if (!kvp.Value && kvp.Key.IsPointingUp == currentShape.anchorRequiresUp)
                {
                    if (board.CanPlace(currentShape, kvp.Key))
                    {
                        VirtualBoard nextBoard = board.PlaceAndClear(currentShape, kvp.Key, _allLinesCache, out _);
                        if (CanSolveSequenceRecursive(nextBoard, batch, order, step + 1))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private ShapeDefinition FindAnyPlayableShape(VirtualBoard board, bool? preferredParity)
        {
            if (board == null) return null;

            List<ShapeDefinition> candidates = new List<ShapeDefinition>();

            foreach (var s in ShapeCatalog.AllShapes)
            {
                if (s != null && IsShapePlayable(board, s))
                {
                    candidates.Add(s);
                }
            }

            if (candidates.Count == 0) return null;

            if (preferredParity.HasValue)
            {
                List<ShapeDefinition> parityMatches = candidates.FindAll(s => s.anchorRequiresUp == preferredParity.Value);
                if (parityMatches.Count > 0)
                {
                    return parityMatches[Random.Range(0, parityMatches.Count)];
                }
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        public bool IsShapePlayable(ShapeDefinition shape)
        {
            if (shape == null || _board == null) return false;
            return IsShapePlayable(CreateVirtualBoard(), shape);
        }

        private bool IsShapePlayable(VirtualBoard board, ShapeDefinition shape)
        {
            if (shape == null || board == null) return false;

            foreach (var kvp in board.Tiles)
            {
                if (!kvp.Value && kvp.Key.IsPointingUp == shape.anchorRequiresUp)
                {
                    if (board.CanPlace(shape, kvp.Key)) return true;
                }
            }
            return false;
        }

        private VirtualBoard CreateVirtualBoard()
        {
            Dictionary<GridCoord, bool> state = new Dictionary<GridCoord, bool>(_board.Tiles.Count);
            foreach (var kvp in _board.Tiles)
            {
                state[kvp.Key] = kvp.Value.IsOccupied;
            }
            return new VirtualBoard(state);
        }

        private void Shuffle(ShapeDefinition[] array)
        {
            if (array == null) return;
            for (int i = 0; i < array.Length; i++)
            {
                int rnd = Random.Range(i, array.Length);
                var temp = array[i];
                array[i] = array[rnd];
                array[rnd] = temp;
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                int rnd = Random.Range(i, list.Count);
                var temp = list[i];
                list[i] = list[rnd];
                list[rnd] = temp;
            }
        }
    }

    public class VirtualBoard
    {
        public readonly Dictionary<GridCoord, bool> Tiles;

        public VirtualBoard(Dictionary<GridCoord, bool> tiles)
        {
            Tiles = tiles ?? new Dictionary<GridCoord, bool>();
        }

        public bool IsOccupied(GridCoord coord)
        {
            return Tiles.TryGetValue(coord, out bool occupied) && occupied;
        }

        public bool CanPlace(ShapeDefinition shape, GridCoord anchor)
        {
            if (shape == null || shape.relativeOffsets == null || shape.relativeOffsets.Length == 0) return false;

            for (int i = 0; i < shape.relativeOffsets.Length; i++)
            {
                GridCoord target = anchor + shape.relativeOffsets[i];
                if (!Tiles.TryGetValue(target, out bool occ) || occ)
                {
                    return false;
                }
            }
            return true;
        }

        public VirtualBoard PlaceAndClear(ShapeDefinition shape, GridCoord anchor, List<List<GridCoord>> allLines, out int clearedTilesCount)
        {
            Dictionary<GridCoord, bool> newTiles = new Dictionary<GridCoord, bool>(Tiles);

            // Mark placed
            if (shape != null && shape.relativeOffsets != null)
            {
                for (int i = 0; i < shape.relativeOffsets.Length; i++)
                {
                    newTiles[anchor + shape.relativeOffsets[i]] = true;
                }
            }

            // Check line clears
            HashSet<GridCoord> toClear = new HashSet<GridCoord>();
            if (allLines != null)
            {
                for (int l = 0; l < allLines.Count; l++)
                {
                    List<GridCoord> line = allLines[l];
                    if (line == null) continue;

                    bool full = true;
                    for (int i = 0; i < line.Count; i++)
                    {
                        if (!newTiles.TryGetValue(line[i], out bool isOcc) || !isOcc)
                        {
                            full = false;
                            break;
                        }
                    }

                    if (full)
                    {
                        for (int i = 0; i < line.Count; i++) toClear.Add(line[i]);
                    }
                }
            }

            clearedTilesCount = toClear.Count;
            foreach (var coord in toClear)
            {
                newTiles[coord] = false;
            }

            return new VirtualBoard(newTiles);
        }
    }
}
