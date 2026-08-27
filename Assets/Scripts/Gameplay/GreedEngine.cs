using System;
using UnityEngine;

namespace PolyFuse.Gameplay
{
    public class GreedEngine : MonoBehaviour
    {
        [Header("Scoring Tuning")]
        [SerializeField] private int _pointsPerUnitPlaced = 10;
        [SerializeField] private int _pointsPerLineClear = 100;
        [SerializeField] private int _multiClearBonus = 150;
        [SerializeField] private int _boardWipeBonus = 1000;

        [Header("Combo Tuning")]
        [SerializeField] private int _comboGraceTurns = 3; // Combo lasts 3 pieces without clear

        [Header("Live State")]
        [SerializeField] private int _currentScore;
        [SerializeField] private int _highScore;
        [SerializeField] private int _comboStreak;
        [SerializeField] private int _graceRemaining;

        private const string HighScoreKey = "PolyFuse_HighScore";
        private int _startingHighScore;
        private bool _hasTriggeredNewHighScoreInRun;

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;
        public int ComboStreak => _comboStreak;
        public int GraceRemaining => _graceRemaining;
        public int MaxGraceTurns => _comboGraceTurns;
        public float AudioPitchMultiplier => 1.0f + (_comboStreak * 0.12f);
        public int Multiplier => Mathf.Max(1, _comboStreak);

        public event Action<int, int> OnScoreChanged; // (currentScore, pointsDelta)
        public event Action<int, int, float> OnComboChanged; // (comboStreak, graceRemaining, audioPitch)
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
            _startingHighScore = _highScore;
            _hasTriggeredNewHighScoreInRun = false;
            OnScoreChanged?.Invoke(_currentScore, 0);
            OnComboChanged?.Invoke(_comboStreak, _graceRemaining, 1.0f);
        }

        public void RecordPiecePlacement(int unitCount)
        {
            int gained = unitCount * _pointsPerUnitPlaced;
            AddScore(gained);
        }

        public int ProcessTurnClears(ClearEvaluationResult clearResult, bool isBoardCompletelyEmpty)
        {
            int totalPointsGained = 0;

            if (clearResult.HasAnyClear)
            {
                // Line clear triggered: Increment streak and reset grace turns buffer to 3
                _comboStreak++;
                _graceRemaining = _comboGraceTurns;
                int mult = _comboStreak;

                // 3-Axis Line clears points
                int linePoints = clearResult.TotalLines * _pointsPerLineClear * mult;
                totalPointsGained = linePoints;

                // Multi-line simultaneous combo bonus
                if (clearResult.TotalLines >= 2)
                {
                    totalPointsGained += (clearResult.TotalLines - 1) * _multiClearBonus * mult;
                }

                // Full Board Wipe
                if (isBoardCompletelyEmpty)
                {
                    totalPointsGained += _boardWipeBonus * mult;
                    OnBoardWipe?.Invoke(_boardWipeBonus * mult);
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
                    }
                }
            }

            OnComboChanged?.Invoke(_comboStreak, _graceRemaining, AudioPitchMultiplier);
            return totalPointsGained;
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
