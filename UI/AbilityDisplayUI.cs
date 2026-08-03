using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's abilities as icons. Configure which groups it displays:
///  - Secondary: unlocked secondary abilities (permanent) — uses the icon map below.
///  - Divine: currently equipped Divine Interventions — uses each asset's own icon.
/// Place two instances (one per group) for separate HUD bars, or enable both on one.
/// </summary>
public class AbilityDisplayUI : MonoBehaviour
{
    [System.Serializable]
    public struct SecondaryIcon
    {
        public SecondaryAbility ability;
        public Sprite icon;
        [Tooltip("Icon size in pixels (square). 0 = use Default Icon Size.")]
        public float size;
    }

    [SerializeField] private PlayerStats stats;

    [Header("What to show")]
    [SerializeField] private bool showSecondary = true;
    [SerializeField] private bool showDivine = true;

    [Header("Secondary icon map")]
    [SerializeField] private SecondaryIcon[] secondaryIcons;

    [Header("Layout")]
    [Tooltip("Parent the icons spawn under (ideally with a Layout Group).")]
    [SerializeField] private Transform iconContainer;
    [Tooltip("Prefab with an Image component, instantiated per shown ability.")]
    [SerializeField] private Image iconPrefab;
    [Tooltip("Fallback icon size (pixels) when an entry doesn't specify its own.")]
    [SerializeField] private float defaultIconSize = 64f;
    [Tooltip("Keep each sprite's aspect ratio inside its size box (prevents squishing non-square icons).")]
    [SerializeField] private bool preserveAspect = true;

    private readonly List<Image> spawned = new List<Image>();

    private void OnEnable()
    {
        if (stats == null) return;

        stats.EnsureInitialized();
        stats.OnAbilitiesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnAbilitiesChanged -= Refresh;
    }

    private void Refresh()
    {
        foreach (var img in spawned)
            if (img != null) Destroy(img.gameObject);
        spawned.Clear();

        if (iconContainer == null || iconPrefab == null) return;

        if (showSecondary && stats.UnlockedSecondary != null)
            foreach (SecondaryAbility ability in stats.UnlockedSecondary)
            {
                if (TryGetSecondary(ability, out Sprite sprite, out float size))
                    SpawnIcon(sprite, size);
            }

        if (showDivine && stats.EquippedDivine != null)
            foreach (SO_DivineIntervention divine in stats.EquippedDivine)
                if (divine != null) SpawnIcon(divine.icon, divine.iconSize);
    }

    private void SpawnIcon(Sprite sprite, float perIconSize)
    {
        if (sprite == null) return;

        float size = perIconSize > 0f ? perIconSize : defaultIconSize;

        Image img = Instantiate(iconPrefab, iconContainer);
        img.sprite = sprite;
        img.enabled = true;
        img.preserveAspect = preserveAspect;
        img.rectTransform.sizeDelta = new Vector2(size, size);

        // Also drive layout-group sizing so it holds whether or not the group controls child size.
        var le = img.GetComponent<LayoutElement>();
        if (le == null) le = img.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;

        spawned.Add(img);
    }

    private bool TryGetSecondary(SecondaryAbility ability, out Sprite sprite, out float size)
    {
        foreach (var entry in secondaryIcons)
            if (entry.ability == ability)
            {
                sprite = entry.icon;
                size = entry.size;
                return true;
            }
        sprite = null;
        size = 0f;
        return false;
    }
}
