using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class PlayerWarpState : PlayerAbilityState
{
    public bool CanWarp { get; private set; }
    private bool isHolding;

    private bool warpInputStop; // will be set by release edge (prev true -> now false)

    private UnityEngine.Vector2 warpDirection;
    private UnityEngine.Vector2 warpDirectionInput;

    private float lastDashTime;

    // track active dash phase after release/timeout
    private bool isDashing;

    // store velocity/gravity/drag during hold so player "follows" pre-dash movement
    private UnityEngine.Vector2 preHoldVelocity;
    private float previousGravityScale;
    private float previousDrag;

    // NEW: exact release edge detection without changing your mechanics
    private bool prevWarpHold;                         // <— ADDED
    private const string WarpReleaseTrigger = "warpRelease";

    // NEW: track whether the release animation finished (set via animation event)
    private bool warpReleaseAnimFinished;    // <— ADDED

    public PlayerWarpState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) 
        : base(player, stateMachine, playerData, animBoolName)
    {
    }

    public bool CheckIfCanWarp()
    {
        return CanWarp && Time.time >= lastDashTime + playerData.warpCooldown;
    }

    public override void Enter()
    {
        base.Enter();
        CanWarp = false;

        player.InputHandler.UseWarpInput();
        // Warping off the ground must not leave a usable jump stored for mid-air.
        player.JumpState.ClearJumpsLeft();
        player.InputBuffer.ConsumeBufferedJump();
        isHolding = true;
        isDashing = false;

        warpDirection = UnityEngine.Vector2.right * player.FacingDirection;

        // capture current velocity and physics so player continues that motion during hold
        preHoldVelocity = player.Core.Movement.RB.velocity;
        previousGravityScale = player.Core.Movement.RB.gravityScale;
        previousDrag = player.Core.Movement.RB.drag;

        // disable gravity/drag effects so the captured velocity is preserved while holding
        player.Core.Movement.RB.gravityScale = 0f;
        player.Core.Movement.RB.drag = 0f;

        Time.timeScale = playerData.holdTimeScale;
        startTime = Time.unscaledTime;

        if (player.WarpDirectionIndicator) player.WarpDirectionIndicator.gameObject.SetActive(true);

        // ADDED: init edge detector with current state of the hold button
        prevWarpHold = player.InputHandler.warpHold;   // <— ADDED
    }

    public override void Exit()
    {
        base.Exit();

        // Don't let an attack button held during the warp fire an attack afterward (which would
        // also inherit the warp's dash velocity and launch the player). A fresh press after the
        // warp still attacks normally.
        player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.primary);
        player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.secondary);

        // restore physics/time
        Time.timeScale = 1f;
        player.Core.Movement.RB.gravityScale = previousGravityScale;
        player.Core.Movement.RB.drag = previousDrag;

        if (player.Core.Movement.CurrentVelocity.y > 0)
        {
            player.Core.Movement.SetVelocityY(player.Core.Movement.CurrentVelocity.y * playerData.dashEndYMultiplier);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (isExitingState) return;

        // ---------- HOLD / AIM ----------
        if (isHolding)
        {
            warpDirectionInput = player.InputHandler.WarpDirectionInput;

            // ADDED: compute release edge (prev true -> now false)
            bool currentWarpHold = player.InputHandler.warpHold;           // <— ADDED
            warpInputStop = prevWarpHold && !currentWarpHold;              // <— ADDED (release edge only)

            if (warpDirectionInput != UnityEngine.Vector2.zero)
            {
                warpDirection = warpDirectionInput;
                warpDirection.Normalize();
            }

            float angle = UnityEngine.Vector2.SignedAngle(UnityEngine.Vector2.right, warpDirection);
            if (player.WarpDirectionIndicator)
                player.WarpDirectionIndicator.rotation = UnityEngine.Quaternion.Euler(0f, 0f, angle);

            // keep the player's pre-hold velocity steady while in slow-time hold
            player.Core.Movement.SetCurrentVelocity(preHoldVelocity);

            // ADDED: update previous after using it this frame
            prevWarpHold = currentWarpHold;                                // <— ADDED
        }

        // ---------- RELEASE or TIMEOUT → start the dash phase ----------
        if (warpInputStop || Time.unscaledTime >= startTime + playerData.maxHoldTime)
        {
            // Only initialize end-of-hold once
            if (isHolding)
            {
                isHolding = false;

                // restore real-time and physics so we can apply dash velocity
                Time.timeScale = 1f;
                startTime = Time.time; // use realtime for dash duration

                player.Core.Movement.RB.gravityScale = previousGravityScale;
                player.Core.Movement.CheckIfShouldFlip(Mathf.RoundToInt(warpDirection.x));
                player.Core.Movement.RB.drag = playerData.drag;

                // Fire the release animation trigger exactly when releasing
                // and clear the finished flag so we wait for the animation event (or fallback to timeout)
                warpReleaseAnimFinished = false;                     // <— ADDED
                player.Anim.SetTrigger(WarpReleaseTrigger);

                // Play the warp execution sound
                AudioManager.PlaySound(SoundType.WARP);

                isDashing = true; // begin dash phase that will end after dashTime
                if (player.WarpDirectionIndicator) player.WarpDirectionIndicator.gameObject.SetActive(false);
            }

            // While dashing, apply velocity and check for dash end
            if (isDashing)
            {
                player.Core.Movement.SetVelocity(playerData.warpVelocity, warpDirection);

                bool dashTimeout = Time.time >= startTime + playerData.dashTime;
                // Finish when animation event sets flag OR when dash timeout elapses (fallback)
                if (warpReleaseAnimFinished || dashTimeout)           // <— CHANGED
                {
                    player.Core.Movement.RB.drag = 0f;
                    isAbilityDone = true;
                    lastDashTime = Time.time;
                    isDashing = false;

                    // safety: reset trigger so it doesn't persist between uses
                    player.Anim.ResetTrigger(WarpReleaseTrigger);    // <— ADDED
                    warpReleaseAnimFinished = false;                 // <— ADDED (clear state)
                }
            }
        }
        // else: still holding → do nothing (we already preserve velocity)
    }

    // Public method to be called from an Animation Event at the end of the warp-release animation
    public void OnWarpReleaseAnimationComplete()                      // <— ADDED
    {
        warpReleaseAnimFinished = true;
    }

    public void ResetCanWarp()
    {
        CanWarp = true;
    }
}
