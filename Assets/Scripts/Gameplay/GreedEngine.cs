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

        [Header("Live State")]
        [SerializeField] private int _currentScore;
        [SerializeField] private int _highScore;
        [SerializeField] private int _comboStreak;

        private const string HighScoreKey = "PolyFuse_HighScore";

        public int CurrentScore => _currentScore;
        public int HighScore => _highScore;
        public int ComboStreak => _comboStreak;
        public float AudioPitchMultiplier => 1.0f + (_comboStreak * 0.12f);
        public int Multiplier => Mathf.Max(1, _comboStreak);

        public event Action<int, int> OnScoreChanged;
        public event Action<int, float> OnComboChanged;
        public event Action<int> OnBoardWipe;

        private void Awake()
        {
            _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void ResetGame()
        {
            _currentScore = 0;
            _comboStreak = 0;
            OnScoreChanged?.Invoke(_currentScore, 0);
            OnComboChanged?.Invoke(_comboStreak, 1.0f);
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
                // Increment Greed Combo Streak
                _comboStreak++;
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
                // No clear this turn -> combo resets to 0
                _comboStreak = 0;
            }

            OnComboChanged?.Invoke(_comboStreak, AudioPitchMultiplier);
            return totalPointsGained;
        }

        private void AddScore(int points)
        {
            _currentScore += points;
            if (_currentScore > _highScore)
            {
                _highScore = _currentScore;
                PlayerPrefs.SetInt(HighScoreKey, _highScore);
            }
            OnScoreChanged?.Invoke(_currentScore, points);
        }
    }
}
