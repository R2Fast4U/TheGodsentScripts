using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    protected int xInput;
    private bool jumpInput;
    private bool isGrounded;
    private bool isTouchingLedge;
    private bool warpInput;
    // Hold-time tracking for look states: player must hold up/down for duration before transitioning
    private float lookHoldTime = 0.5f; // reduced from 1f for better responsiveness
    private float lookUpHoldStartTime = -1f; // -1 means not currently held
    private float lookDownHoldStartTime = -1f;

    public PlayerGroundedState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.Grounded;
        isTouchingLedge = player.TouchingLedge;
    }

    public override void Enter()
    {
        base.Enter();
        player.JumpState.ResetAmountOfJumpsLeft();
        player.WarpState.ResetCanWarp();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        // Track Y input raw value for hold-time detection
        xInput = player.InputHandler.NormInputX;
        jumpInput = player.InputHandler.JumpInput;
        float yInput = player.InputHandler.RawMovementInput.y; // Use raw analog value
        float lookUpThreshold = 0.5f; // Lowered from 0.7f for better controller detection
        //float lookDownThreshold = -0.5f; // Lowered from -0.7f for better controller detection
        float horizontalDeadzone = 0.4f; // Increased from 0.3f for more controller-friendly deadzone
        warpInput = player.InputHandler.WarpInput;
        
        // Track hold times for look up/down
        // If yInput is above lookUpThreshold, player is holding up
        if (yInput > lookUpThreshold)
        {
            if (lookUpHoldStartTime < 0f) // First frame of holding up
                lookUpHoldStartTime = Time.unscaledTime; // use unscaled time to be robust during slow-time
        }
        else
        {
            lookUpHoldStartTime = -1f; // Released, reset timer
        }
        
        // If yInput is below -0.3f, player is holding down
        if (yInput < -0.3f)
        {
            if (lookDownHoldStartTime < 0f) // First frame of holding down
                lookDownHoldStartTime = Time.unscaledTime; // use unscaled time
        }
        else
        {
            lookDownHoldStartTime = -1f; // Released, reset timer
        }

        if (player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.primary])
        {
            lookUpHoldStartTime = -1f;
            lookDownHoldStartTime = -1f;
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.primary);
            stateMachine.ChangeState(player.PrimaryAttackState);
        }
        
        else if (player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.secondary])
        {
            lookUpHoldStartTime = -1f;
            lookDownHoldStartTime = -1f;
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.secondary);
            stateMachine.ChangeState(player.SecondaryAttackState);
        }

        // Look up logic: Only when idle (xInput within deadzone), grounded, yInput above threshold,
        // AND player has held up for lookHoldTime seconds (no single-click trigger)
        else if (Mathf.Abs(xInput) < horizontalDeadzone && isGrounded && lookUpHoldStartTime >= 0f)
        {
            // enforce a small minimum to avoid accidental flicks when users set very low values
            float requiredHold = Mathf.Max(lookHoldTime, 0.05f);
            if (Time.unscaledTime - lookUpHoldStartTime >= requiredHold)
            {
                // Prevent self-transition execution if already in state
                if (stateMachine.CurrentState != player.LookUpState)
                {
                    Debug.Log($"PlayerGroundedState: Entering LookUpState (held for {Time.unscaledTime - lookUpHoldStartTime:F2}s)");
                    stateMachine.ChangeState(player.LookUpState);
                }
                return;
            }
        }
        // Look down logic: Use raw input, lower threshold, and require sustained hold to prevent accidental trigger
        else if (Mathf.Abs(xInput) < horizontalDeadzone && isGrounded && lookDownHoldStartTime >= 0f)
        {
            float requiredHold = Mathf.Max(lookHoldTime, 0.05f);
            if (Time.unscaledTime - lookDownHoldStartTime >= requiredHold)
            {
                // Prevent self-transition execution if already in state
                if (stateMachine.CurrentState != player.LookDownState)
                {
                    Debug.Log($"PlayerGroundedState: Entering LookDownState (held for {Time.unscaledTime - lookDownHoldStartTime:F2}s)");
                    stateMachine.ChangeState(player.LookDownState);
                }
                return;
            }
        }
        // Prevent horizontal movement when looking up/down with controller
        // This prevents walking while looking up/down
        else if (Mathf.Abs(yInput) > 0.3f && Mathf.Abs(xInput) < horizontalDeadzone)
        {
            xInput = 0; // Block horizontal movement when looking up/down
        }

        else if (jumpInput || player.InputBuffer.HasBufferedJump())
        {
            if (player.TouchingWall || player.TouchingWallBack)
            {
                player.InputHandler.UseJumpInput(true);
                player.InputBuffer.ConsumeBufferedJump();
                player.WallJumpState.DetermineWallJumpDirection(player.TouchingWall, player.TouchingWallBack);
                stateMachine.ChangeState(player.WallJumpState);
            }
            else if (player.JumpState.CanJump())
            {
                player.InputHandler.UseJumpInput();
                player.InputBuffer.ConsumeBufferedJump();
                stateMachine.ChangeState(player.JumpState);
            }
        }
        else if (!isGrounded)
        {
            player.InAirState.StartCoyoteTime();
            stateMachine.ChangeState(player.InAirState);
        }
        else if (warpInput && player.WarpState.CheckIfCanWarp())
        {
            // consume the warp input edge so it doesn't persist into unrelated states
            player.InputHandler.UseWarpInput();
            stateMachine.ChangeState(player.WarpState);
        }

    }
    public override void PhysicsUpdate() => base.PhysicsUpdate();
}
