using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] protected SO_WeaponData weaponData;

    protected Animator baseAnimator;
    protected Animator weaponAnimator;
    

    protected PlayerAttackState state;

    public bool preventAutoDisable = false;
    public float attachScaleMultiplier = 1f;
    private Rigidbody2D weaponRb;
    private RigidbodyType2D? originalBodyType;

    protected int attackCounter;

    protected virtual void Awake()
    {
        var baseTransform = transform.Find("Base");
        if (baseTransform != null)
            baseAnimator = baseTransform.GetComponent<Animator>();
        else
            baseAnimator = GetComponent<Animator>();

        var weaponTransform = transform.Find("Weapon");
        if (weaponTransform != null)
            weaponAnimator = weaponTransform.GetComponent<Animator>();

        gameObject.SetActive(false);
    }

    protected virtual void Start()
    {
        var baseTransform = transform.Find("Base");
        if (baseTransform != null && baseAnimator == null)
            baseAnimator = baseTransform.GetComponent<Animator>();
        else if (baseAnimator == null)
            baseAnimator = GetComponent<Animator>();

        var weaponTransform = transform.Find("Weapon");
        if (weaponTransform != null && weaponAnimator == null)
            weaponAnimator = weaponTransform.GetComponent<Animator>();

        if (!preventAutoDisable)
            gameObject.SetActive(false);
    }

    public virtual void EnterWeapon()
    {
        gameObject.SetActive(true);
        Debug.Log($"Weapon '{name}' Transform parent={(transform.parent!=null?transform.parent.name:"(null)")}, localPos={transform.localPosition}, worldPos={transform.position}");

        Debug.Log($"Weapon.EnterWeapon() called on '{name}'. ActiveSelf={gameObject.activeSelf}");
        if (baseAnimator == null)
        {
            var baseTransform = transform.Find("Base");
            if (baseTransform != null)
                baseAnimator = baseTransform.GetComponent<Animator>();
            else
                baseAnimator = GetComponentInChildren<Animator>();
        }

        if (weaponAnimator == null)
        {
            var weaponTransform = transform.Find("Weapon");
            if (weaponTransform != null)
                weaponAnimator = weaponTransform.GetComponent<Animator>();
        }

        Debug.Log($"Weapon '{name}' animator status - baseAnimator={(baseAnimator!=null)}, weaponAnimator={(weaponAnimator!=null)}");

        if (attackCounter >= 2)
        {
            attackCounter = 0;
        }

        bool isAttackUp = false;
        bool isAttackDown = false;
        if (state != null)
        {
            Player p = state.GetPlayer();
            if (p != null)
            {
                float yInput = p.InputHandler.RawMovementInput.y;
                Debug.Log($"Weapon '{name}' attack direction: RawMovementInput.y={yInput}");
                isAttackUp = yInput > 0.3f;
                isAttackDown = yInput < -0.3f;
            }
        }
        
        if (baseAnimator != null)
        {
            var controller = baseAnimator.runtimeAnimatorController;
            if (controller == null)
            {
                Debug.LogWarning($"Weapon '{name}': Base Animator present but runtimeAnimatorController is NULL. Skipping SetBool. Check Animator component assignment in the prefab.");
            }
            else
            {
                var clips = controller.animationClips;
                Debug.Log($"Weapon '{name}': Controller='{controller.name}', clips={clips.Length}");
                if (clips.Length > 0)
                {
                    string clipList = string.Join(", ", System.Array.ConvertAll(clips, c => c != null ? c.name : "(null)"));
                    Debug.Log($"Weapon '{name}' controller clips: {clipList}");
                }

                try
                {
                    var parameters = baseAnimator.parameters;
                    if (parameters != null && parameters.Length > 0)
                    {
                        string paramList = string.Join(", ", System.Array.ConvertAll(parameters, p => p.name + "(" + p.type + ")"));
                        Debug.Log($"Weapon '{name}' animator parameters: {paramList}");
                    }
                    else
                    {
                        Debug.Log($"Weapon '{name}' animator parameters: none");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Weapon '{name}' could not enumerate animator parameters: {ex.Message}");
                }

                bool animatorEnabled = baseAnimator.enabled;
                bool activeInHierarchy = baseAnimator.gameObject.activeInHierarchy;
                Debug.Log($"Weapon '{name}' baseAnimator.enabled={animatorEnabled}, baseAnimator.activeInHierarchy={activeInHierarchy}, controllerType={controller.GetType().Name}, controllerID={controller.GetInstanceID()}");

                if (!animatorEnabled)
                {
                    Debug.LogWarning($"Weapon '{name}': Base Animator exists but is not enabled. Skipping SetBool.");
                }
                else if (!activeInHierarchy)
                {
                    Debug.Log($"Weapon '{name}': Base Animator GameObject inactive; activating and initializing.");
                    baseAnimator.gameObject.SetActive(true);
                    try
                    {
                        baseAnimator.Rebind();
                        baseAnimator.Update(0f);
                        baseAnimator.SetBool("attackUp", isAttackUp);
                        baseAnimator.SetBool("attackDown", isAttackDown);
                        if (!isAttackUp && !isAttackDown)
                            baseAnimator.SetBool("attack", true);
                        baseAnimator.SetInteger("attackCounter", attackCounter);
                        Debug.Log($"Weapon '{name}': Base Animator activated and attack bool set.");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Weapon '{name}': failed to initialize/drive Base Animator after activation: {ex.Message}");
                    }
                }
                else
                {
                    baseAnimator.Rebind();
                    baseAnimator.SetBool("attackUp", isAttackUp);
                    baseAnimator.SetBool("attackDown", isAttackDown);
                    if (!isAttackUp && !isAttackDown)
                        baseAnimator.SetBool("attack", true);
                    baseAnimator.SetInteger("attackCounter", attackCounter);
                    baseAnimator.Update(0f);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Weapon '{name}' has no Base animator. Skipping attack bool.");
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool("attack", true);
            weaponAnimator.SetInteger("attackCounter", attackCounter);
        }

        Debug.Log($"Weapon '{name}' Enter complete: rootActive={gameObject.activeSelf}, baseActive={(baseAnimator!=null?baseAnimator.gameObject.activeInHierarchy:false)}, rootParent={(transform.parent!=null?transform.parent.name:"(null)")}");

        try
        {
            weaponRb = GetComponentInChildren<Rigidbody2D>();
            if (weaponRb != null)
            {
                originalBodyType = weaponRb.bodyType;
                weaponRb.bodyType = RigidbodyType2D.Kinematic;
                Debug.Log($"Weapon '{name}': set Rigidbody2D to Kinematic for attachment (was {originalBodyType}).");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Weapon '{name}': failed to adjust Rigidbody2D: {ex.Message}");
        }
    }

    public virtual void ExitWeapon()
    {
        Debug.Log($"Weapon.ExitWeapon() called on '{name}'. rootActive={gameObject.activeSelf}, baseAnimator={(baseAnimator!=null)}, weaponAnimator={(weaponAnimator!=null)}");
        if (baseAnimator != null)
        {
            Debug.Log($"Weapon '{name}' baseAnimator.gameObject.activeInHierarchy={baseAnimator.gameObject.activeInHierarchy}");
            try { baseAnimator.SetBool("attack", false); } catch (System.Exception ex) { Debug.LogWarning($"Weapon '{name}' failed to clear base attack bool: {ex.Message}"); }
            try { baseAnimator.SetBool("attackUp", false); } catch (System.Exception ex) { Debug.LogWarning($"Weapon '{name}' failed to clear attackUp: {ex.Message}"); }
            try { baseAnimator.SetBool("attackDown", false); } catch (System.Exception ex) { Debug.LogWarning($"Weapon '{name}' failed to clear attackDown: {ex.Message}"); }
        }
        if (weaponAnimator != null)
        {
            try { weaponAnimator.SetBool("attack", false); } catch (System.Exception ex) { Debug.LogWarning($"Weapon '{name}' failed to clear weapon attack bool: {ex.Message}"); }
        }

        Debug.Log($"Weapon '{name}' Transform before deactivate: parent={(transform.parent!=null?transform.parent.name:"(null)" )}, localPos={transform.localPosition}, worldPos={transform.position}");

        try
        {
            if (weaponRb != null && originalBodyType.HasValue)
            {
                weaponRb.bodyType = originalBodyType.Value;
                Debug.Log($"Weapon '{name}': restored Rigidbody2D bodyType to {originalBodyType.Value}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Weapon '{name}': failed to restore Rigidbody2D: {ex.Message}");
        }

        gameObject.SetActive(false);
    }

    public void SetAttackCounter(int counter)
    {
        attackCounter = counter;
    }

    #region Animation Triggers

    public virtual void AnimationFinishTrigger()
    {
        if (state != null)
            state.AnimationFinishTrigger();
    }

    public virtual void AnimationStartMovementTrigger()
    {
        if (state != null)
            state.SetPlayerVelocity(weaponData.movementSpeed[attackCounter]);
    }

    public virtual void AnimationStopMovementTrigger()
    {
        if (state != null)
            state.SetPlayerVelocity(0f);
    }

    public virtual void AnimationTurnOffFlipTrigger()
    {
        if (state != null)
            state.SetFlipCheck(false);
    }

    public virtual void AnimationTurnOnFlipTigger()
    {
        if (state != null)
            state.SetFlipCheck(true);
    }

    public virtual void AnimationActionTrigger() { }


    #endregion

    public void InitializeWeapon(PlayerAttackState state)
    {
        this.state = state;
    }

    public float GetMovementSpeed(int index)
    {
        if (weaponData != null && weaponData.movementSpeed != null && index < weaponData.movementSpeed.Length && index >= 0)
            return weaponData.movementSpeed[index];
        return 0f;
    }

    public float GetAttackClipLength()
    {
        if (baseAnimator == null)
        {
            var baseTransform = transform.Find("Base");
            if (baseTransform != null)
                baseAnimator = baseTransform.GetComponent<Animator>();
            else
                baseAnimator = GetComponentInChildren<Animator>();
        }

        if (baseAnimator == null)
            return 0f;

        var controller = baseAnimator.runtimeAnimatorController;
        if (controller == null)
            return 0f;

        var clips = controller.animationClips;
        if (clips == null || clips.Length == 0)
            return 0f;

        float best = 0f;
        foreach (var c in clips)
        {
            if (c == null)
                continue;
            var name = c.name ?? string.Empty;
            if (name.ToLower().Contains("empty"))
                continue;
            if (c.length > best)
                best = c.length;
        }

        return best;
    }
}
