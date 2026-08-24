using System.Collections;
using System.Collections.Generic;
using PolyFuse.Core;
using PolyFuse.Grid;
using PolyFuse.Interaction;
using PolyFuse.Juice;
using PolyFuse.UI;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PolyFuse.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Core Systems")]
        [SerializeField] private HexBoard _board;
        [SerializeField] private GreedEngine _greedEngine;
        [SerializeField] private PieceSpawner _spawner;
        [SerializeField] private HandTrayManager _trayManager;
        [SerializeField] private JuiceController _juice;
        [SerializeField] private ProceduralAudio _audio;
        [SerializeField] private GameUI _ui;

        private DualClearEvaluator _evaluator;
        private bool _isGameOver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrapOnPlay()
        {
            if (FindFirstObjectByType<GameManager>() == null)
            {
                Debug.Log("[PolyFuse] Auto-bootstrapping GameManager into active scene...");
                GameObject root = new GameObject("PolyFuse_GameManager");
                root.AddComponent<GameManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureComponentsAndSetup();
        }

        private void Start()
        {
            StartNewGame();
        }

        private void EnsureComponentsAndSetup()
        {
            // Camera setup
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0f, -0.6f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 7.0f;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1.0f);

            if (cam.GetComponent<Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<Physics2DRaycaster>();
            }

            // Ensure EventSystem exists with modern InputSystem module
            EventSystem es = FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                es = eventSystemObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                eventSystemObj.AddComponent<InputSystemUIInputModule>();
#else
                eventSystemObj.AddComponent<StandaloneInputModule>();
#endif
            }

            // Board
            if (_board == null)
            {
                _board = FindFirstObjectByType<HexBoard>();
                if (_board == null)
                {
                    GameObject boardObj = new GameObject("HexBoard");
                    _board = boardObj.AddComponent<HexBoard>();
                }
            }
            _board.GenerateBoard();

            // Evaluator
            _evaluator = new DualClearEvaluator(_board);

            // Greed Engine
            if (_greedEngine == null)
            {
                _greedEngine = GetComponent<GreedEngine>() ?? gameObject.AddComponent<GreedEngine>();
            }

            // Piece Spawner
            if (_spawner == null)
            {
                _spawner = GetComponent<PieceSpawner>() ?? gameObject.AddComponent<PieceSpawner>();
            }
            _spawner.Initialize(_board);

            // Hand Tray
            if (_trayManager == null)
            {
                _trayManager = FindFirstObjectByType<HandTrayManager>();
                if (_trayManager == null)
                {
                    GameObject trayObj = new GameObject("HandTrayManager");
                    _trayManager = trayObj.AddComponent<HandTrayManager>();
                }
            }
            _trayManager.Initialize(_board, _spawner);
            _trayManager.OnPiecePlaced += HandlePiecePlaced;
            _trayManager.OnHandDepleted += HandleHandDepleted;

            // Juice & Audio
            if (_juice == null)
            {
                _juice = GetComponent<JuiceController>() ?? gameObject.AddComponent<JuiceController>();
            }
            if (_audio == null)
            {
                _audio = GetComponent<ProceduralAudio>() ?? gameObject.AddComponent<ProceduralAudio>();
            }

            // UI
            if (_ui == null)
            {
                _ui = FindFirstObjectByType<GameUI>();
                if (_ui == null)
                {
                    GameObject uiObj = new GameObject("GameUI");
                    _ui = uiObj.AddComponent<GameUI>();
                }
            }

            if (_ui != null)
            {
                _ui.OnRestartRequested += StartNewGame;
            }

            // Event bindings
            _greedEngine.OnScoreChanged += (score, delta) =>
            {
                if (_ui != null) _ui.UpdateScore(score, _greedEngine.HighScore);
            };

            _greedEngine.OnComboChanged += (combo, pitch) =>
            {
                if (_ui != null) _ui.ShowComboBadge(combo, pitch);
            };
        }

        public void StartNewGame()
        {
            _isGameOver = false;
            _board.ResetBoard();
            _greedEngine.ResetGame();

            if (_ui != null)
            {
                _ui.HideGameOver();
                _ui.UpdateScore(0, _greedEngine.HighScore);
            }

            _trayManager.DealNewHand();
        }

        private void HandlePiecePlaced(DraggablePiece piece, GridCoord anchor)
        {
            if (_isGameOver) return;

            // 1. Commit piece placement to board
            _board.PlaceShape(piece.Shape, anchor);
            _greedEngine.RecordPiecePlacement(piece.Shape.UnitCount);
            _audio.PlayPieceSnap();

            // 2. Evaluate 3-Axis Line Clears
            StartCoroutine(ProcessClearsAndTurnFlow());
        }

        private IEnumerator ProcessClearsAndTurnFlow()
        {
            ClearEvaluationResult result = _evaluator.EvaluateBoard();

            if (result.HasAnyClear)
            {
                // Multi-line hit-stop
                if (result.TotalLines >= 2)
                {
                    _juice.TriggerHitStop();
                }

                _juice.TriggerLineClearShake();
                _audio.PlayLineClear(_greedEngine.AudioPitchMultiplier);

                // Trigger clear animation on tiles
                foreach (var coord in result.tilesToClear)
                {
                    TriangleTile tile = _board.GetTile(coord);
                    if (tile != null)
                    {
                        tile.PlayClearFlash(null);
                    }
                }

                yield return new WaitForSeconds(0.25f);

                // Check full board wipe
                bool isWipe = true;
                foreach (var kvp in _board.Tiles)
                {
                    if (kvp.Value.IsOccupied)
                    {
                        isWipe = false;
                        break;
                    }
                }

                _greedEngine.ProcessTurnClears(result, isWipe);
            }
            else
            {
                _greedEngine.ProcessTurnClears(result, false);
            }

            // 3. Tray Replenishment
            if (_trayManager.RemainingPiecesCount == 0)
            {
                _trayManager.DealNewHand();
            }
            else
            {
                _trayManager.UpdatePiecePlayability();
            }

            // 4. Validate Game Over
            if (!_trayManager.HasAnyPlayablePiece())
            {
                TriggerGameOver();
            }
        }

        private void HandleHandDepleted()
        {
            if (!_isGameOver)
            {
                _trayManager.DealNewHand();
                if (!_trayManager.HasAnyPlayablePiece())
                {
                    TriggerGameOver();
                }
            }
        }

        private void TriggerGameOver()
        {
            _isGameOver = true;
            _audio.PlayGameOver();
            Debug.Log($"[PolyFuse] GAME OVER! Final Score: {_greedEngine.CurrentScore}");

            if (_ui != null)
            {
                _ui.ShowGameOver(_greedEngine.CurrentScore);
            }
        }
    }
}
