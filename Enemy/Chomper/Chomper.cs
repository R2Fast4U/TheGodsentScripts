using UnityEngine;

public class Chomper : Enemy
{
    [Header("Checks")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform touchDamageCheck;
    protected override Transform GroundCheckTrans => groundCheck;
    protected override Transform WallCheckTrans => wallCheck;
    protected override Transform TouchDamageCheckTrans => touchDamageCheck;

    [Header("Layers")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private LayerMask whatIsPlayer;
    protected override LayerMask GroundLayer => whatIsGround;
    protected override LayerMask WallLayer => whatIsWall;
    protected override LayerMask PlayerLayer => whatIsPlayer;

    [Header("Stats")]
    [SerializeField] private float groundCheckDistance = 0.4f;
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private Vector2 knockbackSpeed = new Vector2(5f, 3f);
    protected override float GroundCheckDist => groundCheckDistance;
    protected override float WallCheckDist => wallCheckDistance;
    protected override float MoveSpeed => movementSpeed;
    protected override float MaxHp => maxHealth;
    protected override float KnockbackDur => knockbackDuration;
    protected override Vector2 KnockbackSpd => knockbackSpeed;

    [Header("Touch Damage")]
    [SerializeField] private float touchDamageCooldown = 0.5f;
    [SerializeField] private float touchDamage = 10f;
    [SerializeField] private float touchDamageWidth = 1f;
    [SerializeField] private float touchDamageHeight = 1f;
    protected override float TouchDmgCooldown => touchDamageCooldown;
    protected override float TouchDmg => touchDamage;
    protected override float TouchDmgW => touchDamageWidth;
    protected override float TouchDmgH => touchDamageHeight;

    [Header("Hit Feedback")]
    [SerializeField] private GameObject hitParticles;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] gruntSounds;
    protected override GameObject HitParticlesPrefab => hitParticles;
    protected override AudioClip[] HitSoundClips => hitSounds;
    protected override AudioClip[] GruntSoundClips => gruntSounds;

    [Header("Death Particles")]
    [SerializeField] private GameObject deathScrewParticle;
    [SerializeField] private GameObject deathSpringParticle;
    [SerializeField] private GameObject deathNutParticle1;
    [SerializeField] private GameObject deathNutParticle2;
    [SerializeField] private GameObject deathTinParticle;

    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.5f;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 1f;

    private SpriteRenderer spriteRenderer;
    private bool isFlipping;
    private float lastFlipTime;

    protected override void Start()
    {
        base.Start();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public override void Flip()
    {
        base.Flip();
        if (spriteRenderer != null)
            spriteRenderer.flipX = !spriteRenderer.flipX;
    }

    protected override void EnterWalkingState()
    {
        if (anim != null)
            anim.Play("ChomperWalk");
    }

    protected override void UpdateWalkingState()
    {
        if (isFlipping) return;

        if (Time.time < lastFlipTime + 0.3f)
        {
            ApplyWalkingMovement();
            return;
        }

        base.UpdateWalkingState();
    }

    protected override void OnObstacleDetected()
    {
        isFlipping = true;
        rb.velocity = new Vector2(0f, rb.velocity.y);
        if (anim != null)
            anim.Play("ChomperFlip", 0, 0f);
    }

    private void FlipAnimationEvent()
    {
        Flip();
        isFlipping = false;
        lastFlipTime = Time.time;
        SwitchState(State.Walking);
    }

    private void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
            audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)], footstepVolume);
    }

    protected override void EnterDeadState()
    {
        if (deathSounds != null && deathSounds.Length > 0)
            AudioSource.PlayClipAtPoint(deathSounds[Random.Range(0, deathSounds.Length)], transform.position, deathVolume);
        if (deathNutParticle1 != null) Instantiate(deathNutParticle1, transform.position, deathNutParticle1.transform.rotation);
        if (deathNutParticle2 != null) Instantiate(deathNutParticle2, transform.position, deathNutParticle2.transform.rotation);
        if (deathScrewParticle != null) Instantiate(deathScrewParticle, transform.position, deathScrewParticle.transform.rotation);
        if (deathSpringParticle != null) Instantiate(deathSpringParticle, transform.position, deathSpringParticle.transform.rotation);
        if (deathTinParticle != null) Instantiate(deathTinParticle, transform.position, deathTinParticle.transform.rotation);

        Destroy(gameObject);
    }
}