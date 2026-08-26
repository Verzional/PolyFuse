using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PolyFuse.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("HUD References")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _highScoreText;
        [SerializeField] private Text _scoreDeltaPopup;
        [SerializeField] private Text _comboText;
        [SerializeField] private Text _comboPipsText;
        [SerializeField] private CanvasGroup _comboCanvasGroup;

        [Header("Game Over Overlay")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Text _finalScoreText;
        [SerializeField] private Button _restartButton;

        private Font _uiFont;
        private Coroutine _scorePunchCoroutine;
        private Coroutine _deltaPopupCoroutine;
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

            if (_scoreDeltaPopup != null)
            {
                _scoreDeltaPopup.gameObject.SetActive(false);
            }
        }

        private Font GetSystemFont()
        {
            if (_uiFont != null) return _uiFont;

            // Load native Unity fonts
            _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null) _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (_uiFont == null)
            {
                string[] fontNames = new[] { "Arial", "Helvetica", "San Francisco", "Roboto", "Verdana" };
                foreach (var fname in fontNames)
                {
                    try
                    {
                        _uiFont = Font.CreateDynamicFontFromOSFont(fname, 36);
                        if (_uiFont != null) break;
                    }
                    catch { }
                }
            }

            return _uiFont;
        }

        private void EnsureUIHierarchy()
        {
            if (_scoreText != null) return;

            Canvas existingCanvas = FindFirstObjectByType<Canvas>();
            if (existingCanvas != null && existingCanvas.name == "PolyFuse_Canvas")
            {
                DestroyImmediate(existingCanvas.gameObject);
            }

            GameObject canvasObj = new GameObject("PolyFuse_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.0f; // Perfect portrait width scaling

            canvasObj.AddComponent<GraphicRaycaster>();
            transform.SetParent(canvasObj.transform, false);

            Font font = GetSystemFont();

            // 1. Header Panel (Top Center)
            GameObject headerObj = new GameObject("HeaderPanel");
            headerObj.transform.SetParent(canvas.transform, false);
            RectTransform headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(0f, 260f);
            headerRt.anchoredPosition = new Vector2(0f, -40f);

            // High Score Text (At very top)
            GameObject highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(headerRt, false);
            _highScoreText = highScoreObj.AddComponent<Text>();
            _highScoreText.font = font;
            _highScoreText.raycastTarget = false;
            _highScoreText.text = "BEST  0";
            _highScoreText.fontSize = 30;
            _highScoreText.fontStyle = FontStyle.Bold;
            _highScoreText.alignment = TextAnchor.MiddleCenter;
            _highScoreText.color = new Color(0.65f, 0.78f, 0.95f, 1.0f);
            AddShadow(highScoreObj, new Color(0f, 0f, 0f, 0.6f), new Vector2(1.5f, -1.5f));
            RectTransform hsRt = highScoreObj.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0f, 0.70f);
            hsRt.anchorMax = new Vector2(1f, 1.0f);
            hsRt.sizeDelta = Vector2.zero;

            // Score Text (Large bold number)
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(headerRt, false);
            _scoreText = scoreObj.AddComponent<Text>();
            _scoreText.font = font;
            _scoreText.raycastTarget = false;
            _scoreText.text = "0";
            _scoreText.fontSize = 84;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.alignment = TextAnchor.MiddleCenter;
            _scoreText.color = Color.white;
            AddOutline(scoreObj, new Color(0.05f, 0.08f, 0.14f, 0.8f), new Vector2(2f, -2f));
            RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
            scoreRt.anchorMin = new Vector2(0f, 0.0f);
            scoreRt.anchorMax = new Vector2(1f, 0.72f);
            scoreRt.sizeDelta = Vector2.zero;

            // Score Delta Popup (Floating +300)
            GameObject popupObj = new GameObject("ScoreDeltaPopup");
            popupObj.transform.SetParent(canvas.transform, false);
            _scoreDeltaPopup = popupObj.AddComponent<Text>();
            _scoreDeltaPopup.font = font;
            _scoreDeltaPopup.raycastTarget = false;
            _scoreDeltaPopup.text = "+150";
            _scoreDeltaPopup.fontSize = 46;
            _scoreDeltaPopup.fontStyle = FontStyle.Bold;
            _scoreDeltaPopup.alignment = TextAnchor.MiddleCenter;
            _scoreDeltaPopup.color = new Color(0.20f, 0.90f, 1.0f, 1f);
            AddOutline(popupObj, new Color(0.02f, 0.05f, 0.10f, 0.9f), new Vector2(2f, -2f));
            RectTransform popupRt = popupObj.GetComponent<RectTransform>();
            popupRt.anchorMin = new Vector2(0.5f, 0.78f);
            popupRt.anchorMax = new Vector2(0.5f, 0.78f);
            popupRt.sizeDelta = new Vector2(400f, 80f);
            popupRt.anchoredPosition = new Vector2(0f, 0f);
            popupObj.SetActive(false);

            // 2. Combo Banner Container (Centered below score)
            GameObject comboObj = new GameObject("ComboBanner");
            comboObj.transform.SetParent(canvas.transform, false);
            RectTransform comboRt = comboObj.AddComponent<RectTransform>();
            comboRt.anchorMin = new Vector2(0.5f, 0.73f);
            comboRt.anchorMax = new Vector2(0.5f, 0.73f);
            comboRt.sizeDelta = new Vector2(600f, 120f);
            _comboCanvasGroup = comboObj.AddComponent<CanvasGroup>();
            _comboCanvasGroup.blocksRaycasts = false;
            _comboCanvasGroup.alpha = 0f;

            // Combo Label
            GameObject comboLabelObj = new GameObject("ComboLabel");
            comboLabelObj.transform.SetParent(comboObj.transform, false);
            _comboText = comboLabelObj.AddComponent<Text>();
            _comboText.font = font;
            _comboText.raycastTarget = false;
            _comboText.text = "COMBO ×2!";
            _comboText.fontSize = 48;
            _comboText.fontStyle = FontStyle.Bold;
            _comboText.alignment = TextAnchor.MiddleCenter;
            _comboText.color = new Color(1.0f, 0.82f, 0.20f, 1.0f);
            AddOutline(comboLabelObj, new Color(0.12f, 0.08f, 0.02f, 0.9f), new Vector2(2f, -2f));
            RectTransform clRt = comboLabelObj.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(0f, 0.35f);
            clRt.anchorMax = new Vector2(1f, 1f);
            clRt.sizeDelta = Vector2.zero;

            // Combo Pips (● ● ●)
            GameObject pipsObj = new GameObject("ComboPips");
            pipsObj.transform.SetParent(comboObj.transform, false);
            _comboPipsText = pipsObj.AddComponent<Text>();
            _comboPipsText.font = font;
            _comboPipsText.raycastTarget = false;
            _comboPipsText.text = "● ● ●";
            _comboPipsText.fontSize = 26;
            _comboPipsText.alignment = TextAnchor.MiddleCenter;
            _comboPipsText.color = new Color(1.0f, 0.90f, 0.45f, 0.95f);
            AddShadow(pipsObj, new Color(0f, 0f, 0f, 0.6f), new Vector2(1.5f, -1.5f));
            RectTransform pRt = pipsObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0f);
            pRt.anchorMax = new Vector2(1f, 0.4f);
            pRt.sizeDelta = Vector2.zero;

            // 3. Game Over Panel
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform goRt = _gameOverPanel.AddComponent<RectTransform>();
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.sizeDelta = Vector2.zero;

            Image goBg = _gameOverPanel.AddComponent<Image>();
            goBg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);

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
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.raycastTarget = false;
            titleText.text = "GAME OVER";
            titleText.fontSize = 54;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.96f, 0.35f, 0.42f, 1f);
            AddOutline(titleObj, new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f));
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.7f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.sizeDelta = Vector2.zero;

            // Final Score
            GameObject finalScoreObj = new GameObject("FinalScoreText");
            finalScoreObj.transform.SetParent(contentBox.transform, false);
            _finalScoreText = finalScoreObj.AddComponent<Text>();
            _finalScoreText.font = font;
            _finalScoreText.raycastTarget = false;
            _finalScoreText.text = "SCORE\n0";
            _finalScoreText.fontSize = 38;
            _finalScoreText.fontStyle = FontStyle.Bold;
            _finalScoreText.alignment = TextAnchor.MiddleCenter;
            _finalScoreText.color = Color.white;
            AddOutline(finalScoreObj, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
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
            Text btnLabel = btnLabelObj.AddComponent<Text>();
            btnLabel.font = font;
            btnLabel.raycastTarget = false;
            btnLabel.text = "PLAY AGAIN";
            btnLabel.fontSize = 32;
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color = new Color(0.06f, 0.07f, 0.10f, 1.0f);
            RectTransform blRt = btnLabelObj.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.sizeDelta = Vector2.zero;
        }

        private void AddOutline(GameObject target, Color color, Vector2 dist)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = dist;
            outline.useGraphicAlpha = true;
        }

        private void AddShadow(GameObject target, Color color, Vector2 dist)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = dist;
            shadow.useGraphicAlpha = true;
        }

        public void UpdateScore(int currentScore, int highScore, int pointsDelta = 0)
        {
            if (_scoreText != null)
            {
                _scoreText.text = currentScore.ToString("N0");
                if (pointsDelta > 0)
                {
                    if (_scorePunchCoroutine != null) StopCoroutine(_scorePunchCoroutine);
                    _scorePunchCoroutine = StartCoroutine(PunchScoreText());
                    
                    if (pointsDelta >= 30)
                    {
                        if (_deltaPopupCoroutine != null) StopCoroutine(_deltaPopupCoroutine);
                        _deltaPopupCoroutine = StartCoroutine(ShowDeltaPopup(pointsDelta));
                    }
                }
            }

            if (_highScoreText != null)
            {
                _highScoreText.text = $"BEST  {highScore:N0}";
            }
        }

        public void UpdateComboState(int comboStreak, int graceRemaining, float pitch)
        {
            if (comboStreak <= 0)
            {
                if (_comboCanvasGroup != null && _comboCanvasGroup.alpha > 0f)
                {
                    StartCoroutine(FadeOutCombo());
                }
                return;
            }

            if (_comboText != null)
            {
                string hypeTitle;
                Color hypeColor;

                if (comboStreak == 1) { hypeTitle = "COMBO ×1"; hypeColor = new Color(0.35f, 0.88f, 1.0f); }
                else if (comboStreak == 2) { hypeTitle = "COMBO ×2!"; hypeColor = new Color(1.0f, 0.82f, 0.20f); } // Warm Gold
                else if (comboStreak == 3) { hypeTitle = "GREAT! ×3"; hypeColor = new Color(1.0f, 0.60f, 0.10f); } // Electric Amber
                else if (comboStreak == 4) { hypeTitle = "AMAZING! ×4"; hypeColor = new Color(1.0f, 0.35f, 0.50f); } // Neon Coral
                else if (comboStreak == 5) { hypeTitle = "UNSTOPPABLE! ×5"; hypeColor = new Color(1.0f, 0.15f, 0.65f); } // Magenta
                else if (comboStreak == 6) { hypeTitle = "INCREDIBLE! ×6"; hypeColor = new Color(0.20f, 0.95f, 0.85f); } // Electric Cyan
                else { hypeTitle = $"POLYFUSE GOD! ×{comboStreak}"; hypeColor = new Color(0.85f, 0.40f, 1.0f); } // Prismatic Purple

                _comboText.text = hypeTitle;
                _comboText.color = hypeColor;
            }

            if (_comboPipsText != null)
            {
                string pips = "";
                for (int i = 0; i < 3; i++)
                {
                    pips += (i < graceRemaining) ? "● " : "○ ";
                }
                _comboPipsText.text = pips.TrimEnd();

                if (graceRemaining <= 1)
                {
                    _comboPipsText.color = new Color(0.96f, 0.35f, 0.42f, 1f); // Warning red on 1 piece left
                }
                else
                {
                    _comboPipsText.color = new Color(1.0f, 0.88f, 0.40f, 0.95f);
                }
            }

            if (_comboAnimCoroutine != null) StopCoroutine(_comboAnimCoroutine);
            _comboAnimCoroutine = StartCoroutine(AnimateComboBanner());
        }

        private IEnumerator PunchScoreText()
        {
            if (_scoreText == null) yield break;
            Transform t = _scoreText.transform;
            Vector3 normalScale = Vector3.one;
            Vector3 punchScale = Vector3.one * 1.18f;

            float elapsed = 0f;
            float punchDur = 0.08f;
            while (elapsed < punchDur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(normalScale, punchScale, elapsed / punchDur);
                yield return null;
            }

            elapsed = 0f;
            float settleDur = 0.12f;
            while (elapsed < settleDur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(punchScale, normalScale, elapsed / settleDur);
                yield return null;
            }
            t.localScale = normalScale;
            _scorePunchCoroutine = null;
        }

        private IEnumerator ShowDeltaPopup(int delta)
        {
            if (_scoreDeltaPopup == null) yield break;
            _scoreDeltaPopup.gameObject.SetActive(true);
            _scoreDeltaPopup.text = $"+{delta:N0}";
            
            RectTransform rt = _scoreDeltaPopup.GetComponent<RectTransform>();
            Vector2 startPos = new Vector2(0f, 0f);
            Vector2 endPos = new Vector2(0f, 60f);
            rt.anchoredPosition = startPos;

            Color c = new Color(0.20f, 0.90f, 1.0f, 1f);
            _scoreDeltaPopup.color = c;

            float elapsed = 0f;
            float duration = 0.7f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
                c.a = Mathf.Lerp(1f, 0f, progress * progress);
                _scoreDeltaPopup.color = c;
                yield return null;
            }

            _scoreDeltaPopup.gameObject.SetActive(false);
            _deltaPopupCoroutine = null;
        }

        private IEnumerator AnimateComboBanner()
        {
            if (_comboCanvasGroup == null) yield break;

            _comboCanvasGroup.alpha = 1f;
            Transform t = _comboCanvasGroup.transform;
            Vector3 startScale = Vector3.one * 0.80f;
            Vector3 popScale = Vector3.one * 1.22f;
            Vector3 normalScale = Vector3.one;

            float elapsed = 0f;
            float popDur = 0.12f;
            while (elapsed < popDur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(startScale, popScale, elapsed / popDur);
                yield return null;
            }

            elapsed = 0f;
            float settleDur = 0.10f;
            while (elapsed < settleDur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(popScale, normalScale, elapsed / settleDur);
                yield return null;
            }
            t.localScale = normalScale;
            _comboAnimCoroutine = null;
        }

        private IEnumerator FadeOutCombo()
        {
            if (_comboCanvasGroup == null) yield break;
            float startAlpha = _comboCanvasGroup.alpha;
            float elapsed = 0f;
            float fadeDur = 0.25f;

            while (elapsed < fadeDur)
            {
                elapsed += Time.unscaledDeltaTime;
                _comboCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDur);
                yield return null;
            }
            _comboCanvasGroup.alpha = 0f;
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
