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
        [SerializeField] private Text _finalScoreBestText;
        [SerializeField] private Text _maxComboStatText;
        [SerializeField] private Text _linesClearedStatText;
        [SerializeField] private Text _piecesPlacedStatText;
        [SerializeField] private Button _restartButton;

        [Header("Settings & Celebrations")]
        [SerializeField] private GameObject _settingsModalPanel;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Text _celebrationBannerText;
        [SerializeField] private CanvasGroup _celebrationCanvasGroup;

        private Font _uiFont;
        private Coroutine _scorePunchCoroutine;
        private Coroutine _scoreRollUpCoroutine;
        private Coroutine _deltaPopupCoroutine;
        private Coroutine _comboAnimCoroutine;
        private Coroutine _celebrationCoroutine;

        private int _displayedScore = 0;
        private CanvasGroup _gameOverCanvasGroup;
        private RectTransform _gameOverCardRt;
        private CanvasGroup _settingsModalCanvasGroup;
        private RectTransform _settingsListContainerRt;

        private RectTransform _highScoreRt;
        private RectTransform _scoreRt;
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

        private static Sprite _borderedCardSprite;
        private static Sprite _borderedPillSprite;
        private static Sprite _goldBorderedPillSprite;

        public static Sprite GetBorderedCardSprite()
        {
            if (_borderedCardSprite != null) return _borderedCardSprite;
            _borderedCardSprite = CreateBorderedCardSprite(128, 16, 2.5f, new Vector4(22, 22, 22, 22),
                new Color(0.32f, 0.50f, 0.75f, 1.0f),
                new Color(0.08f, 0.11f, 0.18f, 0.98f));
            return _borderedCardSprite;
        }

        public static Sprite GetBorderedPillSprite()
        {
            if (_borderedPillSprite != null) return _borderedPillSprite;
            _borderedPillSprite = CreateBorderedCardSprite(128, 12, 2.0f, new Vector4(18, 18, 18, 18),
                new Color(0.25f, 0.40f, 0.62f, 0.95f),
                new Color(0.04f, 0.06f, 0.11f, 0.95f));
            return _borderedPillSprite;
        }

        public static Sprite GetGoldBorderedPillSprite()
        {
            if (_goldBorderedPillSprite != null) return _goldBorderedPillSprite;
            _goldBorderedPillSprite = CreateBorderedCardSprite(128, 12, 2.0f, new Vector4(18, 18, 18, 18),
                new Color(0.85f, 0.68f, 0.20f, 0.90f),
                new Color(0.05f, 0.07f, 0.12f, 0.95f));
            return _goldBorderedPillSprite;
        }

        private static Sprite CreateBorderedCardSprite(int size, int radius, float borderWidth, Vector4 border, Color borderColor, Color fillColor)
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

                    // Outer perimeter antialiasing
                    float outerAlpha = 1f;
                    if (px < r || px > size - r || py < r || py > size - r)
                    {
                        outerAlpha = Mathf.Clamp01((r - dist) + 0.5f);
                    }

                    // Distance from outer boundary inwards:
                    float distFromEdge = Mathf.Min(Mathf.Min(px, size - px), Mathf.Min(py, size - py));
                    bool isCorner = (px < r && py < r) || (px > size - r && py < r) || (px < r && py > size - r) || (px > size - r && py > size - r);
                    float dInward = isCorner ? (r - dist) : distFromEdge;

                    float blendT = Mathf.Clamp01((dInward - borderWidth) + 0.5f);
                    Color finalColor = Color.Lerp(borderColor, fillColor, blendT);
                    finalColor.a *= outerAlpha;

                    cols[y * size + x] = finalColor;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

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

        private static Sprite _vignetteSprite;

        public static Sprite GetVignetteSprite()
        {
            if (_vignetteSprite != null) return _vignetteSprite;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color[] cols = new Color[size * size];

            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float distSq = dx * dx + dy * dy; // 0 at center, 1 at edge, 2 at corners

                    // Keep inner 60% radius completely crystal clear (alpha = 0)
                    float alpha = 0f;
                    if (distSq > 0.55f)
                    {
                        float t = (distSq - 0.55f) / 1.45f;
                        alpha = Mathf.Clamp01(t * t * t); // Ultra-smooth cubic edge falloff
                    }

                    cols[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _vignetteSprite;
        }

        private Image _dangerVignette;
        private Coroutine _dangerVignetteCoroutine;

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

            // 0. Ambient Danger Vignette Overlay (Screen edges, non-blocking)
            GameObject vigObj = new GameObject("DangerVignette");
            vigObj.transform.SetParent(canvas.transform, false);
            _dangerVignette = vigObj.AddComponent<Image>();
            _dangerVignette.sprite = GetVignetteSprite();
            _dangerVignette.color = new Color(1.0f, 0.12f, 0.28f, 0f);
            _dangerVignette.raycastTarget = false;
            RectTransform vigRt = vigObj.GetComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.sizeDelta = Vector2.zero;
            vigObj.SetActive(false);

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

            // 4. Direction 1: Arcade Glass Card (Game Over Screen)
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvas.transform, false);
            RectTransform goRt = _gameOverPanel.AddComponent<RectTransform>();
            goRt.anchorMin = Vector2.zero;
            goRt.anchorMax = Vector2.one;
            goRt.sizeDelta = Vector2.zero;

            _gameOverCanvasGroup = _gameOverPanel.AddComponent<CanvasGroup>();

            Image goBg = _gameOverPanel.AddComponent<Image>();
            goBg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f); // Deep dark obsidian veil

            // Central Floating Glass Card (Auto-fitting container with guaranteed equal 44px margins and 24px spacing)
            GameObject cardObj = new GameObject("GameOverCard");
            cardObj.transform.SetParent(_gameOverPanel.transform, false);
            _gameOverCardRt = cardObj.AddComponent<RectTransform>();
            _gameOverCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            _gameOverCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            _gameOverCardRt.pivot = new Vector2(0.5f, 0.5f);
            _gameOverCardRt.sizeDelta = new Vector2(820f, 0f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = GetBorderedCardSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;
            AddShadow(cardObj, new Color(0f, 0f, 0f, 0.85f), new Vector2(0f, -10f));

            VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(44, 44, 44, 44);
            cardLayout.spacing = 24f; // Clean 24px gap between every section
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true; // Layout group directly controls height of children
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            ContentSizeFitter cardFitter = cardObj.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 1. Card Header Title ("GAME OVER")
            GameObject titleObj = new GameObject("GameOverTitle");
            titleObj.transform.SetParent(cardObj.transform, false);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.text = "GAME OVER";
            titleText.fontSize = 44;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.raycastTarget = false;
            titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleText.verticalOverflow = VerticalWrapMode.Overflow;
            AddShadow(titleObj, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.preferredHeight = 48f;
            tLe.minHeight = 48f;

            // 2. Score Readout Section (Floating inside card)
            GameObject scoreSection = new GameObject("ScoreSection");
            scoreSection.transform.SetParent(cardObj.transform, false);
            LayoutElement ssLe = scoreSection.AddComponent<LayoutElement>();
            ssLe.preferredHeight = 150f;
            ssLe.minHeight = 150f;

            VerticalLayoutGroup ssVlg = scoreSection.AddComponent<VerticalLayoutGroup>();
            ssVlg.padding = new RectOffset(0, 0, 0, 0);
            ssVlg.spacing = 4f;
            ssVlg.childAlignment = TextAnchor.MiddleCenter;
            ssVlg.childControlWidth = true;
            ssVlg.childControlHeight = true;
            ssVlg.childForceExpandWidth = true;
            ssVlg.childForceExpandHeight = false;

            // Header Label ("FINAL SCORE")
            GameObject scLblObj = new GameObject("ScoreLabel");
            scLblObj.transform.SetParent(scoreSection.transform, false);
            Text scLbl = scLblObj.AddComponent<Text>();
            scLbl.font = font;
            scLbl.text = "FINAL SCORE";
            scLbl.fontSize = 20;
            scLbl.fontStyle = FontStyle.Bold;
            scLbl.alignment = TextAnchor.MiddleCenter;
            scLbl.color = new Color(0.60f, 0.68f, 0.78f, 1.0f);
            scLbl.raycastTarget = false;
            scLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            scLbl.verticalOverflow = VerticalWrapMode.Overflow;
            LayoutElement sclLe = scLblObj.AddComponent<LayoutElement>();
            sclLe.preferredHeight = 24f;
            sclLe.minHeight = 24f;

            // Final Score Digits (96px Bold White Digits)
            GameObject finalScoreObj = new GameObject("FinalScoreText");
            finalScoreObj.transform.SetParent(scoreSection.transform, false);
            _finalScoreText = finalScoreObj.AddComponent<Text>();
            _finalScoreText.font = font;
            _finalScoreText.raycastTarget = false;
            _finalScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _finalScoreText.verticalOverflow = VerticalWrapMode.Overflow;
            _finalScoreText.text = "0";
            _finalScoreText.fontSize = 96;
            _finalScoreText.fontStyle = FontStyle.Bold;
            _finalScoreText.alignment = TextAnchor.MiddleCenter;
            _finalScoreText.color = Color.white;
            AddOutline(finalScoreObj, new Color(0.04f, 0.06f, 0.10f, 0.85f), new Vector2(2.5f, -2.5f));
            AddShadow(finalScoreObj, new Color(0f, 0f, 0f, 0.75f), new Vector2(3f, -3f));
            LayoutElement fsLe = finalScoreObj.AddComponent<LayoutElement>();
            fsLe.preferredHeight = 106f;
            fsLe.minHeight = 106f;

            // 3. Run Stats Row (More spacious 114px row with bordered pills)
            GameObject statsRowObj = new GameObject("RunStatsRow");
            statsRowObj.transform.SetParent(cardObj.transform, false);
            LayoutElement srLe = statsRowObj.AddComponent<LayoutElement>();
            srLe.preferredHeight = 114f;
            srLe.minHeight = 114f;

            HorizontalLayoutGroup srHlg = statsRowObj.AddComponent<HorizontalLayoutGroup>();
            srHlg.padding = new RectOffset(0, 0, 0, 0);
            srHlg.spacing = 14f;
            srHlg.childAlignment = TextAnchor.MiddleCenter;
            srHlg.childControlWidth = true;
            srHlg.childControlHeight = true;
            srHlg.childForceExpandWidth = true;
            srHlg.childForceExpandHeight = true;

            CreateStatCell(statsRowObj.transform, font, "MAX COMBO", out _maxComboStatText);
            CreateStatCell(statsRowObj.transform, font, "LINES", out _linesClearedStatText);
            CreateStatCell(statsRowObj.transform, font, "PIECES", out _piecesPlacedStatText);

            // 4. Best Score Pill Inset (88px tall with crisp gold border)
            GameObject bestPillObj = new GameObject("BestPill");
            bestPillObj.transform.SetParent(cardObj.transform, false);
            LayoutElement bpLe = bestPillObj.AddComponent<LayoutElement>();
            bpLe.preferredHeight = 88f;
            bpLe.minHeight = 88f;

            Image bpImg = bestPillObj.AddComponent<Image>();
            bpImg.sprite = GetGoldBorderedPillSprite();
            bpImg.type = Image.Type.Sliced;
            bpImg.color = Color.white;

            GameObject bestTextObj = new GameObject("BestScoreText");
            bestTextObj.transform.SetParent(bestPillObj.transform, false);
            _finalScoreBestText = bestTextObj.AddComponent<Text>();
            _finalScoreBestText.font = font;
            _finalScoreBestText.supportRichText = true;
            _finalScoreBestText.raycastTarget = false;
            _finalScoreBestText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _finalScoreBestText.verticalOverflow = VerticalWrapMode.Overflow;
            _finalScoreBestText.text = "BEST:  ★ 0";
            _finalScoreBestText.fontSize = 28;
            _finalScoreBestText.fontStyle = FontStyle.Bold;
            _finalScoreBestText.alignment = TextAnchor.MiddleCenter;
            _finalScoreBestText.color = new Color(1.0f, 0.82f, 0.20f, 1.0f);
            RectTransform btRt = bestTextObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // 5. Play Again Hero Button (104px tall)
            GameObject btnObj = new GameObject("PlayAgainButton");
            btnObj.transform.SetParent(cardObj.transform, false);
            LayoutElement btnLe = btnObj.AddComponent<LayoutElement>();
            btnLe.preferredHeight = 104f;
            btnLe.minHeight = 104f;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = GetSubtleButtonSprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(0.00f, 0.90f, 1.0f, 1.0f);
            AddOutline(btnObj, new Color(0.00f, 0.90f, 1.0f, 0.50f), new Vector2(1.5f, -1.5f));

            _restartButton = btnObj.AddComponent<Button>();
            _restartButton.onClick.AddListener(() =>
            {
                HideGameOver();
                OnRestartRequested?.Invoke();
            });

            GameObject btnLabelObj = new GameObject("BtnLabel");
            btnLabelObj.transform.SetParent(btnObj.transform, false);
            Text btnLabel = btnLabelObj.AddComponent<Text>();
            btnLabel.font = font;
            btnLabel.raycastTarget = false;
            btnLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            btnLabel.verticalOverflow = VerticalWrapMode.Overflow;
            btnLabel.text = "▶  PLAY AGAIN";
            btnLabel.fontSize = 36;
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color = new Color(0.04f, 0.07f, 0.12f, 1.0f);
            RectTransform blRt = btnLabelObj.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.sizeDelta = Vector2.zero;

            _gameOverPanel.SetActive(false);

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
            if (_highScoreText != null)
            {
                _highScoreText.text = $"★ {highScore:N0}";
            }

            if (_scoreText != null)
            {
                if (pointsDelta <= 0 || _displayedScore == currentScore)
                {
                    if (_scoreRollUpCoroutine != null) StopCoroutine(_scoreRollUpCoroutine);
                    _displayedScore = currentScore;
                    _scoreText.text = currentScore.ToString("N0");
                }
                else
                {
                    if (_scoreRollUpCoroutine != null) StopCoroutine(_scoreRollUpCoroutine);
                    _scoreRollUpCoroutine = StartCoroutine(RollUpScoreTicker(_displayedScore, currentScore));

                    if (_scorePunchCoroutine != null) StopCoroutine(_scorePunchCoroutine);
                    _scorePunchCoroutine = StartCoroutine(PunchScoreText());

                    if (pointsDelta >= 30)
                    {
                        if (_deltaPopupCoroutine != null) StopCoroutine(_deltaPopupCoroutine);
                        _deltaPopupCoroutine = StartCoroutine(ShowDeltaPopup(pointsDelta));
                    }
                }
            }
        }

        private IEnumerator RollUpScoreTicker(int startScore, int targetScore)
        {
            float elapsed = 0f;
            float dur = (targetScore - startScore) > 2000 ? 0.32f : 0.20f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float ease = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
                int current = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, ease));
                _scoreText.text = current.ToString("N0");
                yield return null;
            }

            _displayedScore = targetScore;
            _scoreText.text = targetScore.ToString("N0");
            _scoreRollUpCoroutine = null;
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

        public void UpdateComboState(int comboStreak, int graceRemaining, int graceCapacity, float pitch)
        {
            if (comboStreak <= 0)
            {
                if (_comboCanvasGroup != null && _comboCanvasGroup.alpha > 0f)
                {
                    StartCoroutine(FadeOutCombo());
                }
                return;
            }

            if (_comboRt != null && !_comboRt.gameObject.activeSelf)
            {
                _comboRt.gameObject.SetActive(true);
            }

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

                int totalPips = Mathf.Max(3, graceCapacity);
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

        public void ShowGameOver(int finalScore, int maxCombo = 0, int linesCleared = 0, int piecesPlaced = 0)
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                if (_gameOverCanvasGroup != null && _gameOverCardRt != null)
                {
                    StartCoroutine(AnimateModalEntrance(_gameOverCanvasGroup, _gameOverCardRt));
                }
            }
            if (_finalScoreText != null)
            {
                _finalScoreText.text = finalScore.ToString("N0");
            }
            if (_maxComboStatText != null)
            {
                _maxComboStatText.text = maxCombo > 1 ? $"{maxCombo}×" : "1×";
                if (maxCombo >= 5) _maxComboStatText.color = new Color(0.00f, 0.90f, 1.0f, 1.0f); // Electric Cyan
                else if (maxCombo >= 3) _maxComboStatText.color = new Color(1.0f, 0.80f, 0.20f, 1.0f); // Gold
                else _maxComboStatText.color = Color.white;
            }
            if (_linesClearedStatText != null)
            {
                _linesClearedStatText.text = linesCleared.ToString("N0");
            }
            if (_piecesPlacedStatText != null)
            {
                _piecesPlacedStatText.text = piecesPlaced.ToString("N0");
            }
            if (_finalScoreBestText != null)
            {
                int highScore = PlayerPrefs.GetInt("PolyFuse_HighScore", finalScore);
                if (finalScore >= highScore && finalScore > 0)
                {
                    _finalScoreBestText.text = "★  NEW BEST RECORD!  ★";
                    _finalScoreBestText.color = new Color(1.0f, 0.85f, 0.20f, 1.0f);
                }
                else
                {
                    _finalScoreBestText.text = $"★  BEST  {highScore:N0}";
                    _finalScoreBestText.color = new Color(1.0f, 0.82f, 0.20f, 1.0f);
                }
            }

            // Hide Top HUD during Game Over to eliminate duplicate score competition
            if (_scoreRt != null) _scoreRt.gameObject.SetActive(false);
            if (_highScoreRt != null) _highScoreRt.gameObject.SetActive(false);
            if (_settingsBtnRt != null) _settingsBtnRt.gameObject.SetActive(false);
            if (_comboRt != null) _comboRt.gameObject.SetActive(false);
        }

        public void HideGameOver()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(false);
            }

            // Restore Top HUD
            if (_scoreRt != null) _scoreRt.gameObject.SetActive(true);
            if (_highScoreRt != null) _highScoreRt.gameObject.SetActive(true);
            if (_settingsBtnRt != null) _settingsBtnRt.gameObject.SetActive(true);
            if (_comboRt != null) _comboRt.gameObject.SetActive(true);
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

            _settingsModalCanvasGroup = _settingsModalPanel.AddComponent<CanvasGroup>();

            // Fullscreen dark frosted dimming - Tapping backdrop resumes
            Image smBg = _settingsModalPanel.AddComponent<Image>();
            smBg.color = new Color(0.02f, 0.04f, 0.07f, 0.94f);
            Button bgDismiss = _settingsModalPanel.AddComponent<Button>();
            bgDismiss.onClick.AddListener(CloseSettingsModal);

            // Centered Vertical Container (820px wide = ~76% of 1080 screen width)
            GameObject listContainer = new GameObject("ZenListContainer");
            listContainer.transform.SetParent(_settingsModalPanel.transform, false);
            _settingsListContainerRt = listContainer.AddComponent<RectTransform>();
            _settingsListContainerRt.anchorMin = new Vector2(0.5f, 0.5f);
            _settingsListContainerRt.anchorMax = new Vector2(0.5f, 0.5f);
            _settingsListContainerRt.pivot = new Vector2(0.5f, 0.5f);
            _settingsListContainerRt.sizeDelta = new Vector2(820f, 532f);

            VerticalLayoutGroup vlg = listContainer.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 28f; // Clean 28px gap between Title and Buttons
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
            title.fontSize = 52;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.raycastTarget = false;
            AddShadow(titleObj, new Color(0f, 0f, 0f, 0.9f), new Vector2(2f, -2f));
            LayoutElement tLe = titleObj.AddComponent<LayoutElement>();
            tLe.minHeight = 58f;
            tLe.preferredHeight = 58f;

            // 2. Buttons Stack Container (Guarantees 100% equal spacing between all 4 buttons)
            GameObject btnsObj = new GameObject("ButtonsStack");
            btnsObj.transform.SetParent(listContainer.transform, false);
            VerticalLayoutGroup bVlg = btnsObj.AddComponent<VerticalLayoutGroup>();
            bVlg.padding = new RectOffset(0, 0, 0, 0);
            bVlg.spacing = 18f; // Exactly 18px between every button!
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
            }, new Color(0.10f, 0.14f, 0.22f, 0.85f), new Color(0.20f, 0.30f, 0.44f, 0.45f), height: 98f, fontSize: 32);

            // Button 2: Haptics Toggle
            CreateMinimalListButton(btnsObj.transform, font, "HAPTICS:  <color=#10B981>ON</color>", out _hapticsOptionText, () =>
            {
                PolyFuse.Juice.HapticFeedbackManager.Instance?.ToggleHaptics();
                UpdateSettingsButtonLabels();
            }, new Color(0.10f, 0.14f, 0.22f, 0.85f), new Color(0.20f, 0.30f, 0.44f, 0.45f), height: 98f, fontSize: 32);

            // Button 3: Restart Run (Vibrant Crimson with crisp white text)
            Text dummyRestart;
            CreateMinimalListButton(btnsObj.transform, font, "↺  RESTART RUN", out dummyRestart, () =>
            {
                CloseSettingsModal();
                OnRestartRequested?.Invoke();
            }, new Color(0.85f, 0.20f, 0.30f, 0.92f), new Color(0.96f, 0.35f, 0.45f, 0.50f), textColor: Color.white, height: 98f, fontSize: 32);

            // Button 4: Resume Hero Button
            Text dummyResume;
            CreateMinimalListButton(btnsObj.transform, font, "▶  RESUME", out dummyResume, () =>
            {
                CloseSettingsModal();
            }, new Color(0.00f, 0.90f, 1.0f, 1.0f), new Color(0.00f, 0.90f, 1.0f, 0.50f), textColor: new Color(0.04f, 0.07f, 0.12f, 1.0f), height: 98f, fontSize: 34);

            _settingsModalPanel.SetActive(false);
            UpdateSettingsButtonLabels();
        }

        private void CreateStatCell(Transform parent, Font font, string label, out Text valueText)
        {
            GameObject cellObj = new GameObject($"StatCell_{label}");
            cellObj.transform.SetParent(parent, false);
            Image bg = cellObj.AddComponent<Image>();
            bg.sprite = GetBorderedPillSprite();
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            VerticalLayoutGroup vlg = cellObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 12, 12);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(cellObj.transform, false);
            Text lbl = lblObj.AddComponent<Text>();
            lbl.font = font;
            lbl.text = label;
            lbl.fontSize = 17;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = new Color(0.65f, 0.74f, 0.85f, 1.0f);
            lbl.raycastTarget = false;
            lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            lbl.verticalOverflow = VerticalWrapMode.Overflow;

            GameObject valObj = new GameObject("Value");
            valObj.transform.SetParent(cellObj.transform, false);
            valueText = valObj.AddComponent<Text>();
            valueText.font = font;
            valueText.text = "0";
            valueText.fontSize = 38;
            valueText.fontStyle = FontStyle.Bold;
            valueText.alignment = TextAnchor.MiddleCenter;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void CreateMinimalListButton(Transform parent, Font font, string text, out Text labelText, Action onClick, Color bgColor, Color outlineColor, Color? textColor = null, float height = 98f, int fontSize = 32)
        {
            GameObject btnObj = new GameObject($"ListBtn_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(740f, height);

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
            if (active)
            {
                _settingsModalPanel.SetActive(true);
                Time.timeScale = 0.0f;
                UpdateSettingsButtonLabels();
                if (_settingsModalCanvasGroup != null && _settingsListContainerRt != null)
                {
                    StartCoroutine(AnimateModalEntrance(_settingsModalCanvasGroup, _settingsListContainerRt));
                }
            }
            else
            {
                CloseSettingsModal();
            }
        }

        public void CloseSettingsModal()
        {
            if (_settingsModalPanel != null && _settingsModalPanel.activeSelf)
            {
                if (_settingsModalCanvasGroup != null && _settingsListContainerRt != null)
                {
                    StartCoroutine(AnimateModalExit(_settingsModalCanvasGroup, _settingsListContainerRt, () =>
                    {
                        _settingsModalPanel.SetActive(false);
                        Time.timeScale = 1.0f;
                    }));
                }
                else
                {
                    _settingsModalPanel.SetActive(false);
                    Time.timeScale = 1.0f;
                }
            }
            else
            {
                Time.timeScale = 1.0f;
            }
        }

        private IEnumerator AnimateModalEntrance(CanvasGroup cg, RectTransform container)
        {
            if (cg == null || container == null) yield break;

            cg.alpha = 0f;
            Vector3 startScale = Vector3.one * 0.92f;
            Vector3 targetScale = Vector3.one;
            container.localScale = startScale;

            float elapsed = 0f;
            float dur = 0.16f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                cg.alpha = t;
                container.localScale = Vector3.Lerp(startScale, targetScale, ease);
                yield return null;
            }

            cg.alpha = 1f;
            container.localScale = Vector3.one;
        }

        private IEnumerator AnimateModalExit(CanvasGroup cg, RectTransform container, Action onComplete)
        {
            if (cg == null || container == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            float dur = 0.10f;
            Vector3 startScale = container.localScale;
            Vector3 endScale = Vector3.one * 0.95f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                cg.alpha = 1f - t;
                container.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            cg.alpha = 0f;
            onComplete?.Invoke();
        }

        public void SetDangerState(bool inDanger)
        {
            if (_dangerVignetteCoroutine != null) StopCoroutine(_dangerVignetteCoroutine);
            _dangerVignetteCoroutine = StartCoroutine(AnimateDangerVignette(inDanger));
        }

        private IEnumerator AnimateDangerVignette(bool inDanger)
        {
            if (_dangerVignette == null) yield break;

            if (inDanger)
            {
                Color baseCrimson = new Color(0.95f, 0.15f, 0.28f, 1.0f);
                _dangerVignette.gameObject.SetActive(true);

                while (true)
                {
                    float cycle = Time.time % 0.96f; // Exactly synchronized with ProceduralAudio 62.5 BPM heartbeat
                    float pulseAlpha = 0.0f;

                    if (cycle < 0.20f)
                    {
                        float t1 = cycle / 0.20f;
                        pulseAlpha = Mathf.Lerp(0.0f, 0.10f, Mathf.Sin(t1 * Mathf.PI));
                    }
                    else if (cycle >= 0.25f && cycle < 0.46f)
                    {
                        float t2 = (cycle - 0.25f) / 0.21f;
                        pulseAlpha = Mathf.Lerp(0.0f, 0.16f, Mathf.Sin(t2 * Mathf.PI));
                    }

                    _dangerVignette.color = new Color(baseCrimson.r, baseCrimson.g, baseCrimson.b, pulseAlpha);
                    yield return null;
                }
            }
            else
            {
                if (!_dangerVignette.gameObject.activeSelf) yield break;

                // Subtle Heroic Escape Flash: Soft Cyan glow and swift fade out
                Color cyanFlash = new Color(0.0f, 0.95f, 1.0f, 0.22f);
                _dangerVignette.color = cyanFlash;

                float dur = 0.35f;
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / dur);
                    _dangerVignette.color = Color.Lerp(cyanFlash, new Color(0f, 0.95f, 1.0f, 0f), t);
                    yield return null;
                }

                _dangerVignette.gameObject.SetActive(false);
                _dangerVignetteCoroutine = null;
            }
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
