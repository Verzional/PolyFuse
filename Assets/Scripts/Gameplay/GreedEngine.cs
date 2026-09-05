using System;
using UnityEngine;

namespace PolyFuse.Gameplay
{
    public struct TurnClearEventData
    {
        public int totalPointsGained;
        public int baseLinePoints;
        public int tileDensityBonus;
        public int crossAxisBonus;
        public int clutchSaveBonus;
        public int boardWipeBonus;
        public float comboMultiplier;
        public int comboStreak;
        public int streakDelta;
        public int distinctAxes;
        public bool isClutchSave;
        public bool isBoardWipe;
        public int linesCleared;
        public int tilesCleared;
        public string primaryTitle;
        public string multiplierString;
    }

    public class GreedEngine : MonoBehaviour
    {
        [Header("Scoring Tuning")]
        [SerializeField] private int _pointsPerUnitPlaced = 100;
        [SerializeField] private int _singleLineScore = 1000;
        [SerializeField] private int _doubleLineScore = 3500;
        [SerializeField] private int _tripleLineScore = 10000;
        [SerializeField] private int _quadPlusLineScore = 25000;
        [SerializeField] private int _boardWipeBonus = 25000;

        [Header("Arcade Convergence & Clutch Tuning")]
        [SerializeField] private int _dualAxisBonus = 2500; // Bonus base points for clearing 2 axes simultaneously
        [SerializeField] private int _triAxisBonus = 10000; // Bonus base points for clearing all 3 axes simultaneously
        [SerializeField] private int _clutchSaveBonus = 1500; // Bonus base points for clearing on last remaining grace turn
        [SerializeField] private int _tileDensityBounty = 60; // Extra base points per tile cleared in the completed lines

        [Header("Combo Tuning")]
        [SerializeField] private int _comboGraceTurns = 3; // Standard single-line combo grace buffer (3 pieces)
        [SerializeField] private int _multiLineGraceTurns = 5; // Extended grace buffer on multi-line clears (>= 2 lines)
        [SerializeField] private int _boardWipeGraceTurns = 7; // Extended 7-turn grace buffer on full board wipe

        [Header("Live State")]
        [SerializeField] private int _currentScore;
        [SerializeField] private int _highScore;
        [SerializeField] private int _comboStreak;
        [SerializeField] private int _graceRemaining;
        [SerializeField] private int _currentGraceCapacity = 3;

        [Header("Run Stats")]
        [SerializeField] private int _linesClearedInRun;
        [SerializeField] private int _maxComboStreakInRun;
        [SerializeField] private int _piecesPlacedInRun;

        private const string HighScoreKey = "PolyFuse_HighScore";
        private int _startingHighScore;
        private bool _hasTriggeredNewHighScoreInRun;

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;
        public int ComboStreak => _comboStreak;
        public int GraceRemaining => _graceRemaining;
        public int CurrentGraceCapacity => _currentGraceCapacity;
        public int LinesClearedInRun => _linesClearedInRun;
        public int MaxComboStreakInRun => _maxComboStreakInRun;
        public int PiecesPlacedInRun => _piecesPlacedInRun;
        public int MaxGraceTurns => _comboGraceTurns;
        public int MultiLineGraceTurns => _multiLineGraceTurns;
        public int BoardWipeGraceTurns => _boardWipeGraceTurns;
        public float AudioPitchMultiplier => 1.0f + (_comboStreak * 0.12f);
        public float CurrentMultiplier => CalculateComboMultiplier(_comboStreak);
        public int Multiplier => Mathf.RoundToInt(CurrentMultiplier);

        private TurnClearEventData _lastTurnClearData;
        public TurnClearEventData LastTurnClearData => _lastTurnClearData;

        public event Action<int, int> OnScoreChanged; // (currentScore, pointsDelta)
        public event Action<int, int, int, float> OnComboChanged; // (comboStreak, graceRemaining, graceCapacity, audioPitch)
        public event Action<int> OnBoardWipe;
        public event Action<int> OnNewHighScoreAchieved; // (newHighScore)
        public event Action<TurnClearEventData> OnTurnClearDetailed;

        private void Awake()
        {
            _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            _startingHighScore = _highScore;
        }

        public static float CalculateComboMultiplier(int streak)
        {
            if (streak <= 1) return 1.0f;
            return streak switch
            {
                2 => 2.0f,
                3 => 3.5f,
                4 => 5.5f,
                5 => 8.0f,
                6 => 12.0f,
                7 => 18.0f,
                _ => 18.0f + ((streak - 7) * 5.0f)
            };
        }

        public static string FormatMultiplierString(float mult)
        {
            return (mult == Mathf.Floor(mult)) ? $"{Mathf.RoundToInt(mult)}×" : $"{mult:0.0}×";
        }

        public void ResetGame()
        {
            _currentScore = 0;
            _comboStreak = 0;
            _graceRemaining = 0;
            _currentGraceCapacity = _comboGraceTurns;
            _linesClearedInRun = 0;
            _maxComboStreakInRun = 0;
            _piecesPlacedInRun = 0;
            _startingHighScore = _highScore;
            _hasTriggeredNewHighScoreInRun = false;
            _lastTurnClearData = default;
            OnScoreChanged?.Invoke(_currentScore, 0);
            OnComboChanged?.Invoke(_comboStreak, _graceRemaining, _currentGraceCapacity, 1.0f);
        }

        public void RecordPiecePlacement(int unitCount)
        {
            _piecesPlacedInRun++;
            float mult = CalculateComboMultiplier(_comboStreak);
            int gained = Mathf.RoundToInt(unitCount * _pointsPerUnitPlaced * mult);
            AddScore(gained);
        }

        public int ProcessTurnClears(ClearEvaluationResult clearResult, bool isBoardCompletelyEmpty)
        {
            int totalPointsGained = 0;

            if (clearResult.HasAnyClear)
            {
                int activeStreakBeforeClear = _comboStreak;
                int graceBeforeClear = _graceRemaining;
                bool isClutchSave = (activeStreakBeforeClear > 0 && graceBeforeClear == 1);

                // Streak Acceleration: multi-cleaves advance streak faster (+1, +2, +3, +4)
                int streakJump = Mathf.Clamp(clearResult.TotalLines, 1, 4);
                _comboStreak += streakJump;
                _linesClearedInRun += clearResult.TotalLines;
                _maxComboStreakInRun = Mathf.Max(_maxComboStreakInRun, _comboStreak);

                float mult = CalculateComboMultiplier(_comboStreak);

                // Distinct Isometric Axes (Cross-Axis Convergence)
                int distinctAxes = 0;
                if (clearResult.horizontalLines > 0) distinctAxes++;
                if (clearResult.slashLines > 0) distinctAxes++;
                if (clearResult.backslashLines > 0) distinctAxes++;

                int crossAxisBonus = 0;
                if (distinctAxes == 2) crossAxisBonus = _dualAxisBonus;
                else if (distinctAxes == 3) crossAxisBonus = _triAxisBonus;

                int clutchBonus = isClutchSave ? _clutchSaveBonus : 0;
                int tileDensityBonus = clearResult.TotalTilesCount * _tileDensityBounty;
                int baseLinePoints = CalculateLineClearBaseScore(clearResult.TotalLines);

                int subtotalBase = baseLinePoints + crossAxisBonus + clutchBonus + tileDensityBonus;
                totalPointsGained = Mathf.RoundToInt(subtotalBase * mult);

                // Grace Buffer Determination:
                // Board Wipe: 7 turns | Multi-Line (>= 2 lines): 5 turns | Single Line: 3 turns
                if (isBoardCompletelyEmpty)
                {
                    _currentGraceCapacity = _boardWipeGraceTurns;
                    _graceRemaining = _boardWipeGraceTurns;
                }
                else if (clearResult.TotalLines >= 2)
                {
                    _currentGraceCapacity = _multiLineGraceTurns;
                    _graceRemaining = _multiLineGraceTurns;
                }
                else
                {
                    _currentGraceCapacity = _comboGraceTurns;
                    _graceRemaining = _comboGraceTurns;
                }

                // Full Board Wipe jackpot bonus (+25,000 * multiplier)
                int wipePoints = 0;
                if (isBoardCompletelyEmpty)
                {
                    wipePoints = Mathf.RoundToInt(_boardWipeBonus * mult);
                    totalPointsGained += wipePoints;
                    OnBoardWipe?.Invoke(wipePoints);
                }

                // Primary hype headline
                string headline = "";
                if (isBoardCompletelyEmpty) headline = "★ BOARD WIPE! ★";
                else if (distinctAxes == 3) headline = "TRI-AXIS TRINITY!";
                else if (distinctAxes == 2) headline = "DUAL-AXIS CROSS!";
                else if (isClutchSave) headline = "★ CLUTCH SAVE! ★";
                else if (clearResult.TotalLines >= 4) headline = "SUPER NOVA!";
                else if (clearResult.TotalLines == 3) headline = "THE TRIFECTA!";
                else if (clearResult.TotalLines == 2) headline = "DOUBLE CLEAVE!";
                else if (_comboStreak > 1) headline = $"COMBO ×{_comboStreak}!";

                AddScore(totalPointsGained);

                TurnClearEventData eventData = new TurnClearEventData
                {
                    totalPointsGained = totalPointsGained,
                    baseLinePoints = baseLinePoints,
                    tileDensityBonus = tileDensityBonus,
                    crossAxisBonus = crossAxisBonus,
                    clutchSaveBonus = clutchBonus,
                    boardWipeBonus = wipePoints,
                    comboMultiplier = mult,
                    comboStreak = _comboStreak,
                    streakDelta = streakJump,
                    distinctAxes = distinctAxes,
                    isClutchSave = isClutchSave,
                    isBoardWipe = isBoardCompletelyEmpty,
                    linesCleared = clearResult.TotalLines,
                    tilesCleared = clearResult.TotalTilesCount,
                    primaryTitle = headline,
                    multiplierString = FormatMultiplierString(mult)
                };

                _lastTurnClearData = eventData;
                OnTurnClearDetailed?.Invoke(eventData);
            }
            else
            {
                _lastTurnClearData = default;
                // No line clear this turn: Decrement grace
                if (_comboStreak > 0)
                {
                    _graceRemaining--;
                    if (_graceRemaining <= 0)
                    {
                        // Buffer fully expired -> drop combo back to 0
                        _comboStreak = 0;
                        _graceRemaining = 0;
                        _currentGraceCapacity = _comboGraceTurns;
                    }
                }
            }

            OnComboChanged?.Invoke(_comboStreak, _graceRemaining, _currentGraceCapacity, AudioPitchMultiplier);
            return totalPointsGained;
        }

        public int CalculateLineClearBaseScore(int totalLines)
        {
            if (totalLines <= 0) return 0;
            return totalLines switch
            {
                1 => _singleLineScore,
                2 => _doubleLineScore,
                3 => _tripleLineScore,
                _ => _quadPlusLineScore
            };
        }

        private void AddScore(int points)
        {
            _currentScore += points;
            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                PlayerPrefs.SetInt(HighScoreKey, _highScore);

                if (!_hasTriggeredNewHighScoreInRun && (_startingHighScore > 0 || _currentScore >= 100))
                {
                    _hasTriggeredNewHighScoreInRun = true;
                    OnNewHighScoreAchieved?.Invoke(_highScore);
                }
            }
            OnScoreChanged?.Invoke(_currentScore, points);
        }
    }
}

