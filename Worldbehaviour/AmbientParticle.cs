using UnityEngine;

public class AmbientParticle : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private Vector3 floatDirection = Vector3.up;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    
    [Header("Scale Settings")]
    [SerializeField] private bool enablePulsing = false;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmplitude = 0.2f;
    
    [Header("Fade Settings")]
    [SerializeField] private bool enableFading = false;
    [SerializeField] private float fadeSpeed = 1f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1f;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private bool destroyOnLifetime = true;
    
    private Vector3 startPosition;
    private Vector3 startScale;
    private SpriteRenderer spriteRenderer;
    private ParticleSystem particleSystem;
    private float startTime;
    private float currentAlpha;
    
    private void Start()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
        startTime = Time.time;
        
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        particleSystem = GetComponent<ParticleSystem>();
        
        if (spriteRenderer != null)
        {
            currentAlpha = spriteRenderer.color.a;
        }
    }
    
    private void Update()
    {
        if (enableFloating)
        {
            UpdateFloating();
        }
        
        if (enableRotation)
        {
            UpdateRotation();
        }
        
        if (enablePulsing)
        {
            UpdatePulsing();
        }
        
        if (enableFading)
        {
            UpdateFading();
        }
        
        if (destroyOnLifetime && Time.time - startTime > lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    private void UpdateFloating()
    {
        float time = Time.time * floatSpeed;
        Vector3 offset = floatDirection.normalized * Mathf.Sin(time) * floatAmplitude;
        transform.position = startPosition + offset;
    }
    
    private void UpdateRotation()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
    }
    
    private void UpdatePulsing()
    {
        float time = Time.time * pulseSpeed;
        float scaleMultiplier = 1f + Mathf.Sin(time) * pulseAmplitude;
        transform.localScale = startScale * scaleMultiplier;
    }
    
    private void UpdateFading()
    {
        float time = Time.time * fadeSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1f) * 0.5f);
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
        
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            Color startColor = main.startColor.color;
            startColor.a = alpha;
            main.startColor = startColor;
        }
    }
    
    // Public methods for external control
    public void SetFloating(bool enabled, float speed = 1f, float amplitude = 0.5f)
    {
        enableFloating = enabled;
        floatSpeed = speed;
        floatAmplitude = amplitude;
    }
    
    public void SetRotation(bool enabled, float speed = 30f)
    {
        enableRotation = enabled;
        rotationSpeed = speed;
    }
    
    public void SetPulsing(bool enabled, float speed = 2f, float amplitude = 0.2f)
    {
        enablePulsing = enabled;
        pulseSpeed = speed;
        pulseAmplitude = amplitude;
    }
    
    public void SetFading(bool enabled, float speed = 1f)
    {
        enableFading = enabled;
        fadeSpeed = speed;
    }
    
    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
        startTime = Time.time;
    }
    
    public void ResetToStart()
    {
        transform.position = startPosition;
        transform.localScale = startScale;
        startTime = Time.time;
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = currentAlpha;
            spriteRenderer.color = color;
        }
    }
} 