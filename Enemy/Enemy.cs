using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Enemy : MonoBehaviour, IDamageable
{
    protected enum State
    {
        Walking,
        Knockback,
        Dead,
        AttackReload,
        Attack
    }

    protected State currentState;

    protected virtual Transform GroundCheckTrans => null;
    protected virtual Transform WallCheckTrans => null;
    protected virtual Transform TouchDamageCheckTrans => null;
    protected virtual LayerMask GroundLayer => default;
    protected virtual LayerMask WallLayer => default;
    protected virtual LayerMask PlayerLayer => default;

    protected virtual float GroundCheckDist => 0f;
    protected virtual float WallCheckDist => 0f;
    protected virtual float MoveSpeed => 0f;
    protected virtual float MaxHp => 1f;
    protected virtual float KnockbackDur => 0f;
    protected virtual Vector2 KnockbackSpd => Vector2.zero;
    protected virtual float TouchDmgCooldown => 0f;
    protected virtual float TouchDmg => 0f;
    protected virtual float TouchDmgW => 0f;
    protected virtual float TouchDmgH => 0f;

    protected int facingDirection;
    protected int damageDirection;

    protected bool groundDetected;
    protected bool wallDetected;

    protected Rigidbody2D rb;
    protected Animator anim;
    protected Vector2 movement;
    protected Vector2 touchDamageBotLeft;
    protected Vector2 touchDamageTopRight;

    protected float currentHealth;
    protected float knockbackStartTime;
    protected float lastTouchDamageTime;

    protected virtual GameObject HitParticlesPrefab => null;
    protected virtual AudioClip[] HitSoundClips => null;
    protected virtual AudioClip[] GruntSoundClips => null;
    protected AudioSource audioSource;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = MaxHp;
        facingDirection = 1;
        SwitchState(State.Walking);
    }

    protected virtual void Update()
    {
        switch (currentState)
        {
            case State.Walking: UpdateWalkingState(); break;
            case State.Knockback: UpdateKnockbackState(); break;
            case State.Dead: UpdateDeadState(); break;
            case State.AttackReload: UpdateAttackReloadState(); break;
            case State.Attack: UpdateAttackState(); break;
        }
    }

    protected virtual void SwitchState(State state)
    {
        switch (currentState)
        {
            case State.Walking: ExitWalkingState(); break;
            case State.Knockback: ExitKnockbackState(); break;
            case State.Dead: ExitDeadState(); break;
            case State.AttackReload: ExitAttackReloadState(); break;
            case State.Attack: ExitAttackState(); break;
        }
        switch (state)
        {
            case State.Walking: EnterWalkingState(); break;
            case State.Knockback: EnterKnockbackState(); break;
            case State.Dead: EnterDeadState(); break;
            case State.AttackReload: EnterAttackReloadState(); break;
            case State.Attack: EnterAttackState(); break;
        }
        currentState = state;
    }

    protected virtual void EnterWalkingState() { }
    protected virtual void ExitWalkingState() { }

    protected virtual void UpdateWalkingState()
    {
        groundDetected = Physics2D.Raycast(GroundCheckTrans.position, Vector2.down, GroundCheckDist, GroundLayer);
        wallDetected = Physics2D.Raycast(WallCheckTrans.position, Vector2.right, WallCheckDist, WallLayer);

        CheckTouchDamage();

        if (!groundDetected || wallDetected)
            OnObstacleDetected();
        else
            ApplyWalkingMovement();
    }

    protected virtual void ApplyWalkingMovement()
    {
        movement.Set(MoveSpeed * facingDirection, rb.velocity.y);
        rb.velocity = movement;
    }

    protected virtual void OnObstacleDetected()
    {
        Flip();
    }

    protected virtual void EnterKnockbackState()
    {
        knockbackStartTime = Time.time;
        movement.Set(KnockbackSpd.x * damageDirection, KnockbackSpd.y);
        rb.velocity = movement;
        if (anim != null) anim.SetBool("knockback", true);
    }

    protected virtual void UpdateKnockbackState()
    {
        if (Time.time >= knockbackStartTime + KnockbackDur)
            SwitchState(State.Walking);
    }

    protected virtual void ExitKnockbackState()
    {
        if (anim != null) anim.SetBool("knockback", false);
    }

    protected virtual void EnterDeadState()
    {
        Destroy(gameObject);
    }

    protected virtual void UpdateDeadState() { }
    protected virtual void ExitDeadState() { }

    protected virtual void EnterAttackReloadState() { }
    protected virtual void UpdateAttackReloadState() { }
    protected virtual void ExitAttackReloadState() { }

    protected virtual void EnterAttackState() { }
    protected virtual void UpdateAttackState() { }
    protected virtual void ExitAttackState() { }

    protected virtual void CheckTouchDamage()
    {
        var t = TouchDamageCheckTrans;
        if (t == null) return;
        if (Time.time < lastTouchDamageTime + TouchDmgCooldown) return;

        touchDamageBotLeft.Set(t.position.x - TouchDmgW / 2, t.position.y - TouchDmgH / 2);
        touchDamageTopRight.Set(t.position.x + TouchDmgW / 2, t.position.y + TouchDmgH / 2);

        Collider2D[] hits = Physics2D.OverlapAreaAll(touchDamageBotLeft, touchDamageTopRight, PlayerLayer);
        if (hits == null || hits.Length == 0) return;

        lastTouchDamageTime = Time.time;
        foreach (Collider2D hit in hits)
        {
            // The receiver (e.g. the player) may implement these on a child Core
            // component, so fall back to a children search from the hit collider.
            IDamageable damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInChildren<IDamageable>();
            damageable?.Damage(TouchDmg);

            IKnockbackable knockbackable = hit.GetComponent<IKnockbackable>() ?? hit.GetComponentInChildren<IKnockbackable>();
            // Push the target away from this enemy: +1 if it's to our right, -1 if to our left.
            int knockbackDirection = hit.transform.position.x >= transform.position.x ? 1 : -1;
            knockbackable?.Knockback(knockbackDirection);
        }
    }

    public virtual void Damage(float amount)
    {
        currentHealth -= amount;
        DetermineDamageDirection();
        OnDamageFeedback();

        if (currentHealth > 0f)
            SwitchState(State.Knockback);
        else
            SwitchState(State.Dead);
    }

    protected virtual void OnDamageFeedback()
    {
        var p = HitParticlesPrefab;
        if (p != null)
            Instantiate(p, transform.position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

        var hits = HitSoundClips;
        if (hits != null && hits.Length > 0)
            audioSource.PlayOneShot(hits[Random.Range(0, hits.Length)]);

        var grunts = GruntSoundClips;
        if (grunts != null && grunts.Length > 0)
            audioSource.PlayOneShot(grunts[Random.Range(0, grunts.Length)]);
    }

    protected void DetermineDamageDirection()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            damageDirection = player.transform.position.x > transform.position.x ? -1 : 1;
    }

    public virtual void Flip()
    {
        facingDirection *= -1;
    }

    protected virtual void OnDrawGizmos()
    {
        if (GroundCheckTrans != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(GroundCheckTrans.position, new Vector2(GroundCheckTrans.position.x, GroundCheckTrans.position.y - GroundCheckDist));
        }
        if (WallCheckTrans != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(WallCheckTrans.position, new Vector2(WallCheckTrans.position.x + WallCheckDist * facingDirection, WallCheckTrans.position.y));
        }
    }
}