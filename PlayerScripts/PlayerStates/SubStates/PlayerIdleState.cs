using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }
    public override void DoChecks()
    {
        base.DoChecks();
    }

    public override void Enter()
    {
        base.Enter();
        player.Core.Movement.SetVelocityX(0f);
        player.Anim.SetBool("idle", true);
    }

    public override void Exit()
    {
        base.Exit();
        player.Anim.SetBool("idle", false);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // (Buffered jump check removed)

        if(xInput != 0 && !isExitingState)
        {
            stateMachine.ChangeState(player.MoveState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
