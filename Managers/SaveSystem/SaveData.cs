using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain serializable snapshot of everything that should persist to disk between play
/// sessions. Mirrors the persistent fields of <see cref="PlayerStats"/>. Transient state
/// (active buffs/multipliers, the CanAttack/CanWarp gates) is intentionally not saved.
/// </summary>
[Serializable]
public class SaveData
{
    public float maxHealth;
    public float currentHealth;
    public int currentCoins;

    public List<AbilityType> unlockedAbilities = new List<AbilityType>();
    public List<AbilityType> equippedAbilities = new List<AbilityType>();

    public int activeWeaponIndex;

    public bool hasCheckpoint;
    public string checkpointScene;
    public Vector3 checkpointPosition;
}
