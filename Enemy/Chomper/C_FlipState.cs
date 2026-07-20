public class C_FlipState : State
{
    public C_FlipState(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        core.Movement.SetVelocityX(0f);
        entity.anim.Play("ChomperFlip", 0, 0f);
    }

    public override void AnimationTrigger()
    {
        entity.Flip();
        stateMachine.ChangeState(entity.walkingState);
    }
}