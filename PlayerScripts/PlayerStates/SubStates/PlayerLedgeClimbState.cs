using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents the state of the player when climbing a ledge.
/// Handles logic related to ledge detection and climbing transitions.
/// </summary>


public class PlayerLedgeClimbState : PlayerState
{
    private Vector2 detectedPos;
    private Vector2 cornerPos;
    private Vector2 startPos;
    private Vector2 stopPos;
    private bool isHanging;
    private bool isClimbing;

    private int xInput;
    private int yInput;
    private bool jumpInput;

    private float climbStartTime;
    private float climbDuration = 0.25f; // Adjust to match your animation length

    public PlayerLedgeClimbState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }
    public override void Enter()
    {
        base.Enter();
        player.Core.Movement.SetVelocityZero();
        player.transform.position = detectedPos;
        cornerPos = player.Core.Movement.DetermineCornerPosition();
        startPos.Set(cornerPos.x - (playerData.startOffset.x * player.FacingDirection), cornerPos.y - playerData.startOffset.y);
        stopPos.Set(cornerPos.x + (playerData.stopOffset.x * player.FacingDirection), cornerPos.y + playerData.stopOffset.y);

        player.transform.position = startPos;
        // Immediately begin hanging and start the climb so touching a ledge auto-climbs
        climbStartTime = 0f;
        isHanging = true;   // treat as already in hanging pose
        isClimbing = true;  // start climbing right away

        // Trigger climb animation immediately
        // Ensure the climb animation is explicitly played to avoid animator
        // transition conflicts that can cause the clip to only show the first
        // frame. Keep the parameter true as well for any controller transitions.
        if (player.Anim != null)
        {
            try
            {
                player.Anim.Play("climbLedge", 0, 0f);
                player.Anim.speed = 1f;
                player.Anim.SetBool("climbLedge", true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to play climbLedge animation directly: {e.Message}");
                player.Anim.SetBool("climbLedge", true);
            }
        }

        // Play the ledge climb sound
        AudioManager.PlaySound(SoundType.LEDGECLIMB);
    }

    public override void Exit()
    {
        base.Exit();
        isHanging = false;
        if (isClimbing)
        {
            player.transform.position = stopPos;
            isClimbing = false;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        xInput = player.InputHandler.NormInputX;
        jumpInput = player.InputHandler.JumpInput;
        yInput = player.InputHandler.NormInputY;
        // If climbing (we start climbing immediately on Enter), interpolate position to final stop
        if (isClimbing)
        {
            climbStartTime += Time.deltaTime;
            float t = Mathf.Clamp01(climbStartTime / climbDuration);
            player.transform.position = Vector2.Lerp(startPos, stopPos, t);

            if (t >= 1f)
            {
                // Ensure animation flag is cleared in AnimationFinishTrigger as well,
                // but change to Idle when interpolation completes.
                stateMachine.ChangeState(player.IdleState);
            }
        }
        else
        {
            // Safety fallback: keep player at startPos if for some reason climbing wasn't started
            player.Core.Movement.SetVelocityZero();
            player.transform.position = startPos;
        }
    }

    public override void AnimationTrigger()
    {
        base.AnimationTrigger();
        isHanging = true;
    }
    public override void AnimationFinishTrigger()
    {
        base.AnimationFinishTrigger();
        player.Anim.SetBool("climbLedge", false);
    }
    public void SetDetectedPos(Vector2 pos)
    {
        detectedPos = pos;
    }
}