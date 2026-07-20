using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerAbilityState
{
    private Weapon weapon;
    // Runtime-created container used when applying an attachScaleMultiplier so
    // animations on the weapon's own transforms don't overwrite the visual scale.
    private GameObject attachContainer;
    private float attackTimer = 0f;
    private const float defaultAttackDuration = 0.4f;

    private int xInput;
    private float velocityToSet;
    private bool setVelocity;
    private bool shouldCheckFlip;
    private float flipGraceTimer;

    public PlayerAttackState(Player player, PlayerStateMachine stateMachine, PlayerData playerData, string animBoolName) : base(player, stateMachine, playerData, animBoolName)
    {
    }

    private static int comboCounter = 0;
    private static float lastAttackExitTime = 0f;
    private const float comboWindow = 0.8f; // Time window to trigger next attack in combo

    public override void Enter()
    {
        player.Anim.SetBool("inAir", false);
        player.Anim.SetBool("move", false);

        base.Enter();
        setVelocity = false;
        flipGraceTimer = playerData.attackDirectionGracePeriod;
        float moveSpeed = Mathf.Abs(player.Core.Movement.CurrentVelocity.x);
        velocityToSet = Mathf.Max(moveSpeed, playerData.movementVelocity);
        
        Debug.Log($"PlayerAttackState.Enter - weapon={(weapon != null ? weapon.name : "null")}");
        if (weapon != null)
        {
            // Combo Logic: Determine if we should continue combo or reset
            if (Time.time < lastAttackExitTime + comboWindow)
            {
                comboCounter++;
                if (comboCounter >= 2) comboCounter = 0; // Wrap around (assuming 2-hit combo)
            }
            else
            {
                comboCounter = 0; // Reset to first attack
            }
            
            weapon.SetAttackCounter(comboCounter);
            Debug.Log($"PlayerAttackState.Enter - Combo: {comboCounter} (Time since last: {Time.time - lastAttackExitTime:F2}s)");

            // Ensure the weapon is parented and positioned with the player before activating
            if (player != null)
            {
                Transform attach = player.spriteTransform != null ? player.spriteTransform : player.transform;
                // If the weapon is already a child of the attach transform (including an attachContainer)
                // then skip reparenting to avoid stomping on any runtime container we created.
                if (!weapon.transform.IsChildOf(attach))
                {
                    weapon.transform.SetParent(attach, false);
                    weapon.transform.localPosition = Vector3.zero;
                    weapon.transform.position = attach.position;
                }
            }
            weapon.EnterWeapon();
            
            /* REMOVED: Do not force play "WeaponAttack" manually. 
               Let the Animator Controller handle the state via SetBool("attack") and SetInteger("attackCounter").
            // Force the weapon animator to play the base attack animation
            try
            {
                var weaponAnimator = weapon.GetComponent<Animator>();
                if (weaponAnimator != null)
                {
                    weaponAnimator.Play("WeaponAttack", 0, 0f);
                    weaponAnimator.Update(0f);
                    Debug.Log($"PlayerAttackState.Enter - forced weapon animator to play WeaponAttack");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PlayerAttackState.Enter - failed to play weapon animation: {ex.Message}");
            }
            */
        }
        else
            Debug.LogWarning("PlayerAttackState.Enter - weapon is null; SetWeapon may not have been called or Inventory missing.");
        
         // Start an attack timer. Prefer the weapon's clip length if available so
        // the ability ends exactly when the animation finishes.
        float clipLen = 0.25f;
        try { clipLen = weapon != null ? weapon.GetAttackClipLength() : 0f; } catch { clipLen = 0f; }
        if (clipLen > 0f)
            attackTimer = clipLen; // Removed buffer so ability ends exactly with animation
        else
            attackTimer = defaultAttackDuration;
    }

    public override void LogicUpdate()
    {
        xInput = player.InputHandler.NormInputX;

        int oldFacing = player.FacingDirection;

        if (flipGraceTimer > 0f)
        {
            flipGraceTimer -= Time.deltaTime;
            player.Core.Movement.CheckIfShouldFlip(xInput);
        }
        else if (shouldCheckFlip)
        {
            player.Core.Movement.CheckIfShouldFlip(xInput);
        }

        // If the player flipped direction, invert their sliding momentum to match the new direction
        if (player.FacingDirection != oldFacing)
        {
            player.Core.Movement.SetVelocityX(Mathf.Abs(player.Core.Movement.CurrentVelocity.x) * player.FacingDirection);
        }

        player.Core.Movement.SetVelocityX(velocityToSet * xInput);
        
        if (player.InputHandler.JumpInput && isGrounded && player.JumpState.CanJump())
        {
            player.InputHandler.UseJumpInput();
            isAbilityDone = true;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                // Mark the ability done so the base logic will transition out
                isAbilityDone = true;
            }
        }
        
        base.LogicUpdate();
    }

    public override void Exit()
    {
        base.Exit();
        
        lastAttackExitTime = Time.time;

        if (weapon != null)
        {
            weapon.ExitWeapon();
            
            // Stop the weapon animator when exiting the attack state
            try
            {
                var weaponAnimator = weapon.GetComponent<Animator>();
                if (weaponAnimator != null)
                {
                    weaponAnimator.SetBool("isAttacking", false);
                    Debug.Log($"PlayerAttackState.Exit - stopped weapon animator");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PlayerAttackState.Exit - failed to stop weapon animation: {ex.Message}");
            }
        }
        else
            Debug.LogWarning("PlayerAttackState.Exit - weapon is null; nothing to exit.");
        
        // REMOVED: Do not destroy the attachContainer here. It is created in SetWeapon (Start)
        // and should persist for the lifetime of the configuration. Destroying it breaks the
        // parent-child relationship for subsequent attacks.
        /*
        if (attachContainer != null)
        {
            try { Object.Destroy(attachContainer); } catch { }
            attachContainer = null;
        }
        */
    }
    
    public void setWeapon(Weapon weapon)
    {
        this.weapon = weapon;
    }

    public override void SetWeapon(Weapon weapon)
    {
        // Keep existing behaviour but expose via the base API
        // If the supplied Weapon is actually a prefab (not part of the active scene),
        // instantiate a scene instance so we can parent and enable/disable it safely.
        if (weapon == null)
            return;

        Weapon sceneWeapon = weapon;
            try
            {
                if (!weapon.gameObject.scene.IsValid())
                {
                    // Instantiate without a parent first so we can preserve the prefab's original localScale.
                    var instantiated = Object.Instantiate(weapon.gameObject);
                    sceneWeapon = instantiated.GetComponent<Weapon>();
                    // Prevent the Weapon instance's Start() from auto-disabling the object
                    // since we want to configure/parent it immediately.
                    if (sceneWeapon != null)
                        sceneWeapon.preventAutoDisable = true;
                    // Ensure the instantiated prefab is active in the scene (some weapon prefabs
                    // or their Start() may otherwise deactivate themselves). Force active so we can configure it.
                    try { instantiated.SetActive(true); } catch { }
                    Debug.Log($"PlayerAttackState.SetWeapon - instantiated weapon prefab '{weapon.name}' as scene object '{instantiated.name}'");
                }
                else
                {
                    // If it's already a scene object, don't change parent here; we'll parent below while preserving scale.
                }
            }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"PlayerAttackState.SetWeapon - exception while preparing weapon instance: {ex.Message}");
            sceneWeapon = weapon;
        }


        // Assign local reference and zero its local position so it follows the sprite
        this.weapon = sceneWeapon;
        if (this.weapon != null)
            this.weapon.InitializeWeapon(this);
        if (this.weapon != null && player != null)
        {
            // Preserve the original local scale (as authored on the prefab or scene object)
            Vector3 originalLocalScale = this.weapon.transform.localScale;
            Quaternion originalLocalRot = this.weapon.transform.localRotation;

            // Prefer the dedicated sprite transform; fall back to the player's root transform
            Transform attach = player.spriteTransform != null ? player.spriteTransform : player.transform;

            // If a per-weapon multiplier is set, create a non-animated container under the attach
            // point and parent the weapon under it. This prevents the Animator from overriding
            // the visual scale because the animation will apply to the weapon transform beneath
            // the container.
            float scaleMult = 1f;
            try { scaleMult = this.weapon.attachScaleMultiplier; } catch { scaleMult = 1f; }

            if (Mathf.Approximately(scaleMult, 1f))
            {
                // Parent without modifying the world transform (use worldPositionStays=false) and then restore intended local scale/rotation
                this.weapon.transform.SetParent(attach, false);
                this.weapon.transform.localPosition = Vector3.zero;
                this.weapon.transform.localRotation = Quaternion.identity;
                this.weapon.transform.localScale = originalLocalScale;
                this.weapon.transform.position = attach.position;
            }
            else
            {
                // Create a lightweight container GameObject as the immediate child of the attach
                // point. We will scale this container to provide the visual multiplier while
                // leaving the weapon's own localScale as authored so animations behave normally.
                try
                {
                    attachContainer = new GameObject(this.weapon.name + "_AttachRoot");
                    attachContainer.transform.SetParent(attach, false);
                    attachContainer.transform.localPosition = Vector3.zero;
                    attachContainer.transform.localRotation = Quaternion.identity;
                    attachContainer.transform.localScale = Vector3.one * scaleMult;

                    // Now parent the weapon under the container and restore its authored localScale
                    this.weapon.transform.SetParent(attachContainer.transform, false);
                    this.weapon.transform.localPosition = Vector3.zero;
                    this.weapon.transform.localRotation = Quaternion.identity;
                    this.weapon.transform.localScale = originalLocalScale;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"PlayerAttackState.SetWeapon - failed to create attach container: {ex.Message}. Falling back to direct parenting.");
                    this.weapon.transform.SetParent(attach, false);
                    this.weapon.transform.localPosition = Vector3.zero;
                    this.weapon.transform.localRotation = Quaternion.identity;
                    this.weapon.transform.localScale = originalLocalScale * scaleMult;
                }
            }

            Debug.Log($"PlayerAttackState.SetWeapon - parented weapon '{this.weapon.name}' to attach '{attach.name}' (restored scale: {originalLocalScale})");

            // Adjust SpriteRenderer sorting so the weapon draws on top of the player
            try
            {
                var weaponSR = this.weapon.GetComponentInChildren<SpriteRenderer>();
                var playerSR = (player.spriteTransform != null ? player.spriteTransform : player.transform).GetComponent<SpriteRenderer>();
                if (weaponSR != null && playerSR != null)
                {
                    weaponSR.sortingLayerID = playerSR.sortingLayerID;
                    weaponSR.sortingOrder = playerSR.sortingOrder + 1; // weapon above player
                    Debug.Log($"PlayerAttackState.SetWeapon - set weapon sortingOrder to {weaponSR.sortingOrder}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"PlayerAttackState.SetWeapon - failed to set sprite sorting: {ex.Message}");
            }
        }
    }

    public void SetPlayerVelocity(float velocity)
    {
        player.Core.Movement.SetVelocityX(velocity * player.FacingDirection);

        velocityToSet = velocity;
        setVelocity = true;
    }

    public void SetFlipCheck(bool value)
    {
        shouldCheckFlip = value;
    }

    #region Animation Triggers

    public override void AnimationFinishTrigger()
    {
        base.AnimationFinishTrigger();

        isAbilityDone = true;
    }

    #endregion
}
