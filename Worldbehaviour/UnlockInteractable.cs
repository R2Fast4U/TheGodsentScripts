using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interactable that unlocks abilities (and other things) when the player presses Interact in
/// range. Grants secondary/base abilities and divine interventions, optionally coins, fires
/// world effects, and exposes a UnityEvent for anything else. Put it on a GameObject with a
/// trigger Collider2D.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class UnlockInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject interactPrompt;
    [Tooltip("If true, this can only be used once.")]
    [SerializeField] private bool triggerOnce = true;
    [Tooltip("Deactivate this object after use.")]
    [SerializeField] private bool disableAfterUse = false;

    [Header("Unlocks")]
    [SerializeField] private SecondaryAbility[] secondaryAbilities;
    [SerializeField] private BaseAbility[] baseAbilities;
    [SerializeField] private SO_DivineIntervention[] divineInterventions;
    [Tooltip("Also equip the divine interventions (respects the max equipped slots).")]
    [SerializeField] private bool equipDivine = false;
    [Tooltip("Coins granted on unlock (0 = none).")]
    [SerializeField] private int coins = 0;

    [Header("Feedback")]
    [Tooltip("Effects fired on unlock (animation/particles/sound on this or other objects).")]
    [SerializeField] private TriggerEffect[] effectsOnUnlock;
    [Tooltip("Anything else to run on unlock — wire arbitrary methods here.")]
    [SerializeField] private UnityEvent onUnlocked;

    [Tooltip("Save to disk right after unlocking so it isn't lost on quit.")]
    [SerializeField] private bool saveOnUnlock = true;

    private Player player;
    private bool playerInRange;
    private bool used;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (triggerOnce && used) return;

        player = other.GetComponentInParent<Player>();
        playerInRange = player != null;
        if (playerInRange && interactPrompt != null) interactPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        playerInRange = false;
        player = null;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if ((triggerOnce && used) || !playerInRange || player == null || player.InputHandler == null) return;
        if (!player.InputHandler.InteractInput) return;

        player.InputHandler.UseInteractInput();
        Activate();
    }

    private void Activate()
    {
        var stats = player.Stats;
        if (stats != null)
        {
            if (secondaryAbilities != null)
                foreach (var ability in secondaryAbilities)
                    stats.UnlockSecondary(ability);

            if (baseAbilities != null)
                foreach (var ability in baseAbilities)
                    stats.UnlockBase(ability);

            if (divineInterventions != null)
                foreach (var divine in divineInterventions)
                {
                    if (divine == null) continue;
                    stats.UnlockDivine(divine);
                    if (equipDivine) stats.EquipDivine(divine);
                }

            if (coins != 0)
                stats.AddCoins(coins);
        }

        // Feedback / extras.
        if (effectsOnUnlock != null)
            foreach (var effect in effectsOnUnlock)
                if (effect != null) effect.Play();
        onUnlocked?.Invoke();

        if (saveOnUnlock)
            GameManager.Instance?.SaveGame();

        used = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (disableAfterUse) gameObject.SetActive(false);
    }
}
