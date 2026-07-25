using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log("Entering Move State");
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        player.Core.Movement.CheckIfShouldFlip(xInput);

        player.Core.Movement.SetVelocityX(player.MoveSpeed * xInput); // playerData.movementVelocity * active speed multipliers
        if (xInput == 0 && !isExitingState)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void Exit()
    {
        base.Exit();
//        Debug.Log("Exiting Move State");
    }
}


