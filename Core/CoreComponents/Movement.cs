using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : CoreComponent
{
    public Rigidbody2D RB { get; private set; }
    public int FacingDirection { get; private set; } 
    public Vector2 CurrentVelocity { get; private set; }
    private Vector2 workspace;


    private Player player;
    private PlayerData playerData;

    protected override void Awake()
    {
        base.Awake();
        RB = GetComponentInParent<Rigidbody2D>();
        player = GetComponentInParent<Player>();
        if (player != null)
            playerData = player.PlayerData;
        FacingDirection = 1;
    }

    private void FixedUpdate()
    {
        CurrentVelocity = RB.velocity;
        if (player != null)
            AdjustGravityScale();
    }

    #region Physics Helpers

    private void AdjustGravityScale()
    {
        // Use the actual playerData gravity scale instead of hardcoded values

        // 1. Wall Jump Special Case
        if (player.StateMachine.CurrentState is PlayerWallJumpState)
        {
            RB.gravityScale = playerData.gravityScale * playerData.wallJumpMultiplier;
            return;
        }

        // 2. Rising (Jump) Cases
        if (RB.velocity.y > 0.01f) // Use small epsilon
        {
            // If holding jump button: lighter gravity for higher, snappy jump
            if (player.InputHandler.jumpHold)
            {
                RB.gravityScale = playerData.gravityScale * playerData.riseHoldMultiplier;
            }
            // If jump button released: heavier gravity for snappy cut/stop
            else
            {
                RB.gravityScale = playerData.gravityScale * playerData.riseCutMultiplier;
            }
        }
        // 3. Falling Cases
        else if (RB.velocity.y < -0.01f)
        {
            // If we were cutting the jump, but have now started falling, reset the flag.
            if (player.JumpWasCut) player.SetJumpWasCut(false);

            RB.gravityScale = playerData.gravityScale * playerData.fallMultiplier;
        }
        // 4. Apex / Near-Zero Vertical Velocity
        else
        {
            // If we were cutting the jump, but have now reached apex, reset the flag.
            if (player.JumpWasCut) player.SetJumpWasCut(false);

            RB.gravityScale = playerData.gravityScale * playerData.nearZeroMultiplier;
        }

        // 5. Terminal Velocity Clamp
        float maxFall = playerData.maxFallSpeed;
        if (maxFall > 0f) maxFall = -maxFall; // Ensure negative direction

        if (RB.velocity.y < maxFall)
        {
            // Clamp velocity directly
            RB.velocity = new Vector2(RB.velocity.x, maxFall);
            // Update our cache
            CurrentVelocity = RB.velocity;
        }
    }
    public Vector2 DetermineCornerPosition()
    {
        Transform wallCheck = player.WallCheck;
        Transform ledgeCheck = player.LedgeCheck;

        RaycastHit2D xHit = Physics2D.Raycast(wallCheck.position, Vector2.right * player.FacingDirection, playerData.wallCheckDistance, playerData.whatIsGround);
        float xDist = xHit.distance;
        workspace.Set(xDist * player.FacingDirection, 0f);
        RaycastHit2D yHit = Physics2D.Raycast(ledgeCheck.position + (Vector3)(workspace), Vector2.down, ledgeCheck.position.y - wallCheck.position.y, playerData.whatIsGround);
        float yDist = yHit.distance;
        workspace.Set(wallCheck.position.x + (xDist * player.FacingDirection), ledgeCheck.position.y - yDist);
        return workspace;
    }

    // Pins the player so wallCheck sits exactly 'snapDistance' from the wall. Uses the Rigidbody
    // position (setting transform.position on a dynamic body is unreliable — physics treats
    // rb.position as authoritative and depenetrates by a variable amount) and zeroes horizontal
    // velocity so the pin doesn't drift. Vertical velocity (the slide) is preserved, so this is
    // safe to call every physics step to keep the distance perfectly consistent.
    public void SnapToWall(float snapDistance)
    {
        if (player == null || RB == null) return;

        Transform wallCheck = player.WallCheck;
        Vector2 direction = Vector2.right * player.FacingDirection;
        // Check a bit further than normal to ensure we find the wall
        float checkDist = playerData.wallCheckDistance * 1.5f;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, direction, checkDist, playerData.whatIsGround);
        if (hit.collider == null) return;

        // Snap the player so wallCheck is 'snapDistance' away from the wall
        float diff = hit.distance - snapDistance;
        RB.position += direction * diff;
        RB.velocity = new Vector2(0f, RB.velocity.y);
        CurrentVelocity = RB.velocity;
    }
    #endregion

    #region Velocity Setters
    public void SetVelocityZero()
    {
        RB.velocity = Vector2.zero;
        CurrentVelocity = Vector2.zero;
    }

    public void SetVelocity(float velocity, Vector2 angle, int direction)
    {
        angle.Normalize();
        workspace.Set(angle.x * velocity * direction, angle.y * velocity);
        RB.velocity = workspace;
        CurrentVelocity = workspace;
    }

    public void SetVelocity(float velocity, Vector2 direction)
    {
        workspace = direction * velocity;
        RB.velocity = workspace;
        CurrentVelocity = workspace;
    }

    public void SetVelocityX(float velocity)
    {
        workspace.Set(velocity, RB.velocity.y); // Use RB.velocity.y to preserve physics/gravity changes
        RB.velocity = workspace;
        CurrentVelocity = workspace;
    }

    public void SetVelocityY(float velocity)
    {
        workspace.Set(CurrentVelocity.x, velocity);
        RB.velocity = workspace;
        CurrentVelocity = workspace;
    }

    // Used by states that need to set velocity directly without going through RB (e.g. warp hold)
    public void SetCurrentVelocity(Vector2 velocity)
    {
        RB.velocity = velocity;
        CurrentVelocity = velocity;
    }

        public void CheckIfShouldFlip(int xInput)
    {
        if (xInput != 0 && xInput != FacingDirection)
        {
            Flip();
        }
    }

        private void Flip()
    {
        if (player == null) return;
        FacingDirection *= -1;
        player.transform.Rotate(0.0f, 180.0f, 0.0f);
    }
    #endregion
}
