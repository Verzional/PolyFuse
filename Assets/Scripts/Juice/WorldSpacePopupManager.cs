using System.Collections;
using System.Collections.Generic;
using PolyFuse.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace PolyFuse.Juice
{
    /// <summary>
    /// Spawns sleek, naked floating cleave text with alternating dynamic action tilts
    /// and 3D arcade weight directly over cleared lines in world space.
    /// </summary>
    public class WorldSpacePopupManager : MonoBehaviour
    {
        public static WorldSpacePopupManager Instance { get; private set; }

        private const float BASE_WORLD_SCALE = 0.014f;

        [Header("Canvas Settings")]
        private Canvas _worldCanvas;
        private Font _font;

        private int _spawnCounter = 0;
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildWorldCanvas();
        }

        private void BuildWorldCanvas()
        {
            GameObject canvasObj = new GameObject("WorldSpacePopupCanvas");
            canvasObj.transform.SetParent(transform, false);
            canvasObj.transform.position = Vector3.zero;
            canvasObj.transform.localScale = Vector3.one;

            _worldCanvas = canvasObj.AddComponent<Canvas>();
            _worldCanvas.renderMode = RenderMode.WorldSpace;
            _worldCanvas.sortingOrder = 55;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 40f;

            // Load Font
            _font = Resources.Load<Font>("PolyFuse-MainFont");
            if (_font == null) _font = Resources.Load<Font>("Fonts/PolyFuse-MainFont");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public void SpawnCleavePopup(Vector3 worldPos, TurnClearEventData clearData)
        {
            if (_worldCanvas == null) BuildWorldCanvas();

            GameObject popupObj = GetOrCreatePopup();
            popupObj.SetActive(true);

            // Center on cleared tiles with comfortable upward offset
            popupObj.transform.position = new Vector3(worldPos.x, worldPos.y + 0.10f, 0f);

            Text titleText = popupObj.transform.Find("TitleText")?.GetComponent<Text>();
            Text scoreText = popupObj.transform.Find("ScoreText")?.GetComponent<Text>();
            CanvasGroup group = popupObj.GetComponent<CanvasGroup>();

            string title = clearData.primaryTitle;
            Color themeColor;

            if (clearData.isBoardWipe)
            {
                themeColor = new Color(0.88f, 0.45f, 1.0f); // Prismatic Purple
            }
            else if (clearData.distinctAxes == 3)
            {
                themeColor = new Color(0.20f, 0.95f, 0.85f); // Electric Cyan / Radiant Mint
            }
            else if (clearData.distinctAxes == 2)
            {
                themeColor = new Color(0.35f, 0.85f, 1.0f); // Sky Cyan Convergence
            }
            else if (clearData.isClutchSave)
            {
                themeColor = new Color(1.0f, 0.35f, 0.50f); // Neon Coral / Clutch Red
            }
            else if (clearData.linesCleared >= 4)
            {
                themeColor = new Color(0.98f, 0.28f, 0.48f); // Coral Supernova
            }
            else if (clearData.linesCleared == 3)
            {
                themeColor = new Color(0.18f, 0.88f, 1.0f); // Electric Cyan
            }
            else if (clearData.linesCleared == 2)
            {
                themeColor = new Color(1.0f, 0.82f, 0.20f); // Warm Amber Gold
            }
            else
            {
                if (clearData.comboStreak >= 7)
                    themeColor = new Color(0.88f, 0.45f, 1.0f); // Prismatic Purple
                else if (clearData.comboStreak >= 5)
                    themeColor = new Color(0.98f, 0.28f, 0.48f); // Magenta/Coral
                else if (clearData.comboStreak >= 2)
                    themeColor = new Color(1.0f, 0.82f, 0.20f); // Warm Amber Gold
                else
                    themeColor = new Color(0.92f, 0.96f, 1.0f); // Crisp White
            }

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = themeColor;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }

            if (scoreText != null)
            {
                string multiplierTag = (clearData.comboMultiplier > 1.0f)
                    ? $" <size=34><color=#FCD34D>{clearData.multiplierString}</color></size>"
                    : "";
                scoreText.text = $"+{clearData.totalPointsGained:N0}{multiplierTag}";
                scoreText.color = themeColor;
            }

            // Alternating dynamic action tilt (never same angle twice)
            _spawnCounter++;
            bool isLeft = (_spawnCounter % 2 == 1);
            float targetAngle = isLeft ? Random.Range(-6.5f, -4.5f) : Random.Range(4.5f, 6.5f);
            float startAngle = isLeft ? (targetAngle - 4.5f) : (targetAngle + 4.5f);

            StartCoroutine(AnimatePopup(popupObj, group, startAngle, targetAngle));
        }

        public void SpawnCleavePopup(Vector3 worldPos, int lineCount, int pointsEarned, int comboStreak, bool isBoardWipe = false)
        {
            if (_worldCanvas == null) BuildWorldCanvas();

            GameObject popupObj = GetOrCreatePopup();
            popupObj.SetActive(true);

            // Center on cleared tiles with comfortable upward offset
            popupObj.transform.position = new Vector3(worldPos.x, worldPos.y + 0.10f, 0f);

            Text titleText = popupObj.transform.Find("TitleText")?.GetComponent<Text>();
            Text scoreText = popupObj.transform.Find("ScoreText")?.GetComponent<Text>();
            CanvasGroup group = popupObj.GetComponent<CanvasGroup>();

            string title = "";
            Color themeColor;

            if (isBoardWipe)
            {
                title = "★ BOARD WIPE! ★";
                themeColor = new Color(0.88f, 0.45f, 1.0f); // Prismatic Purple
            }
            else if (lineCount >= 4)
            {
                title = "SUPER NOVA!";
                themeColor = new Color(0.98f, 0.28f, 0.48f); // Coral Supernova
            }
            else if (lineCount == 3)
            {
                title = "THE TRIFECTA!";
                themeColor = new Color(0.18f, 0.88f, 1.0f); // Electric Cyan
            }
            else if (lineCount == 2)
            {
                title = "DOUBLE CLEAVE!";
                themeColor = new Color(1.0f, 0.82f, 0.20f); // Warm Amber Gold
            }
            else
            {
                title = (comboStreak > 1) ? $"COMBO ×{comboStreak}!" : "";
                themeColor = (comboStreak > 1) ? new Color(1.0f, 0.82f, 0.20f) : new Color(0.92f, 0.96f, 1.0f);
            }

            if (titleText != null)
            {
                titleText.text = title;
                titleText.color = themeColor;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }

            if (scoreText != null)
            {
                string multiplierTag = (comboStreak > 1 && lineCount > 1) ? $" <size=34><color=#FCD34D>×{comboStreak}</color></size>" : "";
                scoreText.text = $"+{pointsEarned:N0}{multiplierTag}";
                scoreText.color = themeColor;
            }

            // Alternating dynamic action tilt (never same angle twice)
            _spawnCounter++;
            bool isLeft = (_spawnCounter % 2 == 1);
            float targetAngle = isLeft ? Random.Range(-6.5f, -4.5f) : Random.Range(4.5f, 6.5f);
            float startAngle = isLeft ? (targetAngle - 4.5f) : (targetAngle + 4.5f);

            StartCoroutine(AnimatePopup(popupObj, group, startAngle, targetAngle));
        }

        private IEnumerator AnimatePopup(GameObject popupObj, CanvasGroup group, float startAngle, float targetAngle)
        {
            Transform t = popupObj.transform;
            Vector3 startPos = t.position;
            Vector3 targetPos = startPos + Vector3.up * 0.85f;

            Vector3 startScale = Vector3.one * (BASE_WORLD_SCALE * 0.25f);
            Vector3 popScale = Vector3.one * (BASE_WORLD_SCALE * 1.30f);
            Vector3 normalScale = Vector3.one * BASE_WORLD_SCALE;

            group.alpha = 1f;
            t.localScale = startScale;
            t.localRotation = Quaternion.Euler(0f, 0f, startAngle);

            // Phase 1: Explosive elastic slam-in (0.14s)
            float elapsed = 0f;
            float punchDur = 0.14f;
            while (elapsed < punchDur)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / punchDur);

                t.localScale = Vector3.Lerp(startScale, popScale, progress);
                float curAngle = Mathf.Lerp(startAngle, targetAngle + (startAngle > targetAngle ? -1.5f : 1.5f), progress);
                t.localRotation = Quaternion.Euler(0f, 0f, curAngle);

                yield return null;
            }

            // Phase 2: Settle to exact scale & target angle (0.09s)
            elapsed = 0f;
            float settleDur = 0.09f;
            while (elapsed < settleDur)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / settleDur);

                t.localScale = Vector3.Lerp(popScale, normalScale, progress);
                float curAngle = Mathf.Lerp(t.localEulerAngles.z, targetAngle, progress);
                t.localRotation = Quaternion.Euler(0f, 0f, curAngle);

                yield return null;
            }

            t.localScale = normalScale;
            t.localRotation = Quaternion.Euler(0f, 0f, targetAngle);

            // Phase 3: Upward floating drift with smooth quadratic fade (0.85s)
            elapsed = 0f;
            float floatDur = 0.85f;
            while (elapsed < floatDur)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / floatDur);

                // Smooth ease-out float
                float moveT = Mathf.Sin(progress * Mathf.PI * 0.5f);
                t.position = Vector3.Lerp(startPos, targetPos, moveT);

                // Fade out in second half
                if (progress > 0.45f)
                {
                    float fadeProgress = (progress - 0.45f) / 0.55f;
                    group.alpha = 1f - (fadeProgress * fadeProgress);
                }

                yield return null;
            }

            group.alpha = 0f;
            popupObj.SetActive(false);
            _pool.Enqueue(popupObj);
        }

        private GameObject GetOrCreatePopup()
        {
            if (_pool.Count > 0)
            {
                return _pool.Dequeue();
            }

            GameObject popup = new GameObject("WorldPopupItem");
            popup.transform.SetParent(_worldCanvas.transform, false);

            RectTransform rt = popup.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 120f);
            rt.localScale = Vector3.one * BASE_WORLD_SCALE;

            CanvasGroup group = popup.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            // 1. Title Text (Top: "DOUBLE CLEAVE!")
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(popup.transform, false);
            Text title = titleObj.AddComponent<Text>();
            title.font = _font;
            title.fontSize = 32;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            title.verticalOverflow = VerticalWrapMode.Overflow;
            title.raycastTarget = false;

            Outline titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.98f);
            titleOutline.effectDistance = new Vector2(2.5f, -2.5f);

            Shadow titleShadow = titleObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(3f, -3f);

            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.52f);
            tRt.anchorMax = new Vector2(1f, 1.0f);
            tRt.sizeDelta = Vector2.zero;

            // 2. Score Text (Bottom: "+3,500 ×2")
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(popup.transform, false);
            Text score = scoreObj.AddComponent<Text>();
            score.font = _font;
            score.supportRichText = true;
            score.fontSize = 54;
            score.fontStyle = FontStyle.Bold;
            score.alignment = TextAnchor.MiddleCenter;
            score.horizontalOverflow = HorizontalWrapMode.Overflow;
            score.verticalOverflow = VerticalWrapMode.Overflow;
            score.raycastTarget = false;

            Outline scoreOutline = scoreObj.AddComponent<Outline>();
            scoreOutline.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.98f);
            scoreOutline.effectDistance = new Vector2(3.5f, -3.5f);

            Shadow scoreShadow = scoreObj.AddComponent<Shadow>();
            scoreShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            scoreShadow.effectDistance = new Vector2(4f, -4f);

            RectTransform sRt = scoreObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0.0f);
            sRt.anchorMax = new Vector2(1f, 0.58f);
            sRt.sizeDelta = Vector2.zero;

            return popup;
        }
    }
}
