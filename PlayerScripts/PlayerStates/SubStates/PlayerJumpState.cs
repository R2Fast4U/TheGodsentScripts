using UnityEngine;

public class PlayerJumpState : PlayerAbilityState
{
    public bool JumpedFromGround { get; private set; }
    private int amountOfJumpsLeft;
    private float jumpStartTime;
    private bool jumpCut;
    private float variableJumpTime = 0.35f;
    private bool jumpCutApplied; // prevent reapplying the cut multiple times

    public PlayerJumpState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName)
    {
        amountOfJumpsLeft = playerData.amountOfJumps;
    }

    public override void Enter()
    {
        base.Enter();
        JumpedFromGround = isGrounded; // isGrounded is updated in base.Enter() -> DoChecks()
        jumpStartTime = Time.time;
        isAbilityDone = false;
        jumpCut = false;
        jumpCutApplied = false;
        // Apply initial jump velocity using playerData
        player.Core.Movement.SetVelocityY(playerData.jumpVelocity);
        DecreaseAmountOfJumpsLeft();
        
    // Jump is immediate-only now; no buffered inputs to clear
    }

    public override void Exit()
    {
        // Do not call base.Exit() to avoid setting animBoolName ("inAir") to false.
        // This prevents the Animator from glitching and transitioning to Idle 
        // when handing off to PlayerInAirState.
        isExitingState = true;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        // If jump is released before window ends, set jumpCut flag
        // If jump is released within the variable window, or explicit release occurs
        // before the variable window ends, trigger the jump cut. This mirrors the
        // previous mechanics (time-window based) while ensuring the cut is applied
        // only once per jump.
        if (!player.InputHandler.jumpHold && !jumpCutApplied && Time.time - jumpStartTime < variableJumpTime)
        {
            jumpCut = true;
            // Inform the player that a jump cut happened so gravity changes can
            // be applied even if the state machine transitions immediately.
            player.SetJumpWasCut(true);
            jumpCutApplied = true;
        }

        // Check for attack inputs to allow attacking while rising (respecting the CanAttack gate)
        if (player.CanAttack && player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.primary])
        {
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.primary);
            stateMachine.ChangeState(player.PrimaryAttackState);
            return;
        }
        else if (player.CanAttack && player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.secondary])
        {
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.secondary);
            stateMachine.ChangeState(player.SecondaryAttackState);
            return;
        }

        // End jump state after window
        if (Time.time - jumpStartTime >= variableJumpTime)
        {
            isAbilityDone = true;
        }

        // --- Immediate air control during jump state ---
        int xInput = player.InputHandler.NormInputX;
        player.Core.Movement.CheckIfShouldFlip(xInput);
        player.Core.Movement.SetVelocityX(playerData.movementVelocity * xInput);

        player.Anim.SetFloat("yVelocity", player.Core.Movement.CurrentVelocity.y);
        player.Anim.SetFloat("xVelocity", Mathf.Abs(player.Core.Movement.CurrentVelocity.x));
    }

    public bool CanJump() => amountOfJumpsLeft > 0;
    public void ResetAmountOfJumpsLeft() => amountOfJumpsLeft = playerData.amountOfJumps;
    public void DecreaseAmountOfJumpsLeft() => amountOfJumpsLeft--;
    public bool ShouldCutJump() => jumpCut;
}
