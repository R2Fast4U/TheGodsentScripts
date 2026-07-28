using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a circular ("curve slider") health bar from PlayerStats. The fill image must be an
/// Image with Image Type = Filled and Fill Method = Radial 360/180/90; this just sets its
/// fillAmount to the health fraction.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [Tooltip("Image with Image Type = Filled, Fill Method = Radial 360 (the ring that empties).")]
    [SerializeField] private Image fillImage;
    [Tooltip("Higher = snappier. 0 = instant.")]
    [SerializeField] private float smoothSpeed = 8f;

    private float targetFill = 1f;

    private void OnEnable()
    {
        if (stats == null || fillImage == null) return;

        stats.EnsureInitialized();
        stats.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(stats.CurrentHealth, stats.EffectiveMaxHealth);
        fillImage.fillAmount = targetFill; // snap on enable, no animation
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void Update()
    {
        if (fillImage == null) return;

        if (smoothSpeed <= 0f)
            fillImage.fillAmount = targetFill;
        else
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.unscaledDeltaTime * smoothSpeed);
    }
}
