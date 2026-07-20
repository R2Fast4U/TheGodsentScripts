using UnityEngine;
using System.Collections.Generic;

public class ParticleEffectManager : MonoBehaviour
{
    [System.Serializable]
    public class ParticleEffect
    {
        public string effectName;
        public GameObject particlePrefab;
        public float duration = 2f;
        public bool autoDestroy = true;
        public Vector3 offset = Vector3.zero;
    }
    
    [Header("Particle Effects")]
    [SerializeField] private List<ParticleEffect> particleEffects = new List<ParticleEffect>();
    
    [Header("Default Settings")]
    [SerializeField] private bool createDustOnLanding = true;
    [SerializeField] private bool createSparklesOnJump = true;
    [SerializeField] private bool createExplosionOnEnemyDeath = true;
    
    private Dictionary<string, ParticleEffect> effectDictionary = new Dictionary<string, ParticleEffect>();
    
    private void Awake()
    {
        // Build dictionary for quick lookup
        foreach (var effect in particleEffects)
        {
            if (!string.IsNullOrEmpty(effect.effectName))
            {
                effectDictionary[effect.effectName] = effect;
            }
        }
    }
    
    public void CreateEffect(string effectName, Vector3 position, Transform parent = null)
    {
        if (effectDictionary.TryGetValue(effectName, out ParticleEffect effect))
        {
            CreateParticleEffect(effect, position, parent);
        }
        else
        {
            Debug.LogWarning($"Particle effect '{effectName}' not found!");
        }
    }
    
    public void CreateEffect(string effectName, Transform target, Vector3 offset = default)
    {
        if (effectDictionary.TryGetValue(effectName, out ParticleEffect effect))
        {
            Vector3 position = target.position + offset;
            CreateParticleEffect(effect, position, target);
        }
        else
        {
            Debug.LogWarning($"Particle effect '{effectName}' not found!");
        }
    }
    
    private void CreateParticleEffect(ParticleEffect effect, Vector3 position, Transform parent = null)
    {
        if (effect.particlePrefab == null) return;
        
        GameObject particleInstance = Instantiate(effect.particlePrefab, position, Quaternion.identity);
        
        if (parent != null)
        {
            particleInstance.transform.SetParent(parent);
        }
        
        // Add ParticleController if it doesn't exist
        ParticleController controller = particleInstance.GetComponent<ParticleController>();
        if (controller == null)
        {
            controller = particleInstance.AddComponent<ParticleController>();
        }
        
        // Configure the particle controller
        if (effect.autoDestroy)
        {
            Destroy(particleInstance, effect.duration);
        }
    }
    
    // Convenience methods for common effects
    public void CreateDustEffect(Vector3 position)
    {
        CreateEffect("Dust", position);
    }
    
    public void CreateJumpEffect(Transform player)
    {
        CreateEffect("JumpSparkles", player, Vector3.up * 0.5f);
    }
    
    public void CreateLandingEffect(Transform player)
    {
        CreateEffect("LandingDust", player, Vector3.down * 0.5f);
    }
    
    public void CreateExplosionEffect(Vector3 position)
    {
        CreateEffect("Explosion", position);
    }
    
    public void CreateTrailEffect(Transform target)
    {
        CreateEffect("Trail", target, Vector3.zero);
    }
    
    // Method to be called from Player scripts
    public void OnPlayerLanded(Transform player)
    {
        if (createDustOnLanding)
        {
            CreateLandingEffect(player);
        }
    }
    
    public void OnPlayerJumped(Transform player)
    {
        if (createSparklesOnJump)
        {
            CreateJumpEffect(player);
        }
    }
    
    public void OnEnemyDied(Vector3 position)
    {
        if (createExplosionOnEnemyDeath)
        {
            CreateExplosionEffect(position);
        }
    }
    
    // Add new effect at runtime
    public void AddEffect(string name, GameObject prefab, float duration = 2f, bool autoDestroy = true)
    {
        ParticleEffect newEffect = new ParticleEffect
        {
            effectName = name,
            particlePrefab = prefab,
            duration = duration,
            autoDestroy = autoDestroy
        };
        
        particleEffects.Add(newEffect);
        effectDictionary[name] = newEffect;
    }
} 