using UnityEngine;

/// <summary>
/// Entered whenever the player takes damage (driven by <see cref="Combat.OnDamaged"/>).
/// Suspends player control while the knockback impulse rides out, plays the "PlayerHurt"
/// animation, then hands control back to normal locomotion.
/// </summary>
public class PlayerHurtState : PlayerState
{
    // Minimum time the hurt animation is shown, even if there was no knockback
    // (e.g. damage from a source that doesn't push the player).
    private const float minHurtTime = 0.2f;

    private bool isGrounded;

    public PlayerHurtState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.Grounded;
    }

    public override void Enter()
    {
        base.Enter();
        // Clear locomotion bools so the animator resolves cleanly into PlayerHurt.
        player.Anim.SetBool("move", false);
        player.Anim.SetBool("inAir", false);

        // Getting knocked off the ground must not leave a usable jump stored for mid-air.
        player.JumpState.ClearJumpsLeft();
        player.InputBuffer.ConsumeBufferedJump();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        DoChecks();

        if (isExitingState) return;

        // While the knockback impulse is active, hold control and let physics carry it.
        bool knockbackActive = player.Combat != null && player.Combat.IsKnockbackActive;
        if (knockbackActive || Time.time < startTime + minHurtTime)
            return;

        // Recovered — return to normal locomotion.
        if (isGrounded)
            stateMachine.ChangeState(player.IdleState);
        else
            stateMachine.ChangeState(player.InAirState);
    }
}
