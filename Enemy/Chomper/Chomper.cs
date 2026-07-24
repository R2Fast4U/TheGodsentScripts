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
    [SerializeField] private float attackCooldown = 4f;
    [SerializeField] private Vector2 attackJumpForce = new Vector2(48f, 96f);
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
    [SerializeField] private AudioClip[] attackReloadSounds;
    [SerializeField] [Range(0f, 1f)] private float attackReloadVolume = 0.7f;
    [SerializeField] private AudioClip[] attackJumpSounds;
    [SerializeField] [Range(0f, 1f)] private float attackJumpVolume = 0.7f;
    [SerializeField] private AudioClip[] attackLandSounds;
    [SerializeField] [Range(0f, 1f)] private float attackLandVolume = 0.7f;
    [SerializeField] private AudioClip[] deathSounds;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 1f;

    private SpriteRenderer spriteRenderer;
    private bool isFlipping;
    private float lastFlipTime;
    private float attackTimer;
    private float reloadStartTime;
    private bool wasAirborne;

    protected override void Start()
    {
        base.Start();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        attackTimer = attackCooldown;
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

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            SwitchState(State.AttackReload);
        }
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

    public override void Damage(float amount)
    {
        currentHealth -= amount;
        DetermineDamageDirection();
        OnDamageFeedback();

        if (currentHealth <= 0f)
        {
            SwitchState(State.Dead);
            return;
        }

        if (currentState == State.AttackReload || currentState == State.Attack)
            return;

        SwitchState(State.Knockback);
    }

    protected override void EnterAttackReloadState()
    {
        reloadStartTime = Time.time;
        rb.velocity = Vector2.zero;
        if (anim != null)
            anim.Play("ChomperAttackReload");
        if (attackReloadSounds != null && attackReloadSounds.Length > 0)
            audioSource.PlayOneShot(attackReloadSounds[Random.Range(0, attackReloadSounds.Length)], attackReloadVolume);
    }

    protected override void UpdateAttackReloadState()
    {
        if (Time.time >= reloadStartTime + 1f)
            SwitchState(State.Attack);
    }

    protected override void ExitAttackReloadState() { }

    protected override void EnterAttackState()
    {
        wasAirborne = true;
        float dir = facingDirection;
        rb.velocity = new Vector2(attackJumpForce.x * dir, attackJumpForce.y);
        if (anim != null)
            anim.Play("ChomperAttack");
        if (attackJumpSounds != null && attackJumpSounds.Length > 0)
            audioSource.PlayOneShot(attackJumpSounds[Random.Range(0, attackJumpSounds.Length)], attackJumpVolume);
    }

    protected override void UpdateAttackState()
    {
        bool grounded = Physics2D.Raycast(GroundCheckTrans.position, Vector2.down, GroundCheckDist, GroundLayer);
        if (wasAirborne && grounded && rb.velocity.y <= 0f)
        {
            if (attackLandSounds != null && attackLandSounds.Length > 0)
                audioSource.PlayOneShot(attackLandSounds[Random.Range(0, attackLandSounds.Length)], attackLandVolume);
            SwitchState(State.Walking);
        }
        if (!grounded)
            wasAirborne = true;
    }

    protected override void ExitAttackState() { }

    protected override void EnterDeadState()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        rb.simulated = false;

        float destroyDelay = 0.1f;
        if (deathSounds != null && deathSounds.Length > 0)
        {
            var clip = deathSounds[Random.Range(0, deathSounds.Length)];
            audioSource.PlayOneShot(clip, deathVolume);
            destroyDelay = clip.length;
        }
        if (deathNutParticle1 != null) Instantiate(deathNutParticle1, transform.position, deathNutParticle1.transform.rotation);
        if (deathNutParticle2 != null) Instantiate(deathNutParticle2, transform.position, deathNutParticle2.transform.rotation);
        if (deathScrewParticle != null) Instantiate(deathScrewParticle, transform.position, deathScrewParticle.transform.rotation);
        if (deathSpringParticle != null) Instantiate(deathSpringParticle, transform.position, deathSpringParticle.transform.rotation);
        if (deathTinParticle != null) Instantiate(deathTinParticle, transform.position, deathTinParticle.transform.rotation);

        Destroy(gameObject, destroyDelay);
    }
}