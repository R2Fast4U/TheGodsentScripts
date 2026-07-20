using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackDetails
{
    public float damageAmount;
    public Vector2 position;

    public float stunDamageAmount;
}
[System.Serializable]
public struct WeaponAttackDetails
{
    public string attackName;
    public float movementSpeed;
    public float damageAmount;
    public Vector2 position;

}