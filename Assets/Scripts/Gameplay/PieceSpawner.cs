using System.Collections.Generic;
using PolyFuse.Core;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Gameplay
{
    public class PieceSpawner : MonoBehaviour
    {
        [SerializeField] private HexBoard _board;

        public void Initialize(HexBoard board)
        {
            _board = board;
        }

        public ShapeDefinition[] GenerateHandBatch()
        {
            ShapeDefinition[] batch = new ShapeDefinition[3];
            int maxAttempts = 20;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                for (int i = 0; i < 3; i++)
                {
                    batch[i] = ShapeCatalog.GetRandomWeightedShape();
                }

                if (IsAnyPiecePlayable(batch))
                {
                    return batch;
                }
            }

            batch[0] = FindAnyPlayableShape() ?? ShapeCatalog.Shards[0];
            batch[1] = ShapeCatalog.GetRandomWeightedShape();
            batch[2] = ShapeCatalog.GetRandomWeightedShape();

            return batch;
        }

        public bool IsAnyPiecePlayable(IReadOnlyList<ShapeDefinition> shapes)
        {
            if (shapes == null || _board == null) return false;

            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i] != null && IsShapePlayable(shapes[i]))
                    return true;
            }
            return false;
        }

        public bool IsShapePlayable(ShapeDefinition shape)
        {
            if (shape == null || _board == null) return false;

            foreach (var kvp in _board.Tiles)
            {
                if (!kvp.Value.IsOccupied && kvp.Key.IsPointingUp == shape.anchorRequiresUp)
                {
                    if (_board.CanPlaceShape(shape, kvp.Key))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ShapeDefinition FindAnyPlayableShape()
        {
            for (int i = 0; i < ShapeCatalog.Shards.Count; i++)
            {
                if (IsShapePlayable(ShapeCatalog.Shards[i]))
                    return ShapeCatalog.Shards[i];
            }

            for (int i = 0; i < ShapeCatalog.Blades.Count; i++)
            {
                if (IsShapePlayable(ShapeCatalog.Blades[i]))
                    return ShapeCatalog.Blades[i];
            }

            for (int i = 0; i < ShapeCatalog.Cleavers.Count; i++)
            {
                if (IsShapePlayable(ShapeCatalog.Cleavers[i]))
                    return ShapeCatalog.Cleavers[i];
            }

            for (int i = 0; i < ShapeCatalog.Cores.Count; i++)
            {
                if (IsShapePlayable(ShapeCatalog.Cores[i]))
                    return ShapeCatalog.Cores[i];
            }

            return null;
        }
    }
}
