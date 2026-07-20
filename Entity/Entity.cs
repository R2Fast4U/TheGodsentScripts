using UnityEngine;

public class Entity : MonoBehaviour, IDamageable
{
    public FiniteStateMachine stateMachine;

    public D_Entity entityData;

    public Animator anim { get; private set; }
    public AnimationToStatemachine atsm { get; private set; }
    public int lastDamageDirection { get; private set; }
    public Core Core { get; private set; }
    public GameObject aliveGO { get; protected set; }

    public E_WalkingState walkingState { get; protected set; }
    public E_KnockbackState knockbackState { get; protected set; }
    public E_DeadState deadState { get; protected set; }

    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected Transform ledgeCheck;
    [SerializeField] protected Transform playerCheck;
    [SerializeField] protected Transform groundCheck;

    [SerializeField] protected Transform touchDamageCheck;
    [SerializeField] protected float touchDamageCooldown;
    [SerializeField] protected float touchDamage;
    [SerializeField] protected float touchDamageWidth, touchDamageHeight;
    private float lastTouchDamageTime;
    private Vector2 touchDamageBotLeft, touchDamageTopRight;

    protected float currentHealth;
    protected float currentStunResistance;
    protected float lastDamageTime;

    private Vector2 velocityWorkspace;

    protected bool isStunned;
    protected bool isDead;

    public int facingDirection = 1;

    public virtual void Awake()
    {
        Core = GetComponentInChildren<Core>();
        aliveGO = gameObject;

        currentHealth = entityData.maxHealth;
        currentStunResistance = entityData.stunResistance;

        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        atsm = GetComponent<AnimationToStatemachine>();
        if (atsm != null)
            atsm.entity = this;

        stateMachine = new FiniteStateMachine();
    }

    protected virtual void Start()
    {
        CreateStates();
        if (walkingState != null)
            stateMachine.Initialize(walkingState);
    }

    protected virtual void CreateStates()
    {
    }

    public virtual void Update()
    {
        if (stateMachine.currentState != null)
            stateMachine.currentState.LogicUpdate();

        if (anim != null && Core != null && Core.Movement != null)
            anim.SetFloat("yVelocity", Core.Movement.CurrentVelocity.y);

        if (Time.time >= lastDamageTime + entityData.stunRecoveryTime)
        {
            ResetStunResistance();
        }
    }

    public virtual void FixedUpdate()
    {
        if (stateMachine.currentState != null)
            stateMachine.currentState.PhysicsUpdate();
    }

    public virtual bool CheckPlayerInMinAgroRange()
    {
        Vector2 direction = new Vector2(facingDirection, 0f);
        return Physics2D.Raycast(playerCheck.position, direction, entityData.minAgroDistance, entityData.whatIsPlayer);
    }

    public virtual bool CheckPlayerInMaxAgroRange()
    {
        Vector2 direction = new Vector2(facingDirection, 0f);
        return Physics2D.Raycast(playerCheck.position, direction, entityData.maxAgroDistance, entityData.whatIsPlayer);
    }

    public virtual bool CheckPlayerInCloseRangeAction()
    {
        Vector2 direction = new Vector2(facingDirection, 0f);
        return Physics2D.Raycast(playerCheck.position, direction, entityData.closeRangeActionDistance, entityData.whatIsPlayer);
    }

    public virtual void CheckTouchDamage()
    {
        if (touchDamageCheck == null)
            return;

        if (Time.time < lastTouchDamageTime + touchDamageCooldown)
            return;

        touchDamageBotLeft.Set(touchDamageCheck.position.x - touchDamageWidth / 2, touchDamageCheck.position.y - touchDamageHeight / 2);
        touchDamageTopRight.Set(touchDamageCheck.position.x + touchDamageWidth / 2, touchDamageCheck.position.y + touchDamageHeight / 2);

        Collider2D[] hits = Physics2D.OverlapAreaAll(touchDamageBotLeft, touchDamageTopRight, entityData.whatIsPlayer);

        if (hits == null || hits.Length == 0)
            return;

        lastTouchDamageTime = Time.time;

        foreach (Collider2D hit in hits)
        {
            hit.GetComponent<IDamageable>()?.Damage(touchDamage);
        }
    }

    public virtual void Damage(float amount)
    {
        lastDamageTime = Time.time;
        currentHealth -= amount;
        currentStunResistance -= amount;

        DetermineDamageDirection();

        if (currentStunResistance <= 0f)
            isStunned = true;

        OnDamageFeedback();

        if (currentHealth <= 0f)
        {
            isDead = true;
            stateMachine.ChangeState(deadState);
        }
        else
        {
            stateMachine.ChangeState(knockbackState);
        }
    }

    protected virtual void OnDamageFeedback()
    {
    }

    public virtual void DamageHop(float velocity)
    {
        velocityWorkspace.Set(Core.Movement.CurrentVelocity.x, velocity);
        Core.Movement.SetCurrentVelocity(velocityWorkspace);
    }

    protected void DetermineDamageDirection()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            lastDamageDirection = player.transform.position.x > transform.position.x ? -1 : 1;
    }

    public virtual void ResetStunResistance()
    {
        isStunned = false;
        currentStunResistance = entityData.stunResistance;
    }

    public virtual void Flip()
    {
        facingDirection *= -1;
        transform.Rotate(0f, 180f, 0f);
    }

    public bool CheckGround()
    {
        return Physics2D.Raycast(groundCheck.position, Vector2.down, entityData.ledgeCheckDistance, entityData.whatIsGround);
    }

    public bool CheckWall()
    {
        Vector2 direction = new Vector2(facingDirection, 0f);
        return Physics2D.Raycast(wallCheck.position, direction, entityData.wallCheckDistance, entityData.whatIsGround);
    }

    public bool CheckLedge()
    {
        return Physics2D.Raycast(ledgeCheck.position, Vector2.down, entityData.ledgeCheckDistance, entityData.whatIsGround);
    }

    public virtual void OnDrawGizmos()
    {
        if (Core == null || entityData == null)
            return;

        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + (Vector3)(Vector2.right * facingDirection * entityData.wallCheckDistance));
        }

        if (ledgeCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(ledgeCheck.position, ledgeCheck.position + (Vector3)(Vector2.down * entityData.ledgeCheckDistance));
        }

        if (playerCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCheck.position + (Vector3)(Vector2.right * facingDirection * entityData.closeRangeActionDistance), 0.2f);
            Gizmos.DrawWireSphere(playerCheck.position + (Vector3)(Vector2.right * facingDirection * entityData.minAgroDistance), 0.2f);
            Gizmos.DrawWireSphere(playerCheck.position + (Vector3)(Vector2.right * facingDirection * entityData.maxAgroDistance), 0.2f);
        }
    }
}