using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTouchingWallState : PlayerState
{
    protected bool isGrounded;
    protected bool isTouchingWall;
    protected bool jumpInput;
    protected bool jumpIsHeld;
    protected int xInput;

    public PlayerTouchingWallState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.Grounded;
        isTouchingWall = player.TouchingWall;
    }

    public override void Enter()
    {
        base.Enter();
        // Reset warp availability when the player first touches a wall
        // so dash/warp can be used again after contacting the wall.
        if (player?.WarpState != null)
            player.WarpState.ResetCanWarp();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // Update common input variables for child states
        xInput = player.InputHandler.NormInputX;
        jumpInput = player.InputHandler.JumpInput;
        jumpIsHeld = player.InputHandler.jumpHold;
    }

    public override void AnimationTrigger()
    {
        base.AnimationTrigger();
    }

    public override void AnimationFinishTrigger()
    {
        base.AnimationFinishTrigger();
    }
}
