using System;
using UnityEngine;

/// <summary>
/// Core component that lets an entity receive damage from other objects (through
/// <see cref="IDamageable"/>) and be knocked back (through <see cref="IKnockbackable"/>).
/// Lives on the Core child of the entity and is retrieved via <see cref="Core"/>.
/// Knockback is applied through the shared <see cref="Movement"/> component so it plays
/// nicely with the rest of the physics setup.
/// </summary>
public class Combat : CoreComponent, IDamageable, IKnockbackable
{
    [Header("Health")]
    [Tooltip("Fallback health used only when no PlayerStats is available (e.g. non-player entities).")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Optional. When set, health is read/written here so it persists across scenes. " +
             "Auto-found from a PlayerInventory in parents if left empty.")]
    [SerializeField] private PlayerStats stats;

    [Header("Knockback")]
    [Tooltip("Direction/shape of the knockback impulse. X is applied along the hit direction, Y is always up.")]
    [SerializeField] private Vector2 knockbackAngle = new Vector2(1f, 0.5f);
    [SerializeField] private float knockbackStrength = 15f;
    [Tooltip("How long (seconds) the entity is considered to be in knockback after being hit.")]
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Feedback")]
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] gruntSounds;

    // Fallback health state, used only when no PlayerStats is assigned.
    private float fallbackHealth;
    private bool fallbackIsDead;

    public float CurrentHealth => stats != null ? stats.CurrentHealth : fallbackHealth;
    public float MaxHealth => stats != null ? stats.EffectiveMaxHealth : maxHealth;
    public bool IsDead => stats != null ? stats.IsDead : fallbackIsDead;

    /// <summary>True while a knockback impulse is still in effect. States can read this to
    /// suspend player control (e.g. skip movement input) until the knockback finishes.</summary>
    public bool IsKnockbackActive { get; private set; }

    /// <summary>Fired after damage is applied. Args: (amountTaken, currentHealth).</summary>
    public event Action<float, float> OnDamaged;
    /// <summary>Fired once when health reaches zero.</summary>
    public event Action OnDeath;

    private AudioSource audioSource;
    private float knockbackStartTime;

    protected override void Awake()
    {
        base.Awake();

        // Prefer the shared, cross-scene stats. Auto-find from the player's inventory if unassigned.
        if (stats == null)
        {
            var inventory = GetComponentInParent<PlayerInventory>();
            if (inventory != null) stats = inventory.Stats;
        }

        if (stats != null)
            stats.EnsureInitialized();
        else
            fallbackHealth = maxHealth;

        audioSource = GetComponentInParent<AudioSource>();
    }

    private void Update()
    {
        if (IsKnockbackActive && Time.time >= knockbackStartTime + knockbackDuration)
            IsKnockbackActive = false;
    }

    #region IDamageable
    public void Damage(float amount)
    {
        if (IsDead) return;

        if (stats != null)
            stats.ModifyHealth(-amount);
        else
            fallbackHealth -= amount;

        Debug.Log($"{transform.root.name} took {amount} damage. Health: {CurrentHealth}/{MaxHealth}");

        PlayHitFeedback();
        OnDamaged?.Invoke(amount, CurrentHealth);

        if (CurrentHealth <= 0f)
            Die();
    }
    #endregion

    #region IKnockbackable
    public void Knockback(int direction)
    {
        if (IsDead || core.Movement == null) return;

        // SetVelocity normalizes the angle internally and applies direction to the X component.
        core.Movement.SetVelocity(knockbackStrength, knockbackAngle, direction);
        IsKnockbackActive = true;
        knockbackStartTime = Time.time;
    }
    #endregion

    private void Die()
    {
        fallbackIsDead = true; // stats path derives IsDead from health directly
        IsKnockbackActive = false;
        OnDeath?.Invoke();
    }

    private void PlayHitFeedback()
    {
        if (hitParticles != null)
            Instantiate(hitParticles, transform.position, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));

        if (audioSource == null) return;

        if (hitSounds != null && hitSounds.Length > 0)
            audioSource.PlayOneShot(hitSounds[UnityEngine.Random.Range(0, hitSounds.Length)]);

        if (gruntSounds != null && gruntSounds.Length > 0)
            audioSource.PlayOneShot(gruntSounds[UnityEngine.Random.Range(0, gruntSounds.Length)]);
    }
}
