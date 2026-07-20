public class E_DeadState : State
{
    public E_DeadState(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        entity.gameObject.SetActive(false);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        entity.Core.Movement.SetVelocityZero();
    }
}