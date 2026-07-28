using UnityEngine;
using TMPro;

/// <summary>
/// Makes any visual element (Sprite, UI, or TextMeshPro text) invisible by default,
/// then smoothly fades it in when the player enters the trigger collider and fades
/// it back out when the player leaves.
///
/// Setup:
///   1. Attach this to a GameObject (or its parent) that has a Collider2D with
///      "Is Trigger" enabled.
///   2. The collider defines the zone in which the element becomes visible.
///   3. Assign the target(s) you want to fade. The script auto-detects what's
///      available if you leave them empty:
///        • SpriteRenderer  – for sprites / tile assets
///        • CanvasGroup     – for UI elements (wrap your UI text in a CanvasGroup)
///        • TextMeshPro     – for world-space TMP text
///        • TextMeshProUGUI – for screen-space TMP text
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ProximityFade : MonoBehaviour
{
    // ──────────────────────────────── Inspector ────────────────────────────────

    [Header("Player Detection")]
    [Tooltip("Which layers count as the player (same mask you use on Checkpoint, TriggerEffect, etc.).")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Fade Settings")]
    [Tooltip("How many seconds it takes to fade from invisible to fully visible.")]
    [SerializeField, Range(0.05f, 5f)] private float fadeInDuration = 1f;

    [Tooltip("How many seconds it takes to fade from fully visible to invisible.")]
    [SerializeField, Range(0.05f, 5f)] private float fadeOutDuration = 1f;

    [Header("Targets (auto-detected if left empty)")]
    [Tooltip("Optional. Drag a SpriteRenderer here to fade a sprite.")]
    [SerializeField] private SpriteRenderer spriteTarget;

    [Tooltip("Optional. Drag a CanvasGroup here to fade UI elements (images, text, panels).")]
    [SerializeField] private CanvasGroup canvasGroupTarget;

    [Tooltip("Optional. Drag a TextMeshPro component here to fade world-space text.")]
    [SerializeField] private TextMeshPro tmpTarget;

    [Tooltip("Optional. Drag a TextMeshProUGUI component here to fade screen-space UI text.")]
    [SerializeField] private TextMeshProUGUI tmpUITarget;

    // ──────────────────────────────── State ────────────────────────────────────

    private float currentAlpha;
    private float targetAlpha;
    private bool playerInside;

    // ──────────────────────────────── Unity ────────────────────────────────────

    private void Reset()
    {
        // Same convenience as your other trigger scripts.
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        AutoDetectTargets();

        // Start fully invisible.
        currentAlpha = 0f;
        targetAlpha  = 0f;
        ApplyAlpha(0f);
    }

    private void Update()
    {
        // Early-out if we're already at the desired alpha.
        if (Mathf.Approximately(currentAlpha, targetAlpha)) return;

        float duration = playerInside ? fadeInDuration : fadeOutDuration;
        float speed    = 1f / Mathf.Max(duration, 0.001f); // avoid division by zero

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);
        ApplyAlpha(currentAlpha);
    }

    // ──────────────────────────── Trigger Callbacks ────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInside = true;
        targetAlpha  = 1f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerInside = false;
        targetAlpha  = 0f;
    }

    // ──────────────────────────── Helper Methods ──────────────────────────────

    /// <summary>Checks if the collider belongs to the player layer.</summary>
    private bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    /// <summary>Applies the given alpha value to every assigned target.</summary>
    private void ApplyAlpha(float alpha)
    {
        if (spriteTarget != null)
        {
            Color c = spriteTarget.color;
            c.a = alpha;
            spriteTarget.color = c;
        }

        if (canvasGroupTarget != null)
        {
            canvasGroupTarget.alpha = alpha;
        }

        if (tmpTarget != null)
        {
            Color c = tmpTarget.color;
            c.a = alpha;
            tmpTarget.color = c;
        }

        if (tmpUITarget != null)
        {
            Color c = tmpUITarget.color;
            c.a = alpha;
            tmpUITarget.color = c;
        }
    }

    /// <summary>
    /// Auto-detects targets on this GameObject or its children when none have
    /// been explicitly assigned in the Inspector.
    /// </summary>
    private void AutoDetectTargets()
    {
        if (spriteTarget == null)
            spriteTarget = GetComponentInChildren<SpriteRenderer>();

        if (canvasGroupTarget == null)
            canvasGroupTarget = GetComponentInChildren<CanvasGroup>();

        if (tmpTarget == null)
            tmpTarget = GetComponentInChildren<TextMeshPro>();

        if (tmpUITarget == null)
            tmpUITarget = GetComponentInChildren<TextMeshProUGUI>();
    }
}
