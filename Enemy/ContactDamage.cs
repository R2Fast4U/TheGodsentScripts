using UnityEngine;

/// <summary>
/// Deals damage (and knockback) to anything on <see cref="targetLayer"/> that overlaps
/// this object's collider. Drop it on a passive enemy whose hitbox should hurt the player
/// on contact — no AI or state machine required.
///
/// Setup:
///  - Put a Collider2D on this GameObject shaped like the hitbox and tick "Is Trigger".
///  - Set Target Layer to your Player layer.
///  - The other object (the player) needs a Rigidbody2D for the callbacks to fire — it has one.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ContactDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [Tooltip("Seconds between damage ticks while the target stays inside the hitbox.")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("Targeting")]
    [Tooltip("Which layers get hurt (set this to your Player layer).")]
    [SerializeField] private LayerMask targetLayer;

    private float lastDamageTime = float.NegativeInfinity;

    // Fires every physics step while the target is inside a trigger collider.
    private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

    // If the hitbox is a *solid* (non-trigger) collider instead, comment out the line
    // above and use this one:
    // private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);

    private void TryDamage(Collider2D other)
    {
        // Only affect objects on the target (Player) layer.
        if ((targetLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (Time.time < lastDamageTime + damageCooldown) return;

        // The player's IDamageable/IKnockbackable live on its child Core component,
        // so search children from the collider we hit.
        IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInChildren<IDamageable>();
        if (damageable == null) return;

        lastDamageTime = Time.time;
        damageable.Damage(damage);

        IKnockbackable knockbackable = other.GetComponent<IKnockbackable>() ?? other.GetComponentInChildren<IKnockbackable>();
        // Push the target away from this enemy: +1 if it's to our right, -1 if to our left.
        int knockbackDirection = other.transform.position.x >= transform.position.x ? 1 : -1;
        knockbackable?.Knockback(knockbackDirection);
    }
}
