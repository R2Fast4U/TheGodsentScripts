using UnityEngine;
using System.Collections.Generic;

public class AmbientParticleSystem : MonoBehaviour
{
    [System.Serializable]
    public class AmbientParticle
    {
        [Header("Particle Settings")]
        public string particleName;
        public GameObject particlePrefab;
        public int maxParticlesInScene = 5;
        public float spawnRadius = 10f;
        public float minSpawnDistance = 2f;
        public float maxSpawnDistance = 8f;
        
        [Header("Spawn Behavior")]
        public bool spawnOnStart = true;
        public float spawnInterval = 3f;
        public bool respawnWhenDestroyed = true;
        public float respawnDelay = 2f;
        
        [Header("Position Settings")]
        public Vector3 spawnOffset = Vector3.zero;
        public bool spawnOnGround = true;
        public LayerMask groundLayer = 1;
        public float groundCheckDistance = 5f;
        
        [Header("Visual Settings")]
        public bool randomRotation = true;
        public bool randomScale = false;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    }
    
    [Header("Ambient Particles")]
    [SerializeField] private List<AmbientParticle> ambientParticles = new List<AmbientParticle>();
    
    [Header("Scene Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool spawnAroundPlayer = true;
    [SerializeField] private float playerFollowDistance = 15f;
    [SerializeField] private bool cullDistantParticles = true;
    [SerializeField] private float cullDistance = 20f;
    
    private Dictionary<string, List<GameObject>> activeParticles = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, float> lastSpawnTimes = new Dictionary<string, float>();
    private Dictionary<string, float> respawnTimers = new Dictionary<string, float>();
    
    private void Start()
    {
        if (playerTransform == null)
            playerTransform = FindObjectOfType<Player>()?.transform;
            
        InitializeParticleSystems();
        
        // Remove global spawnOnStart usage; initial particles are spawned per-particle in SpawnInitialParticles()
        SpawnInitialParticles();
    }
    
    private void Update()
    {
        if (spawnAroundPlayer && playerTransform != null)
        {
            UpdateParticleSpawning();
        }
        
        if (cullDistantParticles)
        {
            CullDistantParticles();
        }
    }
    
    private void InitializeParticleSystems()
    {
        foreach (var particle in ambientParticles)
        {
            if (!string.IsNullOrEmpty(particle.particleName))
            {
                activeParticles[particle.particleName] = new List<GameObject>();
                lastSpawnTimes[particle.particleName] = 0f;
                respawnTimers[particle.particleName] = 0f;
            }
        }
    }
    
    private void SpawnInitialParticles()
    {
        foreach (var particle in ambientParticles)
        {
            if (particle.spawnOnStart)
            {
                for (int i = 0; i < particle.maxParticlesInScene; i++)
                {
                    SpawnParticle(particle);
                }
            }
        }
    }
    
    private void UpdateParticleSpawning()
    {
        foreach (var particle in ambientParticles)
        {
            if (!particle.spawnOnStart) continue;
            
            string particleName = particle.particleName;
            List<GameObject> currentParticles = activeParticles[particleName];
            
            // Check if we need to spawn more particles
            if (currentParticles.Count < particle.maxParticlesInScene)
            {
                float timeSinceLastSpawn = Time.time - lastSpawnTimes[particleName];
                
                if (timeSinceLastSpawn >= particle.spawnInterval)
                {
                    SpawnParticle(particle);
                    lastSpawnTimes[particleName] = Time.time;
                }
            }
            
            // Handle respawning destroyed particles
            if (particle.respawnWhenDestroyed)
            {
                respawnTimers[particleName] += Time.deltaTime;
                
                if (respawnTimers[particleName] >= particle.respawnDelay)
                {
                    if (currentParticles.Count < particle.maxParticlesInScene)
                    {
                        SpawnParticle(particle);
                    }
                    respawnTimers[particleName] = 0f;
                }
            }
        }
    }
    
    private void SpawnParticle(AmbientParticle particle)
    {
        Debug.Log($"Trying to spawn particle: {particle.particleName}");
        if (particle.particlePrefab == null) return;
        
        Vector3 spawnPosition = GetSpawnPosition(particle);
        
        if (spawnPosition != Vector3.zero)
        {
            GameObject particleInstance = Instantiate(particle.particlePrefab, spawnPosition, GetSpawnRotation(particle));
            
            // Apply random scale if enabled
            if (particle.randomScale)
            {
                float randomScale = Random.Range(particle.scaleRange.x, particle.scaleRange.y);
                particleInstance.transform.localScale = Vector3.one * randomScale;
            }
            
            // Add to active particles list
            string particleName = particle.particleName;
            if (!activeParticles.ContainsKey(particleName))
                activeParticles[particleName] = new List<GameObject>();
                
            activeParticles[particleName].Add(particleInstance);
            
            // Add destruction callback
            StartCoroutine(MonitorParticleDestruction(particleInstance, particleName));
        }
    }
    
    private Vector3 GetSpawnPosition(AmbientParticle particle)
    {
        Vector3 basePosition = playerTransform != null ? playerTransform.position : Vector3.zero;
        
        // Get random position within spawn radius
        Vector2 randomCircle = Random.insideUnitCircle.normalized * 
            Random.Range(particle.minSpawnDistance, particle.maxSpawnDistance);
        
        Vector3 spawnPos = basePosition + new Vector3(randomCircle.x, 0, randomCircle.y) + particle.spawnOffset;
        
        // If spawning on ground, raycast down to find ground
        if (particle.spawnOnGround)
        {
            RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, particle.groundCheckDistance, particle.groundLayer);
            if (hit.collider != null)
            {
                spawnPos = (Vector3)hit.point + particle.spawnOffset;
            }
            else
            {
                // If no ground found, try a different position
                return GetSpawnPosition(particle);
            }
        }
        
        return spawnPos;
    }
    
    private Quaternion GetSpawnRotation(AmbientParticle particle)
    {
        if (particle.randomRotation)
        {
            return Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }
        return Quaternion.identity;
    }
    
    private System.Collections.IEnumerator MonitorParticleDestruction(GameObject particle, string particleName)
    {
        yield return new WaitUntil(() => particle == null);
        
        // Remove from active particles list
        if (activeParticles.ContainsKey(particleName))
        {
            activeParticles[particleName].Remove(particle);
        }
    }
    
    private void CullDistantParticles()
    {
        if (playerTransform == null) return;
        
        foreach (var kvp in activeParticles)
        {
            List<GameObject> particles = kvp.Value;
            
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                if (particles[i] == null)
                {
                    particles.RemoveAt(i);
                    continue;
                }
                
                float distance = Vector3.Distance(playerTransform.position, particles[i].transform.position);
                
                if (distance > cullDistance)
                {
                    Destroy(particles[i]);
                    particles.RemoveAt(i);
                }
            }
        }
    }
    
    // Public methods for manual control
    public void SpawnParticleAtPosition(string particleName, Vector3 position)
    {
        AmbientParticle particle = ambientParticles.Find(p => p.particleName == particleName);
        if (particle != null)
        {
            GameObject particleInstance = Instantiate(particle.particlePrefab, position, GetSpawnRotation(particle));
            
            if (!activeParticles.ContainsKey(particleName))
                activeParticles[particleName] = new List<GameObject>();
                
            activeParticles[particleName].Add(particleInstance);
        }
    }
    
    public void ClearAllParticles()
    {
        foreach (var kvp in activeParticles)
        {
            foreach (var particle in kvp.Value)
            {
                if (particle != null)
                    Destroy(particle);
            }
            kvp.Value.Clear();
        }
    }
    
    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }
    
    // Gizmos for visualization in editor
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, playerFollowDistance);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerTransform.position, cullDistance);
        }
    }
} 