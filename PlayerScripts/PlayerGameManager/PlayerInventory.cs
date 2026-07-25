using UnityEngine;

/// <summary>
/// Player-side inventory: owns the weapon list and provides the API for unlocking/equipping
/// abilities and selecting the active weapon. The persistent record of what's unlocked/equipped
/// (and health/coins) lives in <see cref="PlayerStats"/> so it survives scene changes; this
/// component is the higher-level interface gameplay talks to.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Tooltip("Weapons available to the player. The active one is chosen via PlayerStats.ActiveWeaponIndex.")]
    public Weapon[] Weapons;

    [Tooltip("Shared persistent stats asset. Assign the same PlayerStats asset used by the player's Combat component.")]
    [SerializeField] private PlayerStats stats;
    public PlayerStats Stats => stats;

    private void Awake()
    {
        // Make sure runtime stats exist before anything reads them this scene.
        if (stats != null)
            stats.EnsureInitialized();
    }

    #region Weapons
    /// <summary>The currently selected weapon, or null if the index is invalid.</summary>
    public Weapon ActiveWeapon
    {
        get
        {
            if (Weapons == null || stats == null) return null;
            int i = stats.ActiveWeaponIndex;
            return (i >= 0 && i < Weapons.Length) ? Weapons[i] : null;
        }
    }

    public void SetActiveWeapon(int index)
    {
        if (stats == null || Weapons == null || Weapons.Length == 0) return;
        stats.ActiveWeaponIndex = Mathf.Clamp(index, 0, Weapons.Length - 1);
    }
    #endregion

    #region Abilities
    public bool IsAbilityUnlocked(AbilityType ability) => stats != null && stats.IsUnlocked(ability);
    public bool IsAbilityEquipped(AbilityType ability) => stats != null && stats.IsEquipped(ability);

    public void UnlockAbility(AbilityType ability) => stats?.Unlock(ability);

    /// <summary>Equips an ability; returns false if it isn't unlocked (or no stats assigned).</summary>
    public bool EquipAbility(AbilityType ability) => stats != null && stats.Equip(ability);

    public void UnequipAbility(AbilityType ability) => stats?.Unequip(ability);

    /// <summary>Unlocks and immediately equips an ability (e.g. picking up a new power).</summary>
    public void UnlockAndEquip(AbilityType ability)
    {
        if (stats == null) return;
        stats.Unlock(ability);
        stats.Equip(ability);
    }
    #endregion
}
