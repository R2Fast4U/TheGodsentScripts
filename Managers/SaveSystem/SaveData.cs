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
    [Tooltip("DateTime.Ticks of when this save was first created (for the menu's slot labels).")]
    public long createdTicks;

    public float maxHealth;
    public float currentHealth;
    public int currentCoins;

    public List<BaseAbility> unlockedBase = new List<BaseAbility>();
    public List<SecondaryAbility> unlockedSecondary = new List<SecondaryAbility>();
    public List<string> unlockedDivineIds = new List<string>();
    public List<string> equippedDivineIds = new List<string>();

    public int activeWeaponIndex;

    public bool hasCheckpoint;
    public string checkpointScene;
    public Vector3 checkpointPosition;
}
