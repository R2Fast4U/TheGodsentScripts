 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLookDownState : PlayerGroundedState
{
    private bool isLookingDown;

    private CinemachineOffsetController camController;
    private float lookDownOffset = -15f; // Reduced from -25f to be less extreme and prevent crashes

    private int yInput;
    private float lookDownThreshold = -0.5f; // Lowered for better controller detection
    private float horizontalDeadzone = 0.4f; // Increased for more controller-friendly deadzone

    public PlayerLookDownState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
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
        isLookingDown = true;
        if (camController != null)
        {
            camController.SetVerticalOffset(lookDownOffset);
            Debug.Log("LookDownState: Camera offset set successfully");
        }
        else
        {
            Debug.LogWarning("LookDownState: CameraFollow component not found");
        }
        // Explicitly play the look-down animation so it loops continuously
        // while the bool is true and player hasn't released input.
        player.Anim.Play("lookDown", 0, 0f);
    }

    public override void Exit()
    {
        base.Exit();
        isLookingDown = false;
        if (camController != null)
        {
            camController.SetVerticalOffset(0f);
            Debug.Log("LookDownState: Camera offset reset successfully");
        }
    }



    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Use normalized input to check for release (same as entry detection in grounded state)
        yInput = player.InputHandler.NormInputY;
        int xInput = player.InputHandler.NormInputX;

        // If horizontal movement begins, exit immediately
        if (Mathf.Abs(xInput) >= horizontalDeadzone)
        {
            stateMachine.ChangeState(player.IdleState);
            return;
        }

        // Exit only when yInput indicates release (>= 0), not before.
        // Do NOT use a timeout; only the player releasing the input triggers exit.
        if (yInput >= 0)
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
        isLookingDown = true;
    }

    /// <summary>
    /// Triggered when the look up animation finishes
    /// </summary>
    public override void AnimationFinishTrigger()
    {
        // Completely disabled to prevent crashes from missing animation events
        // base.AnimationFinishTrigger();
        // Intentionally do NOT clear the animator bool here so the look-down state
        // can be held indefinitely until the player releases the input.
    }

}