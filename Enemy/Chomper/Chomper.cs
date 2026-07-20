using UnityEngine;

public class Chomper : Enemy
{
    public GameObject deathScrewParticle;
    public GameObject deathSpringParticle;
    public GameObject deathNutParticle1;
    public GameObject deathNutParticle2;
    public GameObject deathTinParticle;

    public C_FlipState flipState { get; private set; }

    public override void Awake()
    {
        base.Awake();

        var alive = transform.Find("Alive");
        if (alive != null)
            aliveGO = alive.gameObject;
    }

    protected override void CreateStates()
    {
        walkingState = new C_WalkingState(this, stateMachine, "walk");
        knockbackState = new E_KnockbackState(this, stateMachine, "knockback");
        deadState = new C_DeadState(this, stateMachine, "dead");
        flipState = new C_FlipState(this, stateMachine, "flip");
    }
}