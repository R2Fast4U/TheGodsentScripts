 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLookUpState : PlayerGroundedState
{
    private bool isLookingUp;
    private CinemachineOffsetController camController;
    private float lookUpYOffset = 15f; // Reduced from 25f to be less extreme

    private int yInput;
    private float lookUpThreshold = 0.5f; // Lowered for better controller detection
    private float horizontalDeadzone = 0.4f; // Increased for more controller-friendly deadzone

    public PlayerLookUpState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
         camController = GameObject.Find("CameraController").GetComponent<CinemachineOffsetController>();
    }
        public override void DoChecks()
    {
        base.DoChecks();

    }
    public override void Enter()
    {
        base.Enter();
        isLookingUp = true;
        if (camController != null)
            camController.SetVerticalOffset(lookUpYOffset);
        // Explicitly play the look-up animation so it loops continuously
        // while the bool is true and player hasn't released input.
        player.Anim.Play("lookUp", 0, 0f);
    }

    public override void Exit()
    {
        base.Exit();
        isLookingUp = false;
        if (camController != null)
            camController.SetVerticalOffset(0f);
    }

public override void LogicUpdate()
{
    base.LogicUpdate();

    yInput = player.InputHandler.NormInputY;
    int xInput = player.InputHandler.NormInputX;

    // If horizontal movement begins, exit immediately
    if (Mathf.Abs(xInput) >= horizontalDeadzone)
    {
        stateMachine.ChangeState(player.IdleState);
        return;
    }

    // Exit only when yInput indicates release (<= 0), not before.
    // Do NOT use a timeout; only the player releasing the input triggers exit.
    if (yInput <= 0)
    {
        stateMachine.ChangeState(player.IdleState);
        return;
    }
}
    /// <summary>
    /// Handles the logic update for the look up state

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        // Add logic for physics updates in the look up state
    }

    /// <summary>
    /// Triggered when the look up animation starts
    /// </summary>
    public override void AnimationTrigger()
    {
        // Completely disabled to prevent crashes from missing animation events
        // base.AnimationTrigger();
        isLookingUp = true;
    }

    /// <summary>
    /// Triggered when the look up animation finishes
    /// </summary>
    public override void AnimationFinishTrigger()
    {
        // Completely disabled to prevent crashes from missing animation events
        // base.AnimationFinishTrigger();
        // Intentionally do NOT clear the animator bool here so the look state
        // can be held indefinitely until the player releases the input.
    }
        }
    