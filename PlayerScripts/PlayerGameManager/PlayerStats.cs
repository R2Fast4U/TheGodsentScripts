using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent player state shared across scenes. Lives as a ScriptableObject asset so its
/// values survive scene loads within a play session.
///
/// Design-time config (maxHealth, starting coins, default abilities) is serialized and
/// authored in the Inspector. Runtime values (current health/coins, active multipliers,
/// unlocked/equipped abilities, gates, active weapon) are intentionally NOT serialized:
/// they reset at the start of each play session and are (re)built by <see cref="EnsureInitialized"/>,
/// so they persist between scenes but never get baked into the asset. Load a save file into
/// these via the setters/Unlock/Equip calls when you add a save system.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "Data/Player Data/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Serializable]
    public struct StatModifier
    {
        [Tooltip("Source of this modifier (item/buff/ability name) so it can be removed later.")]
        public string id;
        [Tooltip("Multiplier value, e.g. 1.2 = +20%, 0.5 = -50%.")]
        public float multiplier;
    }

    #region Config (serialized, design-time)
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Economy")]
    [SerializeField] private int startingCoins = 0;

    [Header("Starting Abilities")]
    [SerializeField] private List<AbilityType> defaultUnlockedAbilities = new List<AbilityType>();
    [SerializeField] private List<AbilityType> defaultEquippedAbilities = new List<AbilityType>();
    #endregion

    #region Runtime (not serialized — reset per session, persist across scenes)
    private bool isInitialized;

    private float currentHealth;
    private int currentCoins;

    private List<StatModifier> speedMultipliers;
    private List<StatModifier> hpMultipliers;
    private List<StatModifier> damageMultipliers;

    private List<AbilityType> unlockedAbilities;
    private List<AbilityType> equippedAbilities;

    private int activeWeaponIndex;
    private bool canAttack = true;
    private bool canWarp = true;

    private string lastCheckpointScene;
    private Vector3 lastCheckpointPosition;
    private bool hasCheckpoint;
    #endregion

    #region Events
    /// <summary>Args: (currentHealth, effectiveMaxHealth).</summary>
    public event Action<float, float> OnHealthChanged;
    public event Action<int> OnCoinsChanged;
    public event Action OnDeath;
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        // ScriptableObjects keep their runtime values between play sessions in the editor.
        // Clearing the init flag when the asset (re)loads guarantees that a fresh launch always
        // rebuilds stats from config (full health, default abilities, no checkpoint) via the
        // next EnsureInitialized, instead of inheriting the previous session's state.
        isInitialized = false;
    }

    /// <summary>Builds runtime state from config the first time it's needed each session.
    /// Idempotent, so it's safe for every scene's Player to call it on Awake.</summary>
    public void EnsureInitialized()
    {
        if (isInitialized) return;
        ResetToNewGame();
    }

    /// <summary>Wipes runtime state back to a fresh new-game start.</summary>
    public void ResetToNewGame()
    {
        speedMultipliers = new List<StatModifier>();
        hpMultipliers = new List<StatModifier>();
        damageMultipliers = new List<StatModifier>();

        unlockedAbilities = new List<AbilityType>(defaultUnlockedAbilities);
        equippedAbilities = new List<AbilityType>(defaultEquippedAbilities);

        currentCoins = startingCoins;
        activeWeaponIndex = 0;
        canAttack = true;
        canWarp = true;

        lastCheckpointScene = null;
        lastCheckpointPosition = Vector3.zero;
        hasCheckpoint = false;

        currentHealth = EffectiveMaxHealth; // multipliers are empty here, so == maxHealth
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
        OnCoinsChanged?.Invoke(currentCoins);
    }
    #endregion

    #region Health
    public float MaxHealth => maxHealth;
    public float EffectiveMaxHealth => maxHealth * HealthMultiplier;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        ClampHealthToMax();
    }

    /// <summary>Applies a signed change to health (negative = damage, positive = heal).</summary>
    public void ModifyHealth(float delta)
    {
        float max = EffectiveMaxHealth;
        currentHealth = Mathf.Clamp(currentHealth + delta, 0f, max);
        OnHealthChanged?.Invoke(currentHealth, max);
        if (currentHealth <= 0f)
            OnDeath?.Invoke();
    }

    public void Heal(float amount) => ModifyHealth(Mathf.Abs(amount));
    public void RefillHealth() => ModifyHealth(EffectiveMaxHealth);

    private void ClampHealthToMax()
    {
        float max = EffectiveMaxHealth;
        if (currentHealth > max) currentHealth = max;
        OnHealthChanged?.Invoke(currentHealth, max);
    }
    #endregion

    #region Coins
    public int CurrentCoins => currentCoins;

    public void AddCoins(int amount)
    {
        currentCoins = Mathf.Max(0, currentCoins + amount);
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || currentCoins < amount) return false;
        currentCoins -= amount;
        OnCoinsChanged?.Invoke(currentCoins);
        return true;
    }
    #endregion

    #region Multipliers
    // Aggregate = product of every active modifier (1 when none are active).
    public float SpeedMultiplier => Aggregate(speedMultipliers);
    public float HealthMultiplier => Aggregate(hpMultipliers);
    public float DamageMultiplier => Aggregate(damageMultipliers);

    public void AddSpeedMultiplier(string id, float multiplier) => AddModifier(ref speedMultipliers, id, multiplier);
    public void RemoveSpeedMultiplier(string id) => RemoveModifier(speedMultipliers, id);

    public void AddDamageMultiplier(string id, float multiplier) => AddModifier(ref damageMultipliers, id, multiplier);
    public void RemoveDamageMultiplier(string id) => RemoveModifier(damageMultipliers, id);

    // HP multipliers change effective max health, so re-clamp current health when they change.
    public void AddHealthMultiplier(string id, float multiplier)
    {
        AddModifier(ref hpMultipliers, id, multiplier);
        ClampHealthToMax();
    }

    public void RemoveHealthMultiplier(string id)
    {
        RemoveModifier(hpMultipliers, id);
        ClampHealthToMax();
    }

    private static float Aggregate(List<StatModifier> list)
    {
        if (list == null) return 1f;
        float result = 1f;
        foreach (var mod in list)
            result *= mod.multiplier;
        return result;
    }

    // Re-applying the same id refreshes rather than stacks it.
    private static void AddModifier(ref List<StatModifier> list, string id, float multiplier)
    {
        if (list == null) list = new List<StatModifier>();
        list.RemoveAll(m => m.id == id);
        list.Add(new StatModifier { id = id, multiplier = multiplier });
    }

    private static void RemoveModifier(List<StatModifier> list, string id)
    {
        list?.RemoveAll(m => m.id == id);
    }
    #endregion

    #region Abilities
    public IReadOnlyList<AbilityType> UnlockedAbilities => unlockedAbilities;
    public IReadOnlyList<AbilityType> EquippedAbilities => equippedAbilities;

    public bool IsUnlocked(AbilityType ability) => unlockedAbilities != null && unlockedAbilities.Contains(ability);
    public bool IsEquipped(AbilityType ability) => equippedAbilities != null && equippedAbilities.Contains(ability);

    public void Unlock(AbilityType ability)
    {
        if (unlockedAbilities == null) unlockedAbilities = new List<AbilityType>();
        if (!unlockedAbilities.Contains(ability))
            unlockedAbilities.Add(ability);
    }

    /// <summary>Equips an ability. Returns false if it isn't unlocked yet.</summary>
    public bool Equip(AbilityType ability)
    {
        if (!IsUnlocked(ability)) return false;
        if (equippedAbilities == null) equippedAbilities = new List<AbilityType>();
        if (!equippedAbilities.Contains(ability))
            equippedAbilities.Add(ability);
        return true;
    }

    public void Unequip(AbilityType ability) => equippedAbilities?.Remove(ability);
    #endregion

    #region Active Weapon & Gates
    public int ActiveWeaponIndex
    {
        get => activeWeaponIndex;
        set => activeWeaponIndex = Mathf.Max(0, value);
    }

    public bool CanAttack
    {
        get => canAttack;
        set => canAttack = value;
    }

    public bool CanWarp
    {
        get => canWarp;
        set => canWarp = value;
    }
    #endregion

    #region Checkpoint
    public bool HasCheckpoint => hasCheckpoint;
    public string LastCheckpointScene => lastCheckpointScene;
    public Vector3 LastCheckpointPosition => lastCheckpointPosition;

    /// <summary>Records where the player should respawn. Persists across scenes for the session.</summary>
    public void SetCheckpoint(string sceneName, Vector3 position)
    {
        lastCheckpointScene = sceneName;
        lastCheckpointPosition = position;
        hasCheckpoint = true;
    }
    #endregion

    #region Save / Load
    /// <summary>Exports persistent state into a disk-serializable snapshot.</summary>
    public SaveData ToSaveData()
    {
        EnsureInitialized();
        return new SaveData
        {
            maxHealth = maxHealth,
            currentHealth = currentHealth,
            currentCoins = currentCoins,
            unlockedAbilities = new List<AbilityType>(unlockedAbilities),
            equippedAbilities = new List<AbilityType>(equippedAbilities),
            activeWeaponIndex = activeWeaponIndex,
            hasCheckpoint = hasCheckpoint,
            checkpointScene = lastCheckpointScene,
            checkpointPosition = lastCheckpointPosition
        };
    }

    /// <summary>Restores persistent state from a saved snapshot. Null data falls back to a fresh start.</summary>
    public void LoadFromSaveData(SaveData data)
    {
        if (data == null)
        {
            ResetToNewGame();
            return;
        }

        ResetToNewGame(); // clean slate for transient state (multipliers, gates), then apply saved values

        if (data.maxHealth > 0f) maxHealth = data.maxHealth;
        currentCoins = Mathf.Max(0, data.currentCoins);
        unlockedAbilities = new List<AbilityType>(data.unlockedAbilities ?? new List<AbilityType>());
        equippedAbilities = new List<AbilityType>(data.equippedAbilities ?? new List<AbilityType>());
        activeWeaponIndex = Mathf.Max(0, data.activeWeaponIndex);
        hasCheckpoint = data.hasCheckpoint;
        lastCheckpointScene = data.checkpointScene;
        lastCheckpointPosition = data.checkpointPosition;

        currentHealth = Mathf.Clamp(data.currentHealth, 0f, EffectiveMaxHealth);
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
        OnCoinsChanged?.Invoke(currentCoins);
    }
    #endregion
}
