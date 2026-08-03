using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "newPlayerData", menuName = "Data/Player Data/Base Data")]
public class PlayerData : ScriptableObject
{
    [Header("Move State")]
    public float movementVelocity = 22f;

    // HEADER IMPORTANT FOR NEW ABILITIES

    [Header("Jump State")]
    public float jumpVelocity = 45f;
    public int amountOfJumps = 1;

    [Header("Wall Jump State")]
    public float wallJumpVelocity = 15f; // Reduced from 20f to prevent pushing too far
    public float wallJumpTime = 0.25f; // Further reduced from 0.1f for even faster response

    public UnityEngine.Vector2 wallJumpAngle = new UnityEngine.Vector2(0.8f, 1.0f); // Reduced horizontal component from 1.0f to 0.8f


    [Header("Check Variables")]
    public float groundCheckRadius = 0.2f;
    public float wallCheckDistance = 0.8f;
    public LayerMask whatIsGround;

    [Header("InAirState")]
    public float coyoteTime = 0.15f;

    [Header("Wall Slide State")]
    public float wallSlideVelocity = 5f;
    public float wallSlideCooldown = 0.2f; // Time after wall jump before sliding again
    public float fastWallSlideVelocity = 15f; // Faster slide when pressing down

    [Header("Wall Release")]
    [Tooltip("Horizontal push applied when releasing from a wall by pressing away")] 
    public float wallReleasePush = 6f;

    [Header("Wall Climb State")]
    public float wallClimbVelocity = 8f;

    [Header("Warp State")]
    public float warpCooldown = 0.5f;
    public float maxHoldTime = 1f;
    public float holdTimeScale = 0.25f;
    public float dashTime = 0.2f;
    public float warpVelocity = 35f;
    public float drag = 10f;
    public float dashEndYMultiplier = 0.2f;
    public float distanceBetweenAfterImages = 0.5f;

    [Header("Gravity Scale")]
    public float gravityScale = 4.0f;
    [Tooltip("Maximum downward velocity. Can be entered as positive (20) or negative (-20); code normalizes it to a negative clamp.")]
    public float maxFallSpeed = -20f; // Maximum fall speed to prevent infinite acceleration

    [Header("Gravity Multipliers (tweak for feel)")]
    [Tooltip("Multiplier applied to gravity while holding jump during the initial jump (lower -> snappier ascent)")]
    public float riseHoldMultiplier = 1.6f; // Increased from 0.7f to reduce height while keeping high velocity
    [Tooltip("Multiplier applied to gravity when releasing jump early to cut the jump")]
    public float riseCutMultiplier = 4.0f; // Increased from 3.5f to maintain snappy cut feel
    [Tooltip("Multiplier applied to gravity while falling (higher -> faster fall)")]
    public float fallMultiplier = 6.0f;
    [Tooltip("Multiplier applied when vertical speed is near zero to make transitions snappy")]
    public float nearZeroMultiplier = 5.5f;
    [Tooltip("Multiplier applied during wall-jump state")]
    public float wallJumpMultiplier = 0.8f;

    [Header("Attack State")]
    public float attackDirectionGracePeriod = 0.15f;

    [Header("Jump Decrease Multiplier")]
    public float jumpDecreaseValue = 10f;

    [Header("Wall Stick Offset")]
    public float wallStickOffset = 0.08f;

    [Header("Ledge Climb State")]
    public Vector2 startOffset;
    public Vector2 stopOffset;

    [Header("Checkpoint State")]
    [Tooltip("How long the player stays sitting (checkpoint bool true) before standing.")]
    public float checkpointDuration = 3f;
    [Tooltip("Time the stand-up plays before control returns. Match the PlayerStand clip length.")]
    public float checkpointStandDuration = 0.6f;
    [Tooltip("Camera zoom-in amount during a checkpoint (orthographic size / FOV units).")]
    public float checkpointZoomAmount = 1f;
    [Tooltip("How fast the camera zooms IN at the start of a checkpoint (0 = instant).")]
    public float checkpointZoomInSpeed = 4f;
    [Tooltip("How fast the camera eases back after the checkpoint zoom.")]
    public float checkpointZoomRecoverySpeed = 4f;
}
