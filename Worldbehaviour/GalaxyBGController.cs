using UnityEngine;

[DefaultExecutionOrder(10000)] // run after CinemachineBrain
[RequireComponent(typeof(MeshRenderer))]
public class InfiniteTiledBG_OrganicDrift : MonoBehaviour
{
    [Header("Camera with CinemachineBrain")]
    public Camera cam;  // assign your Main Camera

    [Header("Parallax & Tiling")]
    [Range(0f, 1f)] public float parallax = 0.1f;
    public Vector2 worldUnitsPerTile = new Vector2(20f, 20f);

    [Header("Base Drift (linear scroll)")]
    public Vector2 uvScrollPerSecond = Vector2.zero;

    [Header("Organic Motion")]
    [Tooltip("Amplitude of sine wobble in UV space")]
    public float wobbleAmount = 0.005f;
    [Tooltip("Speed of sine wobble (Hz-like, lower = slower)")]
    public float wobbleSpeed = 0.05f;

    [Tooltip("Amplitude of Perlin drift in UV space")]
    public float noiseAmount = 0.01f;
    [Tooltip("Speed factor for Perlin noise evolution")]
    public float noiseSpeed = 0.02f;

    private Material mat;
    private MeshRenderer mr;

    // cache so we only resize when zoom/aspect changes
    private float lastOrthoSize = -1f;
    private float lastAspect = -1f;
    private Vector2 lastWorldUnitsPerTile;

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mat = mr.material; // instance so we don't mutate the shared one
    }

    void Start()
    {
        if (!cam) cam = Camera.main;
        lastWorldUnitsPerTile = worldUnitsPerTile;
        UpdateSizeAndTiling(force:true);
    }

    void LateUpdate()
    {
        if (!cam) return;

        // update size only if zoom/aspect/tiling changes
        if (Mathf.Abs(cam.orthographicSize - lastOrthoSize) > 0.0001f ||
            Mathf.Abs(cam.aspect - lastAspect) > 0.0001f ||
            worldUnitsPerTile != lastWorldUnitsPerTile)
        {
            UpdateSizeAndTiling(force:true);
        }

        // keep quad centered on camera
        transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);

        // --- Base parallax offset ---
        float u = (cam.transform.position.x * parallax) / Mathf.Max(0.0001f, worldUnitsPerTile.x);
        float v = (cam.transform.position.y * parallax) / Mathf.Max(0.0001f, worldUnitsPerTile.y);

        // --- Linear drift ---
        u += uvScrollPerSecond.x * Time.time;
        v += uvScrollPerSecond.y * Time.time;

        // --- Organic sine wobble ---
        float t = Time.time;
        u += Mathf.Sin(t * wobbleSpeed) * wobbleAmount;
        v += Mathf.Cos(t * wobbleSpeed) * wobbleAmount;

        // --- Organic Perlin drift ---
        float n1 = Mathf.PerlinNoise(t * noiseSpeed, 0.0f);
        float n2 = Mathf.PerlinNoise(0.0f, t * noiseSpeed);
        u += (n1 - 0.5f) * noiseAmount;
        v += (n2 - 0.5f) * noiseAmount;

        // --- Keep offsets bounded ---
        u = Mathf.Repeat(u, 1f);
        v = Mathf.Repeat(v, 1f);

        mat.mainTextureOffset = new Vector2(u, v);
    }

    void UpdateSizeAndTiling(bool force = false)
    {
        float worldH = cam.orthographicSize * 2f;
        float worldW = worldH * cam.aspect;

        // scale quad to camera view
        transform.localScale = new Vector3(worldW, worldH, 1f);

        // how many repeats across the quad
        Vector2 tiling = new Vector2(
            Mathf.Max(0.0001f, worldW / Mathf.Max(0.0001f, worldUnitsPerTile.x)),
            Mathf.Max(0.0001f, worldH / Mathf.Max(0.0001f, worldUnitsPerTile.y))
        );

        mat.mainTextureScale = tiling;

        lastOrthoSize = cam.orthographicSize;
        lastAspect = cam.aspect;
        lastWorldUnitsPerTile = worldUnitsPerTile;
    }
}
