using System.Data;
using UnityEngine;

public class PlayerInAirState : PlayerState
{
    //input
    public int xInput;
    private bool jumpInput;
    private bool jumpIsHeld;
    private bool warpInput;
    //checks
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isTouchingWallBack;
    private bool coyoteTime;
    private float lastWallJumpTime;
    private float wallJumpCooldown = 0.5f; // Cooldown to prevent bouncing
    private bool isTouchingLedge;
    

    public PlayerInAirState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.Grounded;
        isTouchingWall = player.TouchingWall;
        isTouchingWallBack = player.TouchingWallBack;
        isTouchingLedge = player.TouchingLedge;
        // Extend jump hold time when near walls for better wall grabbing
        if ((isTouchingWall || isTouchingWallBack) && player.InputHandler.jumpHold)
        {
            player.InputHandler.ExtendJumpHoldTime();
        }
        
        if (isTouchingWall && !isTouchingLedge)
        {
            player.LedgeClimbState.SetDetectedPos(player.transform.position);
        }
    }

    public override void Enter()
    {
        base.Enter();
        player.Anim.SetBool("inAir", true);
        // Reset wall jump override to allow immediate wall grabbing and jumping
        player.InputHandler.ResetWallJumpOverride();
        // No lockout logic here
    }

    public override void Exit()
    {
        base.Exit();
        player.Anim.SetBool("inAir", false);
    }

    public override void LogicUpdate()
    {
        DoChecks();
        base.LogicUpdate();

        CheckCoyoteTime();

    // Jump input is immediate-only now; remove buffered jump to avoid late triggers
    jumpInput = player.InputHandler.JumpInput;
        jumpIsHeld = player.InputHandler.jumpHold;
        xInput = player.InputHandler.NormInputX;
        warpInput = player.InputHandler.WarpInput;


        // Check if enough time has passed since last wall jump
        bool canWallJump = Time.time - lastWallJumpTime > wallJumpCooldown && !player.wallJumpOnCooldown;
                if (player.CanAttack && player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.primary])
        {
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.primary);
            stateMachine.ChangeState(player.PrimaryAttackState);
        }

        else if (player.CanAttack && player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.secondary])
        {
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.secondary);
            stateMachine.ChangeState(player.SecondaryAttackState);
        }

       else if (isGrounded && player.Core.Movement.CurrentVelocity.y < 0.01f)
        {
            stateMachine.ChangeState(player.LandState);
        }
        else if (isTouchingWall && !isTouchingLedge && !isGrounded)
        {
            stateMachine.ChangeState(player.LedgeClimbState);
        }
        else if (isTouchingWall && !isExitingState && Time.time >= lastWallJumpTime + playerData.wallSlideCooldown)
        {
            // Allow wall grabbing/climbing when falling towards a wall
            if (jumpIsHeld && isTouchingLedge)
            {
                stateMachine.ChangeState(player.WallSlideState);
            }
            else if (xInput == player.FacingDirection)
            {
                stateMachine.ChangeState(player.WallSlideState);
            }
        }
        // Wall jump logic (lockout handled in PlayerWallJumpState only)
        else if (jumpInput && (isTouchingWall || isTouchingWallBack) && canWallJump)
        {
            lastWallJumpTime = Time.time; // Record wall jump time
            player.InputHandler.UseJumpInput(isWallJump: true);
            player.WallJumpState.DetermineWallJumpDirection(isTouchingWall, isTouchingWallBack);
            stateMachine.ChangeState(player.WallJumpState);
        }
        // Normal jump logic (only allow buffered jump if coyote time is active)
        else if (jumpInput && player.JumpState.CanJump())
        {
            // Prevent performing a second jump while still rising (near apex exploit).
            // Allow air-jump only if the player is falling (y <= 0) or a short delay
            // has passed since entering the InAirState. This makes the mechanic feel
            // snappier and avoids accidental extra height when clicking near the top.
            float minAirJumpDelay = 0.08f; // seconds; tweak for responsiveness
            bool isFalling = player.Core.Movement.CurrentVelocity.y <= 0f;
            bool delayPassed = Time.time - startTime >= minAirJumpDelay;

            if (isFalling || delayPassed)
            {
                // Use the immediate jump input and consume it
                player.InputHandler.UseJumpInput();
                stateMachine.ChangeState(player.JumpState);
            }
            else
            {
                // Ignore the input for now; require the player to press again once
                // falling or after the short delay. We intentionally removed queued
                // buffering so inputs are edge-only and not applied later.
            }
        }
        else if (player.CanWarp && warpInput && player.WarpState.CheckIfCanWarp())
        {
            // consume the warp input so it doesn't fire again later (e.g., on landing)
            player.InputHandler.UseWarpInput();
            stateMachine.ChangeState(player.WarpState);
        }
        else
        {
            // Always allow air movement here
            player.Core.Movement.CheckIfShouldFlip(xInput);
            player.Core.Movement.SetVelocityX(player.MoveSpeed * xInput);
            player.Anim.SetFloat("yVelocity", player.Core.Movement.CurrentVelocity.y);
            player.Anim.SetFloat("xVelocity", Mathf.Abs(player.Core.Movement.CurrentVelocity.x));
        }
    }

    public override void PhysicsUpdate() => base.PhysicsUpdate();

    private void CheckCoyoteTime()
    {
        if (coyoteTime && Time.time > startTime + playerData.coyoteTime)
        {
            coyoteTime = false;
            player.JumpState.DecreaseAmountOfJumpsLeft();
        }
    }

    public void StartCoyoteTime() => coyoteTime = true;
    
    public void RecordWallJumpTime()
    {
        lastWallJumpTime = Time.time;
    }
}
