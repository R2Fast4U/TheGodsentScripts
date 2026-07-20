using UnityEngine;

public class ParticleController : MonoBehaviour
{
    [Header("Particle System")]
    [SerializeField] private ParticleSystem particleSystem;
    
    [Header("Trigger Settings")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private bool playOnTrigger = false;
    [SerializeField] private bool playOnCollision = false;
    [SerializeField] private string[] triggerTags = { "Player" };
    
    [Header("Animation Settings")]
    [SerializeField] private bool loopParticles = true;
    [SerializeField] private float playDuration = 2f;
    [SerializeField] private bool autoDestroy = false;
    [SerializeField] private float destroyDelay = 3f;
    
    [Header("Position Settings")]
    [SerializeField] private bool followTarget = false;
    [SerializeField] private Transform targetToFollow;
    [SerializeField] private Vector3 offset = Vector3.zero;
    
    private void Start()
    {
        if (particleSystem == null)
            particleSystem = GetComponent<ParticleSystem>();
            
        if (playOnStart)
            PlayParticles();
    }
    
    private void Update()
    {
        if (followTarget && targetToFollow != null)
        {
            transform.position = targetToFollow.position + offset;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!playOnTrigger) return;
        
        if (ShouldTrigger(other.tag))
        {
            PlayParticles();
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!playOnCollision) return;
        
        if (ShouldTrigger(collision.gameObject.tag))
        {
            PlayParticles();
        }
    }
    
    private bool ShouldTrigger(string tag)
    {
        if (triggerTags.Length == 0) return true;
        
        foreach (string triggerTag in triggerTags)
        {
            if (tag == triggerTag) return true;
        }
        return false;
    }
    
    public void PlayParticles()
    {
        if (particleSystem != null)
        {
            particleSystem.Play();
            
            if (autoDestroy)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }
    
    public void StopParticles()
    {
        if (particleSystem != null)
        {
            particleSystem.Stop();
        }
    }
    
    public void PauseParticles()
    {
        if (particleSystem != null)
        {
            particleSystem.Pause();
        }
    }
    
    public void SetTarget(Transform target)
    {
        targetToFollow = target;
        followTarget = true;
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    // Public method to be called from other scripts
    public void TriggerParticles()
    {
        PlayParticles();
    }
} 