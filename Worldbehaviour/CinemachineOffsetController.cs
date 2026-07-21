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

    private float defaultLensValue;
    private float hitZoomTimer;
    private float currentLensValue;
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
        var transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
            transposer.m_TrackedObjectOffset = currentOffset;

        if (isHitZooming)
        {
            hitZoomTimer -= Time.deltaTime;

            if (hitZoomTimer > 0f)
            {
                currentLensValue = defaultLensValue - hitZoomAmount;
            }
            else
            {
                currentLensValue = Mathf.Lerp(currentLensValue, defaultLensValue, Time.deltaTime * hitZoomRecoverySpeed);
                if (Mathf.Abs(currentLensValue - defaultLensValue) < 0.01f)
                {
                    currentLensValue = defaultLensValue;
                    isHitZooming = false;
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
        if (virtualCamera == null || defaultLensValue <= 0f)
            return;

        hitZoomTimer = hitZoomDuration;
        currentLensValue = defaultLensValue - hitZoomAmount;

        if (virtualCamera.m_Lens.Orthographic)
        {
            virtualCamera.m_Lens.OrthographicSize = currentLensValue;
        }
        else
        {
            virtualCamera.m_Lens.FieldOfView = currentLensValue;
        }

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
