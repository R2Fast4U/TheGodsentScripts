using UnityEngine;

public class C_WalkingState : E_WalkingState
{
    public C_WalkingState(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        entity.anim.Play("ChomperWalk");
    }

    protected override void OnObstacleDetected()
    {
        core.Movement.SetVelocityX(0f);
        stateMachine.ChangeState(((Chomper)entity).flipState);
    }
}