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

    #region Base Abilities (movement kit)
    public bool IsBaseUnlocked(BaseAbility ability) => stats != null && stats.IsBaseUnlocked(ability);
    public void UnlockBase(BaseAbility ability) => stats?.UnlockBase(ability);
    #endregion

    #region Secondary Abilities (permanent once unlocked)
    public bool IsSecondaryUnlocked(SecondaryAbility ability) => stats != null && stats.IsSecondaryUnlocked(ability);
    public void UnlockSecondary(SecondaryAbility ability) => stats?.UnlockSecondary(ability);
    #endregion

    #region Divine Interventions (equip up to max)
    public bool IsDivineUnlocked(SO_DivineIntervention divine) => stats != null && stats.IsDivineUnlocked(divine);
    public bool IsDivineEquipped(SO_DivineIntervention divine) => stats != null && stats.IsDivineEquipped(divine);
    public bool DivineSlotsFull => stats != null && stats.DivineSlotsFull;

    public void UnlockDivine(SO_DivineIntervention divine) => stats?.UnlockDivine(divine);

    /// <summary>Equips a divine intervention; returns false if not unlocked or slots are full.</summary>
    public bool EquipDivine(SO_DivineIntervention divine) => stats != null && stats.EquipDivine(divine);

    public void UnequipDivine(SO_DivineIntervention divine) => stats?.UnequipDivine(divine);

    /// <summary>Unlocks a divine and equips it if there's a free slot (e.g. picking one up).</summary>
    public void UnlockAndEquipDivine(SO_DivineIntervention divine)
    {
        if (stats == null) return;
        stats.UnlockDivine(divine);
        stats.EquipDivine(divine);
    }
    #endregion
}
