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
        private bool _inDangerMode;
        private bool _isInHome = true;

        public bool IsGameOver => _isGameOver;
        public bool IsInHome => _isInHome;

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

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            EnsureComponentsAndSetup();
        }

        private void Start()
        {
            ReturnToHome();
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
            cam.transform.position = new Vector3(0f, -1.25f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 7.0f;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1.0f);

            if (FindFirstObjectByType<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            if (cam.GetComponent<Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<Physics2DRaycaster>();
            }

            if (cam.GetComponent<MobileResolutionAdapter>() == null)
            {
                cam.gameObject.AddComponent<MobileResolutionAdapter>();
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

            // Juice & Audio & Particles & Haptics
            if (_juice == null)
            {
                _juice = GetComponent<JuiceController>() ?? gameObject.AddComponent<JuiceController>();
            }
            if (_audio == null)
            {
                _audio = GetComponent<ProceduralAudio>() ?? gameObject.AddComponent<ProceduralAudio>();
            }
            if (FindFirstObjectByType<ProceduralParticleManager>() == null)
            {
                gameObject.AddComponent<ProceduralParticleManager>();
            }
            if (FindFirstObjectByType<HapticFeedbackManager>() == null)
            {
                gameObject.AddComponent<HapticFeedbackManager>();
            }
            if (FindFirstObjectByType<BoardAuraController>() == null)
            {
                if (_board != null)
                {
                    _board.gameObject.AddComponent<BoardAuraController>();
                }
                else
                {
                    gameObject.AddComponent<BoardAuraController>();
                }
            }
            if (FindFirstObjectByType<WorldSpacePopupManager>() == null)
            {
                gameObject.AddComponent<WorldSpacePopupManager>();
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
                _ui.OnPlayRequested += StartNewGame;
                _ui.OnHomeRequested += ReturnToHome;
            }

            // Event bindings
            _greedEngine.OnScoreChanged += (score, delta) =>
            {
                _ui?.UpdateScore(score, _greedEngine.HighScore, delta);
            };

            _greedEngine.OnComboChanged += (streak, grace, capacity, pitch) =>
            {
                _ui?.UpdateComboState(streak, grace, capacity, pitch);
                BoardAuraController.Instance?.SetComboState(streak);
            };

            _greedEngine.OnNewHighScoreAchieved += (highScore) =>
            {
                _ui?.ShowNewHighScoreBanner(highScore);
                _audio.PlayNewBestFanfare();
                ProceduralParticleManager.Instance?.SpawnGoldenStarburst(Vector3.up * 1.5f);
            };

            _greedEngine.OnBoardWipe += (bonus) =>
            {
                _ui?.ShowBoardWipeBanner(bonus);
                ProceduralParticleManager.Instance?.SpawnConfettiBurst(Vector3.zero);
            };
        }

        public void ReturnToHome()
        {
            _isInHome = true;
            _isGameOver = false;
            _inDangerMode = false;
            _audio?.PlayHeartbeat(false);
            BoardAuraController.Instance?.SetDangerState(false);
            if (_ui != null) _ui.SetDangerState(false);
            _board.ResetBoard();
            _greedEngine.ResetGame();
            BoardAuraController.Instance?.SetComboState(0);
            _trayManager.ClearHand();
            Time.timeScale = 1.0f;

            if (_ui != null)
            {
                _ui.HideGameOver();
                _ui.CloseSettingsModal();
                _ui.ShowHomeScreen(_greedEngine.HighScore);
            }
        }

        public void StartNewGame()
        {
            _isInHome = false;
            _isGameOver = false;
            _inDangerMode = false;
            _audio?.PlayHeartbeat(false);
            BoardAuraController.Instance?.SetDangerState(false);
            if (_ui != null) _ui.SetDangerState(false);
            _board.ResetBoard();
            _greedEngine.ResetGame();
            BoardAuraController.Instance?.SetComboState(0);
            Time.timeScale = 1.0f;

            if (_ui != null)
            {
                _ui.HideGameOver();
                _ui.HideHomeScreen();
                _ui.CloseSettingsModal();
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
                // Multi-line hit-stop & punchy screen shake
                if (result.TotalLines >= 2)
                {
                    _juice.TriggerMultiLineHitStop(result.TotalLines);
                    _juice.TriggerMultiLineClearShake(result.TotalLines);
                }
                else
                {
                    _juice.TriggerLineClearShake();
                }

                int streakJump = Mathf.Clamp(result.TotalLines, 1, 4);
                int activeStreak = _greedEngine.ComboStreak + streakJump;
                BoardAuraController.Instance?.TriggerClearSurge(activeStreak);

                if (result.TotalLines >= 2)
                {
                    _audio.PlayMultiLineClear(result.TotalLines, activeStreak);
                    HapticFeedbackManager.Instance?.PlayHeavy();
                }
                else
                {
                    _audio.PlayLineClear(activeStreak);
                    HapticFeedbackManager.Instance?.PlayMedium();
                }

                // Compute geometric center of cleared tiles for world-space popup
                Vector3 clearCenterPos = Vector3.zero;
                int tileCount = 0;
                foreach (var coord in result.tilesToClear)
                {
                    TriangleTile tile = _board.GetTile(coord);
                    if (tile != null)
                    {
                        clearCenterPos += tile.transform.position;
                        tileCount++;
                    }
                }
                if (tileCount > 0) clearCenterPos /= tileCount;

                // Trigger clear animation and particles on tiles
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

                if (isWipe)
                {
                    _audio.PlayBoardWipe();
                    HapticFeedbackManager.Instance?.PlayHeavy();
                }

                int pointsGained = _greedEngine.ProcessTurnClears(result, isWipe);
                TurnClearEventData clearData = _greedEngine.LastTurnClearData;
                WorldSpacePopupManager.Instance?.SpawnCleavePopup(clearCenterPos, clearData);

                if (clearData.isClutchSave)
                {
                    _audio.PlayClutchSave();
                    HapticFeedbackManager.Instance?.PlayHeavy();
                    ProceduralParticleManager.Instance?.SpawnGoldenStarburst(clearCenterPos);
                }
                else if (clearData.distinctAxes >= 2)
                {
                    _audio.PlayCrossAxisConvergence(clearData.distinctAxes);
                    if (clearData.distinctAxes >= 3)
                    {
                        ProceduralParticleManager.Instance?.SpawnGoldenStarburst(clearCenterPos);
                    }
                }
            }
            else
            {
                _greedEngine.ProcessTurnClears(result, false);
            }

            // Danger Mode Check (Fill >= 65% triggers danger; dropping < 50% escapes with celebration)
            float fillRatio = _board.BoardFillRatio;
            if (fillRatio >= 0.65f && !_inDangerMode)
            {
                _inDangerMode = true;
                _audio.PlayHeartbeat(true);
                BoardAuraController.Instance?.SetDangerState(true);
                if (_ui != null) _ui.SetDangerState(true);
            }
            else if (_inDangerMode && fillRatio < 0.50f)
            {
                _inDangerMode = false;
                _audio.PlayHeartbeat(false);
                _audio.PlayCloseCallFanfare();
                BoardAuraController.Instance?.SetDangerState(false);
                if (_ui != null)
                {
                    _ui.SetDangerState(false);
                    _ui.ShowCloseCallBanner();
                }
                ProceduralParticleManager.Instance?.SpawnConfettiBurst(Vector3.zero);
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

        private void TriggerGameOver()
        {
            _isGameOver = true;
            _inDangerMode = false;
            _audio.PlayHeartbeat(false);
            BoardAuraController.Instance?.SetDangerState(false);
            if (_ui != null) _ui.SetDangerState(false);
            _audio.PlayGameOver();
            Debug.Log($"[PolyFuse] GAME OVER! Final Score: {_greedEngine.CurrentScore}");

            if (_ui != null)
            {
                _ui.ShowGameOver(_greedEngine.CurrentScore, _greedEngine.MaxComboStreakInRun, _greedEngine.LinesClearedInRun, _greedEngine.PiecesPlacedInRun);
            }
        }
    }
}
