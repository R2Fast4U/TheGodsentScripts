using UnityEngine;
using Cinemachine;

public class CinemachineOffsetController : MonoBehaviour
{
    public CinemachineVirtualCamera virtualCamera;
    public float smoothTime = 0.3f;

    [Header("Hit Zoom")]
    public float hitZoomAmount = 0.5f;
    public float hitZoomDuration = 0.06f;
    public float hitZoomRecoverySpeed = 18f;

    private Vector3 targetOffset;
    private Vector3 currentOffset;
    private Vector3 velocity = Vector3.zero;

    private float defaultOrthoSize;
    private float hitZoomTimer;
    private float currentOrthoSize;
    private bool isHitZooming;

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
            defaultOrthoSize = virtualCamera.m_Lens.OrthographicSize;
            currentOrthoSize = defaultOrthoSize;
        }
    }

    void LateUpdate()
    {
        if (virtualCamera == null)
            return;

        currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref velocity, smoothTime);
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
            transposer.m_TrackedObjectOffset = currentOffset;

        if (isHitZooming)
        {
            hitZoomTimer -= Time.deltaTime;

            if (hitZoomTimer > 0f)
            {
                currentOrthoSize = defaultOrthoSize - hitZoomAmount;
            }
            else
            {
                currentOrthoSize = Mathf.Lerp(currentOrthoSize, defaultOrthoSize, Time.deltaTime * hitZoomRecoverySpeed);
                if (Mathf.Abs(currentOrthoSize - defaultOrthoSize) < 0.01f)
                {
                    currentOrthoSize = defaultOrthoSize;
                    isHitZooming = false;
                }
            }

            virtualCamera.m_Lens.OrthographicSize = currentOrthoSize;
        }
    }

    public void TriggerHitZoom()
    {
        if (virtualCamera == null || defaultOrthoSize <= 0f)
            return;

        hitZoomTimer = hitZoomDuration;
        currentOrthoSize = defaultOrthoSize - hitZoomAmount;
        virtualCamera.m_Lens.OrthographicSize = currentOrthoSize;
        isHitZooming = true;
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
