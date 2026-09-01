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

        private RectTransform _highScoreRt;
        private RectTransform _scoreRt;
        private RectTransform _settingsBtnRt;
        private RectTransform _comboRt;
        private RectTransform _popupRt;
        private RectTransform _celebrationRt;
        private Rect _lastSafeArea;
        private int _maxGraceInStreak = 3;

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

            ApplySafeArea(Screen.safeArea);
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea)
            {
                ApplySafeArea(Screen.safeArea);
            }
        }

        private void ApplySafeArea(Rect safe)
        {
            _lastSafeArea = safe;

            float screenH = Screen.height;
            if (screenH <= 0) return;

            // Inset from top in screen pixels
            float topInsetPixels = screenH - safe.yMax;
            float scaleFactor = 1920f / screenH;
            float topInsetCanvas = topInsetPixels * scaleFactor;

            // Safe top margin (Dynamic Island is ~59pt which scales to ~140px on canvas)
            float safeTopY = -Mathf.Max(25f, topInsetCanvas + 12f);

            if (_highScoreRt != null)
            {
                _highScoreRt.anchoredPosition = new Vector2(36f, safeTopY - 14f);
            }
            if (_settingsBtnRt != null)
            {
                _settingsBtnRt.anchoredPosition = new Vector2(-36f, safeTopY - 14f);
            }
            if (_scoreRt != null)
            {
                _scoreRt.anchoredPosition = new Vector2(0f, safeTopY - 10f);
            }
            if (_comboRt != null)
            {
                _comboRt.anchoredPosition = new Vector2(0f, safeTopY - 100f);
            }
            if (_popupRt != null)
            {
                _popupRt.anchoredPosition = new Vector2(0f, safeTopY - 55f);
            }
            if (_celebrationRt != null)
            {
                _celebrationRt.anchoredPosition = new Vector2(0f, safeTopY - 170f);
            }
        }

        private Font GetSystemFont()
        {
            if (_uiFont != null) return _uiFont;

            // 1. Load bespoke bundled game font
            _uiFont = Resources.Load<Font>("PolyFuse-MainFont");
            if (_uiFont == null) _uiFont = Resources.Load<Font>("Fonts/PolyFuse-MainFont");

            // 2. OS Dynamic font lookups
            if (_uiFont == null)
            {
                string[] fontNames = new[] { "DIN Alternate", "DIN Alternate Bold", "Futura-Bold", "Futura", "Trebuchet MS", "Helvetica Neue", "Arial" };
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

            // 3. Unity built-in fallback
            if (_uiFont == null) _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_uiFont == null) _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return _uiFont;
        }

        private static Sprite _subtleCardSprite;
        private static Sprite _subtleRowSprite;
        private static Sprite _subtleBtnSprite;
        private static Sprite _toggleTrackSprite;
        private static Sprite _pauseSprite;
        private static Sprite _circleSprite;

        public static Sprite GetSubtleCardSprite()
        {
            if (_subtleCardSprite != null) return _subtleCardSprite;
            _subtleCardSprite = CreateRoundedRectSprite(128, 14, new Vector4(20, 20, 20, 20));
            return _subtleCardSprite;
        }

        public static Sprite GetSubtleRowSprite()
        {
            if (_subtleRowSprite != null) return _subtleRowSprite;
            _subtleRowSprite = CreateRoundedRectSprite(128, 10, new Vector4(16, 16, 16, 16));
            return _subtleRowSprite;
        }

        public static Sprite GetSubtleButtonSprite()
        {
            if (_subtleBtnSprite != null) return _subtleBtnSprite;
            _subtleBtnSprite = CreateRoundedRectSprite(128, 12, new Vector4(18, 18, 18, 18));
            return _subtleBtnSprite;
        }

        public static Sprite GetToggleTrackSprite()
        {
            if (_toggleTrackSprite != null) return _toggleTrackSprite;
            _toggleTrackSprite = CreateRoundedRectSprite(128, 32, new Vector4(34, 34, 34, 34));
            return _toggleTrackSprite;
        }

        private static Sprite CreateRoundedRectSprite(int size, int radius, Vector4 border)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[size * size];

            float r = radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float cx = (px < r) ? r : ((px > size - r) ? size - r : px);
                    float cy = (py < r) ? r : ((py > size - r) ? size - r : py);

                    float dx = px - cx;
                    float dy = py - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 1f;
                    if (px < r || px > size - r || py < r || py > size - r)
                    {
                        alpha = Mathf.Clamp01((r - dist) + 0.5f);
                    }

                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        public static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[size * size];

            float center = size * 0.5f;
            float r = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((r - dist) + 0.5f);
                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _circleSprite;
        }

        public static Sprite GetPauseSprite()
        {
            if (_pauseSprite != null) return _pauseSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[size * size];

            float barWidth = 14f;
            float barHeight = 44f;
            float gap = 16f;
            float center = size * 0.5f;

            float leftBarMinX = center - (gap * 0.5f) - barWidth;
            float leftBarMaxX = center - (gap * 0.5f);
            float rightBarMinX = center + (gap * 0.5f);
            float rightBarMaxX = center + (gap * 0.5f) + barWidth;
            float barMinY = center - (barHeight * 0.5f);
            float barMaxY = center + (barHeight * 0.5f);
            float r = 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float alpha = 0f;

                    if (px >= leftBarMinX - 1f && px <= leftBarMaxX + 1f && py >= barMinY - 1f && py <= barMaxY + 1f)
                    {
                        float cx = Mathf.Clamp(px, leftBarMinX + r, leftBarMaxX - r);
                        float cy = Mathf.Clamp(py, barMinY + r, barMaxY - r);
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                        alpha = Mathf.Clamp01((r - dist) + 0.5f);
                    }
                    else if (px >= rightBarMinX - 1f && px <= rightBarMaxX + 1f && py >= barMinY - 1f && py <= barMaxY + 1f)
                    {
                        float cx = Mathf.Clamp(px, rightBarMinX + r, rightBarMaxX - r);
                        float cy = Mathf.Clamp(py, barMinY + r, barMaxY - r);
                        float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                        alpha = Mathf.Clamp01((r - dist) + 0.5f);
                    }

                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();

            _pauseSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _pauseSprite;
        }

        private static Sprite _gearSprite;

        public static Sprite GetGearSprite()
        {
            if (_gearSprite != null) return _gearSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[size * size];

            float center = size * 0.5f;
            float rHole = size * 0.14f;
            float rHub = size * 0.28f;
            float rTeeth = size * 0.42f;
            int numTeeth = 6;
            float toothPeriod = (2f * Mathf.PI) / numTeeth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 0f;

                    if (dist >= rHole - 1f && dist <= rTeeth + 1f)
                    {
                        float innerAlpha = Mathf.Clamp01((dist - (rHole - 0.5f)));

                        if (dist <= rHub)
                        {
                            alpha = innerAlpha;
                        }
                        else
                        {
                            float angle = Mathf.Atan2(dy, dx);
                            if (angle < 0f) angle += 2f * Mathf.PI;
                            float modAngle = angle % toothPeriod;
                            float halfTooth = toothPeriod * 0.5f;
                            float angleDist = Mathf.Abs(modAngle - halfTooth);
                            float toothWidth = 0.40f * halfTooth;

                            if (angleDist < toothWidth)
                            {
                                float outerAlpha = Mathf.Clamp01((rTeeth - dist) + 0.5f);
                                alpha = Mathf.Min(innerAlpha, outerAlpha);
                            }
                            else if (angleDist < toothWidth + 0.10f)
                            {
                                float sideAlpha = 1f - (angleDist - toothWidth) / 0.10f;
                                float hubAlpha = Mathf.Clamp01((rHub - dist) + 0.5f);
                                float outerAlpha = Mathf.Clamp01((rTeeth - dist) + 0.5f);
                                alpha = Mathf.Max(hubAlpha, sideAlpha * outerAlpha) * innerAlpha;
                            }
                            else
                            {
                                float hubAlpha = Mathf.Clamp01((rHub - dist) + 0.5f);
                                alpha = hubAlpha * innerAlpha;
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

            // 1. High Score Text (Top-Left - Clean floating gold text)
            GameObject highScoreObj = new GameObject("HighScoreText");
            highScoreObj.transform.SetParent(canvas.transform, false);
            _highScoreText = highScoreObj.AddComponent<Text>();
            _highScoreText.font = font;
            _highScoreText.raycastTarget = false;
            _highScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _highScoreText.verticalOverflow = VerticalWrapMode.Overflow;
            _highScoreText.text = "★ 0";
            _highScoreText.fontSize = 46;
            _highScoreText.fontStyle = FontStyle.Bold;
            _highScoreText.alignment = TextAnchor.MiddleLeft;
            _highScoreText.color = new Color(1.0f, 0.82f, 0.20f, 1.0f);
            AddShadow(highScoreObj, new Color(0f, 0f, 0f, 0.7f), new Vector2(2f, -2f));
            _highScoreRt = highScoreObj.GetComponent<RectTransform>();
            _highScoreRt.anchorMin = new Vector2(0f, 1f);
            _highScoreRt.anchorMax = new Vector2(0f, 1f);
            _highScoreRt.pivot = new Vector2(0f, 1f);
            _highScoreRt.sizeDelta = new Vector2(380f, 64f);
            _highScoreRt.anchoredPosition = new Vector2(36f, -28f);

            // 2. Current Score (Top-Mid Center)
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(canvas.transform, false);
            _scoreText = scoreObj.AddComponent<Text>();
            _scoreText.font = font;
            _scoreText.raycastTarget = false;
            _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _scoreText.verticalOverflow = VerticalWrapMode.Overflow;
            _scoreText.text = "0";
            _scoreText.fontSize = 94;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.alignment = TextAnchor.MiddleCenter;
            _scoreText.color = Color.white;
            AddOutline(scoreObj, new Color(0.04f, 0.06f, 0.10f, 0.85f), new Vector2(2.5f, -2.5f));
            _scoreRt = scoreObj.GetComponent<RectTransform>();
            _scoreRt.anchorMin = new Vector2(0.5f, 1f);
            _scoreRt.anchorMax = new Vector2(0.5f, 1f);
            _scoreRt.pivot = new Vector2(0.5f, 1f);
            _scoreRt.sizeDelta = new Vector2(420f, 100f);
            _scoreRt.anchoredPosition = new Vector2(0f, -14f);

            // Score Delta Popup (Floating +300)
            GameObject popupObj = new GameObject("ScoreDeltaPopup");
            popupObj.transform.SetParent(canvas.transform, false);
            _scoreDeltaPopup = popupObj.AddComponent<Text>();
            _scoreDeltaPopup.font = font;
            _scoreDeltaPopup.raycastTarget = false;
            _scoreDeltaPopup.horizontalOverflow = HorizontalWrapMode.Overflow;
            _scoreDeltaPopup.verticalOverflow = VerticalWrapMode.Overflow;
            _scoreDeltaPopup.text = "+150";
            _scoreDeltaPopup.fontSize = 44;
            _scoreDeltaPopup.fontStyle = FontStyle.Bold;
            _scoreDeltaPopup.alignment = TextAnchor.MiddleCenter;
            _scoreDeltaPopup.color = new Color(0.20f, 0.90f, 1.0f, 1f);
            AddOutline(popupObj, new Color(0.02f, 0.05f, 0.10f, 0.9f), new Vector2(2f, -2f));
            _popupRt = popupObj.GetComponent<RectTransform>();
            _popupRt.anchorMin = new Vector2(0.5f, 1f);
            _popupRt.anchorMax = new Vector2(0.5f, 1f);
            _popupRt.pivot = new Vector2(0.5f, 0.5f);
            _popupRt.sizeDelta = new Vector2(300f, 50f);
            _popupRt.anchoredPosition = new Vector2(0f, -65f);
            popupObj.SetActive(false);

            // 3. Combo Count (Dropped down comfortably with clean breathing room)
            GameObject comboObj = new GameObject("ComboBanner");
            comboObj.transform.SetParent(canvas.transform, false);
            _comboRt = comboObj.AddComponent<RectTransform>();
            _comboRt.anchorMin = new Vector2(0.5f, 1f);
            _comboRt.anchorMax = new Vector2(0.5f, 1f);
            _comboRt.pivot = new Vector2(0.5f, 1f);
            _comboRt.sizeDelta = new Vector2(640f, 74f);
            _comboRt.anchoredPosition = new Vector2(0f, -185f);
            _comboCanvasGroup = comboObj.AddComponent<CanvasGroup>();
            _comboCanvasGroup.blocksRaycasts = false;
            _comboCanvasGroup.alpha = 0f;

            GameObject comboTextObj = new GameObject("ComboText");
            comboTextObj.transform.SetParent(comboObj.transform, false);
            _comboText = comboTextObj.AddComponent<Text>();
            _comboText.font = font;
            _comboText.supportRichText = true;
            _comboText.raycastTarget = false;
            _comboText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _comboText.verticalOverflow = VerticalWrapMode.Overflow;
            _comboText.fontSize = 46;
            _comboText.fontStyle = FontStyle.Bold;
            _comboText.alignment = TextAnchor.MiddleCenter;
            _comboText.color = Color.white;
            AddOutline(comboTextObj, new Color(0.04f, 0.06f, 0.10f, 0.8f), new Vector2(1.5f, -1.5f));
            RectTransform ctRt = comboTextObj.GetComponent<RectTransform>();
            ctRt.anchorMin = Vector2.zero;
            ctRt.anchorMax = Vector2.one;
            ctRt.sizeDelta = Vector2.zero;

            // 4. Game Over Panel
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform goRt = _gameOverPanel.AddComponent<RectTransform>();
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.sizeDelta = Vector2.zero;

            Image goBg = _gameOverPanel.AddComponent<Image>();
            goBg.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);

            // Content Box (Dark Obsidian Glass Card)
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(_gameOverPanel.transform, false);
            RectTransform cRt = contentBox.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(560f, 480f);

            Image cbBg = contentBox.AddComponent<Image>();
            cbBg.color = new Color(0.08f, 0.10f, 0.16f, 0.96f);
            AddOutline(contentBox, new Color(0.96f, 0.35f, 0.45f, 0.45f), new Vector2(1.5f, -1.5f));
            AddShadow(contentBox, new Color(0f, 0f, 0f, 0.75f), new Vector2(6f, -6f));

            // Title
            GameObject titleObj = new GameObject("GameOverTitle");
            titleObj.transform.SetParent(contentBox.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.raycastTarget = false;
            titleText.text = "GAME OVER";
            titleText.fontSize = 46;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(0.96f, 0.35f, 0.45f, 1f);
            AddOutline(titleObj, new Color(0.04f, 0.06f, 0.10f, 0.90f), new Vector2(2f, -2f));
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.68f);
            tRt.anchorMax = new Vector2(1f, 0.95f);
            tRt.sizeDelta = Vector2.zero;

            // Final Score
            GameObject finalScoreObj = new GameObject("FinalScoreText");
            finalScoreObj.transform.SetParent(contentBox.transform, false);
            _finalScoreText = finalScoreObj.AddComponent<Text>();
            _finalScoreText.font = font;
            _finalScoreText.raycastTarget = false;
            _finalScoreText.text = "SCORE\n0";
            _finalScoreText.fontSize = 40;
            _finalScoreText.fontStyle = FontStyle.Bold;
            _finalScoreText.alignment = TextAnchor.MiddleCenter;
            _finalScoreText.color = Color.white;
            AddOutline(finalScoreObj, new Color(0.04f, 0.06f, 0.10f, 0.90f), new Vector2(1.5f, -1.5f));
            RectTransform fsRt = finalScoreObj.GetComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0f, 0.32f);
            fsRt.anchorMax = new Vector2(1f, 0.68f);
            fsRt.sizeDelta = Vector2.zero;

            // Restart Button (Electric Cyan)
            GameObject btnObj = new GameObject("RestartButton");
            btnObj.transform.SetParent(contentBox.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.82f, 0.98f, 1f);
            _restartButton = btnObj.AddComponent<Button>();
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.12f, 0.08f);
            btnRt.anchorMax = new Vector2(0.88f, 0.26f);
            btnRt.sizeDelta = Vector2.zero;

            GameObject btnLabelObj = new GameObject("BtnLabel");
            btnLabelObj.transform.SetParent(btnObj.transform, false);
            Text btnLabel = btnLabelObj.AddComponent<Text>();
            btnLabel.font = font;
            btnLabel.raycastTarget = false;
            btnLabel.text = "▶  PLAY AGAIN";
            btnLabel.fontSize = 28;
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color = new Color(0.04f, 0.07f, 0.12f, 1.0f);
            RectTransform blRt = btnLabelObj.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.sizeDelta = Vector2.zero;

            // 5. Top-Right Floating Settings Gear Button (Naked icon, no outer box)
            GameObject setBtnObj = new GameObject("SettingsButton");
            setBtnObj.transform.SetParent(canvas.transform, false);
            _settingsBtnRt = setBtnObj.AddComponent<RectTransform>();
            _settingsBtnRt.anchorMin = new Vector2(1f, 1f);
            _settingsBtnRt.anchorMax = new Vector2(1f, 1f);
            _settingsBtnRt.pivot = new Vector2(1f, 1f);
            _settingsBtnRt.sizeDelta = new Vector2(72f, 72f);
            _settingsBtnRt.anchoredPosition = new Vector2(-36f, -26f);

            Image setHitbox = setBtnObj.AddComponent<Image>();
            setHitbox.color = Color.clear; // Invisible touch hitbox
            setHitbox.raycastTarget = true;
            _settingsButton = setBtnObj.AddComponent<Button>();

            GameObject setIconObj = new GameObject("GearIcon");
            setIconObj.transform.SetParent(setBtnObj.transform, false);
            Image gearImg = setIconObj.AddComponent<Image>();
            gearImg.sprite = GetGearSprite();
            gearImg.color = new Color(0.85f, 0.90f, 0.98f, 0.90f);
            gearImg.raycastTarget = false;
            AddShadow(setIconObj, new Color(0f, 0f, 0f, 0.70f), new Vector2(2f, -2f));
            RectTransform siRt = setIconObj.GetComponent<RectTransform>();
            siRt.anchorMin = new Vector2(0.5f, 0.5f);
            siRt.anchorMax = new Vector2(0.5f, 0.5f);
            siRt.pivot = new Vector2(0.5f, 0.5f);
            siRt.sizeDelta = new Vector2(52f, 52f);

            _settingsButton.onClick.AddListener(ToggleSettingsModal);

            // 6. Celebration Banner (CLOSE CALL & NEW BEST)
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
            RectTransform clbRt = celebTextObj.GetComponent<RectTransform>();
            clbRt.anchorMin = Vector2.zero;
            clbRt.anchorMax = Vector2.one;
            clbRt.sizeDelta = Vector2.zero;

            // 7. Settings Modal Panel
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
                _highScoreText.text = $"★ {highScore:N0}";
            }
        }

        private IEnumerator ShowDeltaPopup(int points)
        {
            if (_scoreDeltaPopup == null) yield break;

            _scoreDeltaPopup.text = $"+{points:N0}";
            _scoreDeltaPopup.gameObject.SetActive(true);

            Vector2 startPos = new Vector2(0f, -65f);
            Vector2 endPos = new Vector2(0f, -125f);
            Color startColor = new Color(0.20f, 0.90f, 1.0f, 1f);
            Color endColor = new Color(0.20f, 0.90f, 1.0f, 0f);

            float elapsed = 0f;
            float dur = 0.55f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                _popupRt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                _scoreDeltaPopup.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            _scoreDeltaPopup.gameObject.SetActive(false);
            _deltaPopupCoroutine = null;
        }

        private IEnumerator PunchScoreText()
        {
            if (_scoreRt == null) yield break;

            Vector3 startScale = Vector3.one;
            Vector3 peakScale = Vector3.one * 1.15f;

            float elapsed = 0f;
            float dur = 0.12f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                _scoreRt.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            _scoreRt.localScale = Vector3.one;
            _scorePunchCoroutine = null;
        }

        public void UpdateComboState(int comboStreak, int graceRemaining, float pitch)
        {
            if (comboStreak <= 0)
            {
                _maxGraceInStreak = 3;
                if (_comboCanvasGroup != null && _comboCanvasGroup.alpha > 0f)
                {
                    StartCoroutine(FadeOutCombo());
                }
                return;
            }

            _maxGraceInStreak = Mathf.Max(_maxGraceInStreak, graceRemaining);

            if (_comboText != null)
            {
                Color hypeColor;
                if (comboStreak == 1) hypeColor = new Color(0.22f, 0.74f, 0.97f, 1.0f); // Sky Cyan
                else if (comboStreak == 2) hypeColor = new Color(0.98f, 0.75f, 0.14f, 1.0f); // Warm Gold
                else if (comboStreak == 3) hypeColor = new Color(0.96f, 0.62f, 0.04f, 1.0f); // Electric Amber
                else if (comboStreak == 4) hypeColor = new Color(0.96f, 0.25f, 0.37f, 1.0f); // Neon Coral
                else if (comboStreak == 5) hypeColor = new Color(0.85f, 0.27f, 0.94f, 1.0f); // Magenta
                else if (comboStreak == 6) hypeColor = new Color(0.02f, 0.71f, 0.83f, 1.0f); // Electric Cyan
                else hypeColor = new Color(0.66f, 0.33f, 0.97f, 1.0f); // Prismatic Purple

                int totalPips = Mathf.Max(3, _maxGraceInStreak);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                // Format: e.g. "‹ 3× ›   ▲  ▲  ▲   ‹ 3× ›"
                sb.Append($"<size=44><b>‹ {comboStreak}× ›</b></size>   ");

                for (int i = 0; i < totalPips; i++)
                {
                    if (i < graceRemaining)
                    {
                        if (graceRemaining <= 1)
                        {
                            sb.Append("<size=38><color=#FF3366>▲</color></size> "); // Urgent red danger triangle
                        }
                        else
                        {
                            sb.Append("<size=38>▲</size> "); // Glowing isometric triangle matching tier color
                        }
                    }
                    else
                    {
                        sb.Append("<size=38><color=#475569>△</color></size> "); // Hollow/extinguished wireframe triangle
                    }
                }

                sb.Append($"  <size=44><b>‹ {comboStreak}× ›</b></size>");

                _comboText.text = sb.ToString();
                _comboText.color = hypeColor;
            }

            if (_comboAnimCoroutine != null) StopCoroutine(_comboAnimCoroutine);
            _comboAnimCoroutine = StartCoroutine(AnimateComboBanner());
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

        private Text _soundOptionText;
        private Text _hapticsOptionText;

        private void BuildSettingsModal(Transform parent, Font font)
        {
            _settingsModalPanel = new GameObject("SettingsModalPanel");
            _settingsModalPanel.transform.SetParent(parent, false);
            RectTransform smRt = _settingsModalPanel.AddComponent<RectTransform>();
            smRt.anchorMin = Vector2.zero;
            smRt.anchorMax = Vector2.one;
            smRt.sizeDelta = Vector2.zero;

            // Fullscreen dark frosted dimming - Tapping backdrop resumes
            Image smBg = _settingsModalPanel.AddComponent<Image>();
            smBg.color = new Color(0.02f, 0.04f, 0.07f, 0.94f);
            Button bgDismiss = _settingsModalPanel.AddComponent<Button>();
            bgDismiss.onClick.AddListener(CloseSettingsModal);

            // Centered Vertical Container (No heavy box)
            GameObject listContainer = new GameObject("ZenListContainer");
            listContainer.transform.SetParent(_settingsModalPanel.transform, false);
            RectTransform lcRt = listContainer.AddComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0.5f, 0.5f);
            lcRt.anchorMax = new Vector2(0.5f, 0.5f);
            lcRt.pivot = new Vector2(0.5f, 0.5f);
            lcRt.sizeDelta = new Vector2(520f, 480f);

            VerticalLayoutGroup vlg = listContainer.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 24f; // Clean 24px gap between Title and Buttons
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Title: "PAUSED"
            GameObject titleObj = new GameObject("PauseTitle");
            titleObj.transform.SetParent(listContainer.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.font = font;
            title.text = "PAUSED";
            title.fontSize = 42;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.raycastTarget = false;
            AddShadow(titleObj, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.minHeight = 52f;
            tLe.preferredHeight = 52f;

            // 2. Buttons Stack Container (Guarantees 100% equal spacing between all 4 buttons)
            GameObject btnsObj = new GameObject("ButtonsStack");
            btnsObj.transform.SetParent(listContainer.transform, false);
            VerticalLayoutGroup bVlg = btnsObj.AddComponent<VerticalLayoutGroup>();
            bVlg.padding = new RectOffset(0, 0, 0, 0);
            bVlg.spacing = 16f; // Exactly 16px between every button!
            bVlg.childAlignment = TextAnchor.MiddleCenter;
            bVlg.childControlWidth = true;
            bVlg.childControlHeight = true;
            bVlg.childForceExpandWidth = true;
            bVlg.childForceExpandHeight = false;

            // Button 1: Sound FX Toggle
            CreateMinimalListButton(btnsObj.transform, font, "SOUND:  <color=#10B981>ON</color>", out _soundOptionText, () =>
            {
                ProceduralAudio audio = FindFirstObjectByType<ProceduralAudio>();
                if (audio != null)
                {
                    audio.ToggleSound();
                    UpdateSettingsButtonLabels();
                }
            }, new Color(0.10f, 0.14f, 0.22f, 0.85f), new Color(0.20f, 0.30f, 0.44f, 0.45f));

            // Button 2: Haptics Toggle
            CreateMinimalListButton(btnsObj.transform, font, "HAPTICS:  <color=#10B981>ON</color>", out _hapticsOptionText, () =>
            {
                PolyFuse.Juice.HapticFeedbackManager.Instance?.ToggleHaptics();
                UpdateSettingsButtonLabels();
            }, new Color(0.10f, 0.14f, 0.22f, 0.85f), new Color(0.20f, 0.30f, 0.44f, 0.45f));

            // Button 3: Restart Run (Vibrant Crimson with crisp white text)
            Text dummyRestart;
            CreateMinimalListButton(btnsObj.transform, font, "↺  RESTART RUN", out dummyRestart, () =>
            {
                CloseSettingsModal();
                OnRestartRequested?.Invoke();
            }, new Color(0.85f, 0.20f, 0.30f, 0.92f), new Color(0.96f, 0.35f, 0.45f, 0.50f), textColor: Color.white);

            // Button 4: Resume Hero Button
            Text dummyResume;
            CreateMinimalListButton(btnsObj.transform, font, "▶  RESUME", out dummyResume, () =>
            {
                CloseSettingsModal();
            }, new Color(0.00f, 0.90f, 1.0f, 1.0f), new Color(0.00f, 0.90f, 1.0f, 0.50f), textColor: new Color(0.04f, 0.07f, 0.12f, 1.0f));

            _settingsModalPanel.SetActive(false);
            UpdateSettingsButtonLabels();
        }

        private void CreateMinimalListButton(Transform parent, Font font, string text, out Text labelText, Action onClick, Color bgColor, Color outlineColor, Color? textColor = null, float height = 84f, int fontSize = 26)
        {
            GameObject btnObj = new GameObject($"ListBtn_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520f, height);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = GetSubtleButtonSprite();
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            if (outlineColor != Color.clear)
            {
                AddOutline(btnObj, outlineColor, new Vector2(1.5f, -1.5f));
            }

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(btnObj.transform, false);
            labelText = lblObj.AddComponent<Text>();
            labelText.font = font;
            labelText.supportRichText = true;
            labelText.text = text;
            labelText.fontSize = fontSize;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = textColor ?? Color.white;
            labelText.raycastTarget = false;

            RectTransform lRt = lblObj.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
        }

        private void UpdateSettingsButtonLabels()
        {
            ProceduralAudio audio = FindFirstObjectByType<ProceduralAudio>();
            if (_soundOptionText != null && audio != null)
            {
                bool on = audio.IsSoundEnabled;
                _soundOptionText.text = on ? "SOUND:  <color=#10B981>ON</color>" : "SOUND:  <color=#64748B>OFF</color>";
            }
            if (_hapticsOptionText != null && PolyFuse.Juice.HapticFeedbackManager.Instance != null)
            {
                bool on = PolyFuse.Juice.HapticFeedbackManager.Instance.IsHapticsEnabled;
                _hapticsOptionText.text = on ? "HAPTICS:  <color=#10B981>ON</color>" : "HAPTICS:  <color=#64748B>OFF</color>";
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

        public void ShowBoardWipeBanner(int bonus)
        {
            if (_celebrationCoroutine != null) StopCoroutine(_celebrationCoroutine);
            _celebrationCoroutine = StartCoroutine(AnimateCelebrationBanner("★ BOARD WIPE! ★", new Color(0.85f, 0.40f, 1.0f)));
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
