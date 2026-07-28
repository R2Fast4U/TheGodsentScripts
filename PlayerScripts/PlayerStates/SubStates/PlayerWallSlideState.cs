using UnityEngine;

public class PlayerWallSlideState : PlayerTouchingWallState
{
    private bool isTouchingWallBack;

    public PlayerWallSlideState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isTouchingWallBack = player.TouchingWallBack;
        
        // Extend jump hold time when wall sliding for better wall grabbing
        if (player.InputHandler.jumpHold)
        {
            player.InputHandler.ExtendJumpHoldTime();
        }
    }

    public override void Enter()
    {
        base.Enter();
        
        // Start the looping wall slide sound
        AudioManager.PlayWallSlide();

        // Play the wall land impact sound
        AudioManager.PlaySound(SoundType.WALLLAND);

        // Reset wall jump override to allow immediate wall grabbing
        player.InputHandler.ResetWallJumpOverride();
        player.Core.Movement.SnapToWall(0.1f); // Snap to consistent distance
        // Set sprite offset for wall stick visual
        if (player.spriteTransform != null)
        {
            float offset = playerData.wallStickOffset;
            player.spriteTransform.localPosition = new Vector3(player.FacingDirection == 1 ? -offset : offset, 0f, 0f);
        }
    }

    public override void Exit()
    {
        base.Exit();

        // Stop the wall slide sound immediately
        AudioManager.StopWallSlide();

        // Play the wall jump sound when transitioning back into the air or performing a wall jump
        if (stateMachine.CurrentState == player.InAirState || stateMachine.CurrentState == player.WallJumpState)
        {
            AudioManager.PlaySound(SoundType.WALLJUMP);
        }

        // Reset sprite offset
        if (player.spriteTransform != null)
            player.spriteTransform.localPosition = Vector3.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        bool jumpInput = player.InputHandler.JumpInput;
        bool jumpIsHeld = player.InputHandler.jumpHold;
        xInput = player.InputHandler.NormInputX;
        int yInput = player.InputHandler.NormInputY;

        // Wall jump from wall slide — respond to immediate jump input only
        if (jumpInput)
        {
            player.InputHandler.UseJumpInput(isWallJump: true);
            player.WallJumpState.DetermineWallJumpDirection(isTouchingWall, isTouchingWallBack);
            stateMachine.ChangeState(player.WallJumpState);
        }
        // Transition to wall grab (removed per user request)
        /*else if (xInput == player.FacingDirection && jumpIsHeld)
        {
            stateMachine.ChangeState(player.WallGrabState);
        }*/
        // Release when not touching wall
        else if (!isTouchingWall)
        {
            stateMachine.ChangeState(player.InAirState);
        }
        // If player presses away from the wall, release with a small push
        else if (xInput == -player.FacingDirection)
        {
            float push = playerData.wallReleasePush;
            player.Core.Movement.SetVelocityX(xInput * push);
            stateMachine.ChangeState(player.InAirState);
        }
        // Continue wall sliding
        else
        {
            // Use faster slide velocity when pressing down
            bool isFastSlide = yInput < 0;
            float slideVelocity = isFastSlide ? playerData.fastWallSlideVelocity : playerData.wallSlideVelocity;
            player.Core.Movement.SetVelocityY(-slideVelocity);
            player.Core.Movement.SetVelocityX(0f);

            // Speed up wall slide sound when fast sliding
            AudioManager.SetWallSlidePitch(isFastSlide ? 1.4f : 1.0f);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Keep the player pinned at a constant distance from the wall every physics step, so the
        // snap is consistent regardless of entry speed/overlap. Only X is pinned; the vertical
        // slide is untouched.
        if (isTouchingWall && !isExitingState)
            player.Core.Movement.SnapToWall(0.1f);
    }
}