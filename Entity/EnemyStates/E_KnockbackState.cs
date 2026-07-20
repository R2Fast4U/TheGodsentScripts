using UnityEngine;

public class E_KnockbackState : State
{
    private Movement movement;

    public E_KnockbackState(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
        movement = core.Movement;
    }

    public override void Enter()
    {
        base.Enter();

        movement.SetVelocityX(entity.entityData.knockbackSpeed.x * entity.lastDamageDirection);
        movement.SetVelocityY(entity.entityData.knockbackSpeed.y);
        entity.anim.SetBool("knockback", true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (Time.time >= startTime + entity.entityData.knockbackDuration)
        {
            stateMachine.ChangeState(entity.walkingState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        entity.anim.SetBool("knockback", false);
    }
}