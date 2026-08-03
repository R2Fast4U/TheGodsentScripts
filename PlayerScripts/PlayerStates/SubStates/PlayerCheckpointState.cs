using UnityEngine;

/// <summary>
/// Checkpoint activation state, driving a Sit → Sitting → Stand animator sub-state machine.
/// On enter: sets "checkpoint" true (Sit → Sitting), blocks input, zooms the camera.
/// After checkpointDuration: sets "checkpoint" false (Sitting → Stand) but STAYS locked while
/// the stand-up plays (checkpointStandDuration). Only then returns control and exits.
/// </summary>
public class PlayerCheckpointState : PlayerState
{
    private bool isGrounded;
    private bool standing;
    private float standStartTime;

    public PlayerCheckpointState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isGrounded = player.Grounded;
    }

    public override void Enter()
    {
        base.Enter(); // sets "checkpoint" true → Sit → Sitting

        standing = false;
        player.Anim.SetBool("move", false);
        player.Core.Movement.SetVelocityX(0f);

        if (player.InputHandler != null)
            player.InputHandler.BlockInput();

        // Zoom in, held for the whole sit + stand window, then eases back. Values tuned on PlayerData.
        float zoomHold = playerData.checkpointDuration + playerData.checkpointStandDuration;
        CinemachineOffsetController.Instance?.PunchLens(
            -Mathf.Abs(playerData.checkpointZoomAmount), zoomHold,
            playerData.checkpointZoomRecoverySpeed, playerData.checkpointZoomInSpeed);
    }

    public override void Exit()
    {
        base.Exit(); // clears "checkpoint" (already false by now)

        if (player.InputHandler != null)
            player.InputHandler.UnblockInput();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        DoChecks();
        if (isExitingState) return;

        if (!standing)
        {
            // Sitting: hold until the duration, then trigger the stand-up.
            if (Time.time >= startTime + playerData.checkpointDuration)
            {
                standing = true;
                standStartTime = Time.time;
                player.Anim.SetBool(animBoolName, false); // "checkpoint" false → Sitting → Stand
            }
            return;
        }

        // Standing: stay locked while the stand-up plays, then return control.
        if (Time.time < standStartTime + playerData.checkpointStandDuration) return;

        if (!isGrounded)
            stateMachine.ChangeState(player.InAirState);
        else if (player.InputHandler.NormInputX != 0)
            stateMachine.ChangeState(player.MoveState);
        else
            stateMachine.ChangeState(player.IdleState);
    }
}
