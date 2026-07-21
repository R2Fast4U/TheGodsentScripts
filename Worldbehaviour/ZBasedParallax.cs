using UnityEngine;

[ExecuteInEditMode]
public class ZBasedParallax : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-assign the Main Camera.")]
    public Camera cam;

    [Header("Parallax Settings")]
    [Tooltip("The Z-coordinate where objects do not shift at all (usually the player's plane).")]
    public float gameplayPlaneZ = 0f;

    [Tooltip("Smooths position to avoid jitter from camera sub-pixel jumps. Higher = smoother, 0 = off.")]
    [Range(0f, 50f)]
    public float positionSmooth = 49f;

    [Header("Editor Helpers")]
    [Tooltip("If checked, the script will automatically calculate and display the Parallax Factor in the inspector based on Z.")]
    public bool autoRecalculateInEditor = true;

    [ReadOnlyPlayMode]
    [SerializeField]
    private float calculatedParallaxFactor;

    private Vector2 startPosition;
    private float startZ;
    private Vector2 startCamPosition;
    private Vector3 currentPos;
    private bool firstFrame = true;
    private float cachedCameraZ = -10f;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            startCamPosition = cam.transform.position;
            cachedCameraZ = cam.transform.position.z;
        }

        startZ = transform.position.z;
        CalculateParallaxFactor();

        // The position where the designer placed the object in the editor
        Vector2 placedPosition = transform.position;

        // Calculate the adjusted startPosition so that when the camera is centered on placedPosition,
        // the object aligns perfectly with placedPosition.
        if (cam != null)
        {
            startPosition = placedPosition - (placedPosition - startCamPosition) * calculatedParallaxFactor;
        }
        else
        {
            startPosition = placedPosition;
        }

        currentPos = new Vector3(startPosition.x, startPosition.y, startZ);
        
        // Immediately apply initial offset at runtime so it doesn't pop/jump
        if (Application.isPlaying)
        {
            transform.position = currentPos;
        }
        
        firstFrame = true;
    }

    private void CalculateParallaxFactor()
    {
        if (cam == null) return;

        // Use current camera Z if playing, or default/estimate in editor
        float camZ = Application.isPlaying ? cam.transform.position.z : cachedCameraZ;
        
        // Avoid division by zero if camera is at the gameplay plane
        if (Mathf.Approximately(camZ, gameplayPlaneZ))
        {
            calculatedParallaxFactor = 0f;
            return;
        }

        // Relative Z distance of this object from the gameplay plane
        float relativeZ = startZ - gameplayPlaneZ;
        
        // Distance from camera to the gameplay plane
        float camDistanceToPlane = camZ - gameplayPlaneZ;

        // Formula mimics true perspective projection:
        // factor = relativeZ / (relativeZ - camDistanceToPlane)
        // This naturally gives:
        // - Z = 0 (gameplay plane) -> factor = 0 (scrolls 100% normally, moves 0% with camera)
        // - Z moving towards camera -> factor becomes negative (scrolls faster than gameplay, foreground)
        // - Z moving away from camera -> factor becomes positive, approaching 1 (scrolls slower, background)
        float denominator = relativeZ - camDistanceToPlane;
        if (Mathf.Abs(denominator) < 0.001f)
        {
            denominator = 0.001f * Mathf.Sign(denominator);
        }

        calculatedParallaxFactor = relativeZ / denominator;
    }

    private void LateUpdate()
    {
        // Allow recalculating in editor during design
        if (!Application.isPlaying)
        {
            if (autoRecalculateInEditor)
            {
                if (cam == null) cam = Camera.main;
                if (cam != null) cachedCameraZ = cam.transform.position.z;
                startPosition = transform.position;
                startZ = transform.position.z;
                CalculateParallaxFactor();
            }
            return;
        }

        if (cam == null) return;

        // How far the camera has moved since the start
        Vector2 travel = (Vector2)cam.transform.position - startCamPosition;

        // Apply the calculated parallax factor
        // The object moves with the camera by (travel * factor)
        // So factor = 1 means it moves 100% with the camera (stays static on screen)
        // factor = 0 means it moves 0% with the camera (stays static in the world)
        Vector2 newPosition = startPosition + (travel * calculatedParallaxFactor);
        Vector3 targetPos = new Vector3(newPosition.x, newPosition.y, startZ);

        // Smooth the position to eliminate sub-pixel jitter
        if (firstFrame)
        {
            currentPos = targetPos;
            firstFrame = false;
        }
        else if (positionSmooth > 0f)
        {
            float alpha = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            currentPos = Vector3.Lerp(currentPos, targetPos, alpha);
        }
        else
        {
            currentPos = targetPos;
        }

        transform.position = currentPos;
    }

    // Reset initialization if moved in editor
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CalculateParallaxFactor();
        }
    }
}

// Simple attribute helper to show read-only field in inspector
public class ReadOnlyPlayModeAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyPlayModeAttribute))]
public class ReadOnlyPlayModeDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif
