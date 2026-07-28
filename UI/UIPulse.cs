using UnityEngine;

/// <summary>
/// Loops a CanvasGroup's alpha between min and max — a smooth "breathing" fade in/out, with an
/// optional Perlin-noise flicker layered on top for a dampened, living flicker. Great for a
/// "press any button to start" prompt. Uses unscaled time so it runs in menus (timeScale 0).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPulse : MonoBehaviour
{
    [SerializeField] private CanvasGroup target;

    [Header("Breathing (fade in/out)")]
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;
    [Tooltip("Full fade-in+out cycles per second (0.5 = one 2-second breath).")]
    [SerializeField] private float speed = 0.8f;

    [Header("Dampened Flicker")]
    [Tooltip("How much random flicker to add on top of the breathing (0 = none).")]
    [SerializeField, Range(0f, 1f)] private float flicker = 0.08f;
    [SerializeField] private float flickerSpeed = 14f;

    private float seed;

    private void Reset() => target = GetComponent<CanvasGroup>();

    private void OnEnable()
    {
        if (target == null) target = GetComponent<CanvasGroup>();
        seed = Random.value * 100f; // so multiple pulses don't flicker in sync
    }

    private void Update()
    {
        if (target == null) return;

        // Smooth breathing: sine mapped to 0..1.
        float breath = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, breath);

        // Dampened flicker: Perlin noise centred on 0.
        if (flicker > 0f)
        {
            float n = Mathf.PerlinNoise(seed + Time.unscaledTime * flickerSpeed, 0f) - 0.5f;
            alpha += n * flicker;
        }

        target.alpha = Mathf.Clamp01(alpha);
    }
}
