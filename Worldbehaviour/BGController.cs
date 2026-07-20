using UnityEngine;

[DefaultExecutionOrder(10000)]
public class ParallaxBackground5Layer_MotionPlus : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform player;
    public Rigidbody2D playerRB2D;

    [Header("Reactive Motion")]
    [Range(0f, 1f)] public float velocityReaction = 0.15f;
    public float velocitySmooth = 10f;

    [Header("Smoothing")]
    [Tooltip("Smooths the camera position used for parallax to remove sub-pixel jitter. Higher = smoother but laggier. 0 = off.")]
    [Range(0f, 50f)] public float camPositionSmooth = 25f;

    [System.Serializable]
    public class Layer
    {
        public MeshRenderer renderer;

        [Header("Parallax & Tiling")]
        [Range(0f, 1f)] public float parallax = 1f;
        public Vector2 worldUnitsPerTile = new Vector2(40f, 40f);

        [Header("Base Drift")]
        public Vector2 uvScrollPerSecond = Vector2.zero;

        [Header("Organic Motion")]
        public float wobbleAmount = 0.1f;
        public float wobbleSpeed  = 0.5f;
        public float noiseAmount  = 0.2f;
        public float noiseSpeed   = 0.8f;

        [Header("Motion Cues (screen-space)")]
        [Tooltip("Slow rotation of UVs (deg/sec). Tiny values like 1–3 look good.")]
        public float rotationPerSecond = 0f;
        [Tooltip("Gently zoom tiling over time (0.01 = +1% per second; negative for zoom-out).")]
        public float tilingZoomPerSecond = 0f;

        [HideInInspector] public Material matInstance;
        [HideInInspector] public Transform quad;
        [HideInInspector] public Vector2 baseTiling;   // cached tiling at current camera size
        [HideInInspector] public float lastOrtho = -1f, lastAspect = -1f;
        [HideInInspector] public Vector2 lastWUPT;
    }

    public Layer[] layers = new Layer[5];

    // --- runtime state ---
    Vector2 smoothedVel;
    Vector2 _accumulatedVelOffset;  // persistent accumulator for velocity reaction
    Vector2 _smoothCamPos;          // smoothed camera position for jitter-free parallax
    Vector3 _lastPlayerPos;
    float _lastOrtho = -1f, _lastAspect = -1f;
    bool _firstFrame = true;

    void Start()
    {
        if (!cam) cam = Camera.main;

        _smoothCamPos = cam ? (Vector2)cam.transform.position : Vector2.zero;

        foreach (var L in layers)
        {
            if (!L?.renderer) continue;
            L.matInstance = L.renderer.material;
            L.quad = L.renderer.transform;
            L.lastWUPT = L.worldUnitsPerTile;
            L.matInstance.renderQueue = 1000; // background
        }
        UpdateSizeAndTiling(true);
    }

    void LateUpdate()
    {
        if (!cam) return;

        float dt = Time.deltaTime;

        // ---- Smooth camera position to remove sub-pixel jitter ----
        Vector2 rawCamPos = cam.transform.position;
        if (_firstFrame)
        {
            _smoothCamPos = rawCamPos;
            _firstFrame = false;
        }
        else if (camPositionSmooth > 0f)
        {
            // Exponential smoothing: fast convergence, no overshoot
            float alpha = 1f - Mathf.Exp(-camPositionSmooth * dt);
            _smoothCamPos = Vector2.Lerp(_smoothCamPos, rawCamPos, alpha);
        }
        else
        {
            _smoothCamPos = rawCamPos;
        }

        Vector2 camPos = _smoothCamPos;

        // ---- Smooth player velocity ----
        Vector2 rawVel = Vector2.zero;
        if (playerRB2D) rawVel = playerRB2D.velocity;
        else if (player)
            rawVel = (Vector2)(player.position - _lastPlayerPos) / Mathf.Max(dt, 1e-6f);
        smoothedVel = Vector2.Lerp(smoothedVel, rawVel, 1f - Mathf.Exp(-velocitySmooth * dt));
        _lastPlayerPos = player ? player.position : _lastPlayerPos;

        // ---- Accumulate velocity reaction offset ----
        // This persists across frames so the effect doesn't get overwritten
        Vector2 react = -smoothedVel * velocityReaction;
        _accumulatedVelOffset.x += (react.x * dt) / Mathf.Max(0.0001f, 40f); // normalised by a reference tile size
        _accumulatedVelOffset.y += (react.y * dt) / Mathf.Max(0.0001f, 40f);
        // Gently decay the accumulator back to zero so it doesn't run away
        _accumulatedVelOffset *= Mathf.Exp(-2f * dt);

        // ---- Resize/retile only on zoom/aspect or WUPT changes ----
        if (Mathf.Abs(cam.orthographicSize - _lastOrtho) > 0.0001f || Mathf.Abs(cam.aspect - _lastAspect) > 0.0001f)
            UpdateSizeAndTiling(true);
        else
            foreach (var L in layers) if (L != null && L.renderer && L.worldUnitsPerTile != L.lastWUPT) FitLayer(L);

        // ---- Update each layer ----
        float t = Time.time;
        foreach (var L in layers)
        {
            if (!L?.renderer || !L.matInstance) continue;

            // Anchor quad to camera (use raw position for the geometry, smooth for UVs)
            var p = L.quad.position;
            L.quad.position = new Vector3(rawCamPos.x, rawCamPos.y, p.z);

            // Base parallax UV (uses smoothed cam pos)
            float u = (camPos.x * L.parallax) / Mathf.Max(0.0001f, L.worldUnitsPerTile.x);
            float v = (camPos.y * L.parallax) / Mathf.Max(0.0001f, L.worldUnitsPerTile.y);

            // Linear drift
            u += L.uvScrollPerSecond.x * t;
            v += L.uvScrollPerSecond.y * t;

            // Organic motion
            u += Mathf.Sin(t * L.wobbleSpeed) * L.wobbleAmount;
            v += Mathf.Cos(t * L.wobbleSpeed) * L.wobbleAmount;
            float n1 = Mathf.PerlinNoise(t * L.noiseSpeed, 0.0f);
            float n2 = Mathf.PerlinNoise(0.0f, t * L.noiseSpeed);
            u += (n1 - 0.5f) * L.noiseAmount;
            v += (n2 - 0.5f) * L.noiseAmount;

            // Velocity reaction (from accumulated offset, scaled per layer)
            u += _accumulatedVelOffset.x * (L.worldUnitsPerTile.x / 40f);
            v += _accumulatedVelOffset.y * (L.worldUnitsPerTile.y / 40f);

            // Rotation & micro zoom of tiling
            Vector2 tiling = L.baseTiling;
            float zoom = 1f + (L.tilingZoomPerSecond * t); // small % over time
            if (zoom < 0.1f) zoom = 0.1f;
            tiling *= zoom;

            if (Mathf.Abs(L.rotationPerSecond) > 0.0001f)
            {
                float ang = L.rotationPerSecond * Mathf.Deg2Rad * t;
                float cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                // rotate the offset vector around center
                float ru = u * cs - v * sn;
                float rv = u * sn + v * cs;
                u = ru; v = rv;
            }

            // Bound and apply
            u = Mathf.Repeat(u, 1f);
            v = Mathf.Repeat(v, 1f);

            L.matInstance.mainTextureScale  = tiling;
            L.matInstance.mainTextureOffset = new Vector2(u, v);
        }
    }

    void UpdateSizeAndTiling(bool force)
    {
        _lastOrtho = cam.orthographicSize;
        _lastAspect = cam.aspect;
        foreach (var L in layers) if (L != null && L.renderer) FitLayer(L);
    }

    void FitLayer(Layer L)
    {
        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        L.quad.localScale = new Vector3(worldW, worldH, 1f);

        Vector2 tiling = new Vector2(
            Mathf.Max(0.0001f, worldW / Mathf.Max(0.0001f, L.worldUnitsPerTile.x)),
            Mathf.Max(0.0001f, worldH / Mathf.Max(0.0001f, L.worldUnitsPerTile.y))
        );

        L.baseTiling = tiling;              // store "base" tiling for zoom
        L.matInstance.mainTextureScale = tiling;

        L.lastOrtho = cam.orthographicSize;
        L.lastAspect = cam.aspect;
        L.lastWUPT = L.worldUnitsPerTile;
    }
}
