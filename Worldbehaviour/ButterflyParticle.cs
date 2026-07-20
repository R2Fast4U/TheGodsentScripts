using UnityEngine;

public class ButterflyParticle : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] butterflySprites;
    [SerializeField] private float animationSpeed = 8f;
    [SerializeField] private bool randomizeStartFrame = true;
    
    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 2f;
    [SerializeField] private float flightAmplitude = 1f;
    [SerializeField] private float directionChangeInterval = 3f;
    [SerializeField] private float maxFlightDistance = 5f;
    
    [Header("Movement Pattern")]
    [SerializeField] private bool enableWandering = true;
    [SerializeField] private float wanderSpeed = 1f;
    [SerializeField] private float wanderAmplitude = 0.5f;
    [SerializeField] private Vector3 wanderDirection = Vector3.right;
    
    [Header("Butterfly Behavior")]
    [SerializeField] private bool enableLanding = true;
    [SerializeField] private float landingChance = 0.1f;
    [SerializeField] private float landingDuration = 2f;
    [SerializeField] private LayerMask landingLayer = 1;
    
    [Header("Visual Effects")]
    [SerializeField] private bool enableFading = true;
    [SerializeField] private float fadeSpeed = 0.5f;
    [SerializeField] private float minAlpha = 0.6f;
    [SerializeField] private float maxAlpha = 1f;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 20f;
    [SerializeField] private bool destroyOnLifetime = true;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 currentDirection;
    private float animationTimer;
    private int currentSpriteIndex;
    private float directionTimer;
    private float landingTimer;
    private bool isLanding;
    private float startTime;
    private float currentAlpha;
    
    private void Start()
    {
        startPosition = transform.position;
        startTime = Time.time;
        
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        if (spriteRenderer != null)
        {
            currentAlpha = spriteRenderer.color.a;
        }
        
        // Randomize starting frame
        if (randomizeStartFrame && butterflySprites.Length > 0)
        {
            currentSpriteIndex = Random.Range(0, butterflySprites.Length);
            UpdateSprite();
        }
        
        // Set initial direction
        SetNewDirection();
        
        // Set initial target position
        SetNewTargetPosition();
    }
    
    private void Update()
    {
        if (isLanding)
        {
            UpdateLanding();
        }
        else
        {
            UpdateFlight();
        }
        
        UpdateAnimation();
        UpdateFading();
        
        if (destroyOnLifetime && Time.time - startTime > lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    private void UpdateFlight()
    {
        // Move towards target position
        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position += direction * flightSpeed * Time.deltaTime;
        
        // Update direction timer
        directionTimer += Time.deltaTime;
        if (directionTimer >= directionChangeInterval)
        {
            SetNewDirection();
            directionTimer = 0f;
        }
        
        // Check if we've reached target position
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            SetNewTargetPosition();
        }
        
        // Random landing chance
        if (enableLanding && Random.Range(0f, 1f) < landingChance * Time.deltaTime)
        {
            TryLand();
        }
        
        // Update sprite direction based on movement
        UpdateSpriteDirection(direction);
    }
    
    private void UpdateLanding()
    {
        landingTimer += Time.deltaTime;
        
        if (landingTimer >= landingDuration)
        {
            isLanding = false;
            landingTimer = 0f;
            SetNewTargetPosition();
        }
    }
    
    private void UpdateAnimation()
    {
        if (butterflySprites.Length == 0) return;
        
        animationTimer += Time.deltaTime * animationSpeed;
        
        if (animationTimer >= 1f)
        {
            animationTimer = 0f;
            currentSpriteIndex = (currentSpriteIndex + 1) % butterflySprites.Length;
            UpdateSprite();
        }
    }
    
    private void UpdateSprite()
    {
        if (spriteRenderer != null && butterflySprites.Length > 0)
        {
            spriteRenderer.sprite = butterflySprites[currentSpriteIndex];
        }
    }
    
    private void UpdateSpriteDirection(Vector3 direction)
    {
        if (spriteRenderer != null)
        {
            // Flip sprite based on horizontal direction
            if (direction.x > 0.1f)
            {
                spriteRenderer.flipX = false;
            }
            else if (direction.x < -0.1f)
            {
                spriteRenderer.flipX = true;
            }
        }
    }
    
    private void UpdateFading()
    {
        if (!enableFading) return;
        
        float time = Time.time * fadeSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1f) * 0.5f);
        
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
    
    private void SetNewDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        currentDirection = Quaternion.Euler(0, 0, randomAngle) * Vector3.right;
    }
    
    private void SetNewTargetPosition()
    {
        if (enableWandering)
        {
            // Create wandering movement
            Vector3 wanderOffset = wanderDirection * Mathf.Sin(Time.time * wanderSpeed) * wanderAmplitude;
            targetPosition = startPosition + currentDirection * Random.Range(2f, maxFlightDistance) + wanderOffset;
        }
        else
        {
            // Simple random position within range
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(2f, maxFlightDistance);
            targetPosition = startPosition + new Vector3(randomCircle.x, randomCircle.y, 0);
        }
    }
    
    private void TryLand()
    {
        // Raycast down to find landing spot
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 3f, landingLayer);
        if (hit.collider != null)
        {
            isLanding = true;
            landingTimer = 0f;
            transform.position = hit.point + Vector2.up * 0.1f; // Slightly above ground
        }
    }
    
    // Public methods for external control
    public void SetFlightSpeed(float speed)
    {
        flightSpeed = speed;
    }
    
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
    
    public void SetSprites(Sprite[] sprites)
    {
        butterflySprites = sprites;
        if (butterflySprites.Length > 0)
        {
            UpdateSprite();
        }
    }
    
    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
        startTime = Time.time;
    }
    
    public void ForceLand()
    {
        TryLand();
    }
    
    public void SetNewStartPosition(Vector3 newPosition)
    {
        startPosition = newPosition;
        SetNewTargetPosition();
    }
    
    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startPosition, maxFlightDistance);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
} 