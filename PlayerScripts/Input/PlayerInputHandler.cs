using System.Numerics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput playerInput;
    private Camera cam;
    public UnityEngine.Vector2 RawMovementInput { get; private set; }
    public UnityEngine.Vector2 RawWarpDirectionInput { get; private set; }
    public UnityEngine.Vector2Int WarpDirectionInput { get; private set; }
    public int NormInputX { get; private set; }
    public int NormInputY { get; private set; }

    public bool JumpInput { get; private set; }
    public bool jumpHold { get; private set; }
    public bool WallJumpOverride { get; private set; }
    public bool InputBlocked { get; private set; }

    public bool WarpInput { get; private set; }
    public bool warpHold { get; private set; }

    public bool[] AttackInputs { get; private set; }// 0: Primary Attack, 1: Secondary Attack
    public int AttackDirectionY { get; private set; }



    [SerializeField] private float inputHoldTime = 0.08f; // reduced for snappier warp responsiveness
    private float jumpInputStartTime;
    private float warpInputStartTime;


    private void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        int count = System.Enum.GetValues(typeof(CombatInputs)).Length;
        AttackInputs = new bool[count];
        cam = Camera.main;
    }
    private void Update()
    {
        // Auto-expire the WarpInput edge flag after a very small window so a held
        // warp button doesn't remain queued and trigger later when arriving in a
        // new state (e.g., landing). We still keep `warpHold` for hold/release
        // detection used by the warp ability.
        if (WarpInput && Time.unscaledTime >= warpInputStartTime + inputHoldTime)
        {
            WarpInput = false;
        }

        // Make JumpInput an edge that auto-expires after a short window so a
        // press+release while in a state that blocks jumping (e.g. LookUp)
        // does not fire later when that state exits.
        if (JumpInput && Time.unscaledTime >= jumpInputStartTime + inputHoldTime)
        {
            JumpInput = false;
        }

    }

    public void OnPrimaryAttackInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            AttackInputs[(int)CombatInputs.primary] = true;
            AttackDirectionY = NormInputY;
        }
        if (context.canceled)
        {
            AttackInputs[(int)CombatInputs.primary] = false;
        }
    }

    public void OnSecondaryAttackInput(InputAction.CallbackContext context)
    {
        // Placeholder for secondary attack input handling
        // Implement attack logic here as needed
        if (context.started)
        {
            AttackInputs[(int)CombatInputs.secondary] = true;
        }
        if (context.canceled)
        {
            AttackInputs[(int)CombatInputs.secondary] = false;
        }
    }


    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (InputBlocked)
        {
            RawMovementInput = UnityEngine.Vector2.zero;
            NormInputX = 0;
            NormInputY = 0;
            return;
        }

        UnityEngine.Vector2 input = context.ReadValue<UnityEngine.Vector2>();

        //deadzone - temporarily reduced for testing
        float deadzone = 0.25f; // Reduced from 0.15f to see if this fixes look down
        if (Mathf.Abs(input.x) < deadzone)
            input.x = 0f;
        if (Mathf.Abs(input.y) < deadzone)
            input.y = 0f;

        RawMovementInput = input;

        // For air movement and general movement, use the original logic
        // The look up/down restrictions will be handled in the specific states
        NormInputX = Mathf.Abs(RawMovementInput.x) > 0.1f ? (int)Mathf.Sign(RawMovementInput.x) : 0;
        NormInputY = Mathf.Abs(RawMovementInput.y) > 0.1f ? (int)Mathf.Sign(RawMovementInput.y) : 0;
    }



    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (InputBlocked)
        {
            return;
        }

        if (context.started)
        {
            JumpInput = true;
            jumpHold = true;
            // Use unscaled time so edge expiry is reliable while timeScale changes
            jumpInputStartTime = Time.unscaledTime;
            GetComponent<InputBuffer>().BufferJump();
        }
        else if (context.canceled)
        {
            jumpHold = false;
        }
    }

    public void UseJumpInput()
    {
        JumpInput = false; // Consume the input
    }

    public void UseJumpInput(bool isWallJump)
    {
        UseJumpInput();
        if (isWallJump)
            WallJumpOverride = true;
    }

    public void ResetJumpInput()
    {
        JumpInput = false;
        jumpHold = false;
    }

    public void ResetWallJumpOverride()
    {
        WallJumpOverride = false;
    }

    public void OnWarpInput(InputAction.CallbackContext context)
    {
        if (InputBlocked)
            return;
        if (context.started)
        {
            WarpInput = true;
            warpHold = true;
            // use unscaled time so hold detection still works while timeScale is changed
            warpInputStartTime = Time.unscaledTime;
        }
        else if (context.canceled)
        {
            // immediate release -> clear hold and also clear the edge flag so it
            // does not trigger later when arriving at a new state.
            warpHold = false;
            WarpInput = false;
        }
    }

    public void OnWarpDirectionInput(InputAction.CallbackContext context)
    {
        RawWarpDirectionInput = context.ReadValue<UnityEngine.Vector2>();
        WarpDirectionInput = Vector2Int.RoundToInt(RawWarpDirectionInput.normalized);
        /*if(playerInput.currentControlScheme == "Keyboard" && cam != null)
        {
            RawWarpDirectionInput = cam.ScreenToWorldPoint(RawWarpDirectionInput) - transform.position;
        }
        */
    }
    public void UseWarpInput()
    {
        WarpInput = false; // Consume the input
    }

    public void UseAttackInput(CombatInputs input)
    {
        if (AttackInputs != null)
            AttackInputs[(int)input] = false;
    }
    private void CheckDashInputHoldTime()
    {
        // Intentionally left empty to avoid converting holds into persistent WarpInput.
    }
    public void ExtendJumpHoldTime()
    {
        // Extend the jump hold time when near walls
        if (jumpHold)
        {
            // No-op: jumpHold is driven by actual input events (started/canceled).
            // Previously we used a timer to auto-clear jumpHold which caused
            // unintended gravity changes. Keep this method in case special
            // behavior is added later, but do not modify jumpHold here.
        }
    }

    public void BlockInput()
    {
        InputBlocked = true;
        // Clear current inputs when blocking
        RawMovementInput = UnityEngine.Vector2.zero;
        NormInputX = 0;
        NormInputY = 0;
        JumpInput = false;
        jumpHold = false;
    }

    public void UnblockInput()
    {
        InputBlocked = false;
    }

    private void CheckJumpInputHoldTime()
    {
        // Removed: do NOT auto-clear jumpHold by timer. jumpHold must reflect
        // the real button state (set on started / cleared on canceled).
        // Leaving this method empty to preserve API surface for calls elsewhere.
    }


    public enum CombatInputs
    {
        primary,
        secondary
    }
}