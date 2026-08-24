using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PolyFuse.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("HUD References")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _highScoreText;
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private CanvasGroup _comboCanvasGroup;

        [Header("Game Over Overlay")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TextMeshProUGUI _finalScoreText;
        [SerializeField] private Button _restartButton;

        private Coroutine _comboAnimCoroutine;

        public event Action OnRestartRequested;

        private void Awake()
        {
            EnsureUIHierarchy();

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(() => OnRestartRequested?.Invoke());
            }

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            if (_comboCanvasGroup != null)
            {
                _comboCanvasGroup.alpha = 0f;
            }
        }

        private void EnsureUIHierarchy()
        {
            if (_scoreText != null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("PolyFuse_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObj.transform, false);
            }

            // Header Panel
            GameObject headerObj = new GameObject("HeaderPanel");
            headerObj.transform.SetParent(canvas.transform, false);
            RectTransform headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 220f);
            headerRt.anchoredPosition = new Vector2(0f, -40f);

            // Score Text
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(headerRt, false);
            _scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            _scoreText.raycastTarget = false;
            _scoreText.text = "0";
            _scoreText.fontSize = 72;
            _scoreText.fontStyle = FontStyles.Bold;
            _scoreText.alignment = TextAlignmentOptions.Center;
            _scoreText.color = Color.white;
            RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0f, 0.4f);
            scoreRt.anchorMax = new Vector2(1f, 1f);
            scoreRt.sizeDelta = Vector2.zero;

            // High Score Text
            GameObject highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(headerRt, false);
            _highScoreText = highScoreObj.AddComponent<TextMeshProUGUI>();
            _highScoreText.raycastTarget = false;
            _highScoreText.text = "BEST 0";
            _highScoreText.fontSize = 28;
            _highScoreText.fontStyle = FontStyles.Bold;
            _highScoreText.alignment = TextAlignmentOptions.Center;
            _highScoreText.color = new Color(0.55f, 0.62f, 0.75f, 1.0f);
            RectTransform hsRt = highScoreObj.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0f, 0f);
            hsRt.anchorMax = new Vector2(1f, 0.4f);
            hsRt.sizeDelta = Vector2.zero;

            // Combo Banner
            GameObject comboObj = new GameObject("ComboBanner");
            comboObj.transform.SetParent(canvas.transform, false);
            RectTransform comboRt = comboObj.AddComponent<RectTransform>();
            comboRt.anchorMin = new Vector2(0.5f, 0.76f);
            comboRt.anchorMax = new Vector2(0.5f, 0.76f);
            comboRt.sizeDelta = new Vector2(600f, 100f);
            _comboCanvasGroup = comboObj.AddComponent<CanvasGroup>();
            _comboCanvasGroup.blocksRaycasts = false;
            _comboCanvasGroup.alpha = 0f;

            _comboText = comboObj.AddComponent<TextMeshProUGUI>();
            _comboText.raycastTarget = false;
            _comboText.text = "COMBO ×2!";
            _comboText.fontSize = 46;
            _comboText.fontStyle = FontStyles.Bold;
            _comboText.alignment = TextAlignmentOptions.Center;
            _comboText.color = new Color(1.0f, 0.78f, 0.20f, 1.0f);

            // Game Over Panel
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform goRt = _gameOverPanel.AddComponent<RectTransform>();
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.sizeDelta = Vector2.zero;

            Image goBg = _gameOverPanel.AddComponent<Image>();
            goBg.color = new Color(0.04f, 0.05f, 0.08f, 0.90f);

            // Content Box
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(_gameOverPanel.transform, false);
            RectTransform cRt = contentBox.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(600f, 500f);

            // Title
            GameObject titleObj = new GameObject("GameOverTitle");
            titleObj.transform.SetParent(contentBox.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.raycastTarget = false;
            titleText.text = "GAME OVER";
            titleText.fontSize = 52;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.96f, 0.35f, 0.42f, 1f);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.7f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.sizeDelta = Vector2.zero;

            // Final Score
            GameObject finalScoreObj = new GameObject("FinalScoreText");
            finalScoreObj.transform.SetParent(contentBox.transform, false);
            _finalScoreText = finalScoreObj.AddComponent<TextMeshProUGUI>();
            _finalScoreText.raycastTarget = false;
            _finalScoreText.text = "SCORE\n0";
            _finalScoreText.fontSize = 38;
            _finalScoreText.alignment = TextAlignmentOptions.Center;
            _finalScoreText.color = Color.white;
            RectTransform fsRt = finalScoreObj.GetComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0f, 0.35f);
            fsRt.anchorMax = new Vector2(1f, 0.7f);
            fsRt.sizeDelta = Vector2.zero;

            // Restart Button
            GameObject btnObj = new GameObject("RestartButton");
            btnObj.transform.SetParent(contentBox.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.20f, 0.85f, 0.95f, 1f);
            _restartButton = btnObj.AddComponent<Button>();
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.15f, 0.05f);
            btnRt.anchorMax = new Vector2(0.85f, 0.28f);
            btnRt.sizeDelta = Vector2.zero;

            GameObject btnLabelObj = new GameObject("BtnLabel");
            btnLabelObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI btnLabel = btnLabelObj.AddComponent<TextMeshProUGUI>();
            btnLabel.raycastTarget = false;
            btnLabel.text = "PLAY AGAIN";
            btnLabel.fontSize = 32;
            btnLabel.fontStyle = FontStyles.Bold;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color = new Color(0.06f, 0.07f, 0.10f, 1.0f);
            RectTransform blRt = btnLabelObj.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.sizeDelta = Vector2.zero;
        }

        public void UpdateScore(int currentScore, int highScore)
        {
            if (_scoreText != null) _scoreText.text = currentScore.ToString("N0");
            if (_highScoreText != null) _highScoreText.text = $"BEST {highScore:N0}";
        }

        public void ShowComboBadge(int comboStreak, float pitch)
        {
            if (comboStreak <= 1)
            {
                if (_comboCanvasGroup != null) _comboCanvasGroup.alpha = 0f;
                return;
            }

            if (_comboText != null)
            {
                _comboText.text = $"COMBO ×{comboStreak}!";
            }

            if (_comboAnimCoroutine != null) StopCoroutine(_comboAnimCoroutine);
            _comboAnimCoroutine = StartCoroutine(AnimateComboBadge());
        }

        private IEnumerator AnimateComboBadge()
        {
            if (_comboCanvasGroup == null) yield break;

            _comboCanvasGroup.alpha = 1f;
            Transform t = _comboCanvasGroup.transform;
            Vector3 startScale = Vector3.one * 0.7f;
            Vector3 popScale = Vector3.one * 1.25f;
            Vector3 normalScale = Vector3.one;

            float elapsed = 0f;
            float popDur = 0.15f;

            while (elapsed < popDur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(startScale, popScale, elapsed / popDur);
                yield return null;
            }

            elapsed = 0f;
            float settleDur = 0.12f;
            while (elapsed < settleDur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(popScale, normalScale, elapsed / settleDur);
                yield return null;
            }
            t.localScale = normalScale;

            yield return new WaitForSecondsRealtime(1.2f);

            elapsed = 0f;
            float fadeDur = 0.3f;
            while (elapsed < fadeDur)
            {
                elapsed += Time.unscaledDeltaTime;
                _comboCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDur);
                yield return null;
            }
            _comboCanvasGroup.alpha = 0f;
            _comboAnimCoroutine = null;
        }

        public void ShowGameOver(int finalScore)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
            }
            if (_finalScoreText != null)
            {
                _finalScoreText.text = $"SCORE\n{finalScore:N0}";
            }
        }

        public void HideGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }
        }
    }
}
