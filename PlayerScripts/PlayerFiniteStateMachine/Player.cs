using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region State Variables
    public PlayerStateMachine StateMachine { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerInAirState InAirState { get; private set; }
    public PlayerLandState LandState { get; private set; }

    public PlayerWallGrabState WallGrabState { get; private set; }
    public PlayerWallSlideState WallSlideState { get; private set; }
    public PlayerWallJumpState WallJumpState { get; private set; }
    public PlayerLedgeClimbState LedgeClimbState { get; private set; }

    public PlayerLookUpState LookUpState { get; private set; }
    public PlayerLookDownState LookDownState { get; private set; }
    public PlayerWarpState WarpState { get; private set; }
    public PlayerAttackState PrimaryAttackState { get; private set; }
    public PlayerAttackState SecondaryAttackState { get; private set; }

    [SerializeField] private PlayerData playerData;
    public PlayerData PlayerData => playerData;
    #endregion

    #region Components
    public Core Core { get; private set; }
    public PlayerInputHandler InputHandler { get; private set; }
    public InputBuffer InputBuffer { get; private set; }
    public Animator Anim { get; private set; }
    public Transform WarpDirectionIndicator { get; private set; }
    public PlayerInventory Inventory { get; private set; }
    #endregion

    #region Check Transforms
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;
    public Transform WallCheck => wallCheck;
    [SerializeField] private Transform ledgeCheck;
    public Transform LedgeCheck => ledgeCheck;
    #endregion

    #region Other Variables
    public Vector2 CurrentVelocity { get;  set; }
    public int FacingDirection => Core.Movement.FacingDirection;

    // Flag set when a jump was cut (player released jump early) so gravity
    // adjustments can be applied across state transitions.
    public bool JumpWasCut { get; private set; }

    public float wallJumpDebugTimer = 0f;
    public bool wallJumpOnCooldown = false;
    #endregion
    #region Sprite Reference
    // Assign this in the Inspector to your PlayerSprite child
    public Transform spriteTransform;
    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        Core = GetComponentInChildren<Core>();
        if (Core == null)
        {
            Debug.LogError("Player requires a child object with a Core component (which itself needs a child Movement component).");
        }
        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine, playerData, "idle");
        MoveState = new PlayerMoveState(this, StateMachine, playerData, "move");
        JumpState = new PlayerJumpState(this, StateMachine, playerData, "inAir");
        LandState = new PlayerLandState(this, StateMachine, playerData, "land");
        InAirState = new PlayerInAirState(this, StateMachine, playerData, "inAir");
        // WallClimbState = new PlayerWallClimbState(this, StateMachine, playerData, "wallClimb");
        WallGrabState = new PlayerWallGrabState(this, StateMachine, playerData, "wallGrab");
        WallSlideState = new PlayerWallSlideState(this, StateMachine, playerData, "wallSlide");
        WallJumpState = new PlayerWallJumpState(this, StateMachine, playerData, "inAir");
        LedgeClimbState = new PlayerLedgeClimbState(this, StateMachine, playerData, "ledgeClimbState");
        LookUpState = new PlayerLookUpState(this, StateMachine, playerData, "lookUp");
        LookDownState = new PlayerLookDownState(this, StateMachine, playerData, "lookDown");
        WarpState = new PlayerWarpState(this, StateMachine, playerData, "warp");
        PrimaryAttackState = new PlayerAttackState(this, StateMachine, playerData, "attack");
        SecondaryAttackState = new PlayerAttackState(this, StateMachine, playerData, "attack");   
    }

    private void Start()
    {
        Anim = GetComponent<Animator>();
        InputHandler = GetComponent<PlayerInputHandler>();
        InputBuffer = GetComponent<InputBuffer>();
        WarpDirectionIndicator = transform.Find("WarpDirectionIndicator");
        Inventory = GetComponent<PlayerInventory>();
    Debug.Log($"Player.Start - Inventory={(Inventory!=null)} WeaponsCount={(Inventory!=null && Inventory.Weapons!=null?Inventory.Weapons.Length:-1)}");
        // Defensive: assign primary weapon via the PlayerAbilityState API. Do
        // bounds/null checks to avoid runtime exceptions if inventory is empty.
        int primaryIndex = (int)PlayerInputHandler.CombatInputs.primary;
        if (Inventory != null && Inventory.Weapons != null && primaryIndex >= 0 && primaryIndex < Inventory.Weapons.Length)
        {
            Weapon primaryWeapon = Inventory.Weapons[primaryIndex];
            Debug.Log($"Player.Start - primaryWeapon={(primaryWeapon!=null?primaryWeapon.name:"null")} (assetPath?)");
            if (primaryWeapon != null && PrimaryAttackState != null)
            {
                PrimaryAttackState.SetWeapon(primaryWeapon);
            }
        }
        // Assign secondary weapon the same way (defensive checks)
        int secondaryIndex = (int)PlayerInputHandler.CombatInputs.secondary;
        if (Inventory != null && Inventory.Weapons != null && secondaryIndex >= 0 && secondaryIndex < Inventory.Weapons.Length)
        {
            Weapon secondaryWeapon = Inventory.Weapons[secondaryIndex];
            Debug.Log($"Player.Start - secondaryWeapon={(secondaryWeapon!=null?secondaryWeapon.name:"null")} (assetPath?)");
            if (secondaryWeapon != null && SecondaryAttackState != null)
            {
                SecondaryAttackState.SetWeapon(secondaryWeapon);
            }
        }


        StateMachine.Initialize(IdleState);

        Core.Movement.RB.gravityScale = playerData.gravityScale;
    }

    private void Update()
    {
        if (Core.Movement.RB == null || InputHandler == null || StateMachine.CurrentState == null)
        {
            Debug.LogError("Missing required component or state in Update.");
            return;
        }

        //CurrentVelocity = Core.Movement.CurrentVelocity; // RESTORED: Necessary so LogicUpdate sees fresh physics state (gravity)
        StateMachine.CurrentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
        if (wallJumpDebugTimer > 0f)
        {
            Debug.Log($"[FixedUpdate] RB.velocity={Core.Movement.RB.velocity}");
            wallJumpDebugTimer -= Time.fixedDeltaTime;
        }

        CurrentVelocity = Core.Movement.CurrentVelocity; // Snapshot velocity once per physics step if needed by states

        if (StateMachine.CurrentState != null)
        {
            StateMachine.CurrentState.PhysicsUpdate();
        }
    }

    #endregion
    #region Check Properties
    public bool Grounded
    {
        get
        {
            bool grounded = Physics2D.OverlapCircle(groundCheck.position, playerData.groundCheckRadius, playerData.whatIsGround);
            if (grounded)
            {
                wallJumpOnCooldown = false;
                JumpWasCut = false;
            }
            return grounded;
        }
    }

    public bool Idle
    {
        get
        {
            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, playerData.groundCheckRadius, playerData.whatIsGround);
            bool isNotMoving = Mathf.Abs(InputHandler.NormInputX) < 0.1f;
            bool idle = isGrounded && isNotMoving;
            if (isGrounded)
            {
                Debug.Log($"Input: {InputHandler.NormInputX}, Grounded: {isGrounded}, Idle: {idle}");
            }
            return idle;
        }
    }

    public bool TouchingWall
    {
        get
        {
            Vector2 rayDirection = Vector2.right * FacingDirection;
            Vector2 rayOrigin = wallCheck.position;
            Debug.DrawRay(rayOrigin, rayDirection * playerData.wallCheckDistance, Color.red, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, playerData.wallCheckDistance, playerData.whatIsGround);
            return hit.collider != null;
        }
    }

    public bool TouchingWallBack
    {
        get
        {
            Vector2 rayDirection = Vector2.right * -FacingDirection;
            Vector2 rayOrigin = wallCheck.position;
            Debug.DrawRay(rayOrigin, rayDirection * playerData.wallCheckDistance, Color.blue, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, playerData.wallCheckDistance, playerData.whatIsGround);
            return hit.collider != null;
        }
    }

    public bool NearWall => TouchingWall || TouchingWallBack;

    public bool NearWallForGrab
    {
        get
        {
            Vector2 rayDirection = Vector2.right * FacingDirection;
            Vector2 rayOrigin = ledgeCheck.position;
            float rayDistance = playerData.wallCheckDistance * 1.5f;
            Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.blue, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, playerData.whatIsGround);
            return hit.collider != null;
        }
    }

    public bool TouchingLedge
    {
        get
        {
            Vector2 rayDirection = Vector2.right * FacingDirection;
            Vector2 rayOrigin = ledgeCheck.position;
            float rayDistance = playerData.wallCheckDistance * 1.5f;
            Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.green, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, playerData.whatIsGround);
            return hit.collider != null;
        }
    }
    #endregion

    public void SetJumpWasCut(bool val) => JumpWasCut = val;
    

    #region Animation Triggers

    private void AnimationTrigger()
    {
        try
        {
            if (StateMachine?.CurrentState != null)
            {
                StateMachine.CurrentState.AnimationTrigger();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AnimationTrigger failed in {StateMachine?.CurrentState?.GetType().Name}: {e.Message}");
        }
    }

    private void AnimationFinishTrigger()
    {
        try
        {
            if (StateMachine?.CurrentState != null)
            {
                StateMachine.CurrentState.AnimationFinishTrigger();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"AnimationFinishTrigger failed in {StateMachine?.CurrentState?.GetType().Name}: {e.Message}");
        }
    }

    #endregion

    #region Utility

   /* private void Flip()
    {
        FacingDirection *= -1;
        transform.Rotate(0.0f, 180.0f, 0.0f);
    }
*/
    #endregion
}
