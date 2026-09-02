using System;
using UnityEngine;

namespace PolyFuse.Gameplay
{
    public class GreedEngine : MonoBehaviour
    {
        [Header("Scoring Tuning")]
        [SerializeField] private int _pointsPerUnitPlaced = 100;
        [SerializeField] private int _singleLineScore = 1000;
        [SerializeField] private int _doubleLineScore = 3500;
        [SerializeField] private int _tripleLineScore = 10000;
        [SerializeField] private int _quadPlusLineScore = 25000;
        [SerializeField] private int _boardWipeBonus = 25000;

        [Header("Combo Tuning")]
        [SerializeField] private int _comboGraceTurns = 3; // Standard single-line combo grace buffer (3 pieces)
        [SerializeField] private int _multiLineGraceTurns = 5; // Extended grace buffer on multi-line clears (>= 2 lines) or board wipe

        [Header("Live State")]
        [SerializeField] private int _currentScore;
        [SerializeField] private int _highScore;
        [SerializeField] private int _comboStreak;
        [SerializeField] private int _graceRemaining;
        [SerializeField] private int _currentGraceCapacity = 3;

        private const string HighScoreKey = "PolyFuse_HighScore";
        private int _startingHighScore;
        private bool _hasTriggeredNewHighScoreInRun;

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;
        public int ComboStreak => _comboStreak;
        public int GraceRemaining => _graceRemaining;
        public int CurrentGraceCapacity => _currentGraceCapacity;
        public int MaxGraceTurns => _comboGraceTurns;
        public int MultiLineGraceTurns => _multiLineGraceTurns;
        public int BoardWipeGraceTurns => _multiLineGraceTurns;
        public float AudioPitchMultiplier => 1.0f + (_comboStreak * 0.12f);
        public int Multiplier => Mathf.Max(1, _comboStreak);

        public event Action<int, int> OnScoreChanged; // (currentScore, pointsDelta)
        public event Action<int, int, int, float> OnComboChanged; // (comboStreak, graceRemaining, graceCapacity, audioPitch)
        public event Action<int> OnBoardWipe;
        public event Action<int> OnNewHighScoreAchieved; // (newHighScore)

        private void Awake()
        {
            _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            _startingHighScore = _highScore;
        }

        public void ResetGame()
        {
            _currentScore = 0;
            _comboStreak = 0;
            _graceRemaining = 0;
            _currentGraceCapacity = _comboGraceTurns;
            _startingHighScore = _highScore;
            _hasTriggeredNewHighScoreInRun = false;
            OnScoreChanged?.Invoke(_currentScore, 0);
            OnComboChanged?.Invoke(_comboStreak, _graceRemaining, _currentGraceCapacity, 1.0f);
        }

        public void RecordPiecePlacement(int unitCount)
        {
            int mult = Mathf.Max(1, _comboStreak);
            int gained = unitCount * _pointsPerUnitPlaced * mult;
            AddScore(gained);
        }

        public int ProcessTurnClears(ClearEvaluationResult clearResult, bool isBoardCompletelyEmpty)
        {
            int totalPointsGained = 0;

            if (clearResult.HasAnyClear)
            {
                // Line clear triggered: Increment streak
                _comboStreak++;
                int mult = Mathf.Max(1, _comboStreak);

                // Multi-line clears (>= 2 lines) or Board Wipe gets extended grace buffer (5 turns)
                if (clearResult.TotalLines >= 2 || isBoardCompletelyEmpty)
                {
                    _currentGraceCapacity = _multiLineGraceTurns;
                    _graceRemaining = _multiLineGraceTurns;
                }
                else
                {
                    _currentGraceCapacity = _comboGraceTurns;
                    _graceRemaining = _comboGraceTurns;
                }

                // Calculate line clear base points with exponential jackpot scaling
                int baseLinePoints = CalculateLineClearBaseScore(clearResult.TotalLines);
                totalPointsGained = baseLinePoints * mult;

                // Full Board Wipe bonus (+25,000 * multiplier)
                if (isBoardCompletelyEmpty)
                {
                    int wipePoints = _boardWipeBonus * mult;
                    totalPointsGained += wipePoints;
                    OnBoardWipe?.Invoke(wipePoints);
                }

                AddScore(totalPointsGained);
            }
            else
            {
                // No line clear this turn
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
