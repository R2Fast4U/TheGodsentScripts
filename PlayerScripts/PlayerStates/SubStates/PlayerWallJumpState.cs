using UnityEngine;

public class PlayerWallJumpState : PlayerAbilityState
{
    private bool isTouchingWall;
    private bool isTouchingWallBack;
    private bool jumpInput;
    private bool jumpIsHeld;
    private int xInput;
    private int wallJumpDirection;
    private float wallJumpStartTime;
    private bool wallJumpTimeOver;
    // Removed unused hasWallJumped field

    public PlayerWallJumpState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName)
        : base(player, stateMachine, playerData, animBoolName) { }

    public override void DoChecks()
    {
        base.DoChecks();
        isTouchingWall = player.TouchingWall;
        isTouchingWallBack = player.TouchingWallBack;
    }

    public override void Enter()
    {
        base.Enter();
        jumpInput = false; // Reset input tracking
        wallJumpStartTime = Time.time;
        wallJumpTimeOver = false;
        player.wallJumpOnCooldown = true; // Set cooldown flag
        // Record wall jump time for cooldown system
        player.InAirState.RecordWallJumpTime();
        // Set wall jump velocity
        player.Core.Movement.SetVelocity(playerData.wallJumpVelocity, playerData.wallJumpAngle, wallJumpDirection);
        // Flip the player to face the jump direction
        if (wallJumpDirection != player.FacingDirection)
        {
            player.Core.Movement.CheckIfShouldFlip(wallJumpDirection);
        }
    // No buffered inputs: jump is immediate-only, nothing to clear
    }

    public override void Exit()
    {
        // Do not call base.Exit() to avoid setting animBoolName ("inAir") to false.
        isExitingState = true;
        player.InputHandler.ResetWallJumpOverride();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

    jumpInput = player.InputHandler.JumpInput;
        jumpIsHeld = player.InputHandler.jumpHold;
        xInput = player.InputHandler.NormInputX;

        // Check if wall jump time is over
        if (Time.time >= wallJumpStartTime + playerData.wallJumpTime)
        {
            wallJumpTimeOver = true;
        }

        // --- WALL JUMP LOCKOUT: Only restrict movement here, not in other states ---
        // Allow input control after a very short delay for better responsiveness
        if (Time.time >= wallJumpStartTime + 0.005f)
        {
            player.Core.Movement.CheckIfShouldFlip(xInput);
            // Allow some horizontal movement control during wall jump for better precision
            if (wallJumpTimeOver)
            {
                player.Core.Movement.SetVelocityX(playerData.movementVelocity * xInput);
            }
            else
            {
                // Allow more horizontal control during wall jump time to prevent being pushed too far
                float limitedMovement = playerData.movementVelocity * 0.7f * xInput;
                player.Core.Movement.SetVelocityX(player.Core.Movement.CurrentVelocity.x + limitedMovement * Time.deltaTime);
            }
        }

        // Priority order for state transitions:
        // 1. Land on ground
        // 2. Wall grab/slide (if touching wall and inputting towards it) - allow this earlier
        // 3. Regular jump (only after wall jump time is over and not wall jumping again)
        // 4. End ability when wall jump time is over

        // 1. Land on ground
        if (isGrounded && player.Core.Movement.CurrentVelocity.y < 0.01f)
        {
            isAbilityDone = true;
        }
        // Check for attacks (allow interruption of wall jump, respecting the CanAttack gate)
        else if (player.CanAttack && player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.primary])
        {
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.primary);
            stateMachine.ChangeState(player.PrimaryAttackState);
        }
        else if (player.CanAttack && player.InputHandler.AttackInputs[(int)PlayerInputHandler.CombatInputs.secondary])
        {
            player.InputHandler.UseAttackInput(PlayerInputHandler.CombatInputs.secondary);
            stateMachine.ChangeState(player.SecondaryAttackState);
        }
        // 2. Wall grab/slide (allow wall grabbing much earlier for better responsiveness)
        else if (player.NearWallForGrab && xInput == player.FacingDirection && Time.time >= wallJumpStartTime + playerData.wallJumpTime)
        {
            // Always transition to WallSlide (Grab removed)
            stateMachine.ChangeState(player.WallSlideState);
        }
        // 3. Regular jump (only after wall jump time is over and we haven't wall jumped again)
        else if (jumpInput && player.JumpState.CanJump() && wallJumpTimeOver)
        {
            player.InputHandler.UseJumpInput();
            stateMachine.ChangeState(player.JumpState);
        }
        // 4. End ability when wall jump time is over
        else if (wallJumpTimeOver)
        {
            isAbilityDone = true;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public void DetermineWallJumpDirection(bool isTouchingWall)
    {
        if (isTouchingWall)
        {
            wallJumpDirection = -player.FacingDirection;
        }
        else
        {
            wallJumpDirection = player.FacingDirection;
        }
    }

    public void DetermineWallJumpDirection(bool isTouchingWall, bool isTouchingWallBack)
    {
        if (isTouchingWall)
        {
            wallJumpDirection = -player.FacingDirection;
        }
        else if (isTouchingWallBack)
        {
            wallJumpDirection = player.FacingDirection;
        }
        else
        {
            wallJumpDirection = -player.FacingDirection; // Default to jumping away from facing direction
        }
    }
} 