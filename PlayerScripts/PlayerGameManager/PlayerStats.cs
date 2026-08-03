using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent player state shared across scenes. Lives as a ScriptableObject asset so its
/// values survive scene loads within a play session.
///
/// Abilities are split into three groups:
///  - Base: the movement kit (unlockable, but usually always-on via allBaseAbilitiesUnlocked).
///  - Secondary: permanent once unlocked, cannot be unequipped (Attack, Warp).
///  - Divine Interventions: equippable powers (max N at once) that grant multipliers and
///    extra abilities; equipping applies their multipliers through the multiplier system.
///
/// Runtime values are NOT serialized: they reset each play session and are (re)built by
/// EnsureInitialized, so they persist between scenes but never get baked into the asset.
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

    [Header("Base Abilities (movement kit)")]
    [Tooltip("When true, all base abilities are always available (the unlocked set below is ignored).")]
    [SerializeField] private bool allBaseAbilitiesUnlocked = true;
    [SerializeField] private List<BaseAbility> defaultUnlockedBase = new List<BaseAbility>();

    [Header("Secondary Abilities (permanent once unlocked)")]
    [SerializeField] private List<SecondaryAbility> defaultUnlockedSecondary = new List<SecondaryAbility>();

    [Header("Divine Interventions")]
    [Tooltip("Every divine intervention in the game — used to resolve them when loading a save.")]
    [SerializeField] private List<SO_DivineIntervention> divineLibrary = new List<SO_DivineIntervention>();
    [SerializeField] private List<SO_DivineIntervention> defaultUnlockedDivine = new List<SO_DivineIntervention>();
    [SerializeField] private List<SO_DivineIntervention> defaultEquippedDivine = new List<SO_DivineIntervention>();
    [Tooltip("Maximum Divine Interventions equipped at once.")]
    [SerializeField] private int maxEquippedDivine = 2;
    #endregion

    #region Runtime (not serialized — reset per session, persist across scenes)
    private bool isInitialized;

    private float currentHealth;
    private int currentCoins;

    private List<StatModifier> speedMultipliers;
    private List<StatModifier> hpMultipliers;
    private List<StatModifier> damageMultipliers;

    private HashSet<BaseAbility> unlockedBase;
    private HashSet<SecondaryAbility> unlockedSecondary;
    private List<SO_DivineIntervention> unlockedDivine;
    private List<SO_DivineIntervention> equippedDivine;

    private int activeWeaponIndex;
    private bool canAttack = true;
    private bool canWarp = true;

    private string lastCheckpointScene;
    private Vector3 lastCheckpointPosition;
    private bool hasCheckpoint;

    private long createdTicks;
    #endregion

    #region Events
    /// <summary>Args: (currentHealth, effectiveMaxHealth).</summary>
    public event Action<float, float> OnHealthChanged;
    public event Action<int> OnCoinsChanged;
    public event Action OnDeath;
    /// <summary>Fired when any ability group changes (for HUD refresh).</summary>
    public event Action OnAbilitiesChanged;
    #endregion

    #region Lifecycle
    private void OnEnable()
    {
        // ScriptableObjects keep runtime values between play sessions in the editor. Clearing
        // the init flag on (re)load guarantees a fresh launch rebuilds from config.
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

        unlockedBase = new HashSet<BaseAbility>(defaultUnlockedBase);
        unlockedSecondary = new HashSet<SecondaryAbility>(defaultUnlockedSecondary);
        unlockedDivine = new List<SO_DivineIntervention>(defaultUnlockedDivine);
        equippedDivine = new List<SO_DivineIntervention>();

        // Equip the default divines (also ensures they're in the unlocked pool). This applies
        // their multipliers before we set starting health below.
        foreach (var d in defaultEquippedDivine)
        {
            if (d == null) continue;
            if (!unlockedDivine.Contains(d)) unlockedDivine.Add(d);
            TryEquipDivineInternal(d);
        }

        currentCoins = startingCoins;
        activeWeaponIndex = 0;
        canAttack = true;
        canWarp = true;

        lastCheckpointScene = null;
        lastCheckpointPosition = Vector3.zero;
        hasCheckpoint = false;

        createdTicks = DateTime.Now.Ticks; // stamped now; overwritten by a loaded save

        currentHealth = EffectiveMaxHealth; // includes any equipped-divine HP multipliers
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
        OnCoinsChanged?.Invoke(currentCoins);
        OnAbilitiesChanged?.Invoke();
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

    #region Base Abilities
    public bool IsBaseUnlocked(BaseAbility ability)
        => allBaseAbilitiesUnlocked || (unlockedBase != null && unlockedBase.Contains(ability));

    public void UnlockBase(BaseAbility ability)
    {
        if (unlockedBase == null) unlockedBase = new HashSet<BaseAbility>();
        if (unlockedBase.Add(ability))
            OnAbilitiesChanged?.Invoke();
    }
    #endregion

    #region Secondary Abilities (permanent once unlocked)
    public IReadOnlyCollection<SecondaryAbility> UnlockedSecondary => unlockedSecondary;

    public bool IsSecondaryUnlocked(SecondaryAbility ability)
        => unlockedSecondary != null && unlockedSecondary.Contains(ability);

    public void UnlockSecondary(SecondaryAbility ability)
    {
        if (unlockedSecondary == null) unlockedSecondary = new HashSet<SecondaryAbility>();
        if (unlockedSecondary.Add(ability))
            OnAbilitiesChanged?.Invoke();
    }
    #endregion

    #region Divine Interventions (equip up to max)
    public int MaxEquippedDivine => maxEquippedDivine;
    public IReadOnlyList<SO_DivineIntervention> UnlockedDivine => unlockedDivine;
    public IReadOnlyList<SO_DivineIntervention> EquippedDivine => equippedDivine;

    public bool IsDivineUnlocked(SO_DivineIntervention d) => d != null && unlockedDivine != null && unlockedDivine.Contains(d);
    public bool IsDivineEquipped(SO_DivineIntervention d) => d != null && equippedDivine != null && equippedDivine.Contains(d);
    public bool DivineSlotsFull => equippedDivine != null && equippedDivine.Count >= maxEquippedDivine;

    public void UnlockDivine(SO_DivineIntervention d)
    {
        if (d == null) return;
        if (unlockedDivine == null) unlockedDivine = new List<SO_DivineIntervention>();
        if (!unlockedDivine.Contains(d))
        {
            unlockedDivine.Add(d);
            OnAbilitiesChanged?.Invoke();
        }
    }

    /// <summary>Equips a divine intervention. Returns false if it isn't unlocked or slots are full.</summary>
    public bool EquipDivine(SO_DivineIntervention d)
    {
        if (!IsDivineUnlocked(d)) return false;
        if (!TryEquipDivineInternal(d)) return false;
        OnAbilitiesChanged?.Invoke();
        return true;
    }

    public void UnequipDivine(SO_DivineIntervention d)
    {
        if (d == null || equippedDivine == null) return;
        if (equippedDivine.Remove(d))
        {
            RemoveDivineModifiers(d);
            OnAbilitiesChanged?.Invoke();
        }
    }

    /// <summary>True if any equipped divine intervention grants the given extra ability.</summary>
    public bool HasDivineEffect(DivineEffect effect)
    {
        if (equippedDivine == null) return false;
        foreach (var d in equippedDivine)
            if (d != null && d.Grants(effect)) return true;
        return false;
    }

    // Adds to the equipped list + applies modifiers, without firing the change event or
    // checking the unlocked pool (callers handle those).
    private bool TryEquipDivineInternal(SO_DivineIntervention d)
    {
        if (d == null) return false;
        if (equippedDivine == null) equippedDivine = new List<SO_DivineIntervention>();
        if (equippedDivine.Contains(d)) return true;
        if (equippedDivine.Count >= maxEquippedDivine) return false;

        equippedDivine.Add(d);
        ApplyDivineModifiers(d);
        return true;
    }

    private void ApplyDivineModifiers(SO_DivineIntervention d)
    {
        string id = "divine:" + d.Id;
        if (!Mathf.Approximately(d.speedMultiplier, 1f)) AddSpeedMultiplier(id, d.speedMultiplier);
        if (!Mathf.Approximately(d.damageMultiplier, 1f)) AddDamageMultiplier(id, d.damageMultiplier);
        if (!Mathf.Approximately(d.healthMultiplier, 1f)) AddHealthMultiplier(id, d.healthMultiplier);
    }

    private void RemoveDivineModifiers(SO_DivineIntervention d)
    {
        string id = "divine:" + d.Id;
        RemoveSpeedMultiplier(id);
        RemoveDamageMultiplier(id);
        RemoveHealthMultiplier(id);
    }
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
            createdTicks = createdTicks,
            maxHealth = maxHealth,
            currentHealth = currentHealth,
            currentCoins = currentCoins,
            unlockedBase = unlockedBase != null ? new List<BaseAbility>(unlockedBase) : new List<BaseAbility>(),
            unlockedSecondary = unlockedSecondary != null ? new List<SecondaryAbility>(unlockedSecondary) : new List<SecondaryAbility>(),
            unlockedDivineIds = IdsOf(unlockedDivine),
            equippedDivineIds = IdsOf(equippedDivine),
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

        ResetToNewGame(); // clean slate (also clears default-equipped divine modifiers below)

        if (data.createdTicks != 0) createdTicks = data.createdTicks; // keep original creation stamp
        if (data.maxHealth > 0f) maxHealth = data.maxHealth;
        currentCoins = Mathf.Max(0, data.currentCoins);

        unlockedBase = new HashSet<BaseAbility>(data.unlockedBase ?? new List<BaseAbility>());
        unlockedSecondary = new HashSet<SecondaryAbility>(data.unlockedSecondary ?? new List<SecondaryAbility>());

        // Rebuild divine state from ids, clearing whatever ResetToNewGame equipped by default.
        foreach (var d in new List<SO_DivineIntervention>(equippedDivine))
            UnequipDivine(d);
        unlockedDivine = ResolveDivine(data.unlockedDivineIds);
        if (data.equippedDivineIds != null)
            foreach (var id in data.equippedDivineIds)
            {
                var d = FindDivine(id);
                if (d != null) TryEquipDivineInternal(d);
            }

        activeWeaponIndex = Mathf.Max(0, data.activeWeaponIndex);
        hasCheckpoint = data.hasCheckpoint;
        lastCheckpointScene = data.checkpointScene;
        lastCheckpointPosition = data.checkpointPosition;

        currentHealth = Mathf.Clamp(data.currentHealth, 0f, EffectiveMaxHealth);
        isInitialized = true;

        OnHealthChanged?.Invoke(currentHealth, EffectiveMaxHealth);
        OnCoinsChanged?.Invoke(currentCoins);
        OnAbilitiesChanged?.Invoke();
    }

    private static List<string> IdsOf(List<SO_DivineIntervention> list)
    {
        var ids = new List<string>();
        if (list != null)
            foreach (var d in list)
                if (d != null) ids.Add(d.Id);
        return ids;
    }

    private List<SO_DivineIntervention> ResolveDivine(List<string> ids)
    {
        var result = new List<SO_DivineIntervention>();
        if (ids == null) return result;
        foreach (var id in ids)
        {
            var d = FindDivine(id);
            if (d != null && !result.Contains(d)) result.Add(d);
        }
        return result;
    }

    private SO_DivineIntervention FindDivine(string id)
    {
        if (string.IsNullOrEmpty(id) || divineLibrary == null) return null;
        foreach (var d in divineLibrary)
            if (d != null && d.Id == id) return d;
        return null;
    }
    #endregion
}
