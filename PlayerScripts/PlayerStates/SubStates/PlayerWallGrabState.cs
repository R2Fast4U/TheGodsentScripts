using UnityEngine;

public class PlayerWallGrabState : PlayerTouchingWallState
{
    private bool isTouchingWallBack;
    private int yInput;

    public PlayerWallGrabState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isTouchingWallBack = player.TouchingWallBack;
    }

    public override void Enter()
    {
        base.Enter();
        // Reset wall jump override to allow immediate wall grabbing
        player.InputHandler.ResetWallJumpOverride();
        player.Core.Movement.SnapToWall(0.1f); // Snap to consistent distance
        // Stop vertical movement when grabbing wall
        player.Core.Movement.SetVelocityY(0f);
        player.Core.Movement.SetVelocityX(0f);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        // Combine regular jump input (immediate only)
        bool hasJumpInput = player.InputHandler.JumpInput;
        yInput = player.InputHandler.NormInputY;

        // If player presses away from the wall, release with a small push
        if (player.InputHandler.NormInputX == -player.FacingDirection)
        {
            float push = playerData.wallReleasePush;
            player.Core.Movement.SetVelocityX(player.InputHandler.NormInputX * push);
            stateMachine.ChangeState(player.InAirState);
            return;
        }

        // Wall jump from wall grab
        if (hasJumpInput)
        {
            player.InputHandler.UseJumpInput(isWallJump: true);
            player.WallJumpState.DetermineWallJumpDirection(isTouchingWall, isTouchingWallBack);
            stateMachine.ChangeState(player.WallJumpState);
        }
        // Wall climb up
        /*else if (yInput == 1)
        {
            stateMachine.ChangeState(player.WallClimbState);
        }
        */
        // Wall slide down
        else if (yInput == -1 || !player.InputHandler.jumpHold)
        {
            stateMachine.ChangeState(player.WallSlideState);
        }
        // Fall off the wall if not touching wall or moving away from wall
        else if (!isTouchingWall)
        {
            stateMachine.ChangeState(player.InAirState);
        }
        // Continue wall grabbing
        else
        {
            // Keep the player stationary on the wall
            player.Core.Movement.SetVelocityY(0f);
            player.Core.Movement.SetVelocityX(0f);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
} 