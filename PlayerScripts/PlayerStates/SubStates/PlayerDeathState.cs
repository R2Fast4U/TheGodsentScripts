using UnityEngine;

/// <summary>
/// Terminal state entered when the player dies. Plays the death animation (via the "dead"
/// animator bool) and freezes control. It never transitions out — the game-over flow reloads
/// the scene, which spawns a fresh player.
/// </summary>
public class PlayerDeathState : PlayerState
{
    public PlayerDeathState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void Enter()
    {
        base.Enter(); // sets the "dead" animator bool → plays the death animation

        // Clear other locomotion bools so the animator resolves cleanly into the death clip.
        player.Anim.SetBool("move", false);
        player.Anim.SetBool("inAir", false);
        player.Anim.SetBool("hurt", false);

        player.Core.Movement.SetVelocityZero();
        if (player.InputHandler != null)
            player.InputHandler.BlockInput();
    }

    // No LogicUpdate/PhysicsUpdate overrides — the player is dead and holds here.
}
