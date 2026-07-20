using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState CurrentState {get; private set;}
    public PlayerState PreviousState {get; private set;}

    public void Initialize(PlayerState startingState)
    {
        CurrentState= startingState;
        PreviousState = null;
        CurrentState.Enter();
    }

    public void ChangeState(PlayerState newState)
    {
        PreviousState = CurrentState;
        // Clear transient edge inputs (jump/warp) on transitions to avoid them
        // being consumed by the next state unexpectedly.
        if (PreviousState != null)
        {
            Player prevPlayer = PreviousState.GetPlayer();
            if (prevPlayer != null && prevPlayer.InputHandler != null)
            {
                // Consume jump edge to avoid queued jumps firing after transitions
                // (we still want warp edges to be consumable by the next state,
                // so DO NOT clear WarpInput here).
                prevPlayer.InputHandler.UseJumpInput();
            }
        }

        // Flip Enter/Exit order so the new state's Animator parameter is set
        // before the old state's param is cleared. This avoids a single-frame
        // gap where no animation bools are active and the renderer can appear
        // empty.
        PlayerState oldState = CurrentState;
        CurrentState = newState;
        if (CurrentState != null)
            CurrentState.Enter();
        if (oldState != null)
            oldState.Exit();
    }
}
