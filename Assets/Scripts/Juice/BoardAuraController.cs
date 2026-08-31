using System.Collections;
using PolyFuse.Core;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Juice
{
    public class BoardAuraController : MonoBehaviour
    {
        public static BoardAuraController Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float _glowWidth = 0.24f;      // Sleek, compact ethereal outer bloom
        [SerializeField] private float _rimThickness = 0.022f;  // Razor-sharp neon hairline
        [SerializeField] private float _marginFromTiles = 0.035f; // Snug, refined outer border clearance

        private MeshFilter _haloFilter;
        private MeshRenderer _haloRenderer;
        private MeshFilter _rimFilter;
        private MeshRenderer _rimRenderer;
        private MaterialPropertyBlock _haloPropBlock;
        private MaterialPropertyBlock _rimPropBlock;

        private Transform _auraContainer;
        private Camera _mainCamera;
        private Color _cameraDefaultBg = new Color(0.06f, 0.07f, 0.10f, 1.0f);
        private HexBoard _board;

        private int _currentCombo = 0;
        private Color _targetColor = new Color(0.20f, 0.35f, 0.55f, 0.30f);
        private Color _currentColor = new Color(0.20f, 0.35f, 0.55f, 0.30f);
        private float _pulseSpeed = 1.0f;
        private float _pulseIntensity = 0.12f;
        private Coroutine _surgeCoroutine;
        private Coroutine _bgTransitionCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _haloPropBlock = new MaterialPropertyBlock();
            _rimPropBlock = new MaterialPropertyBlock();
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _cameraDefaultBg = _mainCamera.backgroundColor;
            }

            _board = GetComponent<HexBoard>() ?? FindFirstObjectByType<HexBoard>();
            BuildAuraHierarchy();
        }

        private void Start()
        {
            RebuildAuraGeometry();
            SetComboState(0);
        }

        public void RebuildAuraGeometry()
        {
            if (_board == null)
            {
                _board = GetComponent<HexBoard>() ?? FindFirstObjectByType<HexBoard>();
            }

            Vector2[] hexVertices = ExtractExactPerimeterPolygon();
            if (hexVertices == null || hexVertices.Length < 3) return;

            if (_haloFilter != null)
            {
                _haloFilter.sharedMesh = GenerateHexRingMesh(hexVertices, _marginFromTiles, _glowWidth, true);
            }
            if (_rimFilter != null)
            {
                _rimFilter.sharedMesh = GenerateHexRingMesh(hexVertices, _marginFromTiles, _rimThickness, false);
            }
        }

        private void Update()
        {
            float time = Time.time;

            // 1. Rainbow chromatic cycle for God mode (Combo >= 7)
            if (_currentCombo >= 7)
            {
                float hue = (time * 0.35f) % 1.0f;
                _targetColor = Color.HSVToRGB(hue, 0.85f, 1.0f);
                _targetColor.a = 0.95f;
            }

            // 2. Smooth color transition
            _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * 6f);

            // 3. Dynamic harmonic breathing pulse
            float pulse = Mathf.Sin(time * _pulseSpeed * Mathf.PI * 2f) * _pulseIntensity;
            float currentAlpha = Mathf.Clamp01(_currentColor.a + pulse);

            // Apply to halo (soft diffused atmospheric underglow: ~35% alpha)
            if (_haloRenderer != null)
            {
                Color haloCol = _currentColor;
                haloCol.a = currentAlpha * 0.40f;
                _haloRenderer.GetPropertyBlock(_haloPropBlock);
                _haloPropBlock.SetColor("_Color", haloCol);
                _haloRenderer.SetPropertyBlock(_haloPropBlock);
            }

            // Apply to rim (sharp razor-clean neon hairline: bright with white-hot core)
            if (_rimRenderer != null)
            {
                Color rimCol = Color.Lerp(_currentColor, Color.white, 0.40f);
                rimCol.a = Mathf.Clamp01(currentAlpha * 1.15f);
                _rimRenderer.GetPropertyBlock(_rimPropBlock);
                _rimPropBlock.SetColor("_Color", rimCol);
                _rimRenderer.SetPropertyBlock(_rimPropBlock);
            }
        }

        public void SetComboState(int comboStreak)
        {
            _currentCombo = comboStreak;

            Color targetBg = _cameraDefaultBg;

            switch (comboStreak)
            {
                case 0:
                    _targetColor = new Color(0.18f, 0.28f, 0.44f, 0.40f); // Sleek titanium slate idle
                    _pulseSpeed = 0.75f;
                    _pulseIntensity = 0.08f;
                    targetBg = new Color(0.06f, 0.07f, 0.10f, 1.0f);
                    break;
                case 1:
                    _targetColor = new Color(0.15f, 0.85f, 1.00f, 0.85f); // Vibrant Sky Cyan
                    _pulseSpeed = 1.2f;
                    _pulseIntensity = 0.12f;
                    targetBg = new Color(0.05f, 0.08f, 0.12f, 1.0f);
                    break;
                case 2:
                    _targetColor = new Color(1.00f, 0.82f, 0.15f, 0.90f); // Warm Amber Gold
                    _pulseSpeed = 1.8f;
                    _pulseIntensity = 0.15f;
                    targetBg = new Color(0.08f, 0.07f, 0.09f, 1.0f);
                    break;
                case 3:
                    _targetColor = new Color(1.00f, 0.55f, 0.10f, 0.95f); // Electric Orange
                    _pulseSpeed = 2.4f;
                    _pulseIntensity = 0.18f;
                    targetBg = new Color(0.09f, 0.06f, 0.09f, 1.0f);
                    break;
                case 4:
                    _targetColor = new Color(1.00f, 0.25f, 0.48f, 1.0f); // Neon Coral/Rose
                    _pulseSpeed = 3.0f;
                    _pulseIntensity = 0.20f;
                    targetBg = new Color(0.10f, 0.05f, 0.09f, 1.0f);
                    break;
                case 5:
                    _targetColor = new Color(0.92f, 0.20f, 1.00f, 1.0f); // Electric Magenta
                    _pulseSpeed = 3.8f;
                    _pulseIntensity = 0.22f;
                    targetBg = new Color(0.09f, 0.05f, 0.12f, 1.0f);
                    break;
                case 6:
                    _targetColor = new Color(0.10f, 1.00f, 0.92f, 1.0f); // Hyper Cyan Supernova
                    _pulseSpeed = 4.8f;
                    _pulseIntensity = 0.25f;
                    targetBg = new Color(0.04f, 0.10f, 0.13f, 1.0f);
                    break;
                default: // 7+ POLYFUSE GOD
                    _pulseSpeed = 6.0f;
                    _pulseIntensity = 0.28f;
                    targetBg = new Color(0.10f, 0.06f, 0.14f, 1.0f);
                    break;
            }

            if (_bgTransitionCoroutine != null) StopCoroutine(_bgTransitionCoroutine);
            _bgTransitionCoroutine = StartCoroutine(TransitionCameraBackground(targetBg));
        }

        public void TriggerClearSurge(int comboStreak)
        {
            SetComboState(comboStreak);

            if (_surgeCoroutine != null) StopCoroutine(_surgeCoroutine);
            _surgeCoroutine = StartCoroutine(DoSurgeAnimation());
        }

        private IEnumerator DoSurgeAnimation()
        {
            if (_auraContainer == null) yield break;

            Vector3 startScale = Vector3.one;
            Vector3 peakScale = Vector3.one * 1.08f;

            // Flash white-hot on surge
            _currentColor = new Color(1f, 1f, 1f, 1f);

            float dur = 0.24f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;

                // Elastic overshoot pulse curve
                float scaleT = Mathf.Sin(t * Mathf.PI);
                _auraContainer.localScale = Vector3.LerpUnclamped(startScale, peakScale, scaleT);

                yield return null;
            }

            _auraContainer.localScale = startScale;
            _surgeCoroutine = null;
        }

        private IEnumerator TransitionCameraBackground(Color targetBg)
        {
            if (_mainCamera == null) yield break;

            Color fromBg = _mainCamera.backgroundColor;
            float dur = 0.6f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                _mainCamera.backgroundColor = Color.Lerp(fromBg, targetBg, t);
                yield return null;
            }

            _mainCamera.backgroundColor = targetBg;
            _bgTransitionCoroutine = null;
        }

        private void BuildAuraHierarchy()
        {
            // Root Container
            GameObject container = new GameObject("BoardAuraContainer");
            container.transform.SetParent(transform, false);
            _auraContainer = container.transform;

            Material defaultMat = TriangleMeshHelper.GetDefaultMaterial();
            Vector2[] hexVertices = ExtractExactPerimeterPolygon();

            // 1. Outer Soft Atmospheric Halo Mesh (sortingOrder = 0)
            GameObject haloObj = new GameObject("AuraHalo");
            haloObj.transform.SetParent(_auraContainer, false);
            _haloFilter = haloObj.AddComponent<MeshFilter>();
            _haloRenderer = haloObj.AddComponent<MeshRenderer>();
            _haloRenderer.sharedMaterial = defaultMat;
            _haloRenderer.sortingOrder = 0;
            _haloFilter.sharedMesh = GenerateHexRingMesh(hexVertices, _marginFromTiles, _glowWidth, true);

            // 2. Inner Sharp Precision Rim Mesh (sortingOrder = 1)
            GameObject rimObj = new GameObject("AuraRim");
            rimObj.transform.SetParent(_auraContainer, false);
            _rimFilter = rimObj.AddComponent<MeshFilter>();
            _rimRenderer = rimObj.AddComponent<MeshRenderer>();
            _rimRenderer.sharedMaterial = defaultMat;
            _rimRenderer.sortingOrder = 1;
            _rimFilter.sharedMesh = GenerateHexRingMesh(hexVertices, _marginFromTiles, _rimThickness, false);
        }

        /// <summary>
        /// Mathematically extracts the 100% exact outer boundary loop of all active board tiles.
        /// </summary>
        private Vector2[] ExtractExactPerimeterPolygon()
        {
            if (_board == null || _board.TileList == null || _board.TileList.Count == 0)
            {
                // Fallback geometry if board not initialized yet
                float H = TriangleMeshHelper.Height;
                return new Vector2[]
                {
                    new Vector2( 2.0f,  3.0f * H),
                    new Vector2( 3.0f,  0.0f),
                    new Vector2( 2.0f, -3.0f * H),
                    new Vector2(-2.0f, -3.0f * H),
                    new Vector2(-3.0f,  0.0f),
                    new Vector2(-2.0f,  3.0f * H)
                };
            }

            float h = TriangleMeshHelper.Height;
            float hw = TriangleMeshHelper.HalfWidth;

            // Collect all directed boundary edges (edges traversed only once)
            var edgeDict = new System.Collections.Generic.Dictionary<(long, long), (Vector2 from, Vector2 to)>();

            long Quantize(Vector2 v)
            {
                long x = (long)Mathf.Round(v.x * 1000f);
                long y = (long)Mathf.Round(v.y * 1000f);
                return (x << 32) ^ (y & 0xFFFFFFFFL);
            }

            var tiles = _board.TileList;
            for (int i = 0; i < tiles.Count; i++)
            {
                TriangleTile tile = tiles[i];
                if (tile == null) continue;

                Vector3 pos = tile.transform.localPosition;
                bool isUp = tile.IsPointingUp;

                Vector2 v0, v1, v2;
                if (isUp)
                {
                    v0 = new Vector2(pos.x, pos.y + 2f * h / 3f);
                    v1 = new Vector2(pos.x + hw, pos.y - h / 3f);
                    v2 = new Vector2(pos.x - hw, pos.y - h / 3f);
                }
                else
                {
                    v0 = new Vector2(pos.x, pos.y - 2f * h / 3f);
                    v1 = new Vector2(pos.x - hw, pos.y + h / 3f);
                    v2 = new Vector2(pos.x + hw, pos.y + h / 3f);
                }

                // Add 3 directed edges in clockwise order
                void AddEdge(Vector2 a, Vector2 b)
                {
                    long ka = Quantize(a);
                    long kb = Quantize(b);
                    var oppKey = (kb, ka);

                    if (edgeDict.ContainsKey(oppKey))
                    {
                        edgeDict.Remove(oppKey); // Shared internal edge
                    }
                    else
                    {
                        edgeDict[(ka, kb)] = (a, b);
                    }
                }

                AddEdge(v0, v1);
                AddEdge(v1, v2);
                AddEdge(v2, v0);
            }

            if (edgeDict.Count == 0) return new Vector2[0];

            // Chain boundary edges into an ordered perimeter loop
            var lookup = new System.Collections.Generic.Dictionary<long, (Vector2 from, Vector2 to, long nextKey)>();
            long firstKey = 0;
            foreach (var kvp in edgeDict)
            {
                if (firstKey == 0) firstKey = kvp.Key.Item1;
                lookup[kvp.Key.Item1] = (kvp.Value.from, kvp.Value.to, kvp.Key.Item2);
            }

            var loop = new System.Collections.Generic.List<Vector2>();
            long currentKey = firstKey;
            int safety = 0;

            while (lookup.ContainsKey(currentKey) && safety++ < 200)
            {
                var edge = lookup[currentKey];
                loop.Add(edge.from);
                currentKey = edge.nextKey;
                if (currentKey == firstKey) break;
            }

            return loop.ToArray();
        }

        private static Vector2[] CalculateOutwardMiterNormals(Vector2[] poly)
        {
            int n = poly.Length;
            Vector2[] miters = new Vector2[n];
            if (n < 3) return miters;

            // 1. Calculate center of the board polygon
            Vector2 center = Vector2.zero;
            for (int i = 0; i < n; i++) center += poly[i];
            center /= n;

            // 2. Determine winding orientation via signed area (Shoelace)
            float signedArea = 0f;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                signedArea += (poly[i].x * poly[next].y - poly[next].x * poly[i].y);
            }
            bool isCCW = signedArea > 0f;

            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                int next = (i + 1) % n;

                Vector2 e1 = (poly[i] - poly[prev]).normalized;
                Vector2 e2 = (poly[next] - poly[i]).normalized;

                // Outward edge normals
                Vector2 n1 = isCCW ? new Vector2(e1.y, -e1.x) : new Vector2(-e1.y, e1.x);
                Vector2 n2 = isCCW ? new Vector2(e2.y, -e2.x) : new Vector2(-e2.y, e2.x);

                Vector2 bisector = (n1 + n2).normalized;

                // 3. Absolute geometric safety check: must point away from polygon center
                Vector2 toVertex = poly[i] - center;
                if (Vector2.Dot(bisector, toVertex) < 0f)
                {
                    bisector = -bisector;
                }

                float dot = Vector2.Dot(bisector, n1);
                float miterScale = (Mathf.Abs(dot) > 0.1f) ? Mathf.Min(1.0f / Mathf.Abs(dot), 1.15f) : 1.0f;

                miters[i] = bisector * miterScale;
            }

            return miters;
        }

        private Mesh GenerateHexRingMesh(Vector2[] poly, float innerOffset, float outerOffset, bool fadeOuter)
        {
            if (poly == null || poly.Length < 3) return new Mesh();

            Mesh mesh = new Mesh();
            mesh.name = fadeOuter ? "HexHaloMesh" : "HexRimMesh";

            int n = poly.Length;
            Vector2[] miters = CalculateOutwardMiterNormals(poly);

            Vector3[] vertices = new Vector3[n * 2];
            Color[] colors = new Color[n * 2];
            int[] triangles = new int[n * 6];

            for (int i = 0; i < n; i++)
            {
                Vector2 innerPos = poly[i] + miters[i] * innerOffset;
                Vector2 outerPos = poly[i] + miters[i] * (innerOffset + outerOffset);

                vertices[i * 2 + 0] = new Vector3(innerPos.x, innerPos.y, 0f);
                vertices[i * 2 + 1] = new Vector3(outerPos.x, outerPos.y, 0f);

                // Vertex alpha gradient (Full intensity on inner edge, 0 alpha on outer halo edge)
                colors[i * 2 + 0] = Color.white;
                colors[i * 2 + 1] = fadeOuter ? new Color(1f, 1f, 1f, 0f) : Color.white;

                int next = (i + 1) % n;

                int i0 = i * 2 + 0;
                int i1 = i * 2 + 1;
                int i2 = next * 2 + 0;
                int i3 = next * 2 + 1;

                int t = i * 6;
                triangles[t + 0] = i0;
                triangles[t + 1] = i1;
                triangles[t + 2] = i2;

                triangles[t + 3] = i1;
                triangles[t + 4] = i3;
                triangles[t + 5] = i2;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
