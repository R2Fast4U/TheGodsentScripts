using UnityEngine;

public class ParrallaxBGFinal : MonoBehaviour
{
    [Header("Leave empty to auto-assign in game")]
    public Camera cam;
    public Transform subject;

    [Header("Parallax Settings")]
    [Tooltip("0 = stays still (far background), 1 = moves with camera (same depth as player), >1 = moves faster (foreground)")]
    [Range(-1f, 1f)]
    public float parallaxFactor = 0.5f;

    [Header("Smoothing")]
    [Tooltip("Smooths position to avoid jitter from camera sub-pixel jumps. Higher = smoother. 0 = off.")]
    [Range(0f, 50f)]
    public float positionSmooth = 30f;

    private Vector2 startPosition;
    private float startZ;
    private Vector2 startCamPosition;
    private Vector3 _currentPos;
    private bool _firstFrame = true;

    private void Start()
    {
        // Auto-assign the Main Camera (Cinemachine drives it automatically)
        if (cam == null)
        {
            cam = Camera.main;
        }

        // Auto-assign the Player as the subject if none was set
        if (subject == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                subject = player.transform;
            }
        }

        startPosition = transform.position;
        startZ = transform.position.z;
        startCamPosition = cam.transform.position;
        _currentPos = transform.position;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        // How far the camera has moved since the start
        Vector2 travel = (Vector2)cam.transform.position - startCamPosition;

        // Apply the parallax factor
        Vector2 newPosition = startPosition + (travel * parallaxFactor);

        Vector3 targetPos;
        if (parallaxFactor <= 1)
        {
            targetPos = new Vector3(newPosition.x, newPosition.y, startZ);
        }
        else
        {
            targetPos = new Vector3(-newPosition.x, -newPosition.y, startZ);
        }

        // Smooth the position to eliminate sub-pixel jitter
        if (_firstFrame)
        {
            _currentPos = targetPos;
            _firstFrame = false;
        }
        else if (positionSmooth > 0f)
        {
            float alpha = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            _currentPos = Vector3.Lerp(_currentPos, targetPos, alpha);
        }
        else
        {
            _currentPos = targetPos;
        }

        transform.position = _currentPos;
    }
}
