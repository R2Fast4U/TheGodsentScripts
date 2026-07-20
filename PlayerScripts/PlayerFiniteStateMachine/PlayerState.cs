using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    protected Core core;
    protected Player player;
    protected PlayerStateMachine stateMachine;
    protected PlayerData playerData;
    public string animBoolName;
    protected float startTime;
    protected bool isAnimationFinished;
    protected bool isExitingState;

    public PlayerState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
    {
        this.player=player;
        this.stateMachine = stateMachine;
        this.playerData = playerData;
        this.animBoolName = animBoolName;
        core = player.Core;

    }

    // Expose the player reference for the state machine so transitions can
    // clear transient/edge inputs (jump/warp) to avoid carry-over between states.
    public Player GetPlayer()
    {
        return player;
    }

    public virtual void Enter()
    {
        DoChecks();
        player.Anim.SetBool(animBoolName, true);
        startTime= Time.time;
        Debug.Log (animBoolName);
        isAnimationFinished= false;
        isExitingState = false;

    }
    public virtual void Exit()
    {
         player.Anim.SetBool(animBoolName, false);
         isExitingState= true;
    }

    public virtual void LogicUpdate()
    {

    }
    public virtual void PhysicsUpdate()
    {
        DoChecks();
    }

    public virtual void DoChecks()
    {

    }

    public virtual void AnimationTrigger()
    {

    }

    public virtual void AnimationFinishTrigger()
    {
        isAnimationFinished= true;
    }
}
