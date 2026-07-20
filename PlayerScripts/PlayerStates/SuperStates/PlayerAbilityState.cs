using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAbilityState : PlayerState
{
    protected bool isAbilityDone;
    protected bool isGrounded;

    /// <summary>
    /// If true, undo the Animator bool that PlayerState.Enter() may have set.
    /// This lets abilities that use Triggers (e.g., warp) avoid playing a clip during hold.
    /// </summary>
    protected bool suppressAnimBoolOnEnter = false;

    public PlayerAbilityState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // If the parent (PlayerState) set an Animator bool on Enter, optionally clear it right away.
        // This keeps the global timing (startTime, etc.) but prevents unwanted animations during "hold".
        if (suppressAnimBoolOnEnter && !string.IsNullOrEmpty(animBoolName))
        {
            // Defensive: only clear if param exists and is a bool
            if (HasBool(animBoolName))
            {
                player.Anim.SetBool(animBoolName, false);
            }
        }

        isAbilityDone = false;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.Grounded;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (isAbilityDone)
        {
            if (isGrounded && player.Core.Movement.CurrentVelocity.y < 0.01f)
            {
                // If there is movement input, go directly to MoveState to avoid an "empty frame" or flicker in Idle
                if (player.InputHandler.NormInputX != 0)
                {
                    stateMachine.ChangeState(player.MoveState);
                }
                else
                {
                    stateMachine.ChangeState(player.IdleState);
                }
            }
            else
            {
                stateMachine.ChangeState(player.InAirState);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    // -------- helpers --------

    /// <summary>
    /// Checks if the Animator has a bool parameter with this name.
    /// Avoids warnings if the controller changes.
    /// </summary>
    protected bool HasBool(string paramName)
    {
        foreach (var p in player.Anim.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Optional weapon setter for ability states. Default implementation is a no-op.
    /// Concrete ability states that use weapons should override this.
    /// </summary>
    public virtual void SetWeapon(Weapon weapon)
    {
        // default: no-op
    }
}
