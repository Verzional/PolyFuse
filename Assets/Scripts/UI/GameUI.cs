using System;
using System.Collections;
using PolyFuse.Juice;
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

        [Header("Settings & Celebrations")]
        [SerializeField] private GameObject _settingsModalPanel;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Text _celebrationBannerText;
        [SerializeField] private CanvasGroup _celebrationCanvasGroup;

        private Font _uiFont;
        private Coroutine _scorePunchCoroutine;
        private Coroutine _deltaPopupCoroutine;
        private Coroutine _comboAnimCoroutine;
        private Coroutine _celebrationCoroutine;

        private RectTransform _headerRt;
        private RectTransform _settingsBtnRt;
        private RectTransform _comboRt;
        private RectTransform _popupRt;
        private RectTransform _celebrationRt;
        private Rect _lastSafeArea;

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

            ApplySafeAreaInsets();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
            {
                ApplySafeAreaInsets();
            }
        }

        private void ApplySafeAreaInsets()
        {
            Rect safe = Screen.safeArea;
            if (safe == _lastSafeArea && _headerRt != null && _headerRt.anchoredPosition.y < 0) return;
            _lastSafeArea = safe;

            float screenH = Screen.height;
            if (screenH <= 0) return;

            // Inset from top in screen pixels
            float topInsetPixels = screenH - safe.yMax;
            float scaleFactor = 1920f / screenH;
            float topInsetCanvas = topInsetPixels * scaleFactor;

            // Safe top margin (Dynamic Island is ~59pt which scales to ~140px on canvas)
            float safeTopY = -Mathf.Max(25f, topInsetCanvas + 12f);

            if (_headerRt != null)
            {
                _headerRt.anchoredPosition = new Vector2(0f, safeTopY);
            }
            if (_settingsBtnRt != null)
            {
                _settingsBtnRt.anchoredPosition = new Vector2(-30f, safeTopY - 5f);
            }
            if (_comboRt != null)
            {
                _comboRt.anchoredPosition = new Vector2(0f, safeTopY - 175f);
            }
            if (_popupRt != null)
            {
                _popupRt.anchoredPosition = new Vector2(0f, safeTopY - 90f);
            }
            if (_celebrationRt != null)
            {
                _celebrationRt.anchoredPosition = new Vector2(0f, safeTopY - 260f);
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

        private static Sprite _gearSprite;

        public static Sprite GetGearSprite()
        {
            if (_gearSprite != null) return _gearSprite;

            int size = 128;
            float center = size * 0.5f;
            float rHole = 15f;
            float rHub = 36f;
            float rTeeth = 52f;
            int numTeeth = 6;
            float toothPeriod = (2f * Mathf.PI) / numTeeth;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] cols = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;

                    if (dist >= rHole && dist <= rTeeth)
                    {
                        if (dist <= rHub)
                        {
                            float innerEdge = Mathf.Clamp01((dist - rHole) / 1.5f);
                            alpha = innerEdge;
                        }
                        else
                        {
                            float angle = Mathf.Atan2(dy, dx);
                            if (angle < 0f) angle += 2f * Mathf.PI;
                            float modAngle = angle % toothPeriod;
                            float halfTooth = toothPeriod * 0.5f;
                            float angleDist = Mathf.Abs(modAngle - halfTooth * 0.5f);
                            float toothWidth = 0.52f * halfTooth;

                            if (angleDist < toothWidth)
                            {
                                float outerEdge = Mathf.Clamp01((rTeeth - dist) / 1.5f);
                                alpha = outerEdge;
                            }
                            else if (angleDist < toothWidth + 0.08f)
                            {
                                float sideFade = 1f - (angleDist - toothWidth) / 0.08f;
                                float outerEdge = Mathf.Clamp01((rTeeth - dist) / 1.5f);
                                alpha = Mathf.Clamp01(sideFade * outerEdge);
                            }
                        }
                    }

                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();

            _gearSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _gearSprite;
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
            _headerRt = headerObj.AddComponent<RectTransform>();
            _headerRt.anchorMin = new Vector2(0f, 1f);
            _headerRt.anchorMax = new Vector2(1f, 1f);
            _headerRt.pivot = new Vector2(0.5f, 1f);
            _headerRt.sizeDelta = new Vector2(0f, 165f);
            _headerRt.anchoredPosition = new Vector2(0f, -25f);

            // High Score Text (At very top)
            GameObject highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(_headerRt, false);
            _highScoreText = highScoreObj.AddComponent<Text>();
            _highScoreText.font = font;
            _highScoreText.raycastTarget = false;
            _highScoreText.text = "BEST  0";
            _highScoreText.fontSize = 28;
            _highScoreText.fontStyle = FontStyle.Bold;
            _highScoreText.alignment = TextAnchor.MiddleCenter;
            _highScoreText.color = new Color(0.65f, 0.78f, 0.95f, 1.0f);
            AddShadow(highScoreObj, new Color(0f, 0f, 0f, 0.6f), new Vector2(1.5f, -1.5f));
            RectTransform hsRt = highScoreObj.GetComponent<RectTransform>();
            hsRt.anchorMin = new Vector2(0f, 0.72f);
            hsRt.anchorMax = new Vector2(1f, 1.0f);
            hsRt.sizeDelta = Vector2.zero;

            // Score Text (Large bold number)
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(_headerRt, false);
            _scoreText = scoreObj.AddComponent<Text>();
            _scoreText.font = font;
            _scoreText.raycastTarget = false;
            _scoreText.text = "0";
            _scoreText.fontSize = 76;
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
            _scoreDeltaPopup.fontSize = 40;
            _scoreDeltaPopup.fontStyle = FontStyle.Bold;
            _scoreDeltaPopup.alignment = TextAnchor.MiddleCenter;
            _scoreDeltaPopup.color = new Color(0.20f, 0.90f, 1.0f, 1f);
            AddOutline(popupObj, new Color(0.02f, 0.05f, 0.10f, 0.9f), new Vector2(2f, -2f));
            _popupRt = popupObj.GetComponent<RectTransform>();
            _popupRt.anchorMin = new Vector2(0.5f, 1f);
            _popupRt.anchorMax = new Vector2(0.5f, 1f);
            _popupRt.pivot = new Vector2(0.5f, 0.5f);
            _popupRt.sizeDelta = new Vector2(400f, 60f);
            _popupRt.anchoredPosition = new Vector2(0f, -115f);
            popupObj.SetActive(false);

            // 2. Combo Banner Container (Directly below Score, completely above the board)
            GameObject comboObj = new GameObject("ComboBanner");
            comboObj.transform.SetParent(canvas.transform, false);
            _comboRt = comboObj.AddComponent<RectTransform>();
            _comboRt.anchorMin = new Vector2(0.5f, 1f);
            _comboRt.anchorMax = new Vector2(0.5f, 1f);
            _comboRt.pivot = new Vector2(0.5f, 1f);
            _comboRt.sizeDelta = new Vector2(500f, 80f);
            _comboRt.anchoredPosition = new Vector2(0f, -200f);
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
            _comboText.fontSize = 38;
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
            _comboPipsText.fontSize = 22;
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

            // 4. Top-Right Settings Icon Button
            GameObject setBtnObj = new GameObject("SettingsButton");
            setBtnObj.transform.SetParent(canvas.transform, false);
            _settingsBtnRt = setBtnObj.AddComponent<RectTransform>();
            _settingsBtnRt.anchorMin = new Vector2(1f, 1f);
            _settingsBtnRt.anchorMax = new Vector2(1f, 1f);
            _settingsBtnRt.pivot = new Vector2(1f, 1f);
            _settingsBtnRt.sizeDelta = new Vector2(74f, 74f);
            _settingsBtnRt.anchoredPosition = new Vector2(-30f, -25f);

            Image setImg = setBtnObj.AddComponent<Image>();
            setImg.color = new Color(0.12f, 0.15f, 0.22f, 0.9f);
            AddOutline(setBtnObj, new Color(0.25f, 0.40f, 0.60f, 0.6f), new Vector2(1.5f, -1.5f));
            _settingsButton = setBtnObj.AddComponent<Button>();

            GameObject setIconObj = new GameObject("GearIcon");
            setIconObj.transform.SetParent(setBtnObj.transform, false);
            Image gearImg = setIconObj.AddComponent<Image>();
            gearImg.sprite = GetGearSprite();
            gearImg.color = new Color(0.75f, 0.88f, 1.0f, 1.0f);
            gearImg.raycastTarget = false;
            RectTransform siRt = setIconObj.GetComponent<RectTransform>();
            siRt.anchorMin = Vector2.zero;
            siRt.anchorMax = Vector2.one;
            siRt.sizeDelta = new Vector2(-24f, -24f);

            _settingsButton.onClick.AddListener(ToggleSettingsModal);

            // 5. Celebration Banner (CLOSE CALL & NEW BEST)
            GameObject celebObj = new GameObject("CelebrationBanner");
            celebObj.transform.SetParent(canvas.transform, false);
            _celebrationRt = celebObj.AddComponent<RectTransform>();
            _celebrationRt.anchorMin = new Vector2(0.5f, 1f);
            _celebrationRt.anchorMax = new Vector2(0.5f, 1f);
            _celebrationRt.pivot = new Vector2(0.5f, 1f);
            _celebrationRt.sizeDelta = new Vector2(650f, 80f);
            _celebrationRt.anchoredPosition = new Vector2(0f, -280f);

            _celebrationCanvasGroup = celebObj.AddComponent<CanvasGroup>();
            _celebrationCanvasGroup.blocksRaycasts = false;
            _celebrationCanvasGroup.alpha = 0f;

            GameObject celebTextObj = new GameObject("CelebrationText");
            celebTextObj.transform.SetParent(celebObj.transform, false);
            _celebrationBannerText = celebTextObj.AddComponent<Text>();
            _celebrationBannerText.font = font;
            _celebrationBannerText.raycastTarget = false;
            _celebrationBannerText.text = "★ NEW RECORD! ★";
            _celebrationBannerText.fontSize = 40;
            _celebrationBannerText.fontStyle = FontStyle.Bold;
            _celebrationBannerText.alignment = TextAnchor.MiddleCenter;
            _celebrationBannerText.color = new Color(1.0f, 0.85f, 0.20f, 1.0f);
            AddOutline(celebTextObj, new Color(0.15f, 0.10f, 0.02f, 0.95f), new Vector2(2f, -2f));
            RectTransform ctRt = celebTextObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;

            // 6. Settings Modal Panel
            BuildSettingsModal(canvas.transform, font);
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

        private Text _soundBtnLabel;
        private Text _hapticsBtnLabel;

        private void BuildSettingsModal(Transform parent, Font font)
        {
            _settingsModalPanel = new GameObject("SettingsModalPanel");
            _settingsModalPanel.transform.SetParent(parent, false);
            RectTransform smRt = _settingsModalPanel.AddComponent<RectTransform>();
            smRt.anchorMin = Vector2.zero;
            smRt.anchorMax = Vector2.one;
            smRt.sizeDelta = Vector2.zero;

            Image smBg = _settingsModalPanel.AddComponent<Image>();
            smBg.color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            // Dialog Card
            GameObject cardObj = new GameObject("Card");
            cardObj.transform.SetParent(_settingsModalPanel.transform, false);
            RectTransform cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(620f, 620f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.10f, 0.12f, 0.18f, 1.0f);
            AddOutline(cardObj, new Color(0.20f, 0.85f, 1.0f, 0.6f), new Vector2(2f, -2f));

            // Title
            GameObject titleObj = new GameObject("SettingsTitle");
            titleObj.transform.SetParent(cardObj.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.font = font;
            title.text = "PAUSED";
            title.fontSize = 42;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(0f, 90f);
            tRt.anchoredPosition = new Vector2(0f, -15f);

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseButton");
            closeBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.sizeDelta = new Vector2(60f, 60f);
            cbRt.anchoredPosition = new Vector2(-15f, -15f);
            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseSettingsModal);

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            Text closeTxt = closeTxtObj.AddComponent<Text>();
            closeTxt.font = font;
            closeTxt.text = "✕";
            closeTxt.fontSize = 32;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = new Color(0.6f, 0.7f, 0.85f, 1f);
            RectTransform ctRt = closeTxtObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;

            // Buttons Container with VerticalLayoutGroup for identical spacing
            GameObject btnContainer = new GameObject("ButtonsContainer");
            btnContainer.transform.SetParent(cardObj.transform, false);
            RectTransform bcRt = btnContainer.AddComponent<RectTransform>();
            bcRt.anchorMin = Vector2.zero;
            bcRt.anchorMax = Vector2.one;
            bcRt.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = btnContainer.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(45, 45, 105, 35);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Button 1: Sound FX Toggle
            CreateSettingsOptionButton(btnContainer.transform, font, "SOUND: ON", out _soundBtnLabel, () =>
            {
                ProceduralAudio audio = FindFirstObjectByType<ProceduralAudio>();
                if (audio != null)
                {
                    audio.ToggleSound();
                    UpdateSettingsButtonLabels();
                }
            });

            // Button 2: Haptics Toggle
            CreateSettingsOptionButton(btnContainer.transform, font, "HAPTICS: ON", out _hapticsBtnLabel, () =>
            {
                PolyFuse.Juice.HapticFeedbackManager.Instance?.ToggleHaptics();
                UpdateSettingsButtonLabels();
            });

            // Button 3: Restart Run
            Text dummy;
            CreateSettingsOptionButton(btnContainer.transform, font, "↺ RESTART RUN", out dummy, () =>
            {
                CloseSettingsModal();
                OnRestartRequested?.Invoke();
            }, new Color(0.96f, 0.35f, 0.45f, 1.0f));

            // Button 4: Resume
            CreateSettingsOptionButton(btnContainer.transform, font, "▶ RESUME", out dummy, () =>
            {
                CloseSettingsModal();
            }, new Color(0.20f, 0.85f, 0.95f, 1.0f));

            _settingsModalPanel.SetActive(false);
            UpdateSettingsButtonLabels();
        }

        private void CreateSettingsOptionButton(Transform parent, Font font, string initialText, out Text labelText, Action onClick, Color? btnColor = null)
        {
            GameObject btnObj = new GameObject("OptButton");
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = btnColor ?? new Color(0.18f, 0.22f, 0.32f, 1.0f);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 92f;
            le.preferredHeight = 92f;
            le.flexibleHeight = 0f;

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btnObj.transform, false);
            labelText = lblObj.AddComponent<Text>();
            labelText.font = font;
            labelText.raycastTarget = false;
            labelText.text = initialText;
            labelText.fontSize = 28;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = btnColor.HasValue ? new Color(0.06f, 0.07f, 0.10f, 1.0f) : Color.white;

            RectTransform lRt = lblObj.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
        }

        private void UpdateSettingsButtonLabels()
        {
            ProceduralAudio audio = FindFirstObjectByType<ProceduralAudio>();
            if (_soundBtnLabel != null && audio != null)
            {
                _soundBtnLabel.text = audio.IsSoundEnabled ? "SOUND FX:  ON" : "SOUND FX:  MUTED";
            }
            if (_hapticsBtnLabel != null && PolyFuse.Juice.HapticFeedbackManager.Instance != null)
            {
                _hapticsBtnLabel.text = PolyFuse.Juice.HapticFeedbackManager.Instance.IsHapticsEnabled ? "HAPTICS:  ON" : "HAPTICS:  OFF";
            }
        }

        public void ToggleSettingsModal()
        {
            if (_settingsModalPanel == null) return;
            bool active = !_settingsModalPanel.activeSelf;
            _settingsModalPanel.SetActive(active);
            Time.timeScale = active ? 0.0f : 1.0f;
            if (active) UpdateSettingsButtonLabels();
        }

        public void CloseSettingsModal()
        {
            if (_settingsModalPanel != null)
            {
                _settingsModalPanel.SetActive(false);
            }
            Time.timeScale = 1.0f;
        }

        public void ShowCloseCallBanner()
        {
            if (_celebrationCoroutine != null) StopCoroutine(_celebrationCoroutine);
            _celebrationCoroutine = StartCoroutine(AnimateCelebrationBanner("HEROIC CLEAR!", new Color(1.0f, 0.40f, 0.55f, 1.0f)));
        }

        public void ShowNewHighScoreBanner(int newRecord)
        {
            if (_highScoreText != null)
            {
                _highScoreText.color = new Color(1.0f, 0.88f, 0.20f, 1.0f);
            }
            if (_celebrationCoroutine != null) StopCoroutine(_celebrationCoroutine);
            _celebrationCoroutine = StartCoroutine(AnimateCelebrationBanner("★ NEW BEST! ★", new Color(1.0f, 0.85f, 0.20f, 1.0f)));
        }

        private IEnumerator AnimateCelebrationBanner(string message, Color color)
        {
            if (_celebrationCanvasGroup == null || _celebrationBannerText == null) yield break;

            _celebrationBannerText.text = message;
            _celebrationBannerText.color = color;
            _celebrationCanvasGroup.alpha = 1f;

            Transform t = _celebrationCanvasGroup.transform;
            Vector3 startScale = Vector3.one * 0.70f;
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

            yield return new WaitForSecondsRealtime(1.1f);

            elapsed = 0f;
            float fadeDur = 0.3f;
            while (elapsed < fadeDur)
            {
                elapsed += Time.unscaledDeltaTime;
                _celebrationCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDur);
                yield return null;
            }

            _celebrationCanvasGroup.alpha = 0f;
            _celebrationCoroutine = null;
        }
    }
}
