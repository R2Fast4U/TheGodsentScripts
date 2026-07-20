using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Enemy : Entity
{
    [SerializeField] protected GameObject hitParticles;
    [SerializeField] protected AudioClip[] hitSounds;
    [SerializeField] protected AudioClip[] gruntSounds;
    protected AudioSource audioSource;

    public override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void CreateStates()
    {
        walkingState = new E_WalkingState(this, stateMachine, "walk");
        knockbackState = new E_KnockbackState(this, stateMachine, "knockback");
        deadState = new E_DeadState(this, stateMachine, "dead");
    }

    protected override void OnDamageFeedback()
    {
        if (hitParticles != null)
            Instantiate(hitParticles, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

        if (hitSounds != null && hitSounds.Length > 0)
            audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length)]);

        if (gruntSounds != null && gruntSounds.Length > 0)
            audioSource.PlayOneShot(gruntSounds[Random.Range(0, gruntSounds.Length)]);
    }
}