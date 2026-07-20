using UnityEngine;

public class E_WalkingState : State
{
    public E_WalkingState(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        core.Movement.SetVelocityX(entity.entityData.movementSpeed * entity.facingDirection);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        var movement = core.Movement;
        if (movement != null)
            movement.SetVelocityX(entity.entityData.movementSpeed * entity.facingDirection);

        entity.CheckTouchDamage();
    }

    public override void DoChecks()
    {
        base.DoChecks();

        if (entity.CheckWall() || !entity.CheckLedge())
        {
            OnObstacleDetected();
        }
    }

    protected virtual void OnObstacleDetected()
    {
        entity.Flip();
    }
}