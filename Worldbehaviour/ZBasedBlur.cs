using UnityEngine;
using UnityEngine.Serialization;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class ZBasedBlur : MonoBehaviour
{   


    [Header("Focus Thresholds")]
    [Tooltip("Objects between Min and Max Z are in perfect focus (no blur).")]

    [SerializeField]
    public float focusRangeMin = -6f;
    [Tooltip("Objects between Min and Max Z are in perfect focus (no blur).")]
    [SerializeField]
    public float focusRangeMax = 6f;

    [Header("Blur Strength Rates")]
    [Tooltip("How fast the blur increases as the object moves further into the background (Z > focusRangeMax).")]
    [SerializeField]
    public float blurRateBackground = 0.03f;
    [Tooltip("How fast the blur increases as the object moves closer to the foreground (Z < focusRangeMin).")]
    [SerializeField]
    public float blurRateForeground = 0.06f;

    [Tooltip("The maximum blur strength allowed for objects in the background (Z > focusRangeMax).")]
    [Range(0f, 10f)]
    [FormerlySerializedAs("maxBlur")]
    public float maxBlurBackground = 5f;
    [Tooltip("The maximum blur strength allowed for objects in the foreground (Z < focusRangeMin).")]
    [Range(0f, 10f)]
    public float maxBlurForeground = 1.5f;

    [Header("Material Settings")]
    [Tooltip("The shader property name for controlling the blur amount.")]
    public string blurPropertyName = "_BlurAmount";

    [Header("Editor Controls")]
    [Tooltip("Check this to preview and update the blur in the editor in real-time.")]
    public bool updateInEditor = true;

    [SerializeField]
    [Tooltip("The calculated blur amount currently applied to the renderer (Read Only).")]
    private float calculatedBlurAmount;

    private Renderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int blurPropertyId;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        spriteRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        blurPropertyId = Shader.PropertyToID(blurPropertyName);
        UpdateBlur();
    }

    private void Update()
    {
        // Update in Editor if checked, or always update during play mode
        if (Application.isPlaying || updateInEditor)
        {
            UpdateBlur();
        }
    }

    private void UpdateBlur()
    {
        // Lazy-initialize to guard against calls before Start() (e.g. ExecuteInEditMode)
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<Renderer>();
        if (spriteRenderer == null) return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        float currentZ = transform.position.z;

        if (currentZ > focusRangeMax)
        {
            // Background blur
            calculatedBlurAmount = (currentZ - focusRangeMax) * blurRateBackground;
            calculatedBlurAmount = Mathf.Clamp(calculatedBlurAmount, 0f, maxBlurBackground);
        }
        else if (currentZ < focusRangeMin)
        {
            // Foreground blur
            calculatedBlurAmount = (focusRangeMin - currentZ) * blurRateForeground;
            calculatedBlurAmount = Mathf.Clamp(calculatedBlurAmount, 0f, maxBlurForeground);
        }
        else
        {
            // In focus
            calculatedBlurAmount = 0f;
        }

        // Apply using MaterialPropertyBlock to avoid instantiating new materials (prevents memory leaks and maintains performance)
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(blurPropertyId, calculatedBlurAmount);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnValidate()
    {
        // Re-initialize PropertyID in case the property name is changed in the Inspector
        blurPropertyId = Shader.PropertyToID(blurPropertyName);
    }
}
