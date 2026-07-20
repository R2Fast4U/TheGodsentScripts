using UnityEngine;

public class C_DeadState : E_DeadState
{
    private Chomper chomper;

    public C_DeadState(Entity entity, FiniteStateMachine stateMachine, string animBoolName)
        : base(entity, stateMachine, animBoolName)
    {
        chomper = entity as Chomper;
    }

    public override void Enter()
    {
        if (chomper != null)
        {
            GameObject alive = chomper.aliveGO;

            if (chomper.deathNutParticle1 != null)
                Object.Instantiate(chomper.deathNutParticle1, alive.transform.position, chomper.deathNutParticle1.transform.rotation);
            if (chomper.deathNutParticle2 != null)
                Object.Instantiate(chomper.deathNutParticle2, alive.transform.position, chomper.deathNutParticle2.transform.rotation);
            if (chomper.deathScrewParticle != null)
                Object.Instantiate(chomper.deathScrewParticle, alive.transform.position, chomper.deathScrewParticle.transform.rotation);
            if (chomper.deathSpringParticle != null)
                Object.Instantiate(chomper.deathSpringParticle, alive.transform.position, chomper.deathSpringParticle.transform.rotation);
            if (chomper.deathTinParticle != null)
                Object.Instantiate(chomper.deathTinParticle, alive.transform.position, chomper.deathTinParticle.transform.rotation);

            Object.Destroy(alive);
        }

        base.Enter();
    }
}