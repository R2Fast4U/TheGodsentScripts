using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AgressiveWeapon : Weapon
{
    protected SO_AggressiveWeaponData aggressiveWeaponData;
        private List<IDamageable> detectedDamageable = new List<IDamageable>();
        
        protected override void Awake()
    {
        base.Awake();

        if (weaponData == null)
        {
            Debug.LogError($"Weapon '{name}': weaponData (SO_AggressiveWeaponData) is not assigned in the Inspector.");
            return;
        }

        if (weaponData.GetType() == typeof(SO_AggressiveWeaponData))
        {
            aggressiveWeaponData = (SO_AggressiveWeaponData)weaponData;
        }
        else
        {
            Debug.LogError($"Weapon '{name}': weaponData is not of type SO_AggressiveWeaponData. Actual type: {weaponData.GetType().Name}");
        }
    }
            public override void AnimationActionTrigger()
    {
        base.AnimationActionTrigger();
        CheckMeleeAttack();
    }

    private void CheckMeleeAttack()
    {
        if (aggressiveWeaponData == null || aggressiveWeaponData.AttackDetails == null || aggressiveWeaponData.AttackDetails.Length == 0)
        {
            Debug.LogError($"Weapon '{name}': aggressiveWeaponData or AttackDetails is null/empty — cannot apply damage.");
            return;
        }

        int idx = attackCounter;
        if (idx < 0 || idx >= aggressiveWeaponData.AttackDetails.Length)
            idx = 0;

        WeaponAttackDetails details = aggressiveWeaponData.AttackDetails[idx];
        Debug.Log($"Weapon '{name}' CheckMeleeAttack: applying {details.damageAmount} damage to {detectedDamageable.Count} target(s).");

        if (detectedDamageable.Count > 0)
            CinemachineOffsetController.Instance?.TriggerHitZoom();

        float damage = details.damageAmount * GetDamageMultiplier();
        foreach (IDamageable item in detectedDamageable.ToList())
        {
            item.Damage(damage);
        }
    }

    // Scales dealt damage by the player's active damage multipliers (from PlayerStats).
    private float GetDamageMultiplier()
    {
        Player player = state != null ? state.GetPlayer() : null;
        return player != null && player.Stats != null ? player.Stats.DamageMultiplier : 1f;
    }

    public void DealDamageTo(Collider2D collision)
    {
        if (!collision.TryGetComponent(out IDamageable damageable)) return;

        if (aggressiveWeaponData == null || aggressiveWeaponData.AttackDetails == null || aggressiveWeaponData.AttackDetails.Length == 0)
        {
            Debug.LogError($"Weapon '{name}': cannot deal damage — aggressiveWeaponData or AttackDetails not set up.");
            return;
        }

        int idx = Mathf.Clamp(attackCounter, 0, aggressiveWeaponData.AttackDetails.Length - 1);
        damageable.Damage(aggressiveWeaponData.AttackDetails[idx].damageAmount * GetDamageMultiplier());
    }

    public void AddToDetected(Collider2D collision)
    {
        Debug.Log("Adding to detected: " + collision.name);
        IDamageable damageable = collision.GetComponent<IDamageable>() ?? collision.GetComponentInParent<IDamageable>();
        if (damageable != null && !detectedDamageable.Contains(damageable))
        {
            detectedDamageable.Add(damageable);
        }
    }

    public void RemoveFromDetected(Collider2D collision)
    {
        Debug.Log("Removing from detected: " + collision.name);
        IDamageable damageable = collision.GetComponent<IDamageable>() ?? collision.GetComponentInParent<IDamageable>();
        if (damageable != null && detectedDamageable.Contains(damageable))
        {
            detectedDamageable.Remove(damageable);
        }
    }

}
