using UnityEngine;
using Cinemachine;

public class CinemachineOffsetController : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public float smoothTime = 0.3f;

    [Header("Hit Zoom (attack landed)")]
    public float hitZoomAmount = 0.5f;
    public float hitZoomDuration = 0.06f;
    public float hitZoomRecoverySpeed = 18f;

    [Header("Player Hit (got hurt)")]
    [Tooltip("How much the camera zooms OUT when the player is hit (added to orthographic size / FOV).")]
    public float playerHitZoomOutAmount = 2f;
    [Tooltip("How long the zoom-out is held before it eases back.")]
    public float playerHitZoomOutDuration = 0.08f;
    public float playerHitZoomOutRecoverySpeed = 8f;
    [Tooltip("Peak positional shake distance when the player is hit.")]
    public float playerHitShakeAmplitude = 1f;
    public float playerHitShakeDuration = 0.25f;

    private Vector3 targetOffset;
    private Vector3 currentOffset;
    private Vector3 velocity = Vector3.zero;

    private float defaultLensValue;
    private float currentLensValue;

    // Generalized lens "punch": zoom-in (negative) or zoom-out (positive) share this machinery.
    private float lensHoldTimer;
    private float lensTargetValue;
    private float lensRecoverySpeed;
    private bool isLensPunching;

    // Positional shake state.
    private float shakeTimer;
    private float shakeDuration;
    private float shakeAmplitude;

    public static CinemachineOffsetController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }


    void Start()
    {
        currentOffset = Vector3.zero;
        targetOffset = Vector3.zero;

        if (virtualCamera != null)
        {
            defaultLensValue = virtualCamera.m_Lens.Orthographic
                ? virtualCamera.m_Lens.OrthographicSize
                : virtualCamera.m_Lens.FieldOfView;
            currentLensValue = defaultLensValue;
        }
    }

    void LateUpdate()
    {
        if (virtualCamera == null)
            return;

        currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref velocity, smoothTime);

        // Layer a decaying positional shake on top of the framing offset.
        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float damper = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
            shakeOffset = (Vector3)(Random.insideUnitCircle * (shakeAmplitude * damper));
        }

        var transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
            transposer.m_TrackedObjectOffset = currentOffset + shakeOffset;

        if (isLensPunching)
        {
            lensHoldTimer -= Time.deltaTime;

            if (lensHoldTimer > 0f)
            {
                currentLensValue = lensTargetValue;
            }
            else
            {
                currentLensValue = Mathf.Lerp(currentLensValue, defaultLensValue, Time.deltaTime * lensRecoverySpeed);
                if (Mathf.Abs(currentLensValue - defaultLensValue) < 0.01f)
                {
                    currentLensValue = defaultLensValue;
                    isLensPunching = false;
                }
            }

            if (virtualCamera.m_Lens.Orthographic)
            {
                virtualCamera.m_Lens.OrthographicSize = currentLensValue;
            }
            else
            {
                virtualCamera.m_Lens.FieldOfView = currentLensValue;
            }
        }
    }

    public void TriggerHitZoom()
    {
        // Zoom IN briefly (negative punch) — used when the player's attack lands.
        PunchLens(-hitZoomAmount, hitZoomDuration, hitZoomRecoverySpeed);
    }

    /// <summary>Camera reaction when the player takes a hit: a quick zoom-out plus a shake.</summary>
    public void TriggerPlayerHit()
    {
        PunchLens(playerHitZoomOutAmount, playerHitZoomOutDuration, playerHitZoomOutRecoverySpeed);
        Shake(playerHitShakeAmplitude, playerHitShakeDuration);
    }

    /// <summary>Nudge the lens by <paramref name="amount"/> (negative = zoom in, positive = zoom out).</summary>
    public void PunchLens(float amount, float duration, float recoverySpeed)
    {
        if (virtualCamera == null || defaultLensValue <= 0f)
            return;

        lensTargetValue = defaultLensValue + amount;
        lensHoldTimer = duration;
        lensRecoverySpeed = recoverySpeed;
        currentLensValue = lensTargetValue;

        if (virtualCamera.m_Lens.Orthographic)
            virtualCamera.m_Lens.OrthographicSize = currentLensValue;
        else
            virtualCamera.m_Lens.FieldOfView = currentLensValue;

        isLensPunching = true;
    }

    /// <summary>Start a decaying positional camera shake.</summary>
    public void Shake(float amplitude, float duration)
    {
        // Don't let a weaker/expiring shake stomp a stronger one already in progress.
        if (shakeTimer > 0f && (shakeAmplitude * Mathf.Clamp01(shakeTimer / shakeDuration)) > amplitude)
            return;

        shakeAmplitude = amplitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }

    public void SetVerticalOffset(float yOffset)
    {
        targetOffset = new Vector3(0, yOffset, 0);
    }

    public void ResetOffset()
    {
        targetOffset = Vector3.zero;
    }
}
