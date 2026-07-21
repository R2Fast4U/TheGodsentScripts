using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSenses : CoreComponent
{
    #region Check Transforms
    [SerializeField] private Transform groundCheck;
    public Transform GroundCheck => groundCheck;
    [SerializeField] private Transform wallCheck;
    public Transform WallCheck => wallCheck;
    [SerializeField] private Transform ledgeCheck;
    public Transform LedgeCheck => ledgeCheck;
    #endregion

    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsGround;

    #region Other Variables
    public Vector2 CurrentVelocity { get; set; }
    public int FacingDirection => core.Movement.FacingDirection;

    public bool JumpWasCut { get; private set; }

    public float wallJumpDebugTimer = 0f;
    public bool wallJumpOnCooldown = false;
    #endregion

    #region Check Properties
    public bool Grounded
    {
        get
        {
            bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
            if (grounded)
            {
                wallJumpOnCooldown = false;
                JumpWasCut = false;
            }
            return grounded;
        }
    }

    public bool TouchingWall
    {
        get
        {
            Vector2 rayDirection = Vector2.right * FacingDirection;
            Vector2 rayOrigin = wallCheck.position;
            Debug.DrawRay(rayOrigin, rayDirection * wallCheckDistance, Color.red, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, wallCheckDistance, whatIsGround);
            return hit.collider != null;
        }
    }

    public bool TouchingWallBack
    {
        get
        {
            Vector2 rayDirection = Vector2.right * -FacingDirection;
            Vector2 rayOrigin = wallCheck.position;
            Debug.DrawRay(rayOrigin, rayDirection * wallCheckDistance, Color.blue, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, wallCheckDistance, whatIsGround);
            return hit.collider != null;
        }
    }

    public bool NearWall => TouchingWall || TouchingWallBack;

    public bool NearWallForGrab
    {
        get
        {
            Vector2 rayDirection = Vector2.right * FacingDirection;
            Vector2 rayOrigin = ledgeCheck.position;
            float rayDistance = wallCheckDistance * 1.5f;
            Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.blue, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, whatIsGround);
            return hit.collider != null;
        }
    }

    public bool TouchingLedge
    {
        get
        {
            Vector2 rayDirection = Vector2.right * FacingDirection;
            Vector2 rayOrigin = ledgeCheck.position;
            float rayDistance = wallCheckDistance * 1.5f;
            Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.green, 0.1f);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, whatIsGround);
            return hit.collider != null;
        }
    }
    #endregion

    public void SetJumpWasCut(bool val) => JumpWasCut = val;
}
