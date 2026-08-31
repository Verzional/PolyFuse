using System;
using System.Collections;
using System.Collections.Generic;
using PolyFuse.Core;
using PolyFuse.Gameplay;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Interaction
{
    public class HandTrayManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HexBoard _board;
        [SerializeField] private PieceSpawner _spawner;

        [Header("Layout Settings")]
        [SerializeField] private Vector3[] _slotPositions = new Vector3[]
        {
            new Vector3(-2.5f, -4.6f, 0f),
            new Vector3(0.0f, -4.6f, 0f),
            new Vector3(2.5f, -4.6f, 0f)
        };

        private readonly DraggablePiece[] _activePieces = new DraggablePiece[3];

        public int RemainingPiecesCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (_activePieces[i] != null) count++;
                }
                return count;
            }
        }

        public IReadOnlyList<DraggablePiece> ActivePieces => _activePieces;

        public event Action<DraggablePiece, GridCoord> OnPiecePlaced; // (piece, anchor)
        public event Action OnHandDepleted;

        public void Initialize(HexBoard board, PieceSpawner spawner)
        {
            _board = board;
            _spawner = spawner;
        }

        public void DealNewHand()
        {
            ClearHand();

            ShapeDefinition[] batch = _spawner.GenerateHandBatch();

            for (int i = 0; i < 3; i++)
            {
                if (batch[i] != null)
                {
                    SpawnPieceInSlot(batch[i], i, i * 0.06f);
                }
            }

            UpdatePiecePlayability();
        }

        private void SpawnPieceInSlot(ShapeDefinition shape, int slotIndex, float dealDelay)
        {
            GameObject pieceObj = new GameObject($"Piece_Slot_{slotIndex}_{shape.id}");
            pieceObj.transform.SetParent(transform, false);

            DraggablePiece piece = pieceObj.AddComponent<DraggablePiece>();
            piece.Initialize(shape, slotIndex, _slotPositions[slotIndex], _board, dealDelay);
            piece.OnPiecePlaced += HandlePiecePlaced;

            _activePieces[slotIndex] = piece;
        }

        private void HandlePiecePlaced(DraggablePiece piece, GridCoord anchor)
        {
            int slot = piece.SlotIndex;
            _activePieces[slot] = null;
            piece.OnPiecePlaced -= HandlePiecePlaced;

            OnPiecePlaced?.Invoke(piece, anchor);

            Destroy(piece.gameObject);

            if (RemainingPiecesCount == 0)
            {
                OnHandDepleted?.Invoke();
            }
            else
            {
                UpdatePiecePlayability();
            }
        }

        public void UpdatePiecePlayability()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_activePieces[i] != null)
                {
                    bool isPlayable = _spawner.IsShapePlayable(_activePieces[i].Shape);
                    _activePieces[i].SetDisabled(!isPlayable);
                }
            }
        }

        public bool HasAnyPlayablePiece()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_activePieces[i] != null && _spawner.IsShapePlayable(_activePieces[i].Shape))
                {
                    return true;
                }
            }
            return false;
        }

        public void ClearHand()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_activePieces[i] != null)
                {
                    _activePieces[i].OnPiecePlaced -= HandlePiecePlaced;
                    Destroy(_activePieces[i].gameObject);
                    _activePieces[i] = null;
                }
            }
        }
    }
}
