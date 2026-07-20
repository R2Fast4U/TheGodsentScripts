using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A soft input buffer that records edge events (button pressed) as timestamps
/// in a queue. Each buffered event expires after bufferTime and is consumed
/// once by gameplay code. This avoids the old behaviour where continuous
/// input state or multiple rapid presses created inconsistent buffering.
/// </summary>
public class InputBuffer : MonoBehaviour
{
    [System.Serializable]
    public class BufferedInputConfig
    {
        public string inputName;
        public float bufferTime = 0.15f;
    }

    [Header("Input Buffering")]
    public BufferedInputConfig jumpBufferConfig = new BufferedInputConfig { inputName = "Jump", bufferTime = 0.2f };
    public BufferedInputConfig wallJumpBufferConfig = new BufferedInputConfig { inputName = "WallJump", bufferTime = 0.15f };

    // Single-slot buffered timestamp (stores last press time) — avoids queue accumulation
    private float jumpLastInputTime = -Mathf.Infinity;
    private bool jumpIsBuffered = false;

    private float wallJumpLastInputTime = -Mathf.Infinity;
    private bool wallJumpIsBuffered = false;

    private void Update()
    {
        UpdateBuffers();
    }

    private void UpdateBuffers()
    {
        float now = Time.unscaledTime; // use unscaled so buffering survives slow-time but expires reliably

        if (jumpIsBuffered && now - jumpLastInputTime > jumpBufferConfig.bufferTime)
            jumpIsBuffered = false;

        if (wallJumpIsBuffered && now - wallJumpLastInputTime > wallJumpBufferConfig.bufferTime)
            wallJumpIsBuffered = false;
    }

    // Called by input handler on input started (edge)
    public void BufferJump()
    {
        jumpLastInputTime = Time.unscaledTime;
        jumpIsBuffered = true;
    }

    public void BufferWallJump()
    {
        wallJumpLastInputTime = Time.unscaledTime;
        wallJumpIsBuffered = true;
    }

    public bool HasBufferedJump()
    {
        return jumpIsBuffered;
    }

    public bool HasBufferedWallJump()
    {
        return wallJumpIsBuffered;
    }

    // Consume one buffered event (if any)
    public void ConsumeBufferedJump()
    {
        jumpIsBuffered = false;
    }

    public void ConsumeBufferedWallJump()
    {
        wallJumpIsBuffered = false;
    }
}