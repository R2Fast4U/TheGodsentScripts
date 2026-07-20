using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHitboxToWeapon : MonoBehaviour
{
    private AgressiveWeapon weapon;

    private void Awake()
    {
        weapon = GetComponentInParent<AgressiveWeapon>();
        if (weapon == null)
            weapon = GetComponent<AgressiveWeapon>();
        if (weapon == null)
            Debug.LogError($"WeaponHitboxToWeapon on '{name}': could not find AgressiveWeapon in parent or self.");
    }

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        var rb = GetComponentInParent<Rigidbody2D>();
        Debug.Log($"WeaponHitboxToWeapon on '{name}': Start — isTrigger={col != null && col.isTrigger}, colliderType={col?.GetType().Name}, activeInHierarchy={gameObject.activeInHierarchy}, layer={gameObject.layer}, rbInParent={rb != null}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Collision detected with: " + collision.name);
        if (weapon == null) return;
        weapon.AddToDetected(collision);
        weapon.DealDamageTo(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (weapon == null) return;
        weapon.RemoveFromDetected(collision);
    }
}
