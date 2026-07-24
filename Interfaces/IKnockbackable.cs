using UnityEngine;

/// <summary>
/// Implemented by anything that can be knocked back by another object (an enemy
/// touch, a weapon, a projectile, etc.). The attacker only supplies the direction
/// the hit came from (+1 = pushed right, -1 = pushed left); the strength, angle and
/// duration of the knockback are tuned on the receiver.
/// </summary>
public interface IKnockbackable
{
    void Knockback(int direction);
}
