using UnityEngine;

/// <summary>
/// A Divine Intervention: an equippable power (max two at a time) that grants stat multipliers
/// and/or extra abilities. Authored as an asset so each one is data-driven.
/// </summary>
[CreateAssetMenu(fileName = "DivineIntervention", menuName = "Data/Abilities/Divine Intervention")]
public class SO_DivineIntervention : ScriptableObject
{
    [Tooltip("Stable id used for saving. Leave empty to use the asset name.")]
    [SerializeField] private string id;
    public string Id => string.IsNullOrEmpty(id) ? name : id;

    public string displayName;
    public Sprite icon;
    [Tooltip("HUD icon size in pixels (square). 0 = use the HUD's default size.")]
    public float iconSize = 0f;

    [Header("Multipliers (1 = no change)")]
    public float speedMultiplier = 1f;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;

    [Header("Extra Abilities Granted")]
    public DivineEffect[] grantedEffects;

    public bool Grants(DivineEffect effect)
    {
        if (grantedEffects == null) return false;
        foreach (var e in grantedEffects)
            if (e == effect) return true;
        return false;
    }
}
